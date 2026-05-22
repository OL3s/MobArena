using Godot;
using MobArena.Scenes.Components.Environment;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Contracts;

namespace MobArena.Scripts;

public partial class Arena : Node
{
    private const string TownScene = "res://scenes/town.tscn";
    private const string MainMenuScene = "res://scenes/main_menu.tscn";

    private SaveNode _saveNode;
    private EnvironmentVisualOverlay _environmentOverlay;
    private WeatherState _weatherState;
    private CompanyRunData _runData;
    private TownPhaseState _phaseState;
    private Button _debugWinButton;
    private Button _debugLoseButton;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _environmentOverlay = GetNodeOrNull<EnvironmentVisualOverlay>("EnvironmentOverlay");
        _weatherState = _saveNode?.WeatherState;
        _runData = _saveNode?.CompanyRunData;
        _phaseState = _saveNode?.TownPhaseState;

        if (_weatherState != null)
            _weatherState.WeatherChanged += RefreshWeatherVisuals;

        var returnButton = GetNode<Button>("ControllerUi/StatusPanel/Row/ReturnButton");
        _debugWinButton = GetNode<Button>("ControllerUi/StatusPanel/Row/DebugWinButton");
        _debugLoseButton = GetNode<Button>("ControllerUi/StatusPanel/Row/DebugLoseButton");
        returnButton.Pressed += ResolveContractForfeit;
        _debugWinButton.Pressed += ResolveContractWin;
        _debugLoseButton.Pressed += ResolveContractLoss;

        RefreshDebugButtons();
        RefreshWeatherVisuals();
        returnButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    public override void _ExitTree()
    {
        if (_weatherState != null)
            _weatherState.WeatherChanged -= RefreshWeatherVisuals;
    }

    public void ResolveContractWin()
    {
        if (ArenaContractResultResolver.ResolveWin(_saveNode) != ArenaContractResultResolver.ContractResult.Completed)
            return;

        SaveAndReturnToTown();
    }

    public void ResolveContractLoss()
    {
        var result = ArenaContractResultResolver.ResolveLoss(_saveNode);
        if (result == ArenaContractResultResolver.ContractResult.ForceRetired)
        {
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
            return;
        }

        if (result == ArenaContractResultResolver.ContractResult.Completed)
            SaveAndReturnToTown();
    }

    public void ResolveContractForfeit()
    {
        if (ArenaContractResultResolver.ResolveForfeit(_saveNode) == ArenaContractResultResolver.ContractResult.Completed)
            SaveAndReturnToTown();
    }

    private void SaveAndReturnToTown()
    {
        _saveNode?.Save();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, TownScene);
    }

    private void RefreshDebugButtons()
    {
        var visible = _saveNode?.DebugEnabled == true;
        _debugWinButton.Visible = visible;
        _debugLoseButton.Visible = visible;
        _debugLoseButton.Text = _runData?.ActiveArenaContract?.IsChampionContract() == true
            ? "Debug Lose"
            : "Debug Forfeit";
    }

    private void RefreshWeatherVisuals()
    {
        _environmentOverlay?.SetWeather(_weatherState?.CurrentWeather ?? WeatherState.WeatherVisual.Cloudy);
    }
}
