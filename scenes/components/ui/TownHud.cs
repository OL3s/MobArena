using Godot;
using MobArena.Scenes.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class TownHud : CanvasLayer
{
    private const string CompanyLogoEditorScenePath = "res://scenes/ui/CompanyLogoEditorOverlay.tscn";
    private static readonly PackedScene CompanyLogoEditorScene = ResourceLoader.Load<PackedScene>(CompanyLogoEditorScenePath);
    private static readonly Texture2D SpeedX0Icon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/pause.svg");
    private static readonly Texture2D SpeedSlowedIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/speed_slowed.svg");
    private static readonly Texture2D SpeedX1Icon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/speed_x1.svg");
    private static readonly Texture2D SpeedX10Icon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/speed_x10.svg");

    [Signal]
    public delegate void BackPressedEventHandler();

    private SaveNode _saveNode;
    private TownTimeState _timeState;
    private CompanyLogo _companyLogo;
    private Label _companyNameLabel;
    private Button _speedToggleButton;
    private Label _dayLabel;
    private TimelineLine _dayProgress;
    private Label _dayPhaseLabel;
    private TimelineLine _bossProgress;
    private Label _bossProgressValue;
    private Timer _timeTickTimer;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _timeState = _saveNode?.TownTimeState ?? new TownTimeState();

        _companyLogo = GetNode<CompanyLogo>("TopPanel/Row/CompanyStatus/Shield");
        _companyNameLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/CompanyName");
        _speedToggleButton = GetNode<Button>("BottomPanel/TimeRow/PauseButton");
        _dayLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarRow/DayLabel");
        _dayProgress = GetNode<TimelineLine>("BottomPanel/TimeRow/CalendarPanel/CalendarRow/DayProgress");
        _dayPhaseLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarRow/DayPhaseLabel");
        _bossProgress = GetNode<TimelineLine>("BottomPanel/TimeRow/TimelinePanel/TimelineRow/BossProgress");
        _bossProgressValue = GetNode<Label>("BottomPanel/TimeRow/TimelinePanel/TimelineRow/BossProgressValue");

        _companyLogo.SetLogoData(_saveNode?.CompanyLogoData ?? CompanyLogoData.CreateDefault());
        _companyLogo.Pressed += OnCompanyLogoPressed;
        GetNode<Button>("TopPanel/Row/SettingsButton").Pressed += OnSettingsPressed;
        GetNode<Button>("TopPanel/Row/BackButton").Pressed += OnBackPressed;
        GetNode<Button>("BottomPanel/TimeRow/SpeedDownButton").Pressed += OnSpeedDownPressed;
        _speedToggleButton.Pressed += OnPausePressed;
        GetNode<Button>("BottomPanel/TimeRow/SpeedUpButton").Pressed += OnSpeedUpPressed;

        _timeTickTimer = new Timer { WaitTime = 1.0, Autostart = true };
        AddChild(_timeTickTimer);
        _timeTickTimer.Timeout += OnTimeTickTimerTimeout;
        _timeState.TimeChanged += RefreshTimeUi;

        RefreshCompanyUi();
        ConfigureTimelineLines();
        RefreshTimeUi();
    }

    public override void _ExitTree()
    {
        if (_timeState != null)
            _timeState.TimeChanged -= RefreshTimeUi;
    }

    private void OnBackPressed()
    {
        EmitSignal(SignalName.BackPressed);
    }

    private static void OnSettingsPressed()
    {
        GlobalOverlay.Get()?.ShowBlurredPopup(
            "Settings",
            "Settings are not implemented yet. This button is wired so it gives clear feedback instead of doing nothing.");
    }

    private void OnSpeedDownPressed()
    {
        _timeState.DecreaseSpeed();
        RefreshSpeedToggleButton();
    }

    private void OnPausePressed()
    {
        _timeState.TogglePaused();
        RefreshSpeedToggleButton();
    }

    private void OnSpeedUpPressed()
    {
        _timeState.IncreaseSpeed();
        RefreshSpeedToggleButton();
    }

    private void OnTimeTickTimerTimeout()
    {
        _timeState.TickOneSecond();
    }

    private void OnCompanyLogoPressed()
    {
        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null || CompanyLogoEditorScene == null || _saveNode == null)
            return;

        var editor = CompanyLogoEditorScene.Instantiate<CompanyLogoEditorOverlay>();
        editor.Configure(_saveNode.CreateEditableCompanyData(), _saveNode.HasCompany, OnCompanyApplied);
        globalOverlay.AddOverlay(editor);
    }

    private void OnCompanyApplied(CompanyLogoData logoData)
    {
        _saveNode.ApplyCompanyData(logoData);
        _companyLogo.SetLogoData(_saveNode.CompanyLogoData);
        RefreshCompanyUi();
    }

    private void RefreshCompanyUi()
    {
        _companyNameLabel.Text = (_saveNode?.CompanyLogoData ?? CompanyLogoData.CreateDefault()).CompanyName;
    }

    private void RefreshTimeUi()
    {
        _dayLabel.Text = _timeState.GetDayLabel();
        _dayProgress.SetValue(_timeState.GetDayProgressValue(), _timeState.GetDayProgressMax());
        _dayPhaseLabel.Text = _timeState.GetDayPhaseLabel();
        _bossProgress.SetValue(_timeState.GetBossProgressValue(), _timeState.GetBossProgressMax());
        _bossProgressValue.Text = _timeState.GetBossDeadlineLabel();
        RefreshSpeedToggleButton();
    }

    private void ConfigureTimelineLines()
    {
        _dayProgress.ClearSegments();
        _dayProgress.AddSegment(0, _timeState.GetTownOpenMinute(), new Color(0.72f, 0.28f, 0.22f, 0.22f));
        _dayProgress.AddSegment(_timeState.GetTownOpenMinute(), _timeState.GetTownCloseMinute(), new Color(0.28f, 0.68f, 0.34f, 0.32f));
        _dayProgress.AddSegment(_timeState.GetTownCloseMinute(), _timeState.GetDayProgressMax(), new Color(0.85f, 0.64f, 0.24f, 0.24f));

        _bossProgress.ClearSegments();
        _bossProgress.AddSegment(_timeState.GetBossFinalDayStart(), _timeState.GetBossProgressMax(), new Color(0.75f, 0.16f, 0.12f, 0.32f));
    }

    private void RefreshSpeedToggleButton()
    {
        _speedToggleButton.Text = _timeState.CurrentSpeed switch
        {
            TownTimeState.TimeSpeed.X100 => "Fast",
            TownTimeState.TimeSpeed.X10 => "Normal",
            TownTimeState.TimeSpeed.X0 => "Paused",
            _ => "Slowed"
        };

        _speedToggleButton.Icon = _timeState.CurrentSpeed switch
        {
            TownTimeState.TimeSpeed.X100 => SpeedX10Icon,
            TownTimeState.TimeSpeed.X10 => SpeedX1Icon,
            TownTimeState.TimeSpeed.X0 => SpeedX0Icon,
            _ => SpeedSlowedIcon
        };
    }
}
