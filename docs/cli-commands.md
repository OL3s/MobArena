# CLI Commands

Run these from the project root with `godot --headless -- <flag>`.

Recognized runtime flags can be stacked in one command. They execute left to right, save after each mutating step, stop on first failure, and quit automatically when the sequence finishes. `--quit` is not required.

Value commands use `--flag=value`. Space-separated values are not consumed, so each space-separated token remains a separate command. Missing or invalid numeric values default to `0`.

Example:

```bash
godot --headless -- --delete-savedata --generate-company --add-money=250 --buy=1 --add-gladiator --contract --next-day
```

## Save Data

- `--save`: save the current runtime save state and exit.
- `--delete-savedata`: delete all project save data under `user://save` and exit without writing a fresh save.
- Aliases: `--delete`, `--del-storage`, `--delete-user-data`.

## Company Setup

- `--generate-company-if-missing`: create and save a default company only when no active company exists, then exit.
- `--generate-company`: create and save a default company, replacing any active company data, then exit.

## Run Mutation

- `--add-gladiator`: add and save one `GladiatorData.CreateDefault()` gladiator to the active company, then exit. Fails if no active company exists.
- `--add-money=<amount>`: add gold to the active company and career totals, then save. Alias: `--add-gold`.
- `--add-fame=<amount>`: add fame to the active company, then save.
- `--lose-fame=<amount>`: remove fame from the active company, clamped to zero, then save.
- `--buy[=index]`: buy and save the generated blacksmith item stock entry at `index`. Defaults to `0`. Aliases: `--buy-equipment`, `--buy-gear`.
- `--contract[=index]`: complete the available arena contract at `index` for the active company. Defaults to `0`. Alias: `--complete-contract`.
- `--complete-day`: complete the current day phase without selecting a contract, then save. Alias: `--complete-arena-day`.
- `--next-day`: advance from night to the next day, including salary and market refresh work, then save.
- `--weather[=value]`: set weather, then save. Accepted values: `Cloudy`/`0`, `Sun`/`1`, `Rain`/`2`. Missing or invalid values default to `Cloudy`.

## Scene Loading

- `--goto-scene=<scene>`: load a scene by name, then continue the command sequence. Supported names: `main-menu`, `town`, `arena`.
- `--goto=<scene>`: alias for `--goto-scene`.
- `--goto-main-menu`: load `res://scenes/main_menu.tscn`.
- `--goto-town`: load `res://scenes/town.tscn`.
- `--goto-arena`: load `res://scenes/arena.tscn`.

`--complete-contract` uses the same contract visibility rules as the arena contract UI: starter contract before the company has completed contracts, generated contracts afterward or when tutorial skipping is enabled.

Order matters. For example, `--add-gladiator --generate-company` fails on a clean save because there is no active company when `--add-gladiator` runs.
