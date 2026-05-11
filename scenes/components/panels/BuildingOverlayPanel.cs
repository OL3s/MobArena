using Godot;

namespace MobArena.Scenes.Components.Panels;

public partial class BuildingOverlayPanel : Control
{
    private const float PanelWidthRatio = 0.68f;
    private const int PanelMinWidth = 420;
    private const int PanelMaxWidth = 820;

    [Export]
    public string Title { get; set; } = "Building";

    [Export(PropertyHint.MultilineText)]
    public string Body { get; set; } = "This building is not implemented yet.";

    [Export]
    public Texture2D IconTexture { get; set; }

    private PanelContainer _panel;
    private TextureRect _icon;
    private Label _title;
    private RichTextLabel _body;
    private Button _closeButton;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _icon = GetNode<TextureRect>("CenterContainer/Panel/MarginContainer/Layout/Header/Icon");
        _title = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/Title");
        _body = GetNode<RichTextLabel>("CenterContainer/Panel/MarginContainer/Layout/Body");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton");

        _title.Text = Title;
        _body.Text = Body;
        _icon.Texture = IconTexture;
        _icon.Visible = IconTexture != null;
        _closeButton.Pressed += QueueFree;

        UpdatePanelWidth();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            UpdatePanelWidth();
    }

    private void UpdatePanelWidth()
    {
        if (_panel == null)
            return;

        var viewportWidth = GetViewportRect().Size.X;
        var width = Mathf.Clamp(Mathf.RoundToInt(viewportWidth * PanelWidthRatio), PanelMinWidth, PanelMaxWidth);
        _panel.CustomMinimumSize = new Vector2(width, 0.0f);
    }
}
