using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class CompletedCompaniesOverlay : Control
{
    private SaveNode _saveNode;
    private Control _body;
    private Label _noRecordsLabel;
    private VBoxContainer _recordList;
    private Label _emptyLabel;
    private Label _titleLabel;
    private CompanyLogo _companyLogo;
    private Control _stats;
    private Label _fameValue;
    private Label _gladiatorsValue;
    private Label _deathsValue;
    private Label _earnedGoldValue;
    private Label _contractsValue;
    private Label _mobsValue;
    private Label _championsValue;
    private Button _deleteButton;
    private int _selectedIndex = -1;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _body = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Content/Body");
        _noRecordsLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/NoRecordsLabel");
        _recordList = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/Body/ListPanel/ScrollContainer/RecordList");
        _emptyLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/EmptyLabel");
        _titleLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/CompanyName");
        _companyLogo = GetNode<CompanyLogo>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Logo");
        _stats = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats");
        _fameValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/FameValue");
        _gladiatorsValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/GladiatorsValue");
        _deathsValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/DeathsValue");
        _earnedGoldValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/EarnedGoldValue");
        _contractsValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/ContractsValue");
        _mobsValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/MobsValue");
        _championsValue = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats/ChampionsValue");
        _deleteButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/DeleteButton");

        _deleteButton.Pressed += OnDeletePressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CloseButton").Pressed += QueueFree;

        RefreshList();
        SelectRecord(-1);
    }

    private void RefreshList()
    {
        foreach (var child in _recordList.GetChildren())
            child.QueueFree();

        var records = _saveNode.CompletedCompanyHistory.Records;
        var hasRecords = records.Count > 0;
        _body.Visible = hasRecords;
        _noRecordsLabel.Visible = !hasRecords;

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            var button = new Button
            {
                CustomMinimumSize = new Vector2(260, 52),
                Text = $"{index + 1}. {record.CompanyName}\nFame {record.FinalFame}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                FocusMode = FocusModeEnum.All
            };

            var capturedIndex = index;
            button.Pressed += () => SelectRecord(capturedIndex);
            _recordList.AddChild(button);
        }
    }

    private void SelectRecord(int index)
    {
        _selectedIndex = index;
        var record = _saveNode.CompletedCompanyHistory.GetRecordOrNull(index);
        var hasRecord = record != null;

        _emptyLabel.Visible = !hasRecord;
        _titleLabel.Visible = hasRecord;
        _companyLogo.Visible = hasRecord;
        _stats.Visible = hasRecord;
        _deleteButton.Disabled = !hasRecord;

        if (!hasRecord)
        {
            _titleLabel.Text = string.Empty;
            return;
        }

        var career = record.CompanyCareerData;
        _titleLabel.Text = record.CompanyName;
        _companyLogo.SetLogoData(record.CompanyLogoData);
        _fameValue.Text = record.FinalFame.ToString();
        _gladiatorsValue.Text = career.TotalGladiatorsInCareer.ToString();
        _deathsValue.Text = career.GladiatorsDead.ToString();
        _earnedGoldValue.Text = career.TotalGoldEarned.ToString();
        _contractsValue.Text = career.ContractsCompleted.ToString();
        _mobsValue.Text = career.MobsKilled.ToString();
        _championsValue.Text = career.ChampionsDefeated.ToString();
    }

    private void OnDeletePressed()
    {
        if (!_saveNode.CompletedCompanyHistory.TryDeleteRecord(_selectedIndex))
            return;

        _saveNode.Save();
        RefreshList();
        SelectRecord(-1);
    }
}
