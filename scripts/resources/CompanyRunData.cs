using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Items;
using System.Linq;

namespace MobArena.Scripts.Resources;

public partial class CompanyRunData : Resource
{
    private const float ConditionWarningThreshold = 5f;

    [Signal]
    public delegate void RunChangedEventHandler();

    [Signal]
    public delegate void GladiatorDiedEventHandler(GladiatorData gladiatorData);

    [Export]
    public int Gold { get; private set; } = 100;

    [Export]
    public int Fame { get; private set; }

    [Export]
    public Array<GladiatorData> Gladiators { get; private set; } = new();

    [Export]
    public Array<GladiatorData> Cemetery { get; private set; } = new();

    [Export]
    public RationInventory Rations { get; private set; } = new();

    [Export]
    public Array<ItemData> Inventory { get; private set; } = new();

    [Export]
    public MarketData Market { get; private set; } = new();

    [Export]
    public RationFeedingPolicyData RationFeedingPolicy { get; private set; } = new();

    [Export]
    public TownAssignmentData TownAssignments { get; private set; } = new();

    [Export]
    public Array<ArenaControlAssignmentData> ArenaControlAssignments { get; private set; } = new();

    public int AliveGladiators => Gladiators.Count;

    [Export]
    public int MobsKilled { get; private set; }

    public void AddGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        if (gladiatorData == null)
            return;

