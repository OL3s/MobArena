using Godot;

namespace MobArena.Scenes.UI;

public partial class CodexGroupPanel : PanelContainer
{
    [Signal]
    public delegate void HeaderPressedEventHandler();

    private static readonly Color ExpandedHeaderColor = new(0.86f, 1f, 0.86f);
    private static readonly Color CollapsedHeaderColor = new(0.78f, 0.78f, 0.78f);

    private Button _headerButton;
    private VBoxContainer _content;
    private string _title = string.Empty;
    private Texture2D _icon;
    private bool _expanded;

    public VBoxContainer Content => _content ??= GetNodeOrNull<VBoxContainer>("MarginContainer/Content");

    public override void _Ready()
    {
        _headerButton = GetNode<Button>("MarginContainer/Content/HeaderButton");
        _content = GetNode<VBoxContainer>("MarginContainer/Content");
        _headerButton.Pressed += () => EmitSignal(SignalName.HeaderPressed);
        RefreshUi();
    }

    public void Configure(string title, Texture2D icon, bool expanded)
    {
        _title = title;
        _icon = icon;
        _expanded = expanded;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        var color = _expanded ? ExpandedHeaderColor : CollapsedHeaderColor;
        _headerButton.Text = _title;
        _headerButton.Icon = _icon;
        _headerButton.AddThemeColorOverride("font_color", color);
        _headerButton.AddThemeColorOverride("font_hover_color", ExpandedHeaderColor);
        _headerButton.AddThemeColorOverride("font_focus_color", ExpandedHeaderColor);
        _headerButton.AddThemeColorOverride("icon_normal_color", color);
        _headerButton.AddThemeColorOverride("icon_hover_color", ExpandedHeaderColor);
        _headerButton.AddThemeColorOverride("icon_focus_color", ExpandedHeaderColor);
    }
}
