using Godot;

namespace MobArena.Scenes.UI;

public partial class CodexEntryRow : HBoxContainer
{
    [Signal]
    public delegate void EntryPressedEventHandler();

    private Button _button;
    private TextureRect _badgeIcon;
    private string _title = string.Empty;
    private Texture2D _entryIcon;
    private Texture2D _badgeTexture;

    public override void _Ready()
    {
        _button = GetNode<Button>("EntryButton");
        _badgeIcon = GetNode<TextureRect>("BadgeIcon");
        _button.Pressed += () => EmitSignal(SignalName.EntryPressed);
        RefreshUi();
    }

    public void Configure(string title, Texture2D entryIcon, Texture2D badgeTexture = null)
    {
        _title = title;
        _entryIcon = entryIcon;
        _badgeTexture = badgeTexture;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _button.Text = _title;
        _button.Icon = _entryIcon;
        _badgeIcon.Texture = _badgeTexture;
        _badgeIcon.Visible = _badgeTexture != null;
    }
}
