using System;
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
	private const int SaveDataCategory = 3;

	private Button _videoButton;
	private Button _soundButton;
	private Button _gameplayButton;
	private Button _saveDataButton;
	private Label _categoryTitle;
	private Control _gameplaySettings;
	private Control _saveDataSettings;
	private CheckBox _debugCheckBox;
	private Label _lowHealthValueLabel;
	private SpinBox _lowHealthSpinBox;
	private Label _placeholderLabel;
	private bool _refreshingUi;

	public override void _Ready()
	{
		_videoButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/VideoButton");
		_soundButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SoundButton");
		_gameplayButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/GameplayButton");
		_saveDataButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SaveDataButton");
		_categoryTitle = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/CategoryTitle");
		_gameplaySettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings");
		_saveDataSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings");
		_placeholderLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/PlaceholderLabel");
		_debugCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/DebugCheckBox");
		_lowHealthValueLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/LowHealthRow/LowHealthValueLabel");
		_lowHealthSpinBox = GetNode<SpinBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/GameplaySettings/LowHealthRow/LowHealthSpinBox");

		_videoButton.Pressed += () => ShowCategory(VideoCategory);
		_soundButton.Pressed += () => ShowCategory(SoundCategory);
		_gameplayButton.Pressed += () => ShowCategory(GameplayCategory);
		_saveDataButton.Pressed += () => ShowCategory(SaveDataCategory);
		_debugCheckBox.Toggled += OnDebugToggled;
		_lowHealthSpinBox.ValueChanged += OnLowHealthWarningChanged;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteRunButton").Pressed += OnDeleteRunDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteCompanyButton").Pressed += OnDeleteCompanyDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteSettingsButton").Pressed += OnDeleteSettingsDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/SaveDataSettings/DeleteAllButton").Pressed += OnDeleteAllSaveDataPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/ApplyButton").Pressed += OnApplyPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/CloseButton").Pressed += QueueFree;

		RefreshSettingsUi();
		ShowCategory(GameplayCategory);
	}

	private void ShowCategory(int category)
	{
		_gameplaySettings.Visible = category == GameplayCategory;
		_saveDataSettings.Visible = category == SaveDataCategory;
		_placeholderLabel.Visible = category != GameplayCategory && category != SaveDataCategory;

		_categoryTitle.Text = category switch
		{
			VideoCategory => "Video",
			SoundCategory => "Sound",
			GameplayCategory => "Gameplay",
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
		_saveDataButton.Disabled = category == SaveDataCategory;
	}

	private void RefreshSettingsUi()
	{
		_refreshingUi = true;
		var settingsConfig = SaveNode.Get().SettingsConfig;
		_debugCheckBox.ButtonPressed = settingsConfig.DebugEnabled;
		_lowHealthSpinBox.Value = Mathf.RoundToInt(settingsConfig.LowHealthWarningRatio * 100f);
		_lowHealthValueLabel.Text = $"{_lowHealthSpinBox.Value:0}%";
		_refreshingUi = false;
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
