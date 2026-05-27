using UnityEngine;

/// <summary>
/// Only coins persist across levels. All other gameplay progress resets per level load.
/// </summary>
public static class LevelProgress
{
    public const string CoinsKey = "PlayerCoins";

    private static readonly string[] GameplayKeys =
    {
        "Upgrade_CurrentIndex",
        "Upgrade_ClickCount",
        "Upgrade_CurrentCost",
        "LaunchForceLevel",
        "LaunchForceMultiplier",
        "leftWing_active",
        "rightWing_active",
        "tail_active",
        "LeftWing_active",
        "RightWing_active",
        "Tail_active"
    };

    public static void ResetGameplayProgress()
    {
        foreach (string key in GameplayKeys)
            PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();
    }
}
