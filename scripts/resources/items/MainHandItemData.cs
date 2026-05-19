using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class MainHandItemData : ItemData
{
    [Export]
    public bool IsTwoHanded { get; private set; }
}
