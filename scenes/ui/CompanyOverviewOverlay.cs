using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;

namespace MobArena.Scenes.UI;

public partial class CompanyOverviewOverlay : Control
{
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
}
