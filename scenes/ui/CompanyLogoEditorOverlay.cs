using Godot;
using System;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class CompanyLogoEditorOverlay : Control
{
    private CompanyLogoData _sourceData;
    private CompanyLogoData _workingData;
    private Action<CompanyLogoData> _onApplied;
    private bool _canCancel;
    private CompanyLogo _preview;
    private LineEdit _companyNameEdit;
    private OptionButton _shieldOptions;
    private OptionButton _logoOptions;
    private Button _cancelButton;

    public override void _Ready()
    {
        _preview = GetNode<CompanyLogo>("CenterContainer/PopupPanel/MarginContainer/Content/Preview");
        _companyNameEdit = GetNode<LineEdit>("CenterContainer/PopupPanel/MarginContainer/Content/CompanyNameEdit");
        _shieldOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/ShieldOptions");
        _logoOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/LogoOptions");
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/ApplyButton").Pressed += OnApplyPressed;
        _cancelButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CancelButton");
        _cancelButton.Pressed += QueueFree;

        ApplyConfigurationToUi();
    }

    public void Configure(CompanyLogoData logoData, bool canCancel, Action<CompanyLogoData> onApplied)
    {
        _sourceData = logoData ?? CompanyLogoData.CreateDefault();
        _workingData = _sourceData.CreateCopy();
        _onApplied = onApplied;
        _canCancel = canCancel;

        if (!IsNodeReady())
            return;

        ApplyConfigurationToUi();
    }

    private void ApplyConfigurationToUi()
    {
        if (_preview == null || _workingData == null)
            return;

        _preview.SetLogoData(_workingData);
        _companyNameEdit.Text = _workingData.CompanyName;
        _cancelButton.Visible = _canCancel;
        PopulateOptions();
    }

    private void PopulateOptions()
    {
        _shieldOptions.Clear();
        for (var i = 0; i < _workingData.GetShieldCount(); i++)
            _shieldOptions.AddItem(_workingData.GetShieldName(i), i);

        _logoOptions.Clear();
        for (var i = 0; i < _workingData.GetLogoCount(); i++)
            _logoOptions.AddItem(_workingData.GetLogoName(i), i);

        _shieldOptions.Select(_workingData.ShieldIndex);
        _logoOptions.Select(_workingData.LogoIndex);

        _shieldOptions.ItemSelected += OnShieldSelected;
        _logoOptions.ItemSelected += OnLogoSelected;
    }

    private void OnShieldSelected(long index)
    {
        _workingData.SetShieldIndex((int)index);
    }

    private void OnLogoSelected(long index)
    {
        _workingData.SetLogoIndex((int)index);
    }

    private void OnApplyPressed()
    {
        _workingData.SetCompanyName(_companyNameEdit.Text);
        _sourceData.CopyFrom(_workingData);
        _onApplied?.Invoke(_sourceData);
        QueueFree();
    }
}
