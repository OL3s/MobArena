using Godot;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.Components.Arena;

public partial class EnemyCombatant : ArenaCombatant
{
    private const string DefaultStatusProfilePath = "res://resources/combat/status_profiles/default_mob_status_profile.tres";
    private const string DefaultChampionStatusProfilePath = "res://resources/combat/status_profiles/default_champion_status_profile.tres";
    private const float DisplayHeight = 88f;
    private const float HandDisplayHeight = 16f;

    private Sprite2D _body;
    private Sprite2D _leftHand;
    private Sprite2D _rightHand;
    private Label _nameLabel;
    private Label _healthLabel;
    private Label _stateLabel;

    public EnemyMobData MobData { get; private set; }

    public override void _Ready()
    {
        ConfigureTopDownMotion();
        _body = GetNode<Sprite2D>("Body");
        _leftHand = GetNode<Sprite2D>("LeftHand");
        _rightHand = GetNode<Sprite2D>("RightHand");
        _nameLabel = GetNode<Label>("NameLabel");
        _healthLabel = GetNode<Label>("HealthLabel");
        _stateLabel = GetNode<Label>("StateLabel");
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveWithSoftCollisionOnly();
        var deltaSeconds = (float)delta;
        DecayExternalForce(deltaSeconds);
        TickCombatantStatusEffects(deltaSeconds);
    }

    public void ConfigureEnemy(EnemyMobData mobData)
    {
        MobData = mobData;
        Name = string.IsNullOrWhiteSpace(mobData?.DisplayName)
            ? "EnemyCombatant"
            : $"{mobData.DisplayName}EnemyCombatant";

        ConfigureCombatState(CreateCombatState(mobData), ArenaCombatTeam.Enemy);

        Refresh();
    }

    private static ArenaCombatState CreateCombatState(EnemyMobData mobData)
    {
        var combatState = new ArenaCombatState();
        combatState.Configure(
            Mathf.Max(1, mobData?.MaxHealth ?? 1),
            mobData?.MaxHealth ?? 1,
            mobData?.ArmorProfile,
            GetStatusProfile(mobData));
        return combatState;
    }

    private static CombatantStatusProfileData GetStatusProfile(EnemyMobData mobData)
    {
        if (mobData?.StatusProfile != null)
            return mobData.StatusProfile;

        var fallbackPath = mobData is ChampionMobData
            ? DefaultChampionStatusProfilePath
            : DefaultStatusProfilePath;
        return ResourceLoader.Load<CombatantStatusProfileData>(fallbackPath);
    }

    protected override void OnCombatStateHealthChanged(int currentHealth, int maxHealth)
    {
        RefreshHealthLabel();
    }

    protected override void OnCombatantStateChanged(ArenaCombatantState state)
    {
        RefreshStateLabel();
        RefreshDebugStateModulate();
    }

    private void Refresh()
    {
        if (!IsNodeReady())
            return;

        ApplyLookVisual(_body, MobData?.GetBodyForwardTexture(), MobData?.GetBodyBackTexture(), DisplayHeight);
        ApplyHandVisuals();
        RefreshDebugStateModulate();

        _nameLabel.Text = MobData?.DisplayName ?? "Enemy";
        RefreshHealthLabel();
        RefreshStateLabel();
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

        _stateLabel.Visible = MobArena.Scripts.SaveNode.Get().DevEnabled;
        _stateLabel.Text = CombatantState.ToString();
        _stateLabel.Modulate = CombatantState == ArenaCombatantState.Windup
            ? new Color(1f, 0.72f, 0.35f)
            : Colors.White;
    }

    private void ApplyHandVisuals()
    {
        if (MobData?.UsesSeparatedHands() != true)
        {
            _leftHand?.Hide();
            _rightHand?.Hide();
            return;
        }

        var handTexture = MobData.GetHandTexture();
        ApplyDirectionalVisual(_leftHand, handTexture, HandDisplayHeight, new Vector2(-24f, -16f));
        ApplyDirectionalVisual(_rightHand, handTexture, HandDisplayHeight, new Vector2(24f, -16f));
        ApplyHandDrawOrder();
    }

    private void RefreshDebugStateModulate()
    {
        ApplyDebugStateModulate(_body, _leftHand, _rightHand);
    }

    private void ApplyHandDrawOrder()
    {
        var handZIndex = LookDirection.Y < 0f ? -2 : 1;
        if (_leftHand != null)
            _leftHand.ZIndex = handZIndex;
        if (_rightHand != null)
            _rightHand.ZIndex = handZIndex;
    }
}
