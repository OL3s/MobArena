using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class RationsOverlay : Control
{
    private SaveNode _saveNode;
    private CompanyRunData _runData;
    private RationStoreData _rationStore;

    private Label _goldLabel;
    private Label _feedbackLabel;

    private Label _poorOwnedBadge;
    private Label _poorCostLabel;
    private Label _poorStockLabel;
    private Button _poorBuyOneButton;
    private Button _poorBuyFiveButton;

    private Label _commonOwnedBadge;
    private Label _commonCostLabel;
    private Label _commonStockLabel;
    private Button _commonBuyOneButton;
    private Button _commonBuyFiveButton;

    private Label _fineOwnedBadge;
    private Label _fineCostLabel;
    private Label _fineStockLabel;
    private Button _fineBuyOneButton;
    private Button _fineBuyFiveButton;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _runData = _saveNode.CompanyRunData;
        _runData.EnsureResources();
        _rationStore = _runData.Market.RationStore;

        _goldLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Summary/GoldLabel");
        _feedbackLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/FeedbackLabel");

        BindCard(RationStoreData.RationQuality.Poor, "PoorCard", out _poorOwnedBadge, out _poorCostLabel, out _poorStockLabel, out _poorBuyOneButton, out _poorBuyFiveButton);
        BindCard(RationStoreData.RationQuality.Common, "CommonCard", out _commonOwnedBadge, out _commonCostLabel, out _commonStockLabel, out _commonBuyOneButton, out _commonBuyFiveButton);
        BindCard(RationStoreData.RationQuality.Fine, "FineCard", out _fineOwnedBadge, out _fineCostLabel, out _fineStockLabel, out _fineBuyOneButton, out _fineBuyFiveButton);

        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton").Pressed += QueueFree;
        _runData.RunChanged += RefreshUi;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void BindCard(
        RationStoreData.RationQuality quality,
        string cardName,
        out Label ownedBadge,
        out Label costLabel,
        out Label stockLabel,
        out Button buyOneButton,
        out Button buyFiveButton)
    {
        var cardPath = $"CenterContainer/Panel/MarginContainer/Layout/RationCards/{cardName}/Content";
        ownedBadge = GetNode<Label>($"{cardPath}/Header/OwnedBadge");
        costLabel = GetNode<Label>($"{cardPath}/CostRow/CostLabel");
        stockLabel = GetNode<Label>($"{cardPath}/StockLabel");
        buyOneButton = GetNode<Button>($"{cardPath}/Buttons/BuyOneButton");
        buyFiveButton = GetNode<Button>($"{cardPath}/Buttons/BuyFiveButton");
        buyOneButton.Pressed += () => TryBuy(quality, 1);
        buyFiveButton.Pressed += () => TryBuy(quality, 5);
    }

    private void TryBuy(RationStoreData.RationQuality quality, int amount)
    {
        if (_rationStore.TryBuyRations(_runData, quality, amount))
        {
            _feedbackLabel.Text = $"Bought {amount} {GetQualityName(quality).ToLowerInvariant()} ration{(amount == 1 ? string.Empty : "s")}.";
            return;
        }

        var totalCost = _rationStore.GetCost(quality) * amount;
        _feedbackLabel.Text = _rationStore.GetStock(quality) < amount
            ? $"Only {_rationStore.GetStock(quality)} {GetQualityName(quality).ToLowerInvariant()} left."
            : $"Need {totalCost} gold.";
        RefreshUi();
    }

    private void RefreshUi()
    {
        _runData.EnsureResources();
        _rationStore = _runData.Market.RationStore;

        _goldLabel.Text = $"Gold: {_runData.Gold}";

        RefreshCard(RationStoreData.RationQuality.Poor, _poorOwnedBadge, _poorCostLabel, _poorStockLabel, _poorBuyOneButton, _poorBuyFiveButton);
        RefreshCard(RationStoreData.RationQuality.Common, _commonOwnedBadge, _commonCostLabel, _commonStockLabel, _commonBuyOneButton, _commonBuyFiveButton);
        RefreshCard(RationStoreData.RationQuality.Fine, _fineOwnedBadge, _fineCostLabel, _fineStockLabel, _fineBuyOneButton, _fineBuyFiveButton);
    }

    private void RefreshCard(
        RationStoreData.RationQuality quality,
        Label ownedBadge,
        Label costLabel,
        Label stockLabel,
        Button buyOneButton,
        Button buyFiveButton)
    {
        var cost = _rationStore.GetCost(quality);
        var stock = _rationStore.GetStock(quality);
        ownedBadge.Text = GetOwnedCount(quality).ToString();
        costLabel.Text = cost.ToString();
        stockLabel.Text = $"Stock {stock}";
        buyOneButton.Disabled = stock < 1 || _runData.Gold < cost;
        buyFiveButton.Disabled = stock < 5 || _runData.Gold < cost * 5;
    }

    private int GetOwnedCount(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => _runData.Rations.PoorRations,
            RationStoreData.RationQuality.Common => _runData.Rations.CommonRations,
            RationStoreData.RationQuality.Fine => _runData.Rations.FineRations,
            _ => 0
        };
    }

    private static string GetQualityName(RationStoreData.RationQuality quality)
    {
        return quality switch
        {
            RationStoreData.RationQuality.Poor => "Poor",
            RationStoreData.RationQuality.Common => "Common",
            RationStoreData.RationQuality.Fine => "Fine",
            _ => "Unknown"
        };
    }
}
