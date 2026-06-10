using Godot;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public static class ArenaCombatEffectSpawner
{
    public static bool TrySpawn(Node parent, Vector2 position, float rotation, ArenaCombatEffectContext context, ArenaCombatEffectData effect)
    {
        var scene = effect?.Scene;
        if (parent == null || scene == null || context == null)
            return false;

        if (context.ChainDepth >= context.MaxChainDepth)
        {
            GD.PushWarning($"Combat spawn blocked: chain depth limit reached for {effect}. {context}");
            return false;
        }

        var instance = scene.Instantiate<Node2D>();
        instance.GlobalPosition = position;
        instance.GlobalRotation = rotation;

        if (instance is not IArenaCombatEffect combatEffect)
        {
            GD.PushError($"Arena combat effect spawned scene '{effect.ScenePath}' without IArenaCombatEffect.");
            instance.QueueFree();
            return false;
        }

        parent.CallDeferred(Node.MethodName.AddChild, instance);
        var childContext = context.WithEffect(effect);
        GD.Print($"Combat spawn: {effect.GetType().Name} at {position} rot={rotation:0.##}. {childContext}");
        combatEffect.Initialize(childContext);
        return true;
    }

    public static bool TrySpawnScene(Node parent, Vector2 position, float rotation, PackedScene scene)
    {
        if (parent == null || scene == null)
            return false;

        var instance = scene.Instantiate<Node2D>();
        instance.GlobalPosition = position;
        instance.GlobalRotation = rotation;
        parent.CallDeferred(Node.MethodName.AddChild, instance);
        GD.Print($"Combat spawn scene: {scene.ResourcePath.GetFile().GetBaseName()} at {position} rot={rotation:0.##}.");
        return true;
    }
}
