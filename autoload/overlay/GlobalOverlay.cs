using Godot;
using System;
using System.Collections.Generic;

namespace MobArena.Scripts;

public partial class GlobalOverlay : CanvasLayer
{
	private const string InfoPopupPanelScenePath = "res://scenes/components/panels/InfoPopupPanel.tscn";
	private const string GoCancelPopupPanelScenePath = "res://scenes/components/panels/GoCancelPopupPanel.tscn";

    private enum PopupKind
    {
        Info,
        GoCancel
    }

    private sealed class PopupRequest
    {
        public PopupKind Kind { get; init; }
        public string Title { get; init; }
        public string RichText { get; init; }
        public Texture2D Image { get; init; }
        public Action GoAction { get; init; }
        public Action ClosedAction { get; init; }
        public bool PauseGameUntilClosed { get; init; }
        public string GoText { get; init; }
        public string CancelText { get; init; }
    }

    [Signal]
    public delegate void PopupGamePauseRequestedEventHandler();

    [Signal]
    public delegate void PopupGameResumeRequestedEventHandler();

    private InfoPopupPanel _activeInfoPopup;
    private GoCancelPopupPanel _activeGoCancelPopup;
    private Action _activeGoAction;
    private Action _activePopupClosedAction;
    private bool _activePopupPausesGame;
    private bool _popupGamePaused;
    private readonly Queue<PopupRequest> _popupQueue = new();

