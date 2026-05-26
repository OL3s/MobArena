using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources.Combat;

[GlobalClass]
public partial class CombatDamageEntryData : Resource
{
    [Export]
    public ArmorDamageType Type { get; private set; } = ArmorDamageType.Slash;

    [Export]
    public int Damage { get; private set; }

    public int GetRawDamage()
    {
        return Mathf.Max(0, Damage);
    }

    public int GetMitigatedDamage(ArmorItemData armor)
    {
        return armor?.ApplyArmorToDamage(this) ?? GetRawDamage();
    }
}
