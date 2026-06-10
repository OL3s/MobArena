using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class AttributeProgressDisplay : Control
{
    private static readonly Color PreviewGainColor = new(0.22f, 0.82f, 0.28f, 0.9f);

    private Control _fill;
    private Control _gain;
    private Label _label;
    private bool _useDefaultFontSize;
    private string _pendingAttributeName;
    private int _pendingLevel;
    private float _pendingProgress;
    private float _pendingGainProgress;
    private bool _pendingHighlightLabel;
    private bool _hasPendingConfigure;

    public override void _Ready()
    {
        _fill = GetNode<Control>("Line/Fill");
        _gain = GetNode<Control>("Line/Gain");
        _label = GetNode<Label>("Label");
        ApplyDefaultFontSize();
        ApplyPendingConfigure();
    }

    public void Configure(GladiatorLevelData levelData, GladiatorLevelData.AttributeKind attributeKind)
    {
        var level = levelData?.GetAttributeLevel(attributeKind) ?? 1;
        var progress = levelData?.GetAttributeLevelProgress(attributeKind) ?? 0f;
        Configure(GetAttributeAbbreviation(attributeKind), level, progress);
    }

    public void Configure(string attributeName, int level, float progress)
    {
        Configure(attributeName, level, progress, 0f);
    }

    public void Configure(string attributeName, int level, float progress, float gainProgress)
    {
        Configure(attributeName, level, progress, gainProgress, false);
    }

    public void Configure(string attributeName, int level, float progress, float gainProgress, bool highlightLabel)
    {
        if (!IsNodeReady())
        {
            _pendingAttributeName = attributeName;
            _pendingLevel = level;
            _pendingProgress = progress;
            _pendingGainProgress = gainProgress;
            _pendingHighlightLabel = highlightLabel;
            _hasPendingConfigure = true;
            return;
        }

        progress = Mathf.Clamp(progress, 0f, 1f);
        gainProgress = Mathf.Clamp(gainProgress, 0f, 1f - progress);
        _label.Text = $"{attributeName} {Mathf.Max(1, level)}";
        _fill.AnchorRight = progress;
        _fill.OffsetRight = 0f;
        _gain.Visible = gainProgress > 0f;
        _gain.AnchorLeft = progress;
        _gain.AnchorRight = progress + gainProgress;
        _gain.OffsetLeft = 0f;
        _gain.OffsetRight = 0f;
        if (highlightLabel)
            _label.AddThemeColorOverride("font_color", PreviewGainColor);
        else
            _label.RemoveThemeColorOverride("font_color");
        _hasPendingConfigure = false;
    }

    public void UseDefaultFontSize()
    {
        _useDefaultFontSize = true;
        ApplyDefaultFontSize();
    }

    private void ApplyDefaultFontSize()
    {
        if (!IsNodeReady() || !_useDefaultFontSize)
            return;

        _label.RemoveThemeFontSizeOverride("font_size");
    }

    private void ApplyPendingConfigure()
    {
        if (!_hasPendingConfigure)
            return;

        Configure(_pendingAttributeName, _pendingLevel, _pendingProgress, _pendingGainProgress, _pendingHighlightLabel);
    }

    private static string GetAttributeAbbreviation(GladiatorLevelData.AttributeKind attributeKind)
    {
        return attributeKind switch
        {
            GladiatorLevelData.AttributeKind.Agility => "AGI",
            GladiatorLevelData.AttributeKind.Vitality => "VIT",
            GladiatorLevelData.AttributeKind.Endurance => "END",
            _ => "STR"
        };
    }
}
