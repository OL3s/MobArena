using Godot;
using System.Collections.Generic;
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

    private const string EnemyResourceDirectory = "res://resources/mobs";
    private const string ItemResourceDirectory = "res://resources/items";

    private Button _enemiesButton;
    private Button _itemsButton;
    private VBoxContainer _entryList;
    private Label _emptyListLabel;
    private Label _emptyDetailsLabel;
    private TextureRect _icon;
    private Label _titleLabel;
    private Label _descriptionLabel;
    private GridContainer _stats;
    private CodexCategory _category = CodexCategory.Enemies;
    private readonly List<MobData> _enemies = new();
    private readonly List<ItemData> _items = new();

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
            foreach (var enemy in _enemies)
                _entryList.AddChild(CreateEntryButton(enemy.DisplayName, enemy.Icon, () => ShowEnemy(enemy)));
            return;
        }

        _emptyListLabel.Visible = _items.Count <= 0;
        foreach (var item in _items)
            _entryList.AddChild(CreateEntryButton(item.DisplayName, item.Icon, () => ShowItem(item)));
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

    private void ShowEnemy(MobData enemy)
    {
        SetDetailsVisible(true);
        _icon.Texture = enemy.Icon;
        _titleLabel.Text = enemy.DisplayName;
        _descriptionLabel.Text = enemy.Description;
        ClearStats();

        if (enemy is EnemyMobData enemyMob)
        {
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
