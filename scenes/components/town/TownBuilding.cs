using Godot;
using MobArena.Scripts;

namespace MobArena.Scenes.Components.Town;

[Tool]
public partial class TownBuilding : Node2D
{
    private static readonly Rect2 InteractionBounds = new(new Vector2(-75.0f, -75.0f), new Vector2(150.0f, 150.0f));

    private string _buildingName = "Town Building";
    private Texture2D _buildingTexture;
    private Texture2D _iconTexture;
    private bool _disabled;

    [Export]
    public string BuildingName
    {
        get => _buildingName;
        set
        {
            _buildingName = value;
            RefreshVisuals();
        }
    }

    [Export]
    public Texture2D BuildingTexture
    {
        get => _buildingTexture;
        set
        {
            _buildingTexture = value;
            RefreshVisuals();
        }
    }

    [Export]
    public Texture2D IconTexture
    {
        get => _iconTexture;
        set
        {
            _iconTexture = value;
            RefreshVisuals();
        }
    }

    [Export]
    public PackedScene SceneToOpen { get; set; }

    [Export]
    public PackedScene OverlayToOpen { get; set; }

    [Export]
    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            RefreshVisuals();
        }
    }

    [Export]
    public string ConfirmationTitle { get; set; } = "Open Building?";

    [Export(PropertyHint.MultilineText)]
    public string ConfirmationMessage { get; set; } = "Go inside this building?";

    [Export]
    public string GoText { get; set; } = "Go";

    [Export]
    public string CancelText { get; set; } = "Cancel";

    [Export]
    public bool RequireConfirmation { get; set; } = true;

    [Export]
    public bool DebugInteraction { get; set; }

    private Area2D _interactionArea;
    private Label _nameLabel;
    private Node2D _visuals;
    private Sprite2D _buildingSprite;
    private Sprite2D _iconSprite;

    public override void _Ready()
    {
        _interactionArea = GetNode<Area2D>("InteractionArea");
        _nameLabel = GetNode<Label>("Visuals/NamePlate/NameLabel");
        _visuals = GetNode<Node2D>("Visuals");
        _buildingSprite = GetNode<Sprite2D>("Visuals/BuildingSprite");
        _iconSprite = GetNode<Sprite2D>("Visuals/IconSprite");

        RefreshVisuals();

        if (Engine.IsEditorHint())
            return;

        _interactionArea.InputPickable = true;
        _interactionArea.InputEvent += OnInteractionInputEvent;
        _interactionArea.MouseEntered += OnMouseEntered;
        _interactionArea.MouseExited += OnMouseExited;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (Disabled || !IsVisibleInTree())
            return;

        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton)
        {
            ActivateIfInside(mouseButton.Position);
            return;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: true } screenTouch)
            ActivateIfInside(screenTouch.Position);
    }

    public void Activate()
    {
        if (DebugInteraction)
            GD.Print($"TownBuilding Activate: {BuildingName}, disabled={Disabled}, overlay={OverlayToOpen != null}, scene={SceneToOpen != null}");

        if (Disabled || (SceneToOpen == null && OverlayToOpen == null))
            return;

        if (OverlayToOpen != null)
        {
            OpenOverlay();
            return;
        }

        if (!RequireConfirmation)
        {
            OpenScene();
            return;
        }

        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null)
        {
            GD.PushWarning("Could not load global overlay. Opening scene directly.");
            OpenScene();
            return;
        }

        globalOverlay.ShowGoCancelPopup(ConfirmationTitle, ConfirmationMessage, OpenScene, GoText, CancelText);
    }

    private void OpenOverlay()
    {
        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null)
        {
            GD.PushWarning($"TownBuilding overlay failed: GlobalOverlay missing for {BuildingName}.");
            return;
        }

        if (DebugInteraction)
            GD.Print($"TownBuilding opening overlay: {BuildingName}");

        globalOverlay.AddOverlay(OverlayToOpen);
    }

    private void OnInteractionInputEvent(Node viewport, InputEvent inputEvent, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            if (DebugInteraction)
                GD.Print($"TownBuilding Area2D click: {BuildingName}");

            GetViewport()?.SetInputAsHandled();
            Activate();
            return;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: true })
        {
            if (DebugInteraction)
                GD.Print($"TownBuilding Area2D touch: {BuildingName}");

            GetViewport()?.SetInputAsHandled();
            Activate();
        }
    }

    private void OnMouseEntered()
    {
        if (Disabled)
            return;

        _visuals.Scale = new Vector2(1.04f, 1.04f);
    }

    private void OnMouseExited()
    {
        _visuals.Scale = Vector2.One;
    }

    private void OpenScene()
    {
        GetTree().ChangeSceneToPacked(SceneToOpen);
    }

    private void ActivateIfInside(Vector2 viewportPosition)
    {
        var worldPosition = GetCanvasTransform().AffineInverse() * viewportPosition;
        var localPosition = ToLocal(worldPosition);
        if (!InteractionBounds.HasPoint(localPosition))
            return;

        if (DebugInteraction)
            GD.Print($"TownBuilding fallback hit: {BuildingName}, viewport={viewportPosition}, local={localPosition}");

        GetViewport()?.SetInputAsHandled();
        Activate();
    }

    private void RefreshVisuals()
    {
        if (!IsInsideTree())
            return;

        _nameLabel ??= GetNodeOrNull<Label>("Visuals/NamePlate/NameLabel");
        _buildingSprite ??= GetNodeOrNull<Sprite2D>("Visuals/BuildingSprite");
        _iconSprite ??= GetNodeOrNull<Sprite2D>("Visuals/IconSprite");

        if (_nameLabel != null)
            _nameLabel.Text = string.IsNullOrWhiteSpace(BuildingName) ? "Town Building" : BuildingName;

        if (_buildingSprite != null && BuildingTexture != null)
            _buildingSprite.Texture = BuildingTexture;

        if (_iconSprite != null && IconTexture != null)
            _iconSprite.Texture = IconTexture;

        Modulate = Disabled ? new Color(0.55f, 0.55f, 0.55f, 1.0f) : Colors.White;
    }
}
