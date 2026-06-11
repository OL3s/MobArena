# Game Logger

`GameLogger` is the project runtime logging API. Use it for diagnostic output instead of direct `GD.Print(...)` calls.

The default output format is:

```text
[Script.cs][Category]: message
```

Warnings and errors include the level and are routed through Godot's warning/error output:

```text
[Script.cs][Category][Warning]: message
[Script.cs][Category][Error]: message
```

## Basic Usage

Use the explicit helper that matches the system you are logging from:

```csharp
GameLogger.Save("Loading data.");
GameLogger.CLI("command sequence completed with exit code 0.");
GameLogger.SceneTransition($"{currentScene} -> {targetScene} ({reason}).");
GameLogger.Combat($"hit {targetName}, damage={damage}.");
GameLogger.Contract($"resolved win for '{contractName}'.");
GameLogger.UI("Opened codex overlay.");
GameLogger.Data("Added gladiator to active roster.");
GameLogger.State($"Weather changed from {previousWeather} to {currentWeather}.");
GameLogger.Input($"Pressed {actionName}.");
```

For less common categories, use the generic API:

```csharp
GameLogger.Print(GameLogCategory.Info, "Something happened.");
```

## Warnings And Errors

Use warnings for recoverable problems that developers should notice:

```csharp
GameLogger.Warning(GameLogCategory.UI, "Overlay scene is missing; falling back to direct scene load.");
```

Use errors for broken state, missing required resources, or paths that should not continue silently:

```csharp
GameLogger.Error(GameLogCategory.Save, $"Save failed for manifest. Error: {error}.");
```

Do not call `GD.PushWarning(...)` or `GD.PushError(...)` directly for new diagnostic messages unless you have a specific Godot integration reason. Prefer `GameLogger.Warning(...)` and `GameLogger.Error(...)` so output keeps the same source/category format.

## Categories

Current categories live in `GameLogCategory` inside `scripts/GameLogger.cs`.

| Category | Use For |
| --- | --- |
| `Info` | Rare general diagnostics that do not fit a specific system. Prefer a specific category when possible. |
| `Save` | Save/load, save deletion, manifest/resource persistence, autosave status. |
| `CLI` | Headless command-line commands, arguments, command results, command failures. |
| `SceneTransition` | Scene changes and transition reasons. |
| `Combat` | Arena hits, combat spawns, action activation, damage, combat-state outcomes. |
| `Contract` | Arena contract setup, resolution, rewards, win/loss/forfeit flow. |
| `UI` | Overlay opening, UI interaction diagnostics, UI fallbacks, display refresh diagnostics. |
| `Data` | Resource/run/career data mutations that are not save persistence. |
| `State` | Shared runtime state changes such as phase, weather, or other state-machine transitions. |
| `Input` | Input actions, control assignment diagnostics, device/controller handling. |

## Message Style

- Keep messages short and factual.
- Do not include the script/class prefix in the message. `GameLogger` adds `[Script.cs]` automatically.
- Include the important values that explain the state change or failure.
- Prefer stable terms like resource path, scene path, action name, contract name, day, phase, amount, and error code.
- Avoid vague messages such as `failed` without the object and reason.

Good:

```csharp
GameLogger.Save($"Load failed for company run. Error: {error}.");
GameLogger.Contract($"setup complete; spawnedPlayers={spawnedPlayers}/{assignedPlayers}, spawnedEnemies={spawnedEnemies}/{expectedEnemies}.");
```

Avoid:

```csharp
GameLogger.Info("Arena: failed.");
GameLogger.UI("CodexOverlay: Opened.");
```

`GameLogger` strips a matching legacy source prefix if one is present, but new logs should not include it.

## Expanding The Logger

Add a new category only when existing categories make logs less readable or hide a distinct system. Before adding one, check whether `Data`, `State`, `UI`, `Combat`, or `Contract` already describes the call.

To add a category:

1. Add the enum value to `GameLogCategory` in `scripts/GameLogger.cs`.
2. Add a convenience helper on `GameLogger` if the category will be common.
3. Add a display-name override in `FormatCategory(...)` only if the enum name should not be printed directly.
4. Update this document's category table.
5. Migrate relevant call sites to the new helper in the same change.

Do not add compatibility wrappers for old logger names. This project should keep one current logging API: `GameLogger`.

## Future Improvements

Possible future additions should stay centralized in `GameLogger`:

- Runtime category filtering for noisy combat/UI logs.
- Build-configuration filtering, such as debug-only verbose logs.
- Optional file logging for longer playtest sessions.
- Structured key/value helpers if logs become hard to parse.
- An on-screen debug console that subscribes to the same logger path.

Do not scatter those features across gameplay scripts. Call sites should continue to use the simple category helpers.
