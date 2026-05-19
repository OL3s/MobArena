using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources;

public partial class TownAssignmentData : Resource
{
    public enum AssignmentLocation
    {
        Courtyard,
        Arena,
        Healer,
        TrainingHall
    }

    [Export]
    public Array<GladiatorData> CourtyardGladiators { get; private set; } = new();

    [Export]
    public Array<GladiatorData> ArenaGladiators { get; private set; } = new();

    [Export]
    public Array<GladiatorData> HealerGladiators { get; private set; } = new();

    [Export]
    public Array<GladiatorData> TrainingHallGladiators { get; private set; } = new();

    public Array<GladiatorData> GetGladiators(AssignmentLocation location)
    {
        EnsureLists();
        return location switch
        {
            AssignmentLocation.Arena => ArenaGladiators,
            AssignmentLocation.Healer => HealerGladiators,
            AssignmentLocation.TrainingHall => TrainingHallGladiators,
            _ => CourtyardGladiators
        };
    }

    public bool TryMoveToLocation(GladiatorData gladiatorData, AssignmentLocation location, int capacity)
    {
        if (gladiatorData == null)
            return false;

        EnsureLists();
        var targetList = GetGladiators(location);
        if (targetList.Contains(gladiatorData))
            return true;

        if (capacity >= 0 && targetList.Count >= capacity)
            return false;

        RemoveEverywhere(gladiatorData);
        targetList.Add(gladiatorData);
        return true;
    }

    public void MoveToCourtyard(GladiatorData gladiatorData)
    {
        TryMoveToLocation(gladiatorData, AssignmentLocation.Courtyard, -1);
    }

    public void RemoveEverywhere(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return;

        EnsureLists();
        CourtyardGladiators.Remove(gladiatorData);
        ArenaGladiators.Remove(gladiatorData);
        HealerGladiators.Remove(gladiatorData);
        TrainingHallGladiators.Remove(gladiatorData);
    }

    public AssignmentLocation? GetLocation(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return null;

        EnsureLists();
        if (CourtyardGladiators.Contains(gladiatorData))
            return AssignmentLocation.Courtyard;
        if (ArenaGladiators.Contains(gladiatorData))
            return AssignmentLocation.Arena;
        if (HealerGladiators.Contains(gladiatorData))
            return AssignmentLocation.Healer;
        if (TrainingHallGladiators.Contains(gladiatorData))
            return AssignmentLocation.TrainingHall;

        return null;
    }

    public void SyncWithActiveRoster(Array<GladiatorData> activeGladiators)
    {
        EnsureLists();
        RemoveMissingGladiators(CourtyardGladiators, activeGladiators);
        RemoveMissingGladiators(ArenaGladiators, activeGladiators);
        RemoveMissingGladiators(HealerGladiators, activeGladiators);
        RemoveMissingGladiators(TrainingHallGladiators, activeGladiators);

        if (activeGladiators == null)
            return;

        foreach (var gladiator in activeGladiators)
        {
            if (gladiator != null && GetLocation(gladiator) == null)
                CourtyardGladiators.Add(gladiator);
        }
    }

    private void EnsureLists()
    {
        CourtyardGladiators ??= new Array<GladiatorData>();
        ArenaGladiators ??= new Array<GladiatorData>();
        HealerGladiators ??= new Array<GladiatorData>();
        TrainingHallGladiators ??= new Array<GladiatorData>();
    }

    private static void RemoveMissingGladiators(Array<GladiatorData> assignedGladiators, Array<GladiatorData> activeGladiators)
    {
        if (assignedGladiators == null)
            return;

        for (var index = assignedGladiators.Count - 1; index >= 0; index--)
        {
            var gladiator = assignedGladiators[index];
            if (gladiator == null || activeGladiators?.Contains(gladiator) != true)
                assignedGladiators.RemoveAt(index);
        }
    }
}
