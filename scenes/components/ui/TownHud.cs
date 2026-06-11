using Godot;
using MobArena.Scenes.TownOverlays;
using MobArena.Scenes.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Contracts;

namespace MobArena.Scenes.Components.UI;

public partial class TownHud : CanvasLayer
{
	private const int DevActionWinArena = 1;
	private const int DevActionAddDefaultGladiator = 2;
	private const int DevActionAddGold = 3;
	private const int DevActionQuickstartArena = 4;
	private const int DevActionEquipmentVisualTest = 5;
	private const int DevActionWeatherCloudy = 101;
	private const int DevActionWeatherSun = 102;
	private const int DevActionWeatherRain = 103;
	private const string DevWeatherSubmenuName = "WeatherSubmenu";
	private const string CompanyLogoEditorScenePath = "res://scenes/ui/CompanyLogoEditorOverlay.tscn";
	private const string CompanyOverviewScenePath = "res://scenes/ui/CompanyOverviewOverlay.tscn";
	private const string GladiatorDeathOverlayScenePath = "res://scenes/ui/GladiatorDeathOverlay.tscn";
	private const string ArenaScenePath = "res://scenes/arena.tscn";
	private const string StarterSlimePitContractPath = "res://resources/contracts/starter_slime_pit.tres";
	private const string EquipmentVisualTestOverlayScenePath = "res://scenes/ui/EquipmentVisualTestOverlay.tscn";
	private const string TownHoverInfoPanelScenePath = "res://scenes/components/ui/TownHoverInfoPanel.tscn";
	private const string ChampionDayPopupTitle = "Champion Day";
	private const string ChampionDayPopupText = "A champion contract is mandatory today. Win to continue the company. Lose, and the company is force-retired.";
	private const string NextDayUpkeepPopupTitle = "Night Upkeep";
	private const string NextDayUpkeepPopupText = "Advancing to the next day resolves all Night costs. Assigned buildings may charge for treatment or training, and every active gladiator is paid salary. These costs can put the company into debt, so use the summary to plan your next contract.";
	private const string FirstNextDayPopupTitle = "The City Opens Up";
	private const string FirstNextDayPopupText = "After a fight, Night is when your company pays salaries and resolves any queued town costs. Use the summary to understand what will happen before starting the next Day.";

	[Signal]
	public delegate void BackPressedEventHandler();

	[Signal]
	public delegate void SelectContractPressedEventHandler();

	[Signal]
	public delegate void BuyGladiatorPressedEventHandler();

	[Export]
	public Texture2D CloudyWeatherIcon { get; set; }

	[Export]
	public Texture2D RainWeatherIcon { get; set; }

	[Export]
	public Texture2D SunWeatherIcon { get; set; }

	[Export]
	public Texture2D GoldIcon { get; set; }

	[Export]
	public Texture2D ChampionIcon { get; set; }

	[Export]
	public PackedScene NextDaySummaryOverlayScene { get; set; }

	[Export]
	public PackedScene WeatherChampionInfoOverlayScene { get; set; }

	private SaveNode _saveNode;
	private TownPhaseState _phaseState;
	private CompanyRunData _subscribedRunData;
	private CompanyCareerData _subscribedCareerData;
	private CompanyLogo _companyLogo;
	private Label _companyNameLabel;
	private Label _gladiatorCountLabel;
	private Label _championsWonCountLabel;
	private Label _goldLabel;
	private Label _fameLabel;
	private Control _criticalRiskRow;
	private Control _idleRow;
	private Control _exhaustionRow;
	private Control _lowHealthRow;
	private Label _criticalRiskLabel;
	private Label _idleLabel;
	private Label _exhaustedLabel;
	private Label _lowHealthLabel;
	private Control _conditionPanel;
	private Control _calendarPanel;
	private Button _nextDayButton;
	private Label _dayLabel;
	private Label _championDueLabel;
	private ProgressBar _championProgressBar;
	private TextureRect _weatherIcon;
	private TextureRect _moonIcon;
	private MenuButton _devButton;
	private PopupMenu _devWeatherSubmenu;
	private TownHoverInfoPanel _hoverInfoPanel;
	private object _hoverSource;

