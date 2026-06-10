using Godot;
using System.Collections.Generic;
using System.Linq;
using MobArena.Scripts.Resources.Items;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.UI;

public partial class CodexOverlay : Control
{
    private enum CodexCategory
    {
        Enemies,
        Items
    }

    private enum ItemTypeCategory
    {
        Armor,
        MainHand,
        OffHand,
        Other
    }

    private const string ItemResourceDirectory = "res://resources/items";
    private const string ChampionIconPath = "res://assets/ui/icons/champion.svg";

    [Export]
    public PackedScene GroupPanelScene { get; set; }

    [Export]
    public PackedScene EntryRowScene { get; set; }

    [Export]
    public PackedScene StatRowScene { get; set; }

    private Button _enemiesButton;
    private Button _itemsButton;
    private VBoxContainer _entryList;
    private Label _emptyListLabel;
    private Label _emptyDetailsLabel;
    private TextureRect _icon;
    private Label _titleLabel;
    private Label _descriptionLabel;
    private VBoxContainer _stats;
    private Texture2D _championIcon;
    private Texture2D _twoHandedIcon;
    private readonly Dictionary<ItemTypeCategory, Texture2D> _itemTypeIcons = new();
    private CodexCategory _category = CodexCategory.Enemies;
    private readonly List<EnemyMobFamilyData> _enemyFamilies = new();
    private readonly List<ItemData> _items = new();
    private readonly Dictionary<EnemyMobFamilyData, bool> _expandedEnemyFamilies = new();
    private readonly Dictionary<ItemTypeCategory, bool> _expandedItemTypes = new();

    public override void _Ready()
    {
        _enemiesButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CategoryRow/EnemiesButton");
        _itemsButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CategoryRow/ItemsButton");
        _entryList = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/Body/ListPanel/ScrollContainer/EntryList");
        _emptyListLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/ListPanel/EmptyListLabel");
        _emptyDetailsLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/EmptyDetailsLabel");
        _icon = GetNode<TextureRect>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Icon");
        _titleLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Title");
        _descriptionLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Description");
        _stats = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats");
        _championIcon = ResourceLoader.Load<Texture2D>(ChampionIconPath);
        _twoHandedIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/items/type_two_handed.svg");
        LoadItemTypeIcons();

        _enemiesButton.Pressed += () => SelectCategory(CodexCategory.Enemies);
        _itemsButton.Pressed += () => SelectCategory(CodexCategory.Items);
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CloseButton").Pressed += QueueFree;

        LoadResources();
        SelectCategory(CodexCategory.Enemies);
    }

    private void LoadResources()
    {
        _enemyFamilies.Clear();
        _items.Clear();

        _enemyFamilies.AddRange(MobFamilyCatalog.LoadEnemyFamiliesList());

        foreach (var path in GetTresPaths(ItemResourceDirectory))
        {
            var item = ResourceLoader.Load<ItemData>(path);
            if (item != null)
                _items.Add(item);
        }
    }

    private void SelectCategory(CodexCategory category)
    {
        _category = category;
        _enemiesButton.ButtonPressed = category == CodexCategory.Enemies;
        _itemsButton.ButtonPressed = category == CodexCategory.Items;
        RefreshEntryList();
        ClearDetails();
    }

    private void RefreshEntryList()
    {
        foreach (var child in _entryList.GetChildren())
            child.QueueFree();

        if (_category == CodexCategory.Enemies)
        {
            _emptyListLabel.Visible = _enemyFamilies.Count <= 0;
            foreach (var enemyFamily in GetSortedEnemyFamilies())
                AddEnemyGroup(enemyFamily);
            return;
        }

        _emptyListLabel.Visible = _items.Count <= 0;
        foreach (var itemGroup in GetSortedItemGroups())
            AddItemGroup(itemGroup.Key, itemGroup);
    }

    private IEnumerable<EnemyMobFamilyData> GetSortedEnemyFamilies()
    {
        return _enemyFamilies
            .OrderBy(family => family.FameValue)
            .ThenBy(family => family.DisplayName);
    }

