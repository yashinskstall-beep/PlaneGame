using UnityEngine;

/// <summary>
/// Shared upgrade cost ladder. Start/increment come from MainMenu (per scene).
/// </summary>
public static class UpgradeCostUtil
{
    public const int ClicksRequired = 5;

    public static int GetTrackedUpgradeCost(int startCost, int increment, int stepIndex)
    {
        return startCost + Mathf.Max(0, stepIndex) * Mathf.Max(0, increment);
    }
}
