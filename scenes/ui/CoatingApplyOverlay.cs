using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.UI;

public partial class CoatingApplyOverlay : Control
{
    private GladiatorData _gladiator;
    private ItemCoatingData _coating;
    private Label _titleLabel;
    private Label _descriptionLabel;
    private TextureRect _coatingIcon;
    private HBoxContainer _equipmentRow;
    private Button _cancelButton;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/Header/TitleLabel");
        _descriptionLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/DescriptionLabel");
        _coatingIcon = GetNode<TextureRect>("CenterContainer/PopupPanel/MarginContainer/Content/Header/CoatingIcon");
        _equipmentRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/EquipmentScroll/EquipmentRow");
        _cancelButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CancelButton");
        _cancelButton.Pressed += QueueFree;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_cancelButton != null)
            _cancelButton.Pressed -= QueueFree;
    }

    public void Configure(GladiatorData gladiator, ItemCoatingData coating)
    {
        _gladiator = gladiator;
        _coating = coating;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        foreach (var child in _equipmentRow.GetChildren())
            child.QueueFree();

        _coatingIcon.Texture = _coating?.UiIcon;
        _titleLabel.Text = _coating == null ? "Apply Coating" : $"Apply {_coating.DisplayName}";
        _descriptionLabel.Text = _gladiator == null
            ? "Choose an equipped item."
            : $"Choose which of {_gladiator.GladiatorName}'s equipped items should receive this coating.";

        var equipment = _gladiator?.Equipment;
        AddEquipmentChoice("Main Hand", equipment?.MainHand);
        AddEquipmentChoice("Armor", equipment?.Armor);
        AddEquipmentChoice("Off Hand", equipment?.OffHand);

        if (_equipmentRow.GetChildCount() <= 0)
            _equipmentRow.AddChild(new Label { Text = "This gladiator has no equipped items to coat." });
    }

    private void AddEquipmentChoice(string slotName, EquipmentItemData item)
    {
        if (item == null)
            return;

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(220, 240),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = slotName,
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "HeaderSmall"
        });

        layout.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(72, 72),
            Texture = item.UiIcon,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });

        layout.AddChild(new Label
        {
            Text = item.DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        layout.AddChild(new Label
        {
            Text = GetChangeLabel(item),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var applyButton = new Button
        {
            Text = "Apply",
            CustomMinimumSize = new Vector2(0, 42),
            FocusMode = FocusModeEnum.All
        };
        applyButton.Pressed += () => ApplyTo(item);
        layout.AddChild(applyButton);

        _equipmentRow.AddChild(panel);
    }

    private string GetChangeLabel(EquipmentItemData item)
    {
        var current = item.AppliedCoating?.Coating?.DisplayName ?? "No coating";
        var next = _coating?.DisplayName ?? "No coating";
        return $"Change: {current} -> {next}";
    }

    private void ApplyTo(EquipmentItemData item)
    {
        var saveNode = SaveNode.Get();
        var runData = saveNode?.CompanyRunData;
        if (runData == null || item == null || _coating == null)
            return;

        if (!runData.TryApplyCoatingToItem(item, _coating, saveNode.CompanyCareerData))
            return;

        runData.RemoveItem(_coating);
        QueueFree();
    }
}
