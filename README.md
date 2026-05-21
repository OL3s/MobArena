# MobArena

MobArena is a Godot 4 project based on the `Mob Gladiator` game idea: a 2D top-down arena game where the player manages a gladiator company, accepts monster-fighting contracts, and directly controls one gladiator during combat.

The repository is in an early prototype state. It contains the Godot project configuration, C# scripts, authored scenes, reusable scene components, SVG UI/world assets, runtime-only company data, and documentation for continued development.

The source idea lives outside this repository in `../GameIdeas/MobGladiator.md`. The local docs summarize the parts needed for implementation work.

## Project Status

- Engine: Godot 4.6
- Renderer: GL Compatibility
- Physics: Jolt Physics
- .NET assembly name: `MobArena`

## Getting Started

1. Install Godot 4.6 or a compatible Godot 4 version.
2. Open this folder as a Godot project.
3. Read `docs/agent-guide.md` before making code, scene, or asset changes.
4. Check the GitHub issues and milestones for the active backlog and current implementation priorities.
5. Start from the current main scene flow in `scenes/main_menu.tscn`, `scenes/town.tscn`, and `scenes/arena.tscn`.

## Validation

Run these from the project root after changing assets, scenes, or C# code:

- `godot --headless --import`: import or reimport Godot assets, including new SVGs and scene resources.
- `dotnet build`: compile the Godot C# project.
- `godot --headless --quit`: load the project headlessly as a quick Godot project sanity check.

## Save Data CLI

- `godot --headless -- --delete-savedata`: delete all project save data under `user://save` and exit without writing a fresh save.
- Aliases: `--delete`, `--del-storage`, `--delete-user-data`.

## Repository Layout

- `project.godot`: Godot project configuration.
- `MobArena.csproj`: Godot C# project file.
- `autoload/overlay/`: Global overlay autoload for blurred popups and confirmation dialogs.
- `autoload/SaveNode.tscn`: Runtime-only save node for temporary company data persistence between scenes.
- `autoload/LocalInputConfig.tscn`: Runtime input/controller configuration autoload for local players and primary input selection.
- `scenes/`: Godot scenes for the current game flow and reusable scene components.
- `scripts/`: C# scripts attached to scenes.
- `scripts/resources/`: Godot C# resources for shared game state, such as town time and company career totals.
- `scripts/resources/mobs/`: Mob/enemy resource classes. `EnemyMobData` `.tres` files are contract/codex-facing mob definitions and can point to a future packed gameplay scene.
- `scripts/resources/contracts/`: Arena contract resource classes. `ArenaContractData` stores fame scaling cost, enemy mob resource entries, and gold rewards; net fame is calculated from mob fame values minus the current-company fame cost.
- `resources/mobs/`: Authored enemy mob `.tres` templates, starting with `green_slime.tres`.
- `resources/contracts/`: Authored arena contract `.tres` templates, starting with `starter_slime_pit.tres`.
- `assets/ui/input_icons/`: Imported input prompt icons used by controls and local co-op UI.
- `assets/ui/company_shield_highres.svg`: Current project/app icon.
- `assets/ui/icons/fame.svg`: Town wealth fame icon.
- `docs/`: Project notes and working guidance, mainly written for AI agents collaborating on this codebase.

## Current Scene Flow

- `scenes/main_menu.tscn`: Main menu and entry point.
- `scenes/town.tscn`: Between-fights town/company management scene with a neutral root, separate `World` node, and separate `ControllerUi` layer.
- `scenes/arena.tscn`: Arena combat placeholder with a neutral root, separate `World` node, and separate `ControllerUi` layer.

The current flow is `Main Menu -> Town`, with roster management folded into the town-center `RosterYard` and modal overlays opened through `GlobalOverlay`. Town buildings currently open modal overlay packed scenes; full scene navigation from town is not used for roster management.

