using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class GladiatorCard : PanelContainer
{
    private const string HealthIconPath = "res://assets/ui/gladiator_icons/health.svg";
    private const string StaminaIconPath = "res://assets/ui/gladiator_icons/stamina.svg";

    private TextureRect _portrait;
    private Label _nameLabel;
    private TextureRect _healthIcon;
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private TextureRect _staminaIcon;
    private ProgressBar _staminaBar;
    private Label _staminaLabel;
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
        _healthIcon = GetNode<TextureRect>("MarginContainer/Layout/Vitals/HealthLine/Icon");
        _healthBar = GetNode<ProgressBar>("MarginContainer/Layout/Vitals/HealthLine/Bar");
        _healthLabel = GetNode<Label>("MarginContainer/Layout/Vitals/HealthLine/Bar/Value");
        _staminaIcon = GetNode<TextureRect>("MarginContainer/Layout/Vitals/StaminaLine/Icon");
        _staminaBar = GetNode<ProgressBar>("MarginContainer/Layout/Vitals/StaminaLine/Bar");
        _staminaLabel = GetNode<Label>("MarginContainer/Layout/Vitals/StaminaLine/Bar/Value");
        _skillIcon = GetNode<TextureRect>("MarginContainer/Layout/Stats/SkillLine/Icon");
        _skillLabel = GetNode<Label>("MarginContainer/Layout/Stats/SkillLine/Skill");
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
        ConfigureBar(_healthBar, _healthLabel, gladiatorData.Health, gladiatorData.MaxHealth);
        ConfigureBar(_staminaBar, _staminaLabel, gladiatorData.Stamina, gladiatorData.MaxStamina);
        _skillIcon.Texture = ResourceLoader.Load<Texture2D>(GetSkillIconPath(gladiatorData.Equipment.Skill));
        _skillLabel.Text = gladiatorData.Equipment.Skill.ToString();
        _strengthLabel.Text = $"Str {gladiatorData.Level.Strength}";
        _agilityLabel.Text = $"Agi {gladiatorData.Level.Agility}";
        _vitalityLabel.Text = $"Vit {gladiatorData.Level.Vitality}";
        _enduranceLabel.Text = $"End {gladiatorData.Level.Endurance}";
    }

    private static void ConfigureBar(ProgressBar bar, Label label, int value, int maxValue)
    {
        bar.MaxValue = Mathf.Max(1, maxValue);
        bar.Value = Mathf.Clamp(value, 0, maxValue);
        label.Text = $"{value}/{maxValue}";
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
