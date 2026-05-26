using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ArmorTypeOverrideData : Resource
{
    [Export]
    public ArmorDamageType Type { get; private set; } = ArmorDamageType.Slash;

    [Export]
    public int Value { get; private set; }
}
