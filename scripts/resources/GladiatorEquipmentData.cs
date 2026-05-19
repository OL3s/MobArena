using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class GladiatorEquipmentData : Resource
{
    private const string DefaultArmorPath = "res://resources/items/armor/cloth_wraps.tres";
    private const string DefaultMainHandPath = "res://resources/items/main_hand/training_sword.tres";
    private const string DefaultOffHandPath = "res://resources/items/off_hand/wooden_buckler.tres";

    public enum SignatureSkill
    {
        Dodge,
        Parry,
        Bash,
        Cleave
    }

    [Export]
    public ArmorItemData Armor { get; private set; }

    [Export]
    public MainHandItemData MainHand { get; private set; }

    [Export]
    public OffHandItemData OffHand { get; private set; }

    [Export]
    public SignatureSkill Skill { get; private set; } = SignatureSkill.Dodge;

    public static GladiatorEquipmentData CreateDefault(RandomNumberGenerator random)
    {
        return new GladiatorEquipmentData
        {
            Armor = ItemData.LoadRuntimeCopy<ArmorItemData>(DefaultArmorPath),
            MainHand = ItemData.LoadRuntimeCopy<MainHandItemData>(DefaultMainHandPath),
            OffHand = ItemData.LoadRuntimeCopy<OffHandItemData>(DefaultOffHandPath),
            Skill = (SignatureSkill)random.RandiRange(0, (int)SignatureSkill.Cleave)
        };
    }

    public void EquipArmor(ArmorItemData armor)
    {
        Armor = armor;
    }

    public void EquipMainHand(MainHandItemData item)
    {
        MainHand = item;
        if (MainHand?.IsTwoHanded == true)
            OffHand = null;
    }

    public bool CanEquipOffHand()
    {
        return MainHand?.IsTwoHanded != true;
    }

    public bool TryEquipOffHand(OffHandItemData item)
    {
        if (!CanEquipOffHand())
            return false;

        OffHand = item;
        return true;
    }

    public void UnequipArmor()
    {
        Armor = null;
    }

    public void UnequipMainHand()
    {
        MainHand = null;
    }

    public void UnequipOffHand()
    {
        OffHand = null;
    }
}
