using Godot;
using Godot.Collections;
using MobArena.Scenes.Components.Arena;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class CombatantStatusProfileData : Resource
{
    [Export]
    public EffectDefenseData EffectDefenseProfile { get; private set; }

    [Export]
    public StatusEffectRulesData StatusRules { get; private set; }

    [Export]
    public Array<StatusEffectType> ImmuneStatuses { get; private set; } = new();

    [Export]
    public Array<CombatantStateStatusMultiplierData> StateStatusMultipliers { get; private set; } = new();

    public float ApplyDefenseToEffect(float value, StatusEffectType type)
    {
        if (IsImmuneTo(type))
            return 0f;

        return (EffectDefenseProfile ?? new EffectDefenseData()).ApplyDefenseToEffect(value, type);
    }

    public float GetMinValue(StatusEffectType type)
    {
        return (StatusRules ?? new StatusEffectRulesData()).GetMinValue(type);
    }

    public float GetMaxValue(StatusEffectType type)
    {
        return (StatusRules ?? new StatusEffectRulesData()).GetMaxValue(type);
    }

    public float GetStateMultiplier(ArenaCombatantState state, StatusEffectType type)
    {
        if (StateStatusMultipliers == null)
            return 1f;

        foreach (var multiplier in StateStatusMultipliers)
        {
            if (multiplier?.State == state && multiplier.Type == type)
                return Mathf.Max(0f, multiplier.Multiplier);
        }

        return 1f;
    }

    public bool IsImmuneTo(StatusEffectType type)
    {
        if (ImmuneStatuses == null)
            return false;

        foreach (var immuneStatus in ImmuneStatuses)
        {
            if (immuneStatus == type)
                return true;
        }

        return false;
    }
}
