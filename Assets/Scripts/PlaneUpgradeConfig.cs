using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class PlaneUpgradePartEntry
{
    public GameObject part;
    public Transform cameraFocusPoint;
    [Tooltip("Smoke/VFX child under this part (e.g. UpgradeSmoke). Drag the child here, or leave empty to auto-find.")]
    public Transform upgradeVfxPoint;
}

[System.Serializable]
public class PlaneGlideSettings
{
    [Tooltip("Rigidbody drag while gliding or climbing. Higher = slows down more in level flight.")]
    public float glideDrag;
    [Tooltip("Rigidbody drag while diving (nose down). Lower = builds more speed in a dive.")]
    public float diveDrag;
    [Tooltip("Constant base air resistance always applied against velocity. Higher = more overall slowdown.")]
    public float airResistanceCoefficient;
    [Tooltip("Extra air resistance that grows with speed squared. Higher = hard speed cap / stronger high-speed braking.")]
    public float velocityResistanceFactor;
    [Tooltip("Extra resistance when velocity is not aligned with the nose. Higher = more slowdown when flying sideways or skidding.")]
    public float orientationResistanceFactor;
    [Tooltip("How fast stored dive momentum is lost while climbing. Higher = shorter climb after a dive.")]
    public float momentumDecayRate;
    [Tooltip("Rigidbody drag while the wreck slides/tumbles after a ground crash. Higher = stops sooner.")]
    [FormerlySerializedAs("groundDragFactor")]
    public float wreckDrag;
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

    public Transform GetVfxAnchor(int index)
    {
        if (upgradeParts == null || index < 0 || index >= upgradeParts.Length)
            return null;

        PlaneUpgradePartEntry entry = upgradeParts[index];
        if (entry == null)
            return null;

        if (entry.upgradeVfxPoint != null)
            return entry.upgradeVfxPoint;

        if (entry.cameraFocusPoint != null)
            return entry.cameraFocusPoint;

        return entry.part != null ? entry.part.transform : null;
    }

    public GameObject GetUpgradeVfxRoot(int index)
    {
        if (upgradeParts == null || index < 0 || index >= upgradeParts.Length)
            return null;

        PlaneUpgradePartEntry entry = upgradeParts[index];
        if (entry?.part == null)
            return null;

        if (entry.upgradeVfxPoint != null && entry.upgradeVfxPoint.gameObject != entry.part)
            return entry.upgradeVfxPoint.gameObject;

        Transform namedChild = entry.part.transform.Find("UpgradeSmoke");
        if (namedChild != null && namedChild.gameObject != entry.part)
            return namedChild.gameObject;

        foreach (ParticleSystem ps in entry.part.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null || ps.transform == entry.part.transform)
                continue;

            if (!ps.transform.IsChildOf(entry.part.transform))
                continue;

            return ps.gameObject;
        }

        return null;
    }

    public void SuppressAllUpgradeVfx()
    {
        if (upgradeParts == null)
            return;

        for (int i = 0; i < upgradeParts.Length; i++)
        {
            PlaneUpgradePartEntry entry = upgradeParts[i];
            if (entry?.part == null)
                continue;

            StopAndHideUpgradeVfx(entry.part, GetUpgradeVfxRoot(i));
        }
    }

    public static void StopAndHideUpgradeVfx(GameObject part, GameObject vfxRoot)
    {
        if (vfxRoot == null)
            return;

        foreach (ParticleSystem ps in vfxRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null)
                continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Never disable the unlocked part — only hide a dedicated smoke child.
        if (part != null && vfxRoot != part && vfxRoot.transform.IsChildOf(part.transform))
            vfxRoot.SetActive(false);

        if (part != null && IsPartUnlocked(part))
            part.SetActive(true);
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
        RefreshPlaneEffects();
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
        RefreshPlaneEffects();
        SuppressAllUpgradeVfx();
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
        planeController.wreckDrag = settings.wreckDrag;
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

    private void RefreshPlaneEffects()
    {
        if (planeController == null)
            return;

        PlaneEffects planeEffects = planeController.GetComponent<PlaneEffects>();
        if (planeEffects != null)
            planeEffects.RefreshFlightTrails();
    }
}
