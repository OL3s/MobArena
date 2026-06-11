using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class MultiplierItemCoatingData : ItemCoatingData
{
    [Export]
    public Array<CoatingDamageMultiplierData> DamageMultipliers { get; private set; } = new();
}
