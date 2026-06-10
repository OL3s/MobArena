using Godot;

namespace MobArena.Scripts.Resources;

public partial class WeatherEffectConfig : Resource
{
    [Export]
    public float RecoveryMultiplier { get; set; } = 1f;

    [Export]
    public float TrainingMultiplier { get; set; } = 1f;

    [Export]
    public float CostMultiplier { get; set; } = 1f;

    public static WeatherEffectConfig Create(float recoveryMultiplier, float trainingMultiplier, float costMultiplier)
    {
        return new WeatherEffectConfig
        {
            RecoveryMultiplier = recoveryMultiplier,
            TrainingMultiplier = trainingMultiplier,
            CostMultiplier = costMultiplier
        };
    }
}
