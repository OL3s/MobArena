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
            GameLogger.Combat($"Combat spawn blocked: chain depth limit reached for {effect}. {context.ToStringExtended()}");
            return false;
        }

        var instance = scene.Instantiate<Node2D>();
        instance.TopLevel = true;
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
        GameLogger.Combat($"Combat spawn: {effect} action={childContext.ActionName} at {position}.");
        combatEffect.Initialize(childContext);
        return true;
    }

    public static bool TrySpawnScene(Node parent, Vector2 position, float rotation, PackedScene scene)
    {
        if (parent == null || scene == null)
            return false;

        var instance = scene.Instantiate<Node2D>();
        instance.TopLevel = true;
        instance.GlobalPosition = position;
        instance.GlobalRotation = rotation;
        parent.CallDeferred(Node.MethodName.AddChild, instance);
        GameLogger.Combat($"Combat spawn scene: {scene.ResourcePath.GetFile().GetBaseName()} at {position}.");
        return true;
    }
}
