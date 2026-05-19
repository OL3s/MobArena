using Godot;

namespace MobArena.Scripts.Resources;

public partial class RationStoreData : Resource
{
    public enum RationQuality
    {
        Poor,
        Common,
        Fine
    }

    [Export]
    public int PoorRationCost { get; private set; } = RationInventory.PoorRationGoldValue;

    [Export]
    public int CommonRationCost { get; private set; } = RationInventory.CommonRationGoldValue;

    [Export]
    public int FineRationCost { get; private set; } = RationInventory.FineRationGoldValue;

    [Export]
    public int DailyPoorRationStock { get; private set; } = 24;

    [Export]
    public int DailyCommonRationStock { get; private set; } = 14;

    [Export]
    public int DailyFineRationStock { get; private set; } = 8;

    [Export]
    public int PoorRationStock { get; private set; } = 24;

    [Export]
    public int CommonRationStock { get; private set; } = 14;

    [Export]
    public int FineRationStock { get; private set; } = 8;

    public void RefreshDailyStock()
    {
        PoorRationStock = Mathf.Max(0, DailyPoorRationStock);
        CommonRationStock = Mathf.Max(0, DailyCommonRationStock);
        FineRationStock = Mathf.Max(0, DailyFineRationStock);
    }

    public bool TryBuyRations(CompanyRunData companyRunData, RationQuality quality, int amount)
    {
        if (companyRunData == null || amount <= 0 || GetStock(quality) < amount)
            return false;

        var totalCost = GetCost(quality) * amount;
        if (!companyRunData.TrySpendGold(totalCost))
            return false;

        RemoveStock(quality, amount);
        AddRations(companyRunData, quality, amount);
        companyRunData.NotifyRunChanged();
        return true;
    }

    public int GetCost(RationQuality quality)
    {
        return quality switch
        {
            RationQuality.Poor => PoorRationCost,
            RationQuality.Common => CommonRationCost,
            RationQuality.Fine => FineRationCost,
            _ => 0
        };
    }

    public int GetStock(RationQuality quality)
    {
        return quality switch
        {
            RationQuality.Poor => PoorRationStock,
            RationQuality.Common => CommonRationStock,
            RationQuality.Fine => FineRationStock,
            _ => 0
        };
    }

    public float GetProvisionValue(RationQuality quality)
    {
        return quality switch
        {
            RationQuality.Poor => RationInventory.PoorRationValue,
            RationQuality.Common => RationInventory.CommonRationValue,
            RationQuality.Fine => RationInventory.FineRationValue,
            _ => 0f
        };
    }

    private void RemoveStock(RationQuality quality, int amount)
    {
        switch (quality)
        {
            case RationQuality.Poor:
                PoorRationStock = Mathf.Max(0, PoorRationStock - amount);
                break;
            case RationQuality.Common:
                CommonRationStock = Mathf.Max(0, CommonRationStock - amount);
                break;
            case RationQuality.Fine:
                FineRationStock = Mathf.Max(0, FineRationStock - amount);
                break;
        }
    }

    private static void AddRations(CompanyRunData companyRunData, RationQuality quality, int amount)
    {
        companyRunData.EnsureResources();
        switch (quality)
        {
            case RationQuality.Poor:
                companyRunData.Rations.AddPoorRations(amount);
                break;
            case RationQuality.Common:
                companyRunData.Rations.AddCommonRations(amount);
                break;
            case RationQuality.Fine:
                companyRunData.Rations.AddFineRations(amount);
                break;
        }
    }
}
