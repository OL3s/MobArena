using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.UI;

namespace MobArena.Scripts;

public partial class RosterHall : Node
{
    private const string TownScene = "res://scenes/town.tscn";
    private const string GladiatorsOverlayScenePath = "res://scenes/ui/GladiatorsOverlay.tscn";
    private static readonly PackedScene GladiatorsOverlayScene = ResourceLoader.Load<PackedScene>(GladiatorsOverlayScenePath);

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
        if (globalOverlay == null || GladiatorsOverlayScene == null)
            return;

        globalOverlay.AddOverlay(GladiatorsOverlayScene.Instantiate<GladiatorsOverlay>());
    }
}
