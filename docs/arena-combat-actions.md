# Arena Combat Actions

This document explains the current resource-driven arena combat action system.

For practical authoring steps, see `docs/authoring-attacks.md` and `docs/authoring-player-items.md`. This file focuses on architecture and runtime behavior.

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

`ArenaCombatState` stores runtime health, armor, and status behavior:

- `MaxHealth`
- `CurrentHealth`
- `ArmorProfile`
- `StatusProfile`
- `ApplyDamage(CombatDamageData)`
- `ApplyRawDamage(int)`
- `ApplyStatusEffect(StatusEffectType, float, ArenaCombatantState)`
- `TickStatusEffects(float)`
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
EnemyMobData.StatusProfile
  -> EnemyCombatant.CombatState
```

Effect scenes should damage targets through:

```csharp
target.ApplyDamage(damage, source);
```

They should not mutate `GladiatorData`, `EnemyMobData`, `CompanyRunData`, contract rewards, or arena results directly.

## Combatant States

Arena combatants use `ArenaCombatantState` for short-lived runtime behavior states. This is separate from the town-management `GladiatorData.Exhaustion` condition.

Current movement multipliers:

- `Default`: `1.0`
- `Exhausted`: `0.5`
- `Release`: `0.2`
- `Windup`: `0.1`
- `Stunned`: `0.0`

Players enter `Exhausted` when they try to perform an action whose `StaminaCost` is higher than their current stamina. The action does not activate. Player exhaustion clears after stamina regenerates back to the failed action's stamina cost, capped by the player's recoverable max stamina, and after a minimum time window. That minimum starts at `1.0` second and is reduced by a non-linear diminishing-returns curve from the gladiator's Endurance level, approaching a `0.25` second floor.

Player normal actions can start only from `Default`. `Exhausted` still allows movement and input reading, but it blocks starting another normal action until it clears.

`Exhausted` is also available as a normal combatant state for future mob behavior and status/profile tuning.

## Damage And Immunity

`CombatDamageData` has one damage array:

- `Entries`: typed instant damage such as Slash, Pierce, Crush, Heat, Cold, Acid, Silver, and Holy.

Damage uses `ArmorData.BaseValue` unless an `ArmorTypeOverrideData` exists for that damage type.

`ArmorData.ImmuneTypes` ignores listed damage types completely. By default, armor is immune to `Silver` and `Holy`; a specific armor profile can remove that by setting a different `ImmuneTypes` array in its `.tres`.

Example:

```text
Weapon damage: Slash 80 + Holy 100
Target armor ImmuneTypes: [Silver, Holy]
Result: Slash resolves normally, Holy is ignored.

Weapon damage: Slash 80 + Holy 100
Target armor ImmuneTypes: [Silver]
Target armor TypeOverrides: [Holy 0]
Result: Slash resolves normally, Holy applies at full value.

Weapon damage: Slash 80 + Holy 100
Target armor ImmuneTypes: [Silver]
Target armor TypeOverrides: [Holy 50]
Result: Slash resolves normally, Holy is mitigated by defense 50.

Weapon damage: Slash 80 + Holy 100
Target armor ImmuneTypes: [Silver]
Target armor TypeOverrides: [Holy -25]
Result: Slash resolves normally, Holy is increased by vulnerability 25%.
```

This keeps damage resolution normalized: Holy and Silver are normal damage types, and immunity decides whether a target ignores them.

## Core Resources

### ArenaCombatActionData

`ArenaCombatActionData` describes how an action is activated.

Current fields:

- `DisplayName`
- `Effect`
- `Buildup`
- `WindupSeconds`
- `StaminaCost`
- `SpawnDistance`
- `MaxChainDepth`

It answers:

```text
When this item or mob activates, what effect should be spawned and with what basic timing?
```

### ArenaCombatApplyData

`ArenaCombatApplyData` describes what a successful hit applies to a target.

Current exported fields:

- `Damage`
- `UseSourceItemDamage`
- `ForceStrength`
- `StatusApplications`

It answers:

```text
When this effect hits, what damage, force, and status values should be applied?
```

Damage resolution currently works like this:

```text
UseSourceItemDamage is true and source item has DamageItemData.Damage
  -> use the source item's DamageItemData.Damage
else
  -> use apply Damage, which may be null
```

Force resolution currently works like this:

```text
ForceStrength <= 0
  -> no force
else
  -> attack direction * ForceStrength
