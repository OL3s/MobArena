using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaCombatEffectData : Resource
{
	[Export(PropertyHint.File, "*.tscn")]
	public string ScenePath { get; private set; } = string.Empty;

    [Export]
    public ArenaCombatApplyData Apply { get; private set; }

	[Export(PropertyHint.File, "*.tscn")]
	public string OnHitScenePath { get; private set; } = string.Empty;

	[Export(PropertyHint.File, "*.tscn")]
	public string OnExpireScenePath { get; private set; } = string.Empty;

    [Export(PropertyHint.Range, "0.01,30,0.01")]
    public float LifetimeSeconds { get; private set; } = 0.16f;

    [Export(PropertyHint.Range, "1,100,1")]
    public int MaxHits { get; private set; } = 1;

	[Export]
	public bool CanHitSameTargetMultipleTimes { get; private set; }

	public PackedScene Scene => LoadPackedScene(ScenePath);

	public PackedScene OnHitScene => LoadPackedScene(OnHitScenePath);

	public PackedScene OnExpireScene => LoadPackedScene(OnExpireScenePath);

	private static PackedScene LoadPackedScene(string scenePath)
	{
		return string.IsNullOrWhiteSpace(scenePath) ? null : ResourceLoader.Load<PackedScene>(scenePath);
	}
}
