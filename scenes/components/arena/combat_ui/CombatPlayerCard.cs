using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Arena.CombatUi;

public partial class CombatPlayerCard : PanelContainer
{
    private Label _playerIdLabel;
    private TextureRect _deviceIcon;
    private Label _nameLabel;
    private ProgressBar _healthBar;
    private ProgressBar _staminaBar;
    private Label _stateLabel;
    private TextureRect _armorIcon;
    private TextureRect _mainHandIcon;
    private TextureRect _offHandIcon;
    private PlayerCombatant _player;
    private ArenaCombatState _subscribedCombatState;
    private PlayerCombatant _subscribedPlayer;
    private bool _disposed;

    public override void _Ready()
    {
        _playerIdLabel = GetNode<Label>("MarginContainer/Layout/HeaderRow/PlayerBadge/PlayerCircle/MarginContainer/PlayerIdLabel");
        _deviceIcon = GetNode<TextureRect>("MarginContainer/Layout/HeaderRow/PlayerBadge/DeviceIcon");
        _nameLabel = GetNode<Label>("MarginContainer/Layout/HeaderRow/NameLabel");
        _healthBar = GetNode<ProgressBar>("MarginContainer/Layout/HealthBar");
        _staminaBar = GetNode<ProgressBar>("MarginContainer/Layout/StaminaBar");
        _stateLabel = GetNode<Label>("MarginContainer/Layout/StateLabel");
        _armorIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/ArmorIcon");
        _mainHandIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/MainHandIcon");
        _offHandIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/OffHandIcon");
        Refresh();
    }

    public override void _ExitTree()
    {
        DisposeBinding();
    }

    public void Configure(PlayerCombatant player)
    {
        DisposeBinding();
        _disposed = false;
        _player = player;
        _subscribedCombatState = _player?.CombatState;
        _subscribedPlayer = _player;

        if (_subscribedCombatState != null)
            _subscribedCombatState.HealthChanged += OnHealthChanged;
        if (_subscribedPlayer != null)
            _subscribedPlayer.CombatantStateChanged += OnCombatantStateChanged;

        Refresh();
    }

    public void DisposeBinding()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_subscribedCombatState != null)
            _subscribedCombatState.HealthChanged -= OnHealthChanged;
        if (_subscribedPlayer != null && GodotObject.IsInstanceValid(_subscribedPlayer))
            _subscribedPlayer.CombatantStateChanged -= OnCombatantStateChanged;

        _subscribedCombatState = null;
        _subscribedPlayer = null;
    }

    public void Refresh()
    {
        if (_disposed || !IsNodeReady())
            return;

        var gladiator = _player?.GladiatorData;
        RefreshPlayerBadge(_player?.ControlAssignment);
        _nameLabel.Text = gladiator?.GladiatorName ?? "Gladiator";

        if (_player?.CombatState != null)
        {
            _healthBar.MaxValue = _player.CombatState.MaxHealth;
            _healthBar.Value = _player.CombatState.CurrentHealth;
        }

        var maxStamina = Mathf.Max(1, gladiator?.MaxStamina ?? 1);
        _staminaBar.MaxValue = maxStamina;
        _staminaBar.Value = Mathf.Clamp(gladiator?.Stamina ?? 0, 0, maxStamina);
        RefreshStateLabel();

        SetIcon(_armorIcon, gladiator?.Equipment?.Armor);
        SetIcon(_mainHandIcon, gladiator?.Equipment?.MainHand);
        SetIcon(_offHandIcon, gladiator?.Equipment?.OffHand);
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (_disposed || !IsNodeReady())
            return;

        _healthBar.MaxValue = maxHealth;
        _healthBar.Value = currentHealth;
    }

    private void OnCombatantStateChanged(ArenaCombatantState state)
    {
        if (!_disposed)
            RefreshStateLabel();
    }

    private void RefreshStateLabel()
    {
        if (_stateLabel == null)
            return;

        var state = _player?.CombatantState ?? ArenaCombatantState.Default;
        _stateLabel.Text = state == ArenaCombatantState.Default ? "Default" : state.ToString();
        _stateLabel.Modulate = state switch
        {
            ArenaCombatantState.Windup => new Color(1f, 0.72f, 0.35f),
            ArenaCombatantState.Exhausted => new Color(0.7f, 0.85f, 1f),
            _ => Colors.White
        };
    }

    private static void SetIcon(TextureRect icon, ItemData item)
    {
        if (icon == null)
            return;

        icon.Texture = item?.UiIcon;
        icon.Modulate = item == null ? new Color(1f, 1f, 1f, 0.22f) : Colors.White;
    }

    private void RefreshPlayerBadge(ArenaControlAssignmentData assignment)
    {
        _playerIdLabel.Text = (assignment?.PlayerId ?? -1).ToString();
        _deviceIcon.Texture = GetControllerIcon(assignment);
        _deviceIcon.Modulate = _deviceIcon.Texture == null ? new Color(1f, 1f, 1f, 0.2f) : Colors.White;
    }

    private static Texture2D GetControllerIcon(ArenaControlAssignmentData assignment)
    {
        var inputConfig = LocalInputConfig.Get();
        return assignment?.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => inputConfig?.EnterIcon,
            LocalInputControllerConfig.ControllerKind.Mouse => inputConfig?.MouseIcon,
            LocalInputControllerConfig.ControllerKind.Touch => inputConfig?.PhoneIcon,
            LocalInputControllerConfig.ControllerKind.Gamepad => inputConfig?.XboxAIcon,
            _ => null
        };
    }
}
