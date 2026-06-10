using Godot;

namespace MobArena.Scripts;

public static class SceneTransitionLogger
{
    public static void LogChange(SceneTree tree, string target, string reason)
    {
        var current = tree?.CurrentScene?.SceneFilePath;
        if (string.IsNullOrWhiteSpace(current))
            current = tree?.CurrentScene?.Name ?? "Unknown";

        if (string.IsNullOrWhiteSpace(target))
            target = "Unknown";

        if (string.IsNullOrWhiteSpace(reason))
            reason = "unspecified";

        GD.Print($"Scene transition: {current} -> {target} ({reason}).");
    }

    public static void LogChange(SceneTree tree, PackedScene targetScene, string reason)
    {
        var target = string.IsNullOrWhiteSpace(targetScene?.ResourcePath)
            ? targetScene?.GetType().Name ?? "UnknownPackedScene"
            : targetScene.ResourcePath;
        LogChange(tree, target, reason);
    }
}
