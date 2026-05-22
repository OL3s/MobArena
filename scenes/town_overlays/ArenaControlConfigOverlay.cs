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
    private const float PromptPulseAmplitude = 0.1f;
    private const float PromptPulseBaseScale = 1.0f;
    private const float PromptPulseMinimumScale = 0.9f;
    private const float PromptPulseSpeed = 3.2f;
    private const float PromptPulsePhaseOffset = 0.75f;

    private RichTextLabel _statusLabel;
    private HBoxContainer _promptRow;
    private HBoxContainer _assignmentRow;
    private Button _resetButton;
    private Button _closeButton;
    private CompanyRunData _runData;
    private LocalInputConfig _localInputConfig;
    private Action _launchAction;
    private int _nextGladiatorIndex;
    private int _waitingDotCount = 1;
    private double _waitingAnimationElapsed;
    private double _promptAnimationElapsed;
    private string _promptSignature = string.Empty;
    private bool _readyPromptOpen;

    public void Configure(Action launchAction)
    {
        _launchAction = launchAction;
    }

    public override void _Ready()
    {
        _statusLabel = GetNode<RichTextLabel>("CenterContainer/Panel/MarginContainer/Layout/StatusLabel");
        _promptRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/PromptRow");
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
        BuildPromptRow();
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
        AnimatePromptRow(delta);

        if (!HasNextGladiator() || _readyPromptOpen)
            return;

        _waitingAnimationElapsed += delta;
        if (_waitingAnimationElapsed < WaitingAnimationIntervalSeconds)
            return;

        _waitingAnimationElapsed = 0d;
        _waitingDotCount = _waitingDotCount >= 3 ? 1 : _waitingDotCount + 1;
        RefreshUi();
    }

    private void AnimatePromptRow(double delta)
    {
        if (_promptRow == null || !_promptRow.Visible)
            return;

        _promptAnimationElapsed += delta;
        for (var index = 0; index < _promptRow.GetChildCount(); index++)
        {
            if (_promptRow.GetChild(index) is not Control prompt)
                continue;

            if (prompt.GetNodeOrNull<Control>("Icon") is not { } icon)
                continue;

            icon.PivotOffset = icon.Size * 0.5f;
            var phase = (float)_promptAnimationElapsed * PromptPulseSpeed - index * PromptPulsePhaseOffset;
            var scale = PromptPulseBaseScale + Mathf.Sin(phase) * PromptPulseAmplitude;
            scale = Mathf.Max(PromptPulseMinimumScale, scale);
            icon.Scale = Vector2.One * scale;
        }
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
        _promptAnimationElapsed = 0d;
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
            _statusLabel.Text = "[center]Assign gladiators to the Arena building first.[/center]";
            _resetButton.Disabled = true;
            BuildPromptRow();
            return;
        }

        _resetButton.Disabled = false;
        _statusLabel.Text = HasNextGladiator()
            ? "[center]Press an input to claim the [b]waiting[/b] gladiator.[/center]"
            : "[center]All arena gladiators have controls assigned.[/center]";

        for (var index = 0; index < arenaGladiators.Count; index++)
        {
            var gladiator = arenaGladiators[index];
            if (gladiator != null)
                _assignmentRow.AddChild(CreateGladiatorCard(gladiator, index));
        }

        BuildPromptRow();
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

    private void BuildPromptRow()
    {
        var shouldShow = HasNextGladiator() && !_readyPromptOpen;
        var signature = _localInputConfig == null
            ? shouldShow.ToString()
            : $"{shouldShow}:{_localInputConfig.HasKeyboardPlayer}:{_localInputConfig.HasTouchPlayer}:{_localInputConfig.CanJoin}";
        if (signature == _promptSignature)
            return;

        _promptSignature = signature;
        foreach (var child in _promptRow.GetChildren())
            child.QueueFree();

        _promptRow.Visible = shouldShow;
        if (!_promptRow.Visible || _localInputConfig == null)
            return;

        if (!_localInputConfig.HasKeyboardPlayer)
            AddPrompt(_localInputConfig.EnterIcon, "Keyboard");
        if (!_localInputConfig.HasTouchPlayer)
            AddPrompt(_localInputConfig.PhoneIcon, "Touch");
        if (_localInputConfig.CanJoin)
            AddPrompt(_localInputConfig.XboxAIcon, "Gamepad");
    }

    private void AddPrompt(Texture2D icon, string label)
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 6);
        _promptRow.AddChild(row);

        row.AddChild(new TextureRect
        {
            Name = "Icon",
            CustomMinimumSize = new Vector2(30f, 30f),
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        });

        row.AddChild(new Label
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        });
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
