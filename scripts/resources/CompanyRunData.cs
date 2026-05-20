using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class CompanyRunData : Resource
{
    private const float ConditionWarningThreshold = 5f;
    private const int HealerPhaseHealAmount = 8;
    private const int HealerGoldCostPerGladiator = 3;
    private const int TrainingGoldCostPerGladiator = 2;
    private const int TrainingStaminaCost = 2;
    private const float TrainingExhaustionCost = 1f;
    private const float PhaseRestExhaustionRecovery = 0.75f;

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
    public Array<ItemData> Inventory { get; private set; } = new();

    [Export]
    public MarketData Market { get; private set; } = new();

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

    public bool TryEquipItemOnGladiator(GladiatorData gladiatorData, ItemData item)
    {
        EnsureResources();
        if (!HasGladiator(gladiatorData))
        {
            GD.PushError($"Equip failed: gladiator '{gladiatorData?.GladiatorName ?? "null"}' is not in the active roster.");
            return false;
        }

        if (!HasItem(item))
        {
            GD.PushError($"Equip failed: item '{item?.DisplayName ?? "null"}' is not in company inventory.");
            return false;
        }

        var equipment = gladiatorData.Equipment;
        if (equipment == null)
        {
            GD.PushError($"Equip failed: gladiator '{gladiatorData.GladiatorName}' has no equipment data.");
            return false;
        }

        return item switch
        {
            ArmorItemData armor => EquipArmor(gladiatorData, equipment, armor),
            MainHandItemData mainHand => EquipMainHand(gladiatorData, equipment, mainHand),
            OffHandItemData offHand => EquipOffHand(gladiatorData, equipment, offHand),
            _ => PushUnsupportedEquipItem(item)
        };
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

    private bool EquipArmor(GladiatorData gladiatorData, GladiatorEquipmentData equipment, ArmorItemData armor)
    {
        Inventory.Remove(armor);
        ReturnEquippedItemToInventory(equipment.Armor, gladiatorData, "armor");
        equipment.EquipArmor(armor);
        GD.Print($"Equip: '{gladiatorData.GladiatorName}' equipped armor '{armor.DisplayName}'.");
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    private bool EquipMainHand(GladiatorData gladiatorData, GladiatorEquipmentData equipment, MainHandItemData mainHand)
    {
        Inventory.Remove(mainHand);
        ReturnEquippedItemToInventory(equipment.MainHand, gladiatorData, "main hand");
        if (mainHand.IsTwoHanded)
            ReturnEquippedItemToInventory(equipment.OffHand, gladiatorData, "off hand");

        equipment.EquipMainHand(mainHand);
        GD.Print($"Equip: '{gladiatorData.GladiatorName}' equipped main hand '{mainHand.DisplayName}'.");
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    private bool EquipOffHand(GladiatorData gladiatorData, GladiatorEquipmentData equipment, OffHandItemData offHand)
    {
        if (!equipment.CanEquipOffHand())
        {
            GD.PushError($"Equip failed: gladiator '{gladiatorData.GladiatorName}' cannot equip off-hand '{offHand.DisplayName}' while using two-handed main hand '{equipment.MainHand?.DisplayName ?? "null"}'.");
            return false;
        }

        Inventory.Remove(offHand);
        ReturnEquippedItemToInventory(equipment.OffHand, gladiatorData, "off hand");
        if (!equipment.TryEquipOffHand(offHand))
        {
            Inventory.Add(offHand);
            GD.PushError($"Equip failed: gladiator '{gladiatorData.GladiatorName}' rejected off-hand '{offHand.DisplayName}'.");
            return false;
        }

        GD.Print($"Equip: '{gladiatorData.GladiatorName}' equipped off hand '{offHand.DisplayName}'.");
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    private static bool PushUnsupportedEquipItem(ItemData item)
    {
        GD.PushError($"Equip failed: item '{item?.DisplayName ?? "null"}' is not an armor, main-hand, or off-hand item.");
        return false;
    }

    public bool TryBuyItem(ItemData item, int price)
    {
        if (item == null || !TrySpendGold(price))
            return false;

        EnsureResources();
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

        AddGladiator(gladiatorData, careerData);
        return true;
    }

    public bool TryBuyGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        return TryBuyGladiator(gladiatorData, careerData, gladiatorData?.InitialCost ?? 0);
    }

    public int GetSaleValue(ItemData item)
    {
        return item == null
            ? 0
            : Mathf.Max(1, item.Cost / 2);
    }

    public int GetSaleValue(GladiatorData gladiatorData)
    {
        return gladiatorData?.GetMarketSaleValue() ?? 0;
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

    public void EnsureResources()
    {
        Inventory ??= new Array<ItemData>();
        Market ??= new MarketData();
        Market.EnsureResources();
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

    public int GetExhaustedGladiatorCount()
    {
        return GetRiskStatusCount(GladiatorRiskStatus.Exhausted, 0f);
    }

    public int GetLowHealthGladiatorCount(float warningRatio)
    {
        return GetRiskStatusCount(GladiatorRiskStatus.LowHealth, warningRatio);
    }

    public int GetCriticalRiskGladiatorCount(float warningRatio)
    {
        return GetRiskStatusCount(GladiatorRiskStatus.Critical, warningRatio);
    }

    public int GetRiskStatusCount(GladiatorRiskStatus riskStatus, float lowHealthWarningRatio)
    {
        var count = 0;
        foreach (var gladiator in Gladiators)
        {
            if (gladiator?.GetRiskStatus(ConditionWarningThreshold, lowHealthWarningRatio) == riskStatus)
                count++;
        }

        return count;
    }

    public void ExecutePhaseBuildingWork()
    {
        EnsureResources();
        RecoverCourtyardAndArenaGladiators();
        ExecuteHealerPhaseWork();
        ExecuteTrainingPhaseWork();
        EmitSignal(SignalName.RunChanged);
    }

    private void RecoverCourtyardAndArenaGladiators()
    {
        foreach (var gladiator in Gladiators)
        {
            if (gladiator == null)
                continue;

            var location = TownAssignments.GetLocation(gladiator);
            if (location is TownAssignmentData.AssignmentLocation.Courtyard or TownAssignmentData.AssignmentLocation.Arena)
                gladiator.SetExhaustion(gladiator.Exhaustion + PhaseRestExhaustionRecovery);
        }
    }

    private void ExecuteHealerPhaseWork()
    {
        foreach (var gladiator in TownAssignments.HealerGladiators)
        {
            if (gladiator == null || !HasGladiator(gladiator) || gladiator.Health >= gladiator.RecoverableMaxHealth)
                continue;

            if (!TrySpendGold(HealerGoldCostPerGladiator))
                break;

            gladiator.RestoreHealth(HealerPhaseHealAmount);
        }
    }

    private void ExecuteTrainingPhaseWork()
    {
        foreach (var gladiator in TownAssignments.TrainingHallGladiators)
        {
            if (gladiator == null || !HasGladiator(gladiator))
                continue;

            if (gladiator.Stamina < TrainingStaminaCost || gladiator.Exhaustion <= TrainingExhaustionCost)
                continue;

            if (!TrySpendGold(TrainingGoldCostPerGladiator))
                break;

            gladiator.SpendStamina(TrainingStaminaCost);
            gladiator.SetExhaustion(gladiator.Exhaustion - TrainingExhaustionCost);
        }
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
