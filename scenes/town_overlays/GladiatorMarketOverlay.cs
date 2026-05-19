using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class GladiatorMarketOverlay : Control
{
    private const string GladiatorCardScenePath = "res://scenes/components/ui/GladiatorCard.tscn";

    private SaveNode _saveNode;
    private CompanyRunData _runData;
    private Label _goldLabel;
    private Label _feedbackLabel;
    private HBoxContainer _gladiatorRow;
    private PackedScene _gladiatorCardScene;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _runData = _saveNode.CompanyRunData;
        _runData.EnsureResources();

        _goldLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/GoldLabel");
        _feedbackLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/FeedbackLabel");
        _gladiatorRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ScrollContainer/GladiatorRow");
        _gladiatorCardScene = ResourceLoader.Load<PackedScene>(GladiatorCardScenePath);

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
        _goldLabel.Text = $"Gold: {_runData.Gold}";

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
        var container = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(210, 420)
        };
        container.AddThemeConstantOverride("separation", 8);

        var card = _gladiatorCardScene.Instantiate<GladiatorCard>();
        container.AddChild(card);
        card.Configure(gladiator);

        var priceLabel = new Label
        {
            Text = $"Hire cost: {gladiator.InitialCost} gold",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        container.AddChild(priceLabel);

        var hireButton = new Button
        {
            Text = "Hire",
            CustomMinimumSize = new Vector2(160, 48),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Disabled = _runData.Gold < gladiator.InitialCost
        };
        hireButton.Pressed += () => OnHirePressed(gladiator);
        container.AddChild(hireButton);

        return container;
    }

    private void OnHirePressed(GladiatorData gladiator)
    {
        if (gladiator == null)
            return;

        var price = gladiator.InitialCost;
        if (_runData.Gold < price)
        {
            _feedbackLabel.Text = $"Need {price} gold to hire {gladiator.GladiatorName}.";
            return;
        }

        if (!_runData.Market.GladiatorStock.Remove(gladiator))
        {
            _feedbackLabel.Text = $"{gladiator.GladiatorName} is no longer available.";
            RefreshUi();
            return;
        }

        if (!_runData.TryBuyGladiator(gladiator, _saveNode.CompanyCareerData, price))
        {
            _runData.Market.GladiatorStock.Add(gladiator);
            _feedbackLabel.Text = $"Could not hire {gladiator.GladiatorName}.";
            RefreshUi();
            return;
        }

        _feedbackLabel.Text = $"Hired {gladiator.GladiatorName}.";
    }
}
