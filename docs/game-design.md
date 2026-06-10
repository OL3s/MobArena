# Game Design

MobArena is based on the `Mob Gladiator` concept from `../GameIdeas/MobGladiator.md`.

## High Concept

MobArena is a simple 2D top-down Godot game where the player owns and manages a company of gladiators. The company accepts arena contracts, fights monsters, earns money and fame, buys better gear, recruits new fighters, and tries to survive increasingly difficult battles.

The player directly controls one selected gladiator during each arena fight. If that gladiator dies, they are permanently dead and the player must continue with another fighter from the company.

## Core Fantasy

The player is both a gladiator manager and an arena fighter.

Outside combat, the player makes business decisions: who to recruit, who to train, what gear to buy, and which contracts to accept. Inside combat, the player must personally survive the arena fight.

## Core Loop

1. Browse the city between fights.
2. Check available arena contracts.
3. Manage gladiators, gear, money, fame, and upkeep.
4. Choose a contract.
5. Select which gladiator will fight.
6. Enter the arena and control that gladiator directly.
7. Defeat the mobs or die trying.
8. Earn money, fame, and experience.
9. Return to the city and prepare for the next contract.

## Main Systems

- Gladiator roster with stats, levels, gear, and permanent death.
- Character appearance resources with separate body-facing art for town/arena and face portraits for UI.
- Recruitment with cheap weak fighters, average fighters, and expensive trained gladiators.
- Company money used for recruitment, gear, healing, training, and upkeep.
- Fame gained from winning contracts, unlocking harder and better-paying fights.
- Arena contracts with different enemy groups, rewards, and restrictions.
- Gear that changes stats, combat strength, and risk when a gladiator dies.
- Equipped gear should eventually be visible on the gladiator's body in town and arena, not only represented by inventory/menu icons.
- City phase for management through simple menus.

## Economy And Time Pressure

The player must balance upkeep costs against income from arena fights. A larger roster gives more backup options, but too many gladiators increase ongoing costs and can drain gold between fights.

Gladiators recover through explicit Day/Night phase transitions instead of continuous time. The Thermae can speed up health or exhaustion recovery for gold, but stamina cannot be restored by treatment buildings.

Gladiators have one management condition value from 0-10: exhaustion. Exhaustion represents readiness after accumulated fatigue from repeated use and training; it should drop when a gladiator is used too often and recover through phase transitions, encouraging roster rotation. Values at 5 or above apply no cap penalty; below 5, the cap multiplier scales from 1 down to 0 as exhaustion approaches 0. Health still cannot exceed the gladiator's base max health.

Training Hall lets the player train a gladiator in town for gold, stamina, and condition. Training can focus one attribute or split the same training effort evenly across all attributes, and competes directly with resting, treatment, upkeep, and saving gold for gear or recruitment.

Long-term company progress should be mostly linear: building upgrades, gladiator attribute growth, gear improvements, and future skill unlocks add power in understandable steps. Contract difficulty should scale faster than that, trending exponentially through fame, family unlocks, and higher threat budgets. This means very successful long runs should eventually hit a practical cap where the player's skill gap is no longer enough to overcome the arena, even if they avoid early starvation, debt collapse, or roster death spirals.

Company data should separate current state from lifetime career totals. Current run state covers values that can go up and down or reset between runs, such as current gold, current gladiators, and current run mob kills. Alive gladiator count should be derived from the current gladiator list until dead/wounded states exist. Career data covers long-term additive records such as total gladiators in the career, deaths, total gold earned, contracts completed, mobs killed, and champions defeated. Spending gold should reduce only current gold; earning gold should increase current gold and also add to total gold earned.

The town phase uses two explicit states: Day before the arena fight and Night after the arena fight. Returning from arena moves Day -> Night. The bottom HUD replaces speed controls with a `Next Day` button that is disabled during Day and enabled at Night.

Phase state should be represented by Godot `Resource`s so management systems can consume the same API without coupling directly to town UI. `TownPhaseState` owns current day and Day/Night phase. `PhaseTransitionController` owns the transition flow and calls company phase work.

