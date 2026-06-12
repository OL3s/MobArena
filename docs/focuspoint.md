# Focus Point

This file is the short-term handoff for the next agent or developer. Keep durable architecture in [architecture.md](architecture.md), current-system references in topic docs, and post-MVP direction in [roadmap.md](roadmap.md).

## Current Status

- Main flow is `Main Menu -> Town -> Arena`.
- Town management is implemented at prototype level through `RosterYard`, reusable town buildings, drag/drop, Market, Recovery Bay, Training Hall, arena contracts, control setup, phase work, weather, champion cadence, and tutorial gates.
- Arena has first-pass runtime flow: assigned player spawning, contract enemy spawning, combat HUD, runtime combat state, main/off-hand player actions, enemy death victory detection, all-player defeat detection, win/loss/forfeit resolution, champion force-retirement, and demo completion.
- Enemy AI and family-specific behavior scenes are not implemented yet. Enemy resources and generic fallback spawning exist.
- Touch can join arena control setup, but touch combat input is not implemented yet.
- Player main-hand and off-hand item actions activate authored `ArenaCombatActionData`. Ability and block inputs are read but do not activate authored gameplay actions yet.
- Resource-driven melee, linear projectile, thrown projectile, and area-of-effect executors exist under `scenes/components/arena/combat/effects/`.
- `tests/attack_effect_sandbox.tscn` loads `tests/attacks/**/*.tres` and is the main manual sandbox for attack/effect patterns.
- Item resources now include non-melee starter coverage such as bows, crossbow, and poison flask, but gameplay tuning and normal-arena verification still need work.
- Market item stock still uses the debug/all-items catalog path. A curated/progression-aware generator is still needed.
- Weather default is `Cloudy`; weather randomizes through `PhaseTransitionController` during phase advancement.

## Current Priority

Continue polishing the resource-driven attack/effect and item path before deepening enemy AI.

Primary goal: verify and tune authored non-melee player items in normal arena/player activation, not only through resource loading or sandbox spawning.

## Next Work

1. Create a dedicated market item stock generator for the Night -> Day refresh cycle so stock is curated/progression-aware instead of using the current debug all-items catalog.
2. Verify bow/crossbow linear projectile items through normal arena player activation.
3. Verify the poison flask thrown-projectile-to-AOE chain through normal arena player activation, including buildup range scaling, acid ticks, and poison lingering.
4. Confirm item store, codex, and item cards display action-pattern icons clearly for non-melee items.
5. Tune costs, requirements, level multipliers, stamina costs, hit sizes, projectile speed/range, AOE timing, and poison values after practical playtesting.
6. Keep item behavior authored through item `.tres` action/effect subresources. Do not hardcode weapon behavior in `PlayerCombatant` or executor scenes.
7. Add block activation from the existing block input path after starter item coverage is playable.
8. Add ability activation from the existing ability input path after block/action basics are stable.
9. Add mob behavior-composition scenes after player action/item coverage is usable, starting with slime movement/chase and a starter slime attack.

## Immediate Rules

- Use [project-overview.md](project-overview.md) as the docs hub.
- Use [architecture.md](architecture.md) for ownership and boundary decisions.
- Use [arena-combat.md](arena-combat.md) for arena runtime behavior.
- Use [arena-combat-actions.md](arena-combat-actions.md) and [authoring-attacks.md](authoring-attacks.md) for action/effect work.
- Use [damage-types.md](damage-types.md) and [status-effects.md](status-effects.md) for combat payload details.
- Use [roadmap.md](roadmap.md) for post-MVP ideas such as dedicated Recovery Bay/Training Hall scenes and multiplayer/input-only clients.
- Run `godot --headless --import`, `dotnet build`, and `godot --headless --quit` after changing action/effect resources, scenes, or C# code.
