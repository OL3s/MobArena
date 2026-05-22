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

    private const string EnemyResourceDirectory = "res://resources/mobs";
    private const string ItemResourceDirectory = "res://resources/items";
    private const string ChampionIconPath = "res://assets/ui/icons/champion.svg";
    private static readonly Color ExpandedFamilyHeaderColor = new(0.86f, 1f, 0.86f);
    private static readonly Color CollapsedFamilyHeaderColor = new(0.78f, 0.78f, 0.78f);
    private static readonly Color EntryGroupPanelColor = new(0.08f, 0.09f, 0.1f, 0.55f);
    private static readonly Color EntryGroupPanelBorderColor = new(0.3f, 0.34f, 0.32f, 0.85f);

    private Button _enemiesButton;
    private Button _itemsButton;
    private VBoxContainer _entryList;
    private Label _emptyListLabel;
    private Label _emptyDetailsLabel;
    private TextureRect _icon;
    private Label _titleLabel;
    private Label _descriptionLabel;
    private GridContainer _stats;
    private Texture2D _championIcon;
    private Texture2D _twoHandedIcon;
    private readonly Dictionary<MobFamily, Texture2D> _familyIcons = new();
    private readonly Dictionary<ItemTypeCategory, Texture2D> _itemTypeIcons = new();
    private CodexCategory _category = CodexCategory.Enemies;
    private readonly List<MobData> _enemies = new();
    private readonly List<ItemData> _items = new();
    private readonly Dictionary<MobFamily, bool> _expandedEnemyFamilies = new();
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
        _stats = GetNode<GridContainer>("CenterContainer/PopupPanel/MarginContainer/Content/Body/DetailsPanel/Details/Stats");
        _championIcon = ResourceLoader.Load<Texture2D>(ChampionIconPath);
        _twoHandedIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/items/type_two_handed.svg");
        LoadFamilyIcons();
        LoadItemTypeIcons();

        _enemiesButton.Pressed += () => SelectCategory(CodexCategory.Enemies);
        _itemsButton.Pressed += () => SelectCategory(CodexCategory.Items);
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CloseButton").Pressed += QueueFree;

        LoadResources();
        SelectCategory(CodexCategory.Enemies);
    }

    private void LoadResources()
    {
        _enemies.Clear();
        _items.Clear();

        foreach (var path in GetTresPaths(EnemyResourceDirectory))
        {
            var enemy = ResourceLoader.Load<MobData>(path);
            if (enemy != null)
                _enemies.Add(enemy);
        }

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
            _emptyListLabel.Visible = _enemies.Count <= 0;
            foreach (var enemyGroup in GetSortedEnemyGroups())
                AddEnemyGroup(enemyGroup.Key, enemyGroup);
            return;
        }

        _emptyListLabel.Visible = _items.Count <= 0;
        foreach (var itemGroup in GetSortedItemGroups())
            AddItemGroup(itemGroup.Key, itemGroup);
    }

    private Button CreateEntryButton(string title, Texture2D icon, System.Action pressedAction)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(280, 54),
            Text = title,
            Icon = icon,
            ExpandIcon = true,
            FocusMode = FocusModeEnum.All,
            Alignment = HorizontalAlignment.Left
        };

        button.Pressed += pressedAction;
        return button;
    }

    private IEnumerable<IGrouping<MobFamily, EnemyMobData>> GetSortedEnemyGroups()
    {
        return _enemies
            .OfType<EnemyMobData>()
            .GroupBy(enemy => enemy.Family)
            .OrderBy(group => group.Sum(enemy => enemy.FameValue))
            .ThenBy(group => group.Key.ToString());
    }

    private void AddEnemyGroup(MobFamily family, IEnumerable<EnemyMobData> enemies)
    {
        if (!_expandedEnemyFamilies.ContainsKey(family))
            _expandedEnemyFamilies[family] = false;

        var isExpanded = _expandedEnemyFamilies[family];
        var groupEnemies = enemies
            .OrderBy(enemy => enemy.FameValue)
            .ThenBy(enemy => enemy.DisplayName)
            .ToList();

        var groupPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = EntryGroupPanelColor,
            BorderColor = EntryGroupPanelBorderColor,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 8,
            ContentMarginTop = 8,
            ContentMarginRight = 8,
            ContentMarginBottom = 8
        };
        groupPanel.AddThemeStyleboxOverride("panel", panelStyle);

        var groupContent = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        groupContent.AddThemeConstantOverride("separation", 6);
        groupPanel.AddChild(groupContent);

        var headerButton = new Button
        {
            CustomMinimumSize = new Vector2(280, 46),
            Text = family.ToString(),
            Icon = GetFamilyIcon(family),
            ExpandIcon = true,
            FocusMode = FocusModeEnum.All,
            Alignment = HorizontalAlignment.Left
        };
        headerButton.AddThemeColorOverride("font_color", isExpanded ? ExpandedFamilyHeaderColor : CollapsedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("font_hover_color", ExpandedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("font_focus_color", ExpandedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("icon_normal_color", isExpanded ? ExpandedFamilyHeaderColor : CollapsedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("icon_hover_color", ExpandedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("icon_focus_color", ExpandedFamilyHeaderColor);
        headerButton.Pressed += () =>
        {
            _expandedEnemyFamilies[family] = !_expandedEnemyFamilies[family];
            RefreshEntryList();
        };
        groupContent.AddChild(headerButton);

        if (!isExpanded)
        {
            _entryList.AddChild(groupPanel);
            return;
        }

        foreach (var enemy in groupEnemies)
            groupContent.AddChild(CreateEnemyEntryRow(enemy));

        _entryList.AddChild(groupPanel);
    }

    private Control CreateEnemyEntryRow(EnemyMobData enemy)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(280, 54),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);

        var button = CreateEntryButton(enemy.DisplayName, enemy.GetIconTexture(), () => ShowEnemy(enemy));
        button.CustomMinimumSize = new Vector2(0, 54);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(button);

        if (enemy is ChampionMobData)
        {
            var championIcon = new TextureRect
            {
                CustomMinimumSize = new Vector2(28, 28),
                Texture = _championIcon,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            row.AddChild(championIcon);
        }

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

        var groupPanel = CreateEntryGroupPanel();
        var groupContent = CreateEntryGroupContent();
        groupPanel.AddChild(groupContent);

        var headerButton = CreateGroupHeaderButton(GetItemTypeLabel(itemType), GetItemTypeIcon(itemType), isExpanded);
        headerButton.Pressed += () =>
        {
            _expandedItemTypes[itemType] = !_expandedItemTypes[itemType];
            RefreshEntryList();
        };
        groupContent.AddChild(headerButton);

        if (!isExpanded)
        {
            _entryList.AddChild(groupPanel);
            return;
        }

        foreach (var item in groupItems)
            groupContent.AddChild(CreateItemEntryRow(item));

        _entryList.AddChild(groupPanel);
    }

    private Control CreateItemEntryRow(ItemData item)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(280, 54),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);

        var button = CreateEntryButton(item.DisplayName, item.Icon, () => ShowItem(item));
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(button);

        if (item is MainHandItemData { IsTwoHanded: true })
        {
            var twoHandedIcon = new TextureRect
            {
                CustomMinimumSize = new Vector2(28, 28),
                Texture = _twoHandedIcon,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            row.AddChild(twoHandedIcon);
        }

        return row;
    }

    private PanelContainer CreateEntryGroupPanel()
    {
        var groupPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = EntryGroupPanelColor,
            BorderColor = EntryGroupPanelBorderColor,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 8,
            ContentMarginTop = 8,
            ContentMarginRight = 8,
            ContentMarginBottom = 8
        };
        groupPanel.AddThemeStyleboxOverride("panel", panelStyle);
        return groupPanel;
    }

    private VBoxContainer CreateEntryGroupContent()
    {
        var groupContent = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        groupContent.AddThemeConstantOverride("separation", 6);
        return groupContent;
    }

    private Button CreateGroupHeaderButton(string title, Texture2D icon, bool isExpanded)
    {
        var headerButton = new Button
        {
            CustomMinimumSize = new Vector2(280, 46),
            Text = title,
            Icon = icon,
            ExpandIcon = true,
            FocusMode = FocusModeEnum.All,
            Alignment = HorizontalAlignment.Left
        };
        headerButton.AddThemeColorOverride("font_color", isExpanded ? ExpandedFamilyHeaderColor : CollapsedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("font_hover_color", ExpandedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("font_focus_color", ExpandedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("icon_normal_color", isExpanded ? ExpandedFamilyHeaderColor : CollapsedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("icon_hover_color", ExpandedFamilyHeaderColor);
        headerButton.AddThemeColorOverride("icon_focus_color", ExpandedFamilyHeaderColor);
        return headerButton;
    }

    private void LoadFamilyIcons()
    {
        _familyIcons[MobFamily.Slimes] = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/family_slimes.svg");
        _familyIcons[MobFamily.Goblins] = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/family_goblins.svg");
        _familyIcons[MobFamily.Undead] = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/family_undead.svg");
        _familyIcons[MobFamily.Demons] = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/family_demons.svg");
    }

    private Texture2D GetFamilyIcon(MobFamily family)
    {
        return _familyIcons.TryGetValue(family, out var icon) ? icon : null;
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
        _icon.Texture = enemy.GetIconTexture();
        _titleLabel.Text = enemy.DisplayName;
        _descriptionLabel.Text = enemy.Description;
        ClearStats();

        if (enemy is EnemyMobData enemyMob)
        {
            AddStat("Family", enemyMob.Family.ToString());
            AddStat("Health", enemyMob.MaxHealth.ToString());
            AddStat("Fame value", enemyMob.FameValue.ToString());
            AddStat("Scene", enemy.Scene == null ? "Not assigned" : enemy.Scene.ResourcePath);
        }
    }

    private void ShowItem(ItemData item)
    {
        SetDetailsVisible(true);
        _icon.Texture = item.Icon;
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
        _stats.AddChild(new Label { Text = label });
        _stats.AddChild(new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right });
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