    private void AddEnemyGroup(EnemyMobFamilyData family)
    {
        if (!_expandedEnemyFamilies.ContainsKey(family))
            _expandedEnemyFamilies[family] = false;

        var isExpanded = _expandedEnemyFamilies[family];
        var groupEnemies = family.Mobs
            .Where(entry => entry?.Mob != null)
            .Select(entry => entry.Mob)
            .OrderBy(enemy => enemy.FameValue)
            .ThenBy(enemy => enemy.DisplayName)
            .ToList();

        var groupPanel = GroupPanelScene?.Instantiate<CodexGroupPanel>();
        if (groupPanel == null)
        {
            GD.PushError("Codex group panel scene is missing or has the wrong root script.");
            return;
        }

        groupPanel.Configure(family.DisplayName, family.UiIcon, isExpanded);
        groupPanel.HeaderPressed += () =>
        {
            _expandedEnemyFamilies[family] = !_expandedEnemyFamilies[family];
            RefreshEntryList();
        };

        if (!isExpanded)
        {
            _entryList.AddChild(groupPanel);
            return;
        }

        foreach (var enemy in groupEnemies)
            groupPanel.Content.AddChild(CreateEnemyEntryRow(enemy));

        _entryList.AddChild(groupPanel);
    }

    private Control CreateEnemyEntryRow(EnemyMobData enemy)
    {
        var row = EntryRowScene?.Instantiate<CodexEntryRow>();
        if (row == null)
        {
            GD.PushError("Codex entry row scene is missing or has the wrong root script.");
            return new Control();
        }

        row.Configure(enemy.DisplayName, enemy.GetUiIconTexture(), enemy is ChampionMobData ? _championIcon : null);
        row.EntryPressed += () => ShowEnemy(enemy);
        return row;
    }

    private IEnumerable<IGrouping<ItemTypeCategory, ItemData>> GetSortedItemGroups()
    {
        return _items
            .GroupBy(GetItemTypeCategory)
            .OrderBy(group => group.Sum(item => item.Cost))
            .ThenBy(group => GetItemTypeLabel(group.Key));
    }

    private void AddItemGroup(ItemTypeCategory itemType, IEnumerable<ItemData> items)
    {
        if (!_expandedItemTypes.ContainsKey(itemType))
            _expandedItemTypes[itemType] = false;

        var isExpanded = _expandedItemTypes[itemType];
        var groupItems = items
            .OrderBy(item => item.Cost)
            .ThenBy(item => item.DisplayName)
            .ToList();

        var groupPanel = GroupPanelScene?.Instantiate<CodexGroupPanel>();
        if (groupPanel == null)
        {
            GD.PushError("Codex group panel scene is missing or has the wrong root script.");
            return;
        }

        groupPanel.Configure(GetItemTypeLabel(itemType), GetItemTypeIcon(itemType), isExpanded);
        groupPanel.HeaderPressed += () =>
        {
            _expandedItemTypes[itemType] = !_expandedItemTypes[itemType];
            RefreshEntryList();
        };

        if (!isExpanded)
        {
            _entryList.AddChild(groupPanel);
            return;
        }

        foreach (var item in groupItems)
            groupPanel.Content.AddChild(CreateItemEntryRow(item));

        _entryList.AddChild(groupPanel);
    }

    private Control CreateItemEntryRow(ItemData item)
    {
        var row = EntryRowScene?.Instantiate<CodexEntryRow>();
        if (row == null)
        {
            GD.PushError("Codex entry row scene is missing or has the wrong root script.");
            return new Control();
        }

        row.Configure(item.DisplayName, item.UiIcon, item is MainHandItemData { IsTwoHanded: true } ? _twoHandedIcon : null);
        row.EntryPressed += () => ShowItem(item);
        return row;
    }

    private void LoadItemTypeIcons()
    {
        _itemTypeIcons[ItemTypeCategory.Armor] = ResourceLoader.Load<Texture2D>("res://assets/ui/items/type_armor.svg");
        _itemTypeIcons[ItemTypeCategory.MainHand] = ResourceLoader.Load<Texture2D>("res://assets/ui/items/type_main_hand.svg");
        _itemTypeIcons[ItemTypeCategory.OffHand] = ResourceLoader.Load<Texture2D>("res://assets/ui/items/type_off_hand.svg");
    }