	public override void _Ready()
	{
		_saveNode = SaveNode.Get();
		_saveNode.RuntimeStateResetting += OnRuntimeStateResetting;
		_saveNode.DevModeChanged += RefreshDevMenu;
		_phaseState = _saveNode?.TownPhaseState ?? new TownPhaseState();

		_companyLogo = GetNode<CompanyLogo>("TopPanel/Row/CompanyStatus/Shield");
		_companyNameLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/CompanyName");
		_gladiatorCountLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/StatsRow/GladiatorCount");
		_championsWonCountLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/StatsRow/ChampionsWonCount");
		_goldLabel = GetNode<Label>("TopPanel/Row/WealthPanel/WealthColumn/GoldRow/GoldLabel");
		_fameLabel = GetNode<Label>("TopPanel/Row/WealthPanel/WealthColumn/FameRow/FameLabel");
		_conditionPanel = GetNode<Control>("TopPanel/Row/ConditionPanel");
		_criticalRiskRow = GetNode<Control>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/CriticalRiskRow");
		_idleRow = GetNode<Control>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/IdleRow");
		_exhaustionRow = GetNode<Control>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/ExhaustionRow");
		_lowHealthRow = GetNode<Control>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/LowHealthRow");
		_criticalRiskLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/CriticalRiskRow/CriticalRiskLabel");
		_idleLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/IdleRow/IdleLabel");
		_exhaustedLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/ExhaustionRow/ExhaustedLabel");
		_lowHealthLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/LowHealthRow/LowHealthLabel");
		var companyStatus = GetNode<Control>("TopPanel/Row/CompanyStatus");
		_nextDayButton = GetNode<Button>("BottomPanel/TimeRow/PhaseActionButton");
		_calendarPanel = GetNode<Control>("BottomPanel/TimeRow/CalendarPanel");
		_dayLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/CalendarRow/DayLabel");
		_championDueLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/ChampionRow/ChampionDueLabel");
		_championProgressBar = GetNode<ProgressBar>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/ChampionProgressBar");
		_weatherIcon = GetNode<TextureRect>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/CalendarRow/IconRow/WeatherIcon");
		_moonIcon = GetNode<TextureRect>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/CalendarRow/IconRow/MoonIcon");
		_devButton = GetNode<MenuButton>("TopPanel/Row/DevButton");
		GetNodeOrNull<Control>("TopPanel/Row/SupplyPanel")?.Hide();
		CreateHoverInfoPanel(GetNode<HBoxContainer>("BottomPanel/TimeRow"));
		AddToGroup("town_hover_info");

		_companyLogo.SetLogoData(_saveNode?.CompanyLogoData ?? CompanyLogoData.CreateDefault());
		_companyLogo.Pressed += OpenCompanyOverview;
		_companyLogo.MouseEntered += OnCompanyLogoMouseEntered;
		_companyLogo.MouseExited += OnCompanyLogoMouseExited;
		companyStatus.GuiInput += OnCompanyStatusGuiInput;
		_calendarPanel.GuiInput += OnCalendarPanelGuiInput;
		GetNode<Button>("TopPanel/Row/BackButton").Pressed += OnBackPressed;
		_nextDayButton.Pressed += OnNextDayPressed;
		SetupDevMenu();
		_phaseState.PhaseChanged += RefreshPhaseUi;

		_subscribedRunData = _saveNode?.CompanyRunData;
		if (_subscribedRunData != null)
		{
			_subscribedRunData.RunChanged += RefreshRunUi;
			_subscribedRunData.GladiatorDied += OnGladiatorDied;
		}

		_subscribedCareerData = _saveNode?.CompanyCareerData;
		if (_subscribedCareerData != null)
			_subscribedCareerData.CareerChanged += RefreshRunUi;

		RefreshCompanyUi();
		RefreshRunUi();
		RefreshDevMenu();
		RefreshPhaseUi();
		ShowPendingGladiatorDeathNotifications();
		SetWeatherVisual(WeatherState.WeatherVisual.Cloudy);
	}

