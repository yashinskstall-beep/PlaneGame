using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Save helpers. Coins are global; upgrade/part progress is per scene.
/// </summary>
public static class LevelProgress
{
    public const string CoinsKey = "PlayerCoins";

    private static bool gameplayResetPending;

    public static string GetSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    private static string ScenePrefix(string sceneName = null)
    {
        return (string.IsNullOrEmpty(sceneName) ? GetSceneName() : sceneName) + "_";
    }

    public static string GetPartActiveKey(string partObjectName, string sceneName = null)
    {
        return ScenePrefix(sceneName) + partObjectName + "_active";
    }

    public static string GetUpgradeCurrentIndexKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "Upgrade_CurrentIndex";
    }

    public static string GetUpgradeClickCountKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "Upgrade_ClickCount";
    }

    public static string GetUpgradeCurrentCostKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "Upgrade_CurrentCost";
    }

    public static string GetLaunchForceLevelKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "LaunchForceLevel";
    }

    public static string GetLaunchForceMultiplierKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "LaunchForceMultiplier";
    }

    public static string GetLaunchForceClickCountKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "LaunchForceClickCount";
    }

    public static string GetBoostLevelKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "BoostLevel";
    }

    public static string GetBoostDurationKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "BoostDuration";
    }

    public static string GetSceneCompletedKey(string sceneName = null)
    {
        return ScenePrefix(sceneName) + "Completed";
    }

    public const string CoinMultiplierLevelKey = "CoinMultiplierLevel";
    public const string CoinMultiplierClickCountKey = "CoinMultiplierClickCount";
    public const string CoinMultiplierValueKey = "CoinMultiplier";

    public static int GetCoinMultiplierLevel()
    {
        return PlayerPrefs.GetInt(CoinMultiplierLevelKey, 1);
    }

    public static float GetCoinMultiplierValue()
    {
        if (PlayerPrefs.HasKey(CoinMultiplierValueKey))
            return PlayerPrefs.GetFloat(CoinMultiplierValueKey, 1f);

        int level = GetCoinMultiplierLevel();
        return 1f + (level - 1) * 0.1f;
    }

    public static bool HasCompletedScene(string sceneName = null)
    {
        return PlayerPrefs.GetInt(GetSceneCompletedKey(sceneName), 0) == 1;
    }

    public static void MarkSceneCompleted(string sceneName = null)
    {
        PlayerPrefs.SetInt(GetSceneCompletedKey(sceneName), 1);
        PlayerPrefs.Save();
    }

    public static void ResetGameplayProgressForScene(string sceneName, string[] partNames = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        PlayerPrefs.DeleteKey(GetUpgradeCurrentIndexKey(sceneName));
        PlayerPrefs.DeleteKey(GetUpgradeClickCountKey(sceneName));
        PlayerPrefs.DeleteKey(GetUpgradeCurrentCostKey(sceneName));
        PlayerPrefs.DeleteKey(GetLaunchForceLevelKey(sceneName));
        PlayerPrefs.DeleteKey(GetLaunchForceMultiplierKey(sceneName));
        PlayerPrefs.DeleteKey(GetLaunchForceClickCountKey(sceneName));
        PlayerPrefs.DeleteKey(GetBoostLevelKey(sceneName));
        PlayerPrefs.DeleteKey(GetBoostDurationKey(sceneName));

        if (partNames != null)
        {
            foreach (string partName in partNames)
            {
                if (!string.IsNullOrEmpty(partName))
                    PlayerPrefs.DeleteKey(GetPartActiveKey(partName, sceneName));
            }
        }

        gameplayResetPending = true;
        PlayerPrefs.Save();
    }

    public static bool ConsumeGameplayResetPending()
    {
        if (!gameplayResetPending)
            return false;

        gameplayResetPending = false;
        return true;
    }
}
