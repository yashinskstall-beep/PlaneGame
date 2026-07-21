using UnityEngine;

/// <summary>
/// Plane part unlock progression (clicks, cost, glide). Owned by menu buy flow; data from PlaneUpgradeConfig on plane.
/// </summary>
public class PartUpgradeSystem
{
    public int CurrentIndex { get; set; }
    public int ClickCount { get; set; }
    public float CurrentCost { get; private set; }

    public PlaneUpgradeConfig Config { get; set; }
    public int CostStart { get; set; } = 50;
    public int CostIncrement { get; set; } = 25;

    public int PartCount => Config != null ? Config.PartCount : 0;

    public bool IsFullyUpgraded() => PartCount > 0 && CurrentIndex >= PartCount;

    public int GetStepIndex() => CurrentIndex * UpgradeCostUtil.ClicksRequired + ClickCount;

    public int GetClickCost()
    {
        if (IsFullyUpgraded())
            return 0;
        return UpgradeCostUtil.GetTrackedUpgradeCost(CostStart, CostIncrement, GetStepIndex());
    }

    public void RefreshCost() => CurrentCost = GetClickCost();

    public void ApplyGlideProgress()
    {
        if (Config == null)
            return;
        Config.ApplyGlideForCurrentUnlocks();
    }

    public void RegisterClick()
    {
        ClickCount++;
        RefreshCost();
        ApplyGlideProgress();
    }

    public bool IsBatchComplete => ClickCount >= UpgradeCostUtil.ClicksRequired;

    public void CompletePartUnlock()
    {
        CurrentIndex++;
        ClickCount = 0;
        RefreshCost();
    }

    public void Reset()
    {
        CurrentIndex = 0;
        ClickCount = 0;
        CurrentCost = CostStart;
        ApplyGlideProgress();
    }

    public void Load()
    {
        UpgradeSaveStore.LoadPartProgress(out int index, out int clicks, out float cost, CostStart);
        CurrentIndex = index;
        ClickCount = clicks;
        CurrentCost = cost;
        RefreshCost();
    }

    public void Save(int playerCoins)
    {
        UpgradeSaveStore.SavePartProgress(CurrentIndex, ClickCount, CurrentCost, playerCoins);
    }

    public string[] GetPartNames() => Config != null ? Config.GetPartNames() : System.Array.Empty<string>();
}
