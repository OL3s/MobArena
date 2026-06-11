using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Market;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class MarketData : Resource
{
	[Export]
	public Array<ItemData> ItemStock { get; private set; } = new();

	[Export]
	public Array<GladiatorData> GladiatorStock { get; private set; } = new();

	[Export]
	public bool HasInitializedItemStock { get; private set; }

	[Export]
	public bool HasInitializedGladiatorStock { get; private set; }

    public void EnsureResources()
    {
        ItemStock ??= new Array<ItemData>();
        GladiatorStock ??= new Array<GladiatorData>();

        if (!HasInitializedItemStock)
        {
            if (ItemStock.Count <= 0)
                RefreshItemStock();
            else
                HasInitializedItemStock = true;
        }

        if (!HasInitializedGladiatorStock)
        {
            if (GladiatorStock.Count <= 0)
                RefreshGladiatorStock();
            else
                HasInitializedGladiatorStock = true;
        }
    }

    public void ExecuteNewDay()
    {
        EnsureResources();
        RefreshItemStock();
        RefreshGladiatorStock();
    }

    public void RefreshItemStock()
    {
        ItemStock = MarketItemStockGenerator.GenerateDebugAllItems();
        HasInitializedItemStock = true;
    }

    public void RefreshGladiatorStock()
    {
        GladiatorStock = MarketGladiatorStockGenerator.GenerateMarketRecruitStock();
        HasInitializedGladiatorStock = true;
    }

}