```

Status application uses `StatusEffectApplicationData` rows and the target's `CombatantStatusProfileData`. Runtime status values use `100` as about one second. Incoming status values do not stack additively; targets keep `max(currentValue, actualHitValue)` so weak repeated hits do not spam-build statuses. Status profiles also define min thresholds, max caps, immunity, effect defense, and state/status multipliers such as Windup + Stun.

### ArenaCombatEffectData

`ArenaCombatEffectData` is the base config for spawned effects.

Current exported fields:

- `ScenePath`
- `Apply`
- `OnHitEffect`
- `OnExpireEffect`
- `OnHitScenePath`
- `OnExpireScenePath`
- `LifetimeSeconds`
- `MaxHits`
- `CanHitSameTargetMultipleTimes`

The resource also exposes computed `PackedScene` properties named `Scene`, `OnHitScene`, and `OnExpireScene` by loading those paths at runtime.

It answers:

```text
What scene gets spawned, what apply payload should it use, and what shared hit rules apply?
```

Null `Apply` remains valid for future pure-spawner effects. Null `Apply.Damage` remains valid for force-only or status-only hits.

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
- keyboard-only uses movement direction for facing/aim
- optional aim with mouse cursor for mouse-mode players
- optional aim with gamepad right stick or right mousepad-style input

Independent aim must not be required by arena action logic. If no separate aim input is present, movement direction should continue to drive facing and action direction. This supports keyboard-only play, simpler movement-only controls, more gamepad/device layouts, and newer players who prefer not to manage movement and aim separately. Mouse aiming should be controlled by a settings toggle that defaults on.

Additional first-pass action inputs exist but do not activate authored effects yet: keyboard `E`/mouse right/gamepad `A` for off-hand, keyboard `F`/mouse-mode `Q`/gamepad `B` for ability, and keyboard `Q`/mouse-mode `Space`/gamepad `Y` for block.

The player path uses source item damage by default.

When an equipped hand item is missing, `PlayerCombatant` uses hidden default punch resources from `resources/combat/player_defaults/`. The main-hand punch is stronger than the off-hand punch, and both still flow through `ArenaCombatActionData`/`ArenaMeleeEffectData` instead of hardcoded damage.

Actions may optionally set `Buildup` to an `ArenaCombatBuildupData` resource. If `Buildup` is null, the action keeps the normal press-to-register behavior. If `Buildup` is present, the first press starts buildup and the second press releases the action with a scalar from `0.1` to `1.0` based on `BuildupSeconds`. The buildup config chooses which authored values use the scalar, such as range, speed, or damage. Current sandbox thrown attacks use buildup with range scaling only.

`ArenaCombatActionData.MaxChainDepth` limits initialized effect chaining and defaults to 12. Set it lower for tests that should prove a chain stops quickly, or higher for deliberate multi-stage attacks. Runtime logs include action name, effect type, buildup scalar, chain depth, hits, target health, and chain-depth blocks.

Each attack effect data subtype owns a type label and icon path. Current icons live under `assets/ui/attacks/` for melee, linear projectile, thrown projectile, and area-of-effect. Item cards and item store showcases traverse the root effect plus `OnHitEffect`/`OnExpireEffect` chains and stack those icons so the item preview shows the action pattern at a glance.

Example item resource structure:

```text
training_sword.tres
  Damage = Resource_training_sword_damage
  MainAction = Resource_training_sword_main_action

Resource_training_sword_main_action: ArenaCombatActionData
  DisplayName = "Sword Slash"
  Effect = Resource_training_sword_melee_effect
  WindupSeconds = 0.04
  SpawnDistance = 34.0

Resource_training_sword_melee_effect: ArenaMeleeEffectData
  ScenePath = res://scenes/components/arena/combat/effects/ArenaMeleeHitbox.tscn
  Apply = Resource_training_sword_apply
  LifetimeSeconds = 0.16
  MaxHits = 1
  HitboxRadius = 30.0
  ActiveSeconds = 0.12
  ForwardOffset = 8.0
