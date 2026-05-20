using Godot;
using Godot.Collections;

namespace MobArena.Scenes.Components.Town;

public static class TownDragDropRules
{
    public static Array<TownDragPayloadKind> GetAllAcceptedKinds()
    {
        return new Array<TownDragPayloadKind>
        {
            TownDragPayloadKind.Gladiator,
            TownDragPayloadKind.Item
        };
    }

    public static bool AcceptsKind(Array<TownDragPayloadKind> acceptedKinds, TownDragPayload payload)
    {
        return acceptedKinds?.Contains(payload.Kind) == true;
    }

    public static bool IsViewportPositionInside(Node2D node, Rect2 localBounds, Vector2 viewportPosition)
    {
        if (node == null || !node.IsVisibleInTree())
            return false;

        var worldPosition = node.GetCanvasTransform().AffineInverse() * viewportPosition;
        return localBounds.HasPoint(node.ToLocal(worldPosition));
    }

    public static string FormatDropMessage(TownDragPayload payload, string targetKind, string targetName)
    {
        return $"Town drop: {payload.Kind} '{payload.GetDebugName()}' dropped on {targetKind} '{targetName}'.";
    }
}
