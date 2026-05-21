using Godot;

namespace MobArena.Scripts.Resources;

public partial class WeatherState : Resource
{
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

        CurrentWeather = weather;
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
}
