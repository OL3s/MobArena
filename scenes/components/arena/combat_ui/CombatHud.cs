using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scripts.Resources.Items;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.Arena.CombatUi;

public partial class CombatHud : CanvasLayer
{
    private const int PlayerCardWidth = 190;
    private const int PlayerCardHeight = 95;
    private const int EquipmentIconSize = 28;

    private HBoxContainer _playerCards;
    private PanelContainer _bossPanel;
    private Label _bossNameLabel;
    private ProgressBar _bossHealthBar;
    private Label _bossHealthLabel;
    private EnemyCombatant _boss;
    private readonly List<PlayerCardBinding> _playerBindings = new();

    public override void _Ready()
    {
        _playerCards = GetNode<HBoxContainer>("BottomMargin/PlayerCards");
        _bossPanel = GetNode<PanelContainer>("TopMargin/BossPanel");
        _bossNameLabel = GetNode<Label>("TopMargin/BossPanel/Content/BossName");
        _bossHealthBar = GetNode<ProgressBar>("TopMargin/BossPanel/Content/BossHealthBar");
        _bossHealthLabel = GetNode<Label>("TopMargin/BossPanel/Content/BossHealthLabel");
        RefreshBossPanel();
    }

    public override void _Process(double delta)
    {
        foreach (var binding in _playerBindings)
            binding.Refresh();

        RefreshBossHealth();
    }

    public override void _ExitTree()
    {
        foreach (var binding in _playerBindings)
            binding.Dispose();

        if (_boss?.CombatState != null)
            _boss.CombatState.HealthChanged -= OnBossHealthChanged;
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

    public void SetBoss(EnemyCombatant boss)
    {
        if (_boss?.CombatState != null)
            _boss.CombatState.HealthChanged -= OnBossHealthChanged;

        _boss = boss;

        if (_boss?.CombatState != null)
            _boss.CombatState.HealthChanged += OnBossHealthChanged;

        RefreshBossPanel();
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

        var nameLabel = new Label
        {
            Text = "Gladiator",
            HorizontalAlignment = HorizontalAlignment.Center,
            ClipText = true
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        content.AddChild(nameLabel);

        var healthBar = CreateProgressBar();
        content.AddChild(healthBar);

        var staminaBar = CreateProgressBar();
        staminaBar.Modulate = new Color(0.55f, 0.85f, 1f);
        content.AddChild(staminaBar);

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

        return new PlayerCardBinding(player, panel, nameLabel, healthBar, staminaBar, armorIcon, mainHandIcon, offHandIcon);
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

    private void OnBossHealthChanged(int currentHealth, int maxHealth)
    {
        RefreshBossHealth();
    }

    private void RefreshBossPanel()
    {
        if (_bossPanel == null)
            return;

        _bossPanel.Visible = _boss != null;
        if (_boss == null)
            return;

        _bossNameLabel.Text = _boss.MobData?.DisplayName ?? "Boss";
        RefreshBossHealth();
    }

    private void RefreshBossHealth()
    {
        if (_bossHealthBar == null || _bossHealthLabel == null || _boss?.CombatState == null)
            return;

        var state = _boss.CombatState;
        _bossHealthBar.MaxValue = state.MaxHealth;
        _bossHealthBar.Value = state.CurrentHealth;
        _bossHealthLabel.Text = $"{state.CurrentHealth}/{state.MaxHealth}";
    }

    private sealed class PlayerCardBinding
    {
        private readonly PlayerCombatant _player;
        private readonly Label _nameLabel;
        private readonly ProgressBar _healthBar;
        private readonly ProgressBar _staminaBar;
        private readonly TextureRect _armorIcon;
        private readonly TextureRect _mainHandIcon;
        private readonly TextureRect _offHandIcon;

        public PanelContainer Panel { get; }

        public PlayerCardBinding(
            PlayerCombatant player,
            PanelContainer panel,
            Label nameLabel,
            ProgressBar healthBar,
            ProgressBar staminaBar,
            TextureRect armorIcon,
            TextureRect mainHandIcon,
            TextureRect offHandIcon)
        {
            _player = player;
            Panel = panel;
            _nameLabel = nameLabel;
            _healthBar = healthBar;
            _staminaBar = staminaBar;
            _armorIcon = armorIcon;
            _mainHandIcon = mainHandIcon;
            _offHandIcon = offHandIcon;

            if (_player.CombatState != null)
                _player.CombatState.HealthChanged += OnHealthChanged;
        }

        public void Dispose()
        {
            if (_player?.CombatState != null)
                _player.CombatState.HealthChanged -= OnHealthChanged;
        }

        public void Refresh()
        {
            var gladiator = _player?.GladiatorData;
            _nameLabel.Text = gladiator?.GladiatorName ?? "Gladiator";

            if (_player?.CombatState != null)
            {
                _healthBar.MaxValue = _player.CombatState.MaxHealth;
                _healthBar.Value = _player.CombatState.CurrentHealth;
            }

            var maxStamina = Mathf.Max(1, gladiator?.MaxStamina ?? 1);
            _staminaBar.MaxValue = maxStamina;
            _staminaBar.Value = Mathf.Clamp(gladiator?.Stamina ?? 0, 0, maxStamina);

            SetIcon(_armorIcon, gladiator?.Equipment?.Armor);
            SetIcon(_mainHandIcon, gladiator?.Equipment?.MainHand);
            SetIcon(_offHandIcon, gladiator?.Equipment?.OffHand);
        }

        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            _healthBar.MaxValue = maxHealth;
            _healthBar.Value = currentHealth;
        }

        private static void SetIcon(TextureRect icon, ItemData item)
        {
            if (icon == null)
                return;

            icon.Texture = item?.UiIcon;
            icon.Modulate = item == null ? new Color(1f, 1f, 1f, 0.22f) : Colors.White;
        }
    }
}
