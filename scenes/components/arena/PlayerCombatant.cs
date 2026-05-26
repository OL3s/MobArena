using Godot;
using MobArena.Scenes.Components.Arena.Effects;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Arena;

public partial class PlayerCombatant : ArenaCombatant
{
    private const string ArenaPlayersGroup = "arena_players";
    private const float DisplayHeight = 96f;
    private const float HandDisplayHeight = 18f;
    private const float DefaultHeldItemDisplayHeight = 48f;

    private Sprite2D _body;
    private Sprite2D _armor;
    private Sprite2D _leftHand;
    private Sprite2D _rightHand;
    private Sprite2D _mainHandItem;
    private Sprite2D _offHandItem;
    private Label _nameLabel;
    private Label _controllerLabel;

    public GladiatorData GladiatorData { get; private set; }
    public ArenaControlAssignmentData ControlAssignment { get; private set; }
    public ArenaCombatInputState InputState { get; private set; } = new();

    private float _mainHandCooldownRemaining;
    private bool _wasMainHandPressed;
    private bool _wasOffHandPressed;
    private bool _wasAbilityPressed;
    private bool _wasBlockPressed;

    public override void _Ready()
    {
        ConfigureTopDownMotion();
        AddToGroup(ArenaPlayersGroup);
        _body = GetNode<Sprite2D>("Body");
        _armor = GetNode<Sprite2D>("Armor");
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
        var mainHandPressed = ReadAssignedMainHandInput();
        var offHandPressed = ReadAssignedOffHandInput();
        var abilityPressed = ReadAssignedAbilityInput();
        var blockPressed = ReadAssignedBlockInput();
        ApplyCombatInput(ReadAssignedMoveInput(), mainHandPressed, offHandPressed, abilityPressed, blockPressed);
        LogActionPresses(mainHandPressed, offHandPressed, abilityPressed, blockPressed);
        UpdateActionCooldowns((float)delta);
        TryActivateMainHand(mainHandPressed);
        _wasMainHandPressed = mainHandPressed;
        _wasOffHandPressed = offHandPressed;
        _wasAbilityPressed = abilityPressed;
        _wasBlockPressed = blockPressed;

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
        ResetActionPressTracking();
        Name = string.IsNullOrWhiteSpace(gladiatorData?.GladiatorName)
            ? "PlayerCombatant"
            : $"{gladiatorData.GladiatorName}PlayerCombatant";

        ConfigureCombatState(CreateCombatState(gladiatorData), ArenaCombatTeam.Player);

        Refresh();
    }

    private static ArenaCombatState CreateCombatState(GladiatorData gladiatorData)
    {
        var combatState = new ArenaCombatState();
        combatState.Configure(
            Mathf.Max(1, gladiatorData?.MaxHealth ?? 1),
            gladiatorData?.Health ?? 1,
            gladiatorData?.Equipment?.Armor?.ArmorProfile);
        return combatState;
    }

    protected override void OnCombatStateHealthChanged(int currentHealth, int maxHealth)
    {
        GladiatorData?.SetHealth(currentHealth);
    }

    private void ApplySettingsDeadzone()
    {
        var deadzone = SaveNode.Get().SettingsConfig?.ArenaMoveDeadzone ?? 0.3f;
        InputState.SetMoveDeadzone(deadzone);
    }

    public void ApplyCombatInput(Vector2 moveDirection, bool mainHandPressed, bool offHandPressed, bool abilityPressed, bool blockPressed)
    {
        InputState ??= new ArenaCombatInputState();
        InputState.SetMoveDirection(moveDirection);
        InputState.SetActionPressed(mainHandPressed, offHandPressed, abilityPressed, blockPressed);
    }

    private Vector2 ReadAssignedMoveInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => ReadKeyboardMoveInput(),
            LocalInputControllerConfig.ControllerKind.Mouse => ReadKeyboardMoveInput(),
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

