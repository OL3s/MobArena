using Godot;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public static class ArenaCombatActionRunner
{
	public static bool TryActivate(ArenaCombatant source, ItemData item, ArenaCombatActionData action)
	{
		var scene = action?.Effect?.Scene;
		if (source == null || scene == null)
			return false;

        var parent = source.GetParent();
        if (parent == null)
            return false;

        var direction = source.LookDirection == Vector2.Zero ? Vector2.Right : source.LookDirection.Normalized();
		var instance = scene.Instantiate<Node2D>();
        parent.AddChild(instance);
        instance.GlobalPosition = source.GlobalPosition + direction * action.SpawnDistance;
        instance.GlobalRotation = direction.Angle();

        if (instance is IArenaCombatEffect combatEffect)
        {
            combatEffect.Initialize(new ArenaCombatEffectContext
            {
                Source = source,
                SourceTeam = source.Team,
                SourceItem = item,
                ItemDamage = item is DamageItemData damageItem ? damageItem.Damage : null,
                Action = action,
                Effect = action.Effect,
                Direction = direction
            });
        }
        else
        {
			GD.PushError($"Arena combat action spawned scene '{action.Effect.ScenePath}' without IArenaCombatEffect.");
            instance.QueueFree();
            return false;
        }

        return true;
    }
}
