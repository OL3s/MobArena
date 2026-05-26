using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class MainHandItemData : DamageItemData
{
    [Export]
    public bool IsTwoHanded { get; private set; }
}
