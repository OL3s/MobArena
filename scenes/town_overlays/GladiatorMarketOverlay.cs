using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class GladiatorMarketOverlay : Control
{
    private const string GladiatorCardScenePath = "res://scenes/components/ui/GladiatorCard.tscn";
    private const string GoldIconPath = "res://assets/ui/icons/gold.svg";

    private SaveNode _saveNode;
    private CompanyRunData _runData;
    private Label _goldLabel;
    private Label _feedbackLabel;
    private HBoxContainer _gladiatorRow;
    private PackedScene _gladiatorCardScene;
    private Texture2D _goldIcon;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _runData = _saveNode.CompanyRunData;
        _runData.EnsureResources();

        _goldLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/GoldLabel");
        _feedbackLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/FeedbackLabel");
        _gladiatorRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ScrollContainer/GladiatorRow");
        _gladiatorCardScene = ResourceLoader.Load<PackedScene>(GladiatorCardScenePath);
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
        var container = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(210, 0)
        };
        container.AddThemeConstantOverride("separation", 6);

        var card = _gladiatorCardScene.Instantiate<GladiatorCard>();
        container.AddChild(card);
        card.Configure(gladiator);

        var actionRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actionRow.AddThemeConstantOverride("separation", 10);
        actionRow.AddChild(new TextureRect
        {
            Texture = _goldIcon,
            CustomMinimumSize = new Vector2(20, 20),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });
        actionRow.AddChild(new Label
        {
            Text = price.ToString(),
            VerticalAlignment = VerticalAlignment.Center
        });

        var hireButton = new Button
        {
            Text = "Hire",
            CustomMinimumSize = new Vector2(160, 48),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Disabled = _runData.Gold < price || !_runData.CanAddGladiator()
        };
        hireButton.Pressed += () => OnHirePressed(gladiator);
        actionRow.AddChild(hireButton);
        container.AddChild(actionRow);

        return container;
    }

    private void OnHirePressed(GladiatorData gladiator)
    {
        if (gladiator == null)
            return;

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
