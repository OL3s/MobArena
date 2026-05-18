# Agent Guide

This directory is primarily for AI agents working on MobArena. Use it to preserve project context, avoid repeated discovery work, and keep future changes consistent.

## Current Project State

- This is a minimal Godot 4.6 C# project.
- Use C# for gameplay and UI scripts unless the user explicitly requests otherwise.
- `project.godot` is the source of truth for current engine-level settings.
- `MobArena.csproj` is the Godot C# project file.
- `GlobalOverlay` is autoloaded from `autoload/overlay/GlobalOverlay.tscn` for blurred popups and modal overlays.
- `SaveNode` is autoloaded from `autoload/SaveNode.tscn` for temporary runtime company data and shared town time between scenes. It is not persistent storage yet.
- `LocalInputConfig` is autoloaded from `autoload/LocalInputConfig.tscn` for runtime local input/controller setup. It reads user-facing input preferences from `SaveNode.SettingsConfig` and exposes `ControllerSetups` for UI to render.
- The game concept comes from `../GameIdeas/MobGladiator.md`.
- `docs/game-design.md` contains the local implementation-oriented summary of that concept.
- The initial scene flow is `scenes/main_menu.tscn` to `scenes/town.tscn`. Roster Hall opens as `scenes/roster_hall.tscn` and returns to town. Other town buildings currently open modal overlay packed scenes.
- Roster Hall should stay town-like: neutral root, empty `World`, shared `TownHud.tscn`, and room-specific action buttons on the left side. Current roster actions should open overlays rather than replacing the room.
- `scenes/town.tscn` and `scenes/arena.tscn` both use neutral roots with a separate `Node2D` world and `CanvasLayer` controller UI.
- Town uses an implied horizontal-road layout with 1:1 buildings as `Node2D` instances of `scenes/components/town/TownBuilding.tscn` arranged in a 3x2 grid. The road is layout space only and is not drawn.
- `TownBuilding` has exported `PackedScene` targets for `OverlayToOpen` and `SceneToOpen`. Use `OverlayToOpen` for town building UI that should stay over town; use `SceneToOpen` for full scene navigation such as Roster Hall.
- `TownBuilding` visuals are per-instance exported textures: `BuildingTexture` and `IconTexture`.
- Town UI should represent champion pressure as time remaining before a mandatory champion fight deadline, not as a simple number of arena fights completed.
- Town phase is intended to use real time with Paused, Slowed, Normal, and Fast speed states. The bottom UI should put left/right speed arrows and a speed toggle on the left, then show current day and a sun-to-moon day progress bar for readability. Champion deadline belongs with the bottom timeline.
- Time speed scale: Paused/x0 pauses time; Slowed/x1 advances 1 in-game minute per real second; Normal/x10 advances 10 in-game minutes per real second; Fast/x100 advances 100 in-game minutes per real second.
- The speed toggle button must expose speed as text plus icon for accessibility: Paused uses pause bars, Slowed uses a `|>` icon, Normal uses one play triangle, and Fast uses two overlapping triangles. Toggling from a running speed pauses to x0; toggling from x0 restores `LastRunningSpeed`.
- Game time tick orchestration belongs in `scripts/resources/GameTimeController.cs`, not in HUD or room scripts. `GameTimeController` is a static service and should not live on `SaveNode`, be exported, or be saved. `GameTimeController.TickOneSecond` is the authority for a one-second simulation tick: it advances `SaveNode.TownTimeState` and then applies time-based company progression.
- Town clock/calendar logic belongs in `scripts/resources/TownTimeState.cs`. The shared runtime time state is `SaveNode.TownTimeState`, and reusable top/bottom HUD behavior belongs in `scenes/components/ui/TownHud.cs`/`.tscn`. HUD code should display and control time, but should not coordinate multiple time progression systems directly.
- Time-based company rules belong in `scripts/resources/CompanyTimeProgression.cs`. Add future passed-time effects there or behind `GameTimeController`, such as provisions decay, exhaustion recovery, upkeep, training progress, healing over time, shop refreshes, and recruitment timers.
- `TownTimeState.ResetToPause()` should be called when entering town flow from main menu or returning from arena combat. Town-like rooms should preserve the shared `SaveNode.TownTimeState` while navigating between each other.
- Company logo selection belongs in `scripts/resources/CompanyLogoData.cs`. Use `CompanyLogo.tscn` to render shield and inner logo layers. In town, shield clicks should open `CompanyOverviewOverlay.tscn`; use its `Edit Company` action to open `CompanyLogoEditorOverlay.tscn` through `GlobalOverlay`. Do not add persistence yet.
- Company run state belongs in `scripts/resources/CompanyRunData.cs` and is separate from career totals. Track frequent mutable values there, including current gold, the current active `GladiatorData` list, current cemetery list, current `RationInventory`, and current run mob kills. New company runs should go through `SaveNode.StartNewCompanyRun()`, which adds two default gladiators and updates `CompanyCareerData` through `CompanyRunData.AddGladiator`. `AliveGladiators` is derived from the active gladiator array. Use its methods for changes such as `AddGladiator`, `AddGold`, `TrySpendGold`, `AddMobKilled`, and `KillGladiator`.
- Gladiator death should go through `CompanyRunData.KillGladiator` so death vitals, active roster removal, cemetery insertion, career death totals, `RunChanged`, and `GladiatorDied` stay consistent. Do not use a separate dead flag on active gladiators; a dead gladiator is one that has been moved from `Gladiators` to `Cemetery`. Town UI listens for `GladiatorDied` and opens `scenes/ui/GladiatorDeathOverlay.tscn` with the dead gladiator card. Company overview's `Cemetery` button opens `scenes/ui/CemeteryOverlay.tscn` to list `CompanyRunData.Cemetery`.
- Ration inventory belongs in `scripts/resources/RationInventory.cs` and is owned by `CompanyRunData`. It tracks poor, common, and fine ration counts, stores fractional consumption progress, and exposes `GetTotal()` for summary UI such as the top town HUD. Ration provision values are 5, 8, and 10 for poor, common, and fine rations. Town time consumes one ration per alive gladiator per day, using poor rations before common and fine rations.
- `GladiatorData` owns gladiator condition values and caps. `Provisions` and `Exhaustion` are exported 0-10 floats; default gladiators start both at 8. Provisions decay over town time, while exhaustion should drop from repeated use and recover over town time. Recoverable caps use the lowest condition value, `min(Exhaustion, Provisions)`: values at 5 or above apply no cap penalty, then the multiplier scales from 1 down to 0 between 5 and 0. Current health and stamina should be restored through gladiator methods so they respect those recoverable caps.
- Reuse `scenes/components/ui/GladiatorCard.tscn` for compact gladiator presentation. The first roster list overlay is `scenes/ui/GladiatorsOverlay.tscn` and should display `CompanyRunData.Gladiators` horizontally.
- Current gladiator equipment data is still placeholder-level. Future equipment work should replace armor, main item, and second item strings with authored Godot `.tres` resources, while keeping the signature skill enum on equipment unless a fuller skill resource system is introduced.
- Company career totals belong in `scripts/resources/CompanyCareerData.cs` and are separate from frequent current state. Track long-term additive values there, including total gladiators in career, gladiators dead, total gold earned, contracts completed, mobs killed, and champions defeated.
- `SaveNode` should remain the runtime holder and persistence boundary. Do not put gameplay mutation helpers there; `HasSave()`, `Save()`, `Load()`, `DeleteSave()`, and `ResetRuntimeState()` manage the simple Godot resource save under `user://save`.
- `SaveNode.Get()` is intentionally strict and throws if the autoload is missing. Save-data delete actions should return to `scenes/main_menu.tscn` after successful deletion so active town, roster, arena, or overlay scenes do not continue running against reset save resources.
- Autosave should remain lightweight and deliberate. Current save triggers are company create/edit, app exit through `SaveNode._ExitTree`, and town day rollover at 00:00 from `TownHud` after `GameTimeController.TickOneSecond` advances time.
- Settings resources that should eventually persist with save/profile data belong on `SaveNode`. Current settings are in `scripts/resources/SettingsConfig.cs`, exposed as `SaveNode.SettingsConfig`.
- The temporary project-wide debug flag belongs in `SettingsConfig.DebugEnabled`, not in a separate autoload. Settings UI should mutate it directly like the other settings fields; gameplay code can read `SaveNode.DebugEnabled` as a convenience.
- Local input controller rows are represented by `scripts/resources/LocalInputControllerConfig.cs`. Do not hardcode connected-device rows directly in UI scenes; refresh and read `LocalInputConfig.ControllerSetups` instead.
- Main menu should keep `Enter Town` disabled until `SaveNode.HasCompany` is true. The company editor should allow cancel only when company data already exists.
- Main menu top-right controls use `SettingsButton.tscn` for reusable settings feedback and `ControlsOverlay.tscn` for controls/input setup. The controls overlay should stay modal through `GlobalOverlay` and keep the blur backdrop.
- `.godot/` is ignored and should be treated as local editor state.

