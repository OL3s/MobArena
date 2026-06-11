using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Items;
using System.Text;

namespace MobArena.Scripts.Resources.Combat;

[GlobalClass]
public partial class CombatDamageData : Resource
{
    [Export]
    public Array<CombatDamageEntryData> Entries { get; private set; } = new();

    public int GetRawTotalDamage()
    {
        var total = 0;
        foreach (var entry in Entries ?? new Array<CombatDamageEntryData>())
        {
            if (entry != null && entry.Damage > 0)
                total += entry.Damage;
        }

        return total;
    }

    public int GetRawDamage(CombatDamageType type)
    {
        var total = 0;
        foreach (var entry in Entries ?? new Array<CombatDamageEntryData>())
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
        foreach (var entry in Entries ?? new Array<CombatDamageEntryData>())
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

    public override string ToString()
    {
        var hasEntries = Entries != null && Entries.Count > 0;
        if (!hasEntries)
            return "Damage=None";

        var builder = new StringBuilder("Damage=");
        var appended = false;
        foreach (var entry in Entries ?? new Array<CombatDamageEntryData>())
        {
            if (entry == null)
                continue;

            if (appended)
                builder.Append("+");

            builder.Append(entry);
            appended = true;
        }

        if (!appended)
            builder.Append("None");

        builder.Append($" Total:{GetRawTotalDamage()}");
        return builder.ToString();
    }
}
