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
    private const string ReturningPlayerTutorialPopupTitle = "Tutorial Mode";
    private const string ReturningPlayerTutorialPopupText = "You have completed company records from a previous run.\n\nDo you want to disable tutorial mode for this run?";
    private const string FirstContractCompletedPopupTitle = "Company Ambition";
    private const string FirstContractCompletedPopupText = "Your first contract is complete.\n\nKeep building your gladiator company.\n\nWin contracts, earn gold, grow your fame, recruit better fighters, and prepare for Champion Day.";
    private const string RecoveryBayTutorialPopupTitle = "Recovery Bay";
    private const string RecoveryBayTutorialPopupText = "[center]Your second contract is complete.\n\nRecovery Bay is now available.\n\nDrag gladiators here to spend gold on healing or exhaustion recovery.[/center]";
    private const string TrainingHallTutorialPopupTitle = "Training Hall";
    private const string TrainingHallTutorialPopupText = "[center]Your third contract is complete.\n\nTraining Hall is now available.\n\nDrag gladiators here to spend gold and exhaustion on training.[/center]";
    private const string ConditionRiskTutorialPopupTitle = "Recovery Matters";
    private const string ConditionRiskTutorialPopupText = "[center]Gladiators can return from fights hurt or exhausted.\n\nExhaustion lowers their max usable health and stamina until they recover. Low-health gladiators are risky to send back into the arena.\n\nRegular rest helps only a little. Use Recovery Bay when someone needs proper care.[/center]";
    private const string DemoCompletePopupTitle = "Thanks for Playing";
    private const string DemoCompletePopupText = "Thanks for playing the demo.\n\nYou defeated the first champion and reached the end of this demo build.";
    private const string RecoveryBayBuildingPath = "res://assets/town/buildings/healer.svg";
    private const string TrainingHallBuildingPath = "res://assets/town/buildings/training_hall.svg";
    private const string ExhaustionIconPath = "res://assets/ui/gladiator_icons/exhaustion.svg";

    private TownBuilding _contractBoard;
    private EnvironmentVisualOverlay _environmentOverlay;
    private TownHud _townHud;
    private TownPhaseState _phaseState;
    private WeatherState _weatherState;
    private SaveNode _saveNode;
    private bool _demoCompleteLocked;

    public override void _Ready()
    {
        _contractBoard = GetNode<TownBuilding>("World/ContractBoard");
        _environmentOverlay = GetNode<EnvironmentVisualOverlay>("EnvironmentOverlay");
        _saveNode = SaveNode.Get();
        _phaseState = _saveNode?.TownPhaseState;
        _weatherState = _saveNode?.WeatherState;
        if (_saveNode != null)
            _saveNode.TutorialModeChanged += RefreshTutorialProgressionState;
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
        LockTownIfDemoComplete(_saveNode);
        CallDeferred(MethodName.ShowDemoCompletePopupIfNeeded);
        CallDeferred(MethodName.ShowReturningPlayerTutorialPromptIfNeeded);
        CallDeferred(MethodName.ShowFirstTownEntryPopupIfNeeded);
        CallDeferred(MethodName.ShowFirstContractCompletedPopupIfNeeded);
        CallDeferred(MethodName.ShowRecoveryBuildingTutorialsIfNeeded);
        CallDeferred(MethodName.ShowConditionRiskTutorialPopupIfNeeded);
    }

    public override void _ExitTree()
    {
        if (_phaseState != null)
            _phaseState.PhaseChanged -= OnPhaseChanged;

        if (_weatherState != null)
            _weatherState.WeatherChanged -= RefreshWeatherVisuals;
        if (_saveNode != null)
            _saveNode.TutorialModeChanged -= RefreshTutorialProgressionState;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (GlobalOverlay.Get()?.HasOpenOverlays == true)
            return;

        if (_demoCompleteLocked)
            return;

        if (!inputEvent.IsActionPressed("ui_accept"))
            return;

        GetViewport()?.SetInputAsHandled();
        _contractBoard?.Activate();
    }

    private void OnMainMenuPressed()
    {
        SaveNode.Get()?.Save();
        SceneTransitionLogger.LogChange(GetTree(), MainMenuScene, "town back to main menu");
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
    }

    private void OnSelectContractPressed()
    {
        if (_demoCompleteLocked)
            return;

        _contractBoard?.Activate();
    }

    private void OnBuyGladiatorPressed()
    {
        if (_demoCompleteLocked)
            return;

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
        if (saveNode.IsDemoComplete)
            return;

        var runData = saveNode.CompanyRunData;
        if (ShouldAskReturningPlayerTutorialPrompt(saveNode, runData))
            return;

        if (saveNode.SkipTutorial || runData == null || runData.HasShownFirstTownEntryPopup)
            return;

        runData.MarkFirstTownEntryPopupShown();
        saveNode.Save();
        GlobalOverlay.Get()?.ShowBlurredPopup(FirstTownEntryPopupTitle, FirstTownEntryPopupText);
    }

    private static void ShowReturningPlayerTutorialPromptIfNeeded()
    {
        var saveNode = SaveNode.Get();
        var runData = saveNode.CompanyRunData;
        if (saveNode.IsDemoComplete || !ShouldAskReturningPlayerTutorialPrompt(saveNode, runData))
            return;

        runData.MarkReturningPlayerTutorialSkipPopupAsked();
        saveNode.Save();
        GlobalOverlay.Get()?.ShowGoCancelPopup(
            ReturningPlayerTutorialPopupTitle,
            ReturningPlayerTutorialPopupText,
            goAction: () =>
            {
                var saveNode = SaveNode.Get();
                saveNode.SetSkipTutorial(true);
                saveNode.Save();
            },
            goText: "Disable",
            cancelText: "Keep",
            pauseGameUntilClosed: true,
            cancelAction: ShowFirstTownEntryPopupIfNeeded);
    }

    private static bool ShouldAskReturningPlayerTutorialPrompt(SaveNode saveNode, CompanyRunData runData)
    {
        return saveNode?.SkipTutorial != true
            && runData != null
            && !runData.HasAskedReturningPlayerTutorialSkipPopup
            && (saveNode.CompletedCompanyHistory?.TotalCompletedRuns ?? 0) > 0;
    }

    private static void ShowFirstContractCompletedPopupIfNeeded()
    {
        var saveNode = SaveNode.Get();
        if (saveNode.IsDemoComplete)
            return;

        var runData = saveNode.CompanyRunData;
        if (saveNode.SkipTutorial || runData == null || !saveNode.HasCompletedContractsForProgression || runData.HasShownFirstContractCompletedPopup)
            return;

        runData.MarkFirstContractCompletedPopupShown();
        saveNode.Save();
        GlobalOverlay.Get()?.ShowBlurredPopup(FirstContractCompletedPopupTitle, FirstContractCompletedPopupText);
    }

    private static void ShowRecoveryBuildingTutorialsIfNeeded()
    {
        var saveNode = SaveNode.Get();
        if (saveNode.IsDemoComplete)
            return;

        var runData = saveNode.CompanyRunData;
        var globalOverlay = GlobalOverlay.Get();
        if (saveNode.SkipTutorial || runData == null || globalOverlay == null)
            return;

        if (saveNode.HasUnlockedRecoveryBayForProgression && !runData.HasShownThermaeTutorialPopup)
        {
            runData.MarkThermaeTutorialPopupShown();
            globalOverlay.ShowBlurredPopup(
                RecoveryBayTutorialPopupTitle,
                RecoveryBayTutorialPopupText,
                ResourceLoader.Load<Texture2D>(RecoveryBayBuildingPath));
        }

        if (saveNode.HasUnlockedTrainingHallForProgression && !runData.HasShownTrainingHallTutorialPopup)
        {
            runData.MarkSpecialtyBuildingsUnlocked();
            runData.MarkTrainingHallTutorialPopupShown();
            globalOverlay.ShowBlurredPopup(
                TrainingHallTutorialPopupTitle,
                TrainingHallTutorialPopupText,
                ResourceLoader.Load<Texture2D>(TrainingHallBuildingPath));
        }

        saveNode.Save();
    }

    private static void ShowConditionRiskTutorialPopupIfNeeded()
    {
        var saveNode = SaveNode.Get();
        if (saveNode.IsDemoComplete)
            return;

        var runData = saveNode.CompanyRunData;
        if (saveNode.SkipTutorial || runData == null || runData.HasShownConditionRiskTutorialPopup || !saveNode.HasUnlockedRecoveryBayForProgression)
            return;

        var lowHealthWarningRatio = saveNode.SettingsConfig?.LowHealthWarningRatio ?? 0.6f;
        if (!HasConditionRiskGladiator(runData, lowHealthWarningRatio))
            return;

        runData.MarkConditionRiskTutorialPopupShown();
        saveNode.Save();
        GlobalOverlay.Get()?.ShowBlurredPopup(
            ConditionRiskTutorialPopupTitle,
            ConditionRiskTutorialPopupText,
            ResourceLoader.Load<Texture2D>(ExhaustionIconPath));
    }

    private static bool HasConditionRiskGladiator(CompanyRunData runData, float lowHealthWarningRatio)
    {
        foreach (var gladiator in runData.Gladiators)
        {
            var riskStatus = gladiator?.GetRiskStatus(5f, lowHealthWarningRatio) ?? GladiatorRiskStatus.None;
            if (riskStatus is GladiatorRiskStatus.Exhausted or GladiatorRiskStatus.LowHealth or GladiatorRiskStatus.Critical)
                return true;
        }

        return false;
    }

    private void LockTownIfDemoComplete(SaveNode saveNode)
    {
        _demoCompleteLocked = saveNode?.IsDemoComplete == true;
        if (!_demoCompleteLocked)
            return;

        var world = GetNodeOrNull<Node>("World");
        if (world != null)
            world.ProcessMode = ProcessModeEnum.Disabled;
        var gladiatorsButton = GetNodeOrNull<Button>("World/RosterYard/ButtonRow/GladiatorsButton");
        if (gladiatorsButton != null)
            gladiatorsButton.Disabled = true;
    }

    private void ShowDemoCompletePopupIfNeeded()
    {
        if (SaveNode.Get()?.IsDemoComplete != true)
            return;

        GlobalOverlay.Get()?.ShowBlurredPopup(
            DemoCompletePopupTitle,
            DemoCompletePopupText,
            closedAction: OnMainMenuPressed,
            pauseGameUntilClosed: true,
            okText: "Menu");
    }

    private void RefreshEnvironmentVisuals()
    {
        _environmentOverlay?.ApplyPhaseState(_phaseState);
    }

    private void OnPhaseChanged()
    {
        RefreshEnvironmentVisuals();
        RefreshWeatherVisuals();
        CallDeferred(MethodName.ShowConditionRiskTutorialPopupIfNeeded);
    }

    private void RefreshWeatherVisuals()
    {
        var weather = _weatherState?.CurrentWeather ?? WeatherState.WeatherVisual.Cloudy;
        _environmentOverlay?.SetWeather(weather);
        _townHud?.SetWeatherVisual(weather);
    }

    private void RefreshTutorialProgressionState()
    {
        foreach (var node in GetTree().GetNodesInGroup(RosterYard.DragDropTargetGroup))
        {
            if (node is TownBuilding building)
                building.RefreshProgressionState();
        }
    }
}
