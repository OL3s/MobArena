using Godot;
using MobArena.Scripts;

namespace MobArena.Scenes.Components.UI;

public partial class SettingsButton : Button
{
	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private static void OnPressed()
	{
		GlobalOverlay.Get()?.ShowBlurredPopup(
			"Settings",
			"Settings are not implemented yet. This button is wired so it gives clear feedback instead of doing nothing.");
	}
}
