using Godot;
using System.Collections.Generic;
using System.Linq;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class EquipmentVisualTestOverlay : Control
{
    private const string ItemResourceDirectory = "res://resources/items";
    private const string PreviewBodyPath = "res://assets/gladiators/gladiator_01_body_forward.svg";
    private const string PreviewHandPath = "res://assets/gladiators/gladiator_01_hand.svg";
    private const float PreviewBodyHeight = 128f;
    private const float PreviewHandHeight = 24f;

    private VBoxContainer _content;
    private Texture2D _previewBodyTexture;
    private Texture2D _previewHandTexture;

    public override void _Ready()
    {
        _content = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Layout/ScrollContainer/Content");
        _previewBodyTexture = ResourceLoader.Load<Texture2D>(PreviewBodyPath);
        _previewHandTexture = ResourceLoader.Load<Texture2D>(PreviewHandPath);
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Header/CloseButton").Pressed += QueueFree;
        BuildRows();
    }

    private void BuildRows(bool reloadFromDisk = false)
    {
        foreach (var child in _content.GetChildren())
            child.QueueFree();

        var items = GetTresPaths(ItemResourceDirectory)
            .Select(path => LoadItem(path, reloadFromDisk))
            .Where(item => item != null)
            .OrderBy(GetSortOrder)
            .ThenBy(item => item.DisplayName)
            .ToList();

        AddSection("Main Hand", items.OfType<MainHandItemData>());
        AddSection("Off Hand", items.OfType<OffHandItemData>());
        AddSection("Armor", items.OfType<ArmorItemData>());
    }

    private void AddSection<T>(string title, IEnumerable<T> items) where T : ItemData
    {
        var titleLabel = new Label
        {
            Text = title,
            ThemeTypeVariation = "HeaderSmall",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        _content.AddChild(titleLabel);

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 14);
        _content.AddChild(grid);

        foreach (var item in items)
            grid.AddChild(CreateItemCard(item));
    }

    private Control CreateItemCard(ItemData item)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(330, item is ArmorItemData ? 210 : 420) };
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        panel.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = item.DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 10);
        layout.AddChild(row);

        row.AddChild(CreateTextureColumn("UI", item.UiIcon, 64f));

        if (item is ArmorItemData armor)
        {
            row.AddChild(CreateTextureColumn("Front", armor.ArmorForwardTexture, 112f));
            row.AddChild(CreateTextureColumn("Back", armor.ArmorBackTexture, 112f));
            layout.AddChild(CreateArmorEditor(armor));
        }
        else
        {
            row.AddChild(CreateTextureColumn("Held", item.GetHeldTexture(), Mathf.Min(item.GetHeldDisplayHeight(80f), 120f)));
            layout.AddChild(CreateHeldEditor(item));
        }

        return panel;
    }

    private static ItemData LoadItem(string path, bool reloadFromDisk)
    {
        return reloadFromDisk
            ? ResourceLoader.Load<ItemData>(path, string.Empty, ResourceLoader.CacheMode.Replace)
            : ResourceLoader.Load<ItemData>(path);
    }

    private Control CreateHeldEditor(ItemData item)
    {
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 6);

        var preview = new Control { CustomMinimumSize = new Vector2(300, 170) };
        layout.AddChild(preview);

        var body = new Sprite2D { Texture = _previewBodyTexture, Position = new Vector2(150, 96) };
        FitSpriteHeight(body, _previewBodyTexture, PreviewBodyHeight);
        preview.AddChild(body);

        var hand = new Sprite2D { Texture = _previewHandTexture, Position = GetPreviewHandPosition(item) };
        FitSpriteHeight(hand, _previewHandTexture, PreviewHandHeight);
        preview.AddChild(hand);

        var heldItem = new Sprite2D
        {
            Texture = item.GetHeldTexture(),
            Position = GetPreviewItemPosition(item),
            Centered = false
        };
        hand.AddChild(heldItem);

        var sizeLabel = new Label();
        var sizeSlider = CreateSlider(12f, 220f, item.GetHeldDisplayHeight(48f));
        layout.AddChild(CreateSliderRow("Size", sizeLabel, sizeSlider));

        var angleLabel = new Label();
        var angleSlider = CreateSlider(-180f, 180f, item.GetHeldRotationDegrees());
        layout.AddChild(CreateSliderRow("Angle", angleLabel, angleSlider));

        var offset = item.GetHeldTextureOffset();
        var offsetXLabel = new Label();
        var offsetXSlider = CreateSlider(-160f, 160f, offset.X);
        layout.AddChild(CreateSliderRow("Offset X", offsetXLabel, offsetXSlider));

        var offsetYLabel = new Label();
        var offsetYSlider = CreateSlider(-160f, 160f, offset.Y);
        layout.AddChild(CreateSliderRow("Offset Y", offsetYLabel, offsetYSlider));

        void RefreshPreview()
        {
            sizeLabel.Text = Mathf.RoundToInt(sizeSlider.Value).ToString();
            angleLabel.Text = $"{Mathf.RoundToInt(angleSlider.Value)} deg";
            offsetXLabel.Text = Mathf.RoundToInt(offsetXSlider.Value).ToString();
            offsetYLabel.Text = Mathf.RoundToInt(offsetYSlider.Value).ToString();
            FitSpriteHeight(heldItem, item.GetHeldTexture(), (float)sizeSlider.Value);
            heldItem.RotationDegrees = (float)angleSlider.Value;
            heldItem.Offset = new Vector2((float)offsetXSlider.Value, (float)offsetYSlider.Value);
        }

        sizeSlider.ValueChanged += _ => RefreshPreview();
        angleSlider.ValueChanged += _ => RefreshPreview();
        offsetXSlider.ValueChanged += _ => RefreshPreview();
        offsetYSlider.ValueChanged += _ => RefreshPreview();
        RefreshPreview();

        var saveButton = new Button
        {
            Text = "Save tuning to .tres",
            CustomMinimumSize = new Vector2(0, 34)
        };
        saveButton.Pressed += () => SaveHeldTuning(
            item,
            (float)sizeSlider.Value,
            (float)angleSlider.Value,
            new Vector2((float)offsetXSlider.Value, (float)offsetYSlider.Value),
            saveButton);
        layout.AddChild(saveButton);

        var resetButton = new Button
        {
            Text = "Reset from .tres",
            CustomMinimumSize = new Vector2(0, 34)
        };
        resetButton.Pressed += () => BuildRows(true);
        layout.AddChild(resetButton);

        return layout;
    }

    private Control CreateArmorEditor(ArmorItemData armor)
    {
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 6);

        var preview = new Control { CustomMinimumSize = new Vector2(300, 170) };
        layout.AddChild(preview);

        var body = new Sprite2D { Texture = _previewBodyTexture, Position = new Vector2(150, 96) };
        FitSpriteHeight(body, _previewBodyTexture, PreviewBodyHeight);
        preview.AddChild(body);

        var armorSprite = new Sprite2D
        {
            Texture = armor.ArmorForwardTexture,
            Position = new Vector2(150, 96),
            Centered = false
        };
        preview.AddChild(armorSprite);

        var sizeLabel = new Label();
        var sizeSlider = CreateSlider(12f, 220f, armor.GetArmorDisplayHeight(96f));
        layout.AddChild(CreateSliderRow("Size", sizeLabel, sizeSlider));

        var offset = armor.GetArmorTextureOffset();
        var offsetXLabel = new Label();
        var offsetXSlider = CreateSlider(-160f, 160f, offset.X);
        layout.AddChild(CreateSliderRow("Offset X", offsetXLabel, offsetXSlider));

        var offsetYLabel = new Label();
        var offsetYSlider = CreateSlider(-160f, 160f, offset.Y);
        layout.AddChild(CreateSliderRow("Offset Y", offsetYLabel, offsetYSlider));

        void RefreshPreview()
        {
            sizeLabel.Text = Mathf.RoundToInt(sizeSlider.Value).ToString();
            offsetXLabel.Text = Mathf.RoundToInt(offsetXSlider.Value).ToString();
            offsetYLabel.Text = Mathf.RoundToInt(offsetYSlider.Value).ToString();
            FitSpriteHeight(armorSprite, armor.ArmorForwardTexture, (float)sizeSlider.Value);
            armorSprite.Offset = new Vector2((float)offsetXSlider.Value, (float)offsetYSlider.Value);
        }

        sizeSlider.ValueChanged += _ => RefreshPreview();
        offsetXSlider.ValueChanged += _ => RefreshPreview();
        offsetYSlider.ValueChanged += _ => RefreshPreview();
        RefreshPreview();

        var saveButton = new Button
        {
            Text = "Save tuning to .tres",
            CustomMinimumSize = new Vector2(0, 34)
        };
        saveButton.Pressed += () => SaveArmorTuning(
            armor,
            (float)sizeSlider.Value,
            new Vector2((float)offsetXSlider.Value, (float)offsetYSlider.Value),
            saveButton);
        layout.AddChild(saveButton);

        var resetButton = new Button
        {
            Text = "Reset from .tres",
            CustomMinimumSize = new Vector2(0, 34)
        };
        resetButton.Pressed += () => BuildRows(true);
        layout.AddChild(resetButton);

        return layout;
    }

    private static Vector2 GetPreviewHandPosition(ItemData item)
    {
        return item is OffHandItemData ? new Vector2(116, 88) : new Vector2(184, 88);
    }

    private static Vector2 GetPreviewItemPosition(ItemData item)
    {
        return item is OffHandItemData ? new Vector2(-12, -2) : new Vector2(12, -2);
    }

    private static HSlider CreateSlider(float min, float max, float value)
    {
        return new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
    }

    private static Control CreateSliderRow(string title, Label valueLabel, HSlider slider)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(new Label { Text = title, CustomMinimumSize = new Vector2(48, 0) });
        row.AddChild(slider);
        valueLabel.CustomMinimumSize = new Vector2(58, 0);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(valueLabel);
        return row;
    }

    private static void SaveHeldTuning(ItemData item, float displayHeight, float rotationDegrees, Vector2 textureOffset, Button saveButton)
    {
        item.SetHeldVisualTuning(displayHeight, rotationDegrees, textureOffset);
        var error = ResourceSaver.Save(item, item.ResourcePath);
        saveButton.Text = error == Error.Ok ? "Saved" : $"Save failed: {error}";
    }

    private static void SaveArmorTuning(ArmorItemData armor, float displayHeight, Vector2 textureOffset, Button saveButton)
    {
        armor.SetArmorVisualTuning(displayHeight, textureOffset);
        var error = ResourceSaver.Save(armor, armor.ResourcePath);
        saveButton.Text = error == Error.Ok ? "Saved" : $"Save failed: {error}";
    }

    private static void FitSpriteHeight(Sprite2D sprite, Texture2D texture, float displayHeight)
    {
        sprite.Texture = texture;
        if (texture != null && texture.GetHeight() > 0)
            sprite.Scale = Vector2.One * (displayHeight / texture.GetHeight());
    }

    private static Control CreateTextureColumn(string label, Texture2D texture, float displayHeight)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(82, 150) };
        column.AddThemeConstantOverride("separation", 4);
        column.AddChild(new Label
        {
            Text = label,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var frame = new CenterContainer { CustomMinimumSize = new Vector2(82, 122) };
        var rect = new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = GetDisplaySize(texture, displayHeight),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        frame.AddChild(rect);
        column.AddChild(frame);
        return column;
    }

    private static Vector2 GetDisplaySize(Texture2D texture, float displayHeight)
    {
        if (texture == null || texture.GetHeight() <= 0)
            return new Vector2(64, displayHeight);

        var scale = displayHeight / texture.GetHeight();
        return new Vector2(texture.GetWidth() * scale, displayHeight);
    }

    private static int GetSortOrder(ItemData item)
    {
        return item switch
        {
            MainHandItemData => 0,
            OffHandItemData => 1,
            ArmorItemData => 2,
            _ => 3
        };
    }

    private static IEnumerable<string> GetTresPaths(string directoryPath)
    {
        var directory = DirAccess.Open(directoryPath);
        if (directory == null)
            yield break;

        directory.ListDirBegin();
        while (true)
        {
            var fileName = directory.GetNext();
            if (string.IsNullOrEmpty(fileName))
                break;
            if (fileName.StartsWith('.'))
                continue;

            var path = $"{directoryPath}/{fileName}";
            if (directory.CurrentIsDir())
            {
                foreach (var childPath in GetTresPaths(path))
                    yield return childPath;
                continue;
            }

            if (fileName.EndsWith(".tres"))
                yield return path;
        }
        directory.ListDirEnd();
    }
}
