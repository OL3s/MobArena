# MobArena

MobArena is a Godot 4.6 C# prototype for a 2D top-down gladiator arena game. The player manages a gladiator company in town, accepts monster-fighting contracts, and controls gladiators directly in arena combat.

The repository contains the Godot project, C# scripts, authored scenes, reusable components, game resources, assets, save-data tooling, and implementation docs.

## Quick Start

1. Install Godot 4.6 or a compatible Godot 4 version with .NET support.
2. Open this folder as a Godot project.
3. Start from `scenes/main_menu.tscn`.

If you are an AI agent, you must read `docs/ai-agent.md` and `docs/focuspoint.md` before changing code, scenes, resources, or docs.

## Project Status

- Engine: Godot 4.6
- Renderer: GL Compatibility
- Physics: Jolt Physics
- .NET assembly: `MobArena`
- Main flow: `Main Menu -> Town -> Arena`
- Current focus: visible, reusable arena combat effects before deeper mob AI

## Validation

Run these from the project root after changing assets, scenes, resources, or C# code:

```bash
godot --headless --import
dotnet build
godot --headless --quit
```

## Combat Test Scene

Run only the preplaced combat test room from the project root with:

```bash
godot scenes/test_mob_fight.tscn
```

For a headless load check, use:

```bash
godot --headless scenes/test_mob_fight.tscn --quit
```

## Documentation

| File | Theme |
| --- | --- |
| `docs/ai-agent.md` | Working rules, architecture constraints, and implementation conventions for AI agents/developers. |
| `docs/focuspoint.md` | Current state, active priority, immediate direction, and next work. |
| `docs/project-overview.md` | Codebase structure, scene flow, resources, systems, and current implementation boundaries. |
| `docs/arena-combat-actions.md` | Resource-driven arena action/effect architecture and planned executor types. |
| `docs/game-design.md` | Game concept, loop, economy, combat direction, and prototype scope. |
| `docs/cli-commands.md` | Headless save-data and run-mutation commands. |

## Important Paths

| Path | Purpose |
| --- | --- |
| `project.godot` | Godot project settings and autoload registration. |
| `MobArena.csproj` | Godot C# project file. |
| `autoload/` | Global overlay, save node, and local input config autoloads. |
| `scenes/` | Main scenes and reusable scene components. |
| `scripts/` | C# gameplay, UI, and resource scripts. |
| `resources/` | Authored `.tres` gameplay resources for mobs, contracts, items, and appearances. |
| `assets/` | UI/world art, icons, shaders, and visual assets. |

## Notes

- Active planning should use GitHub issues/milestones together with `docs/focuspoint.md`.
- The original game idea lives outside this repository at `../GameIdeas/MobGladiator.md`.
- Keep docs factual and update the relevant doc when changing architecture or implemented behavior.
