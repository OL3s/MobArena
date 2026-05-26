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

	public Array<ItemData> ItemStock { get; private set; } = new();

	public Array<GladiatorData> GladiatorStock { get; private set; } = new();

	public bool HasInitializedItemStock { get; private set; }

	public bool HasInitializedGladiatorStock { get; private set; }

    public void EnsureResources()
    {
        ItemStock ??= new Array<ItemData>();
        GladiatorStock ??= new Array<GladiatorData>();
        if (!HasInitializedItemStock || HasItemStockMissingIcons())
            RefreshItemStock();
        if (!HasInitializedGladiatorStock || HasGladiatorStockMissingPortraits())
            RefreshGladiatorStock();
    }

    public void ExecuteNewDay()
    {
        EnsureResources();
        RefreshItemStock();
        RefreshGladiatorStock();
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

    public void RefreshGladiatorStock()
    {
        GladiatorStock ??= new Array<GladiatorData>();
        GladiatorStock.Clear();

        for (var index = 0; index < 3; index++)
            GladiatorStock.Add(GladiatorData.CreateDefault());

        HasInitializedGladiatorStock = true;
    }

    private bool HasItemStockMissingIcons()
    {
        if (ItemStock == null || ItemStock.Count <= 0)
            return true;

        foreach (var item in ItemStock)
        {
            if (item?.UiIcon == null)
                return true;
        }

        return false;
    }

    private bool HasGladiatorStockMissingPortraits()
    {
        if (GladiatorStock == null || GladiatorStock.Count <= 0)
            return true;

        foreach (var gladiator in GladiatorStock)
        {
            if (gladiator == null || gladiator.GetUiIconTexture() == null)
                return true;
        }

        return false;
    }
}
