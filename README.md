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
- `scenes/`: Godot scenes for the current game flow and reusable scene components.
- `scripts/`: C# scripts attached to scenes.
- `scripts/resources/`: Godot C# resources for shared game state, such as town time and company career totals.
- `assets/ui/company_shield_highres.svg`: Current project/app icon.
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
The town overlay tracks company status and gold at the top. The bottom timeline contains speed arrows cycling Paused/Slowed/Normal/Fast, a speed toggle button that pauses/restores the last running speed, the current day, a sun-to-moon day progress bar, and the champion deadline.
Town time logic lives in `scripts/resources/TownTimeState.cs`; the shared runtime instance lives on `SaveNode.TownTimeState`. `TownHud.tscn`/`TownHud.cs` binds the top and bottom town HUD to that shared resource so Town and Roster Hall use the same clock.
Company logo data lives in `scripts/resources/CompanyLogoData.cs`. Clicking the shield in town opens `CompanyOverviewOverlay`, which shows the company name, logo, and lifetime career stats. Its `Change Company` button opens the editor for choosing shield, logo, and company name; persistence will be added later.
Company run state lives in `scripts/resources/CompanyRunData.cs` and covers frequent mutable values such as current gold, the current gladiator list, and current run mob kills. `AliveGladiators` is derived from the current `GladiatorData` array. Company career totals live in `scripts/resources/CompanyCareerData.cs` and are separate long-term counters. Future reward/death/combat systems should update current values through run data methods such as `AddGold`, `TrySpendGold`, and `AddMobKilled`; those methods update career totals only when something additive is earned or killed. `SaveNode.Save()` and `SaveNode.Load()` are intentionally stubbed with `NotImplementedException` until disk persistence is added.
The first roster overlay is `GladiatorsOverlay.tscn`, opened from Roster Hall's `Gladiators` button. It uses reusable `GladiatorCard.tscn` cards to show each current gladiator horizontally with portrait and name.
Main menu requires creating a company before `Enter Town` is enabled. The default suggested company name is `The Bronze Lions`.

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
- Balance real-time town management around upkeep costs, arena income, healing, stamina, training, and mandatory champion deadlines.
- Keep permanent death meaningful: losing a gladiator should also risk the investment made in that fighter.
- Keep the first prototype small: one gladiator, one arena, slime enemies, basic movement and attack, simple contract rewards, and death/replacement.

## Notes For AI Agents

AI agents should read `docs/agent-guide.md` first before making changes. The `docs/` directory is intentionally agent-focused so implementation work stays aligned with the current project state, Godot conventions, and repository expectations.

Also read `docs/focuspoint.md` before starting a new work session. It records what to focus on next and should usually be updated near the end of a session.
