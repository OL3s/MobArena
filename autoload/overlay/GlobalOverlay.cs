using Godot;
using System;

namespace MobArena.Scripts;

public partial class GlobalOverlay : CanvasLayer
{
	private const string InfoPopupPanelScenePath = "res://scenes/components/panels/InfoPopupPanel.tscn";
	private const string GoCancelPopupPanelScenePath = "res://scenes/components/panels/GoCancelPopupPanel.tscn";

    private InfoPopupPanel _activeInfoPopup;
    private GoCancelPopupPanel _activeGoCancelPopup;
    private Action _activeGoAction;

    public static GlobalOverlay Get()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        return sceneTree?.Root?.GetNodeOrNull<GlobalOverlay>("/root/GlobalOverlay");
    }

    public void AddOverlay(PackedScene overlayScene)
    {
        if (overlayScene == null)
        {
            GD.PushError("Overlay scene is null, cannot add.");
            return;
        }

        AddOverlay(overlayScene.Instantiate<Control>());
    }

    public void AddOverlay(Control overlay)
    {
        if (overlay == null)
        {
            GD.PushError("Overlay is null, cannot add.");
            return;
        }

        ClosePopups();

        AddChild(overlay);
        CallDeferred(MethodName.GrabFirstOverlayFocus, overlay);
    }

    private void GrabFirstOverlayFocus(Control overlay)
    {
        var focusTarget = FindFirstFocusableControl(overlay);
        focusTarget?.GrabFocus();
    }

    private static Control FindFirstFocusableControl(Control root)
    {
        if (root == null)
            return null;

        if (IsFocusable(root))
            return root;

        foreach (var child in root.GetChildren())
        {
            if (child is not Control controlChild)
                continue;

            var focusTarget = FindFirstFocusableControl(controlChild);
            if (focusTarget != null)
                return focusTarget;
        }

        return null;
    }

    private static bool IsFocusable(Control control)
    {
        if (control is BaseButton button && button.Disabled)
            return false;

        return control.FocusMode == Control.FocusModeEnum.All
            && control.IsVisibleInTree();
    }

	public void ShowBlurredPopup(string title, string richText, Texture2D image = null)
	{
		var infoPopupPanelScene = ResourceLoader.Load<PackedScene>(InfoPopupPanelScenePath);
		if (infoPopupPanelScene == null)
			return;

        ClosePopups();

		_activeInfoPopup = infoPopupPanelScene.Instantiate<InfoPopupPanel>();
        AddChild(_activeInfoPopup);
        _activeInfoPopup.ShowContent(title, richText, image);
        _activeInfoPopup.Closed += OnInfoPopupClosed;
    }

	public void ShowGoCancelPopup(string title, string richText, Action goAction, string goText = "Go", string cancelText = "Cancel")
	{
		var goCancelPopupPanelScene = ResourceLoader.Load<PackedScene>(GoCancelPopupPanelScenePath);
		if (goCancelPopupPanelScene == null)
			return;

        ClosePopups();

        _activeGoAction = goAction;
		_activeGoCancelPopup = goCancelPopupPanelScene.Instantiate<GoCancelPopupPanel>();
        AddChild(_activeGoCancelPopup);
        _activeGoCancelPopup.ShowContent(title, richText, goText, cancelText);
        _activeGoCancelPopup.GoSelected += OnGoCancelPopupGoSelected;
        _activeGoCancelPopup.Cancelled += OnGoCancelPopupCancelled;
    }

    public void CloseTopOverlay()
    {
        if (GodotObject.IsInstanceValid(_activeGoCancelPopup))
        {
            CloseGoCancelPopup();
            return;
        }

        if (GodotObject.IsInstanceValid(_activeInfoPopup))
        {
            CloseInfoPopup();
            return;
        }

        var childCount = GetChildCount();
        if (childCount > 0)
            GetChild(childCount - 1).QueueFree();
    }

    public void CloseAllOverlays()
    {
        ClosePopups();

        foreach (Node child in GetChildren())
            child.QueueFree();
    }

    private void OnInfoPopupClosed()
    {
        CloseInfoPopup();
    }

    private void OnGoCancelPopupGoSelected()
    {
        var goAction = _activeGoAction;
        CloseGoCancelPopup();
        goAction?.Invoke();
    }

    private void OnGoCancelPopupCancelled()
    {
        CloseGoCancelPopup();
    }

    private void ClosePopups()
    {
        CloseInfoPopup();
        CloseGoCancelPopup();
    }

    private void CloseInfoPopup()
    {
        if (GodotObject.IsInstanceValid(_activeInfoPopup))
        {
            _activeInfoPopup.Closed -= OnInfoPopupClosed;
            _activeInfoPopup.QueueFree();
            _activeInfoPopup = null;
        }
    }

    private void CloseGoCancelPopup()
    {
        if (GodotObject.IsInstanceValid(_activeGoCancelPopup))
        {
            _activeGoCancelPopup.GoSelected -= OnGoCancelPopupGoSelected;
            _activeGoCancelPopup.Cancelled -= OnGoCancelPopupCancelled;
            _activeGoCancelPopup.QueueFree();
            _activeGoCancelPopup = null;
        }

        _activeGoAction = null;
    }
}
