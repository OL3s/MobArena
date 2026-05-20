using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class Arena : Node
{
    private const string TownScene = "res://scenes/town.tscn";

    public override void _Ready()
    {
        var returnButton = GetNode<Button>("ControllerUi/StatusPanel/Row/ReturnButton");
        returnButton.Pressed += OnReturnToTownPressed;

        returnButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void OnReturnToTownPressed()
    {
        var saveNode = SaveNode.Get();
        PhaseTransitionController.CompleteArenaContract(saveNode.TownPhaseState, saveNode.CompanyRunData);
        saveNode.Save();
        GetTree().ChangeSceneToFile(TownScene);
    }
}
