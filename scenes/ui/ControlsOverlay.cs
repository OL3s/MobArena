using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class ControlsOverlay : Control
{
	private CheckBox _autoDetectCheckBox;
	private OptionButton _defaultInputOption;
	private HBoxContainer _connectedRow;
	private TextureRect _primaryJoinIcon;
	private Label _primaryJoinLabel;
	private TextureRect _leaveIcon;
	private Label _leaveLabel;

	public override void _Ready()
	{
		_autoDetectCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Content/InputSettings/AutoDetectCheckBox");
		_defaultInputOption = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/InputSettings/DefaultInputRow/DefaultInputOption");
		_connectedRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ConnectedRow");
		_primaryJoinIcon = GetNode<TextureRect>("CenterContainer/PopupPanel/MarginContainer/Content/PromptRow/JoinPrompt/Icon");
		_primaryJoinLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/PromptRow/JoinPrompt/Label");
		_leaveIcon = GetNode<TextureRect>("CenterContainer/PopupPanel/MarginContainer/Content/PromptRow/LeavePrompt/Icon");
		_leaveLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/PromptRow/LeavePrompt/Label");

		_defaultInputOption.AddItem("Keyboard", (int)SettingsConfig.PrimaryInputMode.Keyboard);
		_defaultInputOption.AddItem("Touch", (int)SettingsConfig.PrimaryInputMode.Touch);
		_defaultInputOption.AddItem("Gamepad", (int)SettingsConfig.PrimaryInputMode.Gamepad);
		_autoDetectCheckBox.Toggled += OnAutoDetectToggled;
		_defaultInputOption.ItemSelected += OnDefaultInputSelected;
		GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;
		RefreshUi();
	}

	private void RefreshUi()
	{
		var localInputConfig = LocalInputConfig.Get();
		localInputConfig?.RefreshConnectedControllerSetups();

		foreach (var child in _connectedRow.GetChildren())
		{
			_connectedRow.RemoveChild(child);
			child.QueueFree();
		}

		if (localInputConfig == null)
			return;

		var settingsConfig = localInputConfig.SettingsConfig;
		_autoDetectCheckBox.ButtonPressed = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputOption.Disabled = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputOption.Select(_defaultInputOption.GetItemIndex((int)settingsConfig.DefaultPrimaryInput));

		foreach (var controllerSetup in localInputConfig.ControllerSetups)
			AddDeviceChip(controllerSetup);

		_primaryJoinIcon.Texture = localInputConfig.JoinPromptIcon;
		_primaryJoinLabel.Text = localInputConfig.JoinPromptLabel;
		_leaveIcon.Texture = localInputConfig.LeavePromptIcon;
		_leaveLabel.Text = localInputConfig.LeavePromptLabel;
	}

	private void OnAutoDetectToggled(bool enabled)
	{
		var settingsConfig = LocalInputConfig.Get()?.SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.AutoDetectPrimaryInput = enabled;
		RefreshUi();
	}

	private void OnDefaultInputSelected(long itemIndex)
	{
		var settingsConfig = LocalInputConfig.Get()?.SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.DefaultPrimaryInput = (SettingsConfig.PrimaryInputMode)_defaultInputOption.GetItemId((int)itemIndex);
		RefreshUi();
	}

	private void AddDeviceChip(LocalInputControllerConfig controllerSetup)
	{
		var chip = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(132, 42)
		};
		chip.AddThemeConstantOverride("separation", 6);

		var textureRect = new TextureRect
		{
			CustomMinimumSize = new Vector2(32, 32),
			Texture = controllerSetup.Icon,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};

		var label = new Label
		{
			Text = controllerSetup.ControllerName,
			VerticalAlignment = VerticalAlignment.Center
		};

		chip.AddChild(textureRect);
		chip.AddChild(label);
		_connectedRow.AddChild(chip);
	}
}
