# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Next Focus

Continue building the company/gladiator data structure layer, while keeping the new controls/input configuration path ready for future gameplay input work.

Priorities:

- Expand the initial `GladiatorData` resource beyond its current name-only placeholder.
- Include core gladiator fields: name, health, stamina, status, basic stats, and alive/dead state.
- Expand reusable gladiator UI from the current portrait/name card as more gladiator fields become real data.
- Expand `CompanyRunData` or split it into a fuller current company/roster resource when roster data is introduced.
- Keep long-term career totals in `CompanyCareerData`; do not mix current spendable values with lifetime stats.
- Keep gameplay mutation helpers on the relevant resources, not on `SaveNode`; `SaveNode` should remain the save/load boundary.
- Store runtime company/roster data through `SaveNode`, matching the current runtime-only save approach.
- Keep disk persistence out for now unless explicitly requested.
- Make the data usable by Town, Roster Hall, and future arena combat without duplicating state.
- Keep the first version minimal and focused on supporting one starting gladiator, roster display, death/replacement, and future contract rewards that update both current state and career counters correctly.
- When input work resumes, wire `LocalInputConfig.ControllerSetups` into gameplay player input routing; `ControlsOverlay` already handles rendering and join/leave editing for current setups.
