using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI;

public static class ItemIconRegistry
{
    private const string ArmorTypeIconPath = "res://assets/ui/items/type_armor.svg";
    private const string MainHandTypeIconPath = "res://assets/ui/items/type_main_hand.svg";
    private const string TwoHandedTypeIconPath = "res://assets/ui/items/type_two_handed.svg";
    private const string OffHandTypeIconPath = "res://assets/ui/items/type_off_hand.svg";

    public static Texture2D LoadItemTypeIcon(ItemData item)
    {
        var iconPath = item switch
        {
            ArmorItemData => ArmorTypeIconPath,
            MainHandItemData mainHand => mainHand.IsTwoHanded ? TwoHandedTypeIconPath : MainHandTypeIconPath,
            OffHandItemData => OffHandTypeIconPath,
            ItemCoatingData => "res://assets/ui/coatings/type_coating.svg",
            _ => UiIconLoader.FallbackIconPath
        };

        return UiIconLoader.LoadIcon(iconPath);
    }
}
