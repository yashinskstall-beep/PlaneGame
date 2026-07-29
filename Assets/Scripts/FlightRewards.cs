using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Flight payout and level-progress side effects. UI shows score; this applies rewards.
/// </summary>
public static class FlightRewards
{
    public static int CalculateCoins(float distanceMeters, bool wasMisfire)
    {
        if (wasMisfire)
            return 0;

        float multiplier = CoinManager.Instance != null
            ? CoinManager.Instance.GetCoinMultiplier()
            : LevelProgress.GetCoinMultiplierValue();

        return Mathf.RoundToInt(Mathf.Max(0f, distanceMeters) * multiplier);
    }

    public static void AwardCoins(int coinsEarned)
    {
        if (coinsEarned <= 0)
            return;

        CoinManager.EnsureInstance();
        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(coinsEarned);
        else
        {
            int updatedBalance = PlayerPrefs.GetInt(LevelProgress.CoinsKey, 0) + coinsEarned;
            PlayerPrefs.SetInt(LevelProgress.CoinsKey, updatedBalance);
            PlayerPrefs.Save();
        }
    }

    public static void OnGoalReached()
    {
        LevelProgress.MarkSceneCompleted();

        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        LevelsUI.UnlockLevel(nextLevelIndex);
    }
}
