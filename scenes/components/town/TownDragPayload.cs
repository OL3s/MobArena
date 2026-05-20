using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Town;

public enum TownDragPayloadKind
{
    Gladiator,
    Item
}

public readonly struct TownDragPayload
{
    public TownDragPayload(GladiatorData gladiator)
    {
        Kind = TownDragPayloadKind.Gladiator;
        Gladiator = gladiator;
        Item = null;
    }

    public TownDragPayload(ItemData item)
    {
        Kind = TownDragPayloadKind.Item;
        Gladiator = null;
        Item = item;
    }

    public TownDragPayloadKind Kind { get; }
    public GladiatorData Gladiator { get; }
    public ItemData Item { get; }

    public string GetDebugName()
    {
        return Kind switch
        {
            TownDragPayloadKind.Gladiator => Gladiator?.GladiatorName ?? "Unknown Gladiator",
            TownDragPayloadKind.Item => Item?.DisplayName ?? "Unknown Item",
            _ => "Unknown Payload"
        };
    }
}
