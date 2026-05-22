using Godot;

namespace MobArena.Scenes.Components.Arena;

public partial class ArenaPlayerCamera : Camera2D
{
    private const string ArenaPlayersGroup = "arena_players";

    [Export]
    public float FollowSmoothing { get; private set; } = 10f;

    public override void _Ready()
    {
        MakeCurrent();
    }

    public override void _PhysicsProcess(double delta)
    {
        var playerCount = 0;
        var positionSum = Vector2.Zero;

        foreach (var node in GetTree().GetNodesInGroup(ArenaPlayersGroup))
        {
            if (node is not Node2D player || !IsInstanceValid(player))
                continue;

            positionSum += player.GlobalPosition;
            playerCount++;
        }

        if (playerCount <= 0)
            return;

        var targetPosition = positionSum / playerCount;
        GlobalPosition = FollowSmoothing <= 0f
            ? targetPosition
            : GlobalPosition.Lerp(targetPosition, 1f - Mathf.Exp(-FollowSmoothing * (float)delta));
    }
}
