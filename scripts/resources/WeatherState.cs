using Godot;

namespace MobArena.Scripts.Resources;

public partial class WeatherState : Resource
{
    public enum WeatherVisual
    {
        Clear,
        Sun,
        Rain
    }

    [Signal]
    public delegate void WeatherChangedEventHandler();

    [Export]
    public WeatherVisual CurrentWeather { get; private set; } = WeatherVisual.Clear;

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
        var weatherValues = System.Enum.GetValues<WeatherVisual>();
        SetWeather(weatherValues[_random.RandiRange(0, weatherValues.Length - 1)]);
    }
}
