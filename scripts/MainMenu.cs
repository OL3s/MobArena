using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.UI;

namespace MobArena.Scripts;

public partial class MainMenu : Control
{
	private const string TownScene = "res://scenes/town.tscn";
	private const string CompanyLogoEditorScenePath = "res://scenes/ui/CompanyLogoEditorOverlay.tscn";
	private const string CompanyOverviewScenePath = "res://scenes/ui/CompanyOverviewOverlay.tscn";
	private const string ControlsOverlayScenePath = "res://scenes/ui/ControlsOverlay.tscn";

	private CompanyLogo _companyLogo;
	private Button _createCompanyButton;
	private Button _enterTownButton;
	private Button _buildCompanyButton;
	private SaveNode _saveNode;

	public override void _Ready()
	{
		_saveNode = SaveNode.Get();
		_companyLogo = GetNode<CompanyLogo>("MenuRow/Shield");
		_createCompanyButton = GetNode<Button>("MenuRow/CreateCompanyButton");
		_enterTownButton = GetNode<Button>("MenuRow/Content/EnterTownButton");
		_buildCompanyButton = GetNode<Button>("MenuRow/Content/BuildCompanyButton");

		_createCompanyButton.Pressed += OnCreateCompanyPressed;
		_companyLogo.Pressed += OnCompanyLogoPressed;
		_enterTownButton.Pressed += OnEnterTownPressed;
		_buildCompanyButton.Pressed += OnCreateCompanyPressed;

		GetNode<Button>("TopRightActions/ControlsButton").Pressed += OnControlsPressed;
		GetNode<Button>("MenuRow/Content/QuitButton").Pressed += OnQuitPressed;

		RefreshCompanyUi();
		CallDeferred(MethodName.GrabDefaultFocus);
	}

	private void OnEnterTownPressed()
	{
		if (_saveNode is not { HasCompany: true })
			return;

		_saveNode.TownTimeState.ResetToPause();
		GetTree().ChangeSceneToFile(TownScene);
	}

	private void OnCreateCompanyPressed()
	{
		OpenCompanyEditor();
	}

	private static void OnControlsPressed()
	{
		var controlsOverlayScene = ResourceLoader.Load<PackedScene>(ControlsOverlayScenePath);
		if (controlsOverlayScene == null)
			return;

		GlobalOverlay.Get()?.AddOverlay(controlsOverlayScene.Instantiate<ControlsOverlay>());
	}

	private void OnCompanyLogoPressed()
	{
		if (_saveNode is { HasCompany: true })
			OpenCompanyOverview();
		else
			OpenCompanyEditor();
	}

	private void OpenCompanyOverview()
	{
		var globalOverlay = GlobalOverlay.Get();
		var companyOverviewScene = ResourceLoader.Load<PackedScene>(CompanyOverviewScenePath);
		if (globalOverlay == null || companyOverviewScene == null)
			return;

		var overview = companyOverviewScene.Instantiate<CompanyOverviewOverlay>();
		overview.EditCompanyRequested += OpenCompanyEditor;
		globalOverlay.AddOverlay(overview);
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

	private void OnCompanyApplied(MobArena.Scripts.Resources.CompanyLogoData logoData)
	{
		_saveNode.CompanyLogoData.CopyFrom(logoData);
		_saveNode.HasCompany = true;
		RefreshCompanyUi();
	}

	private void RefreshCompanyUi()
	{
		if (_saveNode == null)
			return;

		_companyLogo.SetLogoData(_saveNode.CompanyLogoData);
		_companyLogo.Visible = _saveNode.HasCompany;
		_createCompanyButton.Visible = !_saveNode.HasCompany;
		_enterTownButton.Visible = _saveNode.HasCompany;
		_buildCompanyButton.Visible = !_saveNode.HasCompany;
	}

	private void GrabDefaultFocus()
	{
		if (_saveNode is { HasCompany: true })
			_enterTownButton.GrabFocus();
		else
			_buildCompanyButton.GrabFocus();
	}

	private void OnQuitPressed()
	{
		GlobalOverlay.Get()?.CloseAllOverlays();
		GetTree().Quit();
	}
}
