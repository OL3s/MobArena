using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class SettingsOverlay : Control
{
	private const string MainMenuScene = "res://scenes/main_menu.tscn";
	private const int VideoCategory = 0;
	private const int SoundCategory = 1;
	private const int GameplayCategory = 2;
	private const int ControlsCategory = 3;
	private const int DevCategory = 4;
	private const int SaveDataCategory = 5;

	private Button _videoButton;
	private Button _soundButton;
	private Button _gameplayButton;
	private Button _controlsButton;
	private Button _devButton;
	private Button _saveDataButton;
	private Label _categoryTitle;
	private Control _gameplaySettings;
	private Control _controlsSettings;
	private Control _devSettings;
	private Control _saveDataSettings;
	private CheckBox _devModeCheckBox;
	private CheckBox _isDemoCheckBox;
	private CheckBox _showRuntimeTagsCheckBox;
	private CheckBox _skipTutorialCheckBox;
	private Label _lowHealthValueLabel;
	private SpinBox _lowHealthSpinBox;
	private Label _arenaMoveDeadzoneValueLabel;
	private SpinBox _arenaMoveDeadzoneSpinBox;
	private Label _placeholderLabel;
	private bool _refreshingUi;

	public override void _Ready()
	{
		_videoButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/VideoButton");
		_soundButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SoundButton");
		_gameplayButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/GameplayButton");
		_controlsButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/ControlsButton");
		_devButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/DevButton");
		_saveDataButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SaveDataButton");
		_categoryTitle = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/CategoryTitle");
		_gameplaySettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings");
		_controlsSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings");
		_devSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/DevSettings");
		_saveDataSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings");
		_placeholderLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/PlaceholderLabel");
		_devModeCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/DevSettings/DevModeCheckBox");
		_isDemoCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/DevSettings/IsDemoCheckBox");
		_showRuntimeTagsCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/DevSettings/ShowRuntimeTagsCheckBox");
		_skipTutorialCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/SkipTutorialCheckBox");
		_lowHealthValueLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/LowHealthRow/LowHealthValueLabel");
		_lowHealthSpinBox = GetNode<SpinBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/LowHealthRow/LowHealthSpinBox");
		_arenaMoveDeadzoneValueLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/ArenaMoveDeadzoneRow/ArenaMoveDeadzoneValueLabel");
		_arenaMoveDeadzoneSpinBox = GetNode<SpinBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/ArenaMoveDeadzoneRow/ArenaMoveDeadzoneSpinBox");

		_videoButton.Pressed += () => ShowCategory(VideoCategory);
		_soundButton.Pressed += () => ShowCategory(SoundCategory);
		_gameplayButton.Pressed += () => ShowCategory(GameplayCategory);
		_controlsButton.Pressed += () => ShowCategory(ControlsCategory);
		_devButton.Pressed += () => ShowCategory(DevCategory);
		_saveDataButton.Pressed += () => ShowCategory(SaveDataCategory);
		_devModeCheckBox.Toggled += OnDevModeToggled;
		_isDemoCheckBox.Toggled += OnIsDemoToggled;
		_showRuntimeTagsCheckBox.Toggled += OnShowRuntimeTagsToggled;
		_skipTutorialCheckBox.Toggled += OnSkipTutorialToggled;
		_lowHealthSpinBox.ValueChanged += OnLowHealthWarningChanged;
		_arenaMoveDeadzoneSpinBox.ValueChanged += OnArenaMoveDeadzoneChanged;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/RetireCompanyButton").Pressed += OnRetireCompanyPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteRecordsButton").Pressed += OnDeleteRecordsPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/ResetSettingsButton").Pressed += OnResetSettingsPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteAllButton").Pressed += OnDeleteAllSaveDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/ApplyButton").Pressed += OnApplyPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/CloseButton").Pressed += QueueFree;

		RefreshSettingsUi();
		ShowCategory(GameplayCategory);
	}

	private void ShowCategory(int category)
	{
		_gameplaySettings.Visible = category == GameplayCategory;
		_controlsSettings.Visible = category == ControlsCategory;
		_devSettings.Visible = category == DevCategory;
		_saveDataSettings.Visible = category == SaveDataCategory;
		_placeholderLabel.Visible = category != GameplayCategory && category != ControlsCategory && category != DevCategory && category != SaveDataCategory;

		_categoryTitle.Text = category switch
		{
			VideoCategory => "Video",
			SoundCategory => "Sound",
			GameplayCategory => "Gameplay",
			ControlsCategory => "Controls",
			DevCategory => "Dev",
			SaveDataCategory => "Save Data",
			_ => "Video"
		};

		_placeholderLabel.Text = category switch
		{
			VideoCategory => "Video settings will live here: resolution, fullscreen, scale, and visual effects.",
			SoundCategory => "Sound settings will live here: master, music, UI, and effects volume.",
			_ => string.Empty
		};

		_videoButton.Disabled = category == VideoCategory;
		_soundButton.Disabled = category == SoundCategory;
		_gameplayButton.Disabled = category == GameplayCategory;
		_controlsButton.Disabled = category == ControlsCategory;
		_devButton.Disabled = category == DevCategory;
		_saveDataButton.Disabled = category == SaveDataCategory;
	}

	private void RefreshSettingsUi()
	{
		_refreshingUi = true;
		var settingsConfig = SaveNode.Get().SettingsConfig;
		_devModeCheckBox.ButtonPressed = settingsConfig.DevEnabled;
		_isDemoCheckBox.ButtonPressed = settingsConfig.IsDemo;
		_showRuntimeTagsCheckBox.ButtonPressed = settingsConfig.ShowRuntimeTags;
		_skipTutorialCheckBox.ButtonPressed = settingsConfig.SkipTutorial;
		_lowHealthSpinBox.Value = Mathf.RoundToInt(settingsConfig.LowHealthWarningRatio * 100f);
		_lowHealthValueLabel.Text = $"{_lowHealthSpinBox.Value:0}%";
		_arenaMoveDeadzoneSpinBox.Value = Mathf.RoundToInt(settingsConfig.ArenaMoveDeadzone * 100f);
		_arenaMoveDeadzoneValueLabel.Text = $"{_arenaMoveDeadzoneSpinBox.Value:0}%";
		_refreshingUi = false;
	}

	private void OnDevModeToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		SaveNode.Get().SetDevEnabled(enabled);
		RefreshSettingsUi();
	}

	private void OnIsDemoToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		SaveNode.Get().SetIsDemo(enabled);
		RefreshSettingsUi();
	}

	private void OnShowRuntimeTagsToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		SaveNode.Get().SetShowRuntimeTags(enabled);
		RefreshSettingsUi();
	}

	private void OnSkipTutorialToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		SaveNode.Get().SetSkipTutorial(enabled);
		RefreshSettingsUi();
	}

	private void OnLowHealthWarningChanged(double value)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.LowHealthWarningRatio = Mathf.Clamp((float)value / 100f, 0.1f, 1f);
		RefreshSettingsUi();
	}

	private void OnArenaMoveDeadzoneChanged(double value)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.ArenaMoveDeadzone = Mathf.Clamp((float)value / 100f, 0f, 0.95f);
		RefreshSettingsUi();
	}

	private static void OnApplyPressed()
	{
		var error = SaveNode.Get().Save();
		if (error == Error.Ok)
		{
			GlobalOverlay.Get()?.ShowBlurredPopup(
				"Settings",
				"Settings were saved.");
			return;
		}

		GlobalOverlay.Get()?.ShowBlurredPopup(
			"Settings",
			$"Settings could not be saved. Error: {error}.");
	}

	private void OnRetireCompanyPressed()
	{
		ConfirmSaveDataAction(
			SaveNode.SaveDataDeleteScope.RetireCompany,
			"Retire Company?",
			"This ends the current company, saves any qualifying result to Records, and returns to the main menu.",
			"Retire");
	}

	private void OnDeleteRecordsPressed()
	{
		ConfirmSaveDataAction(
			SaveNode.SaveDataDeleteScope.Records,
			"Delete Records?",
			"This permanently deletes completed company records. The active company is not changed.",
			"Delete");
	}

	private void OnResetSettingsPressed()
	{
		ConfirmSaveDataAction(
			SaveNode.SaveDataDeleteScope.Settings,
			"Reset Settings?",
			"This resets gameplay/settings values to defaults. Company and records are not changed.",
			"Reset");
	}

	private void OnDeleteAllSaveDataPressed()
	{
		ConfirmSaveDataAction(
			SaveNode.SaveDataDeleteScope.All,
			"Delete Everything?",
			"This permanently deletes company, records, settings, and all save data.",
			"Delete All");
	}

	private void ConfirmSaveDataAction(SaveNode.SaveDataDeleteScope scope, string title, string text, string confirmText)
	{
		GlobalOverlay.Get()?.ShowGoCancelPopup(
			title,
			text,
			() =>
			{
				DeleteSaveData(scope);
			},
			confirmText);
	}

	private void DeleteSaveData(SaveNode.SaveDataDeleteScope scope)
	{
		var saveNode = SaveNode.Get();

		var error = saveNode.DeleteSaveData(scope);
		if (error != Error.Ok)
		{
			GlobalOverlay.Get()?.ShowBlurredPopup("Save Data", $"Save data could not be deleted. Error: {error}.");
			return;
		}

		GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
		SceneTransitionLogger.LogChange(GetTree(), MainMenuScene, $"delete save data: {scope}");
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
	}
}
