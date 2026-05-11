using Godot;

namespace MobArena.Scripts;

public partial class GoCancelPopupPanel : Control
{
    private const float PanelWidthRatio = 0.6f;
    private const int PanelMinWidth = 360;
    private const int PanelMaxWidth = 760;

    [Signal]
    public delegate void GoSelectedEventHandler();

    [Signal]
    public delegate void CancelledEventHandler();

    private PanelContainer _panel;
    private Label _titleLabel;
    private RichTextLabel _bodyText;
    private Button _goButton;
    private Button _cancelButton;

    public override void _Ready()
    {
        EnsureNodes();
        UpdatePanelWidth();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            UpdatePanelWidth();
    }

    public void ShowContent(string title, string richText, string goText = "Go", string cancelText = "Cancel")
    {
        EnsureNodes();
        _titleLabel.Text = string.IsNullOrWhiteSpace(title) ? "Confirm" : title;
        _bodyText.Text = richText ?? string.Empty;
        _goButton.Text = string.IsNullOrWhiteSpace(goText) ? "Go" : goText;
        _cancelButton.Text = string.IsNullOrWhiteSpace(cancelText) ? "Cancel" : cancelText;
        _goButton.CallDeferred(Control.MethodName.GrabFocus);
        UpdatePanelWidth();
    }

    private void EnsureNodes()
    {
        if (_panel != null)
            return;

        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _titleLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Title");
        _bodyText = GetNode<RichTextLabel>("CenterContainer/Panel/MarginContainer/Layout/BodyText");
        _goButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Buttons/GoButton");
        _cancelButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Buttons/CancelButton");

        _goButton.Pressed += OnGoPressed;
        _cancelButton.Pressed += OnCancelPressed;
    }

    private void UpdatePanelWidth()
    {
        if (_panel == null)
            return;

        var viewportWidth = GetViewportRect().Size.X;
        var width = Mathf.Clamp(Mathf.RoundToInt(viewportWidth * PanelWidthRatio), PanelMinWidth, PanelMaxWidth);
        _panel.CustomMinimumSize = new Vector2(width, 0.0f);
    }

    private void OnGoPressed()
    {
        EmitSignal(SignalName.GoSelected);
    }

    private void OnCancelPressed()
    {
        EmitSignal(SignalName.Cancelled);
    }
}
