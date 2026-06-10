using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scenes.Components.UI;

public partial class ItemStoreDamagePill : PanelContainer
{
    private const string SlashIconPath = "res://assets/ui/items/type_slash.svg";
    private const string PierceIconPath = "res://assets/ui/items/type_pierce.svg";
    private const string CrushIconPath = "res://assets/ui/items/type_crush.svg";
    private const string HeatIconPath = "res://assets/ui/items/type_heat.svg";
    private const string ColdIconPath = "res://assets/ui/items/type_cold.svg";
    private const string AcidIconPath = "res://assets/ui/items/type_acid.svg";
    private const string UnknownIconPath = "res://assets/ui/icons/question_mark.svg";

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

        _icon.Texture = ResourceLoader.Load<Texture2D>(GetIconPath(_pendingType));
        _valueLabel.Text = _pendingValue.ToString();
        TooltipText = $"{_pendingType} {_pendingValue}";
    }

    private static string GetIconPath(CombatDamageType type)
    {
        return type switch
        {
            CombatDamageType.Slash => SlashIconPath,
            CombatDamageType.Pierce => PierceIconPath,
            CombatDamageType.Crush => CrushIconPath,
            CombatDamageType.Heat => HeatIconPath,
            CombatDamageType.Cold => ColdIconPath,
            CombatDamageType.Acid => AcidIconPath,
            _ => UnknownIconPath
        };
    }
}
