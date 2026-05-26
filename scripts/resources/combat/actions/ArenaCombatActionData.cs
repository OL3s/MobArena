using Godot;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scripts.Resources.Combat.Actions;

[GlobalClass]
public partial class ArenaCombatActionData : Resource
{
    [Export]
    public string DisplayName { get; private set; } = "Attack";

    [Export]
    public ArenaCombatEffectData Effect { get; private set; }

    [Export(PropertyHint.Range, "0,10,0.01")]
    public float CooldownSeconds { get; private set; } = 0.6f;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float WindupSeconds { get; private set; }

    [Export(PropertyHint.Range, "0,100,1")]
    public int StaminaCost { get; private set; }

    [Export(PropertyHint.Range, "0,200,1")]
    public float SpawnDistance { get; private set; } = 32f;
}
