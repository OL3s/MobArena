using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI.ItemShowcases;

public partial class ArmorItemShowcase : ItemStoreShowcaseBase
{
    public override void Configure(ItemData item)
    {
        ClearShowcase();
        if (item is ArmorItemData armor)
            AddArmorStats(armor);
    }
}
