using Godot;

namespace MobArena.Scenes.Components.UI.ItemShowcases;

public partial class ItemShowcaseBadge : VBoxContainer
{
    private TextureRect _icon;
    private Label _label;
    private Texture2D _configuredTexture;
    private string _configuredLabel = string.Empty;
    private string _configuredTooltip = string.Empty;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        _label = GetNode<Label>("Label");
        RefreshUi();
    }

    public void Configure(Texture2D texture, string label, string tooltip)
    {
        _configuredTexture = texture;
        _configuredLabel = label;
        _configuredTooltip = tooltip;
        RefreshUi();
    }

    private void RefreshUi()
    {
        TooltipText = _configuredTooltip;
        if (!IsNodeReady())
            return;

        _icon.Texture = _configuredTexture;
        _icon.TooltipText = _configuredTooltip;
        _label.Text = _configuredLabel;
        _label.TooltipText = _configuredTooltip;
    }
}
