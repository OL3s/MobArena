using Godot;
using MobArena.Scenes.Components.Environment;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.Components.Town;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class Town : Node
{
    private const string MainMenuScene = "res://scenes/main_menu.tscn";
    private const string GladiatorsOverlayScene = "res://scenes/ui/GladiatorsOverlay.tscn";
    private const string EquipmentInventoryOverlayScene = "res://scenes/ui/EquipmentInventoryOverlay.tscn";
    private const string GladiatorMarketOverlayScene = "res://scenes/town_overlays/gladiator_market_overlay.tscn";
    private const string FirstTownEntryPopupTitle = "Tutorial";
    private const string FirstTownEntryPopupText = "Todo, add tutorial with tscn animation popups here";

    private TownBuilding _contractBoard;
    private EnvironmentVisualOverlay _environmentOverlay;
    private TownHud _townHud;
    private TownPhaseState _phaseState;
    private WeatherState _weatherState;

    public override void _Ready()
    {
        _contractBoard = GetNode<TownBuilding>("World/ContractBoard");
        _environmentOverlay = GetNode<EnvironmentVisualOverlay>("EnvironmentOverlay");
        var saveNode = SaveNode.Get();
        _phaseState = saveNode?.TownPhaseState;
        _weatherState = saveNode?.WeatherState;
        if (_phaseState != null)
            _phaseState.PhaseChanged += OnPhaseChanged;

        if (_weatherState != null)
            _weatherState.WeatherChanged += RefreshWeatherVisuals;

        _townHud = GetNode<TownHud>("TownHud");
        _townHud.BackPressed += OnMainMenuPressed;
        _townHud.SelectContractPressed += OnSelectContractPressed;
        _townHud.BuyGladiatorPressed += OnBuyGladiatorPressed;
        GetNode<Button>("World/RosterYard/ButtonRow/GladiatorsButton").Pressed += OnGladiatorsPressed;
        GetNode<Button>("World/RosterYard/ButtonRow/EquipmentButton").Pressed += OnEquipmentPressed;
        RefreshEnvironmentVisuals();
        RefreshWeatherVisuals();
        CallDeferred(MethodName.ShowFirstTownEntryPopupIfNeeded);
    }

    public override void _ExitTree()
    {
        if (_phaseState != null)
            _phaseState.PhaseChanged -= OnPhaseChanged;

        if (_weatherState != null)
            _weatherState.WeatherChanged -= RefreshWeatherVisuals;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed("ui_accept"))
            return;

        GetViewport()?.SetInputAsHandled();
        _contractBoard?.Activate();
    }

    private void OnMainMenuPressed()
    {
        SaveNode.Get()?.Save();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
    }

    private void OnSelectContractPressed()
    {
        _contractBoard?.Activate();
    }

    private static void OnBuyGladiatorPressed()
    {
        OpenOverlay(GladiatorMarketOverlayScene);
    }

    private static void OnGladiatorsPressed()
    {
        OpenOverlay(GladiatorsOverlayScene);
    }

    private static void OnEquipmentPressed()
    {
        OpenOverlay(EquipmentInventoryOverlayScene);
    }

    private static void OpenOverlay(string scenePath)
    {
        var globalOverlay = GlobalOverlay.Get();
        var overlayScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (globalOverlay == null || overlayScene == null)
            return;

        globalOverlay.AddOverlay(overlayScene);
    }

    private static void ShowFirstTownEntryPopupIfNeeded()
    {
        var saveNode = SaveNode.Get();
        var runData = saveNode.CompanyRunData;
        if (runData == null || runData.HasShownFirstTownEntryPopup)
            return;

        runData.MarkFirstTownEntryPopupShown();
        saveNode.Save();
        GlobalOverlay.Get()?.ShowBlurredPopup(FirstTownEntryPopupTitle, FirstTownEntryPopupText);
    }

    private void RefreshEnvironmentVisuals()
    {
        _environmentOverlay?.ApplyPhaseState(_phaseState);
    }

    private void OnPhaseChanged()
    {
        RefreshEnvironmentVisuals();
        RefreshWeatherVisuals();
    }

    private void RefreshWeatherVisuals()
    {
        var weather = _weatherState?.CurrentWeather ?? WeatherState.WeatherVisual.Clear;
        _environmentOverlay?.SetWeather(weather);
        _townHud?.SetWeatherVisual(weather);
    }
}
