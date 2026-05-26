using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.Components.UI.ItemShowcases;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class BlacksmithStoreOverlay : Control
{
    private const string ItemStoreListRowScenePath = "res://scenes/components/ui/ItemStoreListRow.tscn";
    private const string ArmorItemShowcaseScenePath = "res://scenes/components/ui/item_showcases/ArmorItemShowcase.tscn";
    private const string MainHandItemShowcaseScenePath = "res://scenes/components/ui/item_showcases/MainHandItemShowcase.tscn";
    private const string OffHandItemShowcaseScenePath = "res://scenes/components/ui/item_showcases/OffHandItemShowcase.tscn";

    private CompanyRunData _runData;
    private Label _goldLabel;
    private VBoxContainer _itemList;
    private TextureRect _detailIcon;
    private Label _detailNameLabel;
    private Label _detailTypeLabel;
    private Label _detailDescriptionLabel;
    private VBoxContainer _showcaseHost;
    private Label _detailCostLabel;
    private Button _buyButton;
    private PackedScene _itemStoreListRowScene;
    private PackedScene _armorItemShowcaseScene;
    private PackedScene _mainHandItemShowcaseScene;
    private PackedScene _offHandItemShowcaseScene;
    private ItemData _selectedItem;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _goldLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Header/GoldLabel");
        _itemList = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/ItemListPanel/ItemListMargin/ItemListScroll/ItemList");
        _detailIcon = GetNode<TextureRect>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/DetailHeader/DetailIcon");
        _detailNameLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/DetailHeader/DetailTitleBlock/DetailNameLabel");
        _detailTypeLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/DetailHeader/DetailTitleBlock/DetailTypeLabel");
        _detailDescriptionLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/DetailDescriptionLabel");
        _showcaseHost = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/ShowcaseScroll/ShowcaseHost");
        _detailCostLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/DetailFooter/DetailCostBox/DetailCostLabel");
        _buyButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/DetailPanel/DetailMargin/DetailLayout/DetailFooter/BuyButton");
        _itemStoreListRowScene = ResourceLoader.Load<PackedScene>(ItemStoreListRowScenePath);
        _armorItemShowcaseScene = ResourceLoader.Load<PackedScene>(ArmorItemShowcaseScenePath);
        _mainHandItemShowcaseScene = ResourceLoader.Load<PackedScene>(MainHandItemShowcaseScenePath);
        _offHandItemShowcaseScene = ResourceLoader.Load<PackedScene>(OffHandItemShowcaseScenePath);

        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;
        _buyButton.Pressed += OnSelectedBuyPressed;
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

        foreach (var child in _itemList.GetChildren())
            child.QueueFree();

        if (_selectedItem == null || !_runData.Market.ItemStock.Contains(_selectedItem))
            _selectedItem = _runData.Market.ItemStock.Count > 0 ? _runData.Market.ItemStock[0] : null;

        foreach (var item in _runData.Market.ItemStock)
        {
            if (item != null)
                _itemList.AddChild(CreateItemRow(item));
        }

        RefreshDetailPanel();
    }

    private void OnBuyPressed(ItemData item)
    {
        if (item == null || _runData.Gold < item.Cost)
            return;

        if (!_runData.TryBuyMarketItem(item))
            RefreshUi();
    }

    private void OnSelectedBuyPressed()
    {
        OnBuyPressed(_selectedItem);
    }

    private ItemStoreListRow CreateItemRow(ItemData item)
    {
        var row = _itemStoreListRowScene.Instantiate<ItemStoreListRow>();
        row.Configure(item, item == _selectedItem);
        row.DetailsPressed += SelectItem;
        return row;
    }

    private void SelectItem(ItemData item)
    {
        _selectedItem = item;
        RefreshUi();
    }

    private void RefreshDetailPanel()
    {
        foreach (var child in _showcaseHost.GetChildren())
            child.QueueFree();

        if (_selectedItem == null)
        {
            _detailIcon.Texture = null;
            _detailNameLabel.Text = "No items available";
            _detailTypeLabel.Text = string.Empty;
            _detailDescriptionLabel.Text = "The blacksmith has no equipment in stock.";
            _detailCostLabel.Text = "-";
            _buyButton.Disabled = true;
            _buyButton.Text = "Buy";
            return;
        }

        _detailIcon.Texture = _selectedItem.UiIcon;
        _detailNameLabel.Text = _selectedItem.DisplayName;
        _detailTypeLabel.Text = GetItemTypeLabel(_selectedItem);
        _detailDescriptionLabel.Text = _selectedItem.Description;
        _detailCostLabel.Text = _selectedItem.Cost.ToString();
        _buyButton.Disabled = _runData.Gold < _selectedItem.Cost;
        _buyButton.Text = _buyButton.Disabled ? "Not enough gold" : "Buy";

        var conditionPercent = Mathf.RoundToInt(Mathf.Clamp(_selectedItem.Condition, 0f, 1f) * 100f);
        if (conditionPercent < 100)
            AddGenericStat("Condition", $"{conditionPercent}%");

        AddSelectedItemShowcase();
    }

    private void AddSelectedItemShowcase()
    {
        var showcaseScene = GetShowcaseScene(_selectedItem);
        if (showcaseScene == null)
        {
            AddGenericStat("Details", "No detailed showcase available.");
            return;
        }

        var showcaseNode = showcaseScene.Instantiate<Control>();
        _showcaseHost.AddChild(showcaseNode);
        if (showcaseNode is IItemStoreShowcase showcase)
            showcase.Configure(_selectedItem);
        else
            GD.PushError($"Item showcase scene '{showcaseScene.ResourcePath}' does not implement IItemStoreShowcase.");
    }

    private PackedScene GetShowcaseScene(ItemData item)
    {
        return item switch
        {
            ArmorItemData => _armorItemShowcaseScene,
            MainHandItemData => _mainHandItemShowcaseScene,
            OffHandItemData => _offHandItemShowcaseScene,
            _ => null
        };
    }

    private void AddGenericStat(string label, string value)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(132, 0) });
        row.AddChild(new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _showcaseHost.AddChild(row);
    }

    private static string GetItemTypeLabel(ItemData item)
    {
        return item switch
        {
            ArmorItemData => "Armor",
            MainHandItemData mainHand => mainHand.IsTwoHanded ? "Two-handed main hand" : "Main hand",
            OffHandItemData => "Off hand",
            _ => "Unknown item type"
        };
    }
}
