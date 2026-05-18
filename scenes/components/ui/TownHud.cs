using Godot;
using MobArena.Scenes.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class TownHud : CanvasLayer
{
	private const string CompanyLogoEditorScenePath = "res://scenes/ui/CompanyLogoEditorOverlay.tscn";
	private const string CompanyOverviewScenePath = "res://scenes/ui/CompanyOverviewOverlay.tscn";
	private const string GladiatorsOverlayScenePath = "res://scenes/ui/GladiatorsOverlay.tscn";
	private const string GladiatorDeathOverlayScenePath = "res://scenes/ui/GladiatorDeathOverlay.tscn";
	private readonly Texture2D _speedX0Icon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/pause.svg");
	private readonly Texture2D _speedSlowedIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/speed_slowed.svg");
	private readonly Texture2D _speedX1Icon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/speed_x1.svg");
	private readonly Texture2D _speedX10Icon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/speed_x10.svg");

	[Signal]
	public delegate void BackPressedEventHandler();

	private SaveNode _saveNode;
	private TownTimeState _timeState;
	private CompanyLogo _companyLogo;
	private Button _gladiatorsButton;
	private Label _companyNameLabel;
	private Label _gladiatorCountLabel;
	private Label _championsWonCountLabel;
	private Label _goldLabel;
	private Label _rationsSupplyLabel;
	private Label _starvingLabel;
	private Label _exhaustedLabel;
	private Button _speedToggleButton;
	private Label _dayLabel;
	private TimelineLine _dayProgress;
	private Label _dayPhaseLabel;
	private TimelineLine _championProgress;
	private Label _championProgressValue;
	private Timer _timeTickTimer;

	public override void _Ready()
	{
		_saveNode = SaveNode.Get();
		_timeState = _saveNode?.TownTimeState ?? new TownTimeState();

		_companyLogo = GetNode<CompanyLogo>("TopPanel/Row/CompanyStatus/Shield");
		_gladiatorsButton = GetNode<Button>("TopPanel/Row/GladiatorsButton");
		_companyNameLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/CompanyName");
		_gladiatorCountLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/StatsRow/GladiatorCount");
		_championsWonCountLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/StatsRow/ChampionsWonCount");
		_goldLabel = GetNode<Label>("TopPanel/Row/GoldPanel/ResourceColumn/GoldRow/GoldLabel");
		_rationsSupplyLabel = GetNode<Label>("TopPanel/Row/GoldPanel/ResourceColumn/RationsRow/RationsSupplyLabel");
		_starvingLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/StarvingRow/StarvingLabel");
		_exhaustedLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/ExhaustionRow/ExhaustedLabel");
		var companyStatus = GetNode<Control>("TopPanel/Row/CompanyStatus");
		_speedToggleButton = GetNode<Button>("BottomPanel/TimeRow/PauseButton");
		_dayLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarRow/DayLabel");
		_dayProgress = GetNode<TimelineLine>("BottomPanel/TimeRow/CalendarPanel/CalendarRow/DayProgress");
		_dayPhaseLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarRow/DayPhaseLabel");
		_championProgress = GetNode<TimelineLine>("BottomPanel/TimeRow/TimelinePanel/TimelineRow/ChampionProgress");
		_championProgressValue = GetNode<Label>("BottomPanel/TimeRow/TimelinePanel/TimelineRow/ChampionProgressValue");

		_companyLogo.SetLogoData(_saveNode?.CompanyLogoData ?? CompanyLogoData.CreateDefault());
		_companyLogo.Pressed += OpenCompanyOverview;
		_companyLogo.MouseEntered += OnCompanyLogoMouseEntered;
		_companyLogo.MouseExited += OnCompanyLogoMouseExited;
		_gladiatorsButton.Pressed += OpenGladiatorsOverview;
		companyStatus.GuiInput += OnCompanyStatusGuiInput;
		GetNode<Button>("TopPanel/Row/BackButton").Pressed += OnBackPressed;
		GetNode<Button>("BottomPanel/TimeRow/SpeedDownButton").Pressed += OnSpeedDownPressed;
		_speedToggleButton.Pressed += OnPausePressed;
		GetNode<Button>("BottomPanel/TimeRow/SpeedUpButton").Pressed += OnSpeedUpPressed;

		_timeTickTimer = GetNode<Timer>("TimeTickTimer");
		_timeTickTimer.Timeout += OnTimeTickTimerTimeout;
		_timeState.TimeChanged += RefreshTimeUi;
		if (_saveNode?.CompanyRunData != null)
		{
			_saveNode.CompanyRunData.RunChanged += RefreshRunUi;
			_saveNode.CompanyRunData.GladiatorDied += OnGladiatorDied;
		}

		if (_saveNode?.CompanyCareerData != null)
			_saveNode.CompanyCareerData.CareerChanged += RefreshRunUi;

		RefreshCompanyUi();
		RefreshRunUi();
		ConfigureTimelineLines();
		RefreshTimeUi();
	}

	public override void _ExitTree()
	{
		if (_timeState != null)
			_timeState.TimeChanged -= RefreshTimeUi;

		if (_companyLogo != null)
		{
			_companyLogo.Pressed -= OpenCompanyOverview;
			_companyLogo.MouseEntered -= OnCompanyLogoMouseEntered;
			_companyLogo.MouseExited -= OnCompanyLogoMouseExited;
		}

		if (_gladiatorsButton != null)
		{
			_gladiatorsButton.Pressed -= OpenGladiatorsOverview;
		}

		if (_saveNode?.CompanyRunData != null)
		{
			_saveNode.CompanyRunData.RunChanged -= RefreshRunUi;
			_saveNode.CompanyRunData.GladiatorDied -= OnGladiatorDied;
		}

		if (_saveNode?.CompanyCareerData != null)
			_saveNode.CompanyCareerData.CareerChanged -= RefreshRunUi;
	}

	private void OnBackPressed()
	{
		EmitSignal(SignalName.BackPressed);
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
		var currentDay = _timeState.CurrentDay;
		GameTimeController.TickOneSecond(_timeState, _saveNode.CompanyRunData, _saveNode.CompanyCareerData);

		if (_timeState.CurrentDay > currentDay)
		{
			GD.Print($"SaveNode: Autosaving at new day {_timeState.CurrentDay}.");
			_saveNode.Save();
		}
	}

	private void OnCompanyStatusGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
			&& inputEvent is not InputEventScreenTouch { Pressed: true })
			return;

		GetViewport()?.SetInputAsHandled();
		OpenCompanyOverview();
	}

	private void OnCompanyLogoMouseEntered()
	{
		SetHoverScale(_companyLogo, true);
	}

	private void OnCompanyLogoMouseExited()
	{
		SetHoverScale(_companyLogo, false);
	}

	private void OnGladiatorDied(GladiatorData gladiatorData)
	{
		_timeState.ResetToPause();

		var deathOverlayScene = ResourceLoader.Load<PackedScene>(GladiatorDeathOverlayScenePath);
		if (deathOverlayScene == null)
			return;

		var overlay = deathOverlayScene.Instantiate<GladiatorDeathOverlay>();
		overlay.Configure(gladiatorData);
		GlobalOverlay.Get()?.AddOverlay(overlay);
		_saveNode.Save();
	}

	private void OpenCompanyOverview()
	{
		var globalOverlay = GlobalOverlay.Get();
		var companyOverviewScene = ResourceLoader.Load<PackedScene>(CompanyOverviewScenePath);
		if (globalOverlay == null || companyOverviewScene == null)
			return;

		var overview = companyOverviewScene.Instantiate<CompanyOverviewOverlay>();
		overview.EditCompanyRequested += OnEditCompanyRequested;
		globalOverlay.AddOverlay(overview);
	}

	private void OpenGladiatorsOverview()
	{
		var globalOverlay = GlobalOverlay.Get();
		var gladiatorsOverlayScene = ResourceLoader.Load<PackedScene>(GladiatorsOverlayScenePath);
		if (globalOverlay == null || gladiatorsOverlayScene == null)
			return;

		globalOverlay.AddOverlay(gladiatorsOverlayScene.Instantiate<GladiatorsOverlay>());
	}

	private static void SetHoverScale(Control control, bool hovered)
	{
		if (control == null)
			return;

		control.PivotOffset = control.Size * 0.5f;
		control.Scale = hovered ? new Vector2(1.04f, 1.04f) : Vector2.One;
	}

	private void OnEditCompanyRequested()
	{
		OpenCompanyEditor();
	}

	private void OpenCompanyEditor()
	{
		var globalOverlay = GlobalOverlay.Get();
		var companyLogoEditorScene = ResourceLoader.Load<PackedScene>(CompanyLogoEditorScenePath);
		if (globalOverlay == null || companyLogoEditorScene == null || _saveNode == null)
			return;

		var editor = companyLogoEditorScene.Instantiate<CompanyLogoEditorOverlay>();
		editor.Configure(_saveNode.CompanyLogoData.CreateCopy(), _saveNode.HasCompany, OnCompanyApplied);
		globalOverlay.AddOverlay(editor);
	}

	private void OnCompanyApplied(CompanyLogoData logoData)
	{
		var isNewCompany = !_saveNode.HasCompany;
		_saveNode.CompanyLogoData.CopyFrom(logoData);
		if (isNewCompany)
			_saveNode.StartNewCompanyRun();

		_saveNode.HasCompany = true;
		_saveNode.Save();
		_companyLogo.SetLogoData(_saveNode.CompanyLogoData);
		RefreshCompanyUi();
	}

	private void RefreshCompanyUi()
	{
		_companyNameLabel.Text = (_saveNode?.CompanyLogoData ?? CompanyLogoData.CreateDefault()).CompanyName;
	}

	private void RefreshRunUi()
	{
		var runData = _saveNode?.CompanyRunData ?? new CompanyRunData();
		var careerData = _saveNode?.CompanyCareerData ?? new CompanyCareerData();

		_gladiatorCountLabel.Text = $"Gladiators: {runData.AliveGladiators}";
		_championsWonCountLabel.Text = $"Champions slayed: {careerData.ChampionsDefeated}";
		_goldLabel.Text = runData.Gold.ToString();
		_rationsSupplyLabel.Text = (runData.Rations?.GetTotal() ?? 0).ToString();
		_starvingLabel.Text = runData.GetStarvingGladiatorCount().ToString();
		_exhaustedLabel.Text = runData.GetExhaustedGladiatorCount().ToString();
	}

	private void RefreshTimeUi()
	{
		_dayLabel.Text = _timeState.GetDayLabel();
		_dayProgress.SetValue(_timeState.GetDayProgressValue(), _timeState.GetDayProgressMax());
		_dayPhaseLabel.Text = _timeState.GetDayPhaseLabel();
		_championProgress.SetValue(_timeState.GetChampionProgressValue(), _timeState.GetChampionProgressMax());
		_championProgressValue.Text = _timeState.GetChampionDeadlineLabel();
		RefreshSpeedToggleButton();
	}

	private void ConfigureTimelineLines()
	{
		_dayProgress.ClearSegments();
		_dayProgress.AddSegment(0, _timeState.GetTownOpenMinute(), new Color(0.72f, 0.28f, 0.22f, 0.22f));
		_dayProgress.AddSegment(_timeState.GetTownOpenMinute(), _timeState.GetTownCloseMinute(), new Color(0.28f, 0.68f, 0.34f, 0.32f));
		_dayProgress.AddSegment(_timeState.GetTownCloseMinute(), _timeState.GetDayProgressMax(), new Color(0.85f, 0.64f, 0.24f, 0.24f));

		_championProgress.ClearSegments();
		_championProgress.AddSegment(_timeState.GetChampionFinalDayStart(), _timeState.GetChampionProgressMax(), new Color(0.75f, 0.16f, 0.12f, 0.32f));
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
			TownTimeState.TimeSpeed.X100 => _speedX10Icon,
			TownTimeState.TimeSpeed.X10 => _speedX1Icon,
			TownTimeState.TimeSpeed.X0 => _speedX0Icon,
			_ => _speedSlowedIcon
		};
	}
}
