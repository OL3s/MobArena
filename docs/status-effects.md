# Status Effects

This document records the planned status effect structure for future arena combat work. Status effects are not implemented yet.

## Goal

Status effects should use buildup instead of simple on/off flags. A weak hit can add some buildup without immediately applying a full effect, while repeated exposure can stack enough buildup to cross the active threshold.

This keeps effects like poison, burn, bleed, freeze, and stun from triggering too easily from one small hit.

## Core Model

Each combatant should track a floating-point buildup value per status effect type.

```text
Combatant status state
  Poison: 0.0
  Burn: 0.0
  Bleed: 0.0
  Freeze: 0.0
  Stun: 0.0
```

Each status value ticks downward over time. The effect only applies while the value is at or above that status effect's activation threshold.

Example:

```text
Poison threshold: 60.0

Poison value 59.9 -> not poisoned
Poison value 60.0 -> poisoned
Poison value 72.5 -> poisoned
```

This means poison sources can stack buildup over multiple hits. The target only counts as poisoned once enough poison has accumulated.

## Basic Status Effects

Start with a small core set.

| Status | Example Threshold | Decay Direction | Active Effect |
| --- | ---: | --- | --- |
| Poison | `60.0` | Slow | Deals steady damage over time. |
| Burn | `50.0` | Fast | Deals faster damage over time, but falls off quickly. |
| Bleed | `50.0` | Medium | Deals physical damage over time, best for blades, claws, and piercing hits. |
| Freeze | `70.0` | Medium/Fast | Briefly immobilizes or heavily slows the target after enough cold buildup. |
| Stun | `80.0` | Fast | Briefly prevents movement and actions, then clears or heavily decays. |

Avoid adding more status types until these basic categories work and feel different in combat.

## Armor And Resistance

Status buildup should interact with defense. Armor should not only reduce direct damage; it should also reduce or increase status buildup where the defense type makes sense.

Current combat already has armor mitigation for direct damage through `ArmorData`, `CombatDamageData`, and `CombatDamageEntryData`. Future status buildup should follow the same general idea: apply defense before adding buildup to the target.

Example:

```text
Incoming burn buildup: 30.0
Defense type: Heat
Target has strong heat armor
Actual burn buildup added: reduced amount, for example 18.0
```

Negative armor or vulnerability should be able to increase buildup, matching the current direct-damage armor direction.

Suggested defense mappings:

| Status | Suggested Defense |
| --- | --- |
| Poison | Future poison/toxin defense, or generic status resistance until that exists. |
| Burn | Heat armor. |
| Bleed | Slash or pierce armor, depending on the source hit. |
| Freeze | Cold armor. |
| Stun | Crush armor, or generic status resistance if the source does not fit crush. |

Use existing armor damage types where they fit. Add new defense types only when there is a clear gameplay need.

## Future Data Shape

Possible authored status application data:

```text
StatusEffectApplication
  EffectType
  BuildupAmount
  DefenseType
```

Possible runtime status state:

```text
StatusEffectState
  EffectType
  CurrentValue
  ActivationThreshold
  DecayPerSecond
  TickInterval
  TickDamage
```

Not every status needs every field. For example, stun may not need tick damage, while poison and burn do.

## Open Design Notes

- Decide whether poison gets its own defense type or uses generic status resistance first.
- Decide whether freeze is a full immobilize or a very heavy slow.
- Decide whether stun clears immediately after triggering or decays sharply below its threshold.
- Keep status buildup separate from direct damage so an attack can deal damage, add status buildup, or do both.
