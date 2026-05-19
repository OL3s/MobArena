using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class MarketData : Resource
{
    private static readonly string[] DefaultItemStockPaths =
    {
        "res://resources/items/armor/cloth_wraps.tres",
        "res://resources/items/main_hand/training_sword.tres",
        "res://resources/items/main_hand/spear.tres",
        "res://resources/items/main_hand/wooden_hammer.tres",
        "res://resources/items/off_hand/wooden_buckler.tres",
        "res://resources/items/off_hand/dagger.tres"
    };

    [Export]
    public RationStoreData RationStore { get; private set; } = new();

    [Export]
    public Array<ItemData> ItemStock { get; private set; } = new();

    [Export]
    public bool HasInitializedItemStock { get; private set; }

    public void EnsureResources()
    {
        RationStore ??= new RationStoreData();
        ItemStock ??= new Array<ItemData>();
        if (!HasInitializedItemStock)
            RefreshItemStock();
    }

    public void ExecuteNewDay()
    {
        EnsureResources();
        RationStore.RefreshDailyStock();
        RefreshItemStock();
    }

    public void RefreshItemStock()
    {
        ItemStock ??= new Array<ItemData>();
        ItemStock.Clear();

        foreach (var itemPath in DefaultItemStockPaths)
        {
            var item = ItemData.LoadRuntimeCopy<ItemData>(itemPath);
            if (item != null)
                ItemStock.Add(item);
        }

        HasInitializedItemStock = true;
    }
}
