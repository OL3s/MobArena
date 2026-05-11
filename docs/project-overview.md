# Project Overview

MobArena is intended to be developed as a Godot-based 2D top-down gladiator arena game. The player manages a gladiator company between fights and directly controls one selected gladiator during arena contracts.

The source concept is `../GameIdeas/MobGladiator.md`. See `docs/game-design.md` for the local implementation-oriented summary.

## Existing Files

- `project.godot` configures the Godot application, renderer, .NET assembly name, and physics engine.
- `MobArena.csproj` configures the Godot C# project.
- `autoload/overlay/GlobalOverlay.tscn` is an autoloaded overlay layer for modal UI.
- `autoload/SaveNode.tscn` is an autoloaded runtime-only save node for temporary company data between scenes.
- `scenes/components/panels/InfoPopupPanel.tscn` provides a blurred OK popup.
- `scenes/components/panels/GoCancelPopupPanel.tscn` provides a blurred Go/Cancel popup.
- `scenes/components/town/TownBuilding.tscn` is the reusable town building template with an exported `PackedScene` target to open.
- `scenes/main_menu.tscn` is the configured main scene.
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
3. `scenes/roster_hall.tscn` represents roster management as a separate scene and can return to town.
4. Other current town buildings open modal overlay packed scenes over town.
5. `scenes/arena.tscn` is still present as an arena combat placeholder.

## Scene Structure

- Town and arena should keep world objects under a `Node2D` named `World`.
- Town and arena should keep HUD, controller navigation, touch controls, and menu overlays under a `CanvasLayer` named `ControllerUi`.
- Town time state and time API live in `scripts/resources/TownTimeState.cs` as a Godot `Resource`; the shared runtime instance is stored on `SaveNode.TownTimeState`.
- `scenes/components/ui/TownHud.tscn` and `TownHud.cs` provide the reusable top and bottom town HUD used by town-like rooms.
- Company logo state lives in `scripts/resources/CompanyLogoData.cs` as a Godot `Resource`.
- `scenes/components/ui/CompanyLogo.tscn` renders the logo as two layers: shield and inner logo.
- `scenes/ui/CompanyLogoEditorOverlay.tscn` edits logo data through `GlobalOverlay`.
- Main menu disables `Enter Town` until company name/logo data is applied through the editor.
- Town buildings are represented by reusable `TownBuilding.tscn` `Node2D` instances positioned in the `World` layer.
- `TownBuilding.tscn` contains the building SVG sprite, icon SVG sprite, text label, and `Area2D` interaction hitbox in one scene file.
- Each `TownBuilding` instance can assign unique `BuildingTexture` and `IconTexture` exports.
- Town buildings should use a 1:1 square footprint. The current town layout is a 3x2 grid split by an implied horizontal road gap; the road is not drawn.
- Town currently includes Arena, Gladiator Market, Blacksmith, Healer, Roster Hall, and Training Hall buildings.
- `TownBuilding.OverlayToOpen` opens modal packed-scene building UI over town. `TownBuilding.SceneToOpen` navigates to another scene and should be reserved for buildings like Roster Hall.
- Shared modal popups should go through `GlobalOverlay` instead of being attached directly to town or arena scenes.
- Keep this split intact so phone, controller, and desktop interactions can share the same scene without mixing gameplay world nodes and interface overlays.
- Room layouts should be authored in `.tscn` files. Scene scripts should look up existing nodes and attach behavior instead of generating the layout in code.
- Use neutral scene roots when combining world and controller UI. Do not put controller UI under a `Node2D` root.
- World layouts are authored for `1152x648`. Town and arena use centered `Camera2D` nodes so aspect-ratio expansion grows outward from the center through engine scene behavior.

## Not Yet Present

- Gameplay combat scripts
- Player, mob, contract, roster, gear, upkeep, healing, stamina, training, boss deadline, or full city UI systems
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
