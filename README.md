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
4. Start from the current main scene flow in `scenes/main_menu.tscn`, `scenes/town.tscn`, and `scenes/arena.tscn`.

## Validation

Run these from the project root after changing assets, scenes, or C# code:

- `godot --headless --import`: import or reimport Godot assets, including new SVGs and scene resources.
- `dotnet build`: compile the Godot C# project.
- `godot --headless --quit`: load the project headlessly as a quick Godot project sanity check.

## Repository Layout

- `project.godot`: Godot project configuration.
- `MobArena.csproj`: Godot C# project file.
- `autoload/overlay/`: Global overlay autoload for blurred popups and confirmation dialogs.
- `autoload/SaveNode.tscn`: Runtime-only save node for temporary company data persistence between scenes.
- `autoload/LocalInputConfig.tscn`: Runtime input/controller configuration autoload for local players and primary input selection.
- `scenes/`: Godot scenes for the current game flow and reusable scene components.
- `scripts/`: C# scripts attached to scenes.
- `scripts/resources/`: Godot C# resources for shared game state, such as town time and company career totals.
- `assets/ui/input_icons/`: Imported input prompt icons used by controls and local co-op UI.
- `assets/ui/company_shield_highres.svg`: Current project/app icon.
- `assets/ui/icons/fame.svg`: Town wealth fame icon.
- `docs/`: Project notes and working guidance, mainly written for AI agents collaborating on this codebase.

## Current Scene Flow

- `scenes/main_menu.tscn`: Main menu and entry point.
- `scenes/town.tscn`: Between-fights town/company management scene with a neutral root, separate `World` node, and separate `ControllerUi` layer.
- `scenes/roster_hall.tscn`: Roster Hall management placeholder; unlike other town buildings, it opens as a separate scene.
- `scenes/arena.tscn`: Arena combat placeholder with a neutral root, separate `World` node, and separate `ControllerUi` layer.

The current flow is `Main Menu -> Town`, with `Town -> Roster Hall -> Town` for roster management. Roster Hall is a town-like room with an empty `World`, the shared `TownHud`, and left-side room action buttons. Other town buildings currently open modal overlay packed scenes.

The company shield in `assets/ui/company_shield_highres.svg` is also the project/app icon. In-game company logos use `CompanyLogo.tscn`, which layers a selectable shield and inner logo.

