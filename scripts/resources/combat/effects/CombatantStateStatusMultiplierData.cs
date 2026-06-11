using Godot;
using MobArena.Scenes.Components.Arena;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class CombatantStateStatusMultiplierData : Resource
{
    [Export]
    public ArenaCombatantState State { get; private set; } = ArenaCombatantState.Default;

    [Export]
    public StatusEffectType Type { get; private set; }

    [Export(PropertyHint.Range, "0,10,0.05")]
    public float Multiplier { get; private set; } = 1f;
}
