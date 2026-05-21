# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Current Status

The current town-management foundation is in place.

- Main flow is `Main Menu -> Town`; `scenes/arena.tscn` is still a combat placeholder.
- Roster management happens through the town-center `RosterYard`. The old separate Roster Hall scene path has been removed.
- Modal UI should go through `GlobalOverlay`; legacy `SceneOverlay` and `ConfirmationOverlay` have been removed.
- Company state is split between `CompanyRunData` for current mutable run state and `CompanyCareerData` for long-term totals. `SaveNode` should remain the save/load/runtime boundary.
- Town drag/drop is a core management system. Current payloads are gladiators and equipment items. Town buildings and roaming roster-yard gladiators can receive drops through `ITownDragDropTarget`.
- `CompanyRunData.TownAssignments` owns assignment lists for courtyard, arena, Thermae, and training hall. Arena currently supports up to four assigned gladiators.
- Arena, Thermae, and Training Hall overlays show assigned gladiator rows. Thermae and Training Hall also expose focus selectors. Arena control assignment is now configured per contract launch from the Arena overlay.
- Market/blacksmith foundations exist: item resources and item stock exist, blacksmith purchasing adds items to `CompanyRunData.Inventory`, and gladiator recruitment is functional through `MarketData.GladiatorStock`.
- The arena-first branch removes continuous town time, legacy supply upkeep, and the champion deadline timer. The town loop now uses `TownPhaseState` with only Day and Night.
- Returning from a completed arena contract completes Day -> Night through `PhaseTransitionController.CompleteArenaContract`, which applies fight exhaustion to arena gladiators, returns them to the courtyard, and clears arena control assignments. Generic debug Day -> Night still uses `CompleteArenaDay`. The town HUD `Next Day` button is enabled only at Night and calls `PhaseTransitionController.AdvanceToNextDay`.
- The town HUD Day action is `Select Contract` and opens the same Arena contracts flow as clicking the Arena building. The bottom HUD scene has been cleaned so the editor layout matches runtime behavior, without hidden legacy speed/timeline controls.
- Champion cadence is back as a lightweight seven-day cycle. The HUD shows `Champion in X days` or `Champion Day!`; contract filtering for Champion Day is still a next implementation step.
- Time-passes phase work runs once on Day -> Night and once on Night -> Day through `CompanyRunData.ExecutePhaseBuildingWork`: courtyard/arena gladiators recover 2 exhaustion and 10% max health, Thermae-assigned gladiators pay gold for selected health or exhaustion treatment, and Training Hall work spends gold, stamina, and exhaustion to add attribute XP for the selected focus. Night -> Day also pays gladiator salary through `CompanyRunData.PayNightSalary`, currently the floor of each gladiator's initial cost divided by 10.
- Town has a frontend `EnvironmentVisualOverlay` for phase/weather visuals. Time-of-day and weather are separate frontend enums; Night follows `TownPhaseState`, while weather currently defaults to Clear.
- Town and arena share `EnvironmentVisualOverlay` for phase/weather visuals. Time-of-day follows `TownPhaseState`; weather lives on shared `SaveNode.WeatherState`, saves under `user://save/weather.tres`, randomizes through `PhaseTransitionController` when phase time advances, and drives both weather HUD icons and `WeatherShaderLayer` shader effects. Weather randomization is phase-aware: Rain is 15% for both Day and Night; Night excludes Sun and uses 85% Cloudy; Day currently uses 50% Cloudy, 35% Sun, and 15% Rain. Weather shaders are split under `assets/shaders/Weather*.gdshader`, with `WeatherShaderLayer` switching materials for the active weather. Cloudy uses slow broad cloud-shadow movement; Sun uses moving flare/shimmer; Rain currently uses separate cloud-background, falling-rain, and splash shader layers.
- Future phase transition animation is a good fit for `PhaseTransitionController` or a helper called by it, because Day -> Night and Night -> Day are centralized gameplay transitions. Leave this for later UX polish rather than mixing it into the current weather shader work.
- The RosterYard gold button previews current phase total near the button, building phase costs in both phases, and salary on visible roster-yard gladiator avatars. Salary displays 0 during Day and the upcoming Night -> Day payment during Night. Building hover badges show the building's own phase cost plus salary for gladiators assigned inside that building. Pressing it opens `GoldCostOverlay`, which discovers `IPhaseGoldCostSource` nodes and lays out visible current-phase costs in side-by-side boxes for gladiators, buildings, and payment result. Building cost previews reuse the centered gold badge position and temporarily hide occupancy badges. The Night `Next Day` button is disabled if current gold cannot cover `CompanyRunData.GetCurrentPhaseGoldCost`.
- Idle assigned gladiators are now a town risk indicator. The clock icon means assigned but no work will run this phase; exhausted uses a separate exhausted icon.
- The Town HUD `Select Contract` action and arena contract launch both validate `CompanyRunData.CanPayArenaReturnUpkeep`, because returning from arena completes Day -> Night and immediately charges current phase upkeep. If the company cannot afford that return upkeep, the Town HUD action is disabled and the arena start button shows `Upkeep Short`.
- Current roster capacity lives on `CompanyRunData.GladiatorCapacity` and defaults to 6. Gladiator add/buy paths should use `CanAddGladiator`, `AddGladiator`, and `TryBuyGladiator` so the cap is enforced before spending gold.
- New companies start with no gladiators. Market recruitment is the first step, and recruit health varies from 20-100% max health with buy/sell value based on current readiness.
- Equipment ownership exists, but equipping/unequipping items onto gladiators is not implemented yet.
- Contract resources are now partially implemented through `ArenaContractData` and `resources/contracts/starter_slime_pit.tres`. Arena combat startup/result handling is still placeholder-level.
- Company customization was expanded on `feature/company-customization`: the editor now supports shield shape, muted shield color, logo icon, logo size, random company names, and full randomization. New company creation opens with a randomized identity. Name generation lives in `CompanyNameGenerator`, while logo state/rendering stays in `CompanyLogoData`/`CompanyLogo`.
- A project CLI save-data delete path exists: `godot --headless -- --delete-savedata`, with aliases `--delete`, `--del-storage`, and `--delete-user-data`. It calls `SaveNode.DeleteSave()` and suppresses exit autosave.
- Completed company history now has a saved resource foundation: `CompletedCompanyHistory` stores capped, fame-sorted `CompletedCompanyRecord` entries with identity, career totals, and final fame only. `SaveNode.TryAddCurrentCompanyToCompletedHistory()` snapshots the active company if it qualifies. The main menu top-right `Records` button opens `CompletedCompaniesOverlay`, which shows `[list][details]` for saved completed companies and can delete entries. Details stay hidden until a company is pressed.
- First town entry now shows a one-time placeholder tutorial popup: `Todo, add tutorial with tscn animation popups here`. The per-run flag is `CompanyRunData.HasShownFirstTownEntryPopup`.
- Arena now has a `Donate` overlay for buying +1 or +5 fame with gold. Costs scale with current fame and are centralized in `CompanyRunData`.
- Enemy mob metadata foundation exists: `MobData`/`EnemyMobData` resources under `scripts/resources/mobs/`, `resources/mobs/green_slime.tres`, and `assets/ui/mobs/green_slime.svg`. The Green Slime packed scene is intentionally null until a runtime combat actor scene is implemented.
- Arena contract cards now render `ArenaContractData` resources with grouped enemy icons and gold/fame reward icons. The starter contract uses three Green Slime entries grouped as `x3` in the card. Green Slime has base fame value 10. Net fame is shown as one final medal value calculated from summed mob fame value minus a current-fame scaling cost, so trivial contracts become less rewarding or negative for high-fame companies.
- The main menu `Codex` button opens `CodexOverlay`, which scans `resources/mobs` and `resources/items` for authored `.tres` entries and shows Enemies/Items subcategories with details.
- Upgradeable building foundation exists through `IUpgradeable`. Thermae and Training Hall show `Upgrade` in their `BuildingOverlayPanel` header; levels/costs are stored on `CompanyRunData`.

