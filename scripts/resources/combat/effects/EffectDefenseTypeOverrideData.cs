using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class EffectDefenseTypeOverrideData : Resource
{
    [Export]
    public StatusEffectType Type { get; private set; }

    [Export]
    public int Value { get; private set; }
}
