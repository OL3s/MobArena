using Godot;
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
        _priorityLabel.Text = $"Priority: {GetPriorityName(policy.Priority)}";
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