        Gladiators.Add(gladiatorData);
        EnsureResources();
        TownAssignments.MoveToCourtyard(gladiatorData);
        careerData?.AddGladiator();
        GD.Print($"CompanyRunData: Added gladiator '{gladiatorData.GladiatorName}'. Active gladiators: {Gladiators.Count}.");
        EmitSignal(SignalName.RunChanged);
    }

    public void AddDefaultGladiators(CompanyCareerData careerData, int count)
    {
        if (count <= 0)
            return;

        for (var index = 0; index < count; index++)
        {
            AddGladiator(GladiatorData.CreateDefault(), careerData);
        }
    }

    public void AddStartingRations()
    {
        EnsureResources();
        Rations.AddPoorRations(2);
        Rations.AddCommonRations(1);
        EmitSignal(SignalName.RunChanged);
    }

    public void AddGold(int amount, CompanyCareerData careerData)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        careerData?.AddGoldEarned(amount);
        EmitSignal(SignalName.RunChanged);
    }

    public void AddFame(int amount)
    {
        if (amount <= 0)
            return;

        Fame += amount;
        EmitSignal(SignalName.RunChanged);
    }

    public void LoseFame(int amount)
    {
        if (amount <= 0)
            return;

        Fame = Mathf.Max(Fame - amount, 0);
        EmitSignal(SignalName.RunChanged);
    }

    public bool TrySpendFame(int amount)
    {
        if (amount <= 0)
            return true;

        if (Fame < amount)
            return false;

        Fame -= amount;
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
            return false;

        Gold -= amount;
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public void AddItem(ItemData item)
    {
        if (item == null)
            return;

        EnsureResources();
        Inventory.Add(item);
        EmitSignal(SignalName.RunChanged);
    }

    public bool RemoveItem(ItemData item)
    {
        if (item == null || Inventory == null)
            return false;

        var removed = Inventory.Remove(item);
        if (removed)
            EmitSignal(SignalName.RunChanged);

        return removed;
    }

    public bool HasItem(ItemData item)
    {
        return item != null && Inventory?.Contains(item) == true;
    }

    public bool HasGladiator(GladiatorData gladiatorData)
    {
        return gladiatorData != null && Gladiators?.Contains(gladiatorData) == true;
    }

    public bool RemoveGladiator(GladiatorData gladiatorData)
    {
        if (gladiatorData == null || Gladiators == null)
            return false;

        var removed = Gladiators.Remove(gladiatorData);
        if (removed)
            TownAssignments?.RemoveEverywhere(gladiatorData);
        if (removed)
            EmitSignal(SignalName.RunChanged);

        return removed;
    }

    public int ReturnGladiatorEquipmentToInventory(GladiatorData gladiatorData)
    {
        var returnedCount = ReturnGladiatorEquipmentToInventory(gladiatorData, true);
        return returnedCount;
    }

    private int ReturnGladiatorEquipmentToInventory(GladiatorData gladiatorData, bool emitChanged)
    {
        EnsureResources();
        var equipment = gladiatorData?.Equipment;
        if (equipment == null)
            return 0;

        var returnedCount = 0;
        returnedCount += ReturnEquippedItemToInventory(equipment.MainHand, gladiatorData, "main hand");
        returnedCount += ReturnEquippedItemToInventory(equipment.Armor, gladiatorData, "armor");
        returnedCount += ReturnEquippedItemToInventory(equipment.OffHand, gladiatorData, "off hand");

        equipment.UnequipMainHand();
        equipment.UnequipArmor();
        equipment.UnequipOffHand();

        if (returnedCount > 0 && emitChanged)
            EmitSignal(SignalName.RunChanged);

        return returnedCount;
    }

    private int ReturnEquippedItemToInventory(ItemData item, GladiatorData gladiatorData, string slotName)
    {
        if (item == null)
            return 0;

        if (Inventory.Contains(item))
        {
            GD.PushError($"Return equipment failed: {slotName} item '{item.DisplayName}' from gladiator '{gladiatorData?.GladiatorName ?? "null"}' is already in company inventory.");
            return 0;
        }

        Inventory.Add(item);
        return 1;
    }

    public bool TryBuyItem(ItemData item, int price)
    {
        if (item == null || !TrySpendGold(price))
            return false;

        EnsureResources();
        item.ApplyPurchasedValue();
        Inventory.Add(item);
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public bool TryBuyItem(ItemData item)
    {
        return TryBuyItem(item, item?.Cost ?? 0);
    }

    public bool TryBuyGladiator(GladiatorData gladiatorData, CompanyCareerData careerData, int price)
    {
        if (gladiatorData == null || !TrySpendGold(price))
            return false;

        gladiatorData.ApplyPurchasedValue();
        AddGladiator(gladiatorData, careerData);
        return true;
    }

    public bool TryBuyGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        return TryBuyGladiator(gladiatorData, careerData, gladiatorData?.InitialCost ?? 0);
    }

    public int GetSaleValue(ItemData item)
    {
        return Mathf.Max(0, item?.Cost ?? 0);
    }

    public int GetSaleValue(GladiatorData gladiatorData)
    {
        return gladiatorData?.GetMarketSaleValue() ?? 0;
    }

    public int GetSaleValue(RationStoreData.RationQuality quality)
    {
        return RationInventory.GetMarketSaleValue(quality);
    }

    public bool TrySellItem(ItemData item, CompanyCareerData careerData)
    {
        var saleValue = GetSaleValue(item);
        if (saleValue <= 0)
        {
            GD.PushError($"Drop sell failed: item '{item?.DisplayName ?? "null"}' has no sale value.");
            return false;
        }

        if (!HasItem(item))
        {
            GD.PushError($"Drop sell failed: item '{item?.DisplayName ?? "null"}' is not in company inventory.");
            return false;
        }

        if (!RemoveItem(item))
            return false;

        AddGold(saleValue, careerData);
        return true;
    }

    public bool TrySellGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        var saleValue = GetSaleValue(gladiatorData);
        if (saleValue <= 0)
        {
            GD.PushError($"Drop sell failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' has no sale value.");
            return false;
        }

        if (!HasGladiator(gladiatorData))
        {
            GD.PushError($"Drop sell failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' is not in the active roster.");
            return false;
        }

        ReturnGladiatorEquipmentToInventory(gladiatorData, false);
        TownAssignments?.RemoveEverywhere(gladiatorData);
        if (!RemoveGladiator(gladiatorData))
            return false;

        AddGold(saleValue, careerData);
        return true;
    }

    public bool TrySellRation(RationStoreData.RationQuality quality, CompanyCareerData careerData)
    {
        EnsureResources();
        var saleValue = GetSaleValue(quality);
        if (saleValue <= 0)
        {
            GD.PushError($"Drop sell failed: {quality} ration has no sale value.");
            return false;
        }

        if (Rations.GetCount(quality) <= 0)
        {
            GD.PushError($"Drop sell failed: company inventory has no {quality} rations.");
            return false;
        }

        if (!Rations.TryRemoveRation(quality))
            return false;

        AddGold(saleValue, careerData);
        return true;
    }

    public bool TryFeedGladiatorRation(GladiatorData gladiatorData, RationStoreData.RationQuality quality)
    {
        EnsureResources();
        if (!HasGladiator(gladiatorData))
        {
            GD.PushError($"Drop feed failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' is not in the active roster.");
            return false;
        }

        var provisionValue = RationStoreData.GetProvisionValue(quality);
        if (gladiatorData.Provisions >= provisionValue)
        {
            GD.Print($"Drop feed skipped: gladiator '{gladiatorData.GladiatorName}' already has {gladiatorData.Provisions:0.0}/{provisionValue:0.0} provisions for {quality} ration.");
            return false;
        }

        if (Rations.GetCount(quality) <= 0)
        {
            GD.PushError($"Drop feed failed: company inventory has no {quality} rations.");
            return false;
        }

        if (!Rations.TryConsumeRation(quality, out _))
        {
            GD.PushError($"Drop feed failed: could not consume {quality} ration despite positive inventory count.");
            return false;
        }

        gladiatorData.SetProvisions(provisionValue);
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public bool TryAssignGladiatorToTownLocation(GladiatorData gladiatorData, TownAssignmentData.AssignmentLocation location, int capacity)
    {
        EnsureResources();
        if (!HasGladiator(gladiatorData))
        {
            GD.PushError($"Town assignment failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' is not in the active roster.");
            return false;
        }

        if (!TownAssignments.TryMoveToLocation(gladiatorData, location, capacity))
        {
            var assignedCount = TownAssignments.GetGladiators(location).Count;
            GD.PushError($"Town assignment failed: could not move gladiator '{gladiatorData.GladiatorName}' to {location} ({assignedCount}/{capacity}).");
            return false;
        }

        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public bool TryMoveGladiatorToCourtyard(GladiatorData gladiatorData)
    {
        EnsureResources();
        if (!HasGladiator(gladiatorData))
        {
            GD.PushError($"Town assignment failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' cannot move to courtyard because they are not in the active roster.");
            return false;
        }

        TownAssignments.MoveToCourtyard(gladiatorData);
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public void RemoveGladiatorFromTownAssignments(GladiatorData gladiatorData)
    {
        EnsureResources();
        TownAssignments.RemoveEverywhere(gladiatorData);
        EmitSignal(SignalName.RunChanged);
    }

    public void AddMobKilled(CompanyCareerData careerData, int amount = 1)
    {
        if (amount <= 0)
            return;

        MobsKilled += amount;
        careerData?.AddMobsKilled(amount);
        EmitSignal(SignalName.RunChanged);
    }

    public void NotifyRunChanged()
    {
        EmitSignal(SignalName.RunChanged);
    }

    public ArenaControlAssignmentData GetArenaControlAssignment(GladiatorData gladiatorData)
    {
        EnsureResources();
        if (gladiatorData == null)
            return null;

        foreach (var assignment in ArenaControlAssignments)
        {
            if (assignment?.Gladiator == gladiatorData)
                return assignment;
        }

        return null;
    }

    public bool TrySetArenaControlAssignment(GladiatorData gladiatorData, LocalInputControllerConfig controllerSetup)
    {
        EnsureResources();
        if (gladiatorData == null || controllerSetup == null)
            return false;

        if (!HasGladiator(gladiatorData) || TownAssignments.GetLocation(gladiatorData) != TownAssignmentData.AssignmentLocation.Arena)
        {
            GD.PushError($"Arena control assignment failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' is not assigned to the Arena building.");
            return false;
        }

        var controllerKey = ArenaControlAssignmentData.GetControllerKey(controllerSetup);
        for (var index = ArenaControlAssignments.Count - 1; index >= 0; index--)
        {
            var assignment = ArenaControlAssignments[index];
            if (assignment == null)
            {
                ArenaControlAssignments.RemoveAt(index);
                continue;
            }

            if (assignment.Gladiator == gladiatorData || assignment.ControllerKey == controllerKey)
                ArenaControlAssignments.RemoveAt(index);
        }

        ArenaControlAssignments.Add(ArenaControlAssignmentData.Create(gladiatorData, controllerSetup));
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public bool ClearArenaControlAssignment(GladiatorData gladiatorData)
    {
        EnsureResources();
        if (gladiatorData == null)
            return false;

        var removed = false;
        for (var index = ArenaControlAssignments.Count - 1; index >= 0; index--)
        {
            if (ArenaControlAssignments[index]?.Gladiator != gladiatorData)
                continue;

            ArenaControlAssignments.RemoveAt(index);
            removed = true;
        }

        if (removed)
            EmitSignal(SignalName.RunChanged);

        return removed;
    }

    public bool SyncArenaControlAssignments(Array<LocalInputControllerConfig> controllerSetups)
    {
        EnsureResources();
        controllerSetups ??= new Array<LocalInputControllerConfig>();

        var changed = PruneArenaControlAssignments(controllerSetups);
        var usedControllerKeys = new System.Collections.Generic.HashSet<string>();
        foreach (var assignment in ArenaControlAssignments)
        {
            if (assignment != null)
                usedControllerKeys.Add(assignment.ControllerKey);
        }

        foreach (var gladiator in TownAssignments.ArenaGladiators)
        {
            if (gladiator == null || GetArenaControlAssignment(gladiator) != null)
                continue;

            var controllerSetup = GetFirstUnusedControllerSetup(controllerSetups, usedControllerKeys);
            if (controllerSetup == null)
                break;

            ArenaControlAssignments.Add(ArenaControlAssignmentData.Create(gladiator, controllerSetup));
            usedControllerKeys.Add(ArenaControlAssignmentData.GetControllerKey(controllerSetup));
            changed = true;
        }

        if (changed)
            EmitSignal(SignalName.RunChanged);

        return changed;
    }

    public bool AreArenaGladiatorsReadyForLaunch(Array<LocalInputControllerConfig> controllerSetups)
    {
        EnsureResources();
        if (TownAssignments.ArenaGladiators.Count <= 0)
            return false;

        foreach (var gladiator in TownAssignments.ArenaGladiators)
        {
            var assignment = GetArenaControlAssignment(gladiator);
            if (assignment == null || !ControllerSetupExists(controllerSetups, assignment.ControllerKey))
                return false;
        }

        return true;
    }

    public void SetAutoFeedThreshold(RationStoreData.RationQuality quality, float threshold)
    {
        EnsureResources();
        RationFeedingPolicy.SetFeedBelow(quality, threshold);
        EmitSignal(SignalName.RunChanged);
    }

    public void SetAutoFeedEnabled(bool enabled)
    {
        EnsureResources();
        RationFeedingPolicy.SetEnabled(enabled);
        EmitSignal(SignalName.RunChanged);
    }

    public void SetAutoFeedPriority(RationFeedingPolicyData.FeedPriority priority)
    {
        EnsureResources();
        RationFeedingPolicy.SetPriority(priority);
        EmitSignal(SignalName.RunChanged);
    }

    public int AutoFeedGladiatorsBelowThreshold()
    {
        EnsureResources();
        if (!RationFeedingPolicy.Enabled)
            return 0;

        var fedCount = 0;

        while (Rations.GetTotal() > 0)
        {
            var gladiator = Gladiators
                .Where(current => current != null && GetAutoFeedRationQuality(current) != null)
                .OrderBy(current => current.Provisions)
                .FirstOrDefault();

            var rationQuality = gladiator == null ? null : GetAutoFeedRationQuality(gladiator);
            if (rationQuality == null)
                break;

            var provisionValue = RationStoreData.GetProvisionValue(rationQuality.Value);
            if (gladiator.Provisions >= provisionValue || !Rations.TryConsumeRation(rationQuality.Value, out _))
                break;

            gladiator.SetProvisions(provisionValue);
            fedCount++;
        }

        if (fedCount > 0)
            EmitSignal(SignalName.RunChanged);

        return fedCount;
    }

    public void EnsureResources()
    {
        Rations ??= new RationInventory();
        Inventory ??= new Array<ItemData>();
        Market ??= new MarketData();
        Market.EnsureResources();
        RationFeedingPolicy ??= new RationFeedingPolicyData();
        RationFeedingPolicy.ClampValues();
        Cemetery ??= new Array<GladiatorData>();
        TownAssignments ??= new TownAssignmentData();
        TownAssignments.SyncWithActiveRoster(Gladiators);
        ArenaControlAssignments ??= new Array<ArenaControlAssignmentData>();
        PruneArenaControlAssignments(null);
    }

    private bool PruneArenaControlAssignments(Array<LocalInputControllerConfig> controllerSetups)
    {
        var changed = false;
        var usedControllerKeys = new System.Collections.Generic.HashSet<string>();
        for (var index = ArenaControlAssignments.Count - 1; index >= 0; index--)
        {
            var assignment = ArenaControlAssignments[index];
            var shouldRemove = assignment == null
                || !HasGladiator(assignment.Gladiator)
                || TownAssignments.GetLocation(assignment.Gladiator) != TownAssignmentData.AssignmentLocation.Arena
                || !usedControllerKeys.Add(assignment.ControllerKey);

            if (!shouldRemove && controllerSetups != null)
                shouldRemove = !ControllerSetupExists(controllerSetups, assignment.ControllerKey);

            if (!shouldRemove)
                continue;

            ArenaControlAssignments.RemoveAt(index);
            changed = true;
        }

        return changed;
    }

    private static LocalInputControllerConfig GetFirstUnusedControllerSetup(Array<LocalInputControllerConfig> controllerSetups, System.Collections.Generic.HashSet<string> usedControllerKeys)
    {
        foreach (var controllerSetup in controllerSetups)
        {
            if (controllerSetup == null)
                continue;

            if (!usedControllerKeys.Contains(ArenaControlAssignmentData.GetControllerKey(controllerSetup)))
                return controllerSetup;
        }

        return null;
    }

    private static bool ControllerSetupExists(Array<LocalInputControllerConfig> controllerSetups, string controllerKey)
    {
        if (controllerSetups == null || string.IsNullOrEmpty(controllerKey))
            return false;

        foreach (var controllerSetup in controllerSetups)
        {
            if (ArenaControlAssignmentData.GetControllerKey(controllerSetup) == controllerKey)
                return true;
        }

        return false;
    }

    private RationStoreData.RationQuality? GetAutoFeedRationQuality(GladiatorData gladiator)
    {
        if (gladiator == null || Rations.GetTotal() <= 0)
            return null;

        var eligibleQualities = new System.Collections.Generic.List<RationStoreData.RationQuality>();
        AddEligibleRationQuality(eligibleQualities, gladiator, RationStoreData.RationQuality.Poor);
        AddEligibleRationQuality(eligibleQualities, gladiator, RationStoreData.RationQuality.Common);
        AddEligibleRationQuality(eligibleQualities, gladiator, RationStoreData.RationQuality.Fine);

        if (eligibleQualities.Count <= 0)
            return null;

        return RationFeedingPolicy.Priority switch
        {
            RationFeedingPolicyData.FeedPriority.CheapestFirst => eligibleQualities
                .OrderBy(GetRationSortValue)
                .First(),
            RationFeedingPolicyData.FeedPriority.BestFirst => eligibleQualities
                .OrderByDescending(GetRationSortValue)
                .First(),
            _ => eligibleQualities
                .OrderBy(quality => Mathf.Abs(RationFeedingPolicy.GetFeedBelow(quality) - gladiator.Provisions))
                .ThenBy(GetRationSortValue)
                .First()
        };
    }

    private void AddEligibleRationQuality(System.Collections.Generic.List<RationStoreData.RationQuality> eligibleQualities, GladiatorData gladiator, RationStoreData.RationQuality quality)
    {
        var provisionValue = RationStoreData.GetProvisionValue(quality);
        if (Rations.GetCount(quality) > 0 && gladiator.Provisions < RationFeedingPolicy.GetFeedBelow(quality) && gladiator.Provisions < provisionValue)
            eligibleQualities.Add(quality);
    }

    private static int GetRationSortValue(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => 0,
            RationStoreData.RationQuality.Common => 1,
            RationStoreData.RationQuality.Fine => 2,
            _ => 0
        };
    }

    public int GetStarvingGladiatorCount()
    {
        var count = 0;
        foreach (var gladiator in Gladiators)
        {
            if (gladiator?.Provisions < ConditionWarningThreshold)
                count++;
        }

        return count;
    }

    public int GetExhaustedGladiatorCount()
    {
        var count = 0;
        foreach (var gladiator in Gladiators)
        {
            if (gladiator?.Exhaustion < ConditionWarningThreshold)
                count++;
        }

        return count;
    }

    public void KillGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        if (gladiatorData == null)
            return;

        var gladiatorIndex = Gladiators.IndexOf(gladiatorData);
        if (gladiatorIndex < 0)
            return;

        ReturnGladiatorEquipmentToInventory(gladiatorData, false);
        TownAssignments?.RemoveEverywhere(gladiatorData);
        gladiatorData.ApplyDeathState();
        Cemetery ??= new Array<GladiatorData>();
        if (!Cemetery.Contains(gladiatorData))
            Cemetery.Add(gladiatorData);

        Gladiators.RemoveAt(gladiatorIndex);
        GD.Print($"CompanyRunData: Removed gladiator '{gladiatorData.GladiatorName}' from active roster and moved to cemetery. Active gladiators: {Gladiators.Count}. Cemetery: {Cemetery.Count}.");

        careerData?.AddGladiatorDeath();
        EmitSignal(SignalName.GladiatorDied, gladiatorData);
        EmitSignal(SignalName.RunChanged);
    }

    public void ApplyGladiatorRecoverableCaps()
    {
        EnsureResources();

        foreach (var gladiator in Gladiators)
        {
            gladiator?.ApplyRecoverableCaps();
        }
    }
}
