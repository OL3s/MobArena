using Godot;

namespace MobArena.Scripts.Resources.Combat.Actions;

[GlobalClass]
public partial class ArenaCombatBuildupData : Resource
{
    public const float MinScalar = 0.1f;
    public const float MaxScalar = 1f;

    [Export(PropertyHint.Range, "0.05,10,0.01")]
    public float BuildupSeconds { get; private set; } = 1f;

    [Export]
    public bool ScaleDamage { get; private set; }

    [Export]
    public bool ScaleRange { get; private set; }

    [Export]
    public bool ScaleSpeed { get; private set; }

    public float GetScalar(float elapsedSeconds)
    {
        var progress = Mathf.Clamp(elapsedSeconds / Mathf.Max(0.05f, BuildupSeconds), 0f, 1f);
        return Mathf.Lerp(MinScalar, MaxScalar, progress);
    }

    public override string ToString()
    {
        return $"Buildup {BuildupSeconds:0.##}s";
    }

    public string ToStringExtended()
    {
        return $"Buildup[Seconds={BuildupSeconds:0.##}, Damage={ScaleDamage}, Range={ScaleRange}, Speed={ScaleSpeed}]";
    }
}
