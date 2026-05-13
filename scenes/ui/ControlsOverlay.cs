using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class ControlsOverlay : Control
{
	private CheckBox _autoDetectCheckBox;
	private Label _defaultInputLabel;
	private OptionButton _defaultInputOption;
	private HBoxContainer _connectedRow;
	private Button _closeButton;
	private bool _refreshingUi;
	private static readonly StyleBoxFlat ConnectedChipStyle = CreateChipStyle(new Color(0.16f, 0.24f, 0.20f, 0.96f), new Color(0.30f, 0.72f, 0.42f, 0.9f));
	private static readonly StyleBoxFlat JoinChipStyle = CreateChipStyle(new Color(0.23f, 0.19f, 0.12f, 0.96f), new Color(0.93f, 0.70f, 0.27f, 0.9f));

	public override void _Ready()
	{
		_autoDetectCheckBox = GetNode<CheckBox>("CenterContainer/PopupPanel/MarginContainer/Content/InputSettings/AutoDetectCheckBox");
		_defaultInputLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/InputSettings/DefaultInputRow/DefaultInputLabel");
		_defaultInputOption = GetNode<OptionButton>("CenterContainer/PopupPanel/MarginContainer/Content/InputSettings/DefaultInputRow/DefaultInputOption");
		_connectedRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ConnectedRow");
		_closeButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton");

		_defaultInputOption.AddItem("Keyboard", (int)SettingsConfig.PrimaryInputMode.Keyboard);
		_defaultInputOption.AddItem("Touch", (int)SettingsConfig.PrimaryInputMode.Touch);
		_defaultInputOption.AddItem("Gamepad", (int)SettingsConfig.PrimaryInputMode.Gamepad);
		_autoDetectCheckBox.Toggled += OnAutoDetectToggled;
		_defaultInputOption.ItemSelected += OnDefaultInputSelected;
		_closeButton.Pressed += QueueFree;
		RefreshUi();
	}

	private void RefreshUi()
	{
		_refreshingUi = true;
		var localInputConfig = LocalInputConfig.Get();

		foreach (var child in _connectedRow.GetChildren())
		{
			_connectedRow.RemoveChild(child);
			child.Free();
		}

		if (localInputConfig == null)
		{
			_closeButton.Disabled = true;
			_refreshingUi = false;
			return;
		}

		var settingsConfig = SaveNode.Get()?.SettingsConfig ?? new SettingsConfig();
		_autoDetectCheckBox.ButtonPressed = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputOption.Disabled = settingsConfig.AutoDetectPrimaryInput;
		_defaultInputLabel.Modulate = _defaultInputOption.Disabled ? new Color(1, 1, 1, 0.45f) : Colors.White;
		_defaultInputOption.Select(_defaultInputOption.GetItemIndex((int)settingsConfig.DefaultPrimaryInput));
		_closeButton.Disabled = localInputConfig.ControllerSetups.Count == 0;

		for (var index = 0; index < localInputConfig.ControllerSetups.Count; index++)
			AddDeviceChip(localInputConfig.ControllerSetups[index], index + 1);

		if (localInputConfig.ControllerSetups.Count < LocalInputConfig.MaxLocalPlayers)
			AddJoinChip(localInputConfig.JoinPromptIcon);

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
		RefreshUi();
	}

	private void OnDefaultInputSelected(long itemIndex)
	{
		if (_refreshingUi)
			return;

		var settingsConfig = SaveNode.Get()?.SettingsConfig;
		if (settingsConfig == null)
			return;

		settingsConfig.DefaultPrimaryInput = (SettingsConfig.PrimaryInputMode)_defaultInputOption.GetItemId((int)itemIndex);
		RefreshUi();
	}

	private void AddDeviceChip(LocalInputControllerConfig controllerSetup, int slotNumber)
	{
		var localInputConfig = LocalInputConfig.Get();
		AddChip($"{slotNumber} Connected", controllerSetup.ControllerName, localInputConfig?.LeavePromptIcon, "To Leave", ConnectedChipStyle);
	}

	private void AddJoinChip(Texture2D icon)
	{
		AddChip("Press", string.Empty, icon, "To join", JoinChipStyle);
	}

	private void AddChip(string topText, string middleText, Texture2D icon, string bottomText, StyleBoxFlat style)
	{
		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(166, 206)
		};
		panel.AddThemeStyleboxOverride("panel", style);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);

		var chip = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(142, 182)
		};
		chip.AddThemeConstantOverride("separation", 8);

		var topLabel = new Label
		{
			Text = topText,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		var middleLabel = new Label
		{
			Text = middleText,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		var textureRect = new TextureRect
		{
			CustomMinimumSize = new Vector2(64, 64),
			Texture = icon,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};

		var bottomLabel = new Label
		{
			Text = bottomText,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		chip.AddChild(topLabel);
		if (!string.IsNullOrEmpty(middleText))
			chip.AddChild(middleLabel);
		chip.AddChild(textureRect);
		chip.AddChild(bottomLabel);
		margin.AddChild(chip);
		panel.AddChild(margin);
		_connectedRow.AddChild(panel);
	}

	private static StyleBoxFlat CreateChipStyle(Color backgroundColor, Color borderColor)
	{
		var style = new StyleBoxFlat
		{
			BgColor = backgroundColor,
			BorderColor = borderColor,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomRight = 12,
			CornerRadiusBottomLeft = 12
		};

		return style;
	}
}