	public override void _ExitTree()
	{
		if (_saveNode != null)
		{
			_saveNode.RuntimeStateResetting -= OnRuntimeStateResetting;
			_saveNode.DevModeChanged -= RefreshDevMenu;
		}

		UnsubscribeResourceSignals();

		if (_companyLogo != null)
		{
			_companyLogo.Pressed -= OpenCompanyOverview;
			_companyLogo.MouseEntered -= OnCompanyLogoMouseEntered;
			_companyLogo.MouseExited -= OnCompanyLogoMouseExited;
		}

		if (_calendarPanel != null)
			_calendarPanel.GuiInput -= OnCalendarPanelGuiInput;
	}

	private void OnRuntimeStateResetting()
	{
		UnsubscribeResourceSignals();
	}

	private void UnsubscribeResourceSignals()
	{
		if (_phaseState != null)
		{
			_phaseState.PhaseChanged -= RefreshPhaseUi;
			_phaseState = null;
		}

		if (_subscribedRunData != null)
		{
			_subscribedRunData.RunChanged -= RefreshRunUi;
			_subscribedRunData.GladiatorDied -= OnGladiatorDied;
			_subscribedRunData = null;
		}

		if (_subscribedCareerData != null)
		{
			_subscribedCareerData.CareerChanged -= RefreshRunUi;
			_subscribedCareerData = null;
		}
	}

	private void OnBackPressed()
	{
		EmitSignal(SignalName.BackPressed);
	}

	private void OnNextDayPressed()
	{
		if (!_phaseState.CanAdvanceToNextDay)
		{
			if (_saveNode?.CompanyRunData?.Gladiators.Count <= 0)
			{
				EmitSignal(SignalName.BuyGladiatorPressed);
				return;
			}

			EmitSignal(SignalName.SelectContractPressed);
			return;
		}

		var runData = _saveNode?.CompanyRunData;
		if (ShouldShowFirstNextDayTutorial(runData))
		{
			runData.MarkNextDayUpkeepPopupShown();
			_saveNode.Save();
			GlobalOverlay.Get()?.ShowBlurredPopup(
				FirstNextDayPopupTitle,
				FirstNextDayPopupText,
				GoldIcon,
				ShowNextDaySummaryOverlay);
			return;
		}

		if (_saveNode?.SkipTutorial != true && runData is { HasShownNextDayUpkeepPopup: false })
		{
			runData.MarkNextDayUpkeepPopupShown();
			_saveNode.Save();
			GlobalOverlay.Get()?.ShowBlurredPopup(
				NextDayUpkeepPopupTitle,
				NextDayUpkeepPopupText,
				GoldIcon,
				ShowNextDaySummaryOverlay);
			return;
		}

		ShowNextDaySummaryOverlay();
	}

	private bool ShouldShowFirstNextDayTutorial(CompanyRunData runData)
	{
		return _saveNode?.SkipTutorial != true
			&& runData is { HasUnlockedSpecialtyBuildings: false }
			&& _saveNode?.HasReachedSpecialtyBuildingsForProgression != true;
	}

	private void ShowNextDaySummaryOverlay()
	{
		var overlay = NextDaySummaryOverlayScene?.Instantiate<NextDaySummaryOverlay>();
		if (overlay == null)
		{
			GD.PushError("Next day summary overlay scene is missing or has the wrong root script.");
			return;
		}

		overlay.Configure(AdvanceToNextDayConfirmed);
		GlobalOverlay.Get()?.AddOverlay(overlay);
	}

	private void AdvanceToNextDayConfirmed()
	{
		var runData = _saveNode.CompanyRunData;
		if (!PhaseTransitionController.AdvanceToNextDay(_phaseState, runData, _saveNode.WeatherState))
			return;

		GD.Print($"SaveNode: Autosaving at day {_phaseState.CurrentDay}.");
		_saveNode.Save();

		if (_phaseState.IsChampionDay)
			ShowChampionDayPopup();
	}

	private void ShowChampionDayPopup()
	{
		GlobalOverlay.Get()?.ShowBlurredPopup(
			ChampionDayPopupTitle,
			ChampionDayPopupText,
			ChampionIcon);
	}

	private void SetupDevMenu()
	{
		_devButton.Visible = _saveNode?.DevEnabled == true;
		var popup = _devButton.GetPopup();
		popup.Clear();
		popup.AddItem("Win arena", DevActionWinArena);
		popup.AddItem("Add default gladiator", DevActionAddDefaultGladiator);
		popup.AddItem("Add 1000 gold", DevActionAddGold);
		popup.AddSeparator();
		popup.AddItem("Quickstart arena", DevActionQuickstartArena);
		popup.AddItem("Equipment visuals", DevActionEquipmentVisualTest);
		SetupDevWeatherMenu(popup);
		popup.IdPressed += OnDevMenuIdPressed;
	}

