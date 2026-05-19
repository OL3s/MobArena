# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Current Status

The first working rations and market pass is implemented.

Completed:

- The market has a functional rations overlay for buying poor, common, and fine rations.
- Buying rations spends current gold through `CompanyRunData.TrySpendGold` and adds inventory to `CompanyRunData.Rations`.
- Ration market stock is saved as run state under `CompanyRunData.Market` and refreshes through `GameTimeController.ExecuteNewDay`.
- Ration values remain aligned with `RationInventory`: poor/common/fine rations provide 5/8/10 provisions.
- New company runs start with 2 poor rations and 1 common ration.
- The town center has a rations button alongside gladiators and equipment.
- The town rations overlay shows current counts and current automatic feed-below settings with ration icons.
- Automatic feeding settings live in a separate clickable overlay, with one feed-below slider per ration quality and a priority selector.
- Automatic feeding runs during town time progression, prioritizes the lowest-provisions gladiator, and consumes eligible ration types according to the feeding policy.
- Starvation warnings remain one popup per day and only show when gladiators are starving and no rations are available.
- Ration UI uses the existing run state and `CompanyRunData.RunChanged` paths rather than duplicating inventory state.

## Next Focus

Expand the market beyond rations: buy new gladiators, create real item resources, and buy items.

Priorities:

- Keep the current town-center roster yard as the main roster-management direction. The old separate Roster Hall direction is no longer the focus.
- Add a functional gladiator market flow for hiring new gladiators with current gold.
- Keep hiring centralized through `CompanyRunData.AddGladiator` so `CompanyCareerData.TotalGladiatorsInCareer` stays correct.
- Add market stock/state for available recruitable gladiators under the existing market/run-state resources instead of adding UI-local recruit state.
- Create real Godot `.tres` item resources for equipment data before expanding item buying: armor, main item, second item, and signature skill should stop being placeholder strings.
- Add item market buying after item resources exist, spending current gold through `CompanyRunData.TrySpendGold` and storing owned items in current run state.
- Keep market access clear from town roster management: hiring new gladiators, buying rations/supplies, and buying future weapon/blacksmith equipment.
- Continue building management actions around `GladiatorData`, `CompanyRunData`, `RationInventory`, cemetery, market, and time-progression resources instead of adding parallel state.
- Keep gladiator death centralized through `CompanyRunData.KillGladiator`; dead gladiators should move from active `Gladiators` to `Cemetery`, not remain in the active roster with a dead flag.
- Continue refining provisions/exhaustion behavior only as needed by real UI or gameplay; recoverable caps use `min(Exhaustion, Provisions)`, with no penalty at 5 or above and a linear multiplier from 1 to 0 below 5.
- Expand reusable gladiator UI only as new real data needs it; current cards already show health, recoverable cap, provisions, exhaustion, max stamina, skill, and stats.
- Keep `GameTimeController` as a static tick coordinator. Persistent time state belongs in `TownTimeState`; persistent run state belongs in `CompanyRunData`.
- Continue the champion-contract due flow later by wiring actual champion contract selection/completion and the end-of-day progression block once contracts exist.
- Keep long-term career totals in `CompanyCareerData`; do not mix current spendable values with lifetime stats.
- Keep gameplay mutation helpers on the relevant resources, not on `SaveNode`; `SaveNode` should remain the save/load boundary.
- Disk persistence is present. Keep autosave deliberate: company create/edit, app exit, and town day rollover at 00:00.
- Keep the first combat prototype minimal and focused on using the existing two-starting-gladiator company, roster display, death/cemetery flow, and future contract rewards that update both current state and career counters correctly.
- When input work resumes, wire `LocalInputConfig.ControllerSetups` into gameplay player input routing; `ControlsOverlay` already handles rendering and join/leave editing for current setups.
