using UnityEngine;

/// <summary>
/// Slingshot launch-force tiers and click batches.
/// </summary>
public class LaunchForceUpgradeSystem
{
    public int Level { get; set; } = 1;
    public int ClickCount { get; set; }
    public float CurrentCost { get; private set; }

    public float[] ForceLevels { get; set; } = { 25f, 30f, 35f };
    public int CostStart { get; set; } = 50;
    public int CostIncrement { get; set; } = 25;

    public int MaxLevel => ForceLevels != null && ForceLevels.Length > 0 ? ForceLevels.Length : 1;

    public int GetStepIndex() => (Level - 1) * UpgradeCostUtil.ClicksRequired + ClickCount;

    public int GetClickCost()
    {
        if (Level >= MaxLevel)
            return 0;
        return UpgradeCostUtil.GetTrackedUpgradeCost(CostStart, CostIncrement, GetStepIndex());
    }

    public void RefreshCost() => CurrentCost = GetClickCost();

    public float GetForceForLevel(int level)
    {
        if (ForceLevels == null || ForceLevels.Length == 0)
            return 25f;
        int index = Mathf.Clamp(level - 1, 0, ForceLevels.Length - 1);
        return ForceLevels[index];
    }

    public float CurrentForce => GetForceForLevel(Level);

    public void RegisterClick()
    {
        ClickCount++;
        RefreshCost();
    }

    public bool IsBatchComplete => ClickCount >= UpgradeCostUtil.ClicksRequired;

    public void CompleteTier()
    {
        if (Level < MaxLevel)
            Level++;
        ClickCount = 0;
        RefreshCost();
    }

    public void ApplyToLauncher(SimpleDragLauncher launcher)
    {
        if (launcher == null)
            return;
        launcher.launchForceMultiplier = CurrentForce;
    }

    public void Reset()
    {
        Level = 1;
        ClickCount = 0;
        CurrentCost = CostStart;
    }

    public void Load()
    {
        float defaultCost = UpgradeCostUtil.GetTrackedUpgradeCost(
            CostStart, CostIncrement, (Level - 1) * UpgradeCostUtil.ClicksRequired);
        UpgradeSaveStore.LoadLaunchForceProgress(out int level, out int clicks, out float cost, defaultCost);
        Level = Mathf.Clamp(level, 1, MaxLevel);
        ClickCount = Mathf.Clamp(clicks, 0, UpgradeCostUtil.ClicksRequired - 1);
        if (Level >= MaxLevel)
            ClickCount = 0;
        CurrentCost = cost;
        RefreshCost();
    }

    public void Save(SimpleDragLauncher launcher)
    {
        float multiplier = launcher != null ? launcher.launchForceMultiplier : CurrentForce;
        UpgradeSaveStore.SaveLaunchForceProgress(Level, ClickCount, CurrentCost, multiplier);
    }
}
