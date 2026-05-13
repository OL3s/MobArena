using Godot;
using MobArena.Scenes.UI;
using MobArena.Scripts;

namespace MobArena.Scenes.Components.UI;

public partial class SettingsButton : Button
{
	private const string SettingsOverlayScenePath = "res://scenes/ui/SettingsOverlay.tscn";
	private static readonly PackedScene SettingsOverlayScene = ResourceLoader.Load<PackedScene>(SettingsOverlayScenePath);

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private static void OnPressed()
	{
		if (SettingsOverlayScene == null)
			return;

		GlobalOverlay.Get()?.AddOverlay(SettingsOverlayScene.Instantiate<SettingsOverlay>());
	}
}
