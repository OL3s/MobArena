using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.UI;

namespace MobArena.Scripts;

public partial class MainMenu : Control
{
    private const string TownScene = "res://scenes/town.tscn";
    private const string CompanyLogoEditorScenePath = "res://scenes/ui/CompanyLogoEditorOverlay.tscn";
    private static readonly PackedScene CompanyLogoEditorScene = ResourceLoader.Load<PackedScene>(CompanyLogoEditorScenePath);

    private CompanyLogo _companyLogo;
    private Button _createCompanyButton;
    private Button _enterTownButton;
    private SaveNode _saveNode;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _companyLogo = GetNode<CompanyLogo>("MenuRow/Shield");
        _createCompanyButton = GetNode<Button>("MenuRow/CreateCompanyButton");
        _enterTownButton = GetNode<Button>("MenuRow/Content/EnterTownButton");

        _createCompanyButton.Pressed += OnCreateCompanyPressed;
        _companyLogo.Pressed += OnCreateCompanyPressed;
        _enterTownButton.Pressed += OnEnterTownPressed;

        GetNode<Button>("MenuRow/Content/QuitButton").Pressed += OnQuitPressed;

        RefreshCompanyUi();
        _createCompanyButton.CallDeferred(Control.MethodName.GrabFocus);
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
        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null || CompanyLogoEditorScene == null || _saveNode == null)
            return;

        var editor = CompanyLogoEditorScene.Instantiate<CompanyLogoEditorOverlay>();
        editor.Configure(_saveNode.CreateEditableCompanyData(), _saveNode.HasCompany, OnCompanyApplied);
        globalOverlay.AddOverlay(editor);
    }

    private void OnCompanyApplied(MobArena.Scripts.Resources.CompanyLogoData logoData)
    {
        _saveNode.ApplyCompanyData(logoData);
        RefreshCompanyUi();
    }

    private void RefreshCompanyUi()
    {
        if (_saveNode == null)
            return;

        _companyLogo.SetLogoData(_saveNode.CompanyLogoData);
        _companyLogo.Visible = _saveNode.HasCompany;
        _createCompanyButton.Visible = !_saveNode.HasCompany;
        _enterTownButton.Disabled = !_saveNode.HasCompany;
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
