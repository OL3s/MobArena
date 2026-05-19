using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class BlacksmithStoreOverlay : Control
{
    private CompanyRunData _runData;
    private Label _goldLabel;
    private VBoxContainer _itemList;

    public override void _Ready()
    {
        _runData = SaveNode.Get().CompanyRunData;
        _runData.EnsureResources();

        _goldLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Header/GoldLabel");
        _itemList = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ScrollContainer/ItemList");

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
        _goldLabel.Text = $"Gold: {_runData.Gold}";

        foreach (var child in _itemList.GetChildren())
            child.QueueFree();

        foreach (var item in _runData.Market.ItemStock)
        {
            if (item != null)
                _itemList.AddChild(CreateItemRow(item));
        }
    }

    private Control CreateItemRow(ItemData item)
    {
        var row = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 74)
        };

        var layout = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddThemeConstantOverride("separation", 12);
        row.AddChild(layout);

        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddChild(text);

        text.AddChild(new Label
        {
            Text = item.DisplayName,
            ThemeTypeVariation = "HeaderSmall"
        });

        text.AddChild(new Label
        {
            Text = $"{GetItemKind(item)} | Condition {item.Condition * 100f:0}% | Cost {item.Cost}g",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var buyButton = new Button
        {
            Text = "Buy",
            CustomMinimumSize = new Vector2(120, 0),
            Disabled = _runData.Gold < item.Cost
        };
        buyButton.Pressed += () => OnBuyPressed(item);
        layout.AddChild(buyButton);

        return row;
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

    private static string GetItemKind(ItemData item)
    {
        return item switch
        {
            ArmorItemData => "Armor",
            MainHandItemData mainHand => mainHand.IsTwoHanded ? "Two-Handed" : "Main Hand",
            OffHandItemData => "Off Hand",
            _ => "Item"
        };
    }
}
