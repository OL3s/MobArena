using Godot;
using MobArena.Scenes.Components.UI;

namespace MobArena.Scripts;

public partial class RosterHall : Node
{
    private const string TownScene = "res://scenes/town.tscn";

    public override void _Ready()
    {
        GetNode<TownHud>("TownHud").BackPressed += OnBackPressed;
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile(TownScene);
    }
}
