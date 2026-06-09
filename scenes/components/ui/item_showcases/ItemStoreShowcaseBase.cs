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
    private const string ArenaMeleeHitboxScenePath = "res://scenes/components/arena/combat/effects/ArenaMeleeHitbox.tscn";

    private static readonly Dictionary<string, string> EffectSceneLabels = new()
    {
        [ArenaMeleeHitboxScenePath] = "Melee Hitbox"
    };

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

        if (action.Effect == null)
            return;

        BeginStatSection("Effect");
		AddStat("Scene", GetEffectSceneLabel(action.Effect.ScenePath));
        AddStat("Effect Lifetime", $"{action.Effect.LifetimeSeconds:0.##}s");
        AddStat("Max Hits", action.Effect.MaxHits.ToString());
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

	private static string GetEffectSceneLabel(string scenePath)
	{
		if (string.IsNullOrWhiteSpace(scenePath))
			return "None";

		return EffectSceneLabels.TryGetValue(scenePath, out var label) ? label : scenePath;
	}
}
