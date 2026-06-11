using Godot;

namespace MobArena.Scenes.Components.UI;

public partial class ContractRewardRow : HBoxContainer
{
    private TextureRect _icon;
    private Label _label;
    private Texture2D _configuredIcon;
    private string _configuredText = string.Empty;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        _label = GetNode<Label>("Label");
        RefreshUi();
    }

    public void Configure(Texture2D icon, string text)
    {
        _configuredIcon = icon;
        _configuredText = text;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _icon.Texture = _configuredIcon;
        _label.Text = _configuredText;
    }
}
