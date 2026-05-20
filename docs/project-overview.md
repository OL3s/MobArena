# Project Overview

MobArena is intended to be developed as a Godot-based 2D top-down gladiator arena game. The player manages a gladiator company between fights and directly controls one selected gladiator during arena contracts.

The source concept is `../GameIdeas/MobGladiator.md`. See `docs/game-design.md` for the local implementation-oriented summary.

## Existing Files

- `project.godot` configures the Godot application, renderer, .NET assembly name, and physics engine.
- `MobArena.csproj` configures the Godot C# project.
- `autoload/overlay/GlobalOverlay.tscn` is an autoloaded overlay layer for modal UI.
- `autoload/SaveNode.tscn` is an autoloaded runtime-only save node for temporary company data between scenes.
- `autoload/LocalInputConfig.tscn` is an autoloaded runtime input configuration node for local controller setup.
- `scenes/components/panels/InfoPopupPanel.tscn` provides a blurred OK popup.
- `scenes/components/panels/GoCancelPopupPanel.tscn` provides a blurred Go/Cancel popup.
- `scenes/components/town/TownBuilding.tscn` is the reusable town building template with an exported `PackedScene` target to open.
- `scenes/main_menu.tscn` is the configured main scene.
- `scenes/ui/ControlsOverlay.tscn` is the current controls/input configuration overlay opened from the main menu.
- `scenes/town.tscn` is the between-fights management scene. It uses a neutral root with a `Node2D` world plus a `CanvasLayer` controller UI.
- `scenes/arena.tscn` is the arena contract placeholder. It uses a neutral root with a `Node2D` world plus a `CanvasLayer` controller UI.
- `scripts/MainMenu.cs`, `scripts/Town.cs`, and `scripts/Arena.cs` contain the C# scripts for the initial navigation flow.
- `assets/ui/company_shield_highres.svg` provides the current project/app icon.
- `.gitignore` excludes Godot editor state and Android export output.
- `.editorconfig` declares UTF-8 files.
- `.gitattributes` normalizes text files to LF line endings.

## Current Scene Flow

1. `scenes/main_menu.tscn` starts the game.
2. `scenes/town.tscn` represents the between-fights city/company phase.
3. Town-center `RosterYard` represents the active roster-management surface.
4. Current town buildings open modal overlay packed scenes over town through `GlobalOverlay`.
5. `scenes/arena.tscn` is still present as an arena combat placeholder.

## Scene Structure

