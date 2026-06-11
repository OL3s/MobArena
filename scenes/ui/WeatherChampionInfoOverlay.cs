using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class WeatherChampionInfoOverlay : Control
{
    private const string CloudyIconPath = "res://assets/ui/icons/clear.svg";
    private const string SunIconPath = "res://assets/ui/icons/sun.svg";
    private const string RainIconPath = "res://assets/ui/icons/rain.svg";
    private static readonly Color DebuffColor = new(1f, 0.36f, 0.24f);
    private static readonly Color CostDebuffColor = new(1f, 0.62f, 0.18f);

    private TextureRect _weatherIcon;
    private Label _weatherNameLabel;
    private Label _weatherNoChangesLabel;
    private HBoxContainer _recoveryRow;
    private HBoxContainer _trainingRow;
    private HBoxContainer _costRow;
    private TextureRect _championIcon;
    private Label _championDueLabel;
    private ProgressBar _championProgressBar;
    private Button _closeButton;
    private WeatherState _weatherState;
    private TownPhaseState _phaseState;

    public override void _Ready()
    {
        _weatherIcon = GetNode<TextureRect>("CenterContainer/Panel/MarginContainer/Layout/WeatherSection/WeatherMargin/WeatherLayout/WeatherHeader/WeatherIcon");
        _weatherNameLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/WeatherSection/WeatherMargin/WeatherLayout/WeatherHeader/WeatherNameLabel");
        _weatherNoChangesLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/WeatherSection/WeatherMargin/WeatherLayout/WeatherEffects/NoChangesLabel");
        _recoveryRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/WeatherSection/WeatherMargin/WeatherLayout/WeatherEffects/RecoveryRow");
        _trainingRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/WeatherSection/WeatherMargin/WeatherLayout/WeatherEffects/TrainingRow");
        _costRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/WeatherSection/WeatherMargin/WeatherLayout/WeatherEffects/CostRow");
        _championIcon = GetNode<TextureRect>("CenterContainer/Panel/MarginContainer/Layout/ChampionSection/ChampionMargin/ChampionLayout/ChampionHeader/ChampionIcon");
        _championDueLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/ChampionSection/ChampionMargin/ChampionLayout/ChampionHeader/ChampionDueLabel");
        _championProgressBar = GetNode<ProgressBar>("CenterContainer/Panel/MarginContainer/Layout/ChampionSection/ChampionMargin/ChampionLayout/ChampionProgressBar");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton");
        _closeButton.Pressed += QueueFree;

        var saveNode = SaveNode.Get();
        _weatherState = saveNode?.WeatherState ?? new WeatherState();
        _phaseState = saveNode?.TownPhaseState ?? new TownPhaseState();
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_closeButton != null)
            _closeButton.Pressed -= QueueFree;
    }

    private void RefreshUi()
    {
        RefreshWeatherSection();
        RefreshChampionSection();
    }

    private void RefreshWeatherSection()
    {
        var weather = _weatherState?.CurrentWeather ?? WeatherState.WeatherVisual.Cloudy;
        _weatherIcon.Texture = ResourceLoader.Load<Texture2D>(GetWeatherIconPath(weather));
        _weatherNameLabel.Text = weather.ToString();

        var effects = _weatherState?.GetCurrentEffectConfig() ?? WeatherEffectConfig.Create(1f, 1f, 1f);
        var hasChanges = !Mathf.IsEqualApprox(effects.RecoveryMultiplier, 1f)
            || !Mathf.IsEqualApprox(effects.TrainingMultiplier, 1f)
            || !Mathf.IsEqualApprox(effects.CostMultiplier, 1f);

        _weatherNoChangesLabel.Visible = !hasChanges;
        ConfigureEffectRow(_recoveryRow, "Recovery", effects.RecoveryMultiplier, false);
        ConfigureEffectRow(_trainingRow, "Training", effects.TrainingMultiplier, false);
        ConfigureEffectRow(_costRow, "Town service cost", effects.CostMultiplier, true);
    }

    private void ConfigureEffectRow(HBoxContainer row, string label, float multiplier, bool higherIsBad)
    {
        var changed = !Mathf.IsEqualApprox(multiplier, 1f);
        row.Visible = changed;
        if (!changed)
            return;

        row.GetNode<Label>("Label").Text = label;
        var valueLabel = row.GetNode<Label>("ValueLabel");
        var percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        valueLabel.Text = percent > 0 ? $"+{percent}%" : $"{percent}%";
        valueLabel.Modulate = higherIsBad && percent > 0 ? CostDebuffColor : DebuffColor;
    }

    private void RefreshChampionSection()
    {
        _championIcon.Texture ??= ResourceLoader.Load<Texture2D>("res://assets/ui/icons/champion.svg");
        _championDueLabel.Text = _phaseState.GetChampionLabel();
        _championProgressBar.MaxValue = 7;
        _championProgressBar.Value = _phaseState.IsChampionDay
            ? 7
            : Mathf.Clamp(7 - _phaseState.DaysUntilChampion, 0, 7);
    }

    private static string GetWeatherIconPath(WeatherState.WeatherVisual weather)
    {
        return weather switch
        {
            WeatherState.WeatherVisual.Sun => SunIconPath,
            WeatherState.WeatherVisual.Rain => RainIconPath,
            _ => CloudyIconPath
        };
    }
}
