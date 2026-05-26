# Arena Combat Actions

This document explains the current resource-driven arena combat action system.

## Goal

Arena combat should avoid hardcoding weapon, mob, projectile, thrown-item, or AoE behavior directly in `PlayerCombatant` or `EnemyCombatant`.

Instead, authored data chooses what action happens, while reusable scene executors perform the runtime behavior.

```text
Authored item or mob .tres
  -> ArenaCombatActionData
      -> ArenaCombatEffectData subtype
          -> reusable effect .tscn
```

## Runtime Combat State

Every arena combatant owns an `ArenaCombatState` resource.

```text
ArenaCombatant
  CombatState: ArenaCombatState
  Team: ArenaCombatTeam
```

`ArenaCombatState` stores runtime health and armor behavior:

- `MaxHealth`
- `CurrentHealth`
- `ArmorProfile`
- `ApplyDamage(CombatDamageData)`
- `ApplyRawDamage(int)`
- `Heal(int)`
- `SetHealth(int)`
- `HealthChanged`
- `Died`

Players configure this state from `GladiatorData`:

```text
GladiatorData.Health
GladiatorData.MaxHealth
GladiatorData.Equipment.Armor.ArmorProfile
  -> PlayerCombatant.CombatState
```

Enemies configure this state from `EnemyMobData`:

```text
EnemyMobData.MaxHealth
EnemyMobData.ArmorProfile
  -> EnemyCombatant.CombatState
```

Effect scenes should damage targets through:

```csharp
target.ApplyDamage(damage, source);
```

They should not mutate `GladiatorData`, `EnemyMobData`, `CompanyRunData`, contract rewards, or arena results directly.

## Core Resources

### ArenaCombatActionData

`ArenaCombatActionData` describes how an action is activated.

Current fields:

- `DisplayName`
- `Effect`
- `CooldownSeconds`
- `WindupSeconds`
- `StaminaCost`
- `SpawnDistance`

It answers:

```text
When this item or mob activates, what effect should be spawned and with what basic timing?
```

### ArenaCombatEffectData

`ArenaCombatEffectData` is the base config for spawned effects.

Current exported fields:

- `ScenePath`
- `Damage`
- `UseSourceItemDamage`
- `OnHitScenePath`
- `OnExpireScenePath`
- `LifetimeSeconds`
- `MaxHits`
- `CanHitSameTargetMultipleTimes`

The resource also exposes computed `PackedScene` properties named `Scene`, `OnHitScene`, and `OnExpireScene` by loading those paths at runtime.

It answers:

```text
What scene gets spawned, what damage should it use, and what shared hit rules apply?
```

Damage resolution currently works like this:

```text
UseSourceItemDamage is true and source item has DamageItemData.Damage
  -> use the source item's DamageItemData.Damage
else
  -> use effect Damage, which may be null
```

This makes authored effect damage the normal case and source item damage the special reusable-item case. Null damage remains valid for future effects such as poison vials that deal no direct hit damage but spawn a cloud scene.

### ArenaMeleeEffectData

`ArenaMeleeEffectData` is the first concrete effect config.

Current fields:

- `HitboxRadius`
- `ActiveSeconds`
- `ForwardOffset`

It configures `ArenaMeleeHitbox.tscn`.

## Runtime Effect Execution

### ArenaCombatActionRunner

`ArenaCombatActionRunner.TryActivate(...)` is the generic action spawner.

It:

- validates source/action/effect/scene
- instantiates the effect scene
- places it in front of the source combatant
- builds an `ArenaCombatEffectContext`
- calls `IArenaCombatEffect.Initialize(...)` on the spawned scene

### ArenaCombatEffectContext

`ArenaCombatEffectContext` carries runtime data into spawned effects:

- `Source`
- `SourceTeam`
- `SourceItem`
- `ItemDamage`
- `Action`
- `Effect`
- `Direction`

### IArenaCombatEffect

Effect scenes implement:

