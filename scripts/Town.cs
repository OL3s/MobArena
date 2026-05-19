using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.Components.Town;

namespace MobArena.Scripts;

public partial class Town : Node
{
    private const string MainMenuScene = "res://scenes/main_menu.tscn";
    private const string GladiatorsOverlayScene = "res://scenes/ui/GladiatorsOverlay.tscn";
    private const string EquipmentInventoryOverlayScene = "res://scenes/ui/EquipmentInventoryOverlay.tscn";
    private const string RationsManagementOverlayScene = "res://scenes/ui/RationsManagementOverlay.tscn";

    private TownBuilding _contractBoard;

    public override void _Ready()
    {
        _contractBoard = GetNode<TownBuilding>("World/ContractBoard");
        GetNode<TownHud>("TownHud").BackPressed += OnMainMenuPressed;
        GetNode<Button>("World/RosterYard/GladiatorsButton").Pressed += OnGladiatorsPressed;
        GetNode<Button>("World/RosterYard/RationsButton").Pressed += OnRationsPressed;
        GetNode<Button>("World/RosterYard/EquipmentButton").Pressed += OnEquipmentPressed;
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

    private static void OnGladiatorsPressed()
    {
        OpenOverlay(GladiatorsOverlayScene);
    }

    private static void OnEquipmentPressed()
    {
        OpenOverlay(EquipmentInventoryOverlayScene);
    }

    private static void OnRationsPressed()
    {
        OpenOverlay(RationsManagementOverlayScene);
    }

    private static void OpenOverlay(string scenePath)
    {
        var globalOverlay = GlobalOverlay.Get();
        var overlayScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (globalOverlay == null || overlayScene == null)
            return;

        globalOverlay.AddOverlay(overlayScene);
    }
}
