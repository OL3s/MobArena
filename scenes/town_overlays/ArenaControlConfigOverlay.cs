using Godot;
using System;
using System.Text;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaControlConfigOverlay : Control
{
    private const float CardWidth = 170f;
    private const double WaitingAnimationIntervalSeconds = 0.35;

    private Label _statusLabel;
    private HBoxContainer _assignmentRow;
    private Button _resetButton;
    private Button _closeButton;
    private CompanyRunData _runData;
    private LocalInputConfig _localInputConfig;
    private Action _launchAction;
    private int _nextGladiatorIndex;
    private int _waitingDotCount = 1;
    private double _waitingAnimationElapsed;
    private bool _readyPromptOpen;

    public void Configure(Action launchAction)
    {
        _launchAction = launchAction;
    }

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/StatusLabel");
        _assignmentRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/AssignmentRow");
        _resetButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/ResetButton");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        _runData = SaveNode.Get()?.CompanyRunData;
        _localInputConfig = LocalInputConfig.Get();

        _resetButton.FocusMode = FocusModeEnum.None;
        _closeButton.FocusMode = FocusModeEnum.None;
        _resetButton.Pressed += ResetAssignments;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshUi;

        ResetAssignments();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsJoinInput(inputEvent))
            return;

        GetViewport()?.SetInputAsHandled();
        if (_readyPromptOpen || !IsVisibleInTree())
            return;

        if (TryHandleJoinInput(inputEvent, out var controllerSetup))
            AssignNextGladiator(controllerSetup);
    }

    private static bool IsJoinInput(InputEvent inputEvent)
    {
        return inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter or Key.KpEnter }
            || inputEvent is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.A }
            || inputEvent is InputEventScreenTouch { Pressed: true };
    }

    public override void _Process(double delta)
    {
        if (!HasNextGladiator() || _readyPromptOpen)
            return;

        _waitingAnimationElapsed += delta;
        if (_waitingAnimationElapsed < WaitingAnimationIntervalSeconds)
            return;

        _waitingAnimationElapsed = 0d;
        _waitingDotCount = _waitingDotCount >= 3 ? 1 : _waitingDotCount + 1;
        RefreshUi();
    }

    private bool TryHandleJoinInput(InputEvent inputEvent, out LocalInputControllerConfig controllerSetup)
    {
        controllerSetup = null;
        if (_localInputConfig == null || !HasNextGladiator())
            return false;

        if (inputEvent is InputEventKey { Pressed: true, Echo: false } key
            && key.Keycode is Key.Enter or Key.KpEnter)
        {
            if (!_localInputConfig.TryJoinKeyboard())
                return false;

            controllerSetup = GetControllerSetup(LocalInputControllerConfig.ControllerKind.Keyboard, -1);
            return controllerSetup != null;
        }

        if (inputEvent is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.A } joypadButton)
        {
            if (!_localInputConfig.TryJoinGamepad(joypadButton.Device))
                return false;

            controllerSetup = GetControllerSetup(LocalInputControllerConfig.ControllerKind.Gamepad, joypadButton.Device);
            return controllerSetup != null;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: true })
        {
            if (!_localInputConfig.TryJoinTouch())
                return false;

            controllerSetup = GetControllerSetup(LocalInputControllerConfig.ControllerKind.Touch, -1);
            return controllerSetup != null;
        }

        return false;
    }

    private LocalInputControllerConfig GetControllerSetup(LocalInputControllerConfig.ControllerKind kind, int deviceId)
    {
        if (_localInputConfig == null)
            return null;

        foreach (var controllerSetup in _localInputConfig.ControllerSetups)
        {
            if (controllerSetup?.Kind == kind && controllerSetup.DeviceId == deviceId)
                return controllerSetup;
        }

        return null;
    }

    private void AssignNextGladiator(LocalInputControllerConfig controllerSetup)
    {
        if (controllerSetup == null || _runData?.TownAssignments?.ArenaGladiators == null)
            return;

        var gladiator = GetNextUnassignedGladiator();
        if (gladiator == null)
            return;

        if (!_runData.TrySetArenaControlAssignment(gladiator, controllerSetup))
            return;

        _nextGladiatorIndex++;
        RefreshUi();
        if (!HasNextGladiator())
            ShowReadyPrompt();
    }

    private GladiatorData GetNextUnassignedGladiator()
    {
        var arenaGladiators = _runData?.TownAssignments?.ArenaGladiators;
        if (arenaGladiators == null)
            return null;

        for (var index = 0; index < arenaGladiators.Count; index++)
        {
            var gladiator = arenaGladiators[index];
            if (gladiator != null && _runData.GetArenaControlAssignment(gladiator) == null)
            {
                _nextGladiatorIndex = index;
                return gladiator;
            }
        }

        return null;
    }

    private bool HasNextGladiator()
    {
        return GetNextUnassignedGladiator() != null;
    }

    private void ResetAssignments()
    {
        _readyPromptOpen = false;
        _nextGladiatorIndex = 0;
        _waitingDotCount = 1;
        _waitingAnimationElapsed = 0d;
        _localInputConfig?.ClearControllerSetups();
        _runData?.ClearArenaControlAssignments();
        RefreshUi();
    }

    private void RefreshUi()
    {
        foreach (var child in _assignmentRow.GetChildren())
            child.QueueFree();

        var arenaGladiators = _runData?.TownAssignments?.ArenaGladiators;
        if (arenaGladiators == null || arenaGladiators.Count <= 0)
        {
            _statusLabel.Text = "Assign gladiators to the Arena building first.";
            _resetButton.Disabled = true;
            return;
        }

        _resetButton.Disabled = false;
        _statusLabel.Text = HasNextGladiator()
            ? "Press Enter, touch the screen, or press A on a gamepad to assign controls from left to right."
            : "All arena gladiators have controls assigned.";

        for (var index = 0; index < arenaGladiators.Count; index++)
        {
            var gladiator = arenaGladiators[index];
            if (gladiator != null)
                _assignmentRow.AddChild(CreateGladiatorCard(gladiator, index));
        }
    }

    private Control CreateGladiatorCard(GladiatorData gladiator, int index)
    {
        var assignment = _runData?.GetArenaControlAssignment(gladiator);
        var isCurrent = assignment == null && index == _nextGladiatorIndex;
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(CardWidth, 210f)
        };
        if (isCurrent)
            panel.Modulate = new Color(1f, 0.92f, 0.55f);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        layout.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(76f, 76f),
            Texture = gladiator.GetPortraitTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });

        layout.AddChild(new Label
        {
            Text = gladiator.GladiatorName,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        layout.AddChild(new Label
        {
            Text = assignment == null ? (isCurrent ? GetWaitingText() : "Unassigned") : GetControllerLabel(assignment),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        return panel;
    }

    private void ShowReadyPrompt()
    {
        if (_readyPromptOpen)
            return;

        _readyPromptOpen = true;
        GlobalOverlay.Get()?.ShowGoCancelPopup(
            "Start Arena?",
            BuildReadySummary(),
            StartArena,
            "Start",
            "Reset",
            cancelAction: ResetAssignments);
    }

    private string GetWaitingText()
    {
        return $"Waiting{new string('.', _waitingDotCount)}";
    }

    private string BuildReadySummary()
    {
        var builder = new StringBuilder("Ready to enter the arena?\n\n");
        var arenaGladiators = _runData?.TownAssignments?.ArenaGladiators;
        if (arenaGladiators == null)
            return builder.ToString();

        for (var index = 0; index < arenaGladiators.Count; index++)
        {
            var gladiator = arenaGladiators[index];
            if (gladiator == null)
                continue;

            var assignment = _runData.GetArenaControlAssignment(gladiator);
            builder.AppendLine($"Player {index + 1}: {gladiator.GladiatorName} - {GetControllerLabel(assignment)}");
        }

        return builder.ToString();
    }

    private void StartArena()
    {
        _readyPromptOpen = false;
        _launchAction?.Invoke();
    }

    private static string GetControllerLabel(ArenaControlAssignmentData assignment)
    {
        if (assignment == null)
            return "Unassigned";

        var deviceLabel = assignment.ControllerKind == LocalInputControllerConfig.ControllerKind.Gamepad
            ? $" device {assignment.DeviceId}"
            : string.Empty;
        return $"{assignment.ControllerName} ({assignment.ControllerKind}{deviceLabel})";
    }
}
