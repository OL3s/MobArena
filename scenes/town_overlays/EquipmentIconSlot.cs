using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.TownOverlays;

public partial class EquipmentIconSlot : PanelContainer
{
    private TextureRect _icon;
    private ItemData _item;
    private string _slotName = "Item";

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        RefreshUi();
    }

    public void Configure(ItemData item, string slotName)
    {
        _item = item;
        _slotName = slotName;
        RefreshUi();
    }

    private void RefreshUi()
    {
        TooltipText = _item == null ? $"{_slotName}: Empty" : $"{_slotName}: {_item.DisplayName}";
        Modulate = _item == null ? new Color(0.65f, 0.65f, 0.65f, 0.85f) : Colors.White;

        if (!IsNodeReady())
            return;

        _icon.Texture = _item?.UiIcon;
        _icon.TooltipText = TooltipText;
    }
}
