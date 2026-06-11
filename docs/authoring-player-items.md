# Authoring Player Items

This document explains how to structure player item resources, including weapons, off-hand items, armor, visuals, damage, and attacks.

## Fast Path

To add a weapon:

1. Duplicate a similar item under `resources/items/main_hand/` or `resources/items/off_hand/`.
2. Set display fields, icon, held visual tuning, cost, and durability.
3. Set `Requirements` and `LevelMultiplier` when the item should scale with gladiator training or ask for minimum attributes.
4. Set `Damage` with one or more typed `CombatDamageEntryData` rows.
5. Set `MainAction` to an `ArenaCombatActionData`.
6. Make the action effect use source item damage unless the effect has its own explosion/cloud/payload damage.
7. Verify the item card, store showcase, codex entry, and in-hand visual.

To add armor:

1. Duplicate a similar file under `resources/items/armor/`.
2. Set display fields, icon, armor textures, and cost.
3. Set `ArmorProfile` with base armor, type overrides, and intended immunities.
4. Verify the armor overlay on a gladiator body.

## Source Files

- Main-hand items live in `resources/items/main_hand/`.
- Off-hand items live in `resources/items/off_hand/`.
- Armor items live in `resources/items/armor/`.
- Item resource scripts live in `scripts/resources/items/`.
- Combat damage resources live in `scripts/resources/combat/`.
- Attack resources live in `scripts/resources/combat/actions/` and `scripts/resources/combat/effects/`.
- Item icons and held/armor art currently live under `assets/ui/items/`.

Use existing items such as `resources/items/main_hand/training_sword.tres`, `resources/items/off_hand/dagger.tres`, and `resources/items/armor/cloth_wraps.tres` as templates.

## Minimum Valid Items

A minimum weapon needs:

```text
MainHandItemData or OffHandItemData
  DisplayName
  Description
  UiIcon
  Cost
  Requirements
  LevelMultiplier
  Damage
  MainAction
```

A minimum armor item needs:

```text
ArmorItemData
  DisplayName
  Description
  UiIcon
  Cost
  Requirements
  LevelMultiplier
  ArmorProfile
```

Held and armor textures are strongly recommended because town and arena characters render equipped gear visibly.

## Item Types

All player-facing item resources inherit `ItemData`. `ItemData` is the generic root for shared display, economy, and durability data. Equippable gear forks through `EquipmentItemData`; coatings fork through `ItemCoatingData`.

```text
ItemData
  DisplayName
  Description
  UiIcon
  Cost
  MaxDurability
  Durability
```

Equippable player gear inherits `EquipmentItemData`.

```text
EquipmentItemData
  HeldTexture
  HeldDisplayHeight
  HeldRotationDegrees
  HeldTextureOffset
  Weight
  Requirements
  LevelMultiplier
  AppliedCoating
```

Combat-capable hand items inherit `DamageItemData`.

```text
DamageItemData
  Damage
  MainAction
```

Concrete hand item types:

- `MainHandItemData`: primary hand weapons, with optional `IsTwoHanded`.
- `OffHandItemData`: off-hand weapons or tools that can also define an attack.

Armor uses `ArmorItemData`.

```text
ArmorItemData
  ArmorProfile
  ArmorForwardTexture
  ArmorBackTexture
  ArmorDisplayHeight
  ArmorTextureOffset
```

## Shared Display Fields

- `DisplayName`: Player-facing item name.
- `Description`: Short codex/store text.
- `UiIcon`: Icon used by cards, codex, inventory, and store UI.
- `TypeTag`: Generic store/codex sorting family, such as `Sword`, `Armor`, `Honing`, or `Poison`. Use `Other` only when no existing family fits.
- `StrengthTag`: Generic store/codex sorting tier or intensity, such as `Training`, `Stone`, `Wood`, `Bronze`, `Iron`, `Black`, `Weak`, `Standard`, `Strong`, `Light`, `Medium`, or `Heavy`. Use `Other` only when no existing tier fits.
- `Cost`: Base buy/sell/economy value.
- `MaxDurability`: Maximum number of durability uses.
- `Durability`: Current remaining durability uses.

