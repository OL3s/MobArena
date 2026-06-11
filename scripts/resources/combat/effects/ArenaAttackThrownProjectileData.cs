using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaAttackThrownProjectileData : ArenaCombatEffectData
{
    public override string AttackTypeLabel => "Thrown Projectile";

    public override string AttackTypeIconPath => "res://assets/ui/attacks/type_projectile_thrown.svg";

    [Export(PropertyHint.Range, "1,3000,1")]
    public float Range { get; private set; } = 280f;

    [Export(PropertyHint.Range, "0.05,10,0.01")]
    public float TravelSeconds { get; private set; } = 0.55f;

    [Export(PropertyHint.Range, "0,400,1")]
    public float ArcHeight { get; private set; } = 72f;

    [Export]
    public Texture2D VisualTexture { get; private set; }

    [Export(PropertyHint.Range, "1,200,1")]
    public float VisualDisplayHeight { get; private set; } = 22f;

    [Export]
    public Vector2 GroundShadowScale { get; private set; } = new(0.42f, 0.28f);

    [Export]
    public Vector2 ApexShadowScale { get; private set; } = new(0.24f, 0.16f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float GroundShadowAlpha { get; private set; } = 0.55f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ApexShadowAlpha { get; private set; } = 0.22f;
}
