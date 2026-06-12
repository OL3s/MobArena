using Godot;
using MobArena.Scenes.Components.Arena.Combat.Effects;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Items;
using Godot.Collections;

namespace MobArena.Scenes.Components.Arena;

public partial class PlayerCombatant : ArenaCombatant
{
    private const string ArenaPlayersGroup = "arena_players";
    private const string DefaultStatusProfilePath = "res://resources/combat/status_profiles/default_player_status_profile.tres";
    private const float DisplayHeight = 96f;
    private const float HandDisplayHeight = 18f;
    private const float DefaultHeldItemDisplayHeight = 48f;
    private const float StaminaRegenPerMaxStaminaPerSecond = 0.2f;
    private const float BaseMinimumExhaustedSeconds = 1f;
    private const float MinimumExhaustedSecondsFloor = 0.25f;
    private const float EnduranceExhaustedRecoveryCurve = 10f;
    private const string DefaultMainHandPunchPath = "res://resources/combat/player_defaults/main_hand_punch.tres";
    private const string DefaultOffHandPunchPath = "res://resources/combat/player_defaults/off_hand_punch.tres";

    private Sprite2D _body;
    private Sprite2D _armor;
    private Sprite2D _leftHand;
    private Sprite2D _rightHand;
    private Sprite2D _mainHandItem;
    private Sprite2D _offHandItem;
    private Label _nameLabel;
    private Label _healthLabel;
    private Label _stateLabel;
    private Label _controllerLabel;

    public GladiatorData GladiatorData { get; private set; }
    public ArenaControlAssignmentData ControlAssignment { get; private set; }
    public ArenaCombatInputState InputState { get; private set; } = new();

    private bool _wasMainHandPressed;
    private bool _wasOffHandPressed;
    private bool _wasAbilityPressed;
    private bool _wasBlockPressed;
    private float _staminaRegenAccumulator;
    private DamageItemData _pendingActionItem;
    private ArenaCombatActionData _pendingAction;
    private float _windupRemaining;
    private float _releaseRemaining;
    private DamageItemData _windupItem;
    private ArenaCombatActionData _windupAction;
    private float _windupElapsed;
    private float _pendingWindupScalar = 1f;
    private int _exhaustedStaminaRecoveryThreshold;
    private float _exhaustedRemainingSeconds;

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
        _healthLabel = GetNode<Label>("HealthLabel");
        _stateLabel = GetNode<Label>("StateLabel");
        _controllerLabel = GetNode<Label>("ControllerLabel");
        InputState ??= new ArenaCombatInputState();
        ApplySettingsDeadzone();
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        var deltaSeconds = (float)delta;
        var mainHandPressed = ReadAssignedMainHandInput();
        var offHandPressed = ReadAssignedOffHandInput();
        var abilityPressed = ReadAssignedAbilityInput();
        var blockPressed = ReadAssignedBlockInput();
        ApplyCombatInput(ReadAssignedMoveInput(), ReadAssignedAimInput(), mainHandPressed, offHandPressed, abilityPressed, blockPressed);
        LogActionPresses(mainHandPressed, offHandPressed, abilityPressed, blockPressed);
        UpdateCombatantState(deltaSeconds);
        UpdateExhaustedState(deltaSeconds);
        UpdateWindup(deltaSeconds);
        TryActivateMainHand(mainHandPressed);
        TryActivateOffHand(offHandPressed);
        RegenerateStamina(deltaSeconds);
        _wasMainHandPressed = mainHandPressed;
        _wasOffHandPressed = offHandPressed;
        _wasAbilityPressed = abilityPressed;
        _wasBlockPressed = blockPressed;

        if (InputState.IsAiming)
            SetLookDirectionFromInput(InputState.AimDirection);
        else if (InputState.IsMoving && CanMove())
            SetLookDirectionFromInput(InputState.MoveDirection);

        if (CanMove())
            MoveWithInputState(InputState);
        else
            MoveWithSoftCollisionOnly();
        DecayExternalForce(deltaSeconds);
        TickCombatantStatusEffects(deltaSeconds);
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
        ClearPendingAction();
        ClearWindup();
        ClearExhaustedRecoveryState();
        SetCombatantState(ArenaCombatantState.Default);
        _staminaRegenAccumulator = 0f;
        Name = string.IsNullOrWhiteSpace(gladiatorData?.GladiatorName)
            ? "PlayerCombatant"
            : $"{gladiatorData.GladiatorName}PlayerCombatant";

        ConfigureCombatState(CreateCombatState(gladiatorData), ArenaCombatTeam.Player);

