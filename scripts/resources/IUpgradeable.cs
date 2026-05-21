namespace MobArena.Scripts.Resources;

public interface IUpgradeable
{
    int UpgradeLevel { get; }
    int MaxUpgradeLevel { get; }
    int GetUpgradeGoldCost();
    bool CanUpgrade();
    bool TryUpgrade();
}
