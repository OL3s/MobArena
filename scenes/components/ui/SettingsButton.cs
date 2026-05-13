using Godot;
using MobArena.Scenes.UI;
using MobArena.Scripts;

namespace MobArena.Scenes.Components.UI;

public partial class SettingsButton : Button
{
	private const string SettingsOverlayScenePath = "res://scenes/ui/SettingsOverlay.tscn";

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private static void OnPressed()
	{
		var settingsOverlayScene = ResourceLoader.Load<PackedScene>(SettingsOverlayScenePath);
		if (settingsOverlayScene == null)
			return;

		GlobalOverlay.Get()?.AddOverlay(settingsOverlayScene.Instantiate<SettingsOverlay>());
	}
}
