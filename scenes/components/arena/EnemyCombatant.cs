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
        _body = GetNode<Sprite2D>("Body");
        _nameLabel = GetNode<Label>("NameLabel");
        _healthLabel = GetNode<Label>("HealthLabel");
        Refresh();
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

        FitSpriteHeight(_body, MobData?.GetBodyForwardTexture(), DisplayHeight);

        _nameLabel.Text = MobData?.DisplayName ?? "Enemy";
        _healthLabel.Text = MobData == null ? string.Empty : $"HP {MobData.MaxHealth}";
    }
}
