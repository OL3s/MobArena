using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class PhaseChangeRow : PanelContainer
{
    private TextureRect _portrait;
    private Label _nameLabel;
    private Label _detailLabel;
    private GladiatorData _gladiator;
    private string _detail = string.Empty;
    private bool _muted;

    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("MarginContainer/Row/Portrait");
        _nameLabel = GetNode<Label>("MarginContainer/Row/NameLabel");
        _detailLabel = GetNode<Label>("MarginContainer/Row/DetailLabel");
        RefreshUi();
    }

    public void Configure(GladiatorData gladiator, string detail, bool muted)
    {
        _gladiator = gladiator;
        _detail = detail;
        _muted = muted;
        RefreshUi();
    }

    private void RefreshUi()
    {
        Modulate = _muted ? new Color(0.72f, 0.72f, 0.72f, 1f) : Colors.White;
        if (!IsNodeReady())
            return;

        _portrait.Texture = _gladiator?.GetUiIconTexture();
        _nameLabel.Text = _gladiator?.GladiatorName ?? "Gladiator";
        _detailLabel.Text = _detail;
    }
}
