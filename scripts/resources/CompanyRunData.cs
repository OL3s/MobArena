using Godot;
using Godot.Collections;
using System.Collections.Generic;
using MobArena.Scripts.Resources.Contracts;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class CompanyRunData : Resource
{
    public const int DefaultGladiatorCapacity = 6;

    public enum TreatmentFocus
    {
        Health,
        Exhaustion
    }

    public enum TrainingFocus
    {
        Overall,
        Strength,
        Agility,
        Vitality,
        Endurance
    }

    private const float ConditionWarningThreshold = 5f;
    private const float PhaseRestHealthRecoveryRatio = 0.1f;
    private const float TreatmentHealthRecoveryRatio = 0.4f;
    private const float TreatmentExhaustionRecovery = 3f;
    private const int TreatmentGoldCostPerGladiator = 3;
    private const int TrainingGoldCostPerGladiator = 2;
    private const int TrainingStaminaCost = 20;
    private const float TrainingExhaustionCost = 1f;
    private const float TrainingAttributeExp = 40f;
    private const float PhaseRestExhaustionRecovery = 2f;
    private const float ArenaFightExhaustionCost = 3f;
    private const int FameDonationBaseGoldCost = 20;
    private const int FameDonationCostGrowthPerFame = 5;
    private const int BuildingUpgradeBaseGoldCost = 50;
    private const int BuildingUpgradeCostGrowth = 50;

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

    [Export]
    public ArenaContractData ActiveArenaContract { get; private set; }

    [Export]
    public Array<GladiatorData> PendingGladiatorDeathNotifications { get; private set; } = new();

    public int AliveGladiators => Gladiators.Count;

    public bool HasActiveArenaContract => ActiveArenaContract != null;

    [Export]
    public int GladiatorCapacity { get; private set; } = DefaultGladiatorCapacity;

    [Export]
    public int MobsKilled { get; private set; }

    [Export]
    public bool HasShownFirstTownEntryPopup { get; private set; }

    [Export]
    public bool HasShownDragTutorialPopup { get; private set; }

    [Export]
    public bool HasShownFirstContractCompletedPopup { get; private set; }

    [Export]
    public bool HasShownNextDayUpkeepPopup { get; private set; }

    [Export]
    public bool HasUnlockedSpecialtyBuildings { get; private set; }

    [Export]
    public bool HasShownThermaeTutorialPopup { get; private set; }

    [Export]
    public bool HasShownTrainingHallTutorialPopup { get; private set; }

    [Export]
    public TreatmentFocus CurrentTreatmentFocus { get; private set; } = TreatmentFocus.Health;

    [Export]
    public TrainingFocus CurrentTrainingFocus { get; private set; } = TrainingFocus.Overall;

    [Export]
    public int HealerUpgradeLevel { get; private set; }

    [Export]
    public int TrainingHallUpgradeLevel { get; private set; }

    public int GetBuildingUpgradeLevel(TownAssignmentData.AssignmentLocation location)
    {
        return location switch
        {
            TownAssignmentData.AssignmentLocation.Healer => HealerUpgradeLevel,
            TownAssignmentData.AssignmentLocation.TrainingHall => TrainingHallUpgradeLevel,
            _ => 0
        };
    }

    public int GetBuildingUpgradeGoldCost(TownAssignmentData.AssignmentLocation location)
    {
        return BuildingUpgradeBaseGoldCost + (GetBuildingUpgradeLevel(location) * BuildingUpgradeCostGrowth);
    }

    public bool CanUpgradeBuilding(TownAssignmentData.AssignmentLocation location, int maxUpgradeLevel)
    {
        return IsUpgradeableTownBuilding(location)
            && GetBuildingUpgradeLevel(location) < maxUpgradeLevel
            && Gold >= GetBuildingUpgradeGoldCost(location);
    }

    public bool TryUpgradeBuilding(TownAssignmentData.AssignmentLocation location, int maxUpgradeLevel)
    {
        if (!CanUpgradeBuilding(location, maxUpgradeLevel) || !TrySpendGold(GetBuildingUpgradeGoldCost(location)))
            return false;

        switch (location)
        {
            case TownAssignmentData.AssignmentLocation.Healer:
                HealerUpgradeLevel++;
                break;
            case TownAssignmentData.AssignmentLocation.TrainingHall:
                TrainingHallUpgradeLevel++;
                break;
            default:
                return false;
        }

        EmitSignal(SignalName.RunChanged);
        return true;
    }

    private static bool IsUpgradeableTownBuilding(TownAssignmentData.AssignmentLocation location)
    {
        return location is TownAssignmentData.AssignmentLocation.Healer or TownAssignmentData.AssignmentLocation.TrainingHall;
    }

    public void SetTreatmentFocus(TreatmentFocus treatmentFocus)
    {
        if (CurrentTreatmentFocus == treatmentFocus)
            return;

        CurrentTreatmentFocus = treatmentFocus;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkFirstTownEntryPopupShown()
    {
        if (HasShownFirstTownEntryPopup)
            return;

        HasShownFirstTownEntryPopup = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkDragTutorialPopupShown()
    {
        if (HasShownDragTutorialPopup)
            return;

        HasShownDragTutorialPopup = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkFirstContractCompletedPopupShown()
    {
        if (HasShownFirstContractCompletedPopup)
            return;

        HasShownFirstContractCompletedPopup = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkNextDayUpkeepPopupShown()
    {
        if (HasShownNextDayUpkeepPopup)
            return;

        HasShownNextDayUpkeepPopup = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkSpecialtyBuildingsUnlocked()
    {
        if (HasUnlockedSpecialtyBuildings)
            return;

        HasUnlockedSpecialtyBuildings = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkThermaeTutorialPopupShown()
    {
        if (HasShownThermaeTutorialPopup)
            return;

        HasShownThermaeTutorialPopup = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void MarkTrainingHallTutorialPopupShown()
    {
        if (HasShownTrainingHallTutorialPopup)
            return;

        HasShownTrainingHallTutorialPopup = true;
        EmitSignal(SignalName.RunChanged);
    }

    public void SetTrainingFocus(TrainingFocus trainingFocus)
    {
        if (CurrentTrainingFocus == trainingFocus)
            return;

        CurrentTrainingFocus = trainingFocus;
        EmitSignal(SignalName.RunChanged);
    }

    public void AddGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        if (gladiatorData == null)
            return;

        if (!CanAddGladiator())
        {
            GD.PushError($"Add gladiator failed: active roster is full ({AliveGladiators}/{GladiatorCapacity}).");
            return;
        }

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

    public bool CanAddGladiator()
    {
        EnsureResources();
        return AliveGladiators < GladiatorCapacity;
    }

    public void AddGold(int amount, CompanyCareerData careerData)
    {
        if (amount <= 0)
            return;

        var previousGold = Gold;
        Gold += amount;
        careerData?.AddGoldEarned(amount);
        GD.Print($"CompanyRunData: Added {amount} gold ({previousGold} -> {Gold}).");
        EmitSignal(SignalName.RunChanged);
    }

    public void AddFame(int amount)
    {
        if (amount <= 0)
            return;

        var previousFame = Fame;
        Fame += amount;
        GD.Print($"CompanyRunData: Added {amount} fame ({previousFame} -> {Fame}).");
        EmitSignal(SignalName.RunChanged);
    }

    public void LoseFame(int amount)
    {
        if (amount <= 0)
            return;

        var previousFame = Fame;
        Fame = Mathf.Max(Fame - amount, 0);
        GD.Print($"CompanyRunData: Lost {amount} fame ({previousFame} -> {Fame}).");
        EmitSignal(SignalName.RunChanged);
    }

    public bool TrySpendFame(int amount)
    {
        if (amount <= 0)
            return true;

        if (Fame < amount)
        {
            GD.Print($"CompanyRunData: Spend fame failed; has {Fame}, needs {amount}.");
            return false;
        }

        var previousFame = Fame;
        Fame -= amount;
        GD.Print($"CompanyRunData: Spent {amount} fame ({previousFame} -> {Fame}).");
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public int GetFameDonationGoldCost(int fameAmount)
    {
        if (fameAmount <= 0)
            return 0;

        var cost = 0;
        for (var index = 0; index < fameAmount; index++)
            cost += FameDonationBaseGoldCost + ((Fame + index) * FameDonationCostGrowthPerFame);

        return cost;
    }

    public bool CanDonateForFame(int fameAmount)
    {
        return fameAmount > 0 && Gold >= GetFameDonationGoldCost(fameAmount);
    }

    public bool TryDonateForFame(int fameAmount)
    {
        var cost = GetFameDonationGoldCost(fameAmount);
        if (fameAmount <= 0 || cost <= 0 || !TrySpendGold(cost))
        {
            GD.Print($"CompanyRunData: Donate for fame failed; fame={fameAmount}, cost={cost}, gold={Gold}.");
            return false;
        }

        GD.Print($"CompanyRunData: Donated {cost} gold for {fameAmount} fame.");
        AddFame(fameAmount);
        return true;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
        {
            GD.Print($"CompanyRunData: Spend gold failed; has {Gold}, needs {amount}.");
            return false;
        }

        var previousGold = Gold;
        Gold -= amount;
        GD.Print($"CompanyRunData: Spent {amount} gold ({previousGold} -> {Gold}).");
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public void SpendGoldAllowDebt(int amount)
    {
        if (amount <= 0)
            return;

        var previousGold = Gold;
        Gold -= amount;
        GD.Print($"CompanyRunData: Spent {amount} gold allowing debt ({previousGold} -> {Gold}).");
        EmitSignal(SignalName.RunChanged);
    }

    public void SetActiveArenaContract(ArenaContractData contractData)
    {
        ActiveArenaContract = contractData;
        GD.Print($"CompanyRunData: Active arena contract set to '{contractData?.DisplayName ?? "None"}'.");
        EmitSignal(SignalName.RunChanged);
    }

    public void ClearActiveArenaContract()
    {
        if (ActiveArenaContract == null)
            return;

        ActiveArenaContract = null;
        GD.Print("CompanyRunData: Cleared active arena contract.");
        EmitSignal(SignalName.RunChanged);
    }

    public void AddItem(ItemData item)
    {
        if (item == null)
            return;

        EnsureResources();
        Inventory.Add(item);
        GD.Print($"CompanyRunData: Added item '{item.DisplayName}' to inventory. Inventory: {Inventory.Count}.");
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
            var replacedMainHand = equipment.MainHand;
            ReturnEquippedItemToInventory(replacedMainHand, gladiatorData, "main hand");
            equipment.UnequipMainHand();
            GD.Print($"Equip: '{gladiatorData.GladiatorName}' unequipped two-handed main hand '{replacedMainHand?.DisplayName ?? "null"}' to equip off hand '{offHand.DisplayName}'.");
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
        GD.Print($"CompanyRunData: Bought item '{item.DisplayName}' for {price} gold. Inventory: {Inventory.Count}.");
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public bool TryBuyItem(ItemData item)
    {
        return TryBuyItem(item, item?.Cost ?? 0);
    }

    public bool TryBuyMarketItem(int itemIndex)
    {
        EnsureResources();
        var stock = Market?.ItemStock;
        if (stock == null || stock.Count <= 0)
        {
            GD.Print("CompanyRunData: Buy market item failed; market item stock is empty.");
            return false;
        }

        if (itemIndex < 0 || itemIndex >= stock.Count)
        {
            GD.Print($"CompanyRunData: Buy market item failed; item index {itemIndex} is outside available range 0..{stock.Count - 1}.");
            return false;
        }

        var item = stock[itemIndex];
        if (item == null)
        {
            GD.Print($"CompanyRunData: Buy market item failed; item index {itemIndex} is empty.");
            return false;
        }

        if (!stock.Remove(item))
        {
            GD.Print($"CompanyRunData: Buy market item failed; item '{item.DisplayName}' could not be removed from stock.");
            return false;
        }

        if (TryBuyItem(item))
            return true;

        stock.Insert(itemIndex, item);
        GD.Print($"CompanyRunData: Buy market item failed; rolled back '{item.DisplayName}' to stock.");
        return false;
    }

    public bool TryBuyMarketItem(ItemData item)
    {
        EnsureResources();
        var itemIndex = Market?.ItemStock?.IndexOf(item) ?? -1;
        if (itemIndex < 0)
        {
            GD.Print($"CompanyRunData: Buy market item failed; item '{item?.DisplayName ?? "null"}' is not in market stock.");
            return false;
        }

        return TryBuyMarketItem(itemIndex);
    }

    public bool TryBuyGladiator(GladiatorData gladiatorData, CompanyCareerData careerData, int price)
    {
        if (gladiatorData == null || !CanAddGladiator() || !TrySpendGold(price))
        {
            GD.Print($"CompanyRunData: Buy gladiator failed; gladiator='{gladiatorData?.GladiatorName ?? "null"}', price={price}, gold={Gold}, roster={AliveGladiators}/{GladiatorCapacity}.");
            return false;
        }

        GD.Print($"CompanyRunData: Bought gladiator '{gladiatorData.GladiatorName}' for {price} gold.");
        AddGladiator(gladiatorData, careerData);
        return true;
    }

    public bool TryBuyGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        return TryBuyGladiator(gladiatorData, careerData, gladiatorData?.GetMarketValue() ?? 0);
    }

    public bool TryBuyMarketGladiator(int gladiatorIndex, CompanyCareerData careerData)
    {
        EnsureResources();
        var stock = Market?.GladiatorStock;
        if (stock == null || stock.Count <= 0)
        {
            GD.Print("CompanyRunData: Buy market gladiator failed; market gladiator stock is empty.");
            return false;
        }

        if (gladiatorIndex < 0 || gladiatorIndex >= stock.Count)
        {
            GD.Print($"CompanyRunData: Buy market gladiator failed; gladiator index {gladiatorIndex} is outside available range 0..{stock.Count - 1}.");
            return false;
        }

        var gladiator = stock[gladiatorIndex];
        if (gladiator == null)
        {
            GD.Print($"CompanyRunData: Buy market gladiator failed; gladiator index {gladiatorIndex} is empty.");
            return false;
        }

        if (!stock.Remove(gladiator))
        {
            GD.Print($"CompanyRunData: Buy market gladiator failed; gladiator '{gladiator.GladiatorName}' could not be removed from stock.");
            return false;
        }

        if (TryBuyGladiator(gladiator, careerData))
            return true;

        stock.Insert(gladiatorIndex, gladiator);
        GD.Print($"CompanyRunData: Buy market gladiator failed; rolled back '{gladiator.GladiatorName}' to stock.");
        return false;
    }

    public bool TryBuyMarketGladiator(GladiatorData gladiatorData, CompanyCareerData careerData)
    {
        EnsureResources();
        var gladiatorIndex = Market?.GladiatorStock?.IndexOf(gladiatorData) ?? -1;
        if (gladiatorIndex < 0)
        {
            GD.Print($"CompanyRunData: Buy market gladiator failed; gladiator '{gladiatorData?.GladiatorName ?? "null"}' is not in market stock.");
            return false;
        }

        return TryBuyMarketGladiator(gladiatorIndex, careerData);
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

        GD.Print($"CompanyRunData: Sold item '{item.DisplayName}' for {saleValue} gold.");
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

        GD.Print($"CompanyRunData: Sold gladiator '{gladiatorData.GladiatorName}' for {saleValue} gold.");
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

        GD.Print($"CompanyRunData: Assigned gladiator '{gladiatorData.GladiatorName}' to {location}.");
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
        GD.Print($"CompanyRunData: Moved gladiator '{gladiatorData.GladiatorName}' to courtyard.");
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
        GD.Print($"CompanyRunData: Added {amount} mob kills. Run mobs killed: {MobsKilled}.");
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

    public void ClearArenaControlAssignments()
    {
        EnsureResources();
        if (ArenaControlAssignments.Count <= 0)
            return;

        ArenaControlAssignments.Clear();
        EmitSignal(SignalName.RunChanged);
    }

    public void CompleteArenaContractAssignments()
    {
        EnsureResources();
        if (TownAssignments.ArenaGladiators.Count <= 0)
            return;

        var foughtGladiators = new Array<GladiatorData>(TownAssignments.ArenaGladiators);
        foreach (var gladiator in foughtGladiators)
        {
            if (!HasGladiator(gladiator))
                continue;

            gladiator.SetExhaustion(gladiator.Exhaustion - ArenaFightExhaustionCost);
            TownAssignments.MoveToCourtyard(gladiator);
        }

        ArenaControlAssignments.Clear();
        EmitSignal(SignalName.RunChanged);
    }

    public bool SyncArenaControlAssignments(Array<LocalInputControllerConfig> controllerSetups)
    {
        EnsureResources();
        controllerSetups ??= new Array<LocalInputControllerConfig>();

        var changed = PruneArenaControlAssignments(controllerSetups);
        var usedControllerKeys = new System.Collections.Generic.HashSet<ArenaControlAssignmentData.ControllerIdentity>();
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
        var usedControllerKeys = new System.Collections.Generic.HashSet<ArenaControlAssignmentData.ControllerIdentity>();
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

    private static LocalInputControllerConfig GetFirstUnusedControllerSetup(Array<LocalInputControllerConfig> controllerSetups, System.Collections.Generic.HashSet<ArenaControlAssignmentData.ControllerIdentity> usedControllerKeys)
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

    private static bool ControllerSetupExists(Array<LocalInputControllerConfig> controllerSetups, ArenaControlAssignmentData.ControllerIdentity controllerKey)
    {
        if (controllerSetups == null)
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

    public int GetIdleAssignedGladiatorCount()
    {
        var count = 0;
        count += GetIdleAssignedGladiatorCount(TownAssignments.HealerGladiators, TownAssignmentData.AssignmentLocation.Healer);
        count += GetIdleAssignedGladiatorCount(TownAssignments.TrainingHallGladiators, TownAssignmentData.AssignmentLocation.TrainingHall);
        return count;
    }

    private int GetIdleAssignedGladiatorCount(IEnumerable<GladiatorData> gladiators, TownAssignmentData.AssignmentLocation assignmentLocation)
    {
        var count = 0;
        foreach (var gladiator in gladiators)
        {
            if (IsGladiatorIdleInTownLocation(gladiator, assignmentLocation))
                count++;
        }

        return count;
    }

    public bool IsGladiatorIdleInTownLocation(GladiatorData gladiator, TownAssignmentData.AssignmentLocation assignmentLocation)
    {
        if (gladiator == null || !HasGladiator(gladiator))
            return false;

        return assignmentLocation switch
        {
            TownAssignmentData.AssignmentLocation.Healer => !CanExecuteTreatmentPhaseWork(gladiator),
            TownAssignmentData.AssignmentLocation.TrainingHall => !CanExecuteTrainingPhaseWork(gladiator),
            _ => false
        };
    }

    private bool CanExecuteTreatmentPhaseWork(GladiatorData gladiator)
    {
        if (gladiator == null || !HasGladiator(gladiator))
            return false;

        return CurrentTreatmentFocus switch
        {
            TreatmentFocus.Exhaustion => gladiator.Exhaustion < GladiatorData.MaxConditionValue,
            _ => gladiator.Health < gladiator.RecoverableMaxHealth && GetHealthRecoveryAmount(gladiator, TreatmentHealthRecoveryRatio) > 0
        };
    }

    private bool CanExecuteTrainingPhaseWork(GladiatorData gladiator)
    {
        return gladiator != null
            && HasGladiator(gladiator)
            && gladiator.Stamina >= TrainingStaminaCost
            && gladiator.Exhaustion > TrainingExhaustionCost;
    }

    public int GetTreatmentHealthRecoveryPreview(GladiatorData gladiator)
    {
        if (gladiator == null || !HasGladiator(gladiator))
            return 0;

        var recoveryAmount = GetHealthRecoveryAmount(gladiator, TreatmentHealthRecoveryRatio);
        return Mathf.Max(0, Mathf.Min(gladiator.RecoverableMaxHealth, gladiator.Health + recoveryAmount) - gladiator.Health);
    }

    public float GetTreatmentExhaustionRecoveryPreview(GladiatorData gladiator)
    {
        if (gladiator == null || !HasGladiator(gladiator))
            return 0f;

        return Mathf.Max(0f, Mathf.Min(GladiatorData.MaxConditionValue, gladiator.Exhaustion + TreatmentExhaustionRecovery) - gladiator.Exhaustion);
    }

    public float GetTrainingAttributeExpPreview(TrainingFocus trainingFocus, GladiatorLevelData.AttributeKind attributeKind)
    {
        if (trainingFocus == TrainingFocus.Overall)
            return TrainingAttributeExp / 4f;

        return GetFocusedTrainingAttribute(trainingFocus) == attributeKind ? TrainingAttributeExp : 0f;
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

    public int GetPhaseBuildingGoldCost(TownAssignmentData.AssignmentLocation assignmentLocation)
    {
        EnsureResources();
        return assignmentLocation switch
        {
            TownAssignmentData.AssignmentLocation.Healer => GetTreatmentPhaseGoldCost(),
            TownAssignmentData.AssignmentLocation.TrainingHall => GetTrainingPhaseGoldCost(),
            _ => 0
        };
    }

    public int GetTownLocationPhaseGoldPreviewCost(TownAssignmentData.AssignmentLocation assignmentLocation, TownPhaseState phaseState)
    {
        return GetPhaseBuildingGoldCost(assignmentLocation) + GetTownLocationSalaryGoldCost(assignmentLocation, phaseState);
    }

    public int GetTownLocationSalaryGoldCost(TownAssignmentData.AssignmentLocation assignmentLocation, TownPhaseState phaseState)
    {
        if (phaseState?.IsNight() != true)
            return 0;

        EnsureResources();
        return GetAssignedGladiatorsSalaryGoldCost(TownAssignments.GetGladiators(assignmentLocation), phaseState);
    }

    public int GetAssignedGladiatorsSalaryGoldCost(IEnumerable<GladiatorData> gladiators, TownPhaseState phaseState)
    {
        if (phaseState?.IsNight() != true)
            return 0;

        var total = 0;
        foreach (var gladiator in gladiators)
        {
            if (HasGladiator(gladiator))
                total += GetGladiatorSalaryGoldCost(gladiator);
        }

        return total;
    }

    public int GetCurrentPhaseBuildingGoldCost()
    {
        return GetPhaseBuildingGoldCostLines().SumCostsForPhase(null, includeAll: true);
    }

    public int GetCurrentPhaseSalaryGoldCost(TownPhaseState phaseState)
    {
        return phaseState?.IsNight() == true ? GetNightSalaryGoldCost() : 0;
    }

    public int GetCurrentPhaseGoldCost(TownPhaseState phaseState)
    {
        return GetCurrentPhaseGoldCostLines().SumCostsForPhase(phaseState);
    }

    public bool CanPayCurrentPhaseGoldCost(TownPhaseState phaseState)
    {
        return Gold >= GetCurrentPhaseGoldCost(phaseState);
    }

    public int GetArenaReturnUpkeepGoldCost(TownPhaseState phaseState)
    {
        return GetCurrentPhaseGoldCost(phaseState);
    }

    public bool CanPayArenaReturnUpkeep(TownPhaseState phaseState)
    {
        return Gold >= GetArenaReturnUpkeepGoldCost(phaseState);
    }

    public IEnumerable<PhaseGoldCostLine> GetCurrentPhaseGoldCostLines()
    {
        foreach (var gladiator in Gladiators)
        {
            if (gladiator != null)
                yield return new PhaseGoldCostLine(gladiator.GladiatorName, GetGladiatorSalaryGoldCost(gladiator), PhaseGoldCostTiming.NightToDay);
        }

        foreach (var line in GetPhaseBuildingGoldCostLines())
            yield return line;
    }

    public IEnumerable<PhaseGoldCostLine> GetPhaseBuildingGoldCostLines()
    {
        yield return new PhaseGoldCostLine("Thermae", GetTreatmentPhaseGoldCost(), PhaseGoldCostTiming.Both);
        yield return new PhaseGoldCostLine("Training Hall", GetTrainingPhaseGoldCost(), PhaseGoldCostTiming.Both);
    }

    public int GetNightSalaryGoldCost()
    {
        var total = 0;
        foreach (var gladiator in Gladiators)
        {
            total += GetGladiatorSalaryGoldCost(gladiator);
        }

        return total;
    }

    public static int GetGladiatorSalaryGoldCost(GladiatorData gladiator)
    {
        return gladiator == null ? 0 : Mathf.FloorToInt(gladiator.InitialCost / 10f);
    }

    public bool PayNightSalary()
    {
        var salary = GetNightSalaryGoldCost();
        GD.Print($"CompanyRunData: Paying night salary: {salary} gold.");
        SpendGoldAllowDebt(salary);
        return true;
    }

    public void ExecutePhaseBuildingWork()
    {
        EnsureResources();
        RecoverCourtyardAndArenaGladiators();
        ExecuteTreatmentPhaseWork();
        ExecuteTrainingPhaseWork();
        GD.Print("CompanyRunData: Executed phase building work.");
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
            {
                gladiator.SetExhaustion(gladiator.Exhaustion + PhaseRestExhaustionRecovery);
                gladiator.RestoreHealth(GetHealthRecoveryAmount(gladiator, PhaseRestHealthRecoveryRatio));
            }
        }
    }

    private void ExecuteTreatmentPhaseWork()
    {
        foreach (var gladiator in TownAssignments.HealerGladiators)
        {
            if (!CanExecuteTreatmentPhaseWork(gladiator))
                continue;

            SpendGoldAllowDebt(TreatmentGoldCostPerGladiator);
            ExecuteTreatmentPhaseWorkForGladiator(gladiator);
        }
    }

    private void ExecuteTreatmentPhaseWorkForGladiator(GladiatorData gladiator)
    {
        if (gladiator == null)
            return;

        if (CurrentTreatmentFocus == TreatmentFocus.Exhaustion)
        {
            gladiator.SetExhaustion(gladiator.Exhaustion + TreatmentExhaustionRecovery);
            return;
        }

        gladiator.RestoreHealth(GetHealthRecoveryAmount(gladiator, TreatmentHealthRecoveryRatio));
    }

    private int GetTreatmentPhaseGoldCost()
    {
        return GetTreatmentPhaseGoldCost(TownAssignments.HealerGladiators);
    }

    private int GetTreatmentPhaseGoldCost(IEnumerable<GladiatorData> gladiators)
    {
        var total = 0;
        foreach (var gladiator in gladiators)
        {
            if (!CanExecuteTreatmentPhaseWork(gladiator))
                continue;

            total += TreatmentGoldCostPerGladiator;
        }

        return total;
    }

    private static int GetHealthRecoveryAmount(GladiatorData gladiator, float maxHealthRatio)
    {
        return gladiator?.MaxHealth > 0
            ? Mathf.Max(1, Mathf.RoundToInt(gladiator.MaxHealth * maxHealthRatio))
            : 0;
    }

    private void ExecuteTrainingPhaseWork()
    {
        foreach (var gladiator in TownAssignments.TrainingHallGladiators)
        {
            if (!CanExecuteTrainingPhaseWork(gladiator))
                continue;

            SpendGoldAllowDebt(TrainingGoldCostPerGladiator);
            gladiator.SpendStamina(TrainingStaminaCost);
            gladiator.SetExhaustion(gladiator.Exhaustion - TrainingExhaustionCost);
            ApplyTrainingFocus(gladiator);
        }
    }

    private void ApplyTrainingFocus(GladiatorData gladiator)
    {
        if (gladiator?.Level == null)
            return;

        if (CurrentTrainingFocus == TrainingFocus.Overall)
        {
            var splitExp = TrainingAttributeExp / 4f;
            gladiator.Level.AddAttributeExp(GladiatorLevelData.AttributeKind.Strength, splitExp);
            gladiator.Level.AddAttributeExp(GladiatorLevelData.AttributeKind.Agility, splitExp);
            gladiator.Level.AddAttributeExp(GladiatorLevelData.AttributeKind.Vitality, splitExp);
            gladiator.Level.AddAttributeExp(GladiatorLevelData.AttributeKind.Endurance, splitExp);
            return;
        }

        gladiator.Level.AddAttributeExp(GetFocusedTrainingAttribute(CurrentTrainingFocus), TrainingAttributeExp);
    }

    private static GladiatorLevelData.AttributeKind GetFocusedTrainingAttribute(TrainingFocus trainingFocus)
    {
        return trainingFocus switch
        {
            TrainingFocus.Agility => GladiatorLevelData.AttributeKind.Agility,
            TrainingFocus.Vitality => GladiatorLevelData.AttributeKind.Vitality,
            TrainingFocus.Endurance => GladiatorLevelData.AttributeKind.Endurance,
            _ => GladiatorLevelData.AttributeKind.Strength
        };
    }

    private int GetTrainingPhaseGoldCost()
    {
        return GetTrainingPhaseGoldCost(TownAssignments.TrainingHallGladiators);
    }

    private int GetTrainingPhaseGoldCost(IEnumerable<GladiatorData> gladiators)
    {
        var total = 0;
        foreach (var gladiator in gladiators)
        {
            if (!CanExecuteTrainingPhaseWork(gladiator))
                continue;

            total += TrainingGoldCostPerGladiator;
        }

        return total;
    }

    public Array<GladiatorData> ConsumePendingGladiatorDeathNotifications()
    {
        PendingGladiatorDeathNotifications ??= new Array<GladiatorData>();
        var pending = new Array<GladiatorData>();
        foreach (var gladiator in PendingGladiatorDeathNotifications)
        {
            if (gladiator != null)
                pending.Add(gladiator);
        }

        PendingGladiatorDeathNotifications.Clear();
        if (pending.Count > 0)
            EmitSignal(SignalName.RunChanged);

        return pending;
    }

    public void KillGladiator(
        GladiatorData gladiatorData,
        CompanyCareerData careerData,
        bool notifyImmediately = true,
        bool queueDeferredNotification = true)
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
        if (notifyImmediately)
        {
            EmitSignal(SignalName.GladiatorDied, gladiatorData);
        }
        else if (queueDeferredNotification)
        {
            PendingGladiatorDeathNotifications ??= new Array<GladiatorData>();
            PendingGladiatorDeathNotifications.Add(gladiatorData);
        }

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
