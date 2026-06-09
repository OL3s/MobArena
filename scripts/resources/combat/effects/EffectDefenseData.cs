using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class EffectDefenseData : Resource
{
    [Export]
    public int BaseValue { get; private set; }

    [Export]
    public Array<EffectDefenseTypeOverrideData> TypeOverrides { get; private set; } = new();

    public int GetDefenseValue(StatusEffectType type)
    {
        if (TypeOverrides == null)
            return BaseValue;

        foreach (var typeOverride in TypeOverrides)
        {
            if (typeOverride?.Type == type)
                return typeOverride.Value;
        }

        return BaseValue;
    }

    public float ApplyDefenseToEffect(float value, StatusEffectType type)
    {
        return ApplyDefenseToEffect(value, GetDefenseValue(type));
    }

    public static float ApplyDefenseToEffect(float value, int defense)
    {
        if (value <= 0f)
            return 0f;

        if (defense == 0)
            return value;

        if (defense < 0)
            return Mathf.Max(0.1f, value * (1f + Mathf.Abs(defense) / 100f));

        var mitigated = value * (value / (value + defense));
        return Mathf.Max(0.1f, mitigated);
    }
}
