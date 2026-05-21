using Godot;
using MobArena.Scenes.Components.Town;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class EquipmentInventoryOverlay : Control
{
    private const string ItemCardScenePath = "res://scenes/components/ui/ItemCard.tscn";

    private CompanyRunData _runData;
    private Label _summaryLabel;
    private GridContainer _itemGrid;
    private PackedScene _itemCardScene;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _summaryLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/SummaryLabel");
        _itemGrid = GetNode<GridContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ScrollContainer/ItemGrid");
        _itemCardScene = ResourceLoader.Load<PackedScene>(ItemCardScenePath);

        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;
        _runData.RunChanged += RefreshUi;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void RefreshUi()
    {
        _runData.EnsureResources();
        _summaryLabel.Text = $"Unequipped items: {_runData.Inventory.Count}";

        foreach (var child in _itemGrid.GetChildren())
            child.QueueFree();

        foreach (var item in _runData.Inventory)
        {
            if (item != null)
                _itemGrid.AddChild(CreateItemCard(item));
        }
    }

    private ItemCard CreateItemCard(ItemData item)
    {
        var card = _itemCardScene.Instantiate<ItemCard>();
        card.Configure(item, ItemCard.CardMode.Equipment);
        card.DragRequested += OnDragRequested;
        return card;
    }

    private void OnDragRequested(ItemData item)
    {
        var rosterYard = GetTree().GetFirstNodeInGroup("roster_yard") as RosterYard;
        if (rosterYard == null || item == null)
            return;

        rosterYard.StartItemDrag(item, GetViewport().GetMousePosition());
        QueueFree();
    }
}
