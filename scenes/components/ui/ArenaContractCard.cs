using Godot;
using System.Collections.Generic;
using MobArena.Scripts.Resources.Contracts;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.Components.UI;

public partial class ArenaContractCard : Button
{
    private const string FameIconPath = "res://assets/ui/icons/fame.svg";
    private const string GoldIconPath = "res://assets/ui/icons/gold.svg";
    private const string StarIconPath = "res://assets/ui/icons/star.svg";
    private const string ChampionIconPath = "res://assets/ui/icons/champion.svg";

    [Signal]
    public delegate void ContractSelectedEventHandler(int contractIndex);

    [Export]
    public int ContractIndex { get; set; }

    [Export]
    public ArenaContractData ContractData { get; set; }

    public int CurrentCompanyFame { get; private set; }

    private HBoxContainer _difficultyStars;
    private TextureRect _familyIcon;
    private Label _familyNameLabel;
    private TextureRect _championIcon;
    private GridContainer _mobsGrid;
    private HBoxContainer _rewardsRow;
    private Texture2D _fameIcon;
    private Texture2D _goldIcon;
    private Texture2D _starIcon;
    private Texture2D _championTexture;

    public override void _Ready()
    {
        ToggleMode = true;
        _difficultyStars = GetNode<HBoxContainer>("MarginContainer/Layout/DifficultyStars");
        _familyIcon = GetNode<TextureRect>("MarginContainer/Layout/MobPanel/MobPanelMargin/CenterContainer/MobPanelLayout/FamilyRow/FamilyIcon");
        _familyNameLabel = GetNode<Label>("MarginContainer/Layout/MobPanel/MobPanelMargin/CenterContainer/MobPanelLayout/FamilyRow/FamilyName");
        _championIcon = GetNode<TextureRect>("MarginContainer/Layout/MobPanel/MobPanelMargin/CenterContainer/MobPanelLayout/FamilyRow/ChampionIcon");
        _mobsGrid = GetNode<GridContainer>("MarginContainer/Layout/MobPanel/MobPanelMargin/CenterContainer/MobPanelLayout/MobsGrid");
        _rewardsRow = GetNode<HBoxContainer>("MarginContainer/Layout/RewardsRow");
        _fameIcon = ResourceLoader.Load<Texture2D>(FameIconPath);
        _goldIcon = ResourceLoader.Load<Texture2D>(GoldIconPath);
        _starIcon = ResourceLoader.Load<Texture2D>(StarIconPath);
        _championTexture = ResourceLoader.Load<Texture2D>(ChampionIconPath);
        _championIcon.Texture = _championTexture;

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
        if (_familyNameLabel == null)
            return;

        var netFameReward = ContractData?.GetNetFameReward(CurrentCompanyFame) ?? 0;
        _familyNameLabel.Text = (ContractData?.Family ?? MobFamily.Slimes).ToString();
        _familyIcon.Texture = ResourceLoader.Load<Texture2D>(GetFamilyIconPath(ContractData?.Family ?? MobFamily.Slimes));
        _championIcon.Visible = ContractData?.IsChampionContract() == true;
        RebuildDifficultyStars();
        RebuildMobsGrid();
        RebuildRow(
            _rewardsRow,
            CreateIconValue(_goldIcon, (ContractData?.GoldReward ?? 0).ToString()),
            CreateIconValue(_fameIcon, netFameReward >= 0 ? $"+{netFameReward}" : netFameReward.ToString()));
    }

    private void RebuildDifficultyStars()
    {
        foreach (var child in _difficultyStars.GetChildren())
            child.QueueFree();

        var starCount = ContractData?.GetThreatStarCount() ?? 1;

        for (var i = 0; i < starCount; i++)
        {
            _difficultyStars.AddChild(new TextureRect
            {
                CustomMinimumSize = new Vector2(22, 22),
                Texture = _starIcon,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            });
        }
    }

    private void RebuildMobsGrid()
    {
        foreach (var child in _mobsGrid.GetChildren())
            child.QueueFree();

        foreach (var mobGroup in GetGroupedMobs())
            _mobsGrid.AddChild(CreateIconValue(mobGroup.Mob?.GetUiIconTexture(), $"x{mobGroup.Count}"));
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

    private static string GetFamilyIconPath(MobFamily family)
    {
        return family switch
        {
            MobFamily.Goblins => "res://assets/ui/icons/family_goblins.svg",
            MobFamily.Undead => "res://assets/ui/icons/family_undead.svg",
            MobFamily.Demons => "res://assets/ui/icons/family_demons.svg",
            _ => "res://assets/ui/icons/family_slimes.svg"
        };
    }

}
