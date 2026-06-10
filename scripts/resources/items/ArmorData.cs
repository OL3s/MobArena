using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ArmorData : Resource
{
    [Export]
    public int BaseValue { get; private set; }

    [Export]
    public Array<ArmorTypeOverrideData> TypeOverrides { get; private set; } = new();

    [Export]
    public Array<CombatDamageType> ImmuneTypes { get; private set; } = new()
    {
        CombatDamageType.Silver,
        CombatDamageType.Holy
    };

    public int GetArmorValue(CombatDamageType type)
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

    public bool IsImmuneTo(CombatDamageType type)
    {
        if (ImmuneTypes == null)
            return false;

        foreach (var immuneType in ImmuneTypes)
        {
            if (immuneType == type)
                return true;
        }

        return false;
    }

    public int ApplyArmorToDamage(int damage, CombatDamageType type)
    {
        if (IsImmuneTo(type))
            return 0;

        return ApplyArmorToDamage(damage, GetArmorValue(type));
    }

    public int ApplyArmorToDamage(CombatDamageEntryData damageEntry)
    {
        return damageEntry == null
            ? 0
            : ApplyArmorToDamage(damageEntry.Damage, damageEntry.Type);
    }

    public int ApplyArmorToDamage(CombatDamageData damageData)
    {
        return damageData?.GetMitigatedTotalDamage(this) ?? 0;
    }

    public static int ApplyArmorToDamage(int damage, int armor)
    {
        if (damage <= 0)
            return 0;

        if (armor == 0)
            return damage;

        if (armor < 0)
        {
            var vulnerableDamage = damage * (1f + Mathf.Abs(armor) / 100f);
            return Mathf.Max(1, Mathf.RoundToInt(vulnerableDamage));
        }

        var mitigatedDamage = damage * (damage / (float)(damage + armor));
        return Mathf.Max(1, Mathf.RoundToInt(mitigatedDamage));
    }

}