Condition is not authored directly. Use `GetCondition()` to derive the normalized `0.0` to `1.0` condition from `Durability / MaxDurability`.

Do not change `Cost` at purchase time. Runtime item copies preserve original authored cost, and resale logic depends on that base value.

Treat authored `Cost` as progression pacing, not a tiny stat adjustment. Training, Stone, and Wood gear should be affordable early purchases; Bronze should cost hundreds of gold; Iron should cost thousands; Black should feel late-game and can sit in five-figure prices. The jumps do not need to be exact multipliers, but the gap between Wood, Bronze, Iron, and Black should be large enough that each material tier changes the economy plan.

## Material And Tier Naming

Use material names as the main player-facing convention for item quality. This lets players learn the economy without memorizing every individual stat line.

Current intended tier language:

- `Training`: cheapest and weakest baseline, safe for starter loops and tutorials.
- `Stone`: crude early weapons between training gear and proper wooden/metal equipment.
- `Wooden`: cheap real equipment, still below metal gear.
- `Bronze`: first dependable upgrade tier.
- `Iron`: stronger mid-tier equipment.
- `Black`: high-grade late-tier physical equipment.

Armor materials should generally provide `Light`, `Medium`, and `Heavy` variants. Light armor is for agility-leaning builds with low weight, medium is balanced, and heavy is for strength-leaning builds with much higher weight and protection.

Prefer names such as `Stone Spear -> Wood Spear -> Bronze Spear`, `Bronze Sword -> Iron Sword -> Black Sword`, `Bronze Greatsword -> Iron Greatsword -> Black Greatsword`, and matching shield/armor material names. Keep training weapons as simple tutorial baselines, but avoid making unrelated fantasy names the only clue that an item is better.

Each authored item should use its own `UiIcon` SVG path, even when it is a tier variant of the same weapon or armor shape. Equippable items should also author a non-zero `Weight` value.

Most normal player item baselines should use physical types such as `Slash`, `Pierce`, and `Crush`. Special types such as `Heat`, `Cold`, and `Acid` are valid when the item fantasy explicitly calls for them, especially explosives, coatings, monster parts, and future upgrades. Do not normalize every item into elemental damage just because the enum supports it.

## Requirements And Gladiator Scaling

Every equippable item inherits optional `Requirements` and `LevelMultiplier` fields from `EquipmentItemData`.

`Weight` is an equipment-only integer tuning value for future handling, stamina, and movement rules. Keep it authored on the equipment item, not on coatings.

`ItemRequirementData` currently supports only the specific combat-skill requirements:

```text
ItemRequirementData
  RequiredStrength
  RequiredAgility
```

The first pass records requirement data but does not need to hard-block equipping. The intended direction is that an undertrained gladiator may still equip a demanding item, but will perform badly with it. For example, too little Strength for a heavy two-handed weapon or too little Agility for a bow can push the gladiator straight into `Exhausted` instead of completing the attack cleanly.

Do not use Vitality or Endurance as item requirements. Vitality represents health. Endurance represents stamina amount, stamina generation, and exhaustion recovery behavior.

`ItemLevelMultiplierData` records how gladiator training should improve item performance:

```text
ItemLevelMultiplierData
  StrengthPowerPerLevel
  AgilitySpeedPerLevel
  StrengthInfluence
  AgilityInfluence
  BaseMultiplier
  MaxMultiplier
  BaseSpeedMultiplier
  MaxSpeedMultiplier
```

The weapon is only part of the output. The gladiator supplies the rest through training. Better material raises the item's authored baseline, while the multiplier config lets relevant combat attributes make the same item meaningfully better in trained hands.

Strength should primarily affect power and heavy control. It should matter much more for hammers, shields, greatswords, and other large/heavy weapons than for knives, flasks, or light tools.

