using Godot;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Actions;

namespace MobArena.Scripts.Resources.Items;

public abstract partial class DamageItemData : EquipmentItemData
{
    [Export]
    public CombatDamageData Damage { get; private set; }

    [Export]
    public ArenaCombatActionData MainAction { get; private set; }
}
