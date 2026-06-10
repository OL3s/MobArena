using Godot;

namespace MobArena.Scenes.Components.Panels;

public partial class BuildingAttributeBar : VBoxContainer
{
    private static readonly Color PreviewGainColor = new(0.22f, 0.82f, 0.28f, 0.9f);

    private Label _valueLabel;
    private ColorRect _line;
    private ColorRect _currentFill;
    private ColorRect _gainFill;
    private string _label = string.Empty;
    private string _text = string.Empty;
    private float _currentRatio;
    private float _gainRatio;

    public override void _Ready()
    {
        _valueLabel = GetNode<Label>("ValueLabel");
        _line = GetNode<ColorRect>("Line");
        _currentFill = GetNode<ColorRect>("Line/CurrentFill");
        _gainFill = GetNode<ColorRect>("Line/GainFill");
        RefreshUi();
    }

    public void Configure(string label, float value, float maxValue, string text, float gainValue = 0f)
    {
        var safeMax = Mathf.Max(1f, maxValue);
        _label = label;
        _text = text;
        _currentRatio = Mathf.Clamp(value / safeMax, 0f, 1f);
        _gainRatio = Mathf.Clamp(gainValue / safeMax, 0f, 1f - _currentRatio);
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _valueLabel.Text = _text;
        _line.TooltipText = _label;
        _currentFill.AnchorRight = _currentRatio;
        _gainFill.Visible = _gainRatio > 0f;
        _gainFill.AnchorLeft = _currentRatio;
        _gainFill.AnchorRight = _currentRatio + _gainRatio;
        _gainFill.Color = PreviewGainColor;
    }
}
