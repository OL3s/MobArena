using Godot;
using System;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class SettingsOverlay : Control
{
	private const int ControlsCategory = 0;
	private const int VideoCategory = 1;
	private const int SoundCategory = 2;
	private const int GameplayCategory = 3;
	private const int AccessibilityCategory = 4;

	private Button _controlsButton;
	private Button _videoButton;
	private Button _soundButton;
	private Button _gameplayButton;
	private Button _accessibilityButton;
	private Label _categoryTitle;
	private CheckBox _autoDetectCheckBox;
	private Label _defaultInputLabel;
	private OptionButton _defaultInputOption;
	private Control _controlsSettings;
	private Label _placeholderLabel;
	private bool _refreshingUi;

	public override void _Ready()
	{
		_controlsButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/ControlsButton");
		_videoButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/VideoButton");
		_soundButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/SoundButton");
		_gameplayButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/GameplayButton");
		_accessibilityButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/CategoryList/AccessibilityButton");
		_categoryTitle = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/CategoryTitle");
		_controlsSettings = GetNode<Control>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings");
		_placeholderLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/PlaceholderLabel");
		_autoDetectCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/AutoDetectCheckBox");
		_defaultInputLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/DefaultInputRow/DefaultInputLabel");
		_defaultInputOption = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Layout/Body/SettingsPanel/SettingsContent/ControlsSettings/DefaultInputRow/DefaultInputOption");

		_defaultInputOption.AddItem("Keyboard", (int)SettingsConfig.PrimaryInputMode.Keyboard);
		_defaultInputOption.AddItem("Touch", (int)SettingsConfig.PrimaryInputMode.Touch);
		_defaultInputOption.AddItem("Gamepad", (int)SettingsConfig.PrimaryInputMode.Gamepad);

		_controlsButton.Pressed += () => ShowCategory(ControlsCategory);
		_videoButton.Pressed += () => ShowCategory(VideoCategory);
		_soundButton.Pressed += () => ShowCategory(SoundCategory);
		_gameplayButton.Pressed += () => ShowCategory(GameplayCategory);
		_accessibilityButton.Pressed += () => ShowCategory(AccessibilityCategory);
		_autoDetectCheckBox.Toggled += OnAutoDetectToggled;
		_defaultInputOption.ItemSelected += OnDefaultInputSelected;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/ApplyButton").Pressed += OnApplyPressed;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Layout/Actions/CloseButton").Pressed += QueueFree;

		RefreshControlsSettings();
		ShowCategory(ControlsCategory);
	}

	private void ShowCategory(int category)
	{
		_controlsSettings.Visible = category == ControlsCategory;
		_placeholderLabel.Visible = category != ControlsCategory;

		_categoryTitle.Text = category switch
		{
			VideoCategory => "Video",
			SoundCategory => "Sound",
			GameplayCategory => "Gameplay",
			AccessibilityCategory => "Accessibility",
			_ => "Controls"
		};

		_placeholderLabel.Text = category switch
		{
			VideoCategory => "Video settings will live here: resolution, fullscreen, scale, and visual effects.",
			SoundCategory => "Sound settings will live here: master, music, UI, and effects volume.",
			GameplayCategory => "Gameplay settings will live here: difficulty, camera, and quality-of-life toggles.",
			AccessibilityCategory => "Accessibility settings will live here: readability, contrast, motion, and assistance options.",
			_ => string.Empty
		};

		_controlsButton.Disabled = category == ControlsCategory;
		_videoButton.Disabled = category == VideoCategory;
		_soundButton.Disabled = category == SoundCategory;
		_gameplayButton.Disabled = category == GameplayCategory;
		_accessibilityButton.Disabled = category == AccessibilityCategory;
	}

	private void RefreshControlsSettings()
	{
		_refreshingUi = true;
		var settingsConfig = SaveNode.Get()?.SettingsConfig ?? new SettingsConfig();
		_autoDetectCheckBox.ButtonPressed = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputOption.Disabled = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputLabel.Modulate = _defaultInputOption.Disabled ? new Color(1, 1, 1, 0.45f) : Colors.White;
		_defaultInputOption.Select(_defaultInputOption.GetItemIndex((int)settingsConfig.DefaultPrimaryInput));
		_refreshingUi = false;
	}

	private void OnAutoDetectToggled(bool enabled)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get()?.SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.AutoDetectPrimaryInput = enabled;
		RefreshControlsSettings();
	}

	private void OnDefaultInputSelected(long itemIndex)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get()?.SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.DefaultPrimaryInput = (SettingsConfig.PrimaryInputMode)_defaultInputOption.GetItemId((int)itemIndex);
		RefreshControlsSettings();
	}

	private static void OnApplyPressed()
	{
		try
		{
			SaveNode.Get()?.Save();
		}
		catch (NotImplementedException)
		{
			GlobalOverlay.Get()?.ShowBlurredPopup(
				"Settings",
				"Settings were applied for this runtime session. Disk save is not implemented yet.");
		}
	}
}
