using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class StatusEffectValueOverrideData : Resource
{
    [Export]
    public StatusEffectType Type { get; private set; }

    [Export]
    public float Value { get; private set; }
}
