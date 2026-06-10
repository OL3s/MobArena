# Authoring Player Items

This document explains how to structure player item resources, including weapons, off-hand items, armor, visuals, damage, and attacks.

## Fast Path

To add a weapon:

1. Duplicate a similar item under `resources/items/main_hand/` or `resources/items/off_hand/`.
2. Set display fields, icon, held visual tuning, cost, and condition.
3. Set `Damage` with one or more typed `CombatDamageEntryData` rows.
4. Set `MainAction` to an `ArenaCombatActionData`.
5. Make the action effect use source item damage unless the effect has its own explosion/cloud/payload damage.
6. Verify the item card, store showcase, codex entry, and in-hand visual.

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
  ArmorProfile
```

Held and armor textures are strongly recommended because town and arena characters render equipped gear visibly.

## Item Types

All player items inherit `ItemData`.

```text
ItemData
  DisplayName
  Description
  UiIcon
  HeldTexture
  HeldDisplayHeight
  HeldRotationDegrees
  HeldTextureOffset
  Cost
  Condition
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
- `Cost`: Base buy/sell/economy value.
- `Condition`: Current condition from `0.0` to `1.0`. Most authored templates start at `1.0`.

Do not change `Cost` at purchase time. Runtime item copies preserve original authored cost, and resale logic depends on that base value.

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

Damage types include physical, elemental, and conditional types such as `Slash`, `Pierce`, `Crush`, `Heat`, `Cold`, `Acid`, `Silver`, `Holy`, `Cursed`, `Undead`, `Demon`, `Beast`, and `Champion`.

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
- Blessed armor can resist `Cursed` or `Undead`.
- Fireproof armor can resist `Heat` but maybe not `Cold`.

`ImmuneTypes` completely ignores those damage types. Use it carefully, because immunity is stronger than high armor.

`ArmorData.ImmuneTypes` defaults to `Silver` and `Holy`. If the armor should not have those immunities, set a different array instead of leaving the default untouched.

## Main-Hand And Off-Hand Roles

`MainHandItemData` and `OffHandItemData` both inherit `DamageItemData`, so both can have `Damage` and `MainAction`.

Current player input supports both main-hand and off-hand action paths in `PlayerCombatant`.

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
        Apply.Damage = Heat or Crush explosion damage
```

Poison flask:

```text
DamageItemData
  MainAction = ArenaCombatActionData
    Effect = ArenaAttackThrownProjectileData
      OnExpireEffect = ArenaAttackAreaOfEffectData
        Apply.UseSourceItemDamage = false
        Apply.StatusApplications = Poison
        LifetimeSeconds > TickSeconds
```

## Authoring Checklist

1. Pick the correct resource type: `MainHandItemData`, `OffHandItemData`, or `ArmorItemData`.
2. Set `DisplayName`, `Description`, `UiIcon`, `Cost`, and `Condition`.
3. Add held or armor visuals and tune display height, rotation, and offset in-game.
4. For weapons, add `CombatDamageData` entries with intentional damage types.
5. For weapons, add `MainAction` with an `ArenaCombatActionData`.
6. Set the root effect and correct `ScenePath`.
7. Add an `ArenaCombatApplyData` and decide whether it uses source item damage.
8. Add chained effects only when the action pattern needs them.
9. Verify item card, store showcase, and codex display.
10. Test attacks in `tests/attack_effect_sandbox.tscn` if the item uses a new attack pattern.
11. Verify armor immunities if the item is armor.
12. Run `godot --headless --import`, `dotnet build`, and `godot --headless --quit` after resource or script changes.

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