```

## Current Melee Hitbox

`ArenaMeleeHitbox.tscn` is the starter reusable melee executor. Runtime arena combat effect executors and helpers live under `scenes/components/arena/combat/effects/`; authored effect config resources live under `scripts/resources/combat/effects/`.

It:

- uses `Area2D`
- initializes from `ArenaMeleeEffectData`
- applies damage through `ArenaCombatant.ApplyDamage(...)`
- applies force through `ArenaCombatant.AddExternalForce(...)`
- applies status values through `ArenaCombatant.ApplyStatusEffect(...)`
- tracks hit targets
- honors `MaxHits`
- honors active time and lifetime
- can spawn initialized chained `OnHitEffect`/`OnExpireEffect` resources
- can spawn raw `OnHitScene`/`OnExpireScene` scenes for visual-only followups

The hitbox does not own rewards, victory checks, death cleanup, or save data.

## Current Projectile And Area Executors

The first reusable non-melee executors are present but are not yet wired into starter item resources. `tests/attack_effect_sandbox.tscn` loads every `ArenaCombatActionData` `.tres` under `tests/attacks/` into its dropdown for scenario testing, then spawns the selected attack at the mouse position when `F` is pressed.

### ArenaAttackLinearProjectile

`ArenaAttackLinearProjectile.tscn` initializes from `ArenaAttackLinearProjectileData`.

It:

- uses `Area2D` with a forward-offset `RectangleShape2D` hitbox
- moves at constant `Speed` until `Range` is reached
- keeps a prepacked scene shadow on the root ground path while the visual child is offset upward by `VisualHeight` for fake 2D height
- exposes `ShadowScale` and `ShadowAlpha` so authored projectiles can tune the ground shadow without generating it in code
- can use an authored `VisualTexture` and `VisualDisplayHeight`; otherwise it falls back to the procedural rectangular visual
- applies optional `Apply` damage/status/force on collision
- tracks targets already hit
- uses `MaxPenetrations` before being destroyed by hits
- can spawn initialized `OnHitEffect`/`OnExpireEffect` resources
- can spawn raw `OnHitScene`/`OnExpireScene` scenes

This is for arrows, bolts, spear-like shots, and magic shots that have an active hitbox during travel.

### ArenaAttackThrownProjectile

`ArenaAttackThrownProjectile.tscn` initializes from `ArenaAttackThrownProjectileData`.

It:

- travels over `TravelSeconds` toward `Range`
- fakes height with `y_offset = -sin(progress * PI) * ArcHeight`
- keeps a prepacked scene shadow on the root ground path and only moves the visual child on the fake height axis
- lerps shadow scale/alpha from ground values to apex values based on arc height
- can use an authored `VisualTexture` and `VisualDisplayHeight`; otherwise it falls back to the procedural circular visual
- does not apply damage during flight
- spawns optional initialized `OnExpireEffect` or raw `OnExpireScene` on landing

This is for bottles, bombs, and vials where the landing/destruction scene owns damage or area behavior.

### ArenaAttackAreaOfEffect

`ArenaAttackAreaOfEffect.tscn` initializes from `ArenaAttackAreaOfEffectData`.

It:

- uses `Area2D` with a circular hit zone
- has `LifetimeSeconds`, `Radius`, and `TickSeconds`
- hits immediately when a target enters or is already inside
- tracks per-target tick cooldowns while targets remain inside
- can run with unlimited hits or honor `MaxHits`
- uses distinct default green circle visual colors, separate from melee
- can spawn initialized or raw chained followups on hit/expiry

This is for poison clouds, fire patches, explosions, shockwaves, healing zones, and other area-of-effect attacks.

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
  WindupSeconds = 0.05
  SpawnDistance = 24.0

Resource_slime_green_bump_apply: ArenaCombatApplyData
  Damage = Resource_slime_green_bump_damage
  UseSourceItemDamage = false

Resource_slime_green_bump_effect: ArenaMeleeEffectData
  ScenePath = res://scenes/components/arena/combat/effects/ArenaMeleeHitbox.tscn
  Apply = Resource_slime_green_bump_apply
  LifetimeSeconds = 0.14
  MaxHits = 1
  HitboxRadius = 24.0
  ActiveSeconds = 0.1
  ForwardOffset = 4.0
```

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

1. Use `tests/attack_effect_sandbox.tscn` to exercise the `tests/attacks/**/*.tres` melee, linear projectile, thrown projectile, and area-of-effect scenarios against 9 training dummies.
2. Add authored starter item resources that use `ArenaAttackLinearProjectileData`, `ArenaAttackThrownProjectileData`, and `ArenaAttackAreaOfEffectData` outside the sandbox.
3. After visible attack/effect scenes are in place, add optional enemy movement/attack/logic components under `EnemyCombatant`-rooted scenes.
4. Add `EnemyMobData.MainAction` or a minimal enemy action component when the first mob attack needs authored tuning.
5. Add `SlimeEnemyCombatant.tscn` and assign slime mob `Scene` fields once slime movement/attack behavior exists.
6. Add enemy death cleanup, arena-level victory detection, and player defeat detection.
