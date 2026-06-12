using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaAttackLinearProjectileData : ArenaCombatEffectData
{
    public override string AttackTypeLabel => "Linear Projectile";

    public override string AttackTypeIconPath => "res://assets/ui/attacks/type_projectile_linear.svg";

    [Export(PropertyHint.Range, "1,2000,1")]
    public float Speed { get; private set; } = 520f;

    [Export(PropertyHint.Range, "1,3000,1")]
    public float Range { get; private set; } = 520f;

    [Export(PropertyHint.Range, "1,300,1")]
    public float HitboxLength { get; private set; } = 42f;

    [Export(PropertyHint.Range, "1,120,1")]
    public float HitboxWidth { get; private set; } = 12f;

    [Export(PropertyHint.Range, "0,120,1")]
    public float VisualHeight { get; private set; } = 8f;

    [Export]
    public Texture2D VisualTexture { get; private set; }

    [Export(PropertyHint.Range, "1,200,1")]
    public float VisualDisplayHeight { get; private set; } = 18f;

    [Export]
    public Vector2 ShadowScale { get; private set; } = new(0.48f, 0.24f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ShadowAlpha { get; private set; } = 0.55f;

    [Export(PropertyHint.Range, "1,100,1")]
    public int MaxPenetrations { get; private set; } = 1;

    [Export]
    public bool BounceOffWalls { get; private set; }
}
