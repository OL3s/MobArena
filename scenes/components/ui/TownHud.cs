using Godot;
using MobArena.Scenes.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class TownHud : CanvasLayer
{
	private const int DevActionCompleteArenaDay = 1;
	private const string CompanyLogoEditorScenePath = "res://scenes/ui/CompanyLogoEditorOverlay.tscn";
	private const string CompanyOverviewScenePath = "res://scenes/ui/CompanyOverviewOverlay.tscn";
	private const string GladiatorDeathOverlayScenePath = "res://scenes/ui/GladiatorDeathOverlay.tscn";
	private const string TownHoverInfoPanelScenePath = "res://scenes/components/ui/TownHoverInfoPanel.tscn";

	[Signal]
	public delegate void BackPressedEventHandler();

	[Signal]
	public delegate void SelectContractPressedEventHandler();

	private SaveNode _saveNode;
	private TownPhaseState _phaseState;
	private CompanyLogo _companyLogo;
	private Label _companyNameLabel;
	private Label _gladiatorCountLabel;
	private Label _championsWonCountLabel;
	private Label _goldLabel;
	private Label _fameLabel;
	private Label _criticalRiskLabel;
	private Label _idleLabel;
	private Label _exhaustedLabel;
	private Label _lowHealthLabel;
	private Control _conditionPanel;
	private Button _nextDayButton;
	private Label _dayLabel;
	private Label _championDueLabel;
	private TextureRect _sunIcon;
	private TextureRect _moonIcon;
	private MenuButton _devButton;
	private TownHoverInfoPanel _hoverInfoPanel;
	private object _hoverSource;

	public override void _Ready()
	{
		_saveNode = SaveNode.Get();
		_phaseState = _saveNode?.TownPhaseState ?? new TownPhaseState();

		_companyLogo = GetNode<CompanyLogo>("TopPanel/Row/CompanyStatus/Shield");
		_companyNameLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/CompanyName");
		_gladiatorCountLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/StatsRow/GladiatorCount");
		_championsWonCountLabel = GetNode<Label>("TopPanel/Row/CompanyStatus/CompanyText/StatsRow/ChampionsWonCount");
		_goldLabel = GetNode<Label>("TopPanel/Row/WealthPanel/WealthColumn/GoldRow/GoldLabel");
		_fameLabel = GetNode<Label>("TopPanel/Row/WealthPanel/WealthColumn/FameRow/FameLabel");
		_conditionPanel = GetNode<Control>("TopPanel/Row/ConditionPanel");
		_criticalRiskLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/CriticalRiskRow/CriticalRiskLabel");
		_idleLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/IdleRow/IdleLabel");
		_exhaustedLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/ExhaustionRow/ExhaustedLabel");
		_lowHealthLabel = GetNode<Label>("TopPanel/Row/ConditionPanel/ConditionColumn/RiskGrid/LowHealthRow/LowHealthLabel");
		var companyStatus = GetNode<Control>("TopPanel/Row/CompanyStatus");
		_nextDayButton = GetNode<Button>("BottomPanel/TimeRow/PhaseActionButton");
		_dayLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/CalendarRow/DayLabel");
		_championDueLabel = GetNode<Label>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/ChampionRow/ChampionDueLabel");
		_sunIcon = GetNode<TextureRect>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/CalendarRow/SunIcon");
		_moonIcon = GetNode<TextureRect>("BottomPanel/TimeRow/CalendarPanel/CalendarColumn/CalendarRow/MoonIcon");
		_devButton = GetNode<MenuButton>("TopPanel/Row/DevButton");
		GetNodeOrNull<Control>("TopPanel/Row/SupplyPanel")?.Hide();
		CreateHoverInfoPanel(GetNode<HBoxContainer>("BottomPanel/TimeRow"));
		AddToGroup("town_hover_info");

		_companyLogo.SetLogoData(_saveNode?.CompanyLogoData ?? CompanyLogoData.CreateDefault());
		_companyLogo.Pressed += OpenCompanyOverview;
		_companyLogo.MouseEntered += OnCompanyLogoMouseEntered;
		_companyLogo.MouseExited += OnCompanyLogoMouseExited;
		companyStatus.GuiInput += OnCompanyStatusGuiInput;
		GetNode<Button>("TopPanel/Row/BackButton").Pressed += OnBackPressed;
		_nextDayButton.Pressed += OnNextDayPressed;
		SetupDevMenu();
		_phaseState.PhaseChanged += RefreshPhaseUi;

		if (_saveNode?.CompanyRunData != null)
		{
			_saveNode.CompanyRunData.RunChanged += RefreshRunUi;
			_saveNode.CompanyRunData.GladiatorDied += OnGladiatorDied;
		}

		if (_saveNode?.CompanyCareerData != null)
			_saveNode.CompanyCareerData.CareerChanged += RefreshRunUi;

		RefreshCompanyUi();
		RefreshRunUi();
		RefreshDevMenu();
		RefreshPhaseUi();
	}

	public override void _ExitTree()
	{
		if (_phaseState != null)
			_phaseState.PhaseChanged -= RefreshPhaseUi;

		if (_companyLogo != null)
		{
			_companyLogo.Pressed -= OpenCompanyOverview;
			_companyLogo.MouseEntered -= OnCompanyLogoMouseEntered;
			_companyLogo.MouseExited -= OnCompanyLogoMouseExited;
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

	private void OnNextDayPressed()
	{
		if (!_phaseState.CanAdvanceToNextDay)
		{
			if (_saveNode?.CompanyRunData?.CanPayArenaReturnUpkeep(_phaseState) == false)
				return;

			EmitSignal(SignalName.SelectContractPressed);
			return;
		}

		if (!PhaseTransitionController.AdvanceToNextDay(_phaseState, _saveNode.CompanyRunData))
			return;

		GD.Print($"SaveNode: Autosaving at day {_phaseState.CurrentDay}.");
		_saveNode.Save();
	}

	private void SetupDevMenu()
	{
		_devButton.Visible = _saveNode?.DebugEnabled == true;
		var popup = _devButton.GetPopup();
		popup.Clear();
		popup.AddItem("Day -> Night", DevActionCompleteArenaDay);
		popup.IdPressed += OnDevMenuIdPressed;
	}

	private void OnDevMenuIdPressed(long id)
	{
		if (_saveNode?.DebugEnabled != true)
			return;

		if (id != DevActionCompleteArenaDay)
			return;

		if (!PhaseTransitionController.CompleteArenaDay(_phaseState, _saveNode.CompanyRunData))
			return;

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
		_criticalRiskLabel.Text = criticalRiskCount.ToString();
		_idleLabel.Text = idleCount.ToString();
		_exhaustedLabel.Text = exhaustedCount.ToString();
		_lowHealthLabel.Text = lowHealthCount.ToString();
	}

	private void RefreshPhaseUi()
	{
		_dayLabel.Text = _phaseState.GetDayLabel();
		_championDueLabel.Text = _phaseState.GetChampionLabel();
		_sunIcon.Visible = _phaseState.IsDay();
		_moonIcon.Visible = _phaseState.IsNight();
		RefreshDevMenu();
		RefreshNextDayButton();
	}

	private void RefreshDevMenu()
	{
		if (_devButton == null)
			return;

		_devButton.Visible = _saveNode?.DebugEnabled == true;
		_devButton.GetPopup().SetItemDisabled(0, !_phaseState.IsDay());
	}

	private void RefreshNextDayButton()
	{
		_nextDayButton.Text = _phaseState.CanAdvanceToNextDay ? "Next Day" : "Select Contract";
		_nextDayButton.Icon = null;
		var runData = _saveNode?.CompanyRunData;
		var canPayPhaseCosts = runData?.CanPayCurrentPhaseGoldCost(_phaseState) != false;
		var canPayArenaReturnUpkeep = runData?.CanPayArenaReturnUpkeep(_phaseState) != false;
		_nextDayButton.Disabled = _phaseState.CanAdvanceToNextDay ? !canPayPhaseCosts : !canPayArenaReturnUpkeep;
		_nextDayButton.TooltipText = GetPhaseActionTooltip(runData, canPayPhaseCosts, canPayArenaReturnUpkeep);
	}

	private string GetPhaseActionTooltip(CompanyRunData runData, bool canPayPhaseCosts, bool canPayArenaReturnUpkeep)
	{
		if (_phaseState.CanAdvanceToNextDay)
			return canPayPhaseCosts
				? string.Empty
				: $"Need {runData.GetCurrentPhaseGoldCost(_phaseState)} gold to pay current phase costs.";

		return canPayArenaReturnUpkeep
			? "Open arena contracts."
			: $"Need {runData.GetArenaReturnUpkeepGoldCost(_phaseState)} gold for upkeep when the arena day ends.";
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
