using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public CameraManager cameraManager;
    [Tooltip("Per-scene unlockable parts (body stays default). Assign one config per scene.")]
    public PlaneUpgradeConfig planeUpgradeConfig;
    public TextMeshProUGUI coinText;
    public Slider upgradeSlider;
    public Button upgradeButton;
    public TextMeshProUGUI costText;
    public AudioManager audioManager;
    public GameObject taptoplay;
    public Button BackBtn;
    public Button boostEnableBtn;
    //public TextMeshProUGUI boostCostText;
    public GameObject PlaneBoosters;
    public Button increaseLaunchForceBtn;
    public TextMeshProUGUI launchForceCostText;
    public TextMeshProUGUI launchForceLevelText;
    public Slider launchForceSlider;
    [Header("Coin Multiplier Upgrade")]
    public Button increaseCoinMultiplierBtn;
    public TextMeshProUGUI coinMultiplierCostText;
    public TextMeshProUGUI coinMultiplierLevelText;
    public Slider coinMultiplierSlider;
    public SimpleDragLauncher dragLauncher;
    public GameObject boostactive;
    public GameObject SettingTab;
    public GameObject levelsPanel;
    public LevelsUI levelsUI;
    public Button levelBtn;

    public GameObject notEnoughCoinsU;

    [Header("Boost Button")]
    [SerializeField] private float boostSlideOffsetX = 500f;
    [SerializeField] private float boostSlideDuration = 0.4f;

    [Header("Timing")]
    public float cameraTransitionDuration = 1.5f;
    public float particleEffectDuration = 1.0f;

    [Header("Upgrade Particle")]
    [Tooltip("Spawned at the part when upgrading if PlaneUpgradeConfig has no in-scene VFX. Assign a particle prefab.")]
    public GameObject upgradeParticleEffect;
    public float upgradeParticleYOffset = 0f;
    [Tooltip("Uniform size of the spawned upgrade particle.")]
    public float upgradeParticleScale = 0.12f;

    [Header("Debug")]
    [SerializeField] private bool debugUpgradeVfx = true;


    private int currentIndex = 0;
    private int clickCount = 0;
    private const int clicksRequired = 5;
    private float currentCost = 10;
    private int playerCoins;
 // private AudioSource audioSource;
    private bool isUpgrading = false;
    private Image menuPanelImage;

    public bool IsUpgrading => isUpgrading;
    
    // Launch force level system
    private int launchForceLevel = 1;
    private int launchForceClickCount = 0;
    private const int maxLaunchForceLevel = 3;
    private readonly float[] launchForceLevels = { 25f, 30f, 35f };
    private readonly int[] launchForceCosts = { 700, 1000, 1500 };

    private int coinMultiplierLevel = 1;
    private int coinMultiplierClickCount = 0;
    private const int maxCoinMultiplierLevel = 11;
    private const float coinMultiplierStep = 0.1f;
    private readonly int[] coinMultiplierCosts = { 600, 900, 1200, 1800, 2500, 3500, 5000, 7000, 10000, 15000, 20000 };
    private const int BoostCost = 500;

    private RectTransform boostButtonRect;
    private Vector2 boostButtonRestPosition;
    private bool boostButtonRestCaptured;
    private bool boostButtonShown;
    private Coroutine boostButtonSlideCoroutine;

    void Awake()
    {
        menuPanelImage = GetComponent<Image>();
        ResolveSceneReferences();
    }

    void OnEnable()
    {
        ResolveSceneReferences();
        ResolveUIReferences();
        CacheUpgradeButtonTransitions();
        EnsureUpgradeShakeComponents();
        RefreshEconomyUI();
    }

    void Start()
    {
        ResolveSceneReferences();
        ResolveUIReferences();

        if (LevelProgress.ConsumeGameplayResetPending())
            ResetUpgradesForNewLevel();
        else
            InitializeFromSavedProgress();

        taptoplay.SetActive(true);
        CaptureBoostButtonRestPosition();
        CacheUpgradeButtonTransitions();
        EnsureUpgradeShakeComponents();
        if (boostEnableBtn != null)
        {
            boostEnableBtn.gameObject.SetActive(false);
            boostButtonShown = false;
        }

        RefreshAllUpgradeUI();
    }

    public string[] GetUpgradePartNames()
    {
        return planeUpgradeConfig != null
            ? planeUpgradeConfig.GetPartNames()
            : System.Array.Empty<string>();
    }

    public void ResetUpgradesForNewLevel()
    {
        isUpgrading = false;
        currentIndex = 0;
        clickCount = 0;
        currentCost = 10f;
        launchForceLevel = 1;
        launchForceClickCount = 0;
        coinMultiplierLevel = 1;
        coinMultiplierClickCount = 0;

        if (planeUpgradeConfig != null)
        {
            foreach (PlaneUpgradePartEntry entry in planeUpgradeConfig.upgradeParts)
            {
                if (entry?.part != null)
                    entry.part.SetActive(false);
            }

            planeUpgradeConfig.ApplyGlideForCurrentUnlocks();
        }

        if (PlaneBoosters != null)
            PlaneBoosters.SetActive(false);
        if (boostactive != null)
            boostactive.SetActive(false);

        if (dragLauncher != null)
            dragLauncher.launchForceMultiplier = launchForceLevels[0];

        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierLevelKey, 1);
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierClickCountKey, 0);
        PlayerPrefs.SetFloat(LevelProgress.CoinMultiplierValueKey, 1f);
        SaveLaunchForceProgress();
        SaveProgress();
        PlayerPrefs.Save();

        ShowUpgradeButtons();
        HideBoostButtonInstant();
        SetLevelsPanelOpen(false);
        SyncPlayerCoins();
        RefreshAllUpgradeUI();
    }

    private void InitializeFromSavedProgress()
    {
        LoadProgress();
        MigrateLegacyProgressIfNeeded();

        if (planeUpgradeConfig != null)
            planeUpgradeConfig.ApplyPartStatesFromSave();

        ReconcileUpgradeIndexFromParts();
        currentIndex = Mathf.Clamp(currentIndex, 0, GetUpgradePartCount());

        if (IsFullyUpgraded())
            SetMaxStateUI();

        LoadLaunchForceLevel();
        LoadCoinMultiplierLevel();
    }

    private void ShowUpgradeButtons()
    {
        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(true);
        if (increaseLaunchForceBtn != null)
            increaseLaunchForceBtn.gameObject.SetActive(true);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(true);
    }

    private Selectable.Transition upgradeButtonTransition = Selectable.Transition.ColorTint;
    private Selectable.Transition launchForceButtonTransition = Selectable.Transition.ColorTint;
    private Selectable.Transition coinMultiplierButtonTransition = Selectable.Transition.ColorTint;
    private bool upgradeButtonTransitionsCached;

    private void CacheUpgradeButtonTransitions()
    {
        if (upgradeButtonTransitionsCached)
            return;

        if (upgradeButton != null)
            upgradeButtonTransition = upgradeButton.transition;
        if (increaseLaunchForceBtn != null)
            launchForceButtonTransition = increaseLaunchForceBtn.transition;
        if (increaseCoinMultiplierBtn != null)
            coinMultiplierButtonTransition = increaseCoinMultiplierBtn.transition;

        upgradeButtonTransitionsCached = true;
    }

    private void ApplyUpgradeButtonState(Button button, Selectable.Transition normalTransition, bool purchasable, bool atMax)
    {
        if (button == null)
            return;

        CacheUpgradeButtonTransitions();
        bool canPurchase = purchasable && !atMax;
        button.interactable = !atMax;
        button.transition = canPurchase ? normalTransition : Selectable.Transition.None;

        ButtonScaleAnimation scaleAnim = button.GetComponent<ButtonScaleAnimation>();
        if (scaleAnim != null)
            scaleAnim.enabled = canPurchase;

        if (!canPurchase && button.targetGraphic != null)
            button.targetGraphic.CrossFadeColor(button.colors.normalColor, 0f, true, true);
    }

    private void EnsureUpgradeShakeComponents()
    {
        EnsureShakeComponent(upgradeButton);
        EnsureShakeComponent(increaseLaunchForceBtn);
        EnsureShakeComponent(increaseCoinMultiplierBtn);
    }

    private static void EnsureShakeComponent(Button button)
    {
        if (button != null && button.GetComponent<ButtonShakeAnimation>() == null)
            button.gameObject.AddComponent<ButtonShakeAnimation>();
    }

    private void PlayInsufficientCoinsShake(Button button)
    {
        if (button == null)
            return;

        ButtonShakeAnimation shake = button.GetComponent<ButtonShakeAnimation>();
        if (shake != null)
            shake.Play();
    }

    private void ResolveSceneReferences()
    {
        if (planeUpgradeConfig == null)
            planeUpgradeConfig = FindObjectOfType<PlaneUpgradeConfig>();

        if (dragLauncher == null)
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();
    }

    private void ResolveUIReferences()
    {
        if (costText == null && upgradeButton != null)
        {
            Transform costTransform = upgradeButton.transform.Find("Cost");
            if (costTransform != null)
                costText = costTransform.GetComponent<TextMeshProUGUI>();
        }
    }

    private void RefreshAllUpgradeUI()
    {
        UpdateCoinUI();
        UpdateCostUI();
        UpdateSliderUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateBoostCostUI();
        UpdateLaunchForceCostUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceSliderUI();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateCoinMultiplierCostUI();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUpgrading)
            return;

        Debug.Log("Panel clicked");
        cameraManager.TransitionToStartCamPos();
    }

    private void SetMenuTapInputEnabled(bool enabled)
    {
        if (menuPanelImage != null)
            menuPanelImage.raycastTarget = enabled;
    }

    private void BeginUpgradeInputBlock()
    {
        isUpgrading = true;
        SetMenuTapInputEnabled(false);
        if (dragLauncher != null)
            dragLauncher.SetDragEnabled(false);
    }

    private void EndUpgradeInputBlock()
    {
        if (cameraManager != null && cameraManager.MainMenu != null)
            cameraManager.MainMenu.SetActive(true);

        SetMenuTapInputEnabled(true);
        isUpgrading = false;
    }

    public void ActivateNextPart()
    {
        if (isUpgrading)
            return;

        if (IsFullyUpgraded())
        {
            SetMaxStateUI();
            return;
        }

        SyncPlayerCoins();
        if (playerCoins < currentCost)
        {
            PlayInsufficientCoinsShake(upgradeButton);
            return;
        }

        if (!TrySpendCoins((int)currentCost))
            return;

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();

        // Progress and cost update
        clickCount++;
        currentCost *= 1.5f;

        UpdateCoinUI();
        UpdateCostUI();
        UpdateSliderUI();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();

        // Activate part if complete
        if (clickCount >= clicksRequired)
        {
            StartCoroutine(UpgradeSequence());
        }
        else
        {
            SaveProgress();
            UpdateButtonInteractable();
        }
    }

    private IEnumerator UpgradeSequence()
    {
        BeginUpgradeInputBlock();
        upgradeButton.interactable = false;
        taptoplay.SetActive(false);
        yield return new WaitForSeconds(0.3f);
        HideBoostButtonInstant();
        upgradeButton.gameObject.SetActive(false);
        increaseLaunchForceBtn.gameObject.SetActive(false);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(false);

        // Step 1: Transition camera to the part
        Transform focusPoint = planeUpgradeConfig != null
            ? planeUpgradeConfig.GetFocusPoint(currentIndex)
            : null;
        if (focusPoint != null)
            yield return StartCoroutine(cameraManager.TransitionToTarget(focusPoint, cameraTransitionDuration));

        // Step 2: Play upgrade VFX (unlocks part inside routine on the same frame as the effect)
        GameObject part = planeUpgradeConfig != null ? planeUpgradeConfig.GetPart(currentIndex) : null;
        audioManager.PlanepartSFX();
        yield return StartCoroutine(PlayPartUpgradeParticlesRoutine(currentIndex, part));

        currentIndex++;
        clickCount = 0;
        UpdateSliderUI();
        UpdateCostUI();

        // Step 4: Transition camera back to main menu
        yield return StartCoroutine(cameraManager.TransitionToTarget(cameraManager.mainMenuPosition, cameraTransitionDuration));
        taptoplay.SetActive(true);
        upgradeButton.gameObject.SetActive(true);
        increaseLaunchForceBtn.gameObject.SetActive(true);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(true);

        if (IsFullyUpgraded())
            SetMaxStateUI();

        SaveProgress();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        EndUpgradeInputBlock();
    }

    // -----------------------------
    // 🧠 UI Helpers
    // -----------------------------

    private int GetUpgradePartCount()
    {
        return planeUpgradeConfig != null ? planeUpgradeConfig.PartCount : 0;
    }

    private bool IsFullyUpgraded()
    {
        int partCount = GetUpgradePartCount();
        return partCount > 0 && currentIndex >= partCount;
    }

    private void ReconcileUpgradeIndexFromParts()
    {
        if (planeUpgradeConfig == null || planeUpgradeConfig.upgradeParts == null)
            return;

        for (int i = 0; i < planeUpgradeConfig.upgradeParts.Length; i++)
        {
            GameObject part = planeUpgradeConfig.GetPart(i);
            if (part != null && PlaneUpgradeConfig.IsPartUnlocked(part))
                currentIndex = Mathf.Max(currentIndex, i + 1);
        }
    }

    private IEnumerator PlayPartUpgradeParticlesRoutine(int index, GameObject part)
    {
        if (part == null)
        {
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        if (!part.activeSelf && planeUpgradeConfig != null)
            planeUpgradeConfig.UnlockPart(index);

        var spawnedInstances = new List<GameObject>();
        var particleSystems = new List<ParticleSystem>();
        var trails = new List<TrailRenderer>();

        TryAddUpgradeParticlePrefab(upgradeParticleEffect, part, spawnedInstances, particleSystems);

        // In-scene particle systems on the part (Wingtrail PS children, etc.)
        AddParticleSystemsFromRoot(part, particleSystems);

        // Trails are supplementary only — never block smoke from spawning
        AddTrailsFromRoot(part, trails);

        bool hasParticles = particleSystems.Count > 0;
        bool hasTrails = trails.Count > 0;

        if (!hasParticles && !hasTrails)
        {
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        ActivateVfxChildren(part);
        yield return null;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null || !IsSceneInstance(ps.gameObject))
                continue;

            ps.gameObject.SetActive(true);
            var emission = ps.emission;
            emission.enabled = true;
            ps.Clear(true);
            ps.Play(true);
        }

        foreach (TrailRenderer trail in trails)
        {
            if (trail == null || !IsSceneInstance(trail.gameObject))
                continue;

            trail.gameObject.SetActive(true);
            trail.emitting = false;
            trail.Clear();
            trail.emitting = true;
        }

        float waitDuration = Mathf.Max(
            particleEffectDuration,
            GetParticleDuration(particleSystems.ToArray()),
            GetTrailDuration(trails.ToArray()));

        yield return new WaitForSeconds(waitDuration > 0f ? waitDuration : particleEffectDuration);

        foreach (GameObject spawned in spawnedInstances)
        {
            if (spawned != null)
                Destroy(spawned, 2f);
        }
    }

    private void TryAddUpgradeParticlePrefab(
        GameObject prefab,
        GameObject part,
        List<GameObject> spawnedInstances,
        List<ParticleSystem> particleSystems)
    {
        if (prefab == null || part == null)
            return;

        if (IsSceneInstance(prefab))
        {
            EnsureActiveForPlayback(prefab, part);
            AddParticleSystemsFromRoot(prefab, particleSystems);
            return;
        }

        GameObject instance = Instantiate(prefab, part.transform);
        instance.transform.localPosition = GetUpgradeParticleLocalPosition(part);
        instance.transform.localRotation = Quaternion.identity;
        float scale = Mathf.Max(0.01f, upgradeParticleScale);
        instance.transform.localScale = Vector3.one * scale;
        instance.SetActive(true);
        spawnedInstances.Add(instance);
        AddParticleSystemsFromRoot(instance, particleSystems);
    }

    private Vector3 GetUpgradeParticleLocalPosition(GameObject part)
    {
        if (part == null)
            return new Vector3(0f, upgradeParticleYOffset, 0f);

        Vector3 localPosition = GetPartVisualCenterLocal(part);
        localPosition.y += upgradeParticleYOffset;
        return localPosition;
    }

    private static Vector3 GetPartVisualCenterLocal(GameObject part)
    {
        Transform partTransform = part.transform;
        bool hasBounds = false;
        Bounds worldBounds = default;

        foreach (MeshFilter meshFilter in part.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            EncapsulateLocalMeshBounds(meshFilter.transform, meshFilter.sharedMesh.bounds, ref worldBounds, ref hasBounds);
        }

        foreach (SkinnedMeshRenderer skinnedMesh in part.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skinnedMesh == null || skinnedMesh.sharedMesh == null)
                continue;

            EncapsulateLocalMeshBounds(skinnedMesh.transform, skinnedMesh.sharedMesh.bounds, ref worldBounds, ref hasBounds);
        }

        if (hasBounds)
            return partTransform.InverseTransformPoint(worldBounds.center);

        foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer is TrailRenderer || renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                worldBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return partTransform.InverseTransformPoint(worldBounds.center);

        foreach (Collider collider in part.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null)
                continue;

            if (!hasBounds)
            {
                worldBounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(collider.bounds);
            }
        }

        if (hasBounds)
            return partTransform.InverseTransformPoint(worldBounds.center);

        return Vector3.zero;
    }

    private static void EncapsulateLocalMeshBounds(
        Transform meshTransform,
        Bounds localMeshBounds,
        ref Bounds worldBounds,
        ref bool hasBounds)
    {
        Vector3 center = localMeshBounds.center;
        Vector3 extents = localMeshBounds.extents;

        Vector3[] corners =
        {
            center + new Vector3(extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, -extents.y, -extents.z)
        };

        foreach (Vector3 localCorner in corners)
        {
            Vector3 worldCorner = meshTransform.TransformPoint(localCorner);
            if (!hasBounds)
            {
                worldBounds = new Bounds(worldCorner, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(worldCorner);
            }
        }
    }

    private static void AddParticleSystemsFromRoot(GameObject root, List<ParticleSystem> particleSystems)
    {
        if (root == null || particleSystems == null)
            return;

        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps != null && IsSceneInstance(ps.gameObject) && !particleSystems.Contains(ps))
                particleSystems.Add(ps);
        }
    }

    private static void AddTrailsFromRoot(GameObject root, List<TrailRenderer> trails)
    {
        if (root == null || trails == null)
            return;

        foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true))
        {
            if (trail != null && IsSceneInstance(trail.gameObject) && !trails.Contains(trail))
                trails.Add(trail);
        }
    }

    private static bool IsSceneInstance(GameObject go)
    {
        return go != null && go.scene.IsValid();
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return "null";

        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private static void EnsureActiveForPlayback(GameObject target, GameObject stopAtAncestor)
    {
        if (target == null)
            return;

        target.SetActive(true);

        Transform t = target.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);

            if (stopAtAncestor != null && t.gameObject == stopAtAncestor)
                break;

            t = t.parent;
        }
    }

    private static int ActivateVfxChildren(GameObject part)
    {
        if (part == null)
            return 0;

        int activated = 0;
        foreach (Transform child in part.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject == part)
                continue;

            bool isVfxChild = child.GetComponent<ParticleSystem>() != null
                || child.GetComponent<TrailRenderer>() != null;

            if (isVfxChild && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
                activated++;
            }
        }

        return activated;
    }

    private static float GetTrailDuration(TrailRenderer[] trails)
    {
        if (trails == null || trails.Length == 0)
            return 0f;

        float maxDuration = 0f;
        foreach (TrailRenderer trail in trails)
        {
            if (trail == null)
                continue;

            if (trail.time > maxDuration)
                maxDuration = trail.time;
        }

        return maxDuration;
    }

    private static float GetParticleDuration(ParticleSystem[] systems)
    {
        if (systems == null || systems.Length == 0)
            return 0f;

        float maxDuration = 0f;
        foreach (ParticleSystem ps in systems)
        {
            ParticleSystem.MainModule main = ps.main;
            float lifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax
                : main.startLifetime.constant;
            float duration = main.duration + lifetime;
            if (duration > maxDuration)
                maxDuration = duration;
        }

        return maxDuration;
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = $"{FormatNumber(playerCoins)}";
    }

    private void UpdateCostUI()
    {
        if (costText != null)
        {
            if (IsFullyUpgraded())
                costText.text = "MAX";
            else
                costText.text = $"{FormatNumber(currentCost)}";
        }
    }

    private void UpdateSliderUI()
    {
        if (upgradeSlider != null)
        {
            if (IsFullyUpgraded())
            {
                upgradeSlider.value = upgradeSlider.maxValue;
            }
            else
            {
                upgradeSlider.minValue = 0;
                upgradeSlider.maxValue = clicksRequired;
                upgradeSlider.value = clickCount;
            }
        }
    }

    private void UpdateButtonInteractable()
    {
        if (upgradeButton != null)
        {
            int partCount = GetUpgradePartCount();
            bool canAfford = playerCoins >= currentCost && currentIndex < partCount;
            bool atMax = IsFullyUpgraded();
            ApplyUpgradeButtonState(upgradeButton, upgradeButtonTransition, canAfford, atMax);

            bool hasEnoughCoins = playerCoins >= currentCost && !IsFullyUpgraded();
            if (notEnoughCoinsU != null)
                notEnoughCoinsU.SetActive(!hasEnoughCoins && !IsFullyUpgraded());
        }
    }

    private void UpdateBoostCostUI()
    {
        // if (boostCostText != null)
        // {
        //     boostCostText.text = BoostCost.ToString();
        // }
    }
    
    private void UpdateLaunchForceCostUI()
    {
        if (launchForceCostText != null)
        {
            if (launchForceLevel >= maxLaunchForceLevel)
                launchForceCostText.text = "MAX";
            else
                launchForceCostText.text = launchForceCosts[launchForceLevel - 1].ToString();
        }
    }
    
    private void UpdateLaunchForceLevelUI()
    {
        if (launchForceLevelText != null)
        {
            launchForceLevelText.text = $"{launchForceLevel}";
        }
    }
    
    private void UpdateLaunchForceSliderUI()
    {
        if (launchForceSlider == null)
            return;

        if (launchForceLevel >= maxLaunchForceLevel)
        {
            launchForceSlider.minValue = 0;
            launchForceSlider.maxValue = 1;
            launchForceSlider.value = 1f;
            return;
        }

        launchForceSlider.minValue = 0;
        launchForceSlider.maxValue = clicksRequired;
        launchForceSlider.value = launchForceClickCount;
    }

    private int GetLaunchForceClickCost()
    {
        if (launchForceLevel >= maxLaunchForceLevel)
            return 0;

        return launchForceCosts[launchForceLevel - 1];
    }

    private void SaveLaunchForceProgress()
    {
        PlayerPrefs.SetInt(LevelProgress.GetLaunchForceLevelKey(), launchForceLevel);
        PlayerPrefs.SetInt(LevelProgress.GetLaunchForceClickCountKey(), launchForceClickCount);

        if (dragLauncher != null)
            PlayerPrefs.SetFloat(LevelProgress.GetLaunchForceMultiplierKey(), dragLauncher.launchForceMultiplier);

        PlayerPrefs.Save();
    }
    
    private void CaptureBoostButtonRestPosition()
    {
        if (boostEnableBtn == null)
            return;

        boostButtonRect = boostEnableBtn.GetComponent<RectTransform>();
        if (boostButtonRect == null || boostButtonRestCaptured)
            return;

        boostButtonRestPosition = boostButtonRect.anchoredPosition;
        boostButtonRestCaptured = true;
    }

    private void UpdateBoostButtonInteractable()
    {
        if (boostEnableBtn == null)
            return;

        CaptureBoostButtonRestPosition();
        if (boostButtonRect == null)
            return;

        bool boostersInactive = PlaneBoosters == null || !PlaneBoosters.activeSelf;
        bool canShowBoost = playerCoins >= BoostCost && boostersInactive;
        boostEnableBtn.interactable = canShowBoost;

        if (canShowBoost)
            ShowBoostButtonSlideIn();
        else
            HideBoostButtonSlideOut();
    }

    private void ShowBoostButtonSlideIn()
    {
        if (boostButtonRect == null)
            return;

        boostEnableBtn.gameObject.SetActive(true);
        if (boostButtonShown && boostButtonSlideCoroutine == null)
            return;

        if (boostButtonSlideCoroutine != null)
            StopCoroutine(boostButtonSlideCoroutine);

        if (!isActiveAndEnabled)
            return;

        boostButtonSlideCoroutine = StartCoroutine(AnimateBoostButton(true));
    }

    private void HideBoostButtonSlideOut()
    {
        if (boostButtonRect == null)
            return;

        if (!boostButtonShown && !boostEnableBtn.gameObject.activeSelf)
            return;

        if (boostButtonSlideCoroutine != null)
            StopCoroutine(boostButtonSlideCoroutine);

        if (!isActiveAndEnabled)
            return;

        boostButtonSlideCoroutine = StartCoroutine(AnimateBoostButton(false));
    }

    private void HideBoostButtonInstant()
    {
        if (boostButtonSlideCoroutine != null)
        {
            StopCoroutine(boostButtonSlideCoroutine);
            boostButtonSlideCoroutine = null;
        }

        boostButtonShown = false;
        if (boostEnableBtn != null)
            boostEnableBtn.gameObject.SetActive(false);
    }

    private IEnumerator AnimateBoostButton(bool slideIn)
    {
        Vector2 hiddenPos = boostButtonRestPosition + new Vector2(boostSlideOffsetX, 0f);
        Vector2 start = slideIn ? hiddenPos : boostButtonRect.anchoredPosition;
        Vector2 end = slideIn ? boostButtonRestPosition : hiddenPos;

        if (slideIn)
        {
            boostEnableBtn.gameObject.SetActive(true);
            boostButtonRect.anchoredPosition = hiddenPos;
        }

        float elapsed = 0f;
        while (elapsed < boostSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / boostSlideDuration);
            boostButtonRect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        boostButtonRect.anchoredPosition = end;

        if (!slideIn)
            boostEnableBtn.gameObject.SetActive(false);

        boostButtonShown = slideIn;
        boostButtonSlideCoroutine = null;
    }

    private void UpdateIncreaseLaunchForceButtonInteractable()
    {
        if (increaseLaunchForceBtn != null)
        {
            bool isMaxLevel = launchForceLevel >= maxLaunchForceLevel;
            bool canAfford = !isMaxLevel && playerCoins >= GetLaunchForceClickCost();
            ApplyUpgradeButtonState(increaseLaunchForceBtn, launchForceButtonTransition, canAfford, isMaxLevel);
        }
    }

    private float GetCoinMultiplierValue()
    {
        return 1f + (coinMultiplierLevel - 1) * coinMultiplierStep;
    }

    private int GetCoinMultiplierClickCost()
    {
        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
            return 0;

        return coinMultiplierCosts[coinMultiplierLevel - 1];
    }

    private void SaveCoinMultiplierProgress()
    {
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierLevelKey, coinMultiplierLevel);
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierClickCountKey, coinMultiplierClickCount);
        PlayerPrefs.SetFloat(LevelProgress.CoinMultiplierValueKey, GetCoinMultiplierValue());
        PlayerPrefs.Save();
    }

    private void UpdateCoinMultiplierCostUI()
    {
        if (coinMultiplierCostText == null)
            return;

        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
            coinMultiplierCostText.text = "MAX";
        else
            coinMultiplierCostText.text = coinMultiplierCosts[coinMultiplierLevel - 1].ToString();
    }

    private void UpdateCoinMultiplierLevelUI()
    {
        if (coinMultiplierLevelText != null)
            coinMultiplierLevelText.text = $"{GetCoinMultiplierValue():0.#}x";
    }

    private void UpdateCoinMultiplierSliderUI()
    {
        if (coinMultiplierSlider == null)
            return;

        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
        {
            coinMultiplierSlider.minValue = 0;
            coinMultiplierSlider.maxValue = 1;
            coinMultiplierSlider.value = 1f;
            return;
        }

        coinMultiplierSlider.minValue = 0;
        coinMultiplierSlider.maxValue = clicksRequired;
        coinMultiplierSlider.value = coinMultiplierClickCount;
    }

    private void UpdateIncreaseCoinMultiplierButtonInteractable()
    {
        if (increaseCoinMultiplierBtn == null)
            return;

        bool isMaxLevel = coinMultiplierLevel >= maxCoinMultiplierLevel;
        bool canAfford = !isMaxLevel && playerCoins >= GetCoinMultiplierClickCost();
        ApplyUpgradeButtonState(increaseCoinMultiplierBtn, coinMultiplierButtonTransition, canAfford, isMaxLevel);
    }

    public void CheatCoins()
    {
        CoinManager.EnsureInstance();
        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(1000000);

        RefreshEconomyUI();
    }

    public void RefreshEconomyUI()
    {
        ResolveUIReferences();
        SyncPlayerCoins();
        UpdateCoinUI();
        UpdateCostUI();
        UpdateLaunchForceCostUI();
        UpdateCoinMultiplierCostUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
    }

    private void SyncPlayerCoins()
    {
        CoinManager.EnsureInstance();
        playerCoins = CoinManager.Instance != null
            ? CoinManager.Instance.GetCoins()
            : PlayerPrefs.GetInt(LevelProgress.CoinsKey, 0);
    }

    private bool TrySpendCoins(int amount)
    {
        SyncPlayerCoins();
        if (playerCoins < amount)
            return false;

        CoinManager.EnsureInstance();
        if (CoinManager.Instance != null)
        {
            if (!CoinManager.Instance.SpendCoins(amount))
                return false;
        }
        else
        {
            playerCoins -= amount;
            PlayerPrefs.SetInt(LevelProgress.CoinsKey, playerCoins);
            PlayerPrefs.Save();
        }

        SyncPlayerCoins();
        return true;
    }

    // -----------------------------
    // 🧠 SAVE / LOAD SYSTEM
    // -----------------------------

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(LevelProgress.GetUpgradeCurrentIndexKey(), currentIndex);
        PlayerPrefs.SetInt(LevelProgress.GetUpgradeClickCountKey(), clickCount);
        PlayerPrefs.SetFloat(LevelProgress.GetUpgradeCurrentCostKey(), currentCost);
        SyncPlayerCoins();
        PlayerPrefs.SetInt(LevelProgress.CoinsKey, playerCoins);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        currentIndex = PlayerPrefs.GetInt(LevelProgress.GetUpgradeCurrentIndexKey(), 0);
        clickCount = PlayerPrefs.GetInt(LevelProgress.GetUpgradeClickCountKey(), 0);
        currentCost = PlayerPrefs.GetFloat(LevelProgress.GetUpgradeCurrentCostKey(), 10f);
        SyncPlayerCoins();
    }

    private void MigrateLegacyProgressIfNeeded()
    {
        if (PlayerPrefs.HasKey(LevelProgress.GetUpgradeCurrentIndexKey()))
            return;

        if (!PlayerPrefs.HasKey("Upgrade_CurrentIndex"))
            return;

        currentIndex = PlayerPrefs.GetInt("Upgrade_CurrentIndex", currentIndex);
        clickCount = PlayerPrefs.GetInt("Upgrade_ClickCount", clickCount);
        currentCost = PlayerPrefs.GetFloat("Upgrade_CurrentCost", currentCost);

        if (planeUpgradeConfig != null)
        {
            foreach (PlaneUpgradePartEntry entry in planeUpgradeConfig.upgradeParts)
            {
                if (entry?.part == null)
                    continue;

                string legacyKey = entry.part.name + "_active";
                if (PlayerPrefs.GetInt(legacyKey, 0) == 1)
                    PlayerPrefs.SetInt(LevelProgress.GetPartActiveKey(entry.part.name), 1);
            }
        }

        SaveProgress();
    }

    // -----------------------------
    // 🏁 MAX STATE HANDLER
    // -----------------------------

    private void SetMaxStateUI()
    {
        if (costText != null)
            costText.text = "MAX";

        if (upgradeSlider != null)
        {
            upgradeSlider.minValue = 0;
            upgradeSlider.maxValue = 1;
            upgradeSlider.value = 1;
        }

        if (upgradeButton != null)
            ApplyUpgradeButtonState(upgradeButton, upgradeButtonTransition, false, true);
    }

    // -----------------------------
    // 🔢 Number Formatter
    // -----------------------------

    private string FormatNumber(float num)
    {
        if (num >= 1_000_000_000)
            return (num / 1_000_000_000f).ToString("0.#") + "B";
        if (num >= 1_000_000)
            return (num / 1_000_000f).ToString("0.#") + "M";
        if (num >= 1_000)
            return (num / 1_000f).ToString("0.#") + "K";
        return num.ToString("0");
    }

    public void BoostEnableBtn()
    {
        //audioSource.Play();
        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        bool boostersInactive = PlaneBoosters == null || !PlaneBoosters.activeSelf;
        if (boostersInactive && TrySpendCoins(BoostCost))
        {
            // Update coin UI
            UpdateCoinUI();
            
            if (PlaneBoosters != null)
                PlaneBoosters.SetActive(true);
            if (boostactive != null)
                boostactive.SetActive(true);
            
            // Update all button interactability and UI
            UpdateBoostCostUI();
            UpdateBoostButtonInteractable();
            UpdateButtonInteractable();
            UpdateIncreaseLaunchForceButtonInteractable();
            UpdateIncreaseCoinMultiplierButtonInteractable();
        }
    }

    private void LoadLaunchForceLevel()
    {
        launchForceLevel = PlayerPrefs.GetInt(LevelProgress.GetLaunchForceLevelKey(), 1);
        if (!PlayerPrefs.HasKey(LevelProgress.GetLaunchForceLevelKey()) && PlayerPrefs.HasKey("LaunchForceLevel"))
            launchForceLevel = PlayerPrefs.GetInt("LaunchForceLevel", launchForceLevel);

        launchForceClickCount = PlayerPrefs.GetInt(LevelProgress.GetLaunchForceClickCountKey(), 0);

        launchForceLevel = Mathf.Clamp(launchForceLevel, 1, maxLaunchForceLevel);
        launchForceClickCount = Mathf.Clamp(launchForceClickCount, 0, clicksRequired - 1);

        if (launchForceLevel >= maxLaunchForceLevel)
            launchForceClickCount = 0;

        if (dragLauncher != null)
            dragLauncher.launchForceMultiplier = launchForceLevels[launchForceLevel - 1];
    }
    
    public void IncreaseLaunchForce()
    {
        if (launchForceLevel >= maxLaunchForceLevel)
            return;

        int clickCost = GetLaunchForceClickCost();
        SyncPlayerCoins();
        if (playerCoins < clickCost)
        {
            PlayInsufficientCoinsShake(increaseLaunchForceBtn);
            Debug.Log("Not enough coins!");
            return;
        }

        if (!TrySpendCoins(clickCost))
            return;

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        UpdateCoinUI();

        launchForceClickCount++;

        if (launchForceClickCount >= clicksRequired)
        {
            launchForceClickCount = 0;
            launchForceLevel++;

            // Re-snap the plane to the ramp rest pose so the new slingshot state
            // doesn't leave it using an older drag/rotation pose.
            if (dragLauncher != null)
                dragLauncher.ResetForNewLaunch();

            // Update all UI
            UpdateLaunchForceCostUI();
            UpdateLaunchForceLevelUI();
            UpdateLaunchForceSliderUI();
            UpdateIncreaseLaunchForceButtonInteractable();
            UpdateButtonInteractable();
            UpdateBoostButtonInteractable();
            UpdateIncreaseCoinMultiplierButtonInteractable();

            Debug.Log($"Launch force upgraded to Level {launchForceLevel}! Force: {dragLauncher.launchForceMultiplier}");
            if (dragLauncher != null)
                dragLauncher.launchForceMultiplier = launchForceLevels[launchForceLevel - 1];
        }

        SaveLaunchForceProgress();

        UpdateLaunchForceCostUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceSliderUI();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
    }

    private void LoadCoinMultiplierLevel()
    {
        coinMultiplierLevel = PlayerPrefs.GetInt(LevelProgress.CoinMultiplierLevelKey, 1);
        coinMultiplierClickCount = PlayerPrefs.GetInt(LevelProgress.CoinMultiplierClickCountKey, 0);
        coinMultiplierLevel = Mathf.Clamp(coinMultiplierLevel, 1, maxCoinMultiplierLevel);
        coinMultiplierClickCount = Mathf.Clamp(coinMultiplierClickCount, 0, clicksRequired - 1);

        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
            coinMultiplierClickCount = 0;
    }

    public void IncreaseCoinMultiplier()
    {
        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
            return;

        int cost = GetCoinMultiplierClickCost();
        SyncPlayerCoins();
        if (playerCoins < cost)
        {
            PlayInsufficientCoinsShake(increaseCoinMultiplierBtn);
            Debug.Log("Not enough coins!");
            return;
        }

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();

        if (!TrySpendCoins(cost))
            return;

        UpdateCoinUI();

        coinMultiplierClickCount++;

        if (coinMultiplierClickCount >= clicksRequired)
        {
            coinMultiplierClickCount = 0;
            coinMultiplierLevel++;
        }

        SaveCoinMultiplierProgress();

        UpdateCoinMultiplierCostUI();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
    }

    public void SettingBtn(){

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        SettingTab.SetActive(true);
    }
       
    public void LevelBtn()
    {
        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        SetLevelsPanelOpen(true);
    }

    public void CloseLevelsPanel()
    {
        SetLevelsPanelOpen(false);
    }

    public void SetLevelsPanelOpen(bool open)
    {
        if (levelsPanel == null)
            return;

        levelsPanel.SetActive(open);

        if (open)
        {
            levelsPanel.transform.SetAsLastSibling();

            if (levelsUI != null)
                levelsUI.RefreshAllButtons();
        }

        UpdateLevelBtnState(open);
    }

    private void UpdateLevelBtnState(bool levelsPanelOpen)
    {
        if (levelBtn == null)
            return;

        levelBtn.gameObject.SetActive(!levelsPanelOpen);
    }
}

