using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class AutoFeedingOverlay : Control
{
    private CompanyRunData _runData;
    private Label _poorThresholdValueLabel;
    private Label _commonThresholdValueLabel;
    private Label _fineThresholdValueLabel;
    private HSlider _poorThresholdSlider;
    private HSlider _commonThresholdSlider;
    private HSlider _fineThresholdSlider;
    private OptionButton _priorityOptionButton;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _poorThresholdValueLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/PoorRow/ValueLabel");
        _commonThresholdValueLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/CommonRow/ValueLabel");
        _fineThresholdValueLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/FineRow/ValueLabel");
        _poorThresholdSlider = GetNode<HSlider>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/PoorRow/ThresholdSlider");
        _commonThresholdSlider = GetNode<HSlider>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/CommonRow/ThresholdSlider");
        _fineThresholdSlider = GetNode<HSlider>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/FineRow/ThresholdSlider");
        _priorityOptionButton = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/AutoFeedPanel/MarginContainer/AutoFeedLayout/PriorityRow/PriorityOptionButton");

        ConfigureSlider(_poorThresholdSlider, RationFeedingPolicyData.PoorFeedBelowMax);
        ConfigureSlider(_commonThresholdSlider, RationFeedingPolicyData.CommonFeedBelowMax);
        ConfigureSlider(_fineThresholdSlider, RationFeedingPolicyData.FineFeedBelowMax);
        ConfigurePriorityOptions();

        _poorThresholdSlider.ValueChanged += value => OnThresholdChanged(RationStoreData.RationQuality.Poor, value);
        _commonThresholdSlider.ValueChanged += value => OnThresholdChanged(RationStoreData.RationQuality.Common, value);
        _fineThresholdSlider.ValueChanged += value => OnThresholdChanged(RationStoreData.RationQuality.Fine, value);
        _priorityOptionButton.ItemSelected += OnPrioritySelected;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;

        _runData.RunChanged += RefreshUi;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private static void ConfigureSlider(HSlider slider, float maxValue)
    {
        slider.MinValue = 0.0;
        slider.MaxValue = maxValue;
        slider.Step = 0.5;
    }

    private void ConfigurePriorityOptions()
    {
        _priorityOptionButton.Clear();
        _priorityOptionButton.AddItem("Closest Fit", (int)RationFeedingPolicyData.FeedPriority.ClosestFit);
        _priorityOptionButton.AddItem("Cheapest First", (int)RationFeedingPolicyData.FeedPriority.CheapestFirst);
        _priorityOptionButton.AddItem("Best First", (int)RationFeedingPolicyData.FeedPriority.BestFirst);
    }

    private void OnThresholdChanged(RationStoreData.RationQuality quality, double value)
    {
        _runData.SetAutoFeedThreshold(quality, (float)value);
    }

    private void OnPrioritySelected(long index)
    {
        var priority = (RationFeedingPolicyData.FeedPriority)_priorityOptionButton.GetItemId((int)index);
        _runData.SetAutoFeedPriority(priority);
    }

    private void RefreshUi()
    {
        _runData.EnsureResources();
        var policy = _runData.RationFeedingPolicy;

        RefreshThresholdRow(RationStoreData.RationQuality.Poor, _poorThresholdSlider, _poorThresholdValueLabel);
        RefreshThresholdRow(RationStoreData.RationQuality.Common, _commonThresholdSlider, _commonThresholdValueLabel);
        RefreshThresholdRow(RationStoreData.RationQuality.Fine, _fineThresholdSlider, _fineThresholdValueLabel);

        for (var index = 0; index < _priorityOptionButton.ItemCount; index++)
        {
            if (_priorityOptionButton.GetItemId(index) == (int)policy.Priority)
            {
                _priorityOptionButton.Select(index);
                break;
            }
        }
    }

    private void RefreshThresholdRow(RationStoreData.RationQuality quality, HSlider slider, Label valueLabel)
    {
        var threshold = _runData.RationFeedingPolicy.GetFeedBelow(quality);
        slider.SetValueNoSignal(threshold);
        valueLabel.Text = threshold.ToString("0.0");
    }
}
