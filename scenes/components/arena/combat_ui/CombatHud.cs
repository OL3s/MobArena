using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.Arena.CombatUi;

public partial class CombatHud : CanvasLayer
{
    private const int PlayerCardWidth = 190;
    private const int PlayerCardHeight = 95;
    private const int EquipmentIconSize = 28;

    private HBoxContainer _playerCards;
    private PanelContainer _championPanel;
    private Label _championNameLabel;
    private ProgressBar _championHealthBar;
    private Label _championHealthLabel;
    private EnemyCombatant _champion;
    private readonly List<PlayerCardBinding> _playerBindings = new();

    public override void _Ready()
    {
        _playerCards = GetNode<HBoxContainer>("BottomMargin/PlayerCards");
        _championPanel = GetNode<PanelContainer>("TopMargin/ChampionPanel");
        _championNameLabel = GetNode<Label>("TopMargin/ChampionPanel/Content/ChampionName");
        _championHealthBar = GetNode<ProgressBar>("TopMargin/ChampionPanel/Content/ChampionHealthBar");
        _championHealthLabel = GetNode<Label>("TopMargin/ChampionPanel/Content/ChampionHealthLabel");
        RefreshChampionPanel();
    }

    public override void _Process(double delta)
    {
        foreach (var binding in _playerBindings)
            binding.Refresh();

        RefreshChampionHealth();
    }

    public override void _ExitTree()
    {
        foreach (var binding in _playerBindings)
            binding.Dispose();

        if (_champion?.CombatState != null)
            _champion.CombatState.HealthChanged -= OnChampionHealthChanged;
    }

    public void SetPlayers(IEnumerable<PlayerCombatant> players)
    {
        foreach (var binding in _playerBindings)
            binding.Dispose();
        _playerBindings.Clear();

        foreach (var child in _playerCards.GetChildren())
            child.QueueFree();

        if (players == null)
            return;

        foreach (var player in players)
        {
            if (player == null)
                continue;

            var binding = CreatePlayerCard(player);
            _playerCards.AddChild(binding.Panel);
            _playerBindings.Add(binding);
            binding.Refresh();
        }
    }

    public void SetPlayers(params PlayerCombatant[] players)
    {
        SetPlayers((IEnumerable<PlayerCombatant>)players);
    }

    public void SetChampion(EnemyCombatant champion)
    {
        if (_champion?.CombatState != null)
            _champion.CombatState.HealthChanged -= OnChampionHealthChanged;

        _champion = champion;

        if (_champion?.CombatState != null)
            _champion.CombatState.HealthChanged += OnChampionHealthChanged;

        RefreshChampionPanel();
    }

