using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Arena;

public partial class PlayerCombatant : ArenaCombatant
{
    private const string ArenaPlayersGroup = "arena_players";
    private const float DisplayHeight = 96f;
    private const float HandDisplayHeight = 18f;
    private const float HeldItemDisplayHeight = 34f;

    private Sprite2D _body;
    private Sprite2D _leftHand;
    private Sprite2D _rightHand;
    private Sprite2D _mainHandItem;
    private Sprite2D _offHandItem;
    private Label _nameLabel;
    private Label _controllerLabel;

    public GladiatorData GladiatorData { get; private set; }
    public ArenaControlAssignmentData ControlAssignment { get; private set; }
    public ArenaCombatInputState InputState { get; private set; } = new();

    public override void _Ready()
    {
        ConfigureTopDownMotion();
        AddToGroup(ArenaPlayersGroup);
        _body = GetNode<Sprite2D>("Body");
        _leftHand = GetNode<Sprite2D>("LeftHand");
        _rightHand = GetNode<Sprite2D>("RightHand");
        _mainHandItem = GetNode<Sprite2D>("RightHand/MainHandItem");
        _offHandItem = GetNode<Sprite2D>("LeftHand/OffHandItem");
        _nameLabel = GetNode<Label>("NameLabel");
        _controllerLabel = GetNode<Label>("ControllerLabel");
        InputState ??= new ArenaCombatInputState();
        ApplySettingsDeadzone();
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        ApplyCombatInput(ReadAssignedMoveInput(), false, false, false);
        if (InputState.IsMoving)
            SetLookDirectionFromInput(InputState.MoveDirection);

        MoveWithInputState(InputState);
        ApplyBodyVisual();
    }

    public void ConfigureGladiator(GladiatorData gladiatorData, ArenaControlAssignmentData controlAssignment)
    {
        GladiatorData = gladiatorData;
        ControlAssignment = controlAssignment;
        InputState ??= new ArenaCombatInputState();
        ApplySettingsDeadzone();
        InputState.Reset();
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

        ApplyBodyVisual();

        _nameLabel.Text = GladiatorData?.GladiatorName ?? "Gladiator";
        _controllerLabel.Text = ControlAssignment == null
            ? "Unassigned"
            : ControlAssignment.DisplayName;
    }

    private void ApplyBodyVisual()
    {
        ApplyLookVisual(_body, GladiatorData?.GetBodyForwardTexture(), GladiatorData?.GetBodyBackTexture(), DisplayHeight);
        ApplyHandVisuals();
        ApplyHeldItemVisuals();
    }

    private void ApplyHandVisuals()
    {
        if (GladiatorData?.UsesSeparatedHands() != true)
        {
            _leftHand?.Hide();
            _rightHand?.Hide();
            return;
        }

        var handTexture = GladiatorData.GetHandTexture();
        ApplyDirectionalVisual(_leftHand, handTexture, HandDisplayHeight, new Vector2(-26f, -20f));
        ApplyDirectionalVisual(_rightHand, handTexture, HandDisplayHeight, new Vector2(26f, -20f));
    }

    private void ApplyHeldItemVisuals()
    {
        var equipment = GladiatorData?.Equipment;
        ApplyDirectionalVisual(_mainHandItem, equipment?.MainHand?.GetHeldTexture(), HeldItemDisplayHeight, new Vector2(12f, -2f));
        ApplyDirectionalVisual(_offHandItem, equipment?.OffHand?.GetHeldTexture(), HeldItemDisplayHeight, new Vector2(-12f, -2f));
    }
}
