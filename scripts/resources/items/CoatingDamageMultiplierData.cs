using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class CoatingDamageMultiplierData : Resource
{
    [Export]
    public CombatDamageType Type { get; private set; } = CombatDamageType.Slash;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float Multiplier { get; private set; } = 1f;
}
