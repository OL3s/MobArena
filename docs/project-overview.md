# Project Overview

MobArena is intended to be developed as a Godot-based 2D top-down gladiator arena game. The player manages a gladiator company between fights and directly controls one selected gladiator during arena contracts.

The source concept is `../GameIdeas/MobGladiator.md`. See `docs/game-design.md` for the local implementation-oriented summary.

## Existing Files

- `project.godot` configures the Godot application, renderer, .NET assembly name, and physics engine.
- `MobArena.csproj` configures the Godot C# project.
- `autoload/overlay/GlobalOverlay.tscn` is an autoloaded overlay layer for modal UI.
- `autoload/SaveNode.tscn` is an autoloaded runtime-only save node for temporary company data between scenes.
- `autoload/LocalInputConfig.tscn` is an autoloaded runtime input configuration node for local controller setup.
- `scenes/components/panels/InfoPopupPanel.tscn` provides a blurred OK popup.
- `scenes/components/panels/GoCancelPopupPanel.tscn` provides a blurred Go/Cancel popup.
- `scenes/components/town/TownBuilding.tscn` is the reusable town building template with an exported `PackedScene` target to open.
- `scenes/main_menu.tscn` is the configured main scene.
- Contract participant controls are assigned from the Arena launch flow instead of being finalized globally from the main menu.
- `scenes/town.tscn` is the between-fights management scene. It uses a neutral root with a `Node2D` world plus a `CanvasLayer` controller UI.
- `scenes/arena.tscn` is the arena contract placeholder. It uses a neutral root with a `Node2D` world plus a `CanvasLayer` controller UI.
- Arena launches should go through real roster, contract, and control assignment data. The town HUD dev menu has `Quickstart arena` for fast iteration with existing roster gladiators and fixed Keyboard/Gamepad assignments; touch arena control is temporarily disabled.
- `scripts/MainMenu.cs`, `scripts/Town.cs`, and `scripts/Arena.cs` contain the C# scripts for the initial navigation flow.
- `assets/ui/company_shield_highres.svg` provides the current project/app icon.
- `.gitignore` excludes Godot editor state and Android export output.
- `.editorconfig` declares UTF-8 files.
- `.gitattributes` normalizes text files to LF line endings.

## Current Scene Flow

1. `scenes/main_menu.tscn` starts the game.
2. `scenes/town.tscn` represents the between-fights city/company phase.
3. Town-center `RosterYard` represents the active roster-management surface.
4. Current town buildings open modal overlay packed scenes over town through `GlobalOverlay`.
5. `scenes/arena.tscn` is still present as an arena combat placeholder.
6. Arena startup should be reached through the town contract flow and its control setup overlay.

## Scene Structure

