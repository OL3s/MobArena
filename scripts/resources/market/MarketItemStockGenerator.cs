using Godot.Collections;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources.Market;

public static class MarketItemStockGenerator
{
    public static Array<ItemData> GenerateDebugAllItems()
    {
        var stock = new Array<ItemData>();
        foreach (var itemPath in MarketItemCatalog.DebugAllItemStockPaths)
        {
            var item = ItemData.LoadRuntimeCopy<ItemData>(itemPath);
            if (item != null)
                stock.Add(item);
            else
                GameLogger.Warning(GameLogCategory.Data, $"Market item stock skipped missing item template '{itemPath}'.");
        }

        GameLogger.Data($"Generated debug market item stock. Count={stock.Count}.");
        return stock;
    }
}
