using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persists the player's best flight distance per scene using PlayerPrefs.
/// </summary>
public static class BestDistanceRecord
{
    private const string KeyPrefix = "BestDistance_";

    public static string GetSceneKey()
    {
        return KeyPrefix + SceneManager.GetActiveScene().name;
    }

    public static float GetBestDistance()
    {
        return PlayerPrefs.GetFloat(GetSceneKey(), 0f);
    }

    public static bool TryUpdateBest(float distance)
    {
        if (distance <= 0f)
            return false;

        float currentBest = GetBestDistance();
        if (distance <= currentBest)
            return false;

        PlayerPrefs.SetFloat(GetSceneKey(), distance);
        PlayerPrefs.Save();
        return true;
    }
}
