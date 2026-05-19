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
    private Label _ownedSummaryLabel;
    private Label _feedbackLabel;

    private Label _poorOwnedLabel;
    private Label _poorStockLabel;
    private Label _poorCostLabel;
    private Label _poorValueLabel;
    private Button _poorBuyOneButton;
    private Button _poorBuyFiveButton;

    private Label _commonOwnedLabel;
    private Label _commonStockLabel;
    private Label _commonCostLabel;
    private Label _commonValueLabel;
    private Button _commonBuyOneButton;
    private Button _commonBuyFiveButton;

    private Label _fineOwnedLabel;
    private Label _fineStockLabel;
    private Label _fineCostLabel;
    private Label _fineValueLabel;
    private Button _fineBuyOneButton;
    private Button _fineBuyFiveButton;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _runData = _saveNode.CompanyRunData;
        _runData.EnsureResources();
        _rationStore = _runData.Market.RationStore;

        _goldLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Summary/GoldLabel");
        _ownedSummaryLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Summary/OwnedSummaryLabel");
        _feedbackLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/FeedbackLabel");

        BindPoorRow();
        BindCommonRow();
        BindFineRow();

        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton").Pressed += QueueFree;
        _runData.RunChanged += RefreshUi;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void BindPoorRow()
    {
        const string rowPath = "CenterContainer/Panel/MarginContainer/Layout/RationRows/PoorRow/Content";
        _poorOwnedLabel = GetNode<Label>($"{rowPath}/Details/OwnedLabel");
        _poorStockLabel = GetNode<Label>($"{rowPath}/Details/StockLabel");
        _poorCostLabel = GetNode<Label>($"{rowPath}/Details/CostLabel");
        _poorValueLabel = GetNode<Label>($"{rowPath}/Details/ValueLabel");
        _poorBuyOneButton = GetNode<Button>($"{rowPath}/Buttons/BuyOneButton");
        _poorBuyFiveButton = GetNode<Button>($"{rowPath}/Buttons/BuyFiveButton");
        _poorBuyOneButton.Pressed += () => TryBuy(RationStoreData.RationQuality.Poor, 1);
        _poorBuyFiveButton.Pressed += () => TryBuy(RationStoreData.RationQuality.Poor, 5);
    }

    private void BindCommonRow()
    {
        const string rowPath = "CenterContainer/Panel/MarginContainer/Layout/RationRows/CommonRow/Content";
        _commonOwnedLabel = GetNode<Label>($"{rowPath}/Details/OwnedLabel");
        _commonStockLabel = GetNode<Label>($"{rowPath}/Details/StockLabel");
        _commonCostLabel = GetNode<Label>($"{rowPath}/Details/CostLabel");
        _commonValueLabel = GetNode<Label>($"{rowPath}/Details/ValueLabel");
        _commonBuyOneButton = GetNode<Button>($"{rowPath}/Buttons/BuyOneButton");
        _commonBuyFiveButton = GetNode<Button>($"{rowPath}/Buttons/BuyFiveButton");
        _commonBuyOneButton.Pressed += () => TryBuy(RationStoreData.RationQuality.Common, 1);
        _commonBuyFiveButton.Pressed += () => TryBuy(RationStoreData.RationQuality.Common, 5);
    }

    private void BindFineRow()
    {
        const string rowPath = "CenterContainer/Panel/MarginContainer/Layout/RationRows/FineRow/Content";
        _fineOwnedLabel = GetNode<Label>($"{rowPath}/Details/OwnedLabel");
        _fineStockLabel = GetNode<Label>($"{rowPath}/Details/StockLabel");
        _fineCostLabel = GetNode<Label>($"{rowPath}/Details/CostLabel");
        _fineValueLabel = GetNode<Label>($"{rowPath}/Details/ValueLabel");
        _fineBuyOneButton = GetNode<Button>($"{rowPath}/Buttons/BuyOneButton");
        _fineBuyFiveButton = GetNode<Button>($"{rowPath}/Buttons/BuyFiveButton");
        _fineBuyOneButton.Pressed += () => TryBuy(RationStoreData.RationQuality.Fine, 1);
        _fineBuyFiveButton.Pressed += () => TryBuy(RationStoreData.RationQuality.Fine, 5);
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
            ? $"The market does not have {amount} {GetQualityName(quality).ToLowerInvariant()} rations left."
            : $"Not enough gold. Need {totalCost} gold.";
        RefreshUi();
    }

    private void RefreshUi()
    {
        _runData.EnsureResources();
        _rationStore = _runData.Market.RationStore;

        _goldLabel.Text = $"Gold: {_runData.Gold}";
        _ownedSummaryLabel.Text = $"Owned: {_runData.Rations.GetTotal()} total ({_runData.Rations.PoorRations} poor, {_runData.Rations.CommonRations} common, {_runData.Rations.FineRations} fine)";

        RefreshRow(RationStoreData.RationQuality.Poor, _poorOwnedLabel, _poorStockLabel, _poorCostLabel, _poorValueLabel, _poorBuyOneButton, _poorBuyFiveButton);
        RefreshRow(RationStoreData.RationQuality.Common, _commonOwnedLabel, _commonStockLabel, _commonCostLabel, _commonValueLabel, _commonBuyOneButton, _commonBuyFiveButton);
        RefreshRow(RationStoreData.RationQuality.Fine, _fineOwnedLabel, _fineStockLabel, _fineCostLabel, _fineValueLabel, _fineBuyOneButton, _fineBuyFiveButton);
    }

    private void RefreshRow(
        RationStoreData.RationQuality quality,
        Label ownedLabel,
        Label stockLabel,
        Label costLabel,
        Label valueLabel,
        Button buyOneButton,
        Button buyFiveButton)
    {
        var cost = _rationStore.GetCost(quality);
        var stock = _rationStore.GetStock(quality);
        ownedLabel.Text = $"Owned: {GetOwnedCount(quality)}";
        stockLabel.Text = $"Stock: {stock}";
        costLabel.Text = $"Cost: {cost} gold";
        valueLabel.Text = $"Provision value: {_rationStore.GetProvisionValue(quality):0}";
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
