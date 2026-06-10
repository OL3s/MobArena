using Godot;
using System;
using System.Collections.Generic;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI.ItemShowcases;

public abstract partial class ItemStoreShowcaseBase : VBoxContainer, IItemStoreShowcase
{
    private const string ItemStoreStatRowScenePath = "res://scenes/components/ui/ItemStoreStatRow.tscn";
    private const string ItemStoreStatSectionScenePath = "res://scenes/components/ui/ItemStoreStatSection.tscn";
    private const string ItemStoreDamagePillScenePath = "res://scenes/components/ui/ItemStoreDamagePill.tscn";
    private const int MaxActionPatternIcons = 12;

    private PackedScene _itemStoreStatRowScene;
    private PackedScene _itemStoreStatSectionScene;
    private PackedScene _itemStoreDamagePillScene;
    private ItemStoreStatSection _activeStatSection;

    public override void _Ready()
    {
        _itemStoreStatRowScene = ResourceLoader.Load<PackedScene>(ItemStoreStatRowScenePath);
        _itemStoreStatSectionScene = ResourceLoader.Load<PackedScene>(ItemStoreStatSectionScenePath);
        _itemStoreDamagePillScene = ResourceLoader.Load<PackedScene>(ItemStoreDamagePillScenePath);
    }

    public abstract void Configure(ItemData item);

