using Godot;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Items;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.UI;

public static class CombatIconRegistry
{
    private const string AdditiveCoatingIconPath = "res://assets/ui/coatings/type_additive.svg";
    private const string MultiplierCoatingIconPath = "res://assets/ui/coatings/type_multiplier.svg";

    private static readonly Dictionary<CombatDamageType, string> InstantIcons = new()
    {
        [CombatDamageType.Slash] = "res://assets/ui/combat/instant/type_slash.svg",
        [CombatDamageType.Pierce] = "res://assets/ui/combat/instant/type_pierce.svg",
        [CombatDamageType.Crush] = "res://assets/ui/combat/instant/type_crush.svg",
        [CombatDamageType.Heat] = "res://assets/ui/combat/instant/type_heat.svg",
        [CombatDamageType.Cold] = "res://assets/ui/combat/instant/type_cold.svg",
        [CombatDamageType.Acid] = "res://assets/ui/combat/instant/type_acid.svg",
        [CombatDamageType.Silver] = "res://assets/ui/combat/instant/type_silver.svg",
        [CombatDamageType.Holy] = "res://assets/ui/combat/instant/type_holy.svg"
    };

    private static readonly Dictionary<StatusEffectType, string> EffectIcons = new()
    {
        [StatusEffectType.Poison] = "res://assets/ui/combat/effects/status_poison.svg",
        [StatusEffectType.Stun] = "res://assets/ui/combat/effects/status_stun.svg"
    };

    public static Texture2D LoadInstantIcon(CombatDamageType type)
    {
        return UiIconLoader.LoadIcon(InstantIcons.GetValueOrDefault(type, UiIconLoader.FallbackIconPath));
    }

    public static Texture2D LoadEffectIcon(StatusEffectType type)
    {
        return UiIconLoader.LoadIcon(EffectIcons.GetValueOrDefault(type, UiIconLoader.FallbackIconPath));
    }

    public static Texture2D LoadCoatingBranchIcon(ItemCoatingData coating)
    {
        var path = coating switch
        {
            AdditiveItemCoatingData => AdditiveCoatingIconPath,
            MultiplierItemCoatingData => MultiplierCoatingIconPath,
            _ => UiIconLoader.FallbackIconPath
        };

        return UiIconLoader.LoadIcon(path);
    }

    public static Texture2D LoadPrimaryCoatingPayloadIcon(ItemCoatingData coating)
    {
        return coating switch
        {
            AdditiveItemCoatingData additive => LoadAdditivePayloadIcon(additive),
            MultiplierItemCoatingData multiplier when multiplier.DamageMultipliers.Count > 0 && multiplier.DamageMultipliers[0] != null
                => LoadInstantIcon(multiplier.DamageMultipliers[0].Type),
            _ => UiIconLoader.LoadFallbackIcon()
        };
    }

    private static Texture2D LoadAdditivePayloadIcon(AdditiveItemCoatingData coating)
    {
        if (coating?.EffectEntries?.Count > 0 && coating.EffectEntries[0] != null)
            return LoadEffectIcon(coating.EffectEntries[0].Type);

        if (coating?.DamageEntries?.Count > 0 && coating.DamageEntries[0] != null)
            return LoadInstantIcon(coating.DamageEntries[0].Type);

        return UiIconLoader.LoadFallbackIcon();
    }
}
