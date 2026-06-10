using Godot;
using MobArena.Scenes.Components.Arena;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.Arena.CombatUi;

public partial class CombatHud : CanvasLayer
{
    [Export]
    public PackedScene PlayerCardScene { get; set; }

    private HBoxContainer _playerCards;
    private PanelContainer _championPanel;
    private Label _championNameLabel;
    private ProgressBar _championHealthBar;
    private Label _championHealthLabel;
    private EnemyCombatant _champion;
    private readonly List<CombatPlayerCard> _playerBindings = new();

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
            binding.DisposeBinding();

        if (_champion?.CombatState != null)
            _champion.CombatState.HealthChanged -= OnChampionHealthChanged;
    }

    public void SetPlayers(IEnumerable<PlayerCombatant> players)
    {
        foreach (var binding in _playerBindings)
            binding.DisposeBinding();
        _playerBindings.Clear();

        foreach (var child in _playerCards.GetChildren())
            child.QueueFree();

        if (players == null)
            return;

        foreach (var player in players)
        {
            if (player == null)
                continue;

            var card = PlayerCardScene?.Instantiate<CombatPlayerCard>();
            if (card == null)
            {
                GD.PushError("Combat player card scene is missing or has the wrong root script.");
                continue;
            }

            card.Configure(player);
            _playerCards.AddChild(card);
            _playerBindings.Add(card);
            card.Refresh();
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
}
