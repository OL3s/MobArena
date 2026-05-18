using Godot;

namespace MobArena.Scripts.Resources;

public partial class RationInventory : Resource
{
    public const float PoorRationValue = 5f;
    public const float CommonRationValue = 8f;
    public const float FineRationValue = 10f;

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
