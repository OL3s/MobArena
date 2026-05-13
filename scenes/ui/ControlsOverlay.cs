using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class ControlsOverlay : Control
{
	private HBoxContainer _connectedRow;
	private Button _closeButton;
	private bool _refreshingUi;
	private StyleBoxFlat _connectedChipStyle;
	private StyleBoxFlat _joinChipStyle;

	public override void _Ready()
	{
		_connectedRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ConnectedRow");
		_closeButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton");
		_connectedChipStyle = CreateChipStyle(new Color(0.16f, 0.24f, 0.20f, 0.96f), new Color(0.30f, 0.72f, 0.42f, 0.9f));
		_joinChipStyle = CreateChipStyle(new Color(0.23f, 0.19f, 0.12f, 0.96f), new Color(0.93f, 0.70f, 0.27f, 0.9f));

		_closeButton.Pressed += QueueFree;
		RefreshUi();
	}

	public override void _ExitTree()
	{
		_connectedChipStyle = null;
		_joinChipStyle = null;
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter or Key.KpEnter })
		{
			if (LocalInputConfig.Get()?.TryJoinKeyboard() == true)
			{
				GetViewport()?.SetInputAsHandled();
				RefreshUi();
			}

			return;
		}

		if (inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Backspace })
		{
			if (LocalInputConfig.Get()?.TryLeaveKeyboard() == true)
			{
				GetViewport()?.SetInputAsHandled();
				RefreshUi();
			}

			return;
		}

		if (inputEvent is InputEventScreenTouch { Pressed: true })
		{
			if (LocalInputConfig.Get()?.TryJoinTouch() == true)
			{
				GetViewport()?.SetInputAsHandled();
				RefreshUi();
			}

			return;
		}

		if (inputEvent is not InputEventJoypadButton { Pressed: true } joypadButton)
			return;

		var localInputConfig = LocalInputConfig.Get();
		if (localInputConfig == null)
			return;

		var changed = joypadButton.ButtonIndex switch
		{
			JoyButton.A => localInputConfig.TryJoinGamepad(joypadButton.Device),
			JoyButton.B => localInputConfig.TryLeaveGamepad(joypadButton.Device),
			_ => false
		};

		if (!changed)
			return;

		GetViewport()?.SetInputAsHandled();
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

		_closeButton.Disabled = localInputConfig.ControllerSetups.Count <= 0;

		for (var index = 0; index < localInputConfig.ControllerSetups.Count; index++)
			AddDeviceChip(localInputConfig.ControllerSetups[index], index + 1);

		if (localInputConfig.ControllerSetups.Count < LocalInputConfig.MaxLocalPlayers)
			AddJoinChip(localInputConfig);

		_refreshingUi = false;
	}

	private void AddDeviceChip(LocalInputControllerConfig controllerSetup, int slotNumber)
	{
		var localInputConfig = LocalInputConfig.Get();
		AddChip($"{slotNumber} Connected", controllerSetup.ControllerName, localInputConfig?.GetLeavePromptIcon(controllerSetup), "To Leave", _connectedChipStyle);
	}

	private void AddJoinChip(LocalInputConfig localInputConfig)
	{
		var icons = new Godot.Collections.Array<Texture2D>();
		if (!localInputConfig.HasKeyboardSetup())
			icons.Add(localInputConfig.KeyboardJoinPromptIcon);

		icons.Add(localInputConfig.JoinPromptIcon);
		var touchText = localInputConfig.HasTouchSetup() ? string.Empty : "or touch";
		AddChip("Press", string.Empty, icons, touchText, "to join", _joinChipStyle);
	}

	private void AddChip(string topText, string middleText, Texture2D icon, string bottomText, StyleBoxFlat style)
	{
		AddChip(topText, middleText, new Godot.Collections.Array<Texture2D> { icon }, string.Empty, bottomText, style);
	}

	private void AddChip(string topText, string middleText, Godot.Collections.Array<Texture2D> icons, string touchText, string bottomText, StyleBoxFlat style)
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

		var iconRow = new HBoxContainer();
		iconRow.AddThemeConstantOverride("separation", 8);
		iconRow.Alignment = BoxContainer.AlignmentMode.Center;

		foreach (var icon in icons)
		{
			var textureRect = new TextureRect
			{
				CustomMinimumSize = new Vector2(56, 56),
				Texture = icon,
				ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
			};
			iconRow.AddChild(textureRect);
		}

		var touchLabel = new Label
		{
			Text = touchText,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		touchLabel.AddThemeFontSizeOverride("font_size", 13);
		touchLabel.Modulate = new Color(1, 1, 1, 0.72f);

		var bottomLabel = new Label
		{
			Text = bottomText,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		chip.AddChild(topLabel);
		if (!string.IsNullOrEmpty(middleText))
			chip.AddChild(middleLabel);
		chip.AddChild(iconRow);
		if (!string.IsNullOrEmpty(touchText))
			chip.AddChild(touchLabel);
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
