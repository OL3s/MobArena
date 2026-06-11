using Godot;
using Godot.Collections;
using MobArena.Scenes.Components.Arena;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaCombatApplyData : Resource
{
    [Export]
    public CombatDamageData Damage { get; private set; }

    [Export]
    public bool UseSourceItemDamage { get; private set; } = true;

    [Export(PropertyHint.Range, "0,2000,1")]
    public float ForceStrength { get; private set; }

    [Export]
    public Array<StatusEffectApplicationData> StatusApplications { get; private set; } = new();

    public CombatDamageData ResolveDamage(CombatDamageData sourceItemDamage)
    {
        if (UseSourceItemDamage && sourceItemDamage != null)
            return sourceItemDamage;

        return Damage;
    }

    public Vector2? GetForce(Vector2 direction)
    {
        if (ForceStrength <= 0f)
            return null;

        var forward = direction == Vector2.Zero ? Vector2.Right : direction.Normalized();
        return forward * ForceStrength;
    }

    public void ApplyStatus(ArenaCombatant target, ArenaCombatant source, int appliedDamage)
    {
        if (target == null || StatusApplications == null)
            return;

        foreach (var statusApplication in StatusApplications)
            target.ApplyStatusEffect(statusApplication, appliedDamage, source);
    }

    public override string ToString()
    {
        if (UseSourceItemDamage)
            return "SourceItemDamage";

        if (Damage != null)
            return Damage.ToString();

        return StatusApplications == null || StatusApplications.Count <= 0 ? "NoApply" : "StatusApply";
    }

    public string ToStringExtended()
    {
        var damageLabel = UseSourceItemDamage
            ? "SourceItemDamage"
            : Damage?.ToString() ?? "Damage=None";
        var forceLabel = ForceStrength <= 0f
            ? "Force=None"
            : $"ForceStrength={ForceStrength:0.#}";

        var statusLabel = StatusApplications == null || StatusApplications.Count <= 0
            ? "Status=None"
            : $"Status={string.Join(",", StatusApplications)}";

        return $"Apply[{damageLabel}, {forceLabel}, {statusLabel}]";
    }
}
