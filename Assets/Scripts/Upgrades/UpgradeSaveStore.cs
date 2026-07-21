using UnityEngine;

/// <summary>
/// PlayerPrefs read/write for upgrade progress via LevelProgress keys.
/// </summary>
public static class UpgradeSaveStore
{
    public static void SavePartProgress(int currentIndex, int clickCount, float currentCost, int playerCoins)
    {
        PlayerPrefs.SetInt(LevelProgress.GetUpgradeCurrentIndexKey(), currentIndex);
        PlayerPrefs.SetInt(LevelProgress.GetUpgradeClickCountKey(), clickCount);
        PlayerPrefs.SetFloat(LevelProgress.GetUpgradeCurrentCostKey(), currentCost);
        PlayerPrefs.SetInt(LevelProgress.CoinsKey, playerCoins);
        PlayerPrefs.Save();
    }

    public static void LoadPartProgress(out int currentIndex, out int clickCount, out float currentCost, float defaultCost)
    {
        currentIndex = PlayerPrefs.GetInt(LevelProgress.GetUpgradeCurrentIndexKey(), 0);
        clickCount = PlayerPrefs.GetInt(LevelProgress.GetUpgradeClickCountKey(), 0);
        currentCost = PlayerPrefs.GetFloat(LevelProgress.GetUpgradeCurrentCostKey(), defaultCost);
    }

    public static void SaveLaunchForceProgress(int level, int clickCount, float currentCost, float multiplier)
    {
        PlayerPrefs.SetInt(LevelProgress.GetLaunchForceLevelKey(), level);
        PlayerPrefs.SetInt(LevelProgress.GetLaunchForceClickCountKey(), clickCount);
        PlayerPrefs.SetFloat(LevelProgress.GetLaunchForceCurrentCostKey(), currentCost);
        PlayerPrefs.SetFloat(LevelProgress.GetLaunchForceMultiplierKey(), multiplier);
        PlayerPrefs.Save();
    }

    public static void LoadLaunchForceProgress(
        out int level,
        out int clickCount,
        out float currentCost,
        float defaultCostForLevel)
    {
        level = PlayerPrefs.GetInt(LevelProgress.GetLaunchForceLevelKey(), 1);
        if (!PlayerPrefs.HasKey(LevelProgress.GetLaunchForceLevelKey()) && PlayerPrefs.HasKey("LaunchForceLevel"))
            level = PlayerPrefs.GetInt("LaunchForceLevel", level);

        clickCount = PlayerPrefs.GetInt(LevelProgress.GetLaunchForceClickCountKey(), 0);
        currentCost = PlayerPrefs.GetFloat(LevelProgress.GetLaunchForceCurrentCostKey(), defaultCostForLevel);
    }

    public static void SaveBoostProgress(int level, float duration)
    {
        PlayerPrefs.SetInt(LevelProgress.GetBoostLevelKey(), level);
        PlayerPrefs.SetFloat(LevelProgress.GetBoostDurationKey(), duration);
        PlayerPrefs.Save();
    }

    public static void LoadBoostProgress(out int level, out float duration, float defaultDuration)
    {
        level = PlayerPrefs.GetInt(LevelProgress.GetBoostLevelKey(), 0);
        duration = PlayerPrefs.GetFloat(LevelProgress.GetBoostDurationKey(), defaultDuration);
    }

    public static void SaveCoinMultiplierProgress(int level, int clickCount, float value)
    {
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierLevelKey, level);
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierClickCountKey, clickCount);
        PlayerPrefs.SetFloat(LevelProgress.CoinMultiplierValueKey, value);
        PlayerPrefs.Save();
    }

    public static void LoadCoinMultiplierProgress(out int level, out int clickCount)
    {
        level = PlayerPrefs.GetInt(LevelProgress.CoinMultiplierLevelKey, 1);
        clickCount = PlayerPrefs.GetInt(LevelProgress.CoinMultiplierClickCountKey, 0);
    }

    public static void ResetCoinMultiplierDefaults()
    {
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierLevelKey, 1);
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierClickCountKey, 0);
        PlayerPrefs.SetFloat(LevelProgress.CoinMultiplierValueKey, 1f);
        PlayerPrefs.Save();
    }
}
