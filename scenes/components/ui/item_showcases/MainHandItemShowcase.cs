using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI.ItemShowcases;

public partial class MainHandItemShowcase : ItemStoreShowcaseBase
{
    public override void Configure(ItemData item)
    {
        ClearShowcase();
        if (item is not MainHandItemData mainHand)
            return;

        BeginStatSection("Main Hand");
        AddStat("Grip", mainHand.IsTwoHanded ? "Two-handed" : "One-handed");
        AddDamageStats(mainHand);
        AddActionStats(mainHand.MainAction);
    }
}
