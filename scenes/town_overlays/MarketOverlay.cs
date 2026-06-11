using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class MarketOverlay : Control
{
    private const string GladiatorMarketOverlayScene = "res://scenes/town_overlays/gladiator_market_overlay.tscn";
    private const string ItemsOverlayScene = "res://scenes/ui/BlacksmithStoreOverlay.tscn";

    public override void _Ready()
    {
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/GladiatorsButton").Pressed += OnGladiatorsPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/BlacksmithButton").Pressed += OnItemsPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton").Pressed += QueueFree;
    }

    private void OnGladiatorsPressed()
    {
        OpenOverlay(GladiatorMarketOverlayScene);
    }

    private void OnItemsPressed()
    {
        OpenOverlay(ItemsOverlayScene);
    }

    private void OpenOverlay(string scenePath)
    {
        var globalOverlay = GlobalOverlay.Get();
        var overlayScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (globalOverlay == null || overlayScene == null)
            return;

        QueueFree();
        globalOverlay.AddOverlay(overlayScene);
    }
}