- Town and arena should keep world objects under a `Node2D` named `World`.
- Town and arena should keep HUD, controller navigation, touch controls, and menu overlays under a `CanvasLayer` named `ControllerUi`.
- Phase transitions live in `scripts/resources/PhaseTransitionController.cs` as a static service, not saved state.
- Town Day/Night phase state lives in `scripts/resources/TownPhaseState.cs` as a Godot `Resource`; the shared runtime instance is stored on `SaveNode.TownPhaseState`.
- `TownPhaseState` also exposes the seven-day champion cadence through Champion Day/countdown helpers used by the town HUD and future contract filtering.
- Healer and Training Hall work executes once on Day -> Night and once on Night -> Day through `CompanyRunData.ExecutePhaseBuildingWork`.
- Settings state lives in `scripts/resources/SettingsConfig.cs` as a Godot `Resource`; the shared runtime instance is stored on `SaveNode.SettingsConfig` until real profile persistence is implemented.
- Local input controller setup rows live in `scripts/resources/LocalInputControllerConfig.cs` as Godot `Resource`s. `LocalInputConfig.ControllerSetups` is a runtime `Array<LocalInputControllerConfig>` rebuilt from connected devices and settings.
- `scenes/components/ui/TownHud.tscn` and `TownHud.cs` provide the reusable top and bottom town HUD used by town-like rooms.
- Company logo state lives in `scripts/resources/CompanyLogoData.cs` as a Godot `Resource`.
- `scenes/components/ui/CompanyLogo.tscn` renders the logo as two layers: shield and inner logo.
- `scenes/ui/CompanyLogoEditorOverlay.tscn` edits logo data through `GlobalOverlay`.
- Main menu disables `Enter Town` until company name/logo data is applied through the editor.
- Main menu top-right UI has reusable settings and controls buttons. `SettingsButton.tscn` currently opens a placeholder feedback popup. The Controls button opens `ControlsOverlay.tscn` through `GlobalOverlay`.
- Town buildings are represented by reusable `TownBuilding.tscn` `Node2D` instances positioned in the `World` layer.
- `TownBuilding.tscn` contains the building SVG sprite, icon SVG sprite, text label, and `Area2D` interaction hitbox in one scene file.
- Each `TownBuilding` instance can assign unique `BuildingTexture` and `IconTexture` exports.
- Town buildings should use a 1:1 square footprint. The current town layout is a 3x2 grid split by an implied horizontal road gap; the road is not drawn.
- Town currently includes Arena, Market, Healer, Training Hall, and the central RosterYard management area with gladiator and equipment buttons.
- `TownBuilding.OverlayToOpen` opens modal packed-scene building UI over town. `TownBuilding.SceneToOpen` exists for future full-scene navigation but is not used by the current town management flow.
- Shared modal popups should go through `GlobalOverlay` instead of being attached directly to town or arena scenes.
- Custom overlays such as `ControlsOverlay.tscn` should also be opened through `GlobalOverlay.AddOverlay`. If they need a blurred modal feel, reuse `assets/shaders/PopupBlurBackdrop.gdshader` on a fullscreen backdrop.
- Keep this split intact so phone, controller, and desktop interactions can share the same scene without mixing gameplay world nodes and interface overlays.
- Room layouts should be authored in `.tscn` files. Scene scripts should look up existing nodes and attach behavior instead of generating the layout in code.
- Use neutral scene roots when combining world and controller UI. Do not put controller UI under a `Node2D` root.
- World layouts are authored for `1152x648`. Town and arena use centered `Camera2D` nodes so aspect-ratio expansion grows outward from the center through engine scene behavior.
- Company overview is currently a `GlobalOverlay` modal opened from the town shield. It displays lifetime counters from `CompanyCareerData`, held by `SaveNode` until real persistence is implemented.
- The old separate Roster Hall scene path has been removed. Roster inspection opens from the town-center `RosterYard` using a horizontal gladiator-list overlay with reusable gladiator cards.
- Current gladiator condition state lives on `GladiatorData`. `Exhaustion` is the remaining 0-10 management condition and affects recoverable health and stamina caps.
- Current gladiator attribute progression lives on `GladiatorLevelData` as EXP-backed Strength/Agility/Vitality/Endurance values. `TotalExp` and `TotalLevel` provide summed APIs for future balancing and contract requirements.
- Legacy supply upkeep, continuous town time, and champion deadline timer pressure have been removed for the arena-first loop.
- Gladiator death is centralized in `CompanyRunData.KillGladiator`. Dead gladiators are removed from active `Gladiators`, moved to `Cemetery`, and `GladiatorDeathOverlay.tscn` displays the dead gladiator card. `CemeteryOverlay.tscn` lists all dead gladiators from `CompanyRunData.Cemetery` through the company overview Cemetery button.
- New company runs start with two default gladiators through `SaveNode.StartNewCompanyRun()`. Added gladiators should use `CompanyRunData.AddGladiator` so `CompanyCareerData.TotalGladiatorsInCareer` stays correct.

## Settings And Input

- `SettingsConfig.AutoDetectPrimaryInput` controls whether the game chooses the primary input mode from platform/device state.
- `SettingsConfig.DefaultPrimaryInput` stores the hard-set primary input when auto-detect is off. Valid modes are None, Keyboard, Touch, and Gamepad.
- `SettingsConfig.DebugEnabled` stores the temporary project-wide debug flag. Settings UI mutates it directly like the other settings fields; gameplay code can read `SaveNode.DebugEnabled` as a convenience.
- `SettingsConfig.LowHealthWarningRatio` stores the low-health warning threshold used by town Risk counts and town-world warning icons. Risk classification is centralized through `GladiatorData.GetRiskStatus(...)`, including the critical combined low-health-and-exhausted state so displays do not duplicate warning icons for one gladiator.
- Auto-detect currently maps console-like platforms to Gamepad, mobile to Gamepad when a gamepad is connected or Touch otherwise, and desktop to Keyboard.
- The controls overlay displays currently configured inputs from `LocalInputConfig.ControllerSetups`; it does not yet implement real join/leave backend behavior.
- Gamepad prompts use imported icons under `assets/ui/input_icons/`. Desktop keyboard primary input is labeled as Keyboard but currently uses the mouse icon for compact visual display.
- Local co-op is planned for up to four local players. Keep future join/leave mutation in `LocalInputConfig` so gameplay scenes can query the same source of truth.

## Not Yet Present

- Gameplay combat scripts
- Player, mob, contract, roster, gear, upkeep, healing, stamina, training, champion deadline failure handling, or full city UI systems
- Real local co-op join/leave input handling and per-player gameplay spawning
- Export presets

## Intended First Prototype

- One controllable gladiator.
- One arena.
- Slime enemies.
- Basic movement and attack.
- Phone, controller, and desktop input support.
- Simple contract selection.
- Money reward after winning.
- Gladiator death and replacement.

## Documentation Policy

Keep this documentation accurate as the project evolves. Prefer documenting real decisions and implemented structure over speculative plans.
