using Godot;
using MobArena.Scenes.Components.Environment;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class Arena : Node
{
    private const string TownScene = "res://scenes/town.tscn";

    private EnvironmentVisualOverlay _environmentOverlay;
    private WeatherState _weatherState;

    public override void _Ready()
    {
        var saveNode = SaveNode.Get();
        _environmentOverlay = GetNodeOrNull<EnvironmentVisualOverlay>("EnvironmentOverlay");
        _weatherState = saveNode?.WeatherState;

        if (_weatherState != null)
            _weatherState.WeatherChanged += RefreshWeatherVisuals;

        var returnButton = GetNode<Button>("ControllerUi/StatusPanel/Row/ReturnButton");
        returnButton.Pressed += OnReturnToTownPressed;

        RefreshWeatherVisuals();
        returnButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    public override void _ExitTree()
    {
        if (_weatherState != null)
            _weatherState.WeatherChanged -= RefreshWeatherVisuals;
    }

    private void OnReturnToTownPressed()
    {
        var saveNode = SaveNode.Get();
        PhaseTransitionController.CompleteArenaContract(saveNode.TownPhaseState, saveNode.CompanyRunData, saveNode.WeatherState);
        saveNode.Save();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, TownScene);
    }

    private void RefreshWeatherVisuals()
    {
        _environmentOverlay?.SetWeather(_weatherState?.CurrentWeather ?? WeatherState.WeatherVisual.Clear);
    }
}
