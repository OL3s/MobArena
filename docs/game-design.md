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
- Recruitment with cheap weak fighters, average fighters, and expensive trained gladiators.
- Company money used for recruitment, gear, healing, training, and upkeep.
- Fame gained from winning contracts, unlocking harder and better-paying fights.
- Arena contracts with different enemy groups, rewards, and restrictions.
- Gear that changes stats, combat strength, and risk when a gladiator dies.
- City phase for management through simple menus.

## Economy And Time Pressure

The player must balance upkeep costs against income from arena fights. A larger roster gives more backup options, but too many gladiators increase ongoing costs and can drain gold between fights.

Gladiators should recover naturally over time. Resting is cheaper if the company does not have too many gladiators, because fewer fighters means lower upkeep while waiting. The healer can speed up health recovery for gold, but stamina cannot be restored by the healer or medic station.

Training Hall lets the player train a gladiator in town for gold and stamina. Training should be useful, but it competes directly with resting, healing, upkeep, and saving gold for gear or recruitment.

The town phase uses real time with x1, x10, and x60 speed states. At x1, time advances 1 in-game minute per real second; x10 advances 10 in-game minutes per real second; x60 advances 60 in-game minutes per real second, meaning one real second is one in-game hour. Time controls should sit on the left side of the bottom UI as left/right arrows that cycle through those speeds. For readability, the bottom timeline should focus on current day and digital time rather than showing week text. Champion deadline display belongs with the bottom timeline and should be time remaining before the required champion fight, not number of arena fights completed.

Time state should be represented by a Godot `Resource` so management systems can consume the same API without coupling directly to town UI. The resource owns current day, digital time, current speed, champion deadline, time advancement, and speed changes.

Town time should advance from a one-second `Timer` tick, not from continuous per-frame processing. Each timer tick advances 1 in-game minute at x1, 10 in-game minutes at x10, and 60 in-game minutes at x60.

If the player fails to fight the champion before the deadline, the run should be lost or otherwise severely failed. This creates pressure to balance economy, roster health, stamina, and arena income instead of waiting forever.

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
- Controller: left stick movement with face buttons or triggers for attack, dodge, ability, and item use.
- Desktop: keyboard and mouse support, with keyboard movement and mouse or key-driven attacks.

Design requirements:

- Core combat actions must map cleanly to all three input styles.
- Menus must be usable with touch, controller focus navigation, and mouse.
- Avoid tiny UI targets because phone support is a first-class requirement.
- Avoid mechanics that require precise mouse-only aiming unless an equivalent controller and touch solution exists.
- Keep the first prototype input set small: move, attack, confirm, cancel, and basic menu navigation.

## Enemy Direction

Start with simple enemies and add complexity gradually.

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

Do not build the full roster, shop, fame, upkeep, or gear systems before the basic arena fight and contract loop works.

## Design Pillars

- Simple top-down combat.
- Meaningful risk through permanent gladiator death.
- Clear management choices between fights.
- Fame unlocks harder contracts and bigger rewards.
- Keep scope small and expand only after the prototype loop works.