Agility should primarily affect action speed and handling. It should matter more for knives, bows, flasks, and small/light items than for large weapons. Large weapons can still benefit from Agility, but with a much smaller influence.

Bows and similar ranged weapons should usually require and scale with Agility. Heavy crossbows can have mixed Strength/Agility requirements because draw weight and handling both matter.

Vitality and Endurance should not be normal item power multipliers. Keep those as overall gladiator stats: Vitality affects health, while Endurance affects stamina amount, stamina generation, and exhaustion recovery behavior.

## Held Visuals

Hand items can define in-world held visuals.

```text
HeldTexture
HeldDisplayHeight
HeldRotationDegrees
HeldTextureOffset
```

`GetHeldTexture()` falls back to `UiIcon` when `HeldTexture` is null.

Tune these fields in the actual town/arena character presentation, not only in a resource inspector preview. The same item can look correct in UI but wrong when attached to a gladiator hand.

## Damage

Weapons use `CombatDamageData`, which contains one or more `CombatDamageEntryData` rows.

```text
CombatDamageData
  Entries

CombatDamageEntryData
  Type
  Damage
```

Damage types are instant combat payload channels: `Slash`, `Pierce`, `Crush`, `Heat`, `Cold`, `Acid`, `Silver`, and `Holy`. Enemy families such as Undead, Demons, or Beasts, and champion identity, are metadata, not damage types.

Use `Heat` or `Cold` only when it makes authored sense. An explosive payload can reasonably be `Crush + Heat`; a frost coating can reasonably be `Cold` plus a status payload. They should remain special cases rather than default weapon-tier damage.

Use multiple entries when the item should split damage across types. Example:

```text
Training Sword
  Slash 80
  Crush 40
```

Splitting damage matters because each entry is mitigated separately against the target's armor and immunity rules.

## Weapon Attacks

Hand items attach their combat behavior through `DamageItemData.MainAction`.

```text
DamageItemData
  Damage
  MainAction -> ArenaCombatActionData
```

The action controls timing and points to an effect.

```text
ArenaCombatActionData
  DisplayName
  Effect
  Buildup
  WindupSeconds
  StaminaCost
  SpawnDistance
  MaxChainDepth
```

See `docs/authoring-attacks.md` for full attack structure details.

For normal weapons, the attack effect usually has an `ArenaCombatApplyData` with `UseSourceItemDamage = true`. That means the hit uses the item's `Damage` field.

Use `UseSourceItemDamage = false` when the effect should use separate authored damage, such as a bomb explosion, poison cloud, fire patch, or magic effect.

## Attack Pattern UI

Item cards and store showcases inspect `MainAction.Effect` and chained effects to display attack-type icons.

The icons come from effect type metadata:

- `ArenaMeleeEffectData`: melee icon.
- `ArenaAttackLinearProjectileData`: linear projectile icon.
- `ArenaAttackThrownProjectileData`: thrown projectile icon.
- `ArenaAttackAreaOfEffectData`: area-of-effect icon.

If an item's card shows the wrong pattern, inspect the root effect and its `OnHitEffect` / `OnExpireEffect` chain.

## Armor Items

Armor defines mitigation through `ArmorProfile`.

```text
ArmorData
  BaseValue
  TypeOverrides
  ImmuneTypes
```

Armor visuals use:

```text
ArmorForwardTexture
ArmorBackTexture
ArmorDisplayHeight
ArmorTextureOffset
```

Use `TypeOverrides` to make armor distinct instead of only raising `BaseValue`. Examples:

- Cloth has low base armor and no strong type identity.
- Mail can resist `Slash` but be weaker against `Crush`.
- Blessed armor can resist `Holy` or `Silver` when that matches the intended fantasy.
- Future fireproof armor can resist `Heat` once mobs, coatings, or upgrade systems make that damage space relevant.

`ImmuneTypes` completely ignores those damage types. Use it carefully, because immunity is stronger than high armor.

`ArmorData.ImmuneTypes` defaults to `Silver` and `Holy`. If the armor should not have those immunities, set a different array instead of leaving the default untouched.

