using Godot;
using System.Collections.Generic;
using MobArena.Scripts.Resources.Contracts;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.Components.UI;

public partial class ArenaContractCard : Button
{
    private const string FameIconPath = "res://assets/ui/icons/fame.svg";
    private const string GoldIconPath = "res://assets/ui/icons/gold.svg";

    [Signal]
    public delegate void ContractSelectedEventHandler(int contractIndex);

    [Export]
    public int ContractIndex { get; set; }

    [Export]
    public ArenaContractData ContractData { get; set; }

    public int CurrentCompanyFame { get; private set; }

    private Label _titleLabel;
    private HBoxContainer _mobsRow;
    private HBoxContainer _rewardsRow;
    private Texture2D _fameIcon;
    private Texture2D _goldIcon;

    public override void _Ready()
    {
        ToggleMode = true;
        _titleLabel = GetNode<Label>("MarginContainer/Layout/Title");
        _mobsRow = GetNode<HBoxContainer>("MarginContainer/Layout/MobsRow");
        _rewardsRow = GetNode<HBoxContainer>("MarginContainer/Layout/RewardsRow");
        _fameIcon = ResourceLoader.Load<Texture2D>(FameIconPath);
        _goldIcon = ResourceLoader.Load<Texture2D>(GoldIconPath);

        Pressed += () => EmitSignal(SignalName.ContractSelected, ContractIndex);
        RefreshUi();
    }

    public void Configure(int contractIndex, ArenaContractData contractData, int currentCompanyFame)
    {
        ContractIndex = contractIndex;
        ContractData = contractData;
        CurrentCompanyFame = currentCompanyFame;
        RefreshUi();
    }

    public void SetCurrentCompanyFame(int currentCompanyFame)
    {
        CurrentCompanyFame = currentCompanyFame;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (_titleLabel == null)
            return;

        var netFameReward = ContractData?.GetNetFameReward(CurrentCompanyFame) ?? 0;
        _titleLabel.Text = ContractData?.DisplayName ?? "Contract";
        RebuildMobsRow();
        RebuildRow(
            _rewardsRow,
            CreateIconValue(_goldIcon, (ContractData?.GoldReward ?? 0).ToString()),
            CreateIconValue(_fameIcon, netFameReward >= 0 ? $"+{netFameReward}" : netFameReward.ToString()));
    }

    private void RebuildMobsRow()
    {
        foreach (var child in _mobsRow.GetChildren())
            child.QueueFree();

        foreach (var mobGroup in GetGroupedMobs())
            _mobsRow.AddChild(CreateIconValue(mobGroup.Mob?.Icon, $"x{mobGroup.Count}"));
    }

    private IEnumerable<(MobData Mob, int Count)> GetGroupedMobs()
    {
        var groupedMobs = new Dictionary<MobData, int>();
        if (ContractData?.Mobs != null)
        {
            foreach (var mob in ContractData.Mobs)
            {
                if (mob == null)
                    continue;

                groupedMobs.TryGetValue(mob, out var count);
                groupedMobs[mob] = count + 1;
            }
        }

        foreach (var pair in groupedMobs)
            yield return (pair.Key, pair.Value);
    }

    private static void RebuildRow(HBoxContainer row, params Control[] children)
    {
        foreach (var child in row.GetChildren())
            child.QueueFree();

        foreach (var child in children)
            row.AddChild(child);
    }

    private static HBoxContainer CreateIconValue(Texture2D icon, string text)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 34),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 5);

        row.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(30, 30),
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        });

        row.AddChild(new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        });

        return row;
    }
}
