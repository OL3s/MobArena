using Godot;

namespace MobArena.Scripts.Resources;

public partial class RationInventory : Resource
{
    public const float PoorRationValue = 6f;
    public const float CommonRationValue = 8f;
    public const float FineRationValue = 10f;
    public const int PoorRationGoldValue = 2;
    public const int CommonRationGoldValue = 4;
    public const int FineRationGoldValue = 7;

    [Export]
    public int PoorRations { get; private set; }

    [Export]
    public int CommonRations { get; private set; }

    [Export]
    public int FineRations { get; private set; }

    [Export]
    public float RationConsumptionProgress { get; private set; }

    public int GetTotal()
    {
        return PoorRations + CommonRations + FineRations;
    }

    public void AddPoorRations(int amount)
    {
        if (amount <= 0)
            return;

        PoorRations += amount;
    }

    public void AddCommonRations(int amount)
    {
        if (amount <= 0)
            return;

        CommonRations += amount;
    }

    public void AddFineRations(int amount)
    {
        if (amount <= 0)
            return;

        FineRations += amount;
    }

    public int AddConsumptionProgress(float rationUnits)
    {
        if (rationUnits <= 0f)
            return 0;

        RationConsumptionProgress += rationUnits;
        var rationCount = Mathf.FloorToInt(RationConsumptionProgress);
        if (rationCount <= 0)
            return 0;

        var consumed = ConsumeRations(rationCount);
        RationConsumptionProgress -= consumed;

        if (GetTotal() <= 0)
            RationConsumptionProgress = 0f;

        return consumed;
    }

    public bool TryConsumeNextRation(out float provisionValue, out string rationName)
    {
        if (PoorRations > 0)
        {
            PoorRations--;
            provisionValue = PoorRationValue;
            rationName = "poor";
            return true;
        }

        if (CommonRations > 0)
        {
            CommonRations--;
            provisionValue = CommonRationValue;
            rationName = "common";
            return true;
        }

        if (FineRations > 0)
        {
            FineRations--;
            provisionValue = FineRationValue;
            rationName = "fine";
            return true;
        }

        provisionValue = 0f;
        rationName = string.Empty;
        return false;
    }

    public bool TryConsumeRation(RationStoreData.RationQuality quality, out float provisionValue)
    {
        switch (quality)
        {
            case RationStoreData.RationQuality.Poor when PoorRations > 0:
                PoorRations--;
                provisionValue = PoorRationValue;
                return true;
            case RationStoreData.RationQuality.Common when CommonRations > 0:
                CommonRations--;
                provisionValue = CommonRationValue;
                return true;
            case RationStoreData.RationQuality.Fine when FineRations > 0:
                FineRations--;
                provisionValue = FineRationValue;
                return true;
            default:
                provisionValue = 0f;
                return false;
        }
    }

    public bool TryRemoveRation(RationStoreData.RationQuality quality)
    {
        switch (quality)
        {
            case RationStoreData.RationQuality.Poor when PoorRations > 0:
                PoorRations--;
                return true;
            case RationStoreData.RationQuality.Common when CommonRations > 0:
                CommonRations--;
                return true;
            case RationStoreData.RationQuality.Fine when FineRations > 0:
                FineRations--;
                return true;
            default:
                return false;
        }
    }

    public int GetCount(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => PoorRations,
            RationStoreData.RationQuality.Common => CommonRations,
            RationStoreData.RationQuality.Fine => FineRations,
            _ => 0
        };
    }

    public static int GetGoldValue(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => PoorRationGoldValue,
            RationStoreData.RationQuality.Common => CommonRationGoldValue,
            RationStoreData.RationQuality.Fine => FineRationGoldValue,
            _ => 0
        };
    }

    public static int GetMarketSaleValue(RationStoreData.RationQuality quality)
    {
        return Mathf.Max(1, GetGoldValue(quality) / 2);
    }

    private int ConsumeRations(int amount)
    {
        if (amount <= 0)
            return 0;

        var remaining = amount;
        var poorUsed = Mathf.Min(PoorRations, remaining);
        PoorRations -= poorUsed;
        remaining -= poorUsed;

        var commonUsed = Mathf.Min(CommonRations, remaining);
        CommonRations -= commonUsed;
        remaining -= commonUsed;

        var fineUsed = Mathf.Min(FineRations, remaining);
        FineRations -= fineUsed;
        remaining -= fineUsed;

        return amount - remaining;
    }
}
