# Agent Guide

This directory is primarily for AI agents working on MobArena. Use it to preserve project context, avoid repeated discovery work, and keep future changes consistent.

## Current Project State

- This is a minimal Godot 4.6 C# project.
- Use C# for gameplay and UI scripts unless the user explicitly requests otherwise.
- `project.godot` is the source of truth for current engine-level settings.
- `MobArena.csproj` is the Godot C# project file.
- `GlobalOverlay` is autoloaded from `autoload/overlay/GlobalOverlay.tscn` for blurred popups and modal overlays.
- `SaveNode` is autoloaded from `autoload/SaveNode.tscn` for temporary runtime company data and shared town time between scenes. It is not persistent storage yet.
- The game concept comes from `../GameIdeas/MobGladiator.md`.
- `docs/game-design.md` contains the local implementation-oriented summary of that concept.
- The initial scene flow is `scenes/main_menu.tscn` to `scenes/town.tscn`. Roster Hall opens as `scenes/roster_hall.tscn` and returns to town. Other town buildings currently open modal overlay packed scenes.
- `scenes/town.tscn` and `scenes/arena.tscn` both use neutral roots with a separate `Node2D` world and `CanvasLayer` controller UI.
- Town uses an implied horizontal-road layout with 1:1 buildings as `Node2D` instances of `scenes/components/town/TownBuilding.tscn` arranged in a 3x2 grid. The road is layout space only and is not drawn.
- `TownBuilding` has exported `PackedScene` targets for `OverlayToOpen` and `SceneToOpen`. Use `OverlayToOpen` for town building UI that should stay over town; use `SceneToOpen` for full scene navigation such as Roster Hall.
- `TownBuilding` visuals are per-instance exported textures: `BuildingTexture` and `IconTexture`.
- Town UI should represent champion pressure as time remaining before a mandatory champion fight deadline, not as a simple number of arena fights completed.
- Town phase is intended to use real time with Paused, Slowed, Normal, and Fast speed states. The bottom UI should put left/right speed arrows and a speed toggle on the left, then show current day and a sun-to-moon day progress bar for readability. Champion deadline belongs with the bottom timeline.
- Time speed scale: Paused/x0 pauses time; Slowed/x1 advances 1 in-game minute per real second; Normal/x10 advances 10 in-game minutes per real second; Fast/x100 advances 100 in-game minutes per real second.
- The speed toggle button must expose speed as text plus icon for accessibility: Paused uses pause bars, Slowed uses a `|>` icon, Normal uses one play triangle, and Fast uses two overlapping triangles. Toggling from a running speed pauses to x0; toggling from x0 restores `LastRunningSpeed`.
- Town time logic belongs in `scripts/resources/TownTimeState.cs`, not directly in room scripts. The shared runtime time state is `SaveNode.TownTimeState`, and reusable top/bottom HUD behavior belongs in `scenes/components/ui/TownHud.cs`/`.tscn`.
- `TownTimeState.ResetToPause()` should be called when entering town flow from main menu or returning from arena combat. Town-like rooms should preserve the shared `SaveNode.TownTimeState` while navigating between each other.
- Company logo selection belongs in `scripts/resources/CompanyLogoData.cs`. Use `CompanyLogo.tscn` to render shield and inner logo layers, and `CompanyLogoEditorOverlay.tscn` through `GlobalOverlay` to edit it. Do not add persistence yet.
- Main menu should keep `Enter Town` disabled until `SaveNode.HasCompany` is true. The company editor should allow cancel only when company data already exists.
- `.godot/` is ignored and should be treated as local editor state.

## Development Guidelines

- Prefer small, focused changes that establish useful structure without overbuilding.
- Do not invent gameplay systems, folder structures, or architecture unless the user asks for them.
- Keep documentation factual. If a feature does not exist yet, describe it as planned or absent rather than implying it is implemented.
- Favor the first prototype loop before adding broader management systems: one gladiator, one arena, slimes, movement, attack, contract reward, death, and replacement.
- When management systems are added, balance upkeep costs against arena income. Resting over time is cheaper with a small roster, healer speeds health recovery for gold, stamina cannot be healed by the healer, and Training Hall spends gold plus stamina to train gladiators.
- Use `TownTimeState` APIs for time: `TickOneSecond`, `AdvanceMinutes`, `IncreaseSpeed`, `DecreaseSpeed`, `TogglePaused`, `GetSpeedLabel`, `GetDayLabel`, `GetDigitalTimeLabel`, `GetDayProgressValue`, `GetDayProgressMax`, `GetDayPhaseLabel`, `IsTownOpen`, `IsTownSleeping`, `AreStoresOpen`, `GetChampionProgressValue`, `GetChampionProgressMax`, `GetChampionDeadlineLabel`, and `IsChampionDue`.
- Treat phone, controller, and desktop compatibility as a baseline requirement for input and UI decisions.
- Keep town and arena scene logic split between `World` for game-space nodes and `ControllerUi` for HUD, controller navigation, touch controls, and overlays.
- Author room layouts as real `.tscn` node trees. C# should wire behavior, signals, and scene transitions rather than constructing whole room layouts at runtime.
- Prefer decoupling and deconstructing repeated pieces into reusable `.tscn` components instead of duplicating behavior in parent scene scripts.
- Town buildings should stay as reusable `Node2D` components with `Area2D` interaction, SVG visuals, an icon, and an in-scene label. Do not regress them to plain `Button` nodes.
- Do not use a `Node2D` root for scenes that also own controller UI. Use a neutral `Node` root, with `World` and `ControllerUi` as siblings.
- Author world layouts in the `1152x648` frame. Use centered `Camera2D` nodes for scenes with a `World` node so wider/taller aspect ratios expand from the center through engine scene behavior, not resize code.
- Use `GlobalOverlay.ShowBlurredPopup` for informational modal text and `GlobalOverlay.ShowGoCancelPopup` for confirmation flows.
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
