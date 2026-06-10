using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaControlAssignmentCard : PanelContainer
{
    [Signal]
    public delegate void PointerJoinRequestedEventHandler(int kind);

    private TextureRect _portrait;
    private Label _nameLabel;
    private Label _assignmentLabel;
    private GladiatorData _gladiator;
    private string _assignmentText = "Unassigned";
    private bool _isCurrent;

    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("MarginContainer/Layout/Portrait");
        _nameLabel = GetNode<Label>("MarginContainer/Layout/NameLabel");
        _assignmentLabel = GetNode<Label>("MarginContainer/Layout/AssignmentLabel");
        GuiInput += OnGuiInput;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        GuiInput -= OnGuiInput;
    }

    public void Configure(GladiatorData gladiator, string assignmentText, bool isCurrent)
    {
        _gladiator = gladiator;
        _assignmentText = assignmentText;
        _isCurrent = isCurrent;
        RefreshUi();
    }

    private void RefreshUi()
    {
        Modulate = _isCurrent ? new Color(1f, 0.92f, 0.55f) : Colors.White;

        if (!IsNodeReady())
            return;

        _portrait.Texture = _gladiator?.GetUiIconTexture();
        _nameLabel.Text = _gladiator?.GladiatorName ?? "Gladiator";
        _assignmentLabel.Text = _assignmentText;
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (!_isCurrent)
            return;

        if (inputEvent is InputEventMouseButton { Pressed: true })
        {
            EmitSignal(SignalName.PointerJoinRequested, (int)LocalInputControllerConfig.ControllerKind.Mouse);
            GetViewport()?.SetInputAsHandled();
            return;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: true })
        {
            EmitSignal(SignalName.PointerJoinRequested, (int)LocalInputControllerConfig.ControllerKind.Touch);
            GetViewport()?.SetInputAsHandled();
        }
    }
}
