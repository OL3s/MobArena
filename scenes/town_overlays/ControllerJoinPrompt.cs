using Godot;

namespace MobArena.Scenes.TownOverlays;

public partial class ControllerJoinPrompt : HBoxContainer
{
    private TextureRect _icon;
    private Label _label;
    private Texture2D _configuredIcon;
    private string _configuredLabel = string.Empty;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        _label = GetNode<Label>("Label");
        RefreshUi();
    }

    public void Configure(Texture2D icon, string label)
    {
        _configuredIcon = icon;
        _configuredLabel = label;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _icon.Texture = _configuredIcon;
        _label.Text = _configuredLabel;
    }
}
