using Godot;

namespace MobArena.Scenes.TownOverlays;

public partial class PhaseCostRow : HBoxContainer
{
    private TextureRect _icon;
    private Label _label;
    private Label _value;
    private Texture2D _configuredIcon;
    private string _configuredLabel = string.Empty;
    private string _configuredValue = string.Empty;
    private bool _highlightRed;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        _label = GetNode<Label>("Label");
        _value = GetNode<Label>("Value");
        RefreshUi();
    }

    public void Configure(Texture2D icon, string label, string value, bool highlightRed)
    {
        _configuredIcon = icon;
        _configuredLabel = label;
        _configuredValue = value;
        _highlightRed = highlightRed;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _icon.Texture = _configuredIcon;
        _label.Text = _configuredLabel;
        _value.Text = _configuredValue;
        _value.RemoveThemeColorOverride("font_color");
        if (_highlightRed)
            _value.AddThemeColorOverride("font_color", new Color(1f, 0.28f, 0.22f));
    }
}
