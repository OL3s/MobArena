using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources.Combat;

[GlobalClass]
public partial class CombatDamageEntryData : Resource
{
    [Export]
    public CombatDamageType Type { get; private set; } = CombatDamageType.Slash;

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

    public override string ToString()
    {
        return $"{Type}:{GetRawDamage()}";
    }
}
