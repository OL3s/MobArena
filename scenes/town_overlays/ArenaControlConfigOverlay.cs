using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaControlConfigOverlay : Control
{
    private Label _statusLabel;
    private VBoxContainer _assignmentList;
    private Button _closeButton;
    private CompanyRunData _runData;
    private bool _refreshingUi;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/StatusLabel");
        _assignmentList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/AssignmentList");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton");
        _runData = SaveNode.Get()?.CompanyRunData;

        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshUi;

        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void RefreshUi()
    {
        if (_refreshingUi)
            return;

        _refreshingUi = true;
        var controllerSetups = LocalInputConfig.Get()?.ControllerSetups ?? new Godot.Collections.Array<LocalInputControllerConfig>();
        _runData?.SyncArenaControlAssignments(controllerSetups);

        foreach (var child in _assignmentList.GetChildren())
            child.QueueFree();

        var assignedGladiators = _runData?.TownAssignments?.ArenaGladiators;
        if (assignedGladiators == null || assignedGladiators.Count <= 0)
        {
            _statusLabel.Text = "Drag gladiators onto the Arena building before assigning controls.";
            _refreshingUi = false;
            return;
        }

        _statusLabel.Text = controllerSetups.Count <= 0
            ? "No local control setups are joined. Add keyboard, touch, or gamepad setups before starting the arena."
            : "Assign each Arena gladiator to one unique local control setup.";

        foreach (var gladiator in assignedGladiators)
        {
            if (gladiator != null)
                _assignmentList.AddChild(CreateAssignmentRow(gladiator, controllerSetups));
        }

        _refreshingUi = false;
    }

    private Control CreateAssignmentRow(GladiatorData gladiator, Godot.Collections.Array<LocalInputControllerConfig> controllerSetups)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 58)
        };
        row.AddThemeConstantOverride("separation", 10);

        row.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(48, 48),
            Texture = gladiator.GetPortraitTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });

        row.AddChild(new Label
        {
            Text = gladiator.GladiatorName,
            CustomMinimumSize = new Vector2(190, 0),
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        });

        var optionButton = new OptionButton
        {
            CustomMinimumSize = new Vector2(240, 44),
            Disabled = controllerSetups.Count <= 0
        };
        optionButton.AddItem("Unassigned", -1);

        var currentAssignment = _runData?.GetArenaControlAssignment(gladiator);
        var selectedItemIndex = 0;
        for (var index = 0; index < controllerSetups.Count; index++)
        {
            var controllerSetup = controllerSetups[index];
            optionButton.AddItem(controllerSetup.ControllerName, index);
            if (currentAssignment?.MatchesController(controllerSetup) == true)
                selectedItemIndex = index + 1;
        }

        optionButton.Select(selectedItemIndex);
        optionButton.ItemSelected += selectedIndex => OnControllerSelected(gladiator, controllerSetups, optionButton.GetItemId((int)selectedIndex));
        row.AddChild(optionButton);

        return row;
    }

    private void OnControllerSelected(GladiatorData gladiator, Godot.Collections.Array<LocalInputControllerConfig> controllerSetups, long controllerIndex)
    {
        if (controllerIndex < 0)
        {
            _runData?.ClearArenaControlAssignment(gladiator);
            return;
        }

        if (controllerIndex >= controllerSetups.Count)
            return;

        _runData?.TrySetArenaControlAssignment(gladiator, controllerSetups[(int)controllerIndex]);
    }
}