Continuous town time has been removed from the current implementation direction. Town advancement happens through explicit Day -> Night and Night -> Day phase transitions, with phase work resolved once per transition.

If the player fails to fight the champion before the deadline, the run should be lost or otherwise severely failed. This creates pressure to balance economy, roster health, stamina, and arena income instead of waiting forever.

Champion Day is the current hard failure pressure point: the player must take a champion contract when it arrives, and losing that champion fight force-retires the gladiator company. The run is recorded if it qualifies for completed-company history, then the active company/run data is wiped and the player returns to the main menu.

## Combat Direction

Combat should be simple 2D top-down action. The player controls one gladiator in an arena and fights mobs directly.

Basic actions:

- Move.
- Attack.
- Dodge or dash.
- Use an ability.
- Use a potion or limited healing item.

Combat should stay small enough for a hobby-scale Godot project while still making different weapons and enemies feel meaningful.

## Platform And Input Direction

MobArena should be designed for phone, controller, and desktop compatibility from the start.

Supported input styles:

- Phone: touch controls with a virtual movement stick and touch-friendly combat buttons.
- Controller: left stick movement, optional right stick or right mousepad-style aiming, and face buttons or triggers for attack, dodge, ability, and item use.
- Desktop: keyboard and mouse support, with keyboard movement, optional mouse aiming, and mouse or key-driven attacks.

Design requirements:

- Core combat actions must map cleanly to all three input styles.
- Independent aim must not be mandatory for core game logic. Movement-only control should remain a supported mode where movement direction also supplies facing/aim direction.
- Mouse aiming should be exposed as a settings toggle that defaults on. Keyboard-only control should not require independent aim input.
- Menus must be usable with touch, controller focus navigation, and mouse.
- Avoid tiny UI targets because phone support is a first-class requirement.
- Avoid mechanics that require precise mouse-only aiming unless an equivalent controller and touch solution exists.
- Keep the first prototype input set small: move, attack, confirm, cancel, and basic menu navigation.

## Enemy Direction

Start with simple enemies and add complexity gradually.

Enemy definitions should be authored as Godot `.tres` resources so contracts, codex UI, and future combat spawning reference the same source of truth. Each enemy resource should hold display data such as name/icon, basic stats such as max health, and a packed-scene reference for the runtime combat actor when one exists.

Enemy `FameValue` is the contract budget, threat, and reward contribution value. Families should feel like power bands rather than equivalent skins. Slimes are the low-entry starter family, beginning at 5 fame and then jumping to 20+ for stronger slimes. Goblins and Undead begin around 40 fame, but Undead should jump harder after the first unit so two common Undead can feel comparable to a large starter Slime swarm. Demons begin with Imp around 60 fame, then scale sharply into late-game values. Add future low-tier enemies only when their family fantasy supports that entry point; do not flatten all families into the same low-value range.

Possible progression:

- Slimes as slow starter enemies.
- Rats or insects as fast weak enemies.
- Goblins as basic melee enemies.
- Skeletons as tougher predictable enemies.
- Archers as ranged enemies that force movement.
- Brutes as slow heavy enemies with high damage.
- Mages with area attacks or debuffs.
- Champion monsters for major contracts.

## First Prototype Scope

Keep the first playable version narrow:

- One controllable gladiator.
- One arena.
- Slime enemies.
- Basic movement and attack.
- Phone, controller, and desktop input support for the prototype actions.
- Simple contract selection.
- Money reward after winning.
- Gladiator death and replacement.

Arena contracts should be authored as resources containing their enemy mob entries and rewards so the selection UI and future combat startup share the same contract definition. Fame gain should scale against current company fame: the mob list provides the base fame value, then an expected-fame cost reduces the net reward so easy contracts are useful early but stop being efficient for famous companies.

Do not build the full roster, shop, fame, upkeep, or gear systems before the basic arena fight and contract loop works.

## Design Pillars

- Simple top-down combat.
- Meaningful risk through permanent gladiator death.
- Clear management choices between fights.
- Fame unlocks harder contracts and bigger rewards.
- Keep scope small and expand only after the prototype loop works.
