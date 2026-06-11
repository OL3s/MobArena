using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources;

public partial class MarketData : Resource
{
    private static readonly string[] DefaultItemStockPaths =
    {
        "res://resources/items/armor/cloth_wraps.tres",
        "res://resources/items/armor/leather_vest.tres",
        "res://resources/items/armor/bronze_light_armor.tres",
        "res://resources/items/armor/bronze_medium_armor.tres",
        "res://resources/items/armor/bronze_heavy_armor.tres",
        "res://resources/items/armor/iron_light_armor.tres",
        "res://resources/items/armor/iron_medium_armor.tres",
        "res://resources/items/armor/iron_heavy_armor.tres",
        "res://resources/items/armor/blacksteel_light_armor.tres",
        "res://resources/items/armor/blacksteel_medium_armor.tres",
        "res://resources/items/armor/blacksteel_heavy_armor.tres",
        "res://resources/items/main_hand/training_sword.tres",
        "res://resources/items/main_hand/wood_sword.tres",
        "res://resources/items/main_hand/black_sword.tres",
        "res://resources/items/main_hand/training_spear.tres",
        "res://resources/items/main_hand/wood_spear.tres",
        "res://resources/items/main_hand/bronze_spear.tres",
        "res://resources/items/main_hand/iron_spear.tres",
        "res://resources/items/main_hand/black_spear.tres",
        "res://resources/items/main_hand/training_hammer.tres",
        "res://resources/items/main_hand/wood_hammer.tres",
        "res://resources/items/main_hand/bronze_hammer.tres",
        "res://resources/items/main_hand/iron_hammer.tres",
        "res://resources/items/main_hand/black_hammer.tres",
        "res://resources/items/main_hand/wooden_hammer.tres",
        "res://resources/items/main_hand/bronze_sword.tres",
        "res://resources/items/main_hand/iron_sword.tres",
        "res://resources/items/main_hand/blacksteel_sword.tres",
        "res://resources/items/main_hand/training_greatsword.tres",
        "res://resources/items/main_hand/wood_greatsword.tres",
        "res://resources/items/main_hand/bronze_greatsword.tres",
        "res://resources/items/main_hand/iron_greatsword.tres",
        "res://resources/items/main_hand/black_greatsword.tres",
        "res://resources/items/main_hand/blacksteel_greatsword.tres",
        "res://resources/items/main_hand/training_greathammer.tres",
        "res://resources/items/main_hand/wood_greathammer.tres",
        "res://resources/items/main_hand/bronze_greathammer.tres",
        "res://resources/items/main_hand/iron_greathammer.tres",
        "res://resources/items/main_hand/black_greathammer.tres",
        "res://resources/items/main_hand/training_axe.tres",
        "res://resources/items/main_hand/wood_axe.tres",
        "res://resources/items/main_hand/bronze_axe.tres",
        "res://resources/items/main_hand/iron_axe.tres",
        "res://resources/items/main_hand/black_axe.tres",
        "res://resources/items/main_hand/training_greataxe.tres",
        "res://resources/items/main_hand/wood_greataxe.tres",
        "res://resources/items/main_hand/bronze_greataxe.tres",
        "res://resources/items/main_hand/iron_greataxe.tres",
        "res://resources/items/main_hand/black_greataxe.tres",
        "res://resources/items/main_hand/training_bow.tres",
        "res://resources/items/main_hand/wooden_bow.tres",
        "res://resources/items/main_hand/bronze_bow.tres",
        "res://resources/items/main_hand/iron_bow.tres",
        "res://resources/items/main_hand/black_bow.tres",
        "res://resources/items/main_hand/hunting_bow.tres",
        "res://resources/items/main_hand/bronze_crossbow.tres",
        "res://resources/items/main_hand/iron_crossbow.tres",
        "res://resources/items/main_hand/black_crossbow.tres",
        "res://resources/items/off_hand/wooden_buckler.tres",
        "res://resources/items/off_hand/bronze_buckler.tres",
        "res://resources/items/off_hand/iron_kite_shield.tres",
        "res://resources/items/off_hand/black_shield.tres",
        "res://resources/items/off_hand/dagger.tres",
        "res://resources/items/off_hand/poison_flask.tres",
        "res://resources/coatings/weak_honing_oil.tres",
        "res://resources/coatings/honing_oil.tres",
        "res://resources/coatings/strong_honing_oil.tres",
        "res://resources/coatings/weak_poison_oil.tres",
        "res://resources/coatings/poison_oil.tres",
        "res://resources/coatings/strong_poison_oil.tres",
        "res://resources/coatings/weak_poison_coating.tres",
        "res://resources/coatings/poison_coating.tres",
        "res://resources/coatings/strong_poison_coating.tres",
        "res://resources/coatings/weak_poison_plating.tres",
        "res://resources/coatings/poison_plating.tres",
        "res://resources/coatings/strong_poison_plating.tres"
    };

	public Array<ItemData> ItemStock { get; private set; } = new();

	public Array<GladiatorData> GladiatorStock { get; private set; } = new();

	public bool HasInitializedItemStock { get; private set; }

	public bool HasInitializedGladiatorStock { get; private set; }

    public void EnsureResources()
    {
        ItemStock ??= new Array<ItemData>();
        GladiatorStock ??= new Array<GladiatorData>();
        if (!HasInitializedItemStock || HasItemStockMissingIcons())
            RefreshItemStock();
        if (!HasInitializedGladiatorStock || HasGladiatorStockMissingPortraits())
            RefreshGladiatorStock();
    }

    public void ExecuteNewDay()
    {
        EnsureResources();
        RefreshItemStock();
        RefreshGladiatorStock();
    }

    public void RefreshItemStock()
    {
        ItemStock ??= new Array<ItemData>();
        ItemStock.Clear();

        foreach (var itemPath in DefaultItemStockPaths)
        {
            var item = ItemData.LoadRuntimeCopy<ItemData>(itemPath);
            if (item != null)
                ItemStock.Add(item);
        }

        HasInitializedItemStock = true;
    }

    public void RefreshGladiatorStock()
    {
        GladiatorStock ??= new Array<GladiatorData>();
        GladiatorStock.Clear();

        for (var index = 0; index < 3; index++)
            GladiatorStock.Add(GladiatorData.CreateDefault());

        HasInitializedGladiatorStock = true;
    }

    private bool HasItemStockMissingIcons()
    {
        if (ItemStock == null || ItemStock.Count <= 0)
            return true;

        foreach (var item in ItemStock)
        {
            if (item?.UiIcon == null)
                return true;
        }

        return false;
    }

    private bool HasGladiatorStockMissingPortraits()
    {
        if (GladiatorStock == null || GladiatorStock.Count <= 0)
            return true;

        foreach (var gladiator in GladiatorStock)
        {
            if (gladiator?.Appearance == null)
                return true;
        }

        return false;
    }
}
