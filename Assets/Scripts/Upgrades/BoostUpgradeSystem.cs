using UnityEngine;

/// <summary>
/// Boost duration unlocks (menu buy). In-flight use stays on PlaneController / FlightHUD.
/// </summary>
public class BoostUpgradeSystem
{
    public int Level { get; set; }
    public float[] Durations { get; set; } = { 2f, 2.5f, 3f, 3.5f, 4f };

    public int MaxLevel => Durations != null && Durations.Length > 0 ? Durations.Length : 1;

    public float GetDurationForLevel(int level)
    {
        if (Durations == null || Durations.Length == 0)
            return 2f;
        int index = Mathf.Clamp(level - 1, 0, Durations.Length - 1);
        return Durations[index];
    }

    public float CurrentDuration => Level <= 0 ? 0f : GetDurationForLevel(Level);

    public bool IsMax => Level >= MaxLevel;

    public void ApplyToPlane(PlaneController plane)
    {
        if (plane == null || Level <= 0)
            return;
        plane.boostDuration = CurrentDuration;
    }

    public void Reset() => Level = 0;

    public void Load()
    {
        UpgradeSaveStore.LoadBoostProgress(out int level, out _, GetDurationForLevel(1));
        Level = Mathf.Clamp(level, 0, MaxLevel);
    }

    public void Save()
    {
        UpgradeSaveStore.SaveBoostProgress(Level, CurrentDuration);
    }
}
