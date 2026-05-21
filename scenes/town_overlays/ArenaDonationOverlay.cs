using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaDonationOverlay : Control
{
    private CompanyRunData _runData;
    private Label _goldLabel;
    private Label _fameLabel;
    private Label _donateOneCostLabel;
    private Label _donateFiveCostLabel;
    private Button _donateOneButton;
    private Button _donateFiveButton;

    public override void _Ready()
    {
        _runData = SaveNode.Get()?.CompanyRunData;
        _goldLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/StatusGrid/GoldValue");
        _fameLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/StatusGrid/FameValue");
        _donateOneCostLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/DonateOneRow/CostLabel");
        _donateFiveCostLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/DonateFiveRow/CostLabel");
        _donateOneButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/DonateOneRow/DonateButton");
        _donateFiveButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/DonateFiveRow/DonateButton");

        _donateOneButton.Pressed += () => DonateForFame(1);
        _donateFiveButton.Pressed += () => DonateForFame(5);
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton").Pressed += QueueFree;

        if (_runData != null)
            _runData.RunChanged += RefreshUi;

        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void RefreshUi()
    {
        if (_runData == null)
            return;

        var donateOneCost = _runData.GetFameDonationGoldCost(1);
        var donateFiveCost = _runData.GetFameDonationGoldCost(5);
        _goldLabel.Text = _runData.Gold.ToString();
        _fameLabel.Text = _runData.Fame.ToString();
        _donateOneCostLabel.Text = $"Cost: {donateOneCost} gold";
        _donateFiveCostLabel.Text = $"Cost: {donateFiveCost} gold";
        _donateOneButton.Disabled = !_runData.CanDonateForFame(1);
        _donateFiveButton.Disabled = !_runData.CanDonateForFame(5);
    }

    private void DonateForFame(int fameAmount)
    {
        if (_runData?.TryDonateForFame(fameAmount) != true)
            return;

        SaveNode.Get()?.Save();
        RefreshUi();
    }
}