    private bool ReadAssignedMainHandInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => ReadKeyboardMainHandInput(),
            LocalInputControllerConfig.ControllerKind.Mouse => ReadMouseMainHandInput(),
            LocalInputControllerConfig.ControllerKind.Gamepad => ReadGamepadMainHandInput(ControlAssignment.DeviceId),
            _ => false
        };
    }

    private static bool ReadKeyboardMainHandInput()
    {
        return Input.IsKeyPressed(Key.Space);
    }

    private static bool ReadMouseMainHandInput()
    {
        return Input.IsMouseButtonPressed(MouseButton.Left);
    }

    private static bool ReadGamepadMainHandInput(int deviceId)
    {
        return Input.IsJoyButtonPressed(deviceId, JoyButton.X);
    }

    private bool ReadAssignedOffHandInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => ReadKeyboardOffHandInput(),
            LocalInputControllerConfig.ControllerKind.Mouse => ReadMouseOffHandInput(),
            LocalInputControllerConfig.ControllerKind.Gamepad => ReadGamepadOffHandInput(ControlAssignment.DeviceId),
            _ => false
        };
    }

    private static bool ReadKeyboardOffHandInput()
    {
        return Input.IsKeyPressed(Key.E);
    }

    private static bool ReadMouseOffHandInput()
    {
        return Input.IsMouseButtonPressed(MouseButton.Right);
    }

    private static bool ReadGamepadOffHandInput(int deviceId)
    {
        return Input.IsJoyButtonPressed(deviceId, JoyButton.A);
    }

    private bool ReadAssignedAbilityInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => ReadKeyboardAbilityInput(),
            LocalInputControllerConfig.ControllerKind.Mouse => ReadMouseAbilityInput(),
            LocalInputControllerConfig.ControllerKind.Gamepad => ReadGamepadAbilityInput(ControlAssignment.DeviceId),
            _ => false
        };
    }

    private static bool ReadKeyboardAbilityInput()
    {
        return Input.IsKeyPressed(Key.F);
    }

    private static bool ReadMouseAbilityInput()
    {
        return Input.IsKeyPressed(Key.Q);
    }

    private static bool ReadGamepadAbilityInput(int deviceId)
    {
        return Input.IsJoyButtonPressed(deviceId, JoyButton.B);
    }

    private bool ReadAssignedBlockInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => ReadKeyboardBlockInput(),
            LocalInputControllerConfig.ControllerKind.Mouse => ReadMouseBlockInput(),
            LocalInputControllerConfig.ControllerKind.Gamepad => ReadGamepadBlockInput(ControlAssignment.DeviceId),
            _ => false
        };
    }

    private static bool ReadKeyboardBlockInput()
    {
        return Input.IsKeyPressed(Key.Q);
    }

    private static bool ReadMouseBlockInput()
    {
        return Input.IsKeyPressed(Key.Space);
    }

    private static bool ReadGamepadBlockInput(int deviceId)
    {
        return Input.IsJoyButtonPressed(deviceId, JoyButton.Y);
    }

    private void LogActionPresses(bool mainHandPressed, bool offHandPressed, bool abilityPressed, bool blockPressed)
    {
        if (mainHandPressed && !_wasMainHandPressed)
            LogActionPressed("Main");
        if (offHandPressed && !_wasOffHandPressed)
            LogActionPressed("Off");
        if (abilityPressed && !_wasAbilityPressed)
            LogActionPressed("Ability");
        if (blockPressed && !_wasBlockPressed)
            LogActionPressed("Block");
    }

    private void LogActionPressed(string actionName)
    {
        GD.Print($"PlayerCombatant: {GladiatorData?.GladiatorName ?? Name} pressed {actionName} action ({ControlAssignment?.DisplayName ?? "Unassigned"}).");
    }

    private void ResetActionPressTracking()
    {
        _wasMainHandPressed = false;
        _wasOffHandPressed = false;
        _wasAbilityPressed = false;
        _wasBlockPressed = false;
    }

    private void UpdateActionCooldowns(float delta)
    {
        if (_mainHandCooldownRemaining > 0f)
            _mainHandCooldownRemaining = Mathf.Max(0f, _mainHandCooldownRemaining - delta);
    }

    private void TryActivateMainHand(bool mainHandPressed)
    {
        if (!mainHandPressed || _wasMainHandPressed || _mainHandCooldownRemaining > 0f || IsDead)
            return;

        if (GladiatorData?.Equipment?.MainHand is not DamageItemData mainHand || mainHand.MainAction == null)
            return;

        var staminaCost = mainHand.MainAction.StaminaCost;
        if (staminaCost > 0 && GladiatorData.Stamina < staminaCost)
            return;

        if (!ArenaCombatActionRunner.TryActivate(this, mainHand, mainHand.MainAction))
            return;

        if (staminaCost > 0)
            GladiatorData.SpendStamina(staminaCost);

        _mainHandCooldownRemaining = Mathf.Max(0f, mainHand.MainAction.CooldownSeconds);
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
        ApplyArmorVisual();
        ApplyHandVisuals();
        ApplyHeldItemVisuals();
    }

    private void ApplyArmorVisual()
    {
        var armor = GladiatorData?.Equipment?.Armor;
        ApplyLookVisual(_armor, armor?.ArmorForwardTexture, armor?.ArmorBackTexture, armor?.GetArmorDisplayHeight(DisplayHeight) ?? DisplayHeight);
        if (_armor != null && armor != null)
            _armor.Offset = armor.GetArmorTextureOffset();
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
        ApplyHandDrawOrder();
    }

    private void ApplyHeldItemVisuals()
    {
        var equipment = GladiatorData?.Equipment;
        ApplyHeldVisual(_mainHandItem, equipment?.MainHand, DefaultHeldItemDisplayHeight, new Vector2(12f, -2f));
        ApplyHeldVisual(_offHandItem, equipment?.OffHand, DefaultHeldItemDisplayHeight, new Vector2(-12f, -2f));
        if (_mainHandItem != null && equipment?.MainHand != null)
            _mainHandItem.RotationDegrees = equipment.MainHand.GetHeldRotationDegrees();
        if (_offHandItem != null && equipment?.OffHand != null)
            _offHandItem.RotationDegrees = equipment.OffHand.GetHeldRotationDegrees();
    }

    private static void ApplyHeldVisual(Sprite2D sprite, MobArena.Scripts.Resources.Items.ItemData item, float fallbackDisplayHeight, Vector2 localPosition)
    {
        ApplyLocalVisual(sprite, item?.GetHeldTexture(), item?.GetHeldDisplayHeight(fallbackDisplayHeight) ?? fallbackDisplayHeight, localPosition, item?.GetHeldTextureOffset() ?? Vector2.Zero);
    }

    private static void ApplyLocalVisual(Sprite2D sprite, Texture2D texture, float displayHeight, Vector2 localPosition, Vector2 textureOffset)
    {
        if (sprite == null)
            return;

        if (texture == null)
        {
            sprite.Hide();
            return;
        }

        sprite.Show();
        sprite.Centered = false;
        sprite.Texture = texture;
        sprite.Position = localPosition;
        sprite.Offset = textureOffset;
        sprite.RotationDegrees = 0f;

        if (texture.GetHeight() > 0)
            sprite.Scale = Vector2.One * (displayHeight / texture.GetHeight());
    }

    private void ApplyHandDrawOrder()
    {
        var handZIndex = LookDirection.Y < 0f ? -2 : 1;
        if (_leftHand != null)
            _leftHand.ZIndex = handZIndex;
        if (_rightHand != null)
            _rightHand.ZIndex = handZIndex;
        if (_mainHandItem != null)
            _mainHandItem.ZIndex = 1;
        if (_offHandItem != null)
            _offHandItem.ZIndex = 1;
    }
}
