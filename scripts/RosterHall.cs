using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.UI;

namespace MobArena.Scripts;

public partial class RosterHall : Node
{
	private const string TownScene = "res://scenes/town.tscn";
	private const string GladiatorsOverlayScenePath = "res://scenes/ui/GladiatorsOverlay.tscn";

    public override void _Ready()
    {
        GetNode<TownHud>("TownHud").BackPressed += OnBackPressed;
        GetNode<Button>("ControllerUi/LeftActions/GladiatorsButton").Pressed += OnGladiatorsPressed;
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile(TownScene);
    }

	private static void OnGladiatorsPressed()
	{
		var globalOverlay = GlobalOverlay.Get();
		var gladiatorsOverlayScene = ResourceLoader.Load<PackedScene>(GladiatorsOverlayScenePath);
		if (globalOverlay == null || gladiatorsOverlayScene == null)
			return;

		globalOverlay.AddOverlay(gladiatorsOverlayScene.Instantiate<GladiatorsOverlay>());
	}
}