    protected void ClearShowcase()
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        _activeStatSection = null;
    }

    protected void AddDamageStats(DamageItemData item)
    {
        BeginStatSection("Damage");
        if (!HasAnyDamage(item.Damage))
        {
            AddStat("Damage", "None");
            return;
        }

        AddStat("Total Damage", item.Damage.GetRawTotalDamage().ToString());
        AddDamagePillStack(item.Damage);
    }

    protected void AddActionStats(ArenaCombatActionData action)
    {
        BeginStatSection("Action");
        if (action == null)
        {
            AddStat("Action", "None");
            return;
        }

        AddStat("Action", action.DisplayName);
        AddStat("Windup", $"{action.WindupSeconds:0.##}s");
        AddStat("Stamina Cost", action.StaminaCost.ToString());
        AddStat("Spawn Distance", action.SpawnDistance.ToString("0.#"));
        AddStat("Max Chain Depth", action.MaxChainDepth.ToString());
        if (action.Buildup != null)
            AddStat("Buildup", action.Buildup.ToString());

        if (action.Effect == null)
            return;

        BeginStatSection("Effect");
        AddActionPatternStack(action.Effect);
        AddStat("Primary Type", action.Effect.AttackTypeLabel);
        AddStat("Effect Lifetime", $"{action.Effect.LifetimeSeconds:0.##}s");
        AddStat("Max Hits", action.Effect.MaxHits.ToString());
        AddStat("Can Multi-Hit", action.Effect.CanHitSameTargetMultipleTimes ? "Yes" : "No");
        var apply = action.Effect.Apply;
        AddStat("Uses Item Damage", apply?.UseSourceItemDamage == true ? "Yes" : "No");

        if (HasAnyDamage(apply?.Damage))
        {
            AddStat("Effect Damage", apply.Damage.GetRawTotalDamage().ToString());
            AddDamagePillStack(apply.Damage);
        }

        if (apply?.ForceStrength > 0f)
            AddStat("Hit Force", apply.ForceStrength.ToString("0.#"));

        if (action.Effect is ArenaMeleeEffectData melee)
        {
            AddStat("Hitbox Radius", melee.HitboxRadius.ToString("0.#"));
            AddStat("Active Time", $"{melee.ActiveSeconds:0.##}s");
            AddStat("Forward Offset", melee.ForwardOffset.ToString("0.#"));
        }
        else if (action.Effect is ArenaAttackLinearProjectileData linear)
        {
            AddStat("Speed", linear.Speed.ToString("0.#"));
            AddStat("Range", linear.Range.ToString("0.#"));
            AddStat("Hitbox", $"{linear.HitboxLength:0.#} x {linear.HitboxWidth:0.#}");
            AddStat("Penetration", linear.MaxPenetrations.ToString());
        }
        else if (action.Effect is ArenaAttackThrownProjectileData thrown)
        {
            AddStat("Range", thrown.Range.ToString("0.#"));
            AddStat("Travel Time", $"{thrown.TravelSeconds:0.##}s");
            AddStat("Arc Height", thrown.ArcHeight.ToString("0.#"));
        }
        else if (action.Effect is ArenaAttackAreaOfEffectData area)
        {
            AddStat("Radius", area.Radius.ToString("0.#"));
            AddStat("Tick Rate", $"{area.TickSeconds:0.##}s");
            AddStat("Unlimited Hits", area.UnlimitedHits ? "Yes" : "No");
        }
    }

    protected void AddArmorStats(ArmorItemData armor)
    {
        BeginStatSection("Armor Profile");
        var profile = armor.ArmorProfile;
        if (profile == null)
        {
            AddStat("Armor", "None");
            return;
        }

        AddStat("Base Armor", profile.BaseValue.ToString());
        AddArmorComparisonStack("Weaknesses", profile, armorValue => armorValue < profile.BaseValue);
        AddArmorComparisonStack("Strengths", profile, armorValue => armorValue > profile.BaseValue);
        AddArmorImmunityStats(profile);
    }

    protected void AddStat(string label, string value)
    {
        _activeStatSection ??= CreateStatSection("Stats");
        var row = _itemStoreStatRowScene.Instantiate<ItemStoreStatRow>();
        row.Configure(label, value);
        _activeStatSection.AddRow(row);
    }

    protected void BeginStatSection(string title)
    {
        _activeStatSection = CreateStatSection(title);
    }

    protected void AddDamagePillStack(CombatDamageData damage)
    {
        if (damage?.Entries == null || damage.Entries.Count <= 0)
            return;

        _activeStatSection ??= CreateStatSection("Damage");
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        foreach (var entry in damage.Entries)
        {
            if (entry == null)
                continue;

            var pill = _itemStoreDamagePillScene.Instantiate<ItemStoreDamagePill>();
            row.AddChild(pill);
            pill.Configure(entry.Type, entry.GetRawDamage());
        }

        _activeStatSection.AddRow(row);
    }

    private void AddActionPatternStack(ArenaCombatEffectData rootEffect)
    {
        var entries = new List<(ArenaCombatEffectData Effect, string Source)>();
        CollectActionPattern(rootEffect, "Start", entries, new HashSet<ArenaCombatEffectData>());
        if (entries.Count <= 0)
            return;

        _activeStatSection ??= CreateStatSection("Effect");

        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 4);
        wrapper.AddChild(new Label
        {
            Text = "Attack Pattern",
            ThemeTypeVariation = "HeaderSmall"
        });

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        wrapper.AddChild(row);

        foreach (var (effect, source) in entries)
            row.AddChild(CreateAttackTypeBadge(effect, source));

        _activeStatSection.AddRow(wrapper);
    }

    private static void CollectActionPattern(ArenaCombatEffectData effect, string source, List<(ArenaCombatEffectData Effect, string Source)> entries, HashSet<ArenaCombatEffectData> visited)
    {
        if (effect == null || entries.Count >= MaxActionPatternIcons)
            return;

        if (!visited.Add(effect))
        {
            entries.Add((effect, $"{source} loop"));
            return;
        }

        entries.Add((effect, source));
        CollectActionPattern(effect.OnHitEffect, "On hit", entries, visited);
        CollectActionPattern(effect.OnExpireEffect, "On expire", entries, visited);
    }

    private static Control CreateAttackTypeBadge(ArenaCombatEffectData effect, string source)
    {
        var badge = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(48f, 62f),
            TooltipText = $"{source}: {effect.AttackTypeLabel}"
        };
        badge.AddThemeConstantOverride("separation", 2);

        var texture = string.IsNullOrWhiteSpace(effect.AttackTypeIconPath)
            ? null
            : ResourceLoader.Load<Texture2D>(effect.AttackTypeIconPath);
        badge.AddChild(new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Vector2(34f, 34f),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = badge.TooltipText
        });
        badge.AddChild(new Label
        {
            Text = AbbreviateAttackType(effect.AttackTypeLabel),
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "HeaderSmall",
            TooltipText = badge.TooltipText
        });

        return badge;
    }

    private static string AbbreviateAttackType(string label)
    {
        return label switch
        {
            "Linear Projectile" => "Linear",
            "Thrown Projectile" => "Throw",
            "Area Of Effect" => "AOE",
            _ => label
        };
    }

    private void AddArmorImmunityStats(ArmorData armor)
    {
        if (armor?.ImmuneTypes == null || armor.ImmuneTypes.Count <= 0)
            return;

        AddStat("Immune", string.Join(", ", armor.ImmuneTypes));
    }

    private static bool HasAnyDamage(CombatDamageData damage)
    {
        return damage?.Entries != null && damage.Entries.Count > 0;
    }

    private void AddArmorComparisonStack(string label, ArmorData armor, Func<int, bool> includeArmorValue)
    {
        var matchingTypes = new List<(CombatDamageType Type, int Value)>();
        foreach (CombatDamageType type in Enum.GetValues<CombatDamageType>())
        {
            var value = armor.GetArmorValue(type);
            if (includeArmorValue(value))
                matchingTypes.Add((type, value));
        }

        if (matchingTypes.Count <= 0)
            return;

        _activeStatSection ??= CreateStatSection("Armor Profile");

        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 4);
        wrapper.AddChild(new Label
        {
            Text = label,
            ThemeTypeVariation = "HeaderSmall"
        });

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        wrapper.AddChild(row);

        foreach (var (type, value) in matchingTypes)
        {
            var pill = _itemStoreDamagePillScene.Instantiate<ItemStoreDamagePill>();
            row.AddChild(pill);
            pill.Configure(type, value);
        }

        _activeStatSection.AddRow(wrapper);
    }

    private ItemStoreStatSection CreateStatSection(string title)
    {
        var section = _itemStoreStatSectionScene.Instantiate<ItemStoreStatSection>();
        AddChild(section);
        section.Configure(title);
        return section;
    }

}