    private PlayerCardBinding CreatePlayerCard(PlayerCombatant player)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(PlayerCardWidth, PlayerCardHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 4);
        margin.AddChild(content);

        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 4);
        content.AddChild(headerRow);

        var (playerBadge, playerIdLabel, deviceIcon, deviceIdLabel) = CreatePlayerBadge();
        headerRow.AddChild(playerBadge);

        var nameLabel = new Label
        {
            Text = "Gladiator",
            HorizontalAlignment = HorizontalAlignment.Center,
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        headerRow.AddChild(nameLabel);

        var healthBar = CreateProgressBar();
        content.AddChild(healthBar);

        var staminaBar = CreateProgressBar();
        staminaBar.Modulate = new Color(0.55f, 0.85f, 1f);
        content.AddChild(staminaBar);

        var stateLabel = new Label
        {
            Text = "Default",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stateLabel.AddThemeFontSizeOverride("font_size", 10);
        content.AddChild(stateLabel);

        var equipmentRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        equipmentRow.AddThemeConstantOverride("separation", 6);
        content.AddChild(equipmentRow);

        var armorIcon = CreateEquipmentIcon();
        var mainHandIcon = CreateEquipmentIcon();
        var offHandIcon = CreateEquipmentIcon();
        equipmentRow.AddChild(armorIcon);
        equipmentRow.AddChild(mainHandIcon);
        equipmentRow.AddChild(offHandIcon);

        return new PlayerCardBinding(player, panel, playerIdLabel, deviceIcon, deviceIdLabel, nameLabel, healthBar, staminaBar, stateLabel, armorIcon, mainHandIcon, offHandIcon);
    }

    private static (PanelContainer Badge, Label PlayerIdLabel, TextureRect DeviceIcon, Label DeviceIdLabel) CreatePlayerBadge()
    {
        var badge = new PanelContainer
        {
            CustomMinimumSize = new Vector2(58f, 22f),
            TooltipText = "Player id | device | device id"
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.1f, 0.12f, 0.86f),
            BorderColor = new Color(1f, 1f, 1f, 0.22f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 11,
            CornerRadiusTopRight = 11,
            CornerRadiusBottomLeft = 11,
            CornerRadiusBottomRight = 11
        };
        badge.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 7);
        margin.AddThemeConstantOverride("margin_right", 7);
        margin.AddThemeConstantOverride("margin_top", 2);
        margin.AddThemeConstantOverride("margin_bottom", 2);
        badge.AddChild(margin);

        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 3);
        margin.AddChild(row);

        var playerIdLabel = new Label
        {
            Text = "-1",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        playerIdLabel.AddThemeFontSizeOverride("font_size", 11);
        row.AddChild(playerIdLabel);

        row.AddChild(CreateBadgeSeparator());

        var deviceIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(14f, 14f),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
        };
        row.AddChild(deviceIcon);

        var deviceIdLabel = new Label
        {
            Text = "-1",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false
        };
        deviceIdLabel.AddThemeFontSizeOverride("font_size", 11);
        row.AddChild(CreateBadgeSeparator(deviceIdLabel));
        row.AddChild(deviceIdLabel);

        return (badge, playerIdLabel, deviceIcon, deviceIdLabel);
    }

    private static Label CreateBadgeSeparator(Control visibilityPeer = null)
    {
        var label = new Label
        {
            Text = "|",
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = new Color(1f, 1f, 1f, 0.45f)
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        if (visibilityPeer != null)
            label.Visible = visibilityPeer.Visible;
        return label;
    }

    private static ProgressBar CreateProgressBar()
    {
        return new ProgressBar
        {
            CustomMinimumSize = new Vector2(0f, 16f),
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            ShowPercentage = false
        };
    }

    private static TextureRect CreateEquipmentIcon()
    {
        return new TextureRect
        {
            CustomMinimumSize = new Vector2(EquipmentIconSize, EquipmentIconSize),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
        };
    }

    private void OnChampionHealthChanged(int currentHealth, int maxHealth)
    {
        RefreshChampionHealth();
    }

    private void RefreshChampionPanel()
    {
        if (_championPanel == null)
            return;

        _championPanel.Visible = _champion != null;
        if (_champion == null)
            return;

        _championNameLabel.Text = _champion.MobData?.DisplayName ?? "Champion";
        RefreshChampionHealth();
    }

    private void RefreshChampionHealth()
    {
        if (_championHealthBar == null || _championHealthLabel == null || _champion?.CombatState == null)
            return;

        var state = _champion.CombatState;
        _championHealthBar.MaxValue = state.MaxHealth;
        _championHealthBar.Value = state.CurrentHealth;
        _championHealthLabel.Text = $"{state.CurrentHealth}/{state.MaxHealth}";
    }

    private sealed class PlayerCardBinding
    {
        private readonly PlayerCombatant _player;
        private readonly Label _playerIdLabel;
        private readonly TextureRect _deviceIcon;
        private readonly Label _deviceIdLabel;
        private readonly Label _nameLabel;
        private readonly ProgressBar _healthBar;
        private readonly ProgressBar _staminaBar;
        private readonly Label _stateLabel;
        private readonly TextureRect _armorIcon;
        private readonly TextureRect _mainHandIcon;
        private readonly TextureRect _offHandIcon;

        public PanelContainer Panel { get; }

        public PlayerCardBinding(
            PlayerCombatant player,
            PanelContainer panel,
            Label playerIdLabel,
            TextureRect deviceIcon,
            Label deviceIdLabel,
            Label nameLabel,
            ProgressBar healthBar,
            ProgressBar staminaBar,
            Label stateLabel,
            TextureRect armorIcon,
            TextureRect mainHandIcon,
            TextureRect offHandIcon)
        {
            _player = player;
            Panel = panel;
            _playerIdLabel = playerIdLabel;
            _deviceIcon = deviceIcon;
            _deviceIdLabel = deviceIdLabel;
            _nameLabel = nameLabel;
            _healthBar = healthBar;
            _staminaBar = staminaBar;
            _stateLabel = stateLabel;
            _armorIcon = armorIcon;
            _mainHandIcon = mainHandIcon;
            _offHandIcon = offHandIcon;

            if (_player.CombatState != null)
                _player.CombatState.HealthChanged += OnHealthChanged;
            if (_player != null)
                _player.CombatantStateChanged += OnCombatantStateChanged;
        }

        public void Dispose()
        {
            if (_player?.CombatState != null)
                _player.CombatState.HealthChanged -= OnHealthChanged;
            if (_player != null)
                _player.CombatantStateChanged -= OnCombatantStateChanged;
        }

        public void Refresh()
        {
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
            _healthBar.MaxValue = maxHealth;
            _healthBar.Value = currentHealth;
        }

        private void OnCombatantStateChanged(ArenaCombatantState state)
        {
            RefreshStateLabel();
        }

        private void RefreshStateLabel()
        {
            if (_stateLabel == null)
                return;

            var state = _player?.CombatantState ?? ArenaCombatantState.Default;
            _stateLabel.Text = state == ArenaCombatantState.Default
                ? "Default"
                : state.ToString();
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

            var deviceId = assignment?.DeviceId ?? -1;
            _deviceIdLabel.Text = deviceId.ToString();
            _deviceIdLabel.Visible = deviceId >= 0;
            if (_deviceIdLabel.GetParent()?.GetChild(_deviceIdLabel.GetIndex() - 1) is Control separator)
                separator.Visible = _deviceIdLabel.Visible;
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
}
