using Godot;

namespace MobArena.Scripts.Resources;

public partial class ArenaControlAssignmentData : Resource
{
    [Export]
    public GladiatorData Gladiator { get; set; }

    [Export]
    public string ControllerName { get; set; } = string.Empty;

    [Export]
    public LocalInputControllerConfig.ControllerKind ControllerKind { get; set; } = LocalInputControllerConfig.ControllerKind.Keyboard;

    [Export]
    public int DeviceId { get; set; } = -1;

    public string ControllerKey => GetControllerKey(ControllerKind, DeviceId, ControllerName);

    public bool MatchesController(LocalInputControllerConfig controllerSetup)
    {
        return controllerSetup != null && ControllerKey == GetControllerKey(controllerSetup);
    }

    public static ArenaControlAssignmentData Create(GladiatorData gladiator, LocalInputControllerConfig controllerSetup)
    {
        return new ArenaControlAssignmentData
        {
            Gladiator = gladiator,
            ControllerName = controllerSetup?.ControllerName ?? string.Empty,
            ControllerKind = controllerSetup?.Kind ?? LocalInputControllerConfig.ControllerKind.Keyboard,
            DeviceId = controllerSetup?.DeviceId ?? -1
        };
    }

    public static string GetControllerKey(LocalInputControllerConfig controllerSetup)
    {
        return controllerSetup == null ? string.Empty : GetControllerKey(controllerSetup.Kind, controllerSetup.DeviceId, controllerSetup.ControllerName);
    }

    private static string GetControllerKey(LocalInputControllerConfig.ControllerKind kind, int deviceId, string controllerName)
    {
        return $"{kind}:{deviceId}:{controllerName}";
    }
}
