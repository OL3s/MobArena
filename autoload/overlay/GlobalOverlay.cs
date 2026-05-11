using Godot;
using System;

namespace MobArena.Scripts;

public partial class GlobalOverlay : CanvasLayer
{
    private const string PopupBlurBackdropName = "PopupBlurBackdrop";
    private const string BlurShaderPath = "res://assets/shaders/PopupBlurBackdrop.gdshader";
    private const string InfoPopupPanelScenePath = "res://scenes/components/panels/InfoPopupPanel.tscn";
    private const string GoCancelPopupPanelScenePath = "res://scenes/components/panels/GoCancelPopupPanel.tscn";

    private static readonly Shader BlurShader = ResourceLoader.Load<Shader>(BlurShaderPath);
    private static readonly PackedScene InfoPopupPanelScene = ResourceLoader.Load<PackedScene>(InfoPopupPanelScenePath);
    private static readonly PackedScene GoCancelPopupPanelScene = ResourceLoader.Load<PackedScene>(GoCancelPopupPanelScenePath);

    private ColorRect _popupBlurBackdrop;
    private InfoPopupPanel _activeInfoPopup;
    private GoCancelPopupPanel _activeGoCancelPopup;
    private Action _activeGoAction;

    public static GlobalOverlay Get()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        return sceneTree?.Root?.GetNodeOrNull<GlobalOverlay>("/root/GlobalOverlay");
    }

    public override void _Ready()
    {
        EnsurePopupBlurBackdrop();
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

        EnsurePopupBlurBackdrop();
        ClosePopups();
        MoveChild(_popupBlurBackdrop, GetChildCount() - 1);

        _popupBlurBackdrop.Visible = true;
        AddChild(overlay);
        overlay.TreeExited += HideBlurIfNoPopup;
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
        if (InfoPopupPanelScene == null)
            return;

        EnsurePopupBlurBackdrop();
        ClosePopups();
        MoveChild(_popupBlurBackdrop, GetChildCount() - 1);

        _activeInfoPopup = InfoPopupPanelScene.Instantiate<InfoPopupPanel>();
        _popupBlurBackdrop.Visible = true;
        AddChild(_activeInfoPopup);
        _activeInfoPopup.ShowContent(title, richText, image);
        _activeInfoPopup.Closed += OnInfoPopupClosed;
    }

    public void ShowGoCancelPopup(string title, string richText, Action goAction, string goText = "Go", string cancelText = "Cancel")
    {
        if (GoCancelPopupPanelScene == null)
            return;

        EnsurePopupBlurBackdrop();
        ClosePopups();
        MoveChild(_popupBlurBackdrop, GetChildCount() - 1);

        _activeGoAction = goAction;
        _activeGoCancelPopup = GoCancelPopupPanelScene.Instantiate<GoCancelPopupPanel>();
        _popupBlurBackdrop.Visible = true;
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

        for (var i = GetChildCount() - 1; i >= 0; i--)
        {
            var child = GetChild(i);
            if (child == _popupBlurBackdrop)
                continue;

            child.QueueFree();
            return;
        }
    }

    public void CloseAllOverlays()
    {
        ClosePopups();

        foreach (Node child in GetChildren())
        {
            if (child == _popupBlurBackdrop)
                continue;

            child.QueueFree();
        }
    }

    private void EnsurePopupBlurBackdrop()
    {
        if (GodotObject.IsInstanceValid(_popupBlurBackdrop))
            return;

        _popupBlurBackdrop = new ColorRect
        {
            Name = PopupBlurBackdropName,
            MouseFilter = Control.MouseFilterEnum.Stop,
            Color = Colors.White,
            Visible = false
        };
        _popupBlurBackdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        if (BlurShader != null)
        {
            _popupBlurBackdrop.Material = new ShaderMaterial
            {
                Shader = BlurShader
            };
        }

        AddChild(_popupBlurBackdrop);
        MoveChild(_popupBlurBackdrop, 0);
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

        HideBlurIfNoPopup();
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
        HideBlurIfNoPopup();
    }

    private void HideBlurIfNoPopup()
    {
        if (!GodotObject.IsInstanceValid(_popupBlurBackdrop))
            return;

        if (!GodotObject.IsInstanceValid(_activeInfoPopup) && !GodotObject.IsInstanceValid(_activeGoCancelPopup))
            _popupBlurBackdrop.Visible = false;
    }
}
