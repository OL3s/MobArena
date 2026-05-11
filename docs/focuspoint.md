# Focus Point

This file captures what to focus on next. Update it near the end of a session so the next agent or developer can resume with the current priority.

## Next Focus

Build the data structure layer for the company and gladiators before adding more UI polish.

Priorities:

- Define gladiator data as Godot `Resource` classes.
- Include core gladiator fields: name, health, stamina, status, basic stats, and alive/dead state.
- Define company/roster data that owns gladiators and company resources such as gold.
- Store runtime company/roster data through `SaveNode`, matching the current runtime-only save approach.
- Keep disk persistence out for now unless explicitly requested.
- Make the data usable by Town, Roster Hall, and future arena combat without duplicating state.
- Keep the first version minimal and focused on supporting one starting gladiator, roster display, death/replacement, and future contract rewards.