## Development Guidelines

- Prefer small, focused changes that establish useful structure without overbuilding.
- Do not invent gameplay systems, folder structures, or architecture unless the user asks for them.
- Keep documentation factual. If a feature does not exist yet, describe it as planned or absent rather than implying it is implemented.
- Favor the first prototype loop before adding broader management systems: one gladiator, one arena, slimes, movement, attack, contract reward, death, and replacement.
- When management systems are added, balance upkeep costs against arena income. Resting over time is cheaper with a small roster, healer speeds health recovery for gold, stamina cannot be healed by the healer, and Training Hall spends gold plus stamina to train gladiators.
- Reward and combat systems should call run-data methods for current changes and career-data methods for lifetime-only events. They should not subtract from career totals when current state changes. Use `CompanyRunData.AddGold` for earned gold, `CompanyRunData.TrySpendGold` for spending current gold, and `CompanyRunData.AddMobKilled` when a mob dies during the current run.
- Use static `GameTimeController.TickOneSecond` for simulation ticks. Use `TownTimeState` APIs for direct clock and UI behavior: `TickOneSecond`, `AdvanceMinutes`, `IncreaseSpeed`, `DecreaseSpeed`, `TogglePaused`, `GetSpeedLabel`, `GetDayLabel`, `GetDigitalTimeLabel`, `GetDayProgressValue`, `GetDayProgressMax`, `GetDayPhaseLabel`, `IsTownOpen`, `IsTownSleeping`, `AreStoresOpen`, `GetChampionProgressValue`, `GetChampionProgressMax`, `GetChampionDeadlineLabel`, and `IsChampionDue`.
- Treat phone, controller, and desktop compatibility as a baseline requirement for input and UI decisions.
- For primary input selection, preserve the `SettingsConfig.AutoDetectPrimaryInput` flow: console-like platforms auto-detect Gamepad; mobile auto-detects Gamepad when one is connected, otherwise Touch; desktop auto-detects Keyboard. If auto-detect is off, use `SettingsConfig.DefaultPrimaryInput`; `None` should create no startup controller setup.
- Controller configuration is intentionally split from local co-op join logic. UI can show up to four local slots and prompts, but actual join/leave backend should be added to `LocalInputConfig` rather than directly to `ControlsOverlay`.
- Keep town and arena scene logic split between `World` for game-space nodes and `ControllerUi` for HUD, controller navigation, touch controls, and overlays.
- Author room layouts as real `.tscn` node trees. C# should wire behavior, signals, and scene transitions rather than constructing whole room layouts at runtime.
- Prefer decoupling and deconstructing repeated pieces into reusable `.tscn` components instead of duplicating behavior in parent scene scripts.
- Town buildings should stay as reusable `Node2D` components with `Area2D` interaction, SVG visuals, an icon, and an in-scene label. Do not regress them to plain `Button` nodes.
- Do not use a `Node2D` root for scenes that also own controller UI. Use a neutral `Node` root, with `World` and `ControllerUi` as siblings.
- Author world layouts in the `1152x648` frame. Use centered `Camera2D` nodes for scenes with a `World` node so wider/taller aspect ratios expand from the center through engine scene behavior, not resize code.
- Use `GlobalOverlay.ShowBlurredPopup` for informational modal text and `GlobalOverlay.ShowGoCancelPopup` for confirmation flows.
- Use `GlobalOverlay.AddOverlay` for custom overlay scenes such as `ControlsOverlay.tscn`; custom overlays that should visually match popups can reuse `assets/shaders/PopupBlurBackdrop.gdshader` on a fullscreen `ColorRect`.
- Preserve Godot-generated file formats and avoid hand-editing generated files unless there is a specific reason.
- Keep repository files text-normalized with LF line endings.

## Godot Notes

- Project name: `MobArena`
- Config version: `5`
- Feature tags include `4.6` and `GL Compatibility`.
- Windows rendering device driver is set to `d3d12`.
- Rendering method is `gl_compatibility` for desktop and mobile.
- 3D physics engine is set to `Jolt Physics`.

## Before Changing Code

1. Inspect the current tree. This project may grow quickly from its initial state.
2. Check whether scenes, scripts, or assets already exist before creating new ones.
3. Prefer following existing naming and folder conventions once they are established.
4. If conventions are absent, use standard Godot project organization and document the choice.

## Suggested Future Docs

- `docs/architecture.md` once scenes, autoloads, scripts, and systems exist.
- `docs/testing.md` once test or validation workflows are introduced.
