using Godot;

namespace MobArena.Scripts.Resources;

public partial class MarketData : Resource
{
    [Export]
    public RationStoreData RationStore { get; private set; } = new();

    public void EnsureResources()
    {
        RationStore ??= new RationStoreData();
    }

    public void ExecuteNewDay()
    {
        EnsureResources();
        RationStore.RefreshDailyStock();
    }
}
