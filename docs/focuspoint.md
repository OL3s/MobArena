# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Next Focus

Build working rations and markets so the player can buy supplies for gold, keep the roster fed, and see those changes reflected in town UI.

Priorities:

- Make the market/rations flow functional before expanding other management systems.
- Wire buying poor, common, and fine rations to current gold through `CompanyRunData.TrySpendGold` and current inventory through `CompanyRunData.Rations`.
- Keep ration quality values aligned with `RationInventory`: poor/common/fine rations restore or provide 5/8/10 provisions.
- Show clear rations totals, costs, and purchase feedback in the existing market/rations overlays without duplicating inventory state.
- Decide whether first-pass rations are bought into inventory only or can also be immediately assigned to starving gladiators from the same flow; keep the implementation minimal either way.
- Ensure starvation warnings remain one popup per day with the affected count, and that buying/applying rations makes the town HUD condition counts update through `CompanyRunData.RunChanged`.
- Keep a clear way to access market functions from the town roster management flow: hiring new gladiators, buying rations/supplies, and future weapon/blacksmith equipment work.
- Build the next management actions around the current `GladiatorData`, `CompanyRunData`, ration, cemetery, and time-progression resources instead of adding parallel state.
- Keep gladiator death centralized through `CompanyRunData.KillGladiator`; dead gladiators should move from active `Gladiators` to `Cemetery`, not remain in the active roster with a dead flag.
- Keep adding gladiators through `CompanyRunData.AddGladiator` so `CompanyCareerData.TotalGladiatorsInCareer` stays correct.
- Continue refining provisions/exhaustion behavior: current caps use `min(Exhaustion, Provisions)`, with no penalty at 5 or above and a linear multiplier from 1 to 0 below 5.
- Expand ration systems now: buying rations, choosing ration quality, and applying poor/common/fine ration values to provisions.
- Convert gladiator equipment fields from placeholder strings into real Godot resource data: armor, main item, second item, and signature skill should be backed by authored `.tres` resources when equipment work starts.
- Expand reusable gladiator UI only as new real data needs it; current cards already show health, recoverable cap, provisions, exhaustion, max stamina, skill, and stats.
- Keep `GameTimeController` as a static tick coordinator. Persistent time state belongs in `TownTimeState`; persistent run state belongs in `CompanyRunData`.
- Use `GameTimeController.ExecuteNewDay` for future day-start rules. It currently enables `TownTimeState.ChampionContractDue` when the champion deadline day is reached and records the current day plus starving gladiator count in `TownTimeState` when any gladiator is starving. `TownHud` shows one starvation warning popup with the affected count and separate champion warning popups with `pauseGameUntilClosed: true`; `GlobalOverlay` queues popup helper calls and emits general popup pause/resume signals so time pauses while blocking warnings are open and resumes the previous running speed afterward.
- Continue the champion-contract due flow next by wiring actual champion contract selection/completion and the end-of-day progression block once contracts exist.
- After rations and markets are working, return to removing the separate Roster Hall scene path and folding roster management fully into town.
- Keep long-term career totals in `CompanyCareerData`; do not mix current spendable values with lifetime stats.
- Keep gameplay mutation helpers on the relevant resources, not on `SaveNode`; `SaveNode` should remain the save/load boundary.
- Store runtime company/roster data through `SaveNode`, matching the current runtime-only save approach.
- Disk persistence is now present. Keep autosave deliberate: company create/edit, app exit, and town day rollover at 00:00.
- Make the data usable by Town and future arena combat without duplicating state; avoid adding new Roster Hall-only state.
- Keep the first combat prototype minimal and focused on using the existing two-starting-gladiator company, roster display, death/cemetery flow, and future contract rewards that update both current state and career counters correctly.
- When input work resumes, wire `LocalInputConfig.ControllerSetups` into gameplay player input routing; `ControlsOverlay` already handles rendering and join/leave editing for current setups.
