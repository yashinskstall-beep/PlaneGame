using UnityEngine;

/// <summary>
/// Global income multiplier upgrade clicks/cost.
/// </summary>
public class CoinMultiplierUpgradeSystem
{
    public int Level { get; set; } = 1;
    public int ClickCount { get; set; }

    public int CostStart { get; set; } = 50;
    public int CostIncrement { get; set; } = 25;
    public float MaxValue { get; set; } = 10f;
    public float Step { get; set; } = 0.2f;

    public int GetTotalClicks() => (Level - 1) * UpgradeCostUtil.ClicksRequired + ClickCount;

    public int GetMaxTotalClicks() => Mathf.RoundToInt((MaxValue - 1f) / Step);

    public bool IsMax => GetTotalClicks() >= GetMaxTotalClicks();

    public float GetValue() => 1f + GetTotalClicks() * Step;

    public int GetClickCost()
    {
        if (IsMax)
            return 0;
        return UpgradeCostUtil.GetTrackedUpgradeCost(CostStart, CostIncrement, GetTotalClicks());
    }

    public void RegisterClick()
    {
        ClickCount++;
        if (ClickCount >= UpgradeCostUtil.ClicksRequired)
        {
            ClickCount = 0;
            Level++;
        }
        Clamp();
    }

    public void Clamp()
    {
        int maxClicks = GetMaxTotalClicks();
        int total = Mathf.Clamp(GetTotalClicks(), 0, maxClicks);
        Level = total / UpgradeCostUtil.ClicksRequired + 1;
        ClickCount = total % UpgradeCostUtil.ClicksRequired;
    }

    public void Reset()
    {
        Level = 1;
        ClickCount = 0;
    }

    public void Load()
    {
        UpgradeSaveStore.LoadCoinMultiplierProgress(out int level, out int clicks);
        Level = level;
        ClickCount = clicks;

        if (PlayerPrefs.HasKey(LevelProgress.CoinMultiplierValueKey))
        {
            float savedValue = PlayerPrefs.GetFloat(LevelProgress.CoinMultiplierValueKey, 1f);
            int totalClicks = Mathf.RoundToInt((savedValue - 1f) / Step);
            Level = totalClicks / UpgradeCostUtil.ClicksRequired + 1;
            ClickCount = totalClicks % UpgradeCostUtil.ClicksRequired;
        }

        Clamp();
    }

    public void Save()
    {
        UpgradeSaveStore.SaveCoinMultiplierProgress(Level, ClickCount, GetValue());
    }
}
