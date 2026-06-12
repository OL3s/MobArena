# MobArena

MobArena is a Godot 4.6 C# prototype for a 2D top-down gladiator arena game. The player manages a gladiator company in town, accepts monster-fighting contracts, and controls assigned gladiators directly in arena combat.

## Quick Start

1. Install Godot 4.6 or a compatible Godot 4 version with .NET support.
2. Open this folder as a Godot project.
3. Start from `scenes/main_menu.tscn`.

## Validation

Run these from the project root after changing C# code, scenes, resources, or imported assets:

```bash
godot --headless --import
dotnet build
godot --headless --quit
```

See [testing.md](docs/testing.md) for manual test scenes, sandbox workflows, and CLI validation helpers.

## AI Agents

AI agents must start with [project-overview.md](docs/project-overview.md) before changing code, scenes, resources, or docs. The overview points to [ai-agent.md](docs/ai-agent.md), [focuspoint.md](docs/focuspoint.md), and the relevant topic docs.

## Documentation

Start with [project-overview.md](docs/project-overview.md). It is the documentation hub and links the full documentation set.

## Important Paths

| Path | Purpose |
| --- | --- |
| `project.godot` | Godot project settings and autoload registration. |
| `MobArena.csproj` | Godot C# project file. |
| `autoload/` | Global overlay, save node, runtime tag overlay, and local input config autoloads. |
| `scenes/` | Main scenes and reusable scene components. |
| `scripts/` | C# gameplay, UI, and resource scripts. |
| `resources/` | Authored gameplay resources for items, coatings, mobs, families, contracts, appearances, combat profiles, and player defaults. |
| `assets/` | UI/world art, icons, shaders, fonts, and visual assets. |
| `tests/` | Manual combat test scenes and attack/effect sandbox resources. |
