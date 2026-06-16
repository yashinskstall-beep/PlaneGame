using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Only coins persist across levels. All other gameplay progress resets per level load.
/// </summary>
public static class LevelProgress
{
    public const string CoinsKey = "PlayerCoins";

    private static bool gameplayResetPending;

    private static readonly string[] GameplayKeys =
    {
        "Upgrade_CurrentIndex",
        "Upgrade_ClickCount",
        "Upgrade_CurrentCost",
        "LaunchForceLevel",
        "LaunchForceMultiplier",
        "CoinMultiplierLevel",
        "CoinMultiplier",
        "leftWing_active",
        "rightWing_active",
        "tail_active",
        "LeftWing_active",
        "RightWing_active",
        "Tail_active",
        "Left_Wing_active",
        "Right_Wing_active"
    };

    public static string GetPartActiveKey(string partObjectName)
    {
        return partObjectName + "_active";
    }

    public static void ResetGameplayProgress(IEnumerable<string> partObjectNames = null)
    {
        foreach (string key in GameplayKeys)
            PlayerPrefs.DeleteKey(key);

        if (partObjectNames != null)
        {
            foreach (string partName in partObjectNames)
            {
                if (string.IsNullOrEmpty(partName))
                    continue;

                PlayerPrefs.DeleteKey(GetPartActiveKey(partName));
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
