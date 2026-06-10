# Authoring Mobs

This document explains how to add or tune an enemy mob resource for contracts, codex display, arena spawning, and future enemy behavior.

## Fast Path

To add a normal enemy:

1. Duplicate a similar file in `resources/mobs/`.
2. Duplicate or reuse a matching appearance in `resources/mob_appearances/`.
3. Set name, description, icon/appearance, family, health, armor, and fame value.
4. Add the mob to the correct family file in `resources/mob_families/`.
5. Open the codex or a contract that can select the mob and verify it appears correctly.

If the mob is not added to a family file, generated contracts and the codex family list will not discover it.

## Source Files

- Mob resources live in `resources/mobs/`.
- Mob appearance resources live in `resources/mob_appearances/`.
- Enemy family resources live in `resources/mob_families/`.
- Mob resource scripts live in `scripts/resources/mobs/`.
- Shared armor and status resources live under `scripts/resources/combat/` and `resources/combat/`.

Use existing mobs such as `resources/mobs/slime_green.tres` and family roots such as `resources/mob_families/slimes.tres` as templates.

## Minimum Valid Mob

A useful enemy resource needs:

```text
EnemyMobData or ChampionMobData
  DisplayName
  Description
  Appearance or UiIcon
  Family
  MaxHealth
  ArmorProfile
  FameValue
```

`Scene` can stay null for now. Null means arena spawning uses the generic `EnemyCombatant.tscn` fallback.

`StatusProfile` can stay null unless the mob needs custom status resistance. Null uses the default mob or champion status profile.

## Full Resource Shape

A normal enemy uses `EnemyMobData`.

```text
EnemyMobData
  DisplayName
  Description
  UiIcon
  Appearance
  Scene
  Family
  MaxHealth
  ArmorProfile
  StatusProfile
  FameValue
```

A champion uses `ChampionMobData`, which currently inherits the same fields as `EnemyMobData`. Champion behavior is identified by the resource type.

## Important Fields

- `DisplayName`: Player-facing name shown in contracts, codex, labels, and combat HUD.
- `Description`: Short readable description for codex and contract flavor.
- `UiIcon`: Fallback UI icon if `Appearance` does not provide one.
- `Appearance`: Preferred visual package for UI and world rendering.
- `Family`: The `MobFamily` enum value used for sorting and matching to family roots.
- `MaxHealth`: Runtime enemy health in arena combat.
- `ArmorProfile`: Authored `ArmorData` for damage mitigation and immunity.
- `FameValue`: The mob's single threat, contract-budget cost, and reward contribution value.

`StatusProfile` is optional. If it is null, `EnemyCombatant` falls back to `resources/combat/status_profiles/default_mob_status_profile.tres` or `default_champion_status_profile.tres` for champions.

`Scene` is optional. If it is null, `ArenaEnemySpawner` uses the generic `scenes/components/arena/EnemyCombatant.tscn` fallback and configures it from the `.tres`.

## Appearance

`MobAppearanceData` inherits the shared `CharacterAppearanceData` fields.

```text
MobAppearanceData
  DisplayName
  UiIcon
  BodyForward
  BodyBack
  UsesSeparatedHands
  HandTexture
```

Use `Appearance.UiIcon` as the primary icon when available. `MobData.GetUiIconTexture()` falls back to `UiIcon` only if the appearance has no icon.

Use separated hands only when the mob design needs hand sprites. Body-only mobs such as slimes should keep `UsesSeparatedHands = false`.

## Family Registration

New mobs do not enter contract generation just because a file exists in `resources/mobs/`. Add the mob to the correct `EnemyMobFamilyData` resource in `resources/mob_families/`.

Each family entry is a `MobFamilyMobEntryData` row.

```text
MobFamilyMobEntryData
  Mob
  MinimumCompanyFame
```

`MinimumCompanyFame` controls when generated contracts can select that mob. Keep each family sorted from easiest to hardest for readability.

## Fame Value

`FameValue` is the most important balance number on an enemy.

It currently acts as:

- Contract threat contribution.
- Contract generation budget cost.
- Gold/fame reward input.
- Codex sorting value inside a family.

Do not tune `FameValue` as flavor only. If it changes, generated contract difficulty and rewards change too.

Current broad bands:

- Starter enemies: around `5` to `30`.
- Early normal enemies: around `40` to `80`.
- Strong normal enemies: around `100` to `150`.
- Champions: should be high enough to anchor Champion Day contracts.

## Health, Armor, And Status

`MaxHealth` controls raw survival time. `ArmorProfile` controls damage mitigation per `CombatDamageType`.

Armor uses:

```text
ArmorData
  BaseValue
  TypeOverrides
  ImmuneTypes
```

Use armor to define what the mob resists, not just how much health it has. Examples:

- Skeletons can resist `Pierce` but be weaker to `Crush`.
- Demons can resist `Heat` or `Cursed`.
- Ice or crystal mobs can resist `Cold` but be weak to `Crush` or `Heat`.

`ArmorData.ImmuneTypes` defaults to `Silver` and `Holy`. If a mob should not have those immunities, set a different `ImmuneTypes` array on its `ArmorProfile`.

Status behavior comes from `CombatantStatusProfileData`. Use it when a mob should resist, ignore, cap, or amplify statuses such as Poison or Stun. See `docs/status-effects.md` for the detailed status model.

## Runtime Scene

`EnemyMobData.Scene` is for future family-specific or unique behavior scenes.

Current rule:

```text
Scene == null
  -> spawn generic EnemyCombatant.tscn
Scene != null
  -> spawn that packed scene and configure it from EnemyMobData
```

Keep `EnemyCombatant` as the shared shell. Put family movement, attack selection, and special behavior into child components on the custom scene instead of duplicating health/team/damage logic.

## Authoring Checklist

1. Create or reuse a UI icon under `assets/ui/mobs/`.
2. Create or reuse body textures and a `MobAppearanceData` under `resources/mob_appearances/`.
3. Create an `EnemyMobData` or `ChampionMobData` under `resources/mobs/`.
4. Set display fields, appearance, family, health, armor, optional status profile, and fame value.
5. Add the mob to the correct `resources/mob_families/*.tres` with a `MinimumCompanyFame` gate.
6. Open the codex and verify the enemy appears under the expected family.
7. Launch a generated or authored contract that can include the mob and verify the icon/name/count display.
8. Verify armor immunities, especially the default `Silver` and `Holy` entries.
9. Run `godot --headless --import`, `dotnet build`, and `godot --headless --quit` after resource changes.

## Common Mistakes

- Adding a mob file but forgetting to add it to a family resource.
- Treating `FameValue` as only a reward number, when it also controls generated difficulty.
- Setting only `UiIcon` while the appearance points to an older icon.
- Forgetting that new `ArmorData` defaults to `Silver` and `Holy` immunity.
- Creating a custom enemy scene that bypasses `ArenaCombatant.ApplyDamage(...)`.
- Duplicating enemy stats in contracts instead of referencing the mob `.tres`.
- Giving a champion normal `EnemyMobData` type instead of `ChampionMobData`.

## Current Limits

- Runtime family-specific enemy AI is not implemented yet.
- Generic enemies spawn through the fallback `EnemyCombatant.tscn` when `Scene` is null.
- Enemy attacks are not yet authored on `EnemyMobData`; player item attacks are currently the main resource-driven combat path.
