using Godot;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public static class ArenaCombatActionRunner
{
	public static bool TryActivate(ArenaCombatant source, ItemData item, ArenaCombatActionData action, float buildupScalar = 1f)
	{
		var scene = action?.Effect?.Scene;
		if (source == null || scene == null)
			return false;

        var parent = source.GetParent();
        if (parent == null)
            return false;

        var direction = source.LookDirection == Vector2.Zero ? Vector2.Right : source.LookDirection.Normalized();
		return ArenaCombatEffectSpawner.TrySpawn(
            parent,
            source.GlobalPosition + direction * action.SpawnDistance,
            direction.Angle(),
            new ArenaCombatEffectContext
            {
                Source = source,
                SourceTeam = source.Team,
                SourceItem = item,
                ItemDamage = item is DamageItemData damageItem ? damageItem.Damage : null,
                Action = action,
                Effect = action.Effect,
                Direction = direction,
                BuildupScalar = Mathf.Clamp(buildupScalar, ArenaCombatBuildupData.MinScalar, ArenaCombatBuildupData.MaxScalar),
                MaxChainDepth = Mathf.Max(0, action.MaxChainDepth)
            },
            action.Effect);
	}
}