The company shield in `assets/ui/company_shield_highres.svg` is also the project/app icon. In-game company logos use `CompanyLogo.tscn`, which layers a selectable shield and inner logo.

Town currently uses an implied horizontal-road layout with buildings arranged in a 3x2 grid. The road is layout space only and is not drawn.
Town and arena share a reusable `EnvironmentVisualOverlay` layered above the world and below HUD/UI. It separates `TimeOfDayVisual` (`Day`, `Night`) from shared `WeatherState` weather (`Clear`, `Sun`, `Rain`) so weather can combine with either day or night. Town drives the time tint from `TownPhaseState`; Night applies a dark blue overlay. Weather state lives on `SaveNode.WeatherState`, is randomized by `PhaseTransitionController` whenever phase time advances, and drives both tint colors and `WeatherShaderLayer` effects. Each weather has its own shader resource under `assets/shaders/Weather*.gdshader`; `WeatherShaderLayer` switches materials instead of branching inside one combined shader.
Each town building can assign `OverlayToOpen` for modal packed-scene overlays. `SceneToOpen` remains available for future full-scene navigation, but current town management uses overlays and the in-town roster yard.
Town building art and icons are assigned per instance through exported `BuildingTexture` and `IconTexture` fields.
Town buildings support `DisableWhenRosterEmpty`: when the active gladiator roster is empty, non-market buildings gray out, suppress hover popups, and ignore interaction so the Market remains the obvious recovery path for buying gladiators.
Upgradeable objects use `scripts/resources/IUpgradeable.cs`. For now, upgradeable town buildings are Thermae and Training Hall through `BuildingOverlayPanel`; their overlay headers show an `Upgrade` button. Upgrade levels and costs are persisted on `CompanyRunData` and mutated through `TryUpgradeBuilding`.
Town drag/drop is a core town interaction system, not a one-off UI helper. `RosterYard` coordinates drag state for town management actions, and drop targets implement `ITownDragDropTarget`, expose accepted `TownDragPayloadKind` values, and register in the shared town drop-target group. Current draggable payloads are gladiators and equipment items. Town buildings and roaming roster-yard gladiators can receive drops; overlapping targets resolve by `TownDragDropPriority`. The Market building currently sells dropped gladiators and equipment items, with a gold-value preview above the building while dragging over it.
Town assignment state is centralized in `CompanyRunData.TownAssignments`. `CompanyRunData.Gladiators` remains the owned active company roster, while `TownAssignmentData` stores location lists for courtyard, arena, Thermae, and training hall. The roster yard displays only `CourtyardGladiators`; dragging a gladiator to a building moves them from courtyard to that building's assignment list. Assignment mutations should go through `CompanyRunData.TryAssignGladiatorToTownLocation`, `TryMoveGladiatorToCourtyard`, and `RemoveGladiatorFromTownAssignments` so the core active roster is validated before location lists change. `TownBuilding.TryTakeGladiator`/`CanTakeGladiator` enforce active-roster validation and capacity before moving through those APIs. `MaxAssignedGladiators` controls fixed-cap buildings such as Arena, Thermae, and training hall. Market keeps its sale behavior for dropped gladiators instead of assigning them.
Occupied town buildings show a centered occupancy badge with a gladiator icon and assigned count/capacity.
Building overlay scenes can show assigned gladiators through `BuildingOverlayPanel.ShowAssignedGladiators`. Arena, Thermae, and Training Hall overlays display an assigned-gladiator row with a grab icon and draggable gladiator portrait buttons. Thermae and Training Hall overlays also expose focus selectors. Dragging from this row closes the overlay and starts a normal town drag; dropping on empty space returns the gladiator to the courtyard.
The town overlay tracks company status, wealth, roster risk warnings, and the current Day/Night phase. Risk warnings use `GladiatorData.GetRiskStatus(...)` so each risky gladiator is counted under exactly one icon: exhausted, low health, or critical for both. Idle assigned gladiators are counted separately through `CompanyRunData.GetIdleAssignedGladiatorCount`; the clock icon means assigned but no work will run this phase, while `exhaustion.svg` is the exhausted warning icon. Low-health risk uses the configurable low-health warning threshold. The old speed controls are replaced by a phase action button. During Day it is an enabled `Select Contract` button that opens the Arena contracts flow; during Night it becomes `Next Day` and advances Night -> Day through the phase-transition controller. The calendar panel also shows the seven-day champion cadence as either `Champion in X days` or `Champion Day!`.
Phase state lives in `scripts/resources/TownPhaseState.cs`; the runtime instance lives on `SaveNode.TownPhaseState`. Phase transitions are centralized in `scripts/resources/PhaseTransitionController.cs`. Returning from a completed arena contract calls `CompleteArenaContract`, which executes Day -> Night work, applies fight exhaustion to arena gladiators, moves them back to the courtyard, and clears arena control assignments. The generic debug Day -> Night path uses `CompleteArenaDay`; the HUD `Next Day` button calls `AdvanceToNextDay`, which moves Night -> Day. Both transitions execute time-passes work once through `CompanyRunData.ExecutePhaseBuildingWork`: courtyard/arena gladiators recover 2 exhaustion and 10% max health, Thermae-assigned gladiators pay gold for the selected health or exhaustion treatment, and Training Hall work spends gold, stamina, and exhaustion to add attribute XP for the selected focus. Night -> Day also pays gladiator salary through `CompanyRunData.PayNightSalary`, currently the floor of each gladiator's initial cost divided by 10.
The town-center roster yard has a compact gold button next to the gladiator and equipment buttons. Hovering it shows the current phase total near the button, building phase costs in both phases, and salary on each visible roster-yard gladiator avatar; salary displays as 0 during Day and as the upcoming Night -> Day payment during Night. Building hover badges show the building's own phase cost plus salary for gladiators assigned inside that building. Pressing the gold button opens `GoldCostOverlay`, which discovers `IPhaseGoldCostSource` nodes and lays out visible current-phase costs in side-by-side boxes for gladiators, buildings, and payment result. While a building cost preview is visible it takes the occupancy badge position; otherwise occupied assignable buildings show occupancy as normal. The Night `Next Day` button is disabled when `CompanyRunData.CanPayCurrentPhaseGoldCost` is false.
The Town HUD `Select Contract` action and arena contract launch both validate `CompanyRunData.CanPayArenaReturnUpkeep`, because returning from arena completes Day -> Night and immediately charges current phase upkeep. If the company cannot afford that return upkeep, the Town HUD action is disabled and the arena start button shows `Upkeep Short`.
Settings state lives in `scripts/resources/SettingsConfig.cs`; the runtime instance lives on `SaveNode.SettingsConfig`. It currently stores the primary input preference for controls: auto-detect on/off and a default primary input of None, Keyboard, Touch, or Gamepad. It also stores the temporary project-wide debug flag as `SettingsConfig.DebugEnabled`; `SaveNode.DebugEnabled` is a read convenience for gameplay checks and gates the compact Town HUD `Dev` menu. `SettingsConfig.LowHealthWarningRatio` controls when town Risk UI and town-world health icons classify gladiators as low health.
Company logo data lives in `scripts/resources/CompanyLogoData.cs`. Clicking the shield in town opens `CompanyOverviewOverlay`, which shows the company name, logo, and lifetime career stats. Its `Edit Company` button opens the editor for choosing shield shape, shield color, logo icon, logo size, and company name. Company name generation lives in `scripts/resources/CompanyNameGenerator.cs`; new company creation starts from a randomized company identity.
Company run state lives in `scripts/resources/CompanyRunData.cs` and covers frequent mutable values such as current gold, the current gladiator list, current cemetery list, equipment inventory, town assignments, arena control assignments, and current run mob kills. New company runs are started through `SaveNode.StartNewCompanyRun()`, which creates a fresh career/run with no starting gladiators so the player must recruit from the Market. `CompanyRunData.AddGladiator` updates `CompanyCareerData.TotalGladiatorsInCareer`. `AliveGladiators` is derived from the current active `Gladiators` array. Gladiator data now keeps `Exhaustion` as the only 0-10 management condition. Low exhaustion caps recoverable health and stamina, and Training Hall phase work spends exhaustion to limit overtraining. Attribute levels are derived from EXP on `GladiatorLevelData` (`StrengthExp`, `AgilityExp`, `VitalityExp`, `EnduranceExp`), with `TotalExp` and `TotalLevel` APIs summing the four attributes. Signature moves such as Dodge/Parry/Bash/Cleave remain plain move identity, not leveled skills. Company career totals live in `scripts/resources/CompanyCareerData.cs` and are separate long-term counters. Completed company records live in `scripts/resources/CompletedCompanyHistory.cs` as a capped, fame-sorted resource of `CompletedCompanyRecord` entries containing company identity, career totals, and final fame only. They are saved separately from the active run. Future reward/death/combat systems should update current values through run data methods such as `AddGladiator`, `AddGold`, `TrySpendGold`, `AddMobKilled`, and `KillGladiator`; those methods update career totals only when something additive is earned, killed, added, or dies. `SaveNode` writes/reads a simple Godot resource save under `user://save` and exposes `HasSave`, `Save`, `Load`, `DeleteSave`, and `ResetRuntimeState` for save lifecycle flows. `SaveNode.Get()` throws if the autoload is missing. Settings save-data delete actions return to the main menu after successful deletion so active save-dependent scenes do not keep running against reset resources.
`CompanyRunData.HasShownFirstTownEntryPopup` tracks the current run's first town entry. `Town.cs` shows a one-time placeholder tutorial popup through `GlobalOverlay.ShowBlurredPopup` and immediately saves after marking it shown.
Current roster capacity lives on `CompanyRunData.GladiatorCapacity` and defaults to 6. New gladiators must pass `CompanyRunData.CanAddGladiator`; `AddGladiator` and `TryBuyGladiator` enforce the cap before adding or spending gold. Phase gold cost timing types live in `scripts/resources/PhaseGoldCost.cs`; scene cost previews use `IPhaseGoldCostSource`, while backend affordability remains in `CompanyRunData`.
Market sale values live on the relevant run/resource APIs. Purchased equipment items keep their original `Cost`; gladiators keep `InitialCost` as base value. `GladiatorData.GetMarketValue()` computes current hire value from initial cost, level-derived stats, vitals, exhaustion, and current health/stamina readiness. `GetMarketSaleValue()` returns half of current market value, so wounded or exhausted gladiators are cheaper to buy and sell. When a gladiator leaves the active roster through sale or death, `CompanyRunData.ReturnGladiatorEquipmentToInventory` returns equipped items to company inventory before removal.
Autosave currently runs when a company is created or edited, when the app exits through `SaveNode._ExitTree`, when the player presses Back from town to main menu, when returning from arena, and when the town HUD advances Night -> Day.
Current spendable fame lives on `CompanyRunData.Fame` beside current gold and should be mutated through `AddFame`, `LoseFame`, and `TrySpendFame`. `LoseFame` clamps at zero. Fame should be awarded when contracts are won, scaled by contract difficulty and special contract modifiers once contracts are implemented.
The Arena contracts overlay has a `Donate` action that opens `ArenaDonationOverlay`, where players can spend gold for +1 or +5 fame. Donation pricing scales upward with current fame through `CompanyRunData.GetFameDonationGoldCost`; purchases go through `TryDonateForFame` so spending and fame gain stay centralized in run data.
Enemy mob metadata starts with `scripts/resources/mobs/MobData.cs` and `EnemyMobData.cs`. The first authored enemy is `resources/mobs/green_slime.tres`, with name, UI icon, max health, and a currently empty packed-scene reference for the future combat actor scene.
Arena contract metadata starts with `scripts/resources/contracts/ArenaContractData.cs`. The first authored contract is `resources/contracts/starter_slime_pit.tres`, which lists three Green Slime mob resources and rewards 20 gold. Green Slime currently has a base fame value of 10 so future easy enemies can tune around nearby values such as 12 or 13. Net fame is calculated from the mob list's total fame value minus a current-fame scaling cost, so easy contracts lose value as company fame rises. Contract cards group identical mob resources visually as one icon with an `xN` count and show one fame medal value for the final net fame result.
The next focus remains equipment assignment: equipping inventory items onto gladiators with validation, using the existing town drag/drop system where possible.
The first roster overlay is `GladiatorsOverlay.tscn`, opened from the town-center roster yard. It uses reusable `GladiatorCard.tscn` cards to show each current gladiator horizontally with portrait and name.
Main menu requires creating a company before `Enter Town` is enabled. The default suggested company name is `The Bronze Lions`.
The main menu top-right controls include `Codex`, `Records`, and the reusable `SettingsButton.tscn`. `CodexOverlay` scans authored `.tres` files under `resources/mobs` and `resources/items`, with Enemies and Items subcategories and a details panel. Arena participant controls are assigned per contract launch from the Arena overlay: pressing Start opens the arena control setup, then Enter/touch/gamepad A joins controllers left-to-right for assigned Arena gladiators before the launch confirmation.
Local input setup is split between `LocalInputConfig` and resources. `LocalInputConfig` is an autoload `Node` because Godot autoloads need scene/node ownership. It owns a runtime `Array<LocalInputControllerConfig>` where each resource describes one setup with name, kind, device id, icon, and joined state. Backend join/leave behavior is not implemented yet; the current overlay is configuration/display scaffolding for up to four local players.

