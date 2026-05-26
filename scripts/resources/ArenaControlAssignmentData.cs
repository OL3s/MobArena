using Godot;

namespace MobArena.Scripts.Resources;

public partial class ArenaControlAssignmentData : Resource
{
	public readonly record struct ControllerIdentity(LocalInputControllerConfig.ControllerKind Kind, int DeviceId);

    [Export]
    public GladiatorData Gladiator { get; set; }

    [Export]
    public LocalInputControllerConfig.ControllerKind ControllerKind { get; set; } = LocalInputControllerConfig.ControllerKind.Keyboard;

    [Export]
    public int DeviceId { get; set; } = -1;

    public ControllerIdentity ControllerKey => GetControllerKey(ControllerKind, DeviceId);
    public string DisplayName => LocalInputControllerConfig.GetDisplayName(ControllerKind, DeviceId);

    public bool MatchesController(LocalInputControllerConfig controllerSetup)
    {
        return controllerSetup != null && ControllerKey == GetControllerKey(controllerSetup);
    }

    public static ArenaControlAssignmentData Create(GladiatorData gladiator, LocalInputControllerConfig controllerSetup)
    {
        return new ArenaControlAssignmentData
        {
            Gladiator = gladiator,
            ControllerKind = controllerSetup?.Kind ?? LocalInputControllerConfig.ControllerKind.Keyboard,
            DeviceId = controllerSetup?.DeviceId ?? -1
        };
    }

    public static ControllerIdentity GetControllerKey(LocalInputControllerConfig controllerSetup)
    {
        return controllerSetup == null ? default : GetControllerKey(controllerSetup.Kind, controllerSetup.DeviceId);
    }

    private static ControllerIdentity GetControllerKey(LocalInputControllerConfig.ControllerKind kind, int deviceId)
    {
        return new ControllerIdentity(kind, deviceId);
    }
}
