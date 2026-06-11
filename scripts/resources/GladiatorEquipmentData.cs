using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class GladiatorEquipmentData : Resource
{
    private const string DefaultArmorPath = "res://resources/items/armor/cloth_wraps.tres";
    private static readonly string[] DefaultMainHandPaths =
    {
        "res://resources/items/main_hand/training_axe.tres",
        "res://resources/items/main_hand/training_bow.tres",
        "res://resources/items/main_hand/training_greataxe.tres",
        "res://resources/items/main_hand/training_greathammer.tres",
        "res://resources/items/main_hand/training_greatsword.tres",
        "res://resources/items/main_hand/training_hammer.tres",
        "res://resources/items/main_hand/training_spear.tres",
        "res://resources/items/main_hand/training_sword.tres"
    };

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
            Armor = random.Randf() < 0.5f ? ItemData.LoadRuntimeCopy<ArmorItemData>(DefaultArmorPath) : null,
            MainHand = random.Randf() < 0.5f ? LoadRandomDefaultMainHand(random) : null,
            OffHand = null,
            Skill = (SignatureSkill)random.RandiRange(0, (int)SignatureSkill.Cleave)
        };
    }

    private static MainHandItemData LoadRandomDefaultMainHand(RandomNumberGenerator random)
    {
        var itemPath = DefaultMainHandPaths[random.RandiRange(0, DefaultMainHandPaths.Length - 1)];
        return ItemData.LoadRuntimeCopy<MainHandItemData>(itemPath);
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
