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
    private OptionButton _shieldColorOptions;
    private OptionButton _logoOptions;
    private OptionButton _logoSizeOptions;
    private Button _cancelButton;

    public override void _Ready()
    {
        _preview = GetNode<CompanyLogo>("CenterContainer/PopupPanel/MarginContainer/Content/Preview");
        _companyNameEdit = GetNode<LineEdit>("CenterContainer/PopupPanel/MarginContainer/Content/CompanyNameRow/CompanyNameEdit");
        _shieldOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/ShieldRow/ShieldOptions");
        _shieldColorOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/ShieldColorRow/ShieldColorOptions");
        _logoOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/LogoRow/LogoOptions");
        _logoSizeOptions = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/LogoSizeRow/LogoSizeOptions");
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/ApplyButton").Pressed += OnApplyPressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CompanyNameRow/RandomNameButton").Pressed += OnRandomNamePressed;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/RandomizeButton").Pressed += OnRandomizePressed;
        _shieldOptions.ItemSelected += OnShieldSelected;
        _logoOptions.ItemSelected += OnLogoSelected;
        _shieldColorOptions.ItemSelected += OnShieldColorSelected;
        _logoSizeOptions.ItemSelected += OnLogoSizeSelected;
        _cancelButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CancelButton");
        _cancelButton.Pressed += QueueFree;

        ApplyConfigurationToUi();
    }

    public void Configure(CompanyLogoData logoData, bool canCancel, Action<CompanyLogoData> onApplied)
    {
        _sourceData = logoData ?? CompanyLogoData.CreateDefault();
        _workingData = _sourceData.CreateCopy();
        if (!canCancel)
            _workingData.RandomizeAll();

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

        _shieldColorOptions.Clear();
        for (var i = 0; i < _workingData.GetShieldColorCount(); i++)
            _shieldColorOptions.AddItem(_workingData.GetShieldColorName(i), i);

        _logoSizeOptions.Clear();
        for (var i = 0; i < _workingData.GetLogoSizeCount(); i++)
            _logoSizeOptions.AddItem(_workingData.GetLogoSizeName(i), i);

        _shieldOptions.Select(_workingData.ShieldIndex);
        _logoOptions.Select(_workingData.GetNormalizedLogoIndex());
        _shieldColorOptions.Select((int)_workingData.ShieldColor);
        _logoSizeOptions.Select((int)_workingData.LogoSize);
    }

    private void OnShieldSelected(long index)
    {
        _workingData.SetShieldIndex((int)index);
    }

    private void OnLogoSelected(long index)
    {
        _workingData.SetLogoIndex((int)index);
    }

    private void OnShieldColorSelected(long index)
    {
        _workingData.SetShieldColor((CompanyLogoData.CompanyShieldColor)index);
    }

    private void OnLogoSizeSelected(long index)
    {
        _workingData.SetLogoSize((CompanyLogoData.CompanyLogoSize)index);
    }

    private void OnApplyPressed()
    {
        _workingData.SetCompanyName(_companyNameEdit.Text);
        _sourceData.CopyFrom(_workingData);
        _onApplied?.Invoke(_sourceData);
        QueueFree();
    }

    private void OnRandomNamePressed()
    {
        _workingData.RandomizeName();
        _companyNameEdit.Text = _workingData.CompanyName;
    }

    private void OnRandomizePressed()
    {
        _workingData.RandomizeAll();
        ApplyConfigurationToUi();
    }
}
