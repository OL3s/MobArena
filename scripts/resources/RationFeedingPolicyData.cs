using Godot;

namespace MobArena.Scripts.Resources;

public partial class RationFeedingPolicyData : Resource
{
    public enum FeedPriority
    {
        ClosestFit,
        CheapestFirst,
        BestFirst
    }

    public const float PoorFeedBelowMax = RationInventory.PoorRationValue - 0.5f;
    public const float CommonFeedBelowMax = RationInventory.CommonRationValue - 0.5f;
    public const float FineFeedBelowMax = RationInventory.FineRationValue * 0.9f;

    [Export]
    public float PoorFeedBelow { get; private set; } = PoorFeedBelowMax;

    [Export]
    public float CommonFeedBelow { get; private set; } = CommonFeedBelowMax;

    [Export]
    public float FineFeedBelow { get; private set; } = FineFeedBelowMax;

    [Export]
    public bool Enabled { get; private set; }

    [Export]
    public FeedPriority Priority { get; private set; } = FeedPriority.ClosestFit;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public void SetFeedBelow(RationStoreData.RationQuality quality, float value)
    {
        switch (quality)
        {
            case RationStoreData.RationQuality.Poor:
                PoorFeedBelow = Mathf.Clamp(value, 0f, PoorFeedBelowMax);
                break;
            case RationStoreData.RationQuality.Common:
                CommonFeedBelow = Mathf.Clamp(value, 0f, CommonFeedBelowMax);
                break;
            case RationStoreData.RationQuality.Fine:
                FineFeedBelow = Mathf.Clamp(value, 0f, FineFeedBelowMax);
                break;
        }
    }

    public float GetFeedBelow(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => PoorFeedBelow,
            RationStoreData.RationQuality.Common => CommonFeedBelow,
            RationStoreData.RationQuality.Fine => FineFeedBelow,
            _ => 0f
        };
    }

    public float GetFeedBelowMax(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => PoorFeedBelowMax,
            RationStoreData.RationQuality.Common => CommonFeedBelowMax,
            RationStoreData.RationQuality.Fine => FineFeedBelowMax,
            _ => 0f
        };
    }

    public void SetPriority(FeedPriority priority)
    {
        Priority = priority;
    }

    public void ClampValues()
    {
        SetFeedBelow(RationStoreData.RationQuality.Poor, PoorFeedBelow);
        SetFeedBelow(RationStoreData.RationQuality.Common, CommonFeedBelow);
        SetFeedBelow(RationStoreData.RationQuality.Fine, FineFeedBelow);
    }
}
