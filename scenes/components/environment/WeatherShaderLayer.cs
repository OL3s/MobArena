using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Environment;

public partial class WeatherShaderLayer : Control
{
    private const string ShaderIntensityParameter = "intensity";
    private const string ShaderUvScaleParameter = "uv_scale";

    [Export]
    public ShaderMaterial ClearMaterial { get; set; }

    [Export]
    public ShaderMaterial RainCloudMaterial { get; set; }

    [Export]
    public ShaderMaterial RainMaterial { get; set; }

    [Export]
    public ShaderMaterial RainSplashMaterial { get; set; }

    [Export]
    public ShaderMaterial SunMaterial { get; set; }

    [Export]
    public float ClearIntensity { get; set; } = 1f;

    [Export]
    public float RainIntensity { get; set; } = 0.6f;

    [Export]
    public float SunIntensity { get; set; } = 0.5f;

    private ColorRect _backgroundLayer;
    private ColorRect _weatherLayer;
    private ColorRect _splashLayer;

    public override void _Ready()
    {
        _backgroundLayer = GetNode<ColorRect>("BackgroundLayer");
        _weatherLayer = GetNode<ColorRect>("WeatherLayer");
        _splashLayer = GetNode<ColorRect>("SplashLayer");
        ApplyWeather(WeatherState.WeatherVisual.Cloudy);
    }

    public void ApplyWeather(WeatherState.WeatherVisual weather)
    {
        if (weather == WeatherState.WeatherVisual.Rain)
        {
            ApplyLayer(_backgroundLayer, RainCloudMaterial, RainIntensity);
            ApplyLayer(_weatherLayer, RainMaterial, RainIntensity);
            ApplyLayer(_splashLayer, RainSplashMaterial, RainIntensity);
            Visible = true;
            return;
        }

        ApplyLayer(_backgroundLayer, null, 0f);
        ApplyLayer(_splashLayer, null, 0f);

        if (weather == WeatherState.WeatherVisual.Sun)
        {
            ApplyLayer(_weatherLayer, SunMaterial, SunIntensity);
            Visible = SunMaterial != null;
            return;
        }

        ApplyLayer(_weatherLayer, ClearMaterial, ClearIntensity);
        Visible = ClearMaterial != null && ClearIntensity > 0f;
    }

    private void ApplyLayer(ColorRect layer, ShaderMaterial material, float intensity)
    {
        if (layer == null)
            return;

        layer.Material = material;
        layer.Visible = material != null && intensity > 0f;

        if (material != null)
        {
            material.SetShaderParameter(ShaderIntensityParameter, intensity);
            material.SetShaderParameter(ShaderUvScaleParameter, GetLayerUvScale(layer));
        }
    }

    private Vector2 GetLayerUvScale(Control layer)
    {
        var viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f || layer == null)
            return Vector2.One;

        return new Vector2(
            Mathf.Max(layer.Size.X / viewportSize.X, 1f),
            Mathf.Max(layer.Size.Y / viewportSize.Y, 1f));
    }
}