	private void SetupDevWeatherMenu(PopupMenu popup)
	{
		_devWeatherSubmenu ??= popup.GetNodeOrNull<PopupMenu>(DevWeatherSubmenuName);
		if (_devWeatherSubmenu == null)
		{
			_devWeatherSubmenu = new PopupMenu { Name = DevWeatherSubmenuName };
			popup.AddChild(_devWeatherSubmenu);
			_devWeatherSubmenu.IdPressed += OnDevWeatherMenuIdPressed;
		}

		_devWeatherSubmenu.Clear();
		_devWeatherSubmenu.AddItem("Cloudy", DevActionWeatherCloudy);
		_devWeatherSubmenu.AddItem("Sun", DevActionWeatherSun);
		_devWeatherSubmenu.AddItem("Rain", DevActionWeatherRain);
		popup.AddSubmenuNodeItem("Change weather to", _devWeatherSubmenu);
	}

	private void OnDevMenuIdPressed(long id)
	{
		if (_saveNode?.DevEnabled != true)
			return;

		switch (id)
		{
			case DevActionWinArena:
				if (!CompleteFirstArenaContract())
					return;
				break;
			case DevActionAddDefaultGladiator:
				_saveNode.CompanyRunData?.AddGladiator(GladiatorData.CreateDefault(), _saveNode.CompanyCareerData);
				break;
			case DevActionAddGold:
				_saveNode.CompanyRunData?.AddGold(1000, _saveNode.CompanyCareerData);
				break;
			case DevActionQuickstartArena:
				QuickstartArena();
				return;
			case DevActionEquipmentVisualTest:
				OpenEquipmentVisualTest();
				return;
			default:
				return;
		}

		_saveNode.Save();
	}

	private static void OpenEquipmentVisualTest()
	{
		var overlayScene = ResourceLoader.Load<PackedScene>(EquipmentVisualTestOverlayScenePath);
		var overlay = overlayScene?.Instantiate<EquipmentVisualTestOverlay>();
		if (overlay == null)
		{
			GD.PushError("Equipment visual test overlay scene is missing or has the wrong root script.");
			return;
		}

		GlobalOverlay.Get()?.AddOverlay(overlay);
	}

	private bool CompleteFirstArenaContract()
	{
		var runData = _saveNode?.CompanyRunData;
		if (runData == null)
		{
			GD.PushError("Dev win arena failed: company run data is missing.");
			return false;
		}

		var contract = GetFirstArenaContract(runData.Fame, _phaseState?.IsChampionDay == true, _saveNode?.HasCompletedContractsForProgression == true);
		if (contract == null)
		{
			GD.PushError("Dev win arena failed: no arena contract is available.");
			return false;
		}

		runData.SetActiveArenaContract(contract);
		return ArenaContractResultResolver.ResolveWin(_saveNode) == ArenaContractResultResolver.ContractResult.Completed;
	}

