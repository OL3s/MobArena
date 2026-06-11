# Status Effects

This document records the current status effect structure for arena combat.

## Goal

Status values use the same rough number scale as combat damage. `100` status value represents about `1.0` second of active effect. Values tick downward at `100` value per second.

Repeated weak attacks do not stack into a strong status. When an attack applies a status value, runtime state uses `max(currentValue, actualHitValue)` rather than addition. Each individual hit must be strong enough to matter.

## Runtime Model

Each combatant tracks a floating-point value per status effect type.

```text
ArenaCombatState
  Poison: current value
  Stun: current value
```

Example:

```text
Poison value from hit: 300

Current poison value 0 -> set to 300
Current poison value 200, new weak hit 80 -> stays 200
Current poison value 200, new strong hit 450 -> set to 450
```

`100` value is about one second, so `300` poison lasts about three seconds before profile caps and normal decay.

## Combatant Status Profile

Status tuning lives on `CombatantStatusProfileData`.

```text
CombatantStatusProfileData
  EffectDefenseProfile: EffectDefenseData
  StatusRules: StatusEffectRulesData
  ImmuneStatuses: StatusEffectType[]
  StateStatusMultipliers: CombatantStateStatusMultiplierData[]
```

Enemies store this through `EnemyMobData.StatusProfile`. If a mob does not explicitly set one, `EnemyCombatant` falls back to `default_mob_status_profile.tres` or `default_champion_status_profile.tres` for `ChampionMobData`.

Players currently use `default_player_status_profile.tres`.

## Min, Max, And Immunity

`StatusEffectRulesData` uses the same base plus override structure as armor and effect defense.

```text
StatusEffectRulesData
  BaseMinValue
  MinValueOverrides[]
  BaseMaxValue
  MaxValueOverrides[]
```

Min means the stored status value is not behaviorally active unless `currentValue > minValue`. Below-min values can still be stored for UI/debug visibility, but they do not affect the enemy.

Max means the stored status value is capped.

Runtime application:

```text
rawValue = authored value or appliedDamage * multiplier
stateScaledValue = rawValue * state/status multiplier
defendedValue = effectDefense.Apply(stateScaledValue)

if status is immune:
  ignore
else:
  currentStatus = min(maxValue, max(currentStatus, defendedValue))
  active = currentStatus > minValue
```

Example:

```text
Stun min = 25
Stun max = 100

Incoming stun after defense = 20 -> stored but inactive
Incoming stun after defense = 80 -> stored and active
Incoming stun after defense = 140 -> stored as capped 100 and active
```

## Effect Defense

Status values interact with `EffectDefenseData`. Positive effect defense reduces status value with the same non-linear shape as armor. Zero defense leaves it unchanged. Negative defense is vulnerability and increases the value.

```text
EffectDefenseData
  BaseValue
  EffectDefenseTypeOverrideData[]
```

Effect defense handles status values such as Poison and Stun. Damage immunity is handled separately by `ArmorData.ImmuneTypes`.

## Stun

Stun is both a status value and a combatant state. While active, it sets `ArenaCombatantState.Stunned` and prevents movement/actions.

Stun usually derives from applied damage:

```text
rawStunValue = appliedDamage * AppliedDamageMultiplier
```

With the new scale, the default multiplier should generally be `1`, so `100` applied damage can represent about one second of stun before defense, state multipliers, min threshold, and cap.

Do not treat Stun as a normal coating or generic status add-on. Stun is an impact/control result and should usually come from applied damage, force, weapon impact, or an explicitly authored stun attack. Only special stun fantasies such as a stun bomb, shock oil, or concussive plating should author direct Stun values.

Windup stun vulnerability is data-driven through `CombatantStateStatusMultiplierData`:

```text
State = Windup
Type = Stun
Multiplier = 2.0
```

Champions resist stun through their champion status profile: higher effect defense, higher stun min, lower stun max, or weaker windup multiplier.

## Poison

Poison is the normal authored status example.

```text
StatusEffectApplicationData
  Type = Poison
  Value = 300
  UseAppliedDamage = false
```

Poison currently ticks `100` raw damage per second while active, matching the updated health/damage scale.

## Open Design Notes

- Decide whether freeze should be a full immobilize or a heavy slow when added.
- Decide if future statuses need active thresholds separate from application min.
- Keep status buildup separate from direct damage so an attack can deal damage, add status value, or do both.
