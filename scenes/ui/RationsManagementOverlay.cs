using Godot;
using MobArena.Scenes.Components.Town;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class RationsManagementOverlay : Control
{
    private const string AutoFeedingOverlayScene = "res://scenes/ui/AutoFeedingOverlay.tscn";

    private CompanyRunData _runData;
    private Label _poorCountLabel;
    private Label _poorFeedBelowLabel;
    private Label _commonCountLabel;
    private Label _commonFeedBelowLabel;
    private Label _fineCountLabel;
    private Label _fineFeedBelowLabel;
    private Label _priorityLabel;
    private Button _poorDragButton;
    private Button _commonDragButton;
    private Button _fineDragButton;
    private Texture2D _poorRationTexture;
    private Texture2D _commonRationTexture;
    private Texture2D _fineRationTexture;
    private Texture2D _dragHandTexture;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _poorCountLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/PoorCard/Content/CountLabel");
        _poorFeedBelowLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/PoorCard/Content/FeedBelowLabel");
        _commonCountLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/CommonCard/Content/CountLabel");
        _commonFeedBelowLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/CommonCard/Content/FeedBelowLabel");
        _fineCountLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/FineCard/Content/CountLabel");
        _fineFeedBelowLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/FineCard/Content/FeedBelowLabel");
        _priorityLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/PriorityLabel");
        _poorDragButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/PoorCard/Content/DragButton");
        _commonDragButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/CommonCard/Content/DragButton");
        _fineDragButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/RationCards/FineCard/Content/DragButton");
        _poorRationTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/rations/poor_ration.svg");
        _commonRationTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/rations/common_ration.svg");
        _fineRationTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/rations/fine_ration.svg");
        _dragHandTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/items/drag_hand.svg");

        ConfigureDragButton(_poorDragButton, RationStoreData.RationQuality.Poor);
        ConfigureDragButton(_commonDragButton, RationStoreData.RationQuality.Common);
        ConfigureDragButton(_fineDragButton, RationStoreData.RationQuality.Fine);

        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedingButton").Pressed += OnAutoFeedingPressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;

        _runData.RunChanged += RefreshUi;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void OnAutoFeedingPressed()
    {
        var overlayScene = ResourceLoader.Load<PackedScene>(AutoFeedingOverlayScene);
        var globalOverlay = GlobalOverlay.Get();
        if (overlayScene == null || globalOverlay == null)
            return;

        QueueFree();
        globalOverlay.AddOverlay(overlayScene);
    }

    private void RefreshUi()
    {
        _runData.EnsureResources();
        var rations = _runData.Rations;
        var policy = _runData.RationFeedingPolicy;

        RefreshCard(_poorCountLabel, _poorFeedBelowLabel, rations.PoorRations, policy.GetFeedBelow(RationStoreData.RationQuality.Poor));
        RefreshCard(_commonCountLabel, _commonFeedBelowLabel, rations.CommonRations, policy.GetFeedBelow(RationStoreData.RationQuality.Common));
        RefreshCard(_fineCountLabel, _fineFeedBelowLabel, rations.FineRations, policy.GetFeedBelow(RationStoreData.RationQuality.Fine));
        _poorDragButton.Disabled = rations.PoorRations <= 0;
        _commonDragButton.Disabled = rations.CommonRations <= 0;
        _fineDragButton.Disabled = rations.FineRations <= 0;
        _priorityLabel.Text = $"Priority: {GetPriorityName(policy.Priority)}";
    }

    private void ConfigureDragButton(Button button, RationStoreData.RationQuality quality)
    {
        button.Icon = _dragHandTexture;
        button.ButtonDown += () => OnDragRationRequested(quality);
    }

    private void OnDragRationRequested(RationStoreData.RationQuality quality)
    {
        var rosterYard = GetTree().GetFirstNodeInGroup("roster_yard") as RosterYard;
        if (rosterYard == null)
            return;

        rosterYard.StartRationDrag(quality, GetRationTexture(quality), GetViewport().GetMousePosition());
        QueueFree();
    }

    private Texture2D GetRationTexture(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Common => _commonRationTexture,
            RationStoreData.RationQuality.Fine => _fineRationTexture,
            _ => _poorRationTexture
        };
    }

    private static void RefreshCard(Label countLabel, Label feedBelowLabel, int count, float feedBelow)
    {
        countLabel.Text = count.ToString();
        feedBelowLabel.Text = $"Feed below {feedBelow:0.0}";
    }

    private static string GetPriorityName(RationFeedingPolicyData.FeedPriority priority)
    {
        return priority switch
        {
            RationFeedingPolicyData.FeedPriority.CheapestFirst => "Cheapest First",
            RationFeedingPolicyData.FeedPriority.BestFirst => "Best First",
            _ => "Closest Fit"
        };
    }
}