## Structure Principles

- Prefer decoupled, deconstructed scene pieces over monolithic scenes or runtime-generated rooms.
- Author rooms and reusable pieces as `.tscn` files; use C# to wire behavior, signals, and transitions.
- Use neutral scene roots when a scene combines game world and UI. Keep `World` as `Node2D` and `ControllerUi` as `CanvasLayer` siblings instead of making controller UI live under a `Node2D` root.
- Reuse small components like `TownBuilding.tscn` for repeated concepts such as town buildings. Town buildings should be real `Node2D` world objects with `Area2D` interaction, not plain UI buttons.
- Author world layouts inside the `1152x648` frame. Town and arena use centered `Camera2D` nodes so aspect-ratio expansion grows outward from the center through engine scene behavior, not resize code.

## Game Direction

- Build a gladiator company through contracts, money, fame, recruitment, and gear.
- Between fights, manage the roster, choose contracts, buy equipment, and handle upkeep.
- During fights, control one selected gladiator in simple 2D top-down arena combat.
- Support phone, controller, and desktop play from the start.
- Keep management light and arena-first: Day is preparation before the arena, Night is after the fight, and phase transitions resolve Thermae/training effects.
- Keep permanent death meaningful: losing a gladiator should also risk the investment made in that fighter.
- Keep the first combat prototype small: use the current two-starting-gladiator company, one arena, slime enemies, basic movement and attack, simple contract rewards, and death/cemetery flow.

## Notes For AI Agents

AI agents should read `docs/agent-guide.md` first before making changes. The `docs/` directory is intentionally agent-focused so implementation work stays aligned with the current project state, Godot conventions, and repository expectations.

Also read `docs/focuspoint.md` before starting a new work session. It records what to focus on next and should usually be updated near the end of a session.

Active implementation work is tracked in GitHub issues and milestones for this repository. Use them together with `docs/focuspoint.md` to choose the next task and keep issue descriptions aligned with the current architecture.
