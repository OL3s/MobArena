using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class GladiatorCard : PanelContainer
{
    private TextureRect _portrait;
    private Label _nameLabel;

    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("MarginContainer/Layout/Portrait");
        _nameLabel = GetNode<Label>("MarginContainer/Layout/Name");
    }

    public void Configure(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return;

        if (!IsNodeReady())
            return;

        _portrait.Texture = gladiatorData.GetPortraitTexture();
        _nameLabel.Text = gladiatorData.GladiatorName;
    }
}
