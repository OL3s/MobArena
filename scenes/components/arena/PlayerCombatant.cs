using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Arena;

public partial class PlayerCombatant : ArenaCombatant
{
    private const float DisplayHeight = 96f;
    private const float TouchDragMoveScale = 48f;

    private Sprite2D _body;
    private Label _nameLabel;
    private Label _controllerLabel;
    private Vector2 _touchMoveInput = Vector2.Zero;

    public GladiatorData GladiatorData { get; private set; }
    public ArenaControlAssignmentData ControlAssignment { get; private set; }
    public ArenaCombatInputState InputState { get; private set; } = new();

    public override void _Ready()
    {
        _body = GetNode<Sprite2D>("Body");
        _nameLabel = GetNode<Label>("NameLabel");
        _controllerLabel = GetNode<Label>("ControllerLabel");
        InputState ??= new ArenaCombatInputState();
        ApplySettingsDeadzone();
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        ApplyCombatInput(ReadAssignedMoveInput(), false, false, false);
        MoveWithInputState(InputState);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (ControlAssignment?.ControllerKind != LocalInputControllerConfig.ControllerKind.Touch)
            return;

        if (inputEvent is InputEventScreenDrag drag)
        {
            _touchMoveInput = drag.Relative / TouchDragMoveScale;
            GetViewport()?.SetInputAsHandled();
        }
        else if (inputEvent is InputEventScreenTouch { Pressed: false })
        {
            _touchMoveInput = Vector2.Zero;
            GetViewport()?.SetInputAsHandled();
        }
    }

    public void ConfigureGladiator(GladiatorData gladiatorData, ArenaControlAssignmentData controlAssignment)
    {
        GladiatorData = gladiatorData;
        ControlAssignment = controlAssignment;
        InputState ??= new ArenaCombatInputState();
        ApplySettingsDeadzone();
        InputState.Reset();
        _touchMoveInput = Vector2.Zero;
        Name = string.IsNullOrWhiteSpace(gladiatorData?.GladiatorName)
            ? "PlayerCombatant"
            : $"{gladiatorData.GladiatorName}PlayerCombatant";

        Refresh();
    }

    private void ApplySettingsDeadzone()
    {
        var deadzone = SaveNode.Get().SettingsConfig?.ArenaMoveDeadzone ?? 0.3f;
        InputState.SetMoveDeadzone(deadzone);
    }

    public void ApplyCombatInput(Vector2 moveDirection, bool mainHandPressed, bool offHandPressed, bool abilityPressed)
    {
        InputState ??= new ArenaCombatInputState();
        InputState.SetMoveDirection(moveDirection);
        InputState.SetActionPressed(mainHandPressed, offHandPressed, abilityPressed);
    }

    private Vector2 ReadAssignedMoveInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => ReadKeyboardMoveInput(),
            LocalInputControllerConfig.ControllerKind.Touch => _touchMoveInput,
            LocalInputControllerConfig.ControllerKind.Gamepad => ReadGamepadMoveInput(ControlAssignment.DeviceId),
            _ => Vector2.Zero
        };
    }

    private static Vector2 ReadKeyboardMoveInput()
    {
        var direction = Vector2.Zero;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
            direction.X -= 1f;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
            direction.X += 1f;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
            direction.Y -= 1f;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
            direction.Y += 1f;

        return direction;
    }

    private static Vector2 ReadGamepadMoveInput(int deviceId)
    {
        var direction = new Vector2(
            Input.GetJoyAxis(deviceId, JoyAxis.LeftX),
            Input.GetJoyAxis(deviceId, JoyAxis.LeftY));

        if (Input.IsJoyButtonPressed(deviceId, JoyButton.DpadLeft))
            direction.X -= 1f;
        if (Input.IsJoyButtonPressed(deviceId, JoyButton.DpadRight))
            direction.X += 1f;
        if (Input.IsJoyButtonPressed(deviceId, JoyButton.DpadUp))
            direction.Y -= 1f;
        if (Input.IsJoyButtonPressed(deviceId, JoyButton.DpadDown))
            direction.Y += 1f;

        return direction;
    }

    private void Refresh()
    {
        if (!IsNodeReady())
            return;

        FitSpriteHeight(_body, GladiatorData?.GetBodyForwardTexture(), DisplayHeight);

        _nameLabel.Text = GladiatorData?.GladiatorName ?? "Gladiator";
        _controllerLabel.Text = ControlAssignment == null
            ? "Unassigned"
            : ControlAssignment.DisplayName;
    }
}
