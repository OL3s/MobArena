using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class StatusEffectApplicationData : Resource
{
    [Export]
    public StatusEffectType Type { get; private set; }

    [Export(PropertyHint.Range, "0,600,1")]
    public float Value { get; private set; }

    [Export]
    public bool UseAppliedDamage { get; private set; }

    [Export(PropertyHint.Range, "0,10,0.1")]
    public float AppliedDamageMultiplier { get; private set; } = 1f;

    public float ResolveValue(int appliedDamage)
    {
        return UseAppliedDamage
            ? Mathf.Max(0, appliedDamage) * AppliedDamageMultiplier
            : Value;
    }

    public override string ToString()
    {
        var source = UseAppliedDamage
            ? $"AppliedDamage*{AppliedDamageMultiplier:0.#}"
            : $"{Value:0.#}";
        return $"{Type}({source})";
    }
}