- Town and arena should keep world objects under a `Node2D` named `World`.
- Town and arena should keep HUD, controller navigation, touch controls, and menu overlays under a `CanvasLayer` named `ControllerUi`.
- Phase transitions live in `scripts/resources/PhaseTransitionController.cs` as a static service, not saved state.
- Future Day/Night transition animation should fit around `PhaseTransitionController`, since that controller owns phase-time advancement and phase work. It is planned UX polish, not part of the current shader foundation.
- Town Day/Night phase state lives in `scripts/resources/TownPhaseState.cs` as a Godot `Resource`; the shared runtime instance is stored on `SaveNode.TownPhaseState`.
- `TownPhaseState` also exposes the seven-day champion cadence through Champion Day/countdown helpers used by the town HUD and future contract filtering.
- Frontend environment visuals live in `scenes/components/environment/EnvironmentVisualOverlay.tscn`, used by both town and arena. It keeps phase-driven time of day separate from shared `WeatherState` weather so Night can compose with Cloudy/Rain and Day can compose with Cloudy/Sun/Rain. `WeatherShaderLayer` provides reusable shader effects for weather by switching between shader materials from `assets/shaders/Weather*.gdshader`; Cloudy uses broad moving cloud shadows, Sun uses moving flare/shimmer, and Rain is split into cloud-background, falling-rain, and splash layers. `PhaseTransitionController` randomizes weather when phase time advances.
- Time-passes work executes once on Day -> Night and once on Night -> Day through `CompanyRunData.ExecutePhaseBuildingWork`: courtyard/arena gladiators recover 2 exhaustion and 10% max health, Thermae-assigned gladiators pay gold for selected health or exhaustion treatment, and Training Hall work spends gold, stamina, and exhaustion to add attribute XP for the selected focus. Night -> Day also pays gladiator salary through `CompanyRunData.PayNightSalary`, currently the floor of each gladiator's initial cost divided by 10.
- Completed arena contracts use `PhaseTransitionController.CompleteArenaContract` instead of the generic debug `CompleteArenaDay`; contract completion applies fight exhaustion to arena gladiators, returns them to the courtyard, and clears arena control assignments after Day -> Night building work.
- The selected arena contract is stored on `CompanyRunData.ActiveArenaContract` when the arena scene launches and cleared when arena results resolve. Arena result logic lives in `Arena.ResolveContractWin()`, `Arena.ResolveContractForfeit()`, and `Arena.ResolveContractLoss()` so future combat conditions can call the same APIs. Loss kills every arena gladiator, while win and forfeit only kill arena gladiators already at 0 health; arena death overlays are queued and shown once town HUD loads. Until real combat result handling exists, `scenes/arena.tscn` exposes debug-only win/loss buttons; the control setup overlay only assigns inputs and launches the arena.
- Settings state lives in `scripts/resources/SettingsConfig.cs` as a Godot `Resource`; the shared runtime instance is stored on `SaveNode.SettingsConfig` until real profile persistence is implemented.
- Weather state lives in `scripts/resources/WeatherState.cs` as a Godot `Resource`; the shared runtime instance is stored on `SaveNode.WeatherState` and saved under `user://save/weather.tres` so town and arena render the same current weather across scene changes and save/load.
- Local input controller setup rows live in `scripts/resources/LocalInputControllerConfig.cs` as Godot `Resource`s. `LocalInputConfig.ControllerSetups` is a runtime `Array<LocalInputControllerConfig>` rebuilt from connected devices and settings.
- `scenes/components/ui/TownHud.tscn` and `TownHud.cs` provide the reusable top and bottom town HUD used by town-like rooms.
- Company logo state lives in `scripts/resources/CompanyLogoData.cs` as a Godot `Resource`. It stores shield shape, shield color, logo icon, logo size, and company name. Random company names are generated by `scripts/resources/CompanyNameGenerator.cs`.
- `scenes/components/ui/CompanyLogo.tscn` renders the logo as two layers: shield and inner logo.
- `scenes/ui/CompanyLogoEditorOverlay.tscn` edits company identity through `GlobalOverlay`, including dice-button randomization for the name or full logo setup.
- Completed company history lives in `scripts/resources/CompletedCompanyHistory.cs` as a Godot `Resource` containing capped, fame-sorted `CompletedCompanyRecord` entries with company identity, career totals, and final fame only. It is persisted by `SaveNode` under `user://save/completed_company_history.tres` and can be viewed from `scenes/ui/CompletedCompaniesOverlay.tscn` through the main menu top-right `Records` button.
- First town entry per run is tracked by `CompanyRunData.HasShownFirstTownEntryPopup`; `Town.cs` currently shows a placeholder tutorial popup once through `GlobalOverlay` and saves the flag.
- Character visual identity uses `CharacterAppearanceData` resources with a UI `UiIcon`, a `BodyForward` texture, and a `BodyBack` texture. Gladiators use `GladiatorAppearanceData` under `resources/gladiator_appearances/`; mobs use `MobAppearanceData` under `resources/mob_appearances/`. Some appearances use `UsesSeparatedHands` plus a circular `HandTexture`; body-only mobs such as slimes keep hands disabled. Menus and compact UI use UI icons, while town/arena/world presentation uses body textures through the relevant `GetBodyForwardTexture()`/`GetBodyBackTexture()` APIs.
- Enemy mob metadata lives under `scripts/resources/mobs/`. `MobData` is the base resource for shared mob display data, and `EnemyMobData` adds family, max health, an authored `ArmorData` profile, and fame value. Fame value is the single mob strength, contract-budget cost, and reward contribution value. Authored enemy templates live under `resources/mobs/`; Slimes, Goblins, Undead, and Demons each have five normal mobs plus one champion template, and currently have no packed combat scene assigned. Current family bands are intentionally uneven: Slimes are the only very-low-entry family (`5 -> 20 -> 30 -> 45 -> 70`), Goblins start at 40 and scale to 150, Undead start at 40 but jump to 80+ immediately, and Demons start with Imp at 60 before scaling sharply upward.
- `scenes/ui/CodexOverlay.tscn` opens from the main menu top-right `Codex` button. It discovers authored `.tres` files under `resources/mobs` and `resources/items`, lets the player switch between Enemies and Items, and displays shared icon/name/description/stat details. Enemy entries are grouped into collapsed icon-labeled mob-family sections sorted by total family fame, with individual mobs sorted from lowest to highest fame and champions marked by the champion icon. Item entries use the same collapsed panel grouping by type, with two-handed weapons under Main Hand and marked with the two-handed icon, sorted by total type cost and then individual item cost.
- Item combat data is resource-container based. `ArmorItemData.ArmorProfile` and `EnemyMobData.ArmorProfile` point to `ArmorData`, which stores `BaseValue`, `ArmorTypeOverrideData` rows, and `ArmorSpecialModifierData` rows. `MainHandItemData` and `OffHandItemData` inherit `DamageItemData`, which stores a `CombatDamageData` container made of `CombatDamageEntryData` rows. `ArmorDamageType` currently has `Slash`, `Pierce`, `Crush`, `Heat`, `Cold`, and `Acid`; `ArmorSpecialType` is separate for conditional tags like `Silver` or `Holy`. Armor mitigation is non-linear for positive armor, neutral at zero armor, and treats negative armor as vulnerability. Damage/armor type icons live under `assets/ui/items/type_*.svg`. Items use `UiIcon` for UI and optional `HeldTexture`/`HeldDisplayHeight` for in-hand world visuals; armor also has `ArmorForwardTexture` and `ArmorBackTexture` overlays.
- Main menu disables `Enter Town` until company name/logo data is applied through the editor.
- Main menu top-right UI has `Codex`, `Records`, and the reusable `SettingsButton.tscn`, which opens the Settings overlay.
- Town buildings are represented by reusable `TownBuilding.tscn` `Node2D` instances positioned in the `World` layer.
- `TownBuilding.tscn` contains the building SVG sprite, icon SVG sprite, text label, and `Area2D` interaction hitbox in one scene file.
- Each `TownBuilding` instance can assign unique `BuildingTexture` and `IconTexture` exports.
- `TownBuilding.DisableWhenRosterEmpty` disables non-market buildings when the active roster is empty. Disabled buildings are grayed out and do not show hover popups or accept interaction; Market stays usable during Day so players can recruit replacements. `TownBuilding.DisableAtNight` disables selected buildings during Night; Arena and Market use it so after-fight Night is for recovery, training, treatment, and advancing to the next day rather than starting another contract or shopping. `TownBuilding.ClosedBuildingTexture` provides optional closed-state art while disabled; Arena and Market use closed-building SVG variants.
- First-run town guidance uses `CompanyCareerData.HasCompletedContracts` and `CompanyCareerData.HasReachedSpecialtyBuildings`: Arena stays hidden only while the company has completed no contracts and has no gladiators. Thermae and Training Hall stay hidden until specialty buildings are unlocked after the second completed contract.
- Town buildings should use a 1:1 square footprint. The current town layout is a 3x2 grid split by an implied horizontal road gap; the road is not drawn.
- Town currently includes Arena, Market, Thermae, Training Hall, and the central RosterYard management area with gladiator and equipment buttons.
- Upgradeable buildings implement `IUpgradeable` through `BuildingOverlayPanel`. Thermae and Training Hall currently expose the upgrade button in the overlay header, with levels and gold costs stored on `CompanyRunData`.
- RosterYard also has a compact gold button. Hovering it shows the current phase total near the button, building phase costs in each building's centered gold badge position, and salary on each visible roster-yard gladiator avatar; salary displays as 0 during Day and as the upcoming Night -> Day payment during Night. Building hover badges show the building's own phase cost plus salary for gladiators assigned inside that building. Pressing it opens `GoldCostOverlay`, which discovers `IPhaseGoldCostSource` nodes and lays out visible current-phase costs in side-by-side boxes for gladiators, buildings, and payment result. Building cost previews hide occupancy badges while visible.
- The Town HUD `Select Contract` action and arena contract launch allow debt for arena-return upkeep. Returning from arena completes Day -> Night and immediately charges current phase upkeep, but projected negative gold no longer blocks the contract flow.
- Arena contracts expose a `Donate` action that opens `scenes/town_overlays/arena_donation_overlay.tscn`. Donations buy +1 or +5 current fame for gold, with costs increasing as `CompanyRunData.Fame` rises. Cost and mutation logic live in `CompanyRunData.GetFameDonationGoldCost`, `CanDonateForFame`, and `TryDonateForFame`.
- Arena contracts are authored as `ArenaContractData` resources under `resources/contracts/`. Before any contract is completed, `starter_slime_pit.tres` is the only shown contract and contains one `slime_green.tres` enemy entry as a low-pressure control tutorial. Total mob fame is the contract threat value used for budget and stars; generated gold reward is deterministic from that enemy threat value, gross earned fame is 10% of that threat value, and `GetFameCost(currentCompanyFame)` subtracts 1% current-fame decay for the final net fame reward. `ArenaContractCard` renders one final fame medal value for the net result and groups duplicate enemy resources into one icon row with an `xN` count.
- `ArenaContractGenerator` generates the active Arena overlay contracts at runtime from `MobFamily`, current company fame, and each mob's fame value. Normal days generate Easy/Medium/Hard contracts, while Champion Day generates Easy/Medium/Hard champion contracts with exactly one `ChampionMobData` plus support mobs from that champion's family. Champion selection picks the highest-fame champion that fits the current tier budget while leaving room for at least one same-family support mob, and support mobs cannot exceed the champion's fame value. Contract cards display total mob-fame threat as non-linear 1-10 stars, starting around 30 threat for 1 star. Authored contract `.tres` resources remain useful examples/fallbacks.
- Champion Day contracts are mandatory stakes. Advancing Night -> Day into Champion Day shows a champion warning popup. A champion-contract loss force-retires the company through `SaveNode.ForceRetireCurrentCompany()`, preserving qualifying completed-company records and wiping active company/run state. A champion-contract win increments `CompanyCareerData.ChampionsDefeated`.
- `TownBuilding.OverlayToOpen` opens modal packed-scene building UI over town. `TownBuilding.SceneToOpen` exists for future full-scene navigation but is not used by the current town management flow.
- Shared modal popups should go through `GlobalOverlay` instead of being attached directly to town or arena scenes.
- Custom overlays should be opened through `GlobalOverlay.AddOverlay`. If they need a blurred modal feel, reuse `assets/shaders/PopupBlurBackdrop.gdshader` on a fullscreen backdrop.
- Keep this split intact so phone, controller, and desktop interactions can share the same scene without mixing gameplay world nodes and interface overlays.
- Room layouts should be authored in `.tscn` files. Scene scripts should look up existing nodes and attach behavior instead of generating the layout in code.
- Use neutral scene roots when combining world and controller UI. Do not put controller UI under a `Node2D` root.
- World layouts are authored for `1152x648`. Town and arena use centered `Camera2D` nodes so aspect-ratio expansion grows outward from the center through engine scene behavior.
- Company overview is currently a `GlobalOverlay` modal opened from the town shield. It displays lifetime counters from `CompanyCareerData`, held by `SaveNode` until real persistence is implemented.
- The old separate Roster Hall scene path has been removed. Roster inspection opens from the town-center `RosterYard` using a horizontal gladiator-list overlay with reusable gladiator cards.
- Current gladiator condition state lives on `GladiatorData`. `Exhaustion` is the remaining 0-10 management condition and affects recoverable health and stamina caps.
- Current gladiator attribute progression lives on `GladiatorLevelData` as EXP-backed Strength/Agility/Vitality/Endurance values. `TotalExp` and `TotalLevel` provide summed APIs for future balancing and contract requirements.
- Legacy supply upkeep, continuous town time, and champion deadline timer pressure have been removed for the arena-first loop.
- Gladiator death is centralized in `CompanyRunData.KillGladiator`. Dead gladiators are removed from active `Gladiators`, moved to `Cemetery`, and `GladiatorDeathOverlay.tscn` displays the dead gladiator card. `CemeteryOverlay.tscn` lists all dead gladiators from `CompanyRunData.Cemetery` through the company overview Cemetery button.
- New company runs start with no gladiators through `SaveNode.StartNewCompanyRun()`. Added gladiators should come from Market recruitment and use `CompanyRunData.AddGladiator` so `CompanyCareerData.TotalGladiatorsInCareer` stays correct.
- Market recruits use varied current health from 20-100% max health. `GladiatorData.GetMarketValue()` and `GetMarketSaleValue()` account for current health/stamina and exhaustion, so injured or exhausted gladiators are cheaper to buy and sell.
- Current roster capacity lives on `CompanyRunData.GladiatorCapacity` and defaults to 6. `CompanyRunData.CanAddGladiator`, `AddGladiator`, and `TryBuyGladiator` enforce this cap before adding or spending gold. Phase gold cost timing types live in `scripts/resources/PhaseGoldCost.cs`; scene cost previews use `IPhaseGoldCostSource`, while backend affordability remains in `CompanyRunData`.
- Equipment visuals are tied to `GladiatorEquipmentData`: equipped armor/weapons from inventory are visible on gladiator body art in the town roster and arena. Menu/card UI uses `UiIcon`; body/world presentation layers body, armor, hands, held items, and shadow. The current implementation is duplicated in town and arena actors and should be extracted into a reusable character visual component before adding attack animation.
- Arena combatants own a runtime `ArenaCombatState` resource. `PlayerCombatant` configures it from `GladiatorData.Health`, derived max health, and equipped `ArmorProfile`, then syncs runtime health changes back into `GladiatorData.SetHealth`. `EnemyCombatant` configures it from `EnemyMobData.MaxHealth` and the mob's authored `ArmorProfile`; enemy health labels display the runtime current/max value. `ArenaCombatant` owns team identity and shared damage entry points so future melee, projectile, thrown, and AoE scenes can target combatants without duplicating damage/armor rules.
- Item combat activation is resource-driven. `DamageItemData.MainAction` points to `ArenaCombatActionData`, which references typed effect config resources such as `ArenaMeleeEffectData`. `ArenaCombatActionRunner` spawns reusable effect scenes and initializes them with `ArenaCombatEffectContext`; `ArenaMeleeHitbox.tscn` is the first executor and applies damage through `ArenaCombatant.ApplyDamage`. Starter sword, hammer, spear, and dagger resources now include inline melee action/effect subresources.

