using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class AdditiveItemCoatingData : ItemCoatingData
{
    [Export]
    public Array<CoatingDamageEntryData> DamageEntries { get; private set; } = new();

    [Export]
    public Array<CoatingEffectEntryData> EffectEntries { get; private set; } = new();
}
