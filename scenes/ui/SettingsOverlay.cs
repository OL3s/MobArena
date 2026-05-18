using System;
using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class SettingsOverlay : Control
{
	private const string MainMenuScene = "res://scenes/main_menu.tscn";
	private const int ControlsCategory = 0;
	private const int VideoCategory = 1;
	private const int SoundCategory = 2;
	private const int GameplayCategory = 3;
	private const int SaveDataCategory = 4;

	private Button _controlsButton;
	private Button _videoButton;
	private Button _soundButton;
	private Button _gameplayButton;
	private Button _saveDataButton;
	private Label _categoryTitle;
	private CheckBox _autoDetectCheckBox;
	private Label _defaultInputLabel;
	private OptionButton _defaultInputOption;
	private Control _controlsSettings;
	private Control _gameplaySettings;
	private Control _saveDataSettings;
	private CheckBox _debugCheckBox;
	private Label _placeholderLabel;
	private bool _refreshingUi;

	public override void _Ready()
	{
		_controlsButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/ControlsButton");
		_videoButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/VideoButton");
		_soundButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SoundButton");
		_gameplayButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/GameplayButton");
		_saveDataButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SaveDataButton");
		_categoryTitle = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/CategoryTitle");
		_controlsSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings");
		_gameplaySettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings");
		_saveDataSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings");
		_placeholderLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/PlaceholderLabel");
		_autoDetectCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/AutoDetectCheckBox");
		_defaultInputLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/DefaultInputRow/DefaultInputLabel");
		_defaultInputOption = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/DefaultInputRow/DefaultInputOption");
		_debugCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/DebugCheckBox");

		_defaultInputOption.AddItem("None", (int)SettingsConfig.PrimaryInputMode.None);
		_defaultInputOption.AddItem("Keyboard", (int)SettingsConfig.PrimaryInputMode.Keyboard);
		_defaultInputOption.AddItem("Touch", (int)SettingsConfig.PrimaryInputMode.Touch);
		_defaultInputOption.AddItem("Gamepad", (int)SettingsConfig.PrimaryInputMode.Gamepad);

		_controlsButton.Pressed += () => ShowCategory(ControlsCategory);
		_videoButton.Pressed += () => ShowCategory(VideoCategory);
		_soundButton.Pressed += () => ShowCategory(SoundCategory);
		_gameplayButton.Pressed += () => ShowCategory(GameplayCategory);
		_saveDataButton.Pressed += () => ShowCategory(SaveDataCategory);
		_autoDetectCheckBox.Toggled += OnAutoDetectToggled;
		_defaultInputOption.ItemSelected += OnDefaultInputSelected;
		_debugCheckBox.Toggled += OnDebugToggled;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteRunButton").Pressed += OnDeleteRunDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteCompanyButton").Pressed += OnDeleteCompanyDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteSettingsButton").Pressed += OnDeleteSettingsDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteAllButton").Pressed += OnDeleteAllSaveDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/ApplyButton").Pressed += OnApplyPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/CloseButton").Pressed += QueueFree;

		RefreshSettingsUi();
		ShowCategory(ControlsCategory);
	}

	private void ShowCategory(int category)
	{
		_controlsSettings.Visible = category == ControlsCategory;
		_gameplaySettings.Visible = category == GameplayCategory;
		_saveDataSettings.Visible = category == SaveDataCategory;
		_placeholderLabel.Visible = category != ControlsCategory && category != GameplayCategory && category != SaveDataCategory;

		_categoryTitle.Text = category switch
		{
			VideoCategory => "Video",
			SoundCategory => "Sound",
			GameplayCategory => "Gameplay",
			SaveDataCategory => "Save Data",
			_ => "Controls"
		};

		_placeholderLabel.Text = category switch
		{
			VideoCategory => "Video settings will live here: resolution, fullscreen, scale, and visual effects.",
			SoundCategory => "Sound settings will live here: master, music, UI, and effects volume.",
			_ => string.Empty
		};

		_controlsButton.Disabled = category == ControlsCategory;
		_videoButton.Disabled = category == VideoCategory;
		_soundButton.Disabled = category == SoundCategory;
		_gameplayButton.Disabled = category == GameplayCategory;
		_saveDataButton.Disabled = category == SaveDataCategory;
	}

	private void RefreshSettingsUi()
	{
		_refreshingUi = true;
		var settingsConfig = SaveNode.Get().SettingsConfig;
		_autoDetectCheckBox.ButtonPressed = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputOption.Disabled = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputLabel.Modulate = _defaultInputOption.Disabled ? new Color(1, 1, 1, 0.45f) : Colors.White;
		_defaultInputOption.Select(_defaultInputOption.GetItemIndex((int)settingsConfig.DefaultPrimaryInput));
		_debugCheckBox.ButtonPressed = settingsConfig.DebugEnabled;
		_refreshingUi = false;
	}

	private void OnAutoDetectToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.AutoDetectPrimaryInput = enabled;
		RefreshSettingsUi();
	}

	private void OnDefaultInputSelected(long itemIndex)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.DefaultPrimaryInput = (SettingsConfig.PrimaryInputMode)_defaultInputOption.GetItemId((int)itemIndex);
		RefreshSettingsUi();
	}

	private void OnDebugToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get().SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.DebugEnabled = enabled;
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

	private void OnDeleteRunDataPressed()
	{
		ConfirmDeleteSaveData(saveNode => saveNode.DeleteRunData());
	}

	private void OnDeleteCompanyDataPressed()
	{
		ConfirmDeleteSaveData(saveNode => saveNode.DeleteCompanyData());
	}

	private void OnDeleteSettingsDataPressed()
	{
		ConfirmDeleteSaveData(saveNode => saveNode.DeleteSettingsData());
	}

	private void OnDeleteAllSaveDataPressed()
	{
		ConfirmDeleteSaveData(saveNode => saveNode.DeleteSave());
	}

	private void ConfirmDeleteSaveData(Func<SaveNode, Error> deleteAction)
	{
		GlobalOverlay.Get()?.ShowGoCancelPopup(
			"Delete Save Data?",
			"This cannot be undone.",
			() =>
			{
				DeleteSaveData(deleteAction);
			},
			"Delete");
	}

	private void DeleteSaveData(Func<SaveNode, Error> deleteAction)
	{
		var saveNode = SaveNode.Get();

		var error = deleteAction(saveNode);
		if (error != Error.Ok)
		{
			GlobalOverlay.Get()?.ShowBlurredPopup("Save Data", $"Save data could not be deleted. Error: {error}.");
			return;
		}

		GetTree().ChangeSceneToFile(MainMenuScene);
	}
}
