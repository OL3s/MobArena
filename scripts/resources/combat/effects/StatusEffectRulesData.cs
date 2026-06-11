using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class StatusEffectRulesData : Resource
{
    [Export]
    public float BaseMinValue { get; private set; } = 1f;

    [Export]
    public Array<StatusEffectValueOverrideData> MinValueOverrides { get; private set; } = new();

    [Export]
    public float BaseMaxValue { get; private set; } = 300f;

    [Export]
    public Array<StatusEffectValueOverrideData> MaxValueOverrides { get; private set; } = new();

    public float GetMinValue(StatusEffectType type)
    {
        return Mathf.Max(0f, GetValue(type, BaseMinValue, MinValueOverrides));
    }

    public float GetMaxValue(StatusEffectType type)
    {
        return Mathf.Max(0f, GetValue(type, BaseMaxValue, MaxValueOverrides));
    }

    private static float GetValue(StatusEffectType type, float baseValue, Array<StatusEffectValueOverrideData> overrides)
    {
        if (overrides == null)
            return baseValue;

        foreach (var valueOverride in overrides)
        {
            if (valueOverride?.Type == type)
                return valueOverride.Value;
        }

        return baseValue;
    }
}
