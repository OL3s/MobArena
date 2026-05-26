using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ArmorSpecialModifierData : Resource
{
    [Export]
    public ArmorSpecialType Type { get; private set; } = ArmorSpecialType.Silver;

    [Export]
    public int Value { get; private set; }
}
