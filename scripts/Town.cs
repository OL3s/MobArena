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
    private const string FirstTownEntryPopupTitle = "Tutorial";
    private const string FirstTownEntryPopupText = "Todo, add tutorial with tscn animation popups here";

    private TownBuilding _contractBoard;
    private EnvironmentVisualOverlay _environmentOverlay;
    private TownPhaseState _phaseState;

    public override void _Ready()
    {
        _contractBoard = GetNode<TownBuilding>("World/ContractBoard");
        _environmentOverlay = GetNode<EnvironmentVisualOverlay>("EnvironmentOverlay");
        _phaseState = SaveNode.Get()?.TownPhaseState;
        if (_phaseState != null)
            _phaseState.PhaseChanged += RefreshEnvironmentVisuals;

        var townHud = GetNode<TownHud>("TownHud");
        townHud.BackPressed += OnMainMenuPressed;
        townHud.SelectContractPressed += OnSelectContractPressed;
        GetNode<Button>("World/RosterYard/GladiatorsButton").Pressed += OnGladiatorsPressed;
        GetNode<Button>("World/RosterYard/EquipmentButton").Pressed += OnEquipmentPressed;
        RefreshEnvironmentVisuals();
        CallDeferred(MethodName.ShowFirstTownEntryPopupIfNeeded);
    }

    public override void _ExitTree()
    {
        if (_phaseState != null)
            _phaseState.PhaseChanged -= RefreshEnvironmentVisuals;
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
        GetTree().ChangeSceneToFile(MainMenuScene);
    }

    private void OnSelectContractPressed()
    {
        _contractBoard?.Activate();
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
}
