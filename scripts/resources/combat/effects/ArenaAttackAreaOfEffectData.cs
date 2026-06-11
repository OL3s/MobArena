using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaAttackAreaOfEffectData : ArenaCombatEffectData
{
    public override string AttackTypeLabel => "Area Of Effect";

    public override string AttackTypeIconPath => "res://assets/ui/attacks/type_area_of_effect.svg";

    [Export(PropertyHint.Range, "1,500,1")]
    public float Radius { get; private set; } = 64f;

    [Export(PropertyHint.Range, "0.01,10,0.01")]
    public float TickSeconds { get; private set; } = 0.5f;

    [Export]
    public bool UnlimitedHits { get; private set; } = true;

    [Export]
    public Color FillColor { get; private set; } = new(0.2f, 0.85f, 0.35f, 0.24f);

    [Export]
    public Color OutlineColor { get; private set; } = new(0.45f, 1f, 0.55f, 0.78f);
}
