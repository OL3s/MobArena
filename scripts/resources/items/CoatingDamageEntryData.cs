using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class CoatingDamageEntryData : Resource
{
    [Export]
    public CombatDamageType Type { get; private set; } = CombatDamageType.Slash;

    [Export]
    public int Value { get; private set; }
}
