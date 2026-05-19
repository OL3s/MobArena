using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Town;

public enum TownDragPayloadKind
{
    Gladiator,
    Item,
    Ration
}

public readonly struct TownDragPayload
{
    public TownDragPayload(GladiatorData gladiator)
    {
        Kind = TownDragPayloadKind.Gladiator;
        Gladiator = gladiator;
        Item = null;
        RationQuality = null;
    }

    public TownDragPayload(ItemData item)
    {
        Kind = TownDragPayloadKind.Item;
        Gladiator = null;
        Item = item;
        RationQuality = null;
    }

    public TownDragPayload(RationStoreData.RationQuality rationQuality)
    {
        Kind = TownDragPayloadKind.Ration;
        Gladiator = null;
        Item = null;
        RationQuality = rationQuality;
    }

    public TownDragPayloadKind Kind { get; }
    public GladiatorData Gladiator { get; }
    public ItemData Item { get; }
    public RationStoreData.RationQuality? RationQuality { get; }

    public string GetDebugName()
    {
        return Kind switch
        {
            TownDragPayloadKind.Gladiator => Gladiator?.GladiatorName ?? "Unknown Gladiator",
            TownDragPayloadKind.Item => Item?.DisplayName ?? "Unknown Item",
            TownDragPayloadKind.Ration => $"{RationQuality?.ToString() ?? "Unknown"} Ration",
            _ => "Unknown Payload"
        };
    }
}
