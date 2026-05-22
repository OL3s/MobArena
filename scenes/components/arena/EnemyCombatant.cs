using Godot;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.Components.Arena;

public partial class EnemyCombatant : ArenaCombatant
{
    private const float DisplayHeight = 88f;

    private Sprite2D _body;
    private Label _nameLabel;
    private Label _healthLabel;

    public EnemyMobData MobData { get; private set; }

    public override void _Ready()
    {
        ConfigureTopDownMotion();
        _body = GetNode<Sprite2D>("Body");
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
        ForceLookDirection(Vector2.Left);
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

        _nameLabel.Text = MobData?.DisplayName ?? "Enemy";
        _healthLabel.Text = MobData == null ? string.Empty : $"HP {MobData.MaxHealth}";
    }
}
