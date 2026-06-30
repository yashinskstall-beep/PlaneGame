using UnityEngine;

[System.Serializable]
public class PlaneUpgradePartEntry
{
    public GameObject part;
    public Transform cameraFocusPoint;
}

[System.Serializable]
public class PlaneGlideSettings
{
    public float glideDrag;
    public float diveDrag;
    public float airResistanceCoefficient;
    public float velocityResistanceFactor;
    public float orientationResistanceFactor;
    public float momentumDecayRate;
    public float groundDragFactor;
}

/// <summary>
/// Per-scene unlockable plane parts and glide tuning by unlock count.
/// Attach to the plane root; MainMenu drives purchases and camera focus.
/// </summary>
public class PlaneUpgradeConfig : MonoBehaviour
{
    public PlaneUpgradePartEntry[] upgradeParts;
    public PlaneController planeController;
    [Tooltip("Glide/drag values indexed by how many parts are currently unlocked (0 = none).")]
    public PlaneGlideSettings[] glideByUnlockCount;

    public int PartCount => upgradeParts != null ? upgradeParts.Length : 0;

    void Awake()
    {
        if (planeController == null)
            planeController = GetComponent<PlaneController>();
    }

    public string[] GetPartNames()
    {
        if (upgradeParts == null || upgradeParts.Length == 0)
            return System.Array.Empty<string>();

        string[] names = new string[upgradeParts.Length];
        for (int i = 0; i < upgradeParts.Length; i++)
        {
            PlaneUpgradePartEntry entry = upgradeParts[i];
            names[i] = entry?.part != null ? entry.part.name : string.Empty;
        }

        return names;
    }

    public GameObject GetPart(int index)
    {
        if (upgradeParts == null || index < 0 || index >= upgradeParts.Length)
            return null;

        return upgradeParts[index]?.part;
    }

    public Transform GetFocusPoint(int index)
    {
        if (upgradeParts == null || index < 0 || index >= upgradeParts.Length)
            return null;

        PlaneUpgradePartEntry entry = upgradeParts[index];
        if (entry == null)
            return null;

        if (entry.cameraFocusPoint != null)
            return entry.cameraFocusPoint;

        return entry.part != null ? entry.part.transform : null;
    }

    public static bool IsPartUnlocked(GameObject part)
    {
        if (part == null)
            return false;

        return PlayerPrefs.GetInt(LevelProgress.GetPartActiveKey(part.name), 0) == 1;
    }

    public void UnlockPart(int index)
    {
        GameObject part = GetPart(index);
        if (part == null)
            return;

        part.SetActive(true);
        PlayerPrefs.SetInt(LevelProgress.GetPartActiveKey(part.name), 1);
        PlayerPrefs.Save();
        ApplyGlideForCurrentUnlocks();
    }

    public void ApplyPartStatesFromSave()
    {
        if (upgradeParts == null)
            return;

        foreach (PlaneUpgradePartEntry entry in upgradeParts)
        {
            if (entry?.part == null)
                continue;

            bool unlocked = IsPartUnlocked(entry.part);
            entry.part.SetActive(unlocked);
        }

        ApplyGlideForCurrentUnlocks();
    }

    public void ApplyGlideForCurrentUnlocks()
    {
        if (planeController == null || glideByUnlockCount == null || glideByUnlockCount.Length == 0)
            return;

        int unlockCount = GetUnlockedPartCount();
        int index = Mathf.Clamp(unlockCount, 0, glideByUnlockCount.Length - 1);
        PlaneGlideSettings settings = glideByUnlockCount[index];
        if (settings == null)
            return;

        planeController.glideDrag = settings.glideDrag;
        planeController.diveDrag = settings.diveDrag;
        planeController.airResistanceCoefficient = settings.airResistanceCoefficient;
        planeController.velocityResistanceFactor = settings.velocityResistanceFactor;
        planeController.orientationResistanceFactor = settings.orientationResistanceFactor;
        planeController.momentumDecayRate = settings.momentumDecayRate;
        planeController.groundDragFactor = settings.groundDragFactor;
    }

    private int GetUnlockedPartCount()
    {
        if (upgradeParts == null)
            return 0;

        int count = 0;
        foreach (PlaneUpgradePartEntry entry in upgradeParts)
        {
            if (entry?.part != null && IsPartUnlocked(entry.part))
                count++;
        }

        return count;
    }
}
