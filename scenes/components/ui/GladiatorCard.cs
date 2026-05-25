using Godot;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.UI;

public partial class GladiatorCard : PanelContainer
{
    private const string HealthIconPath = "res://assets/ui/gladiator_icons/health.svg";
    private const string StaminaIconPath = "res://assets/ui/gladiator_icons/stamina.svg";
    private const float MaxConditionValue = 10f;
    private static readonly Color NormalStatColor = Colors.White;
    private static readonly Color CappedStatColor = new(0.92f, 0.18f, 0.14f);

    private TextureRect _portrait;
    private Label _nameLabel;
    private TextureRect _mainItemIcon;
    private TextureRect _armorIcon;
    private TextureRect _offItemIcon;
    private TextureRect _healthIcon;
    private VitalProgressBar _healthBar;
    private Label _healthLabel;
    private ColorRect _recoverableHealthRange;
    private ColorRect _recoverableMaxHealthMarker;
    private TextureRect _staminaIcon;
    private Label _maxStaminaLabel;
    private VitalProgressBar _exhaustionBar;
    private TextureRect _skillIcon;
    private Label _skillLabel;
    private AttributeProgressDisplay _strengthDisplay;
    private AttributeProgressDisplay _agilityDisplay;
    private AttributeProgressDisplay _vitalityDisplay;
    private AttributeProgressDisplay _enduranceDisplay;
    private GladiatorData _pendingGladiatorData;

    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("MarginContainer/Layout/Portrait");
        _nameLabel = GetNode<Label>("MarginContainer/Layout/Name");
        _mainItemIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/MainItemIcon");
        _armorIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/ArmorIcon");
        _offItemIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/OffItemIcon");
        _healthIcon = GetNode<TextureRect>("MarginContainer/Layout/Vitals/HealthLine/Icon");
        _healthBar = GetNode<VitalProgressBar>("MarginContainer/Layout/Vitals/HealthLine/Bar");
        _healthLabel = GetNode<Label>("MarginContainer/Layout/Vitals/HealthLine/Bar/Value");
        _recoverableHealthRange = GetNode<ColorRect>("MarginContainer/Layout/Vitals/HealthLine/Bar/RecoverableHealthRange");
		_recoverableMaxHealthMarker = GetNode<ColorRect>("MarginContainer/Layout/Vitals/HealthLine/Bar/RecoverableMaxMarker");
        _exhaustionBar = GetNode<VitalProgressBar>("MarginContainer/Layout/Vitals/ConditionLine/ExhaustionBar");
		_skillIcon = GetNode<TextureRect>("MarginContainer/Layout/Stats/SkillLine/Icon");
        _skillLabel = GetNode<Label>("MarginContainer/Layout/Stats/SkillLine/Skill");
        _staminaIcon = GetNode<TextureRect>("MarginContainer/Layout/Stats/SkillLine/StaminaIcon");
        _maxStaminaLabel = GetNode<Label>("MarginContainer/Layout/Stats/SkillLine/MaxStamina");
        _strengthDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Layout/Stats/PrimaryStats/Strength");
        _agilityDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Layout/Stats/PrimaryStats/Agility");
        _vitalityDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Layout/Stats/BodyStats/Vitality");
        _enduranceDisplay = GetNode<AttributeProgressDisplay>("MarginContainer/Layout/Stats/BodyStats/Endurance");

        _healthIcon.Texture = ResourceLoader.Load<Texture2D>(HealthIconPath);
        _staminaIcon.Texture = ResourceLoader.Load<Texture2D>(StaminaIconPath);

        if (_pendingGladiatorData != null)
            Apply(_pendingGladiatorData);
    }

    public void Configure(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return;

        if (!IsNodeReady())
        {
            _pendingGladiatorData = gladiatorData;
            return;
        }

        Apply(gladiatorData);
    }

    private void Apply(GladiatorData gladiatorData)
    {
        _pendingGladiatorData = null;

        _portrait.Texture = gladiatorData.GetUiIconTexture();
        _nameLabel.Text = gladiatorData.GladiatorName;
        ConfigureEquipmentIcons(gladiatorData.Equipment);
        _healthBar.ShowHealth(gladiatorData);
        _exhaustionBar.ShowExhaustion(gladiatorData.Exhaustion);
        _skillIcon.Texture = ResourceLoader.Load<Texture2D>(GetSkillIconPath(gladiatorData.Equipment.Skill));
        _skillLabel.Text = gladiatorData.Equipment.Skill.ToString();
        ConfigureStaminaValue(gladiatorData);
        ConfigureAttributeDisplays(gladiatorData.Level);
    }

    private void ConfigureEquipmentIcons(GladiatorEquipmentData equipment)
    {
        SetEquipmentIcon(_mainItemIcon, equipment?.MainHand);
        SetEquipmentIcon(_armorIcon, equipment?.Armor);
        SetEquipmentIcon(_offItemIcon, equipment?.OffHand);
    }

    private void ConfigureStaminaValue(GladiatorData gladiatorData)
    {
        var recoverableMaxStamina = gladiatorData.RecoverableMaxStamina;
        var isCapped = recoverableMaxStamina < gladiatorData.MaxStamina;
        var color = isCapped ? CappedStatColor : NormalStatColor;

        _maxStaminaLabel.Text = FormatStaminaValue(recoverableMaxStamina, gladiatorData.MaxStamina);
        _maxStaminaLabel.AddThemeColorOverride("font_color", color);
        _staminaIcon.Modulate = color;
    }

    private static string FormatStaminaValue(int value, int maxValue)
    {
        return value == maxValue ? maxValue.ToString() : $"{value}/{maxValue}";
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

    private static void SetEquipmentIcon(TextureRect icon, ItemData item)
    {
        if (icon == null)
            return;

        icon.Texture = item?.UiIcon;
        icon.TooltipText = item?.DisplayName ?? string.Empty;
    }
}
