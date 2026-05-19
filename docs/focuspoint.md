# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Next Focus

Finish the champion-contract due flow around the new-day hook, then continue folding roster management into the town as an open management area where gladiators are visible as idling characters.

Priorities:

- Replace the current `Town -> Roster Hall -> Town` scene navigation with in-town roster management. Roster Hall should become part of the town management space rather than a separate scene.
- Aim for an open area where the player can see current gladiators idling, then drag and drop them into management sections.
- Put the Training Hall section on the left side of this roster management space.
- Put the healing/rest section on the right side of this roster management space.
- Keep a clear way to access market functions from this flow: hiring new gladiators, buying rations/supplies, and future weapon/blacksmith equipment work.
- Build the next management actions around the current `GladiatorData`, `CompanyRunData`, ration, cemetery, and time-progression resources instead of adding parallel state.
- Keep gladiator death centralized through `CompanyRunData.KillGladiator`; dead gladiators should move from active `Gladiators` to `Cemetery`, not remain in the active roster with a dead flag.
- Keep adding gladiators through `CompanyRunData.AddGladiator` so `CompanyCareerData.TotalGladiatorsInCareer` stays correct.
- Continue refining provisions/exhaustion behavior: current caps use `min(Exhaustion, Provisions)`, with no penalty at 5 or above and a linear multiplier from 1 to 0 below 5.
- Expand ration systems next: buying rations, choosing ration quality, and applying poor/common/fine ration values to provisions.
- Convert gladiator equipment fields from placeholder strings into real Godot resource data: armor, main item, second item, and signature skill should be backed by authored `.tres` resources when equipment work starts.
- Expand reusable gladiator UI only as new real data needs it; current cards already show health, recoverable cap, provisions, exhaustion, max stamina, skill, and stats.
- Keep `GameTimeController` as a static tick coordinator. Persistent time state belongs in `TownTimeState`; persistent run state belongs in `CompanyRunData`.
- Use `GameTimeController.ExecuteNewDay` for future day-start rules. It currently enables `TownTimeState.ChampionContractDue` when the champion deadline day is reached, records the current day in `TownTimeState.StarvingWarningDay` when any gladiator is starving, and pauses time for either warning. `TownHud` shows the warning popup and combines champion/starving warnings if both trigger on the same day.
- Continue the champion-contract due flow next by wiring actual champion contract selection/completion and the end-of-day progression block once contracts exist.
- Keep long-term career totals in `CompanyCareerData`; do not mix current spendable values with lifetime stats.
- Keep gameplay mutation helpers on the relevant resources, not on `SaveNode`; `SaveNode` should remain the save/load boundary.
- Store runtime company/roster data through `SaveNode`, matching the current runtime-only save approach.
- Disk persistence is now present. Keep autosave deliberate: company create/edit, app exit, and town day rollover at 00:00.
- Make the data usable by Town and future arena combat without duplicating state; avoid adding new Roster Hall-only state.
- Keep the first combat prototype minimal and focused on using the existing two-starting-gladiator company, roster display, death/cemetery flow, and future contract rewards that update both current state and career counters correctly.
- When input work resumes, wire `LocalInputConfig.ControllerSetups` into gameplay player input routing; `ControlsOverlay` already handles rendering and join/leave editing for current setups.
