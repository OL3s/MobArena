using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI.ItemShowcases;

public partial class OffHandItemShowcase : ItemStoreShowcaseBase
{
    public override void Configure(ItemData item)
    {
        ClearShowcase();
        if (item is not OffHandItemData offHand)
            return;

        BeginStatSection("Off Hand");
        AddStat("Slot", "Off hand");
        AddDamageStats(offHand);
        AddActionStats(offHand.MainAction);
    }
}
