using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class VitalProgressBar : ProgressBar
{
    private const float MaxExhaustionValue = 10f;
    private const float ExhaustionWarningRatio = 0.5f;

    private Control _range;
    private Control _thresholdMarker;
    private Label _valueLabel;

    public override void _Ready()
    {
        _range = GetNodeOrNull<Control>("Range") ?? GetNodeOrNull<Control>("RecoverableHealthRange");
        _thresholdMarker = GetNodeOrNull<Control>("ThresholdMarker")
            ?? GetNodeOrNull<Control>("RecoverableMaxMarker")
            ?? GetNodeOrNull<Control>("PenaltyThreshold");
        _valueLabel = GetNodeOrNull<Label>("Value");
    }

    public void ShowValue(float value, float maxValue, string prefix = null)
    {
        MaxValue = Mathf.Max(1f, maxValue);
        Value = Mathf.Clamp(value, 0f, (float)MaxValue);

        if (_valueLabel == null)
            return;

        _valueLabel.Text = string.IsNullOrWhiteSpace(prefix)
            ? $"{value:0}/{maxValue:0}"
            : $"{prefix} {value:0}/{maxValue:0}";
    }

    public void ShowHealth(GladiatorData gladiatorData, string prefix = null)
    {
        if (gladiatorData == null)
            return;

        ShowValue(gladiatorData.Health, gladiatorData.MaxHealth, prefix);
        var currentRatio = gladiatorData.MaxHealth <= 0 ? 0f : Mathf.Clamp(gladiatorData.Health / (float)gladiatorData.MaxHealth, 0f, 1f);
        ShowRange(currentRatio, gladiatorData.RecoverableConditionRatio);
        ShowThreshold(gladiatorData.RecoverableConditionRatio);
    }

    public void ShowExhaustion(float exhaustion, string prefix = null)
    {
        ShowValue(exhaustion, MaxExhaustionValue, prefix);
        HideRange();
        ShowThreshold(ExhaustionWarningRatio);
    }

    public void ShowThreshold(float ratio)
    {
        if (_thresholdMarker == null)
            return;

        ratio = Mathf.Clamp(ratio, 0f, 1f);
        _thresholdMarker.Visible = true;
        _thresholdMarker.AnchorLeft = ratio;
        _thresholdMarker.AnchorRight = ratio;
        _thresholdMarker.OffsetLeft = -1f;
        _thresholdMarker.OffsetRight = 1f;
    }

    public void ShowRange(float startRatio, float endRatio)
    {
        if (_range == null)
            return;

        startRatio = Mathf.Clamp(startRatio, 0f, 1f);
        endRatio = Mathf.Clamp(endRatio, 0f, 1f);
        _range.Visible = endRatio > startRatio;
        _range.AnchorLeft = startRatio;
        _range.AnchorRight = endRatio;
        _range.OffsetLeft = 0f;
        _range.OffsetRight = 0f;
    }

    public void HideRange()
    {
        if (_range != null)
            _range.Visible = false;
    }
}
