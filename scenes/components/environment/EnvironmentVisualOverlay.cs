using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Environment;

public partial class EnvironmentVisualOverlay : CanvasLayer
{
    public enum TimeOfDayVisual
    {
        Day,
        Night
    }

    [Export]
    public float NightOpacity { get; set; } = 0.42f;

    [Export]
    public float WeatherOpacity { get; set; } = 0.16f;

    [Export]
    public TimeOfDayVisual TimeOfDay { get; private set; } = TimeOfDayVisual.Day;

    [Export]
    public WeatherState.WeatherVisual Weather { get; private set; } = WeatherState.WeatherVisual.Clear;

    private ColorRect _timeTint;
    private ColorRect _weatherTint;
    private WeatherShaderLayer _weatherShaderLayer;

    public override void _Ready()
    {
        _timeTint = GetNode<ColorRect>("TimeTint");
        _weatherTint = GetNode<ColorRect>("WeatherTint");
        _weatherShaderLayer = GetNodeOrNull<WeatherShaderLayer>("WeatherShaderLayer");
        RefreshVisuals();
    }

    public void ApplyPhaseState(TownPhaseState phaseState)
    {
        SetTimeOfDay(phaseState?.IsNight() == true ? TimeOfDayVisual.Night : TimeOfDayVisual.Day);
    }

    public void SetTimeOfDay(TimeOfDayVisual timeOfDay)
    {
        if (TimeOfDay == timeOfDay)
            return;

        TimeOfDay = timeOfDay;
        RefreshVisuals();
    }

    public void SetWeather(WeatherState.WeatherVisual weather)
    {
        if (Weather == weather)
            return;

        Weather = weather;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (_timeTint == null || _weatherTint == null)
            return;

        _timeTint.Color = TimeOfDay == TimeOfDayVisual.Night
            ? new Color(0.02f, 0.035f, 0.12f, Mathf.Clamp(NightOpacity, 0f, 1f))
            : Colors.Transparent;
        _timeTint.Visible = _timeTint.Color.A > 0f;

        _weatherTint.Color = Weather switch
        {
            WeatherState.WeatherVisual.Rain => new Color(0.15f, 0.22f, 0.32f, Mathf.Clamp(WeatherOpacity, 0f, 1f)),
            WeatherState.WeatherVisual.Sun => new Color(1f, 0.74f, 0.25f, Mathf.Clamp(WeatherOpacity * 0.7f, 0f, 1f)),
            _ => Colors.Transparent
        };
        _weatherTint.Visible = _weatherTint.Color.A > 0f;
        _weatherShaderLayer?.ApplyWeather(Weather);
    }
}
