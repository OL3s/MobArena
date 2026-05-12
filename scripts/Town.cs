using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.Components.Town;

namespace MobArena.Scripts;

public partial class Town : Node
{
    private const string MainMenuScene = "res://scenes/main_menu.tscn";

    private TownBuilding _contractBoard;

    public override void _Ready()
    {
        _contractBoard = GetNode<TownBuilding>("World/ContractBoard");
        GetNode<TownHud>("TownHud").BackPressed += OnMainMenuPressed;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed("ui_accept"))
            return;

        GetViewport()?.SetInputAsHandled();
        _contractBoard?.Activate();
    }

    private void OnMainMenuPressed()
    {
        GetTree().ChangeSceneToFile(MainMenuScene);
    }
}