Town currently uses an implied horizontal-road layout with buildings arranged in a 3x2 grid. The road is layout space only and is not drawn.
Each town building can assign `OverlayToOpen` for modal packed-scene overlays or `SceneToOpen` for scene navigation. Roster Hall uses `SceneToOpen`; other current buildings use `OverlayToOpen`.
Town building art and icons are assigned per instance through exported `BuildingTexture` and `IconTexture` fields.
Town drag/drop is a core town interaction system, not a one-off UI helper. `RosterYard` coordinates drag state for town management actions, and drop targets implement `ITownDragDropTarget`, expose accepted `TownDragPayloadKind` values, and register in the shared town drop-target group. Current draggable payloads are gladiators, equipment items, and rations. Town buildings and roaming roster-yard gladiators can receive drops; overlapping targets resolve by `TownDragDropPriority`. The Market building currently sells dropped gladiators, equipment items, and rations, with a gold-value preview above the building while dragging over it. Dropping rations on roaming gladiators feeds them through run-data APIs.
Town assignment state is centralized in `CompanyRunData.TownAssignments`. `CompanyRunData.Gladiators` remains the owned active company roster, while `TownAssignmentData` stores location lists for courtyard, arena, healer, and training hall. The roster yard displays only `CourtyardGladiators`; dragging a gladiator to a building moves them from courtyard to that building's assignment list. Assignment mutations should go through `CompanyRunData.TryAssignGladiatorToTownLocation`, `TryMoveGladiatorToCourtyard`, and `RemoveGladiatorFromTownAssignments` so the core active roster is validated before location lists change. `TownBuilding.TryTakeGladiator`/`CanTakeGladiator` enforce active-roster validation and capacity before moving through those APIs. `MaxAssignedGladiators` controls fixed-cap buildings such as healer and training hall. Arena uses `AssignmentCapacityMode = LocalInputSetups`, so its capacity follows the current local input/controller setup count. Market keeps its sale behavior for dropped gladiators instead of assigning them.
Occupied town buildings show a centered occupancy badge with a gladiator icon and assigned count/capacity.
The town overlay tracks company status, wealth, supplies, and condition warnings at the top. Wealth shows current gold and current fame using separate gold and fame icons. Supply shows a 2x2 ration grid for poor, common, fine, and total rations. The bottom timeline contains speed arrows cycling Paused/Slowed/Normal/Fast, a speed toggle button that pauses/restores the last running speed, the current day, a sun-to-moon day progress bar, and the champion deadline.
Game time orchestration lives in `scripts/resources/GameTimeController.cs` as a static service because it owns tick flow, not persistent state. `GameTimeController.TickOneSecond` is the authority for one-second simulation ticks: it advances `SaveNode.TownTimeState` and then applies time-based company effects. New-day rules live behind `GameTimeController.ExecuteNewDay`, which currently marks champion-contract due state and records starving warning counts. Town clock/calendar logic lives in `scripts/resources/TownTimeState.cs`; it owns current day, minutes into day, speed, labels, champion deadline state, and day-warning flags. Company time effects live in `scripts/resources/CompanyTimeProgression.cs`; it currently applies provisions decay and exhaustion recovery to `CompanyRunData.Gladiators`. `TownHud.tscn`/`TownHud.cs` displays and controls town time, but should call the controller instead of coordinating time systems itself.
Settings state lives in `scripts/resources/SettingsConfig.cs`; the runtime instance lives on `SaveNode.SettingsConfig`. It currently stores the primary input preference for controls: auto-detect on/off and a default primary input of None, Keyboard, Touch, or Gamepad. It also stores the temporary project-wide debug flag as `SettingsConfig.DebugEnabled`; `SaveNode.DebugEnabled` is a read convenience for gameplay checks.
Company logo data lives in `scripts/resources/CompanyLogoData.cs`. Clicking the shield in town opens `CompanyOverviewOverlay`, which shows the company name, logo, and lifetime career stats. Its `Edit Company` button opens the editor for choosing shield, logo, and company name.
Company run state lives in `scripts/resources/CompanyRunData.cs` and covers frequent mutable values such as current gold, the current gladiator list, current cemetery list, current ration inventory, and current run mob kills. New company runs are started through `SaveNode.StartNewCompanyRun()`, which creates a fresh career/run and adds two default gladiators through `CompanyRunData.AddGladiator`; that function updates `CompanyCareerData.TotalGladiatorsInCareer`. `AliveGladiators` is derived from the current active `Gladiators` array. Rations live in `scripts/resources/RationInventory.cs` with poor, common, and fine counts plus `GetTotal()` for summary UI. Ration quality values are 5, 8, and 10 provisions for poor, common, and fine rations. Ration consumption progress is stored on the inventory; town time consumes one ration per alive gladiator per day, using poor rations before common and fine rations. Gladiator data includes two separate 0-10 float management values: `Exhaustion` and `Provisions`; default gladiators start both at 8. `Provisions` represents the fighter's food and thirst condition; better provisions cost gold to maintain, 10 is the best state, and the value slowly decays over time through `CompanyTimeProgression`. If provisions reach 0 during time progression, `CompanyRunData.KillGladiator` applies death vitals, moves the gladiator from `Gladiators` to `Cemetery`, updates career death totals, emits `GladiatorDied`, and the town HUD opens `GladiatorDeathOverlay.tscn` with the dead gladiator card. Company overview has a `Cemetery` button that opens `CemeteryOverlay.tscn` and lists dead gladiators for the current company save. `Exhaustion` represents readiness after accumulated fatigue from repeated use and should push the player to rotate gladiators instead of relying on the same fighter every fight; it recovers over town time through `CompanyTimeProgression`. Current health heals only toward the base max health cap. Recoverable health and stamina use the lowest condition value, `min(Exhaustion, Provisions)`: values at 5 or above have no cap penalty, then the multiplier scales from 1 down to 0 between 5 and 0. Company career totals live in `scripts/resources/CompanyCareerData.cs` and are separate long-term counters. Future reward/death/combat systems should update current values through run data methods such as `AddGladiator`, `AddGold`, `TrySpendGold`, `AddMobKilled`, and `KillGladiator`; those methods update career totals only when something additive is earned, killed, added, or dies. `SaveNode` writes/reads a simple Godot resource save under `user://save` and exposes `HasSave`, `Save`, `Load`, `DeleteSave`, and `ResetRuntimeState` for save lifecycle flows. `SaveNode.Get()` throws if the autoload is missing. Settings save-data delete actions return to the main menu after successful deletion so active save-dependent scenes do not keep running against reset resources.
Market sale values live on the relevant run/resource APIs. Purchased equipment items halve their `Cost` when moved into company inventory. Future purchased gladiators should go through `CompanyRunData.TryBuyGladiator`, which halves `InitialCost` before adding them to the active roster. Gladiator market value is computed by `GladiatorData.GetMarketValue()` from initial cost, level-derived stats, vitals, provisions, and exhaustion; `GetMarketSaleValue()` returns half of that value. Rations are count-based, so they keep full store costs in `RationInventory`/`RationStoreData` and apply `/2` only when selling a ration at the Market. When a gladiator leaves the active roster through sale or death, `CompanyRunData.ReturnGladiatorEquipmentToInventory` returns equipped items to company inventory before removal.
Autosave currently runs when a company is created or edited, when the app exits through `SaveNode._ExitTree`, when the player presses Back from town to main menu, and when town time crosses into a new day at 00:00.
Current spendable fame lives on `CompanyRunData.Fame` beside current gold and should be mutated through `AddFame`, `LoseFame`, and `TrySpendFame`. `LoseFame` clamps at zero. Fame should be awarded when contracts are won, scaled by contract difficulty and special contract modifiers once contracts are implemented.
The next management focus is working rations and markets: buying poor/common/fine rations for gold, updating `CompanyRunData.Rations`, reflecting totals in town UI, and applying supplies through the existing provisions systems instead of adding parallel market state.
The first roster overlay is `GladiatorsOverlay.tscn`, opened from Roster Hall's `Gladiators` button. It uses reusable `GladiatorCard.tscn` cards to show each current gladiator horizontally with portrait and name.
Main menu requires creating a company before `Enter Town` is enabled. The default suggested company name is `The Bronze Lions`.
The main menu top-right controls include reusable `SettingsButton.tscn` and a `Controls` button. The Controls button opens `ControlsOverlay.tscn` through `GlobalOverlay`; this overlay uses the shared blur backdrop shader and renders connected input setups from `LocalInputConfig.ControllerSetups`.
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
- Balance real-time town management around upkeep costs, arena income, healing, stamina, provisions, exhaustion, training, and mandatory champion deadlines.
- Keep permanent death meaningful: losing a gladiator should also risk the investment made in that fighter.
- Keep the first combat prototype small: use the current two-starting-gladiator company, one arena, slime enemies, basic movement and attack, simple contract rewards, and death/cemetery flow.

## Notes For AI Agents

AI agents should read `docs/agent-guide.md` first before making changes. The `docs/` directory is intentionally agent-focused so implementation work stays aligned with the current project state, Godot conventions, and repository expectations.

Also read `docs/focuspoint.md` before starting a new work session. It records what to focus on next and should usually be updated near the end of a session.
