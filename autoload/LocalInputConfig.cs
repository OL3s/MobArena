using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class LocalInputConfig : Node
{
	public const int MaxLocalPlayers = 4;

	[Export]
	public Texture2D XboxAIcon { get; private set; }

	[Export]
	public Texture2D MouseIcon { get; private set; }

	[Export]
	public Texture2D EnterIcon { get; private set; }

	[Export]
	public Texture2D PhoneIcon { get; private set; }

	[Export]
	public Array<LocalInputControllerConfig> ControllerSetups { get; private set; } = new();

	public bool HasKeyboardPlayer => HasControllerKind(LocalInputControllerConfig.ControllerKind.Keyboard);
	public bool HasTouchPlayer => HasControllerKind(LocalInputControllerConfig.ControllerKind.Touch);
	public bool CanJoin => ControllerSetups.Count < MaxLocalPlayers;

	public static LocalInputConfig Get()
	{
		var sceneTree = Engine.GetMainLoop() as SceneTree;
		return sceneTree?.Root?.GetNodeOrNull<LocalInputConfig>("/root/LocalInputConfig");
	}

	public override void _ExitTree()
	{
		ControllerSetups.Clear();
	}

	public void ClearControllerSetups()
	{
		ControllerSetups.Clear();
	}

	public bool TryJoinGamepad(int deviceId)
	{
		if (!CanJoin || HasGamepadPlayer(deviceId))
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create(
			$"Gamepad {ControllerSetups.Count + 1}",
			LocalInputControllerConfig.ControllerKind.Gamepad,
			deviceId,
			XboxAIcon));
		return true;
	}

	public bool TryJoinKeyboard()
	{
		if (!CanJoin || HasKeyboardPlayer)
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create("Keyboard", LocalInputControllerConfig.ControllerKind.Keyboard, -1, MouseIcon));
		return true;
	}

	public bool TryJoinTouch()
	{
		if (!CanJoin || HasTouchPlayer)
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create("Touch", LocalInputControllerConfig.ControllerKind.Touch, -1, PhoneIcon));
		return true;
	}

	public bool HasGamepadPlayer()
	{
		return HasControllerKind(LocalInputControllerConfig.ControllerKind.Gamepad);
	}

	public bool HasGamepadPlayer(int deviceId)
	{
		foreach (var controllerSetup in ControllerSetups)
		{
			if (controllerSetup.Kind == LocalInputControllerConfig.ControllerKind.Gamepad && controllerSetup.DeviceId == deviceId)
				return true;
		}

		return false;
	}

	private bool HasControllerKind(LocalInputControllerConfig.ControllerKind kind)
	{
		foreach (var controllerSetup in ControllerSetups)
		{
			if (controllerSetup.Kind == kind)
				return true;
		}

		return false;
	}

}
