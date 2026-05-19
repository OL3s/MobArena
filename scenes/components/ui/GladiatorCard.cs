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
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private ColorRect _recoverableHealthRange;
    private ColorRect _recoverableMaxHealthMarker;
    private TextureRect _staminaIcon;
    private Label _maxStaminaLabel;
    private ProgressBar _provisionsBar;
    private ProgressBar _exhaustionBar;
    private TextureRect _skillIcon;
    private Label _skillLabel;
    private Label _strengthLabel;
    private Label _agilityLabel;
    private Label _vitalityLabel;
    private Label _enduranceLabel;

    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("MarginContainer/Layout/Portrait");
        _nameLabel = GetNode<Label>("MarginContainer/Layout/Name");
        _mainItemIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/MainItemIcon");
        _armorIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/ArmorIcon");
        _offItemIcon = GetNode<TextureRect>("MarginContainer/Layout/EquipmentRow/OffItemIcon");
        _healthIcon = GetNode<TextureRect>("MarginContainer/Layout/Vitals/HealthLine/Icon");
        _healthBar = GetNode<ProgressBar>("MarginContainer/Layout/Vitals/HealthLine/Bar");
        _healthLabel = GetNode<Label>("MarginContainer/Layout/Vitals/HealthLine/Bar/Value");
        _recoverableHealthRange = GetNode<ColorRect>("MarginContainer/Layout/Vitals/HealthLine/Bar/RecoverableHealthRange");
        _recoverableMaxHealthMarker = GetNode<ColorRect>("MarginContainer/Layout/Vitals/HealthLine/Bar/RecoverableMaxMarker");
        _provisionsBar = GetNode<ProgressBar>("MarginContainer/Layout/Vitals/ConditionLine/ProvisionsBar");
        _exhaustionBar = GetNode<ProgressBar>("MarginContainer/Layout/Vitals/ConditionLine/ExhaustionBar");
        _skillIcon = GetNode<TextureRect>("MarginContainer/Layout/Stats/SkillLine/Icon");
        _skillLabel = GetNode<Label>("MarginContainer/Layout/Stats/SkillLine/Skill");
        _staminaIcon = GetNode<TextureRect>("MarginContainer/Layout/Stats/SkillLine/StaminaIcon");
        _maxStaminaLabel = GetNode<Label>("MarginContainer/Layout/Stats/SkillLine/MaxStamina");
        _strengthLabel = GetNode<Label>("MarginContainer/Layout/Stats/PrimaryStats/Strength");
        _agilityLabel = GetNode<Label>("MarginContainer/Layout/Stats/PrimaryStats/Agility");
        _vitalityLabel = GetNode<Label>("MarginContainer/Layout/Stats/BodyStats/Vitality");
        _enduranceLabel = GetNode<Label>("MarginContainer/Layout/Stats/BodyStats/Endurance");

        _healthIcon.Texture = ResourceLoader.Load<Texture2D>(HealthIconPath);
        _staminaIcon.Texture = ResourceLoader.Load<Texture2D>(StaminaIconPath);
    }

    public void Configure(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return;

        if (!IsNodeReady())
            return;

        _portrait.Texture = gladiatorData.GetPortraitTexture();
        _nameLabel.Text = gladiatorData.GladiatorName;
        ConfigureEquipmentIcons(gladiatorData.Equipment);
        ConfigureBar(_healthBar, _healthLabel, gladiatorData.Health, gladiatorData.MaxHealth);
        ConfigureRecoverableHealthRange(_recoverableHealthRange, gladiatorData.Health, gladiatorData.RecoverableConditionRatio, gladiatorData.MaxHealth);
        ConfigureHealthCapMarker(_recoverableMaxHealthMarker, gladiatorData.RecoverableConditionRatio);
        ConfigureConditionBar(_provisionsBar, gladiatorData.Provisions);
        ConfigureConditionBar(_exhaustionBar, gladiatorData.Exhaustion);
        _skillIcon.Texture = ResourceLoader.Load<Texture2D>(GetSkillIconPath(gladiatorData.Equipment.Skill));
        _skillLabel.Text = gladiatorData.Equipment.Skill.ToString();
        ConfigureStaminaValue(gladiatorData);
        _strengthLabel.Text = $"Str {gladiatorData.Level.Strength}";
        _agilityLabel.Text = $"Agi {gladiatorData.Level.Agility}";
        _vitalityLabel.Text = $"Vit {gladiatorData.Level.Vitality}";
        _enduranceLabel.Text = $"End {gladiatorData.Level.Endurance}";
    }

    private void ConfigureEquipmentIcons(GladiatorEquipmentData equipment)
    {
        SetEquipmentIcon(_mainItemIcon, equipment?.MainHand);
        SetEquipmentIcon(_armorIcon, equipment?.Armor);
        SetEquipmentIcon(_offItemIcon, equipment?.OffHand);
    }

    private static void ConfigureBar(ProgressBar bar, Label label, int value, int maxValue)
    {
        bar.MaxValue = Mathf.Max(1, maxValue);
        bar.Value = Mathf.Clamp(value, 0, maxValue);
        label.Text = $"{value}/{maxValue}";
    }

    private static void ConfigureConditionBar(ProgressBar bar, float value)
    {
        bar.MaxValue = MaxConditionValue;
        bar.Value = Mathf.Clamp(value, 0f, MaxConditionValue);
    }

    private void ConfigureStaminaValue(GladiatorData gladiatorData)
    {
        var recoverableMaxStamina = gladiatorData.RecoverableMaxStamina;
        var isCapped = recoverableMaxStamina < gladiatorData.MaxStamina;
        var color = isCapped ? CappedStatColor : NormalStatColor;

        _maxStaminaLabel.Text = recoverableMaxStamina.ToString();
        _maxStaminaLabel.AddThemeColorOverride("font_color", color);
        _staminaIcon.Modulate = color;
    }

    private static void ConfigureHealthCapMarker(Control marker, float recoverableRatio)
    {
        var ratio = Mathf.Clamp(recoverableRatio, 0f, 1f);
        marker.AnchorLeft = ratio;
        marker.AnchorRight = ratio;
        marker.OffsetLeft = -1f;
        marker.OffsetRight = 1f;
    }

    private static void ConfigureRecoverableHealthRange(Control range, int health, float recoverableRatio, int maxHealth)
    {
        var currentRatio = maxHealth <= 0 ? 0f : Mathf.Clamp(health / (float)maxHealth, 0f, 1f);
        var recoverableHealthRatio = Mathf.Clamp(recoverableRatio, 0f, 1f);

        range.Visible = recoverableHealthRatio > currentRatio;
        range.AnchorLeft = currentRatio;
        range.AnchorRight = recoverableHealthRatio;
        range.OffsetLeft = 0f;
        range.OffsetRight = 0f;
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

        icon.Texture = item?.Icon;
        icon.TooltipText = item?.DisplayName ?? string.Empty;
    }
}