    private static ItemTypeCategory GetItemTypeCategory(ItemData item)
    {
        return item switch
        {
            ArmorItemData => ItemTypeCategory.Armor,
            MainHandItemData => ItemTypeCategory.MainHand,
            OffHandItemData => ItemTypeCategory.OffHand,
            _ => ItemTypeCategory.Other
        };
    }

    private static string GetItemTypeLabel(ItemTypeCategory itemType)
    {
        return itemType switch
        {
            ItemTypeCategory.Armor => "Armor",
            ItemTypeCategory.MainHand => "Main Hand",
            ItemTypeCategory.OffHand => "Off Hand",
            _ => "Other"
        };
    }

    private Texture2D GetItemTypeIcon(ItemTypeCategory itemType)
    {
        return _itemTypeIcons.TryGetValue(itemType, out var icon) ? icon : null;
    }

    private void ShowEnemy(MobData enemy)
    {
        SetDetailsVisible(true);
        _icon.Texture = enemy.GetUiIconTexture();
        _titleLabel.Text = enemy.DisplayName;
        _descriptionLabel.Text = enemy.Description;
        ClearStats();

        if (enemy is EnemyMobData enemyMob)
        {
            AddStat("Family", GetFamilyLabel(enemyMob));
            AddStat("Health", enemyMob.MaxHealth.ToString());
            AddStat("Fame value", enemyMob.FameValue.ToString());
            AddStat("Scene", enemy.Scene == null ? "Not assigned" : enemy.Scene.ResourcePath);
        }
    }

    private string GetFamilyLabel(EnemyMobData enemy)
    {
        var family = _enemyFamilies.FirstOrDefault(familyData => familyData.Mobs.Any(entry => entry?.Mob == enemy));
        return family?.DisplayName ?? enemy.Family.ToString();
    }

    private void ShowItem(ItemData item)
    {
        SetDetailsVisible(true);
        _icon.Texture = item.UiIcon;
        _titleLabel.Text = item.DisplayName;
        _descriptionLabel.Text = item.Description;
        ClearStats();
        AddStat("Cost", item.Cost.ToString());
        AddStat("Condition", $"{Mathf.RoundToInt(item.Condition * 100f)}%");

        if (item is MainHandItemData mainHandItem)
            AddStat("Type", mainHandItem.IsTwoHanded ? "Two-handed" : "Main hand");
        else if (item is OffHandItemData)
            AddStat("Type", "Off hand");
        else if (item is ArmorItemData)
            AddStat("Type", "Armor");
    }

    private void ClearDetails()
    {
        _icon.Texture = null;
        _titleLabel.Text = string.Empty;
        _descriptionLabel.Text = string.Empty;
        ClearStats();
        SetDetailsVisible(false);
    }

    private void SetDetailsVisible(bool visible)
    {
        _emptyDetailsLabel.Visible = !visible;
        _icon.Visible = visible;
        _titleLabel.Visible = visible;
        _descriptionLabel.Visible = visible;
        _stats.Visible = visible;
    }

    private void ClearStats()
    {
        foreach (var child in _stats.GetChildren())
            child.QueueFree();
    }

    private void AddStat(string label, string value)
    {
        var row = StatRowScene?.Instantiate<CodexStatRow>();
        if (row == null)
        {
            GD.PushError("Codex stat row scene is missing or has the wrong root script.");
            return;
        }

        row.Configure(label, value);
        _stats.AddChild(row);
    }

    private static IEnumerable<string> GetTresPaths(string directoryPath)
    {
        var directory = DirAccess.Open(directoryPath);
        if (directory == null)
            yield break;

        directory.ListDirBegin();
        while (true)
        {
            var entry = directory.GetNext();
            if (string.IsNullOrEmpty(entry))
                break;

            if (entry.StartsWith('.'))
                continue;

            var path = $"{directoryPath}/{entry}";
            if (directory.CurrentIsDir())
            {
                foreach (var childPath in GetTresPaths(path))
                    yield return childPath;
            }
            else if (entry.EndsWith(".tres", System.StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }
        directory.ListDirEnd();
    }
}
