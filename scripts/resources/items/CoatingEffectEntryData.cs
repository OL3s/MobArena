using Godot;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class CoatingEffectEntryData : Resource
{
    [Export]
    public StatusEffectType Type { get; private set; }

    [Export(PropertyHint.Range, "0,600,1")]
    public float Value { get; private set; }
}
