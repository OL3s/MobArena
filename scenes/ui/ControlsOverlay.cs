using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class ControlsOverlay : Control
{
	private HBoxContainer _connectedRow;
	private Button _resetButton;
	private Button _closeButton;
	private PanelContainer _touchJoinChip;
	private PanelContainer _touchLeaveChip;
	private LocalInputControllerConfig _touchLeaveControllerSetup;
	private bool _refreshingUi;
	private StyleBoxFlat _connectedChipStyle;
	private StyleBoxFlat _joinChipStyle;

	public override void _Ready()
	{
		_connectedRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ConnectedRow");
		_resetButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/ResetButton");
		_closeButton = GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/Actions/CloseButton");
		_connectedChipStyle = CreateChipStyle(new Color(0.16f, 0.24f, 0.20f, 0.96f), new Color(0.30f, 0.72f, 0.42f, 0.9f));
		_joinChipStyle = CreateChipStyle(new Color(0.23f, 0.19f, 0.12f, 0.96f), new Color(0.93f, 0.70f, 0.27f, 0.9f));

		_resetButton.Pressed += OnResetPressed;
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
		if (inputEvent is InputEventScreenTouch { Pressed: true } screenTouch)
		{
			if (TryHandleChipTouch(screenTouch.Position))
				GetViewport()?.SetInputAsHandled();

			return;
		}

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
		_touchJoinChip = null;
		_touchLeaveChip = null;
		_touchLeaveControllerSetup = null;

		ClearChips();

		if (localInputConfig == null)
		{
			_resetButton.Disabled = true;
			_closeButton.Disabled = true;
			_refreshingUi = false;
			return;
		}

		_resetButton.Disabled = localInputConfig.ControllerSetups.Count <= 0;
		_closeButton.Disabled = false;

		for (var index = 0; index < localInputConfig.ControllerSetups.Count; index++)
			AddDeviceChip(localInputConfig.ControllerSetups[index], index + 1);

		if (localInputConfig.ControllerSetups.Count < LocalInputConfig.MaxLocalPlayers)
			AddJoinChip(localInputConfig);

		_refreshingUi = false;
	}

	private void ClearChips()
	{
		if (!GodotObject.IsInstanceValid(_connectedRow))
			return;

		foreach (var child in _connectedRow.GetChildren())
		{
			_connectedRow.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void OnResetPressed()
	{
		var localInputConfig = LocalInputConfig.Get();
		if (localInputConfig == null)
			return;

		localInputConfig.ClearControllerSetups();
		RefreshUi();
	}

	private void AddDeviceChip(LocalInputControllerConfig controllerSetup, int slotNumber)
	{
		var localInputConfig = LocalInputConfig.Get();
		if (controllerSetup.Kind == LocalInputControllerConfig.ControllerKind.Touch)
		{
			_touchLeaveChip = AddChip($"{slotNumber} Connected", controllerSetup.ControllerName, localInputConfig?.TouchJoinPromptIcon, "To Leave", _connectedChipStyle);
			_touchLeaveControllerSetup = controllerSetup;
			return;
		}

		AddChip($"{slotNumber} Connected", controllerSetup.ControllerName, localInputConfig?.GetLeavePromptIcon(controllerSetup), "To Leave", _connectedChipStyle);
	}

	private void AddJoinChip(LocalInputConfig localInputConfig)
	{
		var icons = new Godot.Collections.Array<Texture2D>();
		if (!localInputConfig.HasKeyboardSetup())
			icons.Add(localInputConfig.KeyboardJoinPromptIcon);

		icons.Add(localInputConfig.JoinPromptIcon);
		if (!localInputConfig.HasTouchSetup())
			icons.Add(localInputConfig.TouchJoinPromptIcon);

		_touchJoinChip = AddChip("Press", string.Empty, icons, string.Empty, "to join", _joinChipStyle);
	}

	private PanelContainer AddChip(string topText, string middleText, Texture2D icon, string bottomText, StyleBoxFlat style)
	{
		return AddChip(topText, middleText, new Godot.Collections.Array<Texture2D> { icon }, string.Empty, bottomText, style);
	}

	private PanelContainer AddChip(string topText, string middleText, Godot.Collections.Array<Texture2D> icons, string touchText, string bottomText, StyleBoxFlat style)
	{
		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(166, 206),
			MouseFilter = MouseFilterEnum.Stop
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

		var bottomLabel = new Label
		{
			Text = bottomText,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		chip.AddChild(topLabel);
		if (!string.IsNullOrEmpty(middleText))
		{
			var middleLabel = new Label
			{
				Text = middleText,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			chip.AddChild(middleLabel);
		}
		if (icons.Count > 0)
			chip.AddChild(iconRow);
		if (!string.IsNullOrEmpty(touchText))
		{
			var touchLabel = new Label
			{
				Text = touchText,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			touchLabel.AddThemeFontSizeOverride("font_size", 13);
			touchLabel.Modulate = new Color(1, 1, 1, 0.72f);
			chip.AddChild(touchLabel);
		}
		chip.AddChild(bottomLabel);
		margin.AddChild(chip);
		panel.AddChild(margin);
		_connectedRow.AddChild(panel);
		return panel;
	}

	private bool TryHandleChipTouch(Vector2 position)
	{
		if (_refreshingUi)
			return false;

		if (IsPointInside(_touchJoinChip, position) && TryJoinTouch())
		{
			CallDeferred(MethodName.RefreshUi);
			return true;
		}

		if (IsPointInside(_touchLeaveChip, position) && TryLeaveController(_touchLeaveControllerSetup))
		{
			CallDeferred(MethodName.RefreshUi);
			return true;
		}

		return false;
	}

	private bool TryJoinTouch()
	{
		return LocalInputConfig.Get()?.TryJoinTouch() == true;
	}

	private bool TryLeaveController(LocalInputControllerConfig controllerSetup)
	{
		if (controllerSetup == null)
			return false;

		var localInputConfig = LocalInputConfig.Get();
		if (localInputConfig == null)
			return false;

		return controllerSetup.Kind switch
		{
			LocalInputControllerConfig.ControllerKind.Keyboard => localInputConfig.TryLeaveKeyboard(),
			LocalInputControllerConfig.ControllerKind.Touch => localInputConfig.TryLeaveTouch(),
			LocalInputControllerConfig.ControllerKind.Gamepad => localInputConfig.TryLeaveGamepad(controllerSetup.DeviceId),
			_ => false
		};
	}

	private static bool IsPointInside(Control control, Vector2 position)
	{
		return GodotObject.IsInstanceValid(control)
			&& control.IsVisibleInTree()
			&& control.GetGlobalRect().HasPoint(position);
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
