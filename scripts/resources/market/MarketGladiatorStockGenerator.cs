using Godot.Collections;
using MobArena.Scripts.Resources.Gladiators;

namespace MobArena.Scripts.Resources.Market;

public static class MarketGladiatorStockGenerator
{
    public static Array<GladiatorData> GenerateMarketRecruitStock(int count = 3)
    {
        var stock = new Array<GladiatorData>();
        for (var index = 0; index < count; index++)
        {
            var gladiator = GladiatorGenerator.CreateDefault();
            stock.Add(gladiator);
            GameLogger.Data($"Generated market gladiator {index + 1}/{count}: '{gladiator.GladiatorName}', value={gladiator.GetMarketValue()}, health={gladiator.Health}/{gladiator.MaxHealth}, stamina={gladiator.Stamina}/{gladiator.MaxStamina}, exhaustion={gladiator.Exhaustion:0.#}, totalLevel={gladiator.Level?.TotalLevel ?? 0}.");
        }

        GameLogger.Data($"Generated market gladiator stock. Count={stock.Count}.");
        return stock;
    }
}
