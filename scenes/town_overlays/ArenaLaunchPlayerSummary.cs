using Godot;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaLaunchPlayerSummary : PanelContainer
{
    [Export]
    public PackedScene EquipmentSlotScene { get; set; }

    private TextureRect _portrait;
    private Label _nameLabel;
    private HBoxContainer _equipmentSlots;
    private TextureRect _controllerIcon;
    private GladiatorData _gladiator;
    private Texture2D _controllerTexture;
    private string _controllerTooltip = "Unassigned";
    private int _playerIndex;

    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("MarginContainer/Row/Portrait");
        _nameLabel = GetNode<Label>("MarginContainer/Row/NameLabel");
        _equipmentSlots = GetNode<HBoxContainer>("MarginContainer/Row/EquipmentSlots");
        _controllerIcon = GetNode<TextureRect>("MarginContainer/Row/ControllerIcon");
        RefreshUi();
    }

    public void Configure(GladiatorData gladiator, int playerIndex, Texture2D controllerTexture, string controllerTooltip)
    {
        _gladiator = gladiator;
        _playerIndex = playerIndex;
        _controllerTexture = controllerTexture;
        _controllerTooltip = controllerTooltip;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _portrait.Texture = _gladiator?.GetUiIconTexture();
        _portrait.TooltipText = _gladiator?.GladiatorName ?? string.Empty;
        _nameLabel.Text = $"P{_playerIndex + 1} {_gladiator?.GladiatorName ?? "Gladiator"}";
        _controllerIcon.Texture = _controllerTexture;
        _controllerIcon.TooltipText = _controllerTooltip;

        foreach (var child in _equipmentSlots.GetChildren())
            child.QueueFree();

        var equipment = _gladiator?.Equipment;
        AddItemSlot(equipment?.MainHand, "Main hand");
        AddItemSlot(equipment?.Armor, "Armor");
        AddItemSlot(equipment?.OffHand, "Off hand");
    }

    private void AddItemSlot(ItemData item, string slotName)
    {
        var slot = EquipmentSlotScene?.Instantiate<EquipmentIconSlot>();
        if (slot == null)
        {
            GD.PushError("Equipment icon slot scene is missing or has the wrong root script.");
            return;
        }

        slot.Configure(item, slotName);
        _equipmentSlots.AddChild(slot);
    }
}
