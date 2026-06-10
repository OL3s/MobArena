using Godot;

namespace MobArena.Scripts.Resources;

public partial class WeatherState : Resource
{
    public const float NeutralMultiplier = 1f;
    public const float SunRecoveryMultiplier = 0.75f;
    public const float RainTrainingMultiplier = 0.75f;
    public const float RainCostMultiplier = 1.25f;

    public enum WeatherVisual
    {
        Cloudy,
        Sun,
        Rain
    }

    [Signal]
    public delegate void WeatherChangedEventHandler();

    [Export]
    public WeatherVisual CurrentWeather { get; private set; } = WeatherVisual.Cloudy;

    private readonly RandomNumberGenerator _random = new();

    public WeatherState()
    {
        _random.Randomize();
    }

    public void SetWeather(WeatherVisual weather)
    {
        if (CurrentWeather == weather)
            return;

        var previousWeather = CurrentWeather;
        CurrentWeather = weather;
        GD.Print($"WeatherState: Weather changed from {previousWeather} to {CurrentWeather}.");
        EmitSignal(SignalName.WeatherChanged);
    }

    public void ChooseRandomWeather()
    {
        ChooseRandomWeather(null);
    }

    public void ChooseRandomWeather(TownPhaseState phaseState)
    {
        var roll = _random.Randf();
        if (phaseState?.IsNight() == true)
        {
            SetWeather(roll < 0.85f ? WeatherVisual.Cloudy : WeatherVisual.Rain);
            return;
        }

        SetWeather(roll switch
        {
            < 0.5f => WeatherVisual.Cloudy,
            < 0.85f => WeatherVisual.Sun,
            _ => WeatherVisual.Rain
        });
    }

    public WeatherEffectConfig GetCurrentEffectConfig()
    {
        return CurrentWeather switch
        {
            WeatherVisual.Sun => WeatherEffectConfig.Create(SunRecoveryMultiplier, NeutralMultiplier, NeutralMultiplier),
            WeatherVisual.Rain => WeatherEffectConfig.Create(NeutralMultiplier, RainTrainingMultiplier, RainCostMultiplier),
            _ => WeatherEffectConfig.Create(NeutralMultiplier, NeutralMultiplier, NeutralMultiplier)
        };
    }

    public string GetCurrentEffectSummary()
    {
        return CurrentWeather switch
        {
            WeatherVisual.Sun => "Sun: recovery -25%",
            WeatherVisual.Rain => "Rain: training -25%, Thermae/Training Hall costs +25%",
            _ => "Cloudy: normal recovery, training, and costs"
        };
    }
}
