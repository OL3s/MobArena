using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Environment;

public partial class WeatherShaderLayer : ColorRect
{
    private const string ShaderIntensityParameter = "intensity";

    [Export]
    public ShaderMaterial ClearMaterial { get; set; }

    [Export]
    public ShaderMaterial RainMaterial { get; set; }

    [Export]
    public ShaderMaterial SunMaterial { get; set; }

    [Export]
    public float RainIntensity { get; set; } = 0.6f;

    [Export]
    public float SunIntensity { get; set; } = 0.5f;

    public override void _Ready()
    {
        ApplyWeather(WeatherState.WeatherVisual.Clear);
    }

    public void ApplyWeather(WeatherState.WeatherVisual weather)
    {
        var material = weather switch
        {
            WeatherState.WeatherVisual.Rain => RainMaterial,
            WeatherState.WeatherVisual.Sun => SunMaterial,
            _ => ClearMaterial
        };

        Material = material;
        Visible = weather != WeatherState.WeatherVisual.Clear && material != null;

        if (material == null)
            return;

        if (weather == WeatherState.WeatherVisual.Rain)
            material.SetShaderParameter(ShaderIntensityParameter, RainIntensity);
        else if (weather == WeatherState.WeatherVisual.Sun)
            material.SetShaderParameter(ShaderIntensityParameter, SunIntensity);
    }
}
