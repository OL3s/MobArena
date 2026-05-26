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

Current fields:

- `Scene`
- `Damage`
- `UseSourceItemDamage`
- `OnHitScene`
- `OnExpireScene`
- `LifetimeSeconds`
- `MaxHits`
- `CanHitSameTargetMultipleTimes`

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
- gamepad `A`

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
  Scene = ArenaMeleeHitbox.tscn
  UseSourceItemDamage = true
  LifetimeSeconds = 0.16
  MaxHits = 1
  HitboxRadius = 30.0
  ActiveSeconds = 0.12
  ForwardOffset = 8.0
```

## Current Melee Hitbox

`ArenaMeleeHitbox.tscn` is the first reusable effect executor.

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

The intended mob path is the same system, but the action should be authored on `EnemyMobData` instead of an item.

```text
EnemyCombatant AI
  -> EnemyMobData.MainAction
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
  Scene = ArenaMeleeHitbox.tscn
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
- Mob `.tres` files should own mob activation once `EnemyMobData.MainAction` is added.
- `ArenaCombatActionData` owns activation timing and effect reference.
- `ArenaCombatEffectData` subtypes own effect-specific tuning.
- Effect `.tscn` files are reusable runtime executors initialized from config.
- `ArenaCombatant.ApplyDamage(...)` is the common damage entry point.
- Arena result systems own rewards, deaths, contract completion, and scene transitions.

## Next Work

1. Add `EnemyMobData.MainAction`.
2. Add a starter `Slime Bump` action/effect to `slime_green.tres`.
3. Add simple `EnemyCombatant` chase behavior toward the nearest living player.
4. Let enemy AI trigger its authored action in range.
5. Add enemy death cleanup and arena-level victory detection.
6. Add player defeat detection.
7. Add projectile, thrown, and AoE effect configs after melee combat proves stable.
