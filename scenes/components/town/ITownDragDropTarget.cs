using Godot;
using Godot.Collections;

namespace MobArena.Scenes.Components.Town;

public interface ITownDragDropTarget
{
    string DropTargetName { get; }

    int TownDragDropPriority { get; }

    Array<TownDragPayloadKind> AcceptedTownDragDropKinds { get; }

    bool CanReceiveTownDragDrop(TownDragPayload payload, Vector2 viewportPosition);

    bool CanPreviewTownDragDrop(TownDragPayload payload);

    void SetTownDragDropPreview(TownDragPayload? payload, Vector2 viewportPosition);

    void ReceiveTownDragDrop(TownDragPayload payload, Vector2 viewportPosition);
}

public static class TownDragDropTargetExtensions
{
    public static bool AcceptsTownDragPayloadKind(this ITownDragDropTarget target, TownDragPayload payload)
    {
        return TownDragDropRules.AcceptsKind(target.AcceptedTownDragDropKinds, payload);
    }
}