```csharp
public interface IArenaCombatEffect
{
    void Initialize(ArenaCombatEffectContext context);
}
```

## Current Player Flow

Players can currently trigger main-hand melee actions.

```text
Player input
  -> PlayerCombatant
  -> GladiatorData.Equipment.MainHand
  -> DamageItemData.MainAction
  -> ArenaCombatActionRunner.TryActivate(...)
  -> ArenaMeleeHitbox.tscn
  -> target.ApplyDamage(...)
```

Current first-pass input:

- keyboard `Space`
- mouse left
- gamepad `X`

Additional first-pass action inputs exist but do not activate authored effects yet: keyboard `E`/mouse right/gamepad `A` for off-hand, keyboard `F`/mouse-mode `Q`/gamepad `B` for ability, and keyboard `Q`/mouse-mode `Space`/gamepad `Y` for block.

The player path uses source item damage by default.

Example item resource structure:

```text
training_sword.tres
  Damage = Resource_training_sword_damage
  MainAction = Resource_training_sword_main_action

Resource_training_sword_main_action: ArenaCombatActionData
  DisplayName = "Sword Slash"
  Effect = Resource_training_sword_melee_effect
  CooldownSeconds = 0.55
  WindupSeconds = 0.04
  SpawnDistance = 34.0

Resource_training_sword_melee_effect: ArenaMeleeEffectData
  ScenePath = res://scenes/components/arena/combat/effects/ArenaMeleeHitbox.tscn
  UseSourceItemDamage = true
  LifetimeSeconds = 0.16
  MaxHits = 1
  HitboxRadius = 30.0
  ActiveSeconds = 0.12
  ForwardOffset = 8.0
```

## Current Melee Hitbox

`ArenaMeleeHitbox.tscn` is the first reusable effect executor. Runtime arena combat effect executors and helpers live under `scenes/components/arena/combat/effects/`; authored effect config resources live under `scripts/resources/combat/effects/`.

It:

- uses `Area2D`
- initializes from `ArenaMeleeEffectData`
- applies damage through `ArenaCombatant.ApplyDamage(...)`
- tracks hit targets
- honors `MaxHits`
- honors active time and lifetime
- can spawn `OnHitScene`
- can spawn `OnExpireScene`

The hitbox does not own rewards, victory checks, death cleanup, or save data.

## Planned Mob Flow

Mobs do not activate actions yet.

The intended mob path should use the same action/effect system, but enemy behavior should be composed through scenes and child components instead of turning `EnemyCombatant` into one large AI switchboard.

Recommended ownership:

```text
EnemyMobData .tres
  -> identity, stats, visuals, armor, fame, optional behavior scene override

EnemyCombatant.tscn
  -> generic fallback shell for health, team, collision, visuals, and damage entry points

Family or unique enemy .tscn
  -> EnemyCombatant root plus movement, attack, and logic child components

ArenaCombatActionData / ArenaCombatEffectData
  -> authored activation and effect tuning

Reusable effect .tscn
  -> visible runtime executor initialized from config
```

`EnemyMobData.Scene` should be treated as an override. `null` means the arena uses the fallback generic `EnemyCombatant.tscn`; a non-null scene means this mob or family needs custom behavior composition.

Example future slime setup:

```text
SlimeEnemyCombatant.tscn
  EnemyCombatant
    SlimeMovementController
    EnemyMeleeAttackController
    ChaseNearestPlayerBrain

slime_green.tres
  Scene = SlimeEnemyCombatant.tscn
  MaxHealth = 12
  ArmorProfile = Resource_slime_green_armor
  MainAction = Resource_slime_green_bump_action
```

The action trigger path would then be:

```text
Enemy logic/attack component
  -> EnemyMobData.MainAction or component-assigned action
  -> ArenaCombatActionRunner.TryActivate(enemy, null, action)
  -> ArenaMeleeHitbox.tscn
  -> player.ApplyDamage(...)
```