## Main-Hand And Off-Hand Roles

`MainHandItemData` and `OffHandItemData` both inherit `DamageItemData`, so both can have `Damage` and `MainAction`.

Current player input supports both main-hand and off-hand action paths in `PlayerCombatant`.

If a hand slot is empty, `PlayerCombatant` falls back to hidden unarmed default resources under `resources/combat/player_defaults/`: `main_hand_punch.tres` for main-hand input and weaker `off_hand_punch.tres` for off-hand input. These resources are not normal inventory/store/codex items.

Design guidance:

- Main-hand items should usually be the primary damage plan.
- Off-hand items should usually be faster, defensive, utility-focused, status-focused, or lower-damage.
- `IsTwoHanded` should be true only when the item fantasy and equipment rules require occupying both hands.

## Non-Melee Item Examples

Use the reusable attack executors to create real non-melee player items.

Bow:

```text
MainHandItemData
  Damage = Pierce
  MainAction = ArenaCombatActionData
    Effect = ArenaAttackLinearProjectileData
      ScenePath = ArenaAttackLinearProjectile.tscn
      Apply.UseSourceItemDamage = true
```

Bomb:

```text
OffHandItemData or MainHandItemData
  Damage = optional or null
  MainAction = ArenaCombatActionData
    Buildup.ScaleRange = true
    Effect = ArenaAttackThrownProjectileData
      ScenePath = ArenaAttackThrownProjectile.tscn
      OnExpireEffect = ArenaAttackAreaOfEffectData
        Apply.UseSourceItemDamage = false
        Apply.Damage = Crush + Heat explosion damage
```

Poison flask:

```text
DamageItemData
  MainAction = ArenaCombatActionData
    Effect = ArenaAttackThrownProjectileData
      OnExpireEffect = ArenaAttackAreaOfEffectData
        Apply.UseSourceItemDamage = false
        Apply.Damage = Acid tick damage
        Apply.StatusApplications = Poison
        LifetimeSeconds > TickSeconds
```

The poison flask pattern should deal `Acid` damage while a target remains inside the AOE and apply `Poison` status so the effect can linger after the target leaves. Blob/slime mobs should use poison-immune status profiles so they ignore the status but can still take acid damage unless their armor says otherwise.

## Coatings

Coatings are item resources under `resources/coatings/` using `ItemCoatingData`, which inherits the generic `ItemData` root. Market has a single `Items` storefront for both equipment and coatings. Bought coatings enter the shared Items inventory and can be dragged onto a gladiator to choose which equipped item receives the coating.

Application APIs:

```csharp
runData.TryApplyCoatingToItem(item, coating, careerData);
```

Coatings have their own durability separate from the item durability. It uses the same source-of-truth model as items: `Durability` and `MaxDurability` are authored as integer uses, and `GetCondition()` derives the normalized `0.0` to `1.0` condition. Authored templates should start full with `Durability == MaxDurability`; oils should generally have lower max durability so they expire faster than normal item durability.

Shared coating resource shape:

```text
ItemCoatingData
  DisplayName
  Description
  UiIcon
  Cost
  MaxDurability
  Durability
```

There are two concrete coating branches:

```text
AdditiveItemCoatingData
  DamageEntries
  EffectEntries

MultiplierItemCoatingData
  DamageMultipliers
```

Additive damage and effect entries are interpreted by the item slot that carries the coating:

- On main-hand/off-hand items, `DamageEntries` are added attack damage and `EffectEntries` are added status/effect buildup.
- On armor items, `DamageEntries` are temporary damage-defense values and `EffectEntries` are temporary effect-defense values.

Current entry resources:

```text
CoatingDamageEntryData
  Type: CombatDamageType
  Value: int

CoatingEffectEntryData
  Type: StatusEffectType
  Value: float

CoatingDamageMultiplierData
  Type: CombatDamageType
  Multiplier: float
```