	private void QuickstartArena()
	{
		var runData = _saveNode?.CompanyRunData;
		if (runData == null)
		{
			GD.PushError("Quickstart arena failed: company run data is missing.");
			return;
		}

		runData.EnsureResources();
		if (runData.Gladiators.Count <= 0)
			return;

		var contract = GetQuickstartContract(runData.Fame, _phaseState?.IsChampionDay == true, _saveNode?.HasCompletedContractsForProgression == true);
		if (contract == null)
		{
			GD.PushError("Quickstart arena failed: no contract is available.");
			return;
		}

		var localInputConfig = LocalInputConfig.Get();
		localInputConfig?.ClearControllerSetups();
		runData.ClearArenaControlAssignments();
		var previousArenaGladiators = new Godot.Collections.Array<GladiatorData>(runData.TownAssignments.ArenaGladiators);
		foreach (var gladiator in previousArenaGladiators)
		{
			if (gladiator != null)
				runData.TryMoveGladiatorToCourtyard(gladiator);
		}

		var controllerSetups = CreateQuickstartControllerSetups(localInputConfig, Mathf.Min(runData.Gladiators.Count, LocalInputConfig.MaxLocalPlayers));
		for (var index = 0; index < controllerSetups.Count; index++)
		{
			var gladiator = runData.Gladiators[index];
			if (gladiator == null)
				continue;

			if (!runData.TryAssignGladiatorToTownLocation(gladiator, TownAssignmentData.AssignmentLocation.Arena, LocalInputConfig.MaxLocalPlayers))
				continue;

			runData.TrySetArenaControlAssignment(gladiator, controllerSetups[index]);
		}

		runData.SetActiveArenaContract(contract);
		runData.NotifyRunChanged();
		_saveNode.Save();
		GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
		SceneTransitionLogger.LogChange(GetTree(), ArenaScenePath, "town quickstart arena");
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, ArenaScenePath);
	}

	private static ArenaContractData GetQuickstartContract(int companyFame, bool isChampionDay, bool hasCompletedContracts)
	{
		return GetFirstArenaContract(companyFame, isChampionDay, hasCompletedContracts);
	}

	private static ArenaContractData GetFirstArenaContract(int companyFame, bool isChampionDay, bool hasCompletedContracts)
	{
		if (!hasCompletedContracts)
			return ResourceLoader.Load<ArenaContractData>(StarterSlimePitContractPath);

		var generatedContracts = ArenaContractGenerator.GenerateRandomContracts(companyFame, isChampionDay);
		if (generatedContracts.Count > 0)
			return generatedContracts[0];

		return null;
	}

	private static Godot.Collections.Array<LocalInputControllerConfig> CreateQuickstartControllerSetups(LocalInputConfig localInputConfig, int controllerCount)
	{
		var controllerSetups = new Godot.Collections.Array<LocalInputControllerConfig>();
		for (var index = 0; index < controllerCount; index++)
		{
			switch (index)
			{
				case 0:
					localInputConfig?.TryJoinKeyboard();
					controllerSetups.Add(GetControllerSetup(localInputConfig, LocalInputControllerConfig.ControllerKind.Keyboard, -1)
						?? LocalInputControllerConfig.Create(LocalInputControllerConfig.ControllerKind.Keyboard, -1, DeviceIconRegistry.LoadDeviceIcon(LocalInputControllerConfig.ControllerKind.Keyboard)));
					break;
				case 1:
					localInputConfig?.TryJoinMouse();
					controllerSetups.Add(GetControllerSetup(localInputConfig, LocalInputControllerConfig.ControllerKind.Mouse, -1)
						?? LocalInputControllerConfig.Create(LocalInputControllerConfig.ControllerKind.Mouse, -1, DeviceIconRegistry.LoadDeviceIcon(LocalInputControllerConfig.ControllerKind.Mouse)));
					break;
				case 2:
					localInputConfig?.TryJoinGamepad(0);
					controllerSetups.Add(GetControllerSetup(localInputConfig, LocalInputControllerConfig.ControllerKind.Gamepad, 0)
						?? LocalInputControllerConfig.Create(LocalInputControllerConfig.ControllerKind.Gamepad, 0, DeviceIconRegistry.LoadDeviceIcon(LocalInputControllerConfig.ControllerKind.Gamepad)));
					break;
				case 3:
					localInputConfig?.TryJoinGamepad(1);
					controllerSetups.Add(GetControllerSetup(localInputConfig, LocalInputControllerConfig.ControllerKind.Gamepad, 1)
						?? LocalInputControllerConfig.Create(LocalInputControllerConfig.ControllerKind.Gamepad, 1, DeviceIconRegistry.LoadDeviceIcon(LocalInputControllerConfig.ControllerKind.Gamepad)));
					break;
			}
		}

		return controllerSetups;
	}

	private static LocalInputControllerConfig GetControllerSetup(LocalInputConfig localInputConfig, LocalInputControllerConfig.ControllerKind kind, int deviceId)
	{
		if (localInputConfig == null)
			return null;

		foreach (var controllerSetup in localInputConfig.ControllerSetups)
		{
			if (controllerSetup?.Kind == kind && controllerSetup.DeviceId == deviceId)
				return controllerSetup;
		}

		return null;
	}

	private void OnDevWeatherMenuIdPressed(long id)
	{
		if (_saveNode?.DevEnabled != true || _saveNode.WeatherState == null)
			return;

		var weather = id switch
		{
			DevActionWeatherCloudy => WeatherState.WeatherVisual.Cloudy,
			DevActionWeatherSun => WeatherState.WeatherVisual.Sun,
			DevActionWeatherRain => WeatherState.WeatherVisual.Rain,
			_ => _saveNode.WeatherState.CurrentWeather
		};

		_saveNode.WeatherState.SetWeather(weather);
		SetWeatherVisual(weather);
		_saveNode.Save();
	}

	private void OnCompanyStatusGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
			&& inputEvent is not InputEventScreenTouch { Pressed: true })
			return;

		GetViewport()?.SetInputAsHandled();
		OpenCompanyOverview();
	}

	private void OnCalendarPanelGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
			&& inputEvent is not InputEventScreenTouch { Pressed: true })
			return;

		GetViewport()?.SetInputAsHandled();
		if (WeatherChampionInfoOverlayScene == null)
		{
			GD.PushError("Weather & Champion overlay scene is missing.");
			return;
		}

		GlobalOverlay.Get()?.AddOverlay(WeatherChampionInfoOverlayScene.Instantiate<WeatherChampionInfoOverlay>());
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
		ShowGladiatorDeathOverlay(gladiatorData);
		_saveNode.Save();
	}

	private void ShowPendingGladiatorDeathNotifications()
	{
		var pending = _saveNode?.CompanyRunData?.ConsumePendingGladiatorDeathNotifications();
		if (pending == null || pending.Count <= 0)
			return;

		foreach (var gladiator in pending)
			ShowGladiatorDeathOverlay(gladiator);

		_saveNode.Save();
	}

	private static void ShowGladiatorDeathOverlay(GladiatorData gladiatorData)
	{
		var deathOverlayScene = ResourceLoader.Load<PackedScene>(GladiatorDeathOverlayScenePath);
		if (deathOverlayScene == null)
			return;

		var overlay = deathOverlayScene.Instantiate<GladiatorDeathOverlay>();
		overlay.Configure(gladiatorData);
		GlobalOverlay.Get()?.AddOverlay(overlay);
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
		_fameLabel.Text = runData.Fame.ToString();
		var lowHealthWarningRatio = _saveNode?.SettingsConfig?.LowHealthWarningRatio ?? 0.6f;
		var criticalRiskCount = runData.GetCriticalRiskGladiatorCount(lowHealthWarningRatio);
		var idleCount = runData.GetIdleAssignedGladiatorCount();
		var exhaustedCount = runData.GetExhaustedGladiatorCount();
		var lowHealthCount = runData.GetLowHealthGladiatorCount(lowHealthWarningRatio);
		_conditionPanel.Visible = criticalRiskCount + idleCount + exhaustedCount + lowHealthCount > 0;
		_criticalRiskRow.Visible = criticalRiskCount > 0;
		_idleRow.Visible = idleCount > 0;
		_exhaustionRow.Visible = exhaustedCount > 0;
		_lowHealthRow.Visible = lowHealthCount > 0;
		_criticalRiskLabel.Text = criticalRiskCount.ToString();
		_idleLabel.Text = idleCount.ToString();
		_exhaustedLabel.Text = exhaustedCount.ToString();
		_lowHealthLabel.Text = lowHealthCount.ToString();
		RefreshNextDayButton();
	}

	private void RefreshPhaseUi()
	{
		_dayLabel.Text = _phaseState.GetDayLabel();
		_championDueLabel.Text = _phaseState.GetChampionLabel();
		RefreshChampionProgressBar();
		_moonIcon.Visible = _phaseState.IsNight();
		RefreshDevMenu();
		RefreshNextDayButton();
	}

	private void RefreshChampionProgressBar()
	{
		if (_championProgressBar == null)
			return;

		_championProgressBar.Visible = !_phaseState.IsChampionDay;
		_championProgressBar.MaxValue = 7;
		_championProgressBar.Value = Mathf.Clamp(7 - _phaseState.DaysUntilChampion, 0, 7);
	}

	public void SetWeatherVisual(WeatherState.WeatherVisual weather)
	{
		if (_weatherIcon == null)
			return;

		_weatherIcon.Texture = weather switch
		{
			WeatherState.WeatherVisual.Rain => RainWeatherIcon,
			WeatherState.WeatherVisual.Sun => SunWeatherIcon,
			_ => CloudyWeatherIcon ?? SunWeatherIcon
		};
	}

	private void RefreshDevMenu()
	{
		if (_devButton == null)
			return;

		_devButton.Visible = _saveNode?.DevEnabled == true;
		var popup = _devButton.GetPopup();
		popup.SetItemDisabled(0, !_phaseState.IsDay());

		var quickstartIndex = popup.GetItemIndex(DevActionQuickstartArena);
		if (quickstartIndex < 0)
			return;

		var gladiatorCount = _saveNode?.CompanyRunData?.Gladiators?.Count ?? 0;
		popup.SetItemDisabled(quickstartIndex, gladiatorCount <= 0);
		popup.SetItemTooltip(quickstartIndex, gladiatorCount <= 0
			? "Buy at least one gladiator before quickstarting the arena."
			: $"Assign the first up to four gladiators to {GetQuickstartControllerOrderLabel()} and launch the first contract.");
	}

	private static string GetQuickstartControllerOrderLabel()
	{
		return string.Join(", ",
			LocalInputControllerConfig.GetDisplayName(LocalInputControllerConfig.ControllerKind.Keyboard, -1),
			LocalInputControllerConfig.GetDisplayName(LocalInputControllerConfig.ControllerKind.Mouse, -1),
			LocalInputControllerConfig.GetDisplayName(LocalInputControllerConfig.ControllerKind.Gamepad, 0),
			LocalInputControllerConfig.GetDisplayName(LocalInputControllerConfig.ControllerKind.Gamepad, 1));
	}

	private void RefreshNextDayButton()
	{
		_nextDayButton.Icon = null;
		var runData = _saveNode?.CompanyRunData;
		var hasNoGladiators = runData?.Gladiators.Count <= 0;
		_nextDayButton.Text = _phaseState.CanAdvanceToNextDay ? "Next Day" : hasNoGladiators ? "Buy Gladiator" : "Select Contract";
		var canPayPhaseCosts = runData?.CanPayCurrentPhaseGoldCost(_phaseState) != false;
		var canPayArenaReturnUpkeep = runData?.CanPayArenaReturnUpkeep(_phaseState) != false;
		_nextDayButton.Disabled = false;
		_nextDayButton.TooltipText = GetPhaseActionTooltip(runData, canPayPhaseCosts, canPayArenaReturnUpkeep);
	}

	private string GetPhaseActionTooltip(CompanyRunData runData, bool canPayPhaseCosts, bool canPayArenaReturnUpkeep)
	{
		if (_phaseState.CanAdvanceToNextDay)
			return canPayPhaseCosts
				? string.Empty
				: "Advance to the next day and go into debt.";

		if (runData?.Gladiators.Count <= 0)
			return "Open the gladiator market.";

		return canPayArenaReturnUpkeep
			? "Open arena contracts."
			: "Open arena contracts and go into debt if upkeep is due.";
	}

	public void ShowGladiatorHoverInfo(object source, GladiatorData gladiatorData)
	{
		if (gladiatorData == null)
			return;

		_hoverSource = source;
		_hoverInfoPanel.ShowGladiator(gladiatorData);
	}

	public void ShowBuildingHoverInfo(object source, Texture2D icon, string title, string description)
	{
		_hoverSource = source;
		_hoverInfoPanel.ShowBuilding(icon, title, description);
	}

	public void HideHoverInfo(object source)
	{
		if (_hoverSource != source)
			return;

		_hoverSource = null;
		_hoverInfoPanel.Clear();
	}

	private void CreateHoverInfoPanel(HBoxContainer timeRow)
	{
		var hoverInfoPanelScene = ResourceLoader.Load<PackedScene>(TownHoverInfoPanelScenePath);
		if (hoverInfoPanelScene == null)
		{
			GD.PushError($"Town HUD failed to load hover info panel scene: {TownHoverInfoPanelScenePath}");
			return;
		}

		_hoverInfoPanel = hoverInfoPanelScene.Instantiate<TownHoverInfoPanel>();
		timeRow.AddChild(_hoverInfoPanel);
	}
}