## Settings And Input

- Arena control setup is explicit per contract launch. `LocalInputConfig` records controllers joined in that launch flow instead of auto-creating startup controller setup.
- `SettingsConfig.DebugEnabled` stores the temporary project-wide debug flag. Settings UI mutates it directly like the other settings fields; gameplay code can read `SaveNode.DebugEnabled` as a convenience. The Town HUD shows a compact `Dev` menu only while this flag is enabled.
- `SettingsConfig.LowHealthWarningRatio` stores the low-health warning threshold used by town Risk counts and town-world warning icons. Risk classification is centralized through `GladiatorData.GetRiskStatus(...)`, including the critical combined low-health-and-exhausted state so displays do not duplicate warning icons for one gladiator.
- Idle assigned gladiators are counted separately through `CompanyRunData.GetIdleAssignedGladiatorCount`. The clock icon is `assets/ui/gladiator_icons/idle.svg` and means assigned but no work will run this phase; `assets/ui/gladiator_icons/exhaustion.svg` is now the exhausted warning icon.
- Auto-detect currently maps console-like platforms to Gamepad, mobile to Gamepad when a gamepad is connected or Touch otherwise, and desktop to Keyboard.
- `ArenaControlConfigOverlay` clears and rebuilds `LocalInputConfig.ControllerSetups` per contract by assigning controls to Arena gladiators left-to-right.
- Gamepad prompts use imported icons under `assets/ui/input_icons/`. Desktop keyboard primary input is labeled as Keyboard but currently uses the mouse icon for compact visual display.
- Local co-op is planned for up to four local players. Keep future join/leave mutation in `LocalInputConfig` so gameplay scenes can query the same source of truth.

## Not Yet Present

- Gameplay combat scripts
- Player, roster, gear, upkeep, healing, stamina, training, champion deadline failure handling, or full city UI systems
- Runtime enemy mob actors; enemy `.tres` metadata exists, but mob gameplay scenes are not implemented yet
- Runtime contract execution; arena contract `.tres` metadata exists, but actual combat spawning/result handling is still placeholder-level
- Real local co-op join/leave input handling and per-player gameplay spawning
- Export presets

## Intended First Prototype

- One controllable gladiator.
- One arena.
- Slime enemies.
- Basic movement and attack.
- Phone, controller, and desktop input support.
- Simple contract selection.
- Money reward after winning.
- Gladiator death and replacement.

## Documentation Policy

Keep this documentation accurate as the project evolves. Prefer documenting real decisions and implemented structure over speculative plans.