Multiplier coatings multiply matching typed damage rows instead of adding flat values. The first authored multiplier family is `Weak Honing Oil`, `Honing Oil`, and `Strong Honing Oil`, each multiplying `Slash` damage while active.

The starting poison family uses the additive branch and has Oil, Coating, and Plating rows:

- `Weak Poison Oil`: low durability, poison effect only.
- `Poison Oil`: low durability, stronger poison effect.
- `Strong Poison Oil`: low durability, poison effect plus a small acid damage/defense row.
- `Weak Poison Coating`: medium durability, acid row plus poison effect row.
- `Poison Coating`: medium durability, stronger acid and poison rows.
- `Strong Poison Coating`: medium-high durability, strong acid and poison rows.
- `Weak Poison Plating`: high durability, acid row plus poison effect row.
- `Poison Plating`: higher durability, stronger acid and poison rows.
- `Strong Poison Plating`: highest current durability, strongest current acid and poison rows.

Oil naming means cheap, short-lived, and usually weaker. `Coating` is the middle form with better durability and values. `Plating` is expensive, longer-lasting, and generally stronger.

Power labels should read as `Weak`, no prefix for normal, `Strong`, and only later `Extreme` if needed. Example families: `Weak Poison Oil`, `Poison Oil`, `Strong Poison Oil`; `Weak Poison Coating`, `Poison Coating`, `Strong Poison Coating`; `Weak Poison Plating`, `Poison Plating`, `Strong Poison Plating`.

Do not treat `Stun` as a normal coating effect. Stun is an impact/control status that usually derives from applied damage, force, or explicitly authored attack logic. Only explicit stun fantasies such as a future `Shock Oil`, `Stun Bomb`, or `Concussive Plating` should include a stun effect entry.

Coating resources need only UI icons for now, not in-world held sprites. The Items storefront uses the coating's authored `UiIcon` like every other item. Instant rows use `assets/ui/combat/instant/type_<damage>.svg`; effect rows use `assets/ui/combat/effects/status_<effect>.svg`. Add matching SVGs whenever a new `CombatDamageType` or `StatusEffectType` is added.

## Authoring Checklist

1. Pick the correct resource type: `MainHandItemData`, `OffHandItemData`, or `ArmorItemData`.
2. Set `DisplayName`, `Description`, `UiIcon`, `Cost`, `MaxDurability`, and `Durability`.
3. Add `Requirements` and `LevelMultiplier` if the item should care about gladiator level or attributes.
4. Add held or armor visuals and tune display height, rotation, and offset in-game.
5. For weapons, add `CombatDamageData` entries with intentional damage types.
6. For weapons, add `MainAction` with an `ArenaCombatActionData`.
7. Set the root effect and correct `ScenePath`.
8. Add an `ArenaCombatApplyData` and decide whether it uses source item damage.
9. Add chained effects only when the action pattern needs them.
10. Verify item card, store showcase, and codex display.
11. Test attacks in `tests/attack_effect_sandbox.tscn` if the item uses a new attack pattern.
12. Verify armor immunities if the item is armor.
13. Run `godot --headless --import`, `dotnet build`, and `godot --headless --quit` after resource or script changes.

## Common Mistakes

- Creating weapon damage but forgetting `MainAction`.
- Creating `MainAction` but forgetting the effect `ScenePath`.
- Leaving `UseSourceItemDamage = true` on effects that should use their own explosion/cloud damage.
- Duplicating damage in both the item and effect without deciding which one should apply.
- Forgetting that each damage entry is mitigated separately.
- Leaving default `Silver` and `Holy` armor immunities when the item should not have them.
- Using `ImmuneTypes` on armor when a type override would be enough.
- Tuning held visuals only from the item card instead of checking town and arena characters.
- Forgetting that item attack icons come from effect chains, not from item type.
- Naming item tiers inconsistently so players cannot tell whether material quality is an upgrade.
- Normalizing `Heat` or `Cold` across ordinary player items instead of keeping them for explicit fantasies such as explosions, frost, mobs, coatings, or upgrades.
