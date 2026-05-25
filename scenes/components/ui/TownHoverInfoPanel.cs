using Godot;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI;

public partial class TownHoverInfoPanel : PanelContainer
{
    private TextureRect _icon;
    private Label _title;
    private RichTextLabel _description;
    private VBoxContainer _equipmentColumn;
    private TextureRect _mainHandIcon;
    private TextureRect _armorIcon;
    private TextureRect _offHandIcon;
    private VBoxContainer _vitalsColumn;
    private VitalProgressBar _healthBar;
    private Label _healthLabel;
    private VitalProgressBar _exhaustionBar;
    private Label _exhaustionLabel;
    private VBoxContainer _skillColumn;
    private TextureRect _skillIcon;
    private Label _skillLabel;
    private Label _staminaLabel;
    private AttributeProgressDisplay _strengthDisplay;
    private AttributeProgressDisplay _agilityDisplay;
    private AttributeProgressDisplay _vitalityDisplay;
    private AttributeProgressDisplay _enduranceDisplay;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("MarginContainer/Row/IdentityColumn/Icon");
        _title = GetNode<Label>("MarginContainer/Row/IdentityColumn/Title");
        _description = GetNode<RichTextLabel>("MarginContainer/Row/Details/Description");
        _equipmentColumn = GetNode<VBoxContainer>("MarginContainer/Row/EquipmentColumn");
        _mainHandIcon = GetNode<TextureRect>("MarginContainer/Row/EquipmentColumn/MainHandIcon");
        _armorIcon = GetNode<TextureRect>("MarginContainer/Row/EquipmentColumn/ArmorIcon");
        _offHandIcon = GetNode<TextureRect>("MarginContainer/Row/EquipmentColumn/OffHandIcon");
        _vitalsColumn = GetNode<VBoxContainer>("MarginContainer/Row/VitalsColumn");
        _healthBar = GetNode<VitalProgressBar>("MarginContainer/Row/VitalsColumn/HealthRow/Bar");
        _healthLabel = GetNode<Label>("MarginContainer/Row/VitalsColumn/HealthRow/Value");
        _exhaustionBar = GetNode<VitalProgressBar>("MarginContainer/Row/VitalsColumn/ExhaustionRow/Bar");
        _exhaustionLabel = GetNode<Label>("MarginContainer/Row/VitalsColumn/ExhaustionRow/Value");
        _skillColumn = GetNode<VBoxContainer>("MarginContainer/Row/SkillColumn");
        _skillIcon = GetNode<TextureRect>("MarginContainer/Row/SkillColumn/TopRow/SkillIcon");
        _skillLabel = GetNode<Label>("MarginContainer/Row/SkillColumn/TopRow/SkillLabel");
        _staminaLabel = GetNode<Label>("MarginContainer/Row/SkillColumn/TopRow/StaminaLabel");
        _strengthDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Row/SkillColumn/StatsGrid/StrengthLabel");
        _agilityDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Row/SkillColumn/StatsGrid/AgilityLabel");
        _vitalityDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Row/SkillColumn/StatsGrid/VitalityLabel");
        _enduranceDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Row/SkillColumn/StatsGrid/EnduranceLabel");
        Clear();
    }

    public void ShowGladiator(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return;

        Visible = true;
        _icon.Texture = gladiatorData.GetUiIconTexture();
        _title.Text = gladiatorData.GladiatorName;
        _description.Visible = false;
        _equipmentColumn.Visible = true;
        _vitalsColumn.Visible = true;
        _skillColumn.Visible = true;

        SetEquipmentIcon(_mainHandIcon, gladiatorData.Equipment?.MainHand, "Main hand");
        SetEquipmentIcon(_armorIcon, gladiatorData.Equipment?.Armor, "Armor");
        SetEquipmentIcon(_offHandIcon, gladiatorData.Equipment?.OffHand, "Off hand");
        _healthBar.ShowHealth(gladiatorData, "HP");
        _exhaustionBar.ShowExhaustion(gladiatorData.Exhaustion, "Exh");

        _skillIcon.Texture = ResourceLoader.Load<Texture2D>(GetSkillIconPath(gladiatorData.Equipment.Skill));
        _skillLabel.Text = gladiatorData.Equipment.Skill.ToString();
        _staminaLabel.Text = FormatStaminaValue(gladiatorData.RecoverableMaxStamina, gladiatorData.MaxStamina);
        ConfigureAttributeDisplays(gladiatorData.Level);
    }

    private static string FormatStaminaValue(int value, int maxValue)
    {
        return value == maxValue ? maxValue.ToString() : $"{value}/{maxValue}";
    }

    public void ShowBuilding(Texture2D icon, string title, string description)
    {
        Visible = true;
        _icon.Texture = icon;
        _title.Text = string.IsNullOrWhiteSpace(title) ? "Building" : title;
        _description.Text = string.IsNullOrWhiteSpace(description) ? "Town building." : description;
        _description.Visible = true;
        _equipmentColumn.Visible = false;
        _vitalsColumn.Visible = false;
        _skillColumn.Visible = false;
    }

    public void Clear()
    {
        Visible = false;
    }

    private static void SetEquipmentIcon(TextureRect icon, ItemData item, string fallbackTooltip)
    {
        icon.Texture = item?.UiIcon;
        icon.TooltipText = item?.DisplayName ?? fallbackTooltip;
        icon.Modulate = item == null ? new Color(1f, 1f, 1f, 0.28f) : Colors.White;
    }

    private void ConfigureAttributeDisplays(GladiatorLevelData levelData)
    {
        _strengthDisplay.Configure(levelData, GladiatorLevelData.AttributeKind.Strength);
        _agilityDisplay.Configure(levelData, GladiatorLevelData.AttributeKind.Agility);
        _vitalityDisplay.Configure(levelData, GladiatorLevelData.AttributeKind.Vitality);
        _enduranceDisplay.Configure(levelData, GladiatorLevelData.AttributeKind.Endurance);
    }

    private static string GetSkillIconPath(GladiatorEquipmentData.SignatureSkill skill)
    {
        return skill switch
        {
            GladiatorEquipmentData.SignatureSkill.Parry => "res://assets/ui/gladiator_icons/skill_parry.svg",
            GladiatorEquipmentData.SignatureSkill.Bash => "res://assets/ui/gladiator_icons/skill_bash.svg",
            GladiatorEquipmentData.SignatureSkill.Cleave => "res://assets/ui/gladiator_icons/skill_cleave.svg",
            _ => "res://assets/ui/gladiator_icons/skill_dodge.svg"
        };
    }
}
