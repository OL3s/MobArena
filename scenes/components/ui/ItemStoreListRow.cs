using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI;

public partial class ItemStoreListRow : HBoxContainer
{
    [Signal]
    public delegate void DetailsPressedEventHandler(ItemData item);

    private TextureRect _itemIcon;
    private TextureRect _typeIcon;
    private Label _nameLabel;
    private Label _costLabel;
    private Button _detailsButton;
    private ItemData _item;
    private bool _selected;

    public override void _Ready()
    {
        _itemIcon = GetNode<TextureRect>("ItemIcon");
        _typeIcon = GetNode<TextureRect>("TypeIcon");
        _nameLabel = GetNode<Label>("NameLabel");
        _costLabel = GetNode<Label>("CostBox/CostLabel");
        _detailsButton = GetNode<Button>("DetailsButton");
        _detailsButton.Pressed += OnDetailsPressed;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_detailsButton != null)
            _detailsButton.Pressed -= OnDetailsPressed;
    }

    public void Configure(ItemData item, bool selected)
    {
        _item = item;
        _selected = selected;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _itemIcon.Texture = _item?.UiIcon;
        _typeIcon.Texture = null;
        _typeIcon.Hide();
        _nameLabel.Text = _item?.DisplayName ?? "Item";
        _costLabel.Text = (_item?.Cost ?? 0).ToString();
        _detailsButton.Disabled = _selected;
        _detailsButton.Text = _selected ? "Shown" : "Details";
    }

    private void OnDetailsPressed()
    {
        if (_item != null)
            EmitSignal(SignalName.DetailsPressed, _item);
    }

}
