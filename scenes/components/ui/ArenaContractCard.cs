using Godot;

namespace MobArena.Scenes.Components.UI;

public partial class ArenaContractCard : Button
{
    [Signal]
    public delegate void ContractSelectedEventHandler(int contractIndex);

    [Export]
    public int ContractIndex { get; set; }

    [Export]
    public string ContractTitle { get; set; } = "Contract";

    [Export]
    public string EnemyPreview { get; set; } = "Enemies unknown";

    [Export]
    public string RewardText { get; set; } = "Reward unknown";

    [Export]
    public string RiskText { get; set; } = "Risk unknown";

    private Label _titleLabel;
    private Label _enemyLabel;
    private Label _rewardLabel;
    private Label _riskLabel;

    public override void _Ready()
    {
        ToggleMode = true;
        _titleLabel = GetNode<Label>("MarginContainer/Layout/Title");
        _enemyLabel = GetNode<Label>("MarginContainer/Layout/EnemyPreview");
        _rewardLabel = GetNode<Label>("MarginContainer/Layout/Reward");
        _riskLabel = GetNode<Label>("MarginContainer/Layout/Risk");

        Pressed += () => EmitSignal(SignalName.ContractSelected, ContractIndex);
        RefreshText();
    }

    public void Configure(int contractIndex, string contractTitle, string enemyPreview, string rewardText, string riskText)
    {
        ContractIndex = contractIndex;
        ContractTitle = contractTitle;
        EnemyPreview = enemyPreview;
        RewardText = rewardText;
        RiskText = riskText;
        RefreshText();
    }

    private void RefreshText()
    {
        if (_titleLabel == null)
            return;

        _titleLabel.Text = ContractTitle;
        _enemyLabel.Text = EnemyPreview;
        _rewardLabel.Text = RewardText;
        _riskLabel.Text = RiskText;
    }
}
