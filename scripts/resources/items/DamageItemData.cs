using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scripts.Resources.Items;

public abstract partial class DamageItemData : ItemData
{
    [Export]
    public CombatDamageData Damage { get; private set; }
}