        Refresh();
    }

    private static ArenaCombatState CreateCombatState(GladiatorData gladiatorData)
    {
        var entryHealth = Mathf.Clamp(gladiatorData?.Health ?? 1, 0, gladiatorData?.MaxHealth ?? 1);
        var combatState = new ArenaCombatState();
        combatState.Configure(
            Mathf.Max(1, entryHealth),
            entryHealth,
            gladiatorData?.Equipment?.Armor?.ArmorProfile,
            ResourceLoader.Load<CombatantStatusProfileData>(DefaultStatusProfilePath),
            GetBlockArmorProfiles(gladiatorData));
        return combatState;
    }

    private static Array<ArmorData> GetBlockArmorProfiles(GladiatorData gladiatorData)
    {
        var profiles = new Array<ArmorData>();
        AddBlockArmorProfile(profiles, gladiatorData?.Equipment?.MainHand);
        AddBlockArmorProfile(profiles, gladiatorData?.Equipment?.OffHand);
        return profiles;
    }

    private static void AddBlockArmorProfile(Array<ArmorData> profiles, DamageItemData item)
    {
        if (item?.BlockArmorProfile != null)
            profiles.Add(item.BlockArmorProfile);
    }

    protected override void OnCombatStateHealthChanged(int currentHealth, int maxHealth)
    {
        GladiatorData?.SetHealth(currentHealth);
        RefreshHealthLabel();
    }

    public void SnapshotRuntimeHealthToGladiator()
    {
        if (GladiatorData == null || CombatState == null)
            return;

        GladiatorData.SetHealth(CombatState.CurrentHealth);
    }

    protected override void OnCombatantStateChanged(ArenaCombatantState state)
    {
        RefreshStateLabel();
        RefreshDebugStateModulate();
    }

    private void ApplySettingsDeadzone()
    {
        var deadzone = SaveNode.Get().SettingsConfig?.ArenaMoveDeadzone ?? 0.3f;
        InputState.SetMoveDeadzone(deadzone);
    }

    public void ApplyCombatInput(Vector2 moveDirection, Vector2 aimDirection, bool mainHandPressed, bool offHandPressed, bool abilityPressed, bool blockPressed)
    {
        InputState ??= new ArenaCombatInputState();
        InputState.SetMoveDirection(moveDirection);
        InputState.SetAimDirection(aimDirection);
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

    private Vector2 ReadAssignedAimInput()
    {
        return ControlAssignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => Vector2.Zero,
            LocalInputControllerConfig.ControllerKind.Mouse => ReadMouseAimInput(),
            LocalInputControllerConfig.ControllerKind.Gamepad => ReadGamepadAimInput(ControlAssignment.DeviceId),
            _ => Vector2.Zero
        };
    }

    private Vector2 ReadMouseAimInput()
    {
        return GetGlobalMousePosition() - GlobalPosition;
    }

    private static Vector2 ReadGamepadAimInput(int deviceId)
    {
        return new Vector2(
            Input.GetJoyAxis(deviceId, JoyAxis.RightX),
            Input.GetJoyAxis(deviceId, JoyAxis.RightY));
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
        if (SaveNode.Get()?.DevEnabled != true)
            return;

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
        GameLogger.Combat($"PlayerCombatant: {GladiatorData?.GladiatorName ?? Name} pressed {actionName} action ({ControlAssignment?.DisplayName ?? "Unassigned"}).");
    }

    private void ResetActionPressTracking()
    {
        _wasMainHandPressed = false;
        _wasOffHandPressed = false;
        _wasAbilityPressed = false;
        _wasBlockPressed = false;
    }

    private void TryActivateMainHand(bool mainHandPressed)
    {
        if (!mainHandPressed || _wasMainHandPressed || IsDead)
            return;

        var mainHand = GetMainHandActionItem();
        if (mainHand?.MainAction == null)
            return;

        HandleActionPress(mainHand, mainHand.MainAction);
    }

    private void TryActivateOffHand(bool offHandPressed)
    {
        if (!offHandPressed || _wasOffHandPressed || IsDead)
            return;

        var offHand = GetOffHandActionItem();
        if (offHand?.MainAction == null)
            return;

        HandleActionPress(offHand, offHand.MainAction);
    }

    private DamageItemData GetMainHandActionItem()
    {
        return GladiatorData?.Equipment?.MainHand ?? ResourceLoader.Load<MainHandItemData>(DefaultMainHandPunchPath);
    }

    private DamageItemData GetOffHandActionItem()
    {
        var equipment = GladiatorData?.Equipment;
        if (equipment?.MainHand?.IsTwoHanded == true)
            return null;

        return equipment?.OffHand ?? ResourceLoader.Load<OffHandItemData>(DefaultOffHandPunchPath);
    }

    private void HandleActionPress(DamageItemData item, ArenaCombatActionData action)
    {
        if (item == null || action == null)
            return;

        if (action.Windup != null)
        {
            if (_windupAction == action && _windupItem == item)
            {
                if (action.Windup.CanReleaseEarly != true)
                    return;

                if (TrySpendStaminaAndStartAction(item, action, GetWindupScalar(action), true))
                    ClearWindup();
                return;
            }

            if (_windupAction != null || !CanStartAction())
                return;

            _windupItem = item;
            _windupAction = action;
            _windupElapsed = 0f;
            SetCombatantState(ArenaCombatantState.Windup);
            RefreshStateLabel();
            return;
        }

        if (!CanStartAction())
            return;

        TrySpendStaminaAndStartAction(item, action, 1f);
    }

    private bool TrySpendStaminaAndStartAction(DamageItemData item, ArenaCombatActionData action, float windupScalar, bool skipWindup = false)
    {
        if (!CanStartAction() && !CanReleaseWindup(action))
            return false;

        var staminaCost = action.StaminaCost;
        if (staminaCost > 0)
        {
            if (GladiatorData.Stamina < staminaCost)
            {
                ExhaustFromFailedAction(staminaCost);
                return false;
            }

            GladiatorData.SpendStamina(staminaCost);
        }

        StartAction(item, action, windupScalar, skipWindup);
        return true;
    }

    private bool CanReleaseWindup(ArenaCombatActionData action)
    {
        return action != null && _windupAction == action && CombatantState == ArenaCombatantState.Windup;
    }

    private float GetWindupScalar(ArenaCombatActionData action)
    {
        return action?.Windup?.GetScalar(_windupElapsed, action.WindupSeconds) ?? 1f;
    }

    private void StartAction(DamageItemData item, ArenaCombatActionData action, float windupScalar = 1f, bool skipWindup = false)
    {
        _pendingActionItem = item;
        _pendingAction = action;
        _pendingWindupScalar = Mathf.Clamp(windupScalar, ArenaCombatWindupData.MinScalar, ArenaCombatWindupData.MaxScalar);
        _windupRemaining = skipWindup ? 0f : Mathf.Max(0f, action.WindupSeconds);

        if (_windupRemaining <= 0f)
        {
            ExecutePendingAction();
            return;
        }

        SetCombatantState(ArenaCombatantState.Windup);
    }

    private void UpdateCombatantState(float delta)
    {
        if (delta <= 0f || IsDead)
            return;

        if (CombatantState == ArenaCombatantState.Windup)
        {
            if (_windupAction != null)
                return;

            _windupRemaining -= delta;
            if (_windupRemaining <= 0f)
                ExecutePendingAction();

            return;
        }

        if (CombatantState == ArenaCombatantState.Blocking)
        {
            if (InputState.BlockPressed)
                return;

            SetCombatantState(ArenaCombatantState.Default);
            return;
        }

        if (CombatantState != ArenaCombatantState.Release)
        {
            if (CombatantState == ArenaCombatantState.Default && InputState.BlockPressed)
                SetCombatantState(ArenaCombatantState.Blocking);

            return;
        }

        _releaseRemaining -= delta;
        if (_releaseRemaining <= 0f)
        {
            ClearPendingAction();
            SetCombatantState(ArenaCombatantState.Default);
        }
    }

    private void UpdateWindup(float delta)
    {
        if (_windupAction == null || delta <= 0f || IsDead)
            return;

        _windupElapsed += delta;
        if (_windupAction.Windup.CanReleaseEarly != true && _windupElapsed >= Mathf.Max(0.05f, _windupAction.WindupSeconds))
        {
            var item = _windupItem;
            var action = _windupAction;
            if (TrySpendStaminaAndStartAction(item, action, ArenaCombatWindupData.MaxScalar, true))
                ClearWindup();
            return;
        }

        RefreshStateLabel();
    }

    private void ExecutePendingAction()
    {
        if (_pendingAction == null || _pendingActionItem == null)
        {
            ClearPendingAction();
            SetCombatantState(ArenaCombatantState.Default);
            return;
        }

        var action = _pendingAction;
        var item = _pendingActionItem;
        var activated = ArenaCombatActionRunner.TryActivate(this, item, action, _pendingWindupScalar);
        _releaseRemaining = Mathf.Max(0.05f, action.Effect?.LifetimeSeconds ?? 0.05f);
        SetCombatantState(ArenaCombatantState.Release);

        if (!activated)
        {
            ClearPendingAction();
            SetCombatantState(ArenaCombatantState.Default);
        }
    }

    private void ClearPendingAction()
    {
        _pendingActionItem = null;
        _pendingAction = null;
        _windupRemaining = 0f;
        _releaseRemaining = 0f;
        _pendingWindupScalar = 1f;
    }

    private void ClearWindup()
    {
        _windupItem = null;
        _windupAction = null;
        _windupElapsed = 0f;
        RefreshStateLabel();
    }

    private void ExhaustFromFailedAction(int staminaCost)
    {
        ClearPendingAction();
        ClearWindup();
        var recoverableMax = Mathf.Max(1, GladiatorData?.RecoverableMaxStamina ?? staminaCost);
        var recoveryThreshold = Mathf.Clamp(staminaCost, 1, recoverableMax);
        _exhaustedStaminaRecoveryThreshold = Mathf.Max(_exhaustedStaminaRecoveryThreshold, recoveryThreshold);
        _exhaustedRemainingSeconds = Mathf.Max(_exhaustedRemainingSeconds, GetMinimumExhaustedSeconds());
        SetCombatantState(ArenaCombatantState.Exhausted);
    }

    private float GetMinimumExhaustedSeconds()
    {
        var endurance = Mathf.Max(0, GladiatorData?.Level?.Endurance ?? 0);
        var reductionRatio = endurance / (endurance + EnduranceExhaustedRecoveryCurve);
        return Mathf.Lerp(BaseMinimumExhaustedSeconds, MinimumExhaustedSecondsFloor, reductionRatio);
    }

    private void UpdateExhaustedState(float delta)
    {
        if (CombatantState != ArenaCombatantState.Exhausted || delta <= 0f)
            return;

        _exhaustedRemainingSeconds = Mathf.Max(0f, _exhaustedRemainingSeconds - delta);
        TryClearExhaustedState();
    }

    private void RegenerateStamina(float delta)
    {
        if (GladiatorData == null || IsDead || delta <= 0f)
            return;

        var recoverableMaxStamina = GladiatorData.RecoverableMaxStamina;
        if (recoverableMaxStamina <= 0 || GladiatorData.Stamina >= recoverableMaxStamina)
        {
            _staminaRegenAccumulator = 0f;
            return;
        }

        _staminaRegenAccumulator += recoverableMaxStamina * StaminaRegenPerMaxStaminaPerSecond * delta;
        var restoreAmount = Mathf.FloorToInt(_staminaRegenAccumulator);
        if (restoreAmount <= 0)
            return;

        GladiatorData.RestoreStamina(restoreAmount);
        _staminaRegenAccumulator -= restoreAmount;

        TryClearExhaustedState();
    }

    private void TryClearExhaustedState()
    {
        if (CombatantState == ArenaCombatantState.Exhausted && HasRecoveredFromExhausted())
        {
            ClearExhaustedRecoveryState();
            SetCombatantState(ArenaCombatantState.Default);
        }
    }

    private bool HasRecoveredFromExhausted()
    {
        var threshold = Mathf.Max(1, _exhaustedStaminaRecoveryThreshold);
        return _exhaustedRemainingSeconds <= 0f && GladiatorData?.Stamina >= threshold;
    }

    private void ClearExhaustedRecoveryState()
    {
        _exhaustedStaminaRecoveryThreshold = 0;
        _exhaustedRemainingSeconds = 0f;
    }

    private void Refresh()
    {
        if (!IsNodeReady())
            return;

        ApplyBodyVisual();

        _nameLabel.Text = GladiatorData?.GladiatorName ?? "Gladiator";
        RefreshHealthLabel();
        RefreshStateLabel();
        _controllerLabel.Text = ControlAssignment == null
            ? "Unassigned"
            : ControlAssignment.DisplayName;
    }

    private void RefreshHealthLabel()
    {
        if (_healthLabel == null)
            return;

        _healthLabel.Text = CombatState == null
            ? string.Empty
            : $"HP {CombatState.CurrentHealth}/{CombatState.MaxHealth}";
    }

    private void RefreshStateLabel()
    {
        if (_stateLabel == null)
            return;

        _stateLabel.Visible = SaveNode.Get().DevEnabled;
        _stateLabel.Text = _windupAction?.Windup == null
            ? CombatantState.ToString()
            : $"Windup {GetWindupScalar(_windupAction):0.00}";
        _stateLabel.Modulate = CombatantState switch
        {
            ArenaCombatantState.Windup => new Color(1f, 0.72f, 0.35f),
            ArenaCombatantState.Exhausted => new Color(0.7f, 0.85f, 1f),
            _ => Colors.White
        };
    }

    private void ApplyBodyVisual()
    {
        ApplyLookVisual(_body, GladiatorData?.GetBodyForwardTexture(), GladiatorData?.GetBodyBackTexture(), DisplayHeight);
        ApplyArmorVisual();
        ApplyHandVisuals();
        ApplyHeldItemVisuals();
        RefreshDebugStateModulate();
    }

    private void RefreshDebugStateModulate()
    {
        ApplyDebugStateModulate(_body, _armor, _leftHand, _rightHand, _mainHandItem, _offHandItem);
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

    private static void ApplyHeldVisual(Sprite2D sprite, MobArena.Scripts.Resources.Items.EquipmentItemData item, float fallbackDisplayHeight, Vector2 localPosition)
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
