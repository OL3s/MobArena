using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class BlacksmithStoreOverlay : Control
{
    private const string ItemCardScenePath = "res://scenes/components/ui/ItemCard.tscn";

    private CompanyRunData _runData;
    private Label _goldLabel;
    private GridContainer _itemGrid;
    private PackedScene _itemCardScene;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _goldLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Header/GoldLabel");
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
        _goldLabel.Text = _runData.Gold.ToString();

        foreach (var child in _itemGrid.GetChildren())
            child.QueueFree();

        foreach (var item in _runData.Market.ItemStock)
        {
            if (item != null)
                _itemGrid.AddChild(CreateItemCard(item));
        }
    }

    private void OnBuyPressed(ItemData item)
    {
        if (item == null || _runData.Gold < item.Cost)
            return;

        if (!_runData.Market.ItemStock.Remove(item))
            return;

        if (!_runData.TryBuyItem(item))
        {
            _runData.Market.ItemStock.Add(item);
            RefreshUi();
        }
    }

    private ItemCard CreateItemCard(ItemData item)
    {
        var card = _itemCardScene.Instantiate<ItemCard>();
        card.Configure(item, ItemCard.CardMode.Purchase, _runData.Gold >= item.Cost);
        card.BuyPressed += OnBuyPressed;
        return card;
    }

}
