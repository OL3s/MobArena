using Godot;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.Components.Arena;

public partial class EnemyCombatant : ArenaCombatant
{
    private const float DisplayHeight = 88f;
    private const float HandDisplayHeight = 16f;

    private Sprite2D _body;
    private Sprite2D _leftHand;
    private Sprite2D _rightHand;
    private Label _nameLabel;
    private Label _healthLabel;

    public EnemyMobData MobData { get; private set; }

    public override void _Ready()
    {
        ConfigureTopDownMotion();
        _body = GetNode<Sprite2D>("Body");
        _leftHand = GetNode<Sprite2D>("LeftHand");
        _rightHand = GetNode<Sprite2D>("RightHand");
        _nameLabel = GetNode<Label>("NameLabel");
        _healthLabel = GetNode<Label>("HealthLabel");
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveWithSoftCollisionOnly();
    }

    public void ConfigureEnemy(EnemyMobData mobData)
    {
        MobData = mobData;
        Name = string.IsNullOrWhiteSpace(mobData?.DisplayName)
            ? "EnemyCombatant"
            : $"{mobData.DisplayName}EnemyCombatant";

        Refresh();
    }

    private void Refresh()
    {
        if (!IsNodeReady())
            return;

        ApplyLookVisual(_body, MobData?.GetBodyForwardTexture(), MobData?.GetBodyBackTexture(), DisplayHeight);
        ApplyHandVisuals();

        _nameLabel.Text = MobData?.DisplayName ?? "Enemy";
        _healthLabel.Text = MobData == null ? string.Empty : $"HP {MobData.MaxHealth}";
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

    private void ApplyHandDrawOrder()
    {
        var handZIndex = LookDirection.Y < 0f ? -2 : 1;
        if (_leftHand != null)
            _leftHand.ZIndex = handZIndex;
        if (_rightHand != null)
            _rightHand.ZIndex = handZIndex;
    }
}
