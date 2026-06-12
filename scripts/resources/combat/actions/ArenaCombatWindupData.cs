using Godot;

namespace MobArena.Scripts.Resources.Combat.Actions;

[GlobalClass]
public partial class ArenaCombatWindupData : Resource
{
    public const float MinScalar = 0.1f;
    public const float MaxScalar = 1f;

    [Export(PropertyHint.Range, "0.05,10,0.01")]
    public float WindupSeconds { get; private set; } = 1f;

    [Export]
    public bool ScaleDamage { get; private set; }

    [Export]
    public bool ScaleRange { get; private set; }

    [Export]
    public bool ScaleSpeed { get; private set; }

    [Export]
    public bool CanReleaseEarly { get; private set; } = true;

    public float GetScalar(float elapsedSeconds)
    {
        return GetScalar(elapsedSeconds, WindupSeconds);
    }

    public float GetScalar(float elapsedSeconds, float maxSeconds)
    {
        var progress = Mathf.Clamp(elapsedSeconds / Mathf.Max(0.05f, maxSeconds), 0f, 1f);
        return Mathf.Lerp(MinScalar, MaxScalar, progress);
    }

    public override string ToString()
    {
        return $"Windup {WindupSeconds:0.##}s";
    }

    public string ToStringExtended()
    {
        return $"Windup[Seconds={WindupSeconds:0.##}, Damage={ScaleDamage}, Range={ScaleRange}, Speed={ScaleSpeed}, EarlyRelease={CanReleaseEarly}]";
    }
}