## Next Focus

Work the short-term backlog in this order unless the user redirects.

1. Filter Arena contracts by champion cadence: on Champion Day, only champion contracts should be selectable.
2. Move controller configuration into contract start: configure local controllers dynamically each time a contract is launched, not from main menu/global setup.
3. `#18 Equip inventory items onto gladiators with validation`
4. `#3 Add item combat stats and equipment requirements`
5. `#49 Improve gladiator market recruit cards and variety`
6. `#10 Improve Thermae overlay around phase-based paid treatment`
7. `#11 Expand Training Hall progression actions on phase transitions`
8. `#16 Implement MVP XP and level progression`

## Immediate Direction

- Continue polishing controller configuration at contract launch. The current flow opens arena control setup from Start, assigns controls left-to-right with Enter/touch/gamepad A, then prompts to launch or reset.
- Do not make the main menu the place where contract control setup is finalized. Main menu controls can remain a general display/settings entry point, but contract participant/controller mapping should stay resolved immediately before starting a contract.
- Keep launch validation in run/resource APIs where practical so stale controller setup or assignment state cannot start a contract accidentally.
- After contract-launch controller config, continue with equipment assignment: let players equip/unequip items from `CompanyRunData.Inventory` onto a gladiator's `GladiatorEquipmentData`.
- Preserve slot rules: armor/main-hand/off-hand type validation, two-handed main-hand clearing or blocking off-hand, and replaced equipment returning to inventory.
- Prefer using the existing town drag/drop system. Dragging equipment onto roaming roster-yard gladiators should be the primary physical interaction, with an overlay/button path only if needed for clarity.
- Keep item movement centralized through `CompanyRunData` helpers and emit `RunChanged` after mutations.
- Keep authored `.tres` item files immutable templates. Runtime owned/stock items should remain duplicated resource instances.
- Add clear invalid-action feedback for unaffordable, wrong slot, missing item, dead/cemetery gladiator, or requirement failures. Use concise labels/tooltips or `GlobalOverlay` popups.
- Avoid adding parallel UI-local state for inventory, equipment, market stock, assignments, or selected combat participants.

## Short-Term Direction

- Improve gladiator market cards and recruit variety after the equip path is usable. Hiring must stay centralized through `CompanyRunData.TryBuyGladiator`/`AddGladiator` so purchase value and `CompanyCareerData.TotalGladiatorsInCareer` stay correct.
- Tune Thermae treatment balance after playtesting. It currently supports paid health treatment and paid exhaustion recovery, but does not restore stamina.
- Expand Training Hall progression once `#16` defines final MVP rules. It currently supports overall or focused attribute XP; exhaustion remains the limiter for repeated use and overtraining.
- Keep gladiator death centralized through `CompanyRunData.KillGladiator`; dead gladiators move from active `Gladiators` to `Cemetery`, not an active-roster dead flag.

## Later, Not Now

- `#4`, `#8`, and `#20-#25` cover ContractData, CombatResultData, real arena contract launch, player gladiator spawning, slime enemies, combat resolution, and arena HUD/result flow.
- `#26-#30` cover champion contract/deadline behavior after the real contract/combat path exists.
- `#31-#35` cover controller/touch/responsive/accessibility polish after core flows are functional.
- Add phase transition animation around `PhaseTransitionController` once core phase flow and weather visuals are stable.
- `#36-#41` cover MVP checklist, balance, bug bash, docs, and release tagging after the playable loop exists.
