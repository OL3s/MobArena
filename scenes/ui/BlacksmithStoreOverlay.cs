using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.Components.UI.ItemShowcases;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class BlacksmithStoreOverlay : Control
{
    private enum StoreItemTypeCategory
    {
        All,
        MainHand,
        OffHand,
        Armor,
        Coating,
        Other
    }

    private enum StoreSortMode
    {
        Default,
        Name,
        Price,
        Weight,
        Durability
    }

    private const string ItemStoreListRowScenePath = "res://scenes/components/ui/ItemStoreListRow.tscn";
    private const string ArmorItemShowcaseScenePath = "res://scenes/components/ui/item_showcases/ArmorItemShowcase.tscn";
    private const string MainHandItemShowcaseScenePath = "res://scenes/components/ui/item_showcases/MainHandItemShowcase.tscn";
    private const string OffHandItemShowcaseScenePath = "res://scenes/components/ui/item_showcases/OffHandItemShowcase.tscn";
    private const string StoreDetailRowScenePath = "res://scenes/ui/StoreDetailRow.tscn";

    private CompanyRunData _runData;
    private Label _goldLabel;
    private OptionButton _filterOptions;
    private OptionButton _sortOptions;
    private VBoxContainer _itemList;
    private HBoxContainer _categoryDividerTemplate;
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
    private PackedScene _storeDetailRowScene;
    private ItemData _selectedItem;
    private StoreItemTypeCategory _selectedTypeCategory = StoreItemTypeCategory.All;
    private StoreSortMode _selectedSortMode = StoreSortMode.Default;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _goldLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Header/GoldLabel");
        _filterOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/SortRow/FilterOptions");
        _sortOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/SortRow/SortOptions");
        _itemList = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/ItemListPanel/ItemListMargin/ItemListScroll/ItemList");
        _categoryDividerTemplate = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/StoreBody/ItemListPanel/ItemListMargin/CategoryDividerTemplate");
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
        _storeDetailRowScene = ResourceLoader.Load<PackedScene>(StoreDetailRowScenePath);

        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;
        ConfigureFilterOptions();
        ConfigureSortOptions();
        _filterOptions.ItemSelected += OnFilterSelected;
        _sortOptions.ItemSelected += OnSortSelected;
        _buyButton.Pressed += OnSelectedBuyPressed;
        _runData.RunChanged += RefreshUi;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
        if (_filterOptions != null)
            _filterOptions.ItemSelected -= OnFilterSelected;
        if (_sortOptions != null)
            _sortOptions.ItemSelected -= OnSortSelected;
    }

    private void ConfigureFilterOptions()
    {
        _filterOptions.Clear();
        AddFilterOption(StoreItemTypeCategory.All);
        AddFilterOption(StoreItemTypeCategory.MainHand);
        AddFilterOption(StoreItemTypeCategory.OffHand);
        AddFilterOption(StoreItemTypeCategory.Armor);
        AddFilterOption(StoreItemTypeCategory.Coating);
    }

    private void AddFilterOption(StoreItemTypeCategory category)
    {
        _filterOptions.AddIconItem(LoadCategoryIcon(category), category.ToString(), (int)category);
    }

    private void ConfigureSortOptions()
    {
        _sortOptions.Clear();
        foreach (var sortMode in System.Enum.GetValues<StoreSortMode>())
            _sortOptions.AddItem(sortMode.ToString(), (int)sortMode);
    }

    private void OnFilterSelected(long index)
    {
        _selectedTypeCategory = (StoreItemTypeCategory)_filterOptions.GetItemId((int)index);
        RefreshUi();
    }

    private void OnSortSelected(long index)
    {
        _selectedSortMode = (StoreSortMode)_sortOptions.GetItemId((int)index);
        RefreshUi();
    }

    private void RefreshUi()
    {
        _runData.EnsureResources();
        _goldLabel.Text = _runData.Gold.ToString();

        foreach (var child in _itemList.GetChildren())
            child.QueueFree();

        var visibleItems = GetVisibleSortedItems().ToList();

        if (_selectedItem == null || !visibleItems.Contains(_selectedItem))
            _selectedItem = visibleItems.Count > 0 ? visibleItems[0] : null;

        StoreItemTypeCategory? previousCategory = null;
        foreach (var item in visibleItems)
        {
            if (item == null)
                continue;

            var category = GetItemTypeCategory(item);
            if (ShouldShowCategoryDividers() && category != previousCategory)
            {
                _itemList.AddChild(CreateCategoryDivider(category));
                previousCategory = category;
            }

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

    private Control CreateCategoryDivider(StoreItemTypeCategory category)
    {
        var divider = _categoryDividerTemplate.Duplicate() as HBoxContainer;
        if (divider == null)
        {
            GD.PushError("Store category divider template is missing or has the wrong root type.");
            return new Control();
        }

        divider.Show();
        divider.GetNode<TextureRect>("Icon").Texture = LoadCategoryIcon(category);
        divider.GetNode<Label>("Label").Text = category.ToString();
        return divider;
    }

    private IEnumerable<ItemData> GetVisibleSortedItems()
    {
        if (_runData?.Market?.ItemStock == null)
            return Enumerable.Empty<ItemData>();

        return _runData.Market.ItemStock
            .Where(item => item != null)
            .Where(item => _selectedTypeCategory == StoreItemTypeCategory.All || GetItemTypeCategory(item) == _selectedTypeCategory)
            .OrderBy(item => GetPrimarySortValue(item))
            .ThenBy(item => GetSortTieBreaker(item))
            .ThenBy(item => GetDefaultStrengthSortValue(item))
            .ThenBy(item => item.Cost)
            .ThenBy(item => item.DisplayName);
    }

    private bool ShouldShowCategoryDividers()
    {
        return _selectedTypeCategory == StoreItemTypeCategory.All && _selectedSortMode == StoreSortMode.Default;
    }

    private IComparable GetPrimarySortValue(ItemData item)
    {
        return _selectedSortMode switch
        {
            StoreSortMode.Name => item.DisplayName,
            StoreSortMode.Price => item.Cost,
            StoreSortMode.Weight => item is EquipmentItemData equipment ? equipment.Weight : int.MaxValue,
            StoreSortMode.Durability => item.MaxDurability,
            _ => GetTypeSortOrder(GetItemTypeCategory(item))
        };
    }

    private IComparable GetSortTieBreaker(ItemData item)
    {
        return _selectedSortMode switch
        {
            StoreSortMode.Name => item.Cost,
            StoreSortMode.Price => item.DisplayName,
            StoreSortMode.Weight => item.DisplayName,
            StoreSortMode.Durability => item.DisplayName,
            _ => item.TypeTag
        };
    }

    private IComparable GetDefaultStrengthSortValue(ItemData item)
    {
        return _selectedSortMode == StoreSortMode.Default ? GetStrengthSortOrder(item.StrengthTag) : 0;
    }

    private static int GetStrengthSortOrder(ItemStrengthTag strengthTag)
    {
        return strengthTag switch
        {
            ItemStrengthTag.Training => 1,
            ItemStrengthTag.Stone => 2,
            ItemStrengthTag.Wood => 3,
            ItemStrengthTag.Bronze => 4,
            ItemStrengthTag.Iron => 5,
            ItemStrengthTag.Black => 6,
            ItemStrengthTag.Weak => 7,
            ItemStrengthTag.Standard => 8,
            ItemStrengthTag.Strong => 9,
            ItemStrengthTag.Light => 10,
            ItemStrengthTag.Medium => 11,
            ItemStrengthTag.Heavy => 12,
            _ => 0
        };
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
            _detailDescriptionLabel.Text = "The market has no items in stock.";
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

        AddGenericStat("Max durability", _selectedItem.MaxDurability.ToString());
        if (_selectedItem is EquipmentItemData equipment)
            AddGenericStat("Weight", equipment.Weight.ToString());

        AddSelectedItemDetails();
    }

    private void AddSelectedItemDetails()
    {
        if (_selectedItem is ItemCoatingData coating)
        {
            AddCoatingStats(coating);
            return;
        }

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

    private void AddCoatingStats(ItemCoatingData coating)
    {
        switch (coating)
        {
            case AdditiveItemCoatingData additive:
                if (additive.DamageEntries.Count > 0)
                {
                    AddSectionLabel("Instant");
                    foreach (var damage in additive.DamageEntries)
                    {
                        if (damage != null)
                            AddGenericStat(damage.Type.ToString(), $"+{damage.Value}");
                    }
                }

                if (additive.EffectEntries.Count > 0)
                {
                    AddSectionLabel("Effects");
                    foreach (var effect in additive.EffectEntries)
                    {
                        if (effect != null)
                            AddGenericStat(effect.Type.ToString(), $"+{effect.Value:0}");
                    }
                }
                break;
            case MultiplierItemCoatingData multiplier:
                if (multiplier.DamageMultipliers.Count > 0)
                {
                    AddSectionLabel("Instant");
                    foreach (var damageMultiplier in multiplier.DamageMultipliers)
                    {
                        if (damageMultiplier != null)
                            AddGenericStat(damageMultiplier.Type.ToString(), $"x{damageMultiplier.Multiplier:0.##}");
                    }
                }
                break;
        }
    }

    private void AddSectionLabel(string label)
    {
        _showcaseHost.AddChild(new Label
        {
            Text = label,
            ThemeTypeVariation = "HeaderSmall"
        });
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
        var row = _storeDetailRowScene?.Instantiate<StoreDetailRow>();
        if (row == null)
        {
            GD.PushError("Store detail row scene is missing or has the wrong root script.");
            return;
        }

        _showcaseHost.AddChild(row);
        row.Configure(label, value);
    }

    private static string GetItemTypeLabel(ItemData item)
    {
        return item switch
        {
            ArmorItemData => "Armor",
            MainHandItemData mainHand => mainHand.IsTwoHanded ? "Two-handed main hand" : "Main hand",
            OffHandItemData => "Off hand",
            ItemCoatingData => "Coating",
            _ => "Unknown item type"
        };
    }

    private static StoreItemTypeCategory GetItemTypeCategory(ItemData item)
    {
        return item switch
        {
            MainHandItemData => StoreItemTypeCategory.MainHand,
            OffHandItemData => StoreItemTypeCategory.OffHand,
            ArmorItemData => StoreItemTypeCategory.Armor,
            ItemCoatingData => StoreItemTypeCategory.Coating,
            _ => StoreItemTypeCategory.Other
        };
    }

    private static int GetTypeSortOrder(StoreItemTypeCategory category)
    {
        return category switch
        {
            StoreItemTypeCategory.MainHand => 0,
            StoreItemTypeCategory.OffHand => 1,
            StoreItemTypeCategory.Armor => 2,
            StoreItemTypeCategory.Coating => 3,
            StoreItemTypeCategory.Other => 4,
            _ => 6
        };
    }

    private static Texture2D LoadCategoryIcon(StoreItemTypeCategory category)
    {
        var path = category switch
        {
            StoreItemTypeCategory.All => "res://assets/ui/items/type_item.svg",
            StoreItemTypeCategory.MainHand => "res://assets/ui/items/type_main_hand.svg",
            StoreItemTypeCategory.OffHand => "res://assets/ui/items/type_off_hand.svg",
            StoreItemTypeCategory.Armor => "res://assets/ui/items/type_armor.svg",
            StoreItemTypeCategory.Coating => "res://assets/ui/coatings/type_coating.svg",
            _ => UiIconLoader.FallbackIconPath
        };

        return UiIconLoader.LoadIcon(path);
    }
}
