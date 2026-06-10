using Godot;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scripts.Resources.Combat.Actions;

[GlobalClass]
public partial class ArenaCombatActionData : Resource
{
    public const int DefaultMaxChainDepth = 12;

    [Export]
    public string DisplayName { get; private set; } = "Attack";

    [Export]
    public ArenaCombatEffectData Effect { get; private set; }

    [Export]
    public ArenaCombatBuildupData Buildup { get; private set; }

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float WindupSeconds { get; private set; }

    [Export(PropertyHint.Range, "0,100,1")]
    public int StaminaCost { get; private set; }

    [Export(PropertyHint.Range, "0,200,1")]
    public float SpawnDistance { get; private set; } = 32f;

    [Export(PropertyHint.Range, "0,64,1")]
    public int MaxChainDepth { get; private set; } = DefaultMaxChainDepth;

    public override string ToString()
    {
        var effectLabel = Effect == null ? "Effect=None" : Effect.ToString();
        var buildupLabel = Buildup == null ? "Buildup=None" : Buildup.ToString();
        return $"Action[{DisplayName}, Windup={WindupSeconds:0.##}, Stamina={StaminaCost}, Spawn={SpawnDistance:0.#}, MaxChainDepth={MaxChainDepth}, {buildupLabel}, {effectLabel}]";
    }
}
