using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ItemLevelMultiplierData : Resource
{
    [Export]
    public GladiatorLevelData.AttributeKind Attribute { get; private set; } = GladiatorLevelData.AttributeKind.Strength;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MultiplierPerLevel { get; private set; } = 0.03f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float StrengthPowerPerLevel { get; private set; } = 0.03f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AgilitySpeedPerLevel { get; private set; } = 0.03f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float StrengthInfluence { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AgilityInfluence { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,10,0.01")]
    public float BaseMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,10,0.01")]
    public float MaxMultiplier { get; private set; } = 2f;

    [Export(PropertyHint.Range, "0,10,0.01")]
    public float BaseSpeedMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,10,0.01")]
    public float MaxSpeedMultiplier { get; private set; } = 2f;

    public float GetMultiplier(GladiatorLevelData levels)
    {
        if (levels == null)
            return BaseMultiplier;

        var attributeLevel = levels.GetAttributeLevel(Attribute);
        return Mathf.Min(MaxMultiplier, BaseMultiplier + Mathf.Max(0, attributeLevel - 1) * MultiplierPerLevel);
    }

    public float GetStrengthPowerMultiplier(GladiatorLevelData levels)
    {
        if (levels == null)
            return BaseMultiplier;

        var levelBonus = Mathf.Max(0, levels.Strength - 1) * StrengthPowerPerLevel * StrengthInfluence;
        return Mathf.Min(MaxMultiplier, BaseMultiplier + levelBonus);
    }

    public float GetAgilitySpeedMultiplier(GladiatorLevelData levels)
    {
        if (levels == null)
            return BaseSpeedMultiplier;

        var levelBonus = Mathf.Max(0, levels.Agility - 1) * AgilitySpeedPerLevel * AgilityInfluence;
        return Mathf.Min(MaxSpeedMultiplier, BaseSpeedMultiplier + levelBonus);
    }
}
