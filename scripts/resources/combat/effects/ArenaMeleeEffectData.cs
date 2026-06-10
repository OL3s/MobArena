using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaMeleeEffectData : ArenaCombatEffectData
{
    public override string AttackTypeLabel => "Melee";

    public override string AttackTypeIconPath => "res://assets/ui/attacks/type_melee.svg";

    [Export(PropertyHint.Range, "1,200,1")]
    public float HitboxRadius { get; private set; } = 28f;

    [Export(PropertyHint.Range, "0.01,5,0.01")]
    public float ActiveSeconds { get; private set; } = 0.12f;

    [Export(PropertyHint.Range, "0,200,1")]
    public float ForwardOffset { get; private set; } = 12f;
}
