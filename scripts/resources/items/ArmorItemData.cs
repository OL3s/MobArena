using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ArmorItemData : ItemData
{
    [Export]
    public ArmorData ArmorProfile { get; private set; }

    public int GetArmorValue(ArmorDamageType type)
    {
        return ArmorProfile?.GetArmorValue(type) ?? 0;
    }

    public int ApplyArmorToDamage(int damage, ArmorDamageType type)
    {
        return ArmorProfile?.ApplyArmorToDamage(damage, type) ?? ArmorData.ApplyArmorToDamage(damage, 0);
    }

    public int ApplyArmorToDamage(CombatDamageEntryData damageEntry)
    {
        return ArmorProfile?.ApplyArmorToDamage(damageEntry) ?? damageEntry?.GetRawDamage() ?? 0;
    }

    public int ApplyArmorToDamage(CombatDamageData damageData)
    {
        return ArmorProfile?.ApplyArmorToDamage(damageData) ?? damageData?.GetRawTotalDamage() ?? 0;
    }

    public static int ApplyArmorToDamage(int damage, int armor)
    {
        return ArmorData.ApplyArmorToDamage(damage, armor);
    }

    public int GetSpecialtyValue(ArmorSpecialType type)
    {
        return ArmorProfile?.GetSpecialtyValue(type) ?? 0;
    }
}