    public static GlobalOverlay Get()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        return sceneTree?.Root?.GetNodeOrNull<GlobalOverlay>("/root/GlobalOverlay");
    }

	public override void _ExitTree()
	{
		CloseAllOverlaysImmediate();
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

	public void ShowBlurredPopup(string title, string richText, Texture2D image = null, Action closedAction = null, bool pauseGameUntilClosed = false)
	{
        EnqueueOrShowPopup(new PopupRequest
        {
            Kind = PopupKind.Info,
            Title = title,
            RichText = richText,
            Image = image,
            ClosedAction = closedAction,
            PauseGameUntilClosed = pauseGameUntilClosed
        });
    }

	public void ShowGoCancelPopup(string title, string richText, Action goAction, string goText = "Go", string cancelText = "Cancel", bool pauseGameUntilClosed = false)
	{
        EnqueueOrShowPopup(new PopupRequest
        {
            Kind = PopupKind.GoCancel,
            Title = title,
            RichText = richText,
            GoAction = goAction,
            PauseGameUntilClosed = pauseGameUntilClosed,
            GoText = goText,
            CancelText = cancelText
        });
    }

    public void CloseTopOverlay()
    {
        if (GodotObject.IsInstanceValid(_activeGoCancelPopup))
        {
            CloseGoCancelPopup(true);
            return;
        }

        if (GodotObject.IsInstanceValid(_activeInfoPopup))
        {
            CloseInfoPopup(true, true);
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

	public void CloseAllOverlaysImmediate()
	{
		_activeInfoPopup = null;
		_activeGoCancelPopup = null;
		_activeGoAction = null;
        _activePopupClosedAction = null;
        _activePopupPausesGame = false;
        _popupGamePaused = false;
        _popupQueue.Clear();

		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.Free();
		}
	}

	public bool HasOverlays()
	{
		return GetChildCount() > 0;
	}

    private void OnInfoPopupClosed()
    {
        CloseInfoPopup(true, true);
    }

    private void OnGoCancelPopupGoSelected()
    {
        var goAction = _activeGoAction;
        CloseGoCancelPopup(false);
        goAction?.Invoke();

        if (!HasActivePopup())
            ShowNextQueuedPopup();
    }

    private void OnGoCancelPopupCancelled()
    {
        CloseGoCancelPopup(true);
    }

    private void ClosePopups()
    {
        _popupQueue.Clear();
        CloseInfoPopup(false, false);
        CloseGoCancelPopup(false);
    }

    private void CloseInfoPopup(bool showNext, bool invokeClosedAction)
    {
        var closedAction = _activePopupClosedAction;
        _activePopupClosedAction = null;
        _activePopupPausesGame = false;

        if (GodotObject.IsInstanceValid(_activeInfoPopup))
        {
            _activeInfoPopup.Closed -= OnInfoPopupClosed;
            _activeInfoPopup.QueueFree();
            _activeInfoPopup = null;
        }

        if (invokeClosedAction)
            closedAction?.Invoke();

        if (showNext)
            ShowNextQueuedPopup();

        RefreshPopupGamePauseState();
    }

    private void CloseGoCancelPopup(bool showNext)
    {
        _activePopupPausesGame = false;

        if (GodotObject.IsInstanceValid(_activeGoCancelPopup))
        {
            _activeGoCancelPopup.GoSelected -= OnGoCancelPopupGoSelected;
            _activeGoCancelPopup.Cancelled -= OnGoCancelPopupCancelled;
            _activeGoCancelPopup.QueueFree();
            _activeGoCancelPopup = null;
        }

        _activeGoAction = null;

        if (showNext)
            ShowNextQueuedPopup();

        RefreshPopupGamePauseState();
    }

    private void EnqueueOrShowPopup(PopupRequest request)
    {
        if (request == null)
            return;

        if (HasActivePopup())
        {
            _popupQueue.Enqueue(request);
            return;
        }

        ShowPopupRequest(request);
    }

    private void ShowNextQueuedPopup()
    {
        if (HasActivePopup())
            return;

        while (_popupQueue.Count > 0)
        {
            if (ShowPopupRequest(_popupQueue.Dequeue()))
                return;
        }
    }

    private bool ShowPopupRequest(PopupRequest request)
    {
        return request.Kind switch
        {
            PopupKind.GoCancel => ShowGoCancelPopupNow(request),
            _ => ShowInfoPopupNow(request)
        };
    }

    private bool ShowInfoPopupNow(PopupRequest request)
    {
		var infoPopupPanelScene = ResourceLoader.Load<PackedScene>(InfoPopupPanelScenePath);
		if (infoPopupPanelScene == null)
			return false;

		_activeInfoPopup = infoPopupPanelScene.Instantiate<InfoPopupPanel>();
        _activePopupClosedAction = request.ClosedAction;
        _activePopupPausesGame = request.PauseGameUntilClosed;
        AddChild(_activeInfoPopup);
        _activeInfoPopup.ShowContent(request.Title, request.RichText, request.Image);
        _activeInfoPopup.Closed += OnInfoPopupClosed;
        CallDeferred(MethodName.GrabFirstOverlayFocus, _activeInfoPopup);
        RefreshPopupGamePauseState();
        return true;
    }

    private bool ShowGoCancelPopupNow(PopupRequest request)
    {
		var goCancelPopupPanelScene = ResourceLoader.Load<PackedScene>(GoCancelPopupPanelScenePath);
		if (goCancelPopupPanelScene == null)
			return false;

        _activeGoAction = request.GoAction;
		_activePopupPausesGame = request.PauseGameUntilClosed;
		_activeGoCancelPopup = goCancelPopupPanelScene.Instantiate<GoCancelPopupPanel>();
        AddChild(_activeGoCancelPopup);
        _activeGoCancelPopup.ShowContent(request.Title, request.RichText, request.GoText, request.CancelText);
        _activeGoCancelPopup.GoSelected += OnGoCancelPopupGoSelected;
        _activeGoCancelPopup.Cancelled += OnGoCancelPopupCancelled;
        CallDeferred(MethodName.GrabFirstOverlayFocus, _activeGoCancelPopup);
        RefreshPopupGamePauseState();
        return true;
    }

    private bool HasActivePopup()
    {
        return GodotObject.IsInstanceValid(_activeInfoPopup)
            || GodotObject.IsInstanceValid(_activeGoCancelPopup);
    }

    private void RefreshPopupGamePauseState()
    {
        if (_activePopupPausesGame)
        {
            if (!_popupGamePaused)
            {
                _popupGamePaused = true;
                EmitSignal(SignalName.PopupGamePauseRequested);
            }

            return;
        }

        if (!_popupGamePaused)
            return;

        _popupGamePaused = false;
        EmitSignal(SignalName.PopupGameResumeRequested);
    }
}
