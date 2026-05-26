using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources.Combat;

[GlobalClass]
public partial class CombatDamageData : Resource
{
    [Export]
    public Array<CombatDamageEntryData> Entries { get; private set; } = new();

    public int GetRawTotalDamage()
    {
        var total = 0;
        foreach (var entry in Entries)
        {
            if (entry != null && entry.Damage > 0)
                total += entry.Damage;
        }

        return total;
    }

    public int GetRawDamage(ArmorDamageType type)
    {
        var total = 0;
        foreach (var entry in Entries)
        {
            if (entry?.Type == type && entry.Damage > 0)
                total += entry.Damage;
        }

        return total;
    }

    public int GetMitigatedTotalDamage(ArmorItemData armor)
    {
        return GetMitigatedTotalDamage(armor?.ArmorProfile);
    }

    public int GetMitigatedTotalDamage(ArmorData armor)
    {
        var total = 0;
        foreach (var entry in Entries)
        {
            if (entry == null)
                continue;

            total += armor?.ApplyArmorToDamage(entry) ?? entry.GetRawDamage();
        }

        return total;
    }

    public int GetMitigatedTotalDamage(GladiatorData gladiator)
    {
        return GetMitigatedTotalDamage(gladiator?.Equipment?.Armor?.ArmorProfile);
    }
}