Because mobs do not have item damage, mob effects should usually set `Damage` directly and set `UseSourceItemDamage = false`.

Example future mob resource structure:

```text
slime_green.tres
  MaxHealth = 12
  ArmorProfile = Resource_slime_green_armor
  MainAction = Resource_slime_green_bump_action

Resource_slime_green_bump_damage: CombatDamageData
  Entries = [Crush 3]

Resource_slime_green_bump_action: ArenaCombatActionData
  DisplayName = "Slime Bump"
  Effect = Resource_slime_green_bump_effect
  CooldownSeconds = 0.9
  WindupSeconds = 0.05
  SpawnDistance = 24.0

Resource_slime_green_bump_effect: ArenaMeleeEffectData
  ScenePath = res://scenes/components/arena/combat/effects/ArenaMeleeHitbox.tscn
  Damage = Resource_slime_green_bump_damage
  UseSourceItemDamage = false
  LifetimeSeconds = 0.14
  MaxHits = 1
  HitboxRadius = 24.0
  ActiveSeconds = 0.1
  ForwardOffset = 4.0
```

## Future Effect Types

The current resources are designed to support more effect config subtypes later.

### Projectile

Likely future files:

```text
ArenaProjectileEffectData.cs
ArenaProjectile.tscn
```

Expected config:

- speed
- max distance
- pierce count
- wall hit scene
- expire scene
- optional impact scene

Example use cases:

- arrows
- magic bolts
- poison globules
- fireballs that spawn explosions

### Thrown Projectile

Likely future files:

```text
ArenaThrownEffectData.cs
ArenaThrownProjectile.tscn
```

Expected config:

- speed or duration
- arc height
- target distance
- `IsBounceable`
- max bounces
- can hit during flight
- can hit on landing
- floor hit scene
- wall hit scene

The visual arc should be faked by moving a child visual along:

```text
y_offset = -sin(progress * PI) * ArcHeight
```

The root can stay on the ground path for collision and placement.

### Area Effect

Likely future files:

```text
ArenaAreaEffectData.cs
ArenaAreaEffect.tscn
```

Expected config:

- radius or shape
- duration
- tick interval
- one-shot or ticking behavior
- per-target repeat rules

Example use cases:

- poison cloud
- fire patch
- explosion
- shockwave
- healing zone

## Ownership Rules

- Item `.tres` files own item activation through `DamageItemData.MainAction`.
- Mob `.tres` files own enemy identity, stats, visuals, armor, fame, and optional behavior scene overrides.
- Mob `.tscn` scenes own behavior composition through child movement, attack, and logic components.
- Mob action tuning may live on `EnemyMobData` once `EnemyMobData.MainAction` is added, or on a small behavior/action component when scene-local behavior needs it.
- `ArenaCombatActionData` owns activation timing and effect reference.
- `ArenaCombatEffectData` subtypes own effect-specific tuning.
- Effect `.tscn` files are reusable runtime executors initialized from config.
- `ArenaCombatant.ApplyDamage(...)` is the common damage entry point.
- Arena result systems own rewards, deaths, contract completion, and scene transitions.
- `EnemyCombatant` should stay a shared runtime shell, not the place where every family movement or attack rule is switched.

## Next Work

1. Make the current melee effect scene visibly usable in arena with clear placement, timing, hit, and damage/debug feedback.
2. Add reusable visible projectile effect data and `.tscn` executors.
3. Add reusable visible thrown-projectile effect data and `.tscn` executors.
4. Add reusable visible area-effect data and `.tscn` executors.
5. After visible attack/effect scenes are in place, add optional enemy movement/attack/logic components under `EnemyCombatant`-rooted scenes.
6. Add `EnemyMobData.MainAction` or a minimal enemy action component when the first mob attack needs authored tuning.
7. Add `SlimeEnemyCombatant.tscn` and assign slime mob `Scene` fields once slime movement/attack behavior exists.
8. Add enemy death cleanup, arena-level victory detection, and player defeat detection.
