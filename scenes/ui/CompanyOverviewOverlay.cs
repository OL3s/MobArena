using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources.Contracts;

namespace MobArena.Scenes.UI;

public partial class CompanyOverviewOverlay : Control
{
    private const string CemeteryOverlayScenePath = "res://scenes/ui/CemeteryOverlay.tscn";
    private const string MainMenuScenePath = "res://scenes/main_menu.tscn";

    [Signal]
    public delegate void EditCompanyRequestedEventHandler();

    private SaveNode _saveNode;
    private Label _companyNameLabel;
    private CompanyLogo _companyLogo;
    private Label _totalGladiatorsLabel;
    private Label _aliveGladiatorsLabel;
    private Label _gladiatorsDeadLabel;
    private Label _totalGoldEarnedLabel;
    private Label _contractsCompletedLabel;
    private Label _mobsKilledLabel;
    private Label _championsDefeatedLabel;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _companyNameLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/CompanyName");
        _companyLogo = GetNode<CompanyLogo>("CenterContainer/PopupPanel/MarginContainer/Content/Logo");
        _totalGladiatorsLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/TotalGladiatorsValue");
        _aliveGladiatorsLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/AliveGladiatorsValue");
        _gladiatorsDeadLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/GladiatorsDeadValue");
        _totalGoldEarnedLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/TotalGoldEarnedValue");
        _contractsCompletedLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/ContractsCompletedValue");
        _mobsKilledLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/MobsKilledValue");
        _championsDefeatedLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Stats/ChampionsDefeatedValue");

        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/EditCompanyButton").Pressed += OnEditCompanyPressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CemeteryButton").Pressed += OnCemeteryPressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/RetireButton").Pressed += OnRetirePressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CloseButton").Pressed += QueueFree;

        RefreshUi();
    }

    private void RefreshUi()
    {
        if (_saveNode == null)
            return;

        var logoData = _saveNode.CompanyLogoData;
        var careerData = _saveNode.CompanyCareerData;

        _companyNameLabel.Text = logoData.CompanyName;
        _companyLogo.SetLogoData(logoData);
        _totalGladiatorsLabel.Text = careerData.TotalGladiatorsInCareer.ToString();
        _aliveGladiatorsLabel.Text = _saveNode.CompanyRunData.AliveGladiators.ToString();
        _gladiatorsDeadLabel.Text = careerData.GladiatorsDead.ToString();
        _totalGoldEarnedLabel.Text = careerData.TotalGoldEarned.ToString();
        _contractsCompletedLabel.Text = careerData.ContractsCompleted.ToString();
        _mobsKilledLabel.Text = careerData.MobsKilled.ToString();
        _championsDefeatedLabel.Text = careerData.ChampionsDefeated.ToString();
    }

    private void OnEditCompanyPressed()
    {
        EmitSignal(SignalName.EditCompanyRequested);
    }

    private void OnCemeteryPressed()
    {
        var cemeteryOverlayScene = ResourceLoader.Load<PackedScene>(CemeteryOverlayScenePath);
        if (cemeteryOverlayScene == null)
            return;

        GlobalOverlay.Get()?.AddOverlay(cemeteryOverlayScene.Instantiate<CemeteryOverlay>());
    }

    private void OnRetirePressed()
    {
        GlobalOverlay.Get()?.ShowGoCancelPopup(
            "Retire Company?",
            "Are you sure you want to retire this company? This ends the current run and returns to the main menu.",
            ForceRetireCompany,
            "Retire",
            "Cancel");
    }

    private void ForceRetireCompany()
    {
        ArenaContractResultResolver.ResolveCompanyLoss(
            _saveNode,
            "Company Retired",
            "You chose to retire the company. The run has ended, and any qualifying result was recorded.");
        GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
        SceneTransitionLogger.LogChange(GetTree(), MainMenuScenePath, "company retired");
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScenePath);
    }
}
