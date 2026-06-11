using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scenes.Components.UI;

public partial class ItemStoreDamagePill : PanelContainer
{
    private TextureRect _icon;
    private Label _valueLabel;
    private CombatDamageType _pendingType = CombatDamageType.Slash;
    private int _pendingValue;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("MarginContainer/Row/Icon");
        _valueLabel = GetNode<Label>("MarginContainer/Row/ValueLabel");
        RefreshUi();
    }

    public void Configure(CombatDamageType type, int value)
    {
        _pendingType = type;
        _pendingValue = value;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _icon.Texture = CombatIconRegistry.LoadInstantIcon(_pendingType);
        _valueLabel.Text = _pendingValue.ToString();
        TooltipText = $"{_pendingType} {_pendingValue}";
    }
}
