using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class GladiatorMarketOverlay : Control
{
    private const string GladiatorMarketCardScenePath = "res://scenes/town_overlays/GladiatorMarketCard.tscn";
    private const string GoldIconPath = "res://assets/ui/icons/gold.svg";

    private SaveNode _saveNode;
    private CompanyRunData _runData;
    private Label _goldLabel;
    private Label _feedbackLabel;
    private HBoxContainer _gladiatorRow;
    private PackedScene _gladiatorMarketCardScene;
    private Texture2D _goldIcon;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _runData = _saveNode.CompanyRunData;
        _runData.EnsureResources();
        _runData.EnsureFirstContractMarketReadiness(_saveNode.CompanyCareerData);

        _goldLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/GoldLabel");
        _feedbackLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/FeedbackLabel");
        _gladiatorRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ScrollContainer/GladiatorRow");
        _gladiatorMarketCardScene = ResourceLoader.Load<PackedScene>(GladiatorMarketCardScenePath);
        _goldIcon = ResourceLoader.Load<Texture2D>(GoldIconPath);

        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton").Pressed += QueueFree;
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
        _runData.EnsureResources();
        _runData.EnsureFirstContractMarketReadiness(_saveNode.CompanyCareerData);
        _goldLabel.Text = _runData.Gold.ToString();

        foreach (var child in _gladiatorRow.GetChildren())
            child.QueueFree();

        foreach (var gladiator in _runData.Market.GladiatorStock)
        {
            if (gladiator != null)
                _gladiatorRow.AddChild(CreateRecruitCard(gladiator));
        }

        if (_runData.Market.GladiatorStock.Count <= 0)
            _feedbackLabel.Text = "No recruits available until the market refreshes.";
    }

    private Control CreateRecruitCard(GladiatorData gladiator)
    {
        var price = gladiator.GetMarketValue();
        var card = _gladiatorMarketCardScene?.Instantiate<GladiatorMarketCard>();
        if (card == null)
        {
            GD.PushError("Gladiator market card scene is missing or has the wrong root script.");
            return new Control();
        }

        card.Configure(gladiator, _goldIcon, price, _runData.Gold >= price && _runData.CanAddGladiator());
        card.HirePressed += OnHirePressed;
        return card;
    }

    private void OnHirePressed(GladiatorData gladiator)
    {
        if (gladiator == null)
            return;

        var isFirstGladiatorPurchase = _runData.AliveGladiators <= 0;
        var price = gladiator.GetMarketValue();
        if (_runData.Gold < price)
        {
            _feedbackLabel.Text = $"Need {price} gold to hire {gladiator.GladiatorName}.";
            return;
        }

        if (!_runData.CanAddGladiator())
        {
            _feedbackLabel.Text = $"Roster is full ({_runData.AliveGladiators}/{_runData.GladiatorCapacity}).";
            RefreshUi();
            return;
        }

        if (!_runData.TryBuyMarketGladiator(gladiator, _saveNode.CompanyCareerData))
        {
            _feedbackLabel.Text = $"Could not hire {gladiator.GladiatorName}.";
            RefreshUi();
            return;
        }

        _feedbackLabel.Text = $"Hired {gladiator.GladiatorName}.";
        if (isFirstGladiatorPurchase)
            QueueFree();
    }
}
