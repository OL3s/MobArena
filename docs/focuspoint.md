# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Current Status

The current town-management foundation is in place.

- Main flow is `Main Menu -> Town`; `scenes/arena.tscn` is still a combat placeholder.
- Roster management happens through the town-center `RosterYard`. The old separate Roster Hall scene path has been removed.
- Modal UI should go through `GlobalOverlay`; legacy `SceneOverlay` and `ConfirmationOverlay` have been removed.
- Company state is split between `CompanyRunData` for current mutable run state and `CompanyCareerData` for long-term totals. `SaveNode` should remain the save/load/runtime boundary.
- Town drag/drop is a core management system. Current payloads are gladiators, equipment items, and rations. Town buildings and roaming roster-yard gladiators can receive drops through `ITownDragDropTarget`.
- `CompanyRunData.TownAssignments` owns assignment lists for courtyard, arena, healer, and training hall. Arena capacity follows `LocalInputConfig.ControllerSetups.Count`.
- Arena, Healer, and Training Hall overlays show assigned gladiator rows. Arena also has control assignment setup stored in `CompanyRunData.ArenaControlAssignments`.
- Market/rations/blacksmith foundations exist: rations can be bought/sold/fed, item resources and item stock exist, blacksmith purchasing adds items to `CompanyRunData.Inventory`, and gladiator recruitment is functional through `MarketData.GladiatorStock`.
- Equipment ownership exists, but equipping/unequipping items onto gladiators is not implemented yet.
- Contract/combat resources are not implemented yet. Arena contract cards are still mock cards and arena combat startup is still placeholder.

## Next Focus

Work the short-term backlog in this order unless the user redirects.

1. `#18 Equip inventory items onto gladiators with validation`
2. `#3 Add item combat stats and equipment requirements`
3. `#49 Improve gladiator market recruit cards and variety`
4. `#10 Implement Healer overlay with paid health recovery`
5. `#13 Apply town open/closed behavior to buildings and overlays`
6. `#11 Implement Training Hall progression actions`
7. `#16 Implement MVP XP and level progression`

## Immediate Direction

- Start with equipment assignment: let players equip/unequip items from `CompanyRunData.Inventory` onto a gladiator's `GladiatorEquipmentData`.
- Preserve slot rules: armor/main-hand/off-hand type validation, two-handed main-hand clearing or blocking off-hand, and replaced equipment returning to inventory.
- Prefer using the existing town drag/drop system. Dragging equipment onto roaming roster-yard gladiators should be the primary physical interaction, with an overlay/button path only if needed for clarity.
- Keep item movement centralized through `CompanyRunData` helpers and emit `RunChanged` after mutations.
- Keep authored `.tres` item files immutable templates. Runtime owned/stock items should remain duplicated resource instances.
- Add clear invalid-action feedback for unaffordable, wrong slot, missing item, dead/cemetery gladiator, or requirement failures. Use concise labels/tooltips or `GlobalOverlay` popups.
- Avoid adding parallel UI-local state for inventory, equipment, market stock, assignments, or selected combat participants.

## Short-Term Direction

- Improve gladiator market cards and recruit variety after the equip path is usable. Hiring must stay centralized through `CompanyRunData.TryBuyGladiator`/`AddGladiator` so purchase value and `CompanyCareerData.TotalGladiatorsInCareer` stay correct.
- Make Healer actions spend current gold and restore health for assigned living gladiators. Do not restore stamina there.
- Wire town open/closed rules through `TownTimeState.IsTownOpen()` and `AreStoresOpen()` rather than duplicating time logic in overlays.
- Build Training Hall actions around the XP/progression system once `#16` defines the MVP rules.
- Keep gladiator death centralized through `CompanyRunData.KillGladiator`; dead gladiators move from active `Gladiators` to `Cemetery`, not an active-roster dead flag.

## Later, Not Now

- `#4`, `#8`, and `#20-#25` cover ContractData, CombatResultData, real arena contract launch, player gladiator spawning, slime enemies, combat resolution, and arena HUD/result flow.
- `#26-#30` cover champion contract/deadline behavior after the real contract/combat path exists.
- `#31-#35` cover controller/touch/responsive/accessibility polish after core flows are functional.
- `#36-#41` cover MVP checklist, balance, bug bash, docs, and release tagging after the playable loop exists.
