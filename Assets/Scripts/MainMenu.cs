using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

[System.Serializable]
public class UpgradeAdVisualSet
{
    [Tooltip("Drag the whole coin/price section (e.g. CostS). Not the coin image or text alone.")]
    [FormerlySerializedAs("coinRow")]
    public GameObject coinSection;

    [Tooltip("Drag the whole AD section (parent with background + icon + FREE text). Not children separately.")]
    [FormerlySerializedAs("adRow")]
    public GameObject adSection;
}

public class MainMenu : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public CameraTransitionController cameraManager;
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
    public GameObject boostEnableBtnParent;
    public TextMeshProUGUI boostLevelText;
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
    public PlaneController planeController;
    public GameObject boostactive;
    public GameObject SettingTab;
    public GameObject levelsPanel;
    public LevelsUI levelsUI;
    public Button levelBtn;

    public GameObject notEnoughCoinsU;

    [Header("Rewarded Ad Upgrades")]
    [Tooltip("Assign whole sections only: CostS → Coin Section, AD → Ad Section.")]
    public UpgradeAdVisualSet planeUpgradeAdVisuals = new UpgradeAdVisualSet();
    public UpgradeAdVisualSet launchForceUpgradeAdVisuals = new UpgradeAdVisualSet();
    public UpgradeAdVisualSet coinMultiplierUpgradeAdVisuals = new UpgradeAdVisualSet();
    [Tooltip("Shown briefly when the player requests a rewarded ad that is not loaded yet.")]
    public TextMeshProUGUI adNotLoadedNotificationText;
    [SerializeField] private float adNotLoadedNotificationDuration = 2f;
    [SerializeField] private float adNotLoadedPopInDuration = 0.22f;
    [SerializeField] private float adNotLoadedPopOutDuration = 0.15f;
    [SerializeField] private float adNotLoadedPopOvershoot = 1.18f;

    [Header("Boost Button")]
    [SerializeField] private float boostSlideOffsetX = 500f;
    [SerializeField] private float boostSlideDuration = 0.4f;
    [SerializeField] private float boostMaxLevelHideDelay = 10f;

    [Header("Timing")]
    public float cameraTransitionDuration = 1.5f;
    public float particleEffectDuration = 1.0f;

    [Header("Upgrade Particle")]
    [Tooltip("Optional fallback if a part has no in-scene smoke child. Leave empty when each part has its own UpgradeSmoke child.")]
    public GameObject upgradeParticleEffect;
    public float upgradeParticleYOffset = 0f;
    [Tooltip("Uniform size of the spawned upgrade particle.")]
    public float upgradeParticleScale = 1f;

    [Header("Debug")]
    [SerializeField] private GameObject cheatCoinsButton;
    [SerializeField] private bool debugUpgradeVfx = false;

    private PartUpgradeSystem partUpgrades = new PartUpgradeSystem();
    private LaunchForceUpgradeSystem launchForceUpgrades = new LaunchForceUpgradeSystem();
    private BoostUpgradeSystem boostUpgrades = new BoostUpgradeSystem();
    private CoinMultiplierUpgradeSystem coinMultiplierUpgrades = new CoinMultiplierUpgradeSystem();

    // Keep the existing menu state names so all button flows and Inspector-driven setup remain intact.
    private int currentIndex { get => partUpgrades.CurrentIndex; set => partUpgrades.CurrentIndex = value; }
    private int clickCount { get => partUpgrades.ClickCount; set => partUpgrades.ClickCount = value; }
    private const int clicksRequired = UpgradeCostUtil.ClicksRequired;

    [Header("Upgrade Economy (set per scene)")]
    [Tooltip("First click cost for plane, slingshot, and income upgrades. Change this on each scene's MainMenu (e.g. Forest 50, Desert 100).")]
    [SerializeField] private int upgradeCostStart = 50;
    [Tooltip("Extra cost added on every click. Change per scene if the ladder should climb faster/slower.")]
    [SerializeField] private int upgradeCostIncrement = 25;

    private float currentCost => partUpgrades.CurrentCost;
    private int playerCoins;
 // private AudioSource audioSource;
    private bool isUpgrading = false;
    private Image menuPanelImage;

    public bool IsUpgrading => isUpgrading;
    
    [Header("Launch Force Upgrade")]
    [SerializeField] private float[] launchForceLevels = { 25f, 30f, 35f };
    [SerializeField] private int[] launchForceCosts = { 50, 175, 300 };
    [Tooltip("Optional slingshot band visual. Auto-resolved from drag launcher if empty.")]
    public RubberBandVisual rubberBandVisual;
    [Tooltip("Optional slingshot smoke spawn point. Auto-finds SlingshotCamPos if empty.")]
    public Transform slingshotUpgradeVfxPoint;

    [Header("Boost Upgrade")]
    [Tooltip("Boost level durations. Level 1 uses index 0, level 5 uses index 4.")]
    [SerializeField] private float[] boostDurations = { 2f, 2.5f, 3f, 3.5f, 4f };

    // Launch force level system
    private int launchForceLevel { get => launchForceUpgrades.Level; set => launchForceUpgrades.Level = value; }
    private int launchForceClickCount { get => launchForceUpgrades.ClickCount; set => launchForceUpgrades.ClickCount = value; }
    private float launchForceCurrentCost => launchForceUpgrades.CurrentCost;
    private int maxLaunchForceLevel => launchForceUpgrades.MaxLevel;

    private int boostLevel { get => boostUpgrades.Level; set => boostUpgrades.Level = value; }
    private int maxBoostLevel => boostUpgrades.MaxLevel;

    private int coinMultiplierLevel { get => coinMultiplierUpgrades.Level; set => coinMultiplierUpgrades.Level = value; }
    private int coinMultiplierClickCount { get => coinMultiplierUpgrades.ClickCount; set => coinMultiplierUpgrades.ClickCount = value; }
    private const float maxCoinMultiplierValue = 10f;
    private const float coinMultiplierStep = 0.2f;

    private static readonly Color UpgradeCostAffordableColor = Color.white;
    private static readonly Color UpgradeCostUnaffordableColor = new Color(1f, 0.55f, 0.55f);

    private RectTransform boostButtonRect;
    private Vector2 boostButtonRestPosition;
    private bool boostButtonRestCaptured;
    private bool boostButtonShown;
    private Coroutine boostButtonSlideCoroutine;
    private Coroutine boostButtonMaxLevelHideCoroutine;
    private Coroutine adNotLoadedNotificationCoroutine;
    private Vector3 adNotLoadedNotificationRestScale = Vector3.one;
    private bool adNotLoadedNotificationRestScaleCaptured;

    void Awake()
    {
        menuPanelImage = GetComponent<Image>();
        ResolveSceneReferences();
        EnsureLaunchForceConfig();
        EnsureBoostConfig();
        SyncUpgradeSystemRefs();
    }

    private void OnValidate()
    {
        EnsureLaunchForceConfig();
        EnsureBoostConfig();
        SyncUpgradeSystemRefs();
    }

    private void SyncUpgradeSystemRefs()
    {
        if (partUpgrades == null)
            partUpgrades = new PartUpgradeSystem();
        if (launchForceUpgrades == null)
            launchForceUpgrades = new LaunchForceUpgradeSystem();
        if (boostUpgrades == null)
            boostUpgrades = new BoostUpgradeSystem();
        if (coinMultiplierUpgrades == null)
            coinMultiplierUpgrades = new CoinMultiplierUpgradeSystem();

        partUpgrades.Config = planeUpgradeConfig;
        partUpgrades.CostStart = upgradeCostStart;
        partUpgrades.CostIncrement = upgradeCostIncrement;
        launchForceUpgrades.ForceLevels = launchForceLevels;
        launchForceUpgrades.CostStart = upgradeCostStart;
        launchForceUpgrades.CostIncrement = upgradeCostIncrement;
        boostUpgrades.Durations = boostDurations;
        coinMultiplierUpgrades.CostStart = upgradeCostStart;
        coinMultiplierUpgrades.CostIncrement = upgradeCostIncrement;
        coinMultiplierUpgrades.MaxValue = maxCoinMultiplierValue;
        coinMultiplierUpgrades.Step = coinMultiplierStep;
    }

    private void EnsureLaunchForceConfig()
    {
        if (launchForceLevels == null || launchForceLevels.Length == 0)
            launchForceLevels = new[] { 25f, 30f, 35f };

        if (launchForceCosts == null || launchForceCosts.Length == 0)
            launchForceCosts = new int[launchForceLevels.Length];

        if (launchForceCosts.Length != launchForceLevels.Length)
        {
            int[] resizedCosts = new int[launchForceLevels.Length];
            for (int i = 0; i < resizedCosts.Length; i++)
                resizedCosts[i] = i < launchForceCosts.Length ? launchForceCosts[i] : launchForceCosts[launchForceCosts.Length - 1];
            launchForceCosts = resizedCosts;
        }

        for (int i = 0; i < launchForceCosts.Length; i++)
            launchForceCosts[i] = GetTrackedUpgradeCost(upgradeCostStart, i * clicksRequired);
    }

    private int GetTrackedUpgradeCost(int startCost, int stepIndex)
    {
        return UpgradeCostUtil.GetTrackedUpgradeCost(startCost, upgradeCostIncrement, stepIndex);
    }

    private int GetPlaneUpgradeStepIndex()
    {
        return partUpgrades.GetStepIndex();
    }

    private int GetPlaneUpgradeClickCost()
    {
        return partUpgrades.GetClickCost();
    }

    private void RefreshPlaneUpgradeCost()
    {
        partUpgrades.RefreshCost();
    }

    private int GetLaunchForceStepIndex()
    {
        return launchForceUpgrades.GetStepIndex();
    }

    private void RefreshLaunchForceUpgradeCost()
    {
        launchForceUpgrades.RefreshCost();
    }

    private float GetLaunchForceMultiplierForLevel(int level)
    {
        return launchForceUpgrades.GetForceForLevel(level);
    }

    private int GetLaunchForceCostForLevel(int level)
    {
        EnsureLaunchForceConfig();
        return GetTrackedUpgradeCost(upgradeCostStart, (level - 1) * clicksRequired);
    }

    private void EnsureBoostConfig()
    {
        if (boostDurations == null || boostDurations.Length == 0)
            boostDurations = new[] { 2f, 2.5f, 3f, 3.5f, 4f };
    }

    private float GetBoostDurationForLevel(int level)
    {
        return boostUpgrades.GetDurationForLevel(level);
    }

    void OnEnable()
    {
        ResolveSceneReferences();
        SyncUpgradeSystemRefs();
        ResolveUIReferences();
        CacheUpgradeButtonTransitions();
        EnsureUpgradeShakeComponents();
        ResetUpgradeAdUiForMainMenu();
        SetupRewardedAdUpgrades();
        SubscribeToRewardedAdEvents();
        KeepRewardedAdReadyInBackground();
        ApplyCheatCoinsButtonVisibility();
        RefreshEconomyUI();
    }

    void OnDisable()
    {
        UnsubscribeFromRewardedAdEvents();
        HideAdNotLoadedNotificationInstant();
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
        GameObject boostSlideTarget = GetBoostSlideTarget();
        if (boostSlideTarget != null)
        {
            boostSlideTarget.SetActive(false);
            boostButtonShown = false;
        }
        if (boostEnableBtn != null)
            boostEnableBtn.gameObject.SetActive(true);

        ApplyCheatCoinsButtonVisibility();
        SetupRewardedAdUpgrades();
        KeepRewardedAdReadyInBackground();
        RefreshAllUpgradeUI();
        LogUpgradeVfxStartup();
    }

    private void LogUpgradeVfxStartup()
    {
        if (!debugUpgradeVfx)
            return;

        string prefabInfo = upgradeParticleEffect != null
            ? $"{upgradeParticleEffect.name} (sceneInstance={IsSceneInstance(upgradeParticleEffect)})"
            : "NOT ASSIGNED";

        int partCount = planeUpgradeConfig != null ? planeUpgradeConfig.PartCount : 0;
        Debug.Log(
            $"[UpgradeVFX] Startup: prefab={prefabInfo}, scale={upgradeParticleScale}, yOffset={upgradeParticleYOffset}, " +
            $"planeUpgradeConfig={(planeUpgradeConfig != null ? planeUpgradeConfig.name : "NULL")}, partCount={partCount}",
            this);

        if (planeUpgradeConfig == null || partCount == 0)
            Debug.LogWarning("[UpgradeVFX] Startup: PlaneUpgradeConfig missing or has no upgrade parts.", this);

        if (planeUpgradeConfig != null)
        {
            for (int i = 0; i < planeUpgradeConfig.PartCount; i++)
            {
                GameObject part = planeUpgradeConfig.GetPart(i);
                GameObject vfx = planeUpgradeConfig.GetUpgradeVfxRoot(i);
                Debug.Log(
                    $"[UpgradeVFX] Startup part[{i}]: part='{part?.name ?? "null"}' vfxChild='{vfx?.name ?? "NOT FOUND"}'",
                    this);
            }
        }
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
        SyncUpgradeSystemRefs();
        partUpgrades.Reset();
        launchForceUpgrades.Reset();
        boostUpgrades.Reset();
        coinMultiplierUpgrades.Reset();

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

        ApplyBoostSettings();

        if (dragLauncher != null)
            dragLauncher.launchForceMultiplier = GetLaunchForceMultiplierForLevel(1);

        GetRubberBandVisual()?.ApplyLaunchForceLevel(1);

        UpgradeSaveStore.ResetCoinMultiplierDefaults();
        SaveLaunchForceProgress();
        SaveBoostProgress();
        SaveProgress();
        PlayerPrefs.Save();

        ShowUpgradeButtons();
        HideBoostButtonInstant();
        SetLevelsPanelOpen(false);
        ClearAllUpgradeAdOffersAndRefresh();
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
        LoadBoostLevel();
        LoadCoinMultiplierLevel();
        MigrateUpgradeAdsUnlockedFromProgress();
        planeUpgradeConfig?.SuppressAllUpgradeVfx();
    }

    private void MigrateUpgradeAdsUnlockedFromProgress()
    {
        if (LevelProgress.AreUpgradeAdsUnlocked())
            return;

        bool hasUpgradeProgress = clickCount > 0
            || currentIndex > 0
            || launchForceClickCount > 0
            || launchForceLevel > 1
            || coinMultiplierClickCount > 0
            || coinMultiplierLevel > 1;

        if (hasUpgradeProgress)
            LevelProgress.MarkUpgradeAdsUnlocked();
    }

    private bool AreUpgradeAdsUnlocked()
    {
        return LevelProgress.AreUpgradeAdsUnlocked();
    }

    private void MarkUpgradeAdsUnlocked()
    {
        LevelProgress.MarkUpgradeAdsUnlocked();
    }

    private void ResetUpgradeAdUiForMainMenu()
    {
        ClearAllUpgradeAdOffers();
        ResetUpgradeAdVisuals();
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

    private enum UpgradeAdOfferType
    {
        PlanePart,
        LaunchForce,
        CoinMultiplier
    }

    /// <summary>
    /// AD row is enabled only after the player spends coins on this button and
    /// can no longer afford the next click. Cleared after watching an ad or leaving to play.
    /// </summary>
    private bool planeUpgradeAdEnabled;
    private bool launchForceUpgradeAdEnabled;
    private bool coinMultiplierUpgradeAdEnabled;
    private bool isShowingUpgradeAd;
    private bool isWaitingForRewardedAd;
    private float upgradeAdRefreshTimer;
    private float rewardedPreloadTimer;

    private bool IsUpgradeAdEnabled(UpgradeAdOfferType offerType)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return planeUpgradeAdEnabled;
            case UpgradeAdOfferType.LaunchForce:
                return launchForceUpgradeAdEnabled;
            case UpgradeAdOfferType.CoinMultiplier:
                return coinMultiplierUpgradeAdEnabled;
            default:
                return false;
        }
    }

    private void SetUpgradeAdEnabled(UpgradeAdOfferType offerType, bool enabled)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                planeUpgradeAdEnabled = enabled;
                break;
            case UpgradeAdOfferType.LaunchForce:
                launchForceUpgradeAdEnabled = enabled;
                break;
            case UpgradeAdOfferType.CoinMultiplier:
                coinMultiplierUpgradeAdEnabled = enabled;
                break;
        }
    }

    private void ClearAllUpgradeAdOffers()
    {
        planeUpgradeAdEnabled = false;
        launchForceUpgradeAdEnabled = false;
        coinMultiplierUpgradeAdEnabled = false;
    }

    private bool IsUpgradeAtMax(UpgradeAdOfferType offerType)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return IsFullyUpgraded();
            case UpgradeAdOfferType.LaunchForce:
                return launchForceLevel >= maxLaunchForceLevel;
            case UpgradeAdOfferType.CoinMultiplier:
                return IsCoinMultiplierMax();
            default:
                return true;
        }
    }

    private void UpdateAdEligibilityAfterPaidUpgrade(UpgradeAdOfferType offerType)
    {
        SyncPlayerCoins();
        MarkUpgradeAdsUnlocked();

        SetUpgradeAdEnabled(offerType, false);

        if (!IsUpgradeAtMax(offerType) && !CanAffordUpgrade(offerType))
        {
            SetUpgradeAdEnabled(offerType, true);
            KeepRewardedAdReadyInBackground();
        }

        RefreshUpgradeAdStates();
    }

    /// <summary>
    /// After a full unlock (wing, slingshot level, etc.) ads stay off until the player
    /// spends coins on a normal upgrade click again.
    /// </summary>
    private void SuppressAdAfterFullUpgrade(UpgradeAdOfferType offerType)
    {
        SetUpgradeAdEnabled(offerType, false);
        RefreshUpgradeAdStates();
    }

    private void ClearUpgradeAdOffer(UpgradeAdOfferType offerType)
    {
        SetUpgradeAdEnabled(offerType, false);
        RefreshUpgradeAdStates();
    }

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

    private void ApplyUpgradeButtonAdModeState(Button button, Selectable.Transition normalTransition)
    {
        if (button == null)
            return;

        CacheUpgradeButtonTransitions();
        button.interactable = true;
        button.transition = normalTransition;

        ButtonScaleAnimation scaleAnim = button.GetComponent<ButtonScaleAnimation>();
        if (scaleAnim != null)
            scaleAnim.enabled = true;
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

    private void PlayInsufficientCoinsShake(Button button, UpgradeAdOfferType? offerType = null, System.Action onComplete = null)
    {
        if (button == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (offerType.HasValue && IsUpgradeAdOfferVisible(offerType.Value))
        {
            onComplete?.Invoke();
            return;
        }

        ButtonShakeAnimation shake = button.GetComponent<ButtonShakeAnimation>();
        if (shake != null)
            shake.Play(onComplete);
        else
            onComplete?.Invoke();
    }

    private static void SetUpgradeButtonShakeFeedback(Button button, bool enabled)
    {
        if (button == null)
            return;

        ButtonShakeAnimation shake = button.GetComponent<ButtonShakeAnimation>();
        if (shake != null)
            shake.playHaptics = enabled;
    }

    private void EnsureRewardedAdManager()
    {
        RewardedAdManager.EnsureExists();
    }

    private void PreloadUpgradeRewardedAds()
    {
        EnsureRewardedAdManager();
        RewardedAdManager.Instance?.SetKeepRewardedAdWarm(true);
        RewardedAdManager.Instance?.EnsureRewardedAdPreloaded();
    }

    private void KeepRewardedAdReadyInBackground()
    {
        if (isUpgrading)
            return;

        PreloadUpgradeRewardedAds();
    }

    private void SetupRewardedAdUpgrades()
    {
        EnsureRewardedAdManager();
        ResolveUpgradeAdVisuals(planeUpgradeAdVisuals, upgradeButton);
        ResolveUpgradeAdVisuals(launchForceUpgradeAdVisuals, increaseLaunchForceBtn);
        ResolveUpgradeAdVisuals(coinMultiplierUpgradeAdVisuals, increaseCoinMultiplierBtn);
        ClearAllUpgradeAdOffers();
        ResetUpgradeAdVisuals();
        SubscribeToRewardedAdEvents();
    }

    private void SubscribeToRewardedAdEvents()
    {
        EnsureRewardedAdManager();
        if (RewardedAdManager.Instance == null)
            return;

        RewardedAdManager.Instance.OnRewardedAdLoaded -= OnRewardedAdBecameReady;
        RewardedAdManager.Instance.OnRewardedAdLoaded += OnRewardedAdBecameReady;

        if (RewardedAdManager.Instance.IsRewardedAdReady())
            RefreshUpgradeAdStates();
    }

    private void UnsubscribeFromRewardedAdEvents()
    {
        if (RewardedAdManager.Instance == null)
            return;

        RewardedAdManager.Instance.OnRewardedAdLoaded -= OnRewardedAdBecameReady;
    }

    private void OnRewardedAdBecameReady()
    {
        RefreshUpgradeAdStates();
    }

    void Update()
    {
        rewardedPreloadTimer -= Time.unscaledDeltaTime;
        if (rewardedPreloadTimer <= 0f)
        {
            rewardedPreloadTimer = 2f;
            KeepRewardedAdReadyInBackground();
        }

        if (!HasPendingUpgradeAdOffer())
            return;

        upgradeAdRefreshTimer -= Time.unscaledDeltaTime;
        if (upgradeAdRefreshTimer > 0f)
            return;

        upgradeAdRefreshTimer = 0.5f;
        RefreshUpgradeAdStates();
    }

    private bool HasPendingUpgradeAdOffer()
    {
        return planeUpgradeAdEnabled || launchForceUpgradeAdEnabled || coinMultiplierUpgradeAdEnabled;
    }

    private static void ResolveUpgradeAdVisuals(UpgradeAdVisualSet visuals, Button button)
    {
        if (visuals == null || button == null)
            return;

        if (visuals.coinSection == null)
        {
            Transform coinSectionTransform = FindUpgradeButtonChild(button.transform, "CostS", "Cost", "CoinRow");
            if (coinSectionTransform == null)
                coinSectionTransform = FindUpgradeButtonChild(button.transform, "CoinIcon", "CoinImage", "Costb");

            if (coinSectionTransform != null)
                visuals.coinSection = coinSectionTransform.gameObject;
        }

        if (visuals.adSection == null)
        {
            Transform adSectionTransform = FindUpgradeButtonChild(button.transform, "AD", "AdRow", "AdIcon");
            if (adSectionTransform != null)
                visuals.adSection = adSectionTransform.gameObject;
        }
    }

    private static Transform FindUpgradeButtonChild(Transform parent, params string[] names)
    {
        if (parent == null)
            return null;

        foreach (string childName in names)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                return child;
        }

        return null;
    }

    private void ResetUpgradeAdVisuals()
    {
        ApplyUpgradeAdVisual(planeUpgradeAdVisuals, false);
        ApplyUpgradeAdVisual(launchForceUpgradeAdVisuals, false);
        ApplyUpgradeAdVisual(coinMultiplierUpgradeAdVisuals, false);
    }

    private static void ApplyUpgradeAdVisual(UpgradeAdVisualSet visuals, bool showAdRow)
    {
        if (visuals == null)
            return;

        if (visuals.coinSection != null)
            visuals.coinSection.SetActive(!showAdRow);

        if (visuals.adSection != null)
            visuals.adSection.SetActive(showAdRow);
    }

    private void ClearAllUpgradeAdOffersAndRefresh()
    {
        ClearAllUpgradeAdOffers();
        RefreshUpgradeAdStates();
    }

    private void HandleInsufficientCoinsClick(Button button, UpgradeAdOfferType offerType)
    {
        if (IsUpgradeAdOfferVisible(offerType))
        {
            TryShowUpgradeRewardedAd(offerType);
            return;
        }

        OnInsufficientCoinsForUpgrade(button, offerType);
    }

    private void OnInsufficientCoinsForUpgrade(Button button, UpgradeAdOfferType offerType)
    {
        RefreshAllUpgradeCostUI();

        if (IsUpgradeAdOfferVisible(offerType))
            return;

        PlayInsufficientCoinsShake(button, offerType);
    }

    private bool CanAffordUpgrade(UpgradeAdOfferType offerType)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return playerCoins >= GetPlaneUpgradeClickCost();
            case UpgradeAdOfferType.LaunchForce:
                return playerCoins >= GetLaunchForceClickCost();
            case UpgradeAdOfferType.CoinMultiplier:
                return playerCoins >= GetCoinMultiplierClickCost();
            default:
                return false;
        }
    }

    private bool IsUpgradeAdOfferActive(UpgradeAdOfferType offerType)
    {
        if (!IsUpgradeAdEnabled(offerType) || isUpgrading || isShowingUpgradeAd)
            return false;

        if (IsUpgradeAtMax(offerType))
            return false;

        SyncPlayerCoins();
        return !CanAffordUpgrade(offerType);
    }

    private bool IsUpgradeAdOfferVisible(UpgradeAdOfferType offerType)
    {
        if (!IsUpgradeAdEnabled(offerType) || isUpgrading)
            return false;

        if (IsUpgradeAtMax(offerType))
            return false;

        SyncPlayerCoins();
        return !CanAffordUpgrade(offerType);
    }

    private bool CanWatchUpgradeRewardedAd(UpgradeAdOfferType offerType)
    {
        if (!IsUpgradeAdOfferActive(offerType))
            return false;

        EnsureRewardedAdManager();
        return RewardedAdManager.Instance != null && RewardedAdManager.Instance.IsRewardedAdReady();
    }

    private void RefreshUpgradeAdStates()
    {
        SyncPlayerCoins();

        if (planeUpgradeAdEnabled && (IsFullyUpgraded() || CanAffordUpgrade(UpgradeAdOfferType.PlanePart)))
            planeUpgradeAdEnabled = false;

        if (launchForceUpgradeAdEnabled && (launchForceLevel >= maxLaunchForceLevel || CanAffordUpgrade(UpgradeAdOfferType.LaunchForce)))
            launchForceUpgradeAdEnabled = false;

        if (coinMultiplierUpgradeAdEnabled && (IsCoinMultiplierMax() || CanAffordUpgrade(UpgradeAdOfferType.CoinMultiplier)))
            coinMultiplierUpgradeAdEnabled = false;

        bool planeAdMode = IsUpgradeAdOfferVisible(UpgradeAdOfferType.PlanePart);
        bool launchForceAdMode = IsUpgradeAdOfferVisible(UpgradeAdOfferType.LaunchForce);
        bool coinMultiplierAdMode = IsUpgradeAdOfferVisible(UpgradeAdOfferType.CoinMultiplier);

        ApplyUpgradeAdVisual(planeUpgradeAdVisuals, planeAdMode);
        ApplyUpgradeAdVisual(launchForceUpgradeAdVisuals, launchForceAdMode);
        ApplyUpgradeAdVisual(coinMultiplierUpgradeAdVisuals, coinMultiplierAdMode);

        SetUpgradeButtonShakeFeedback(upgradeButton, !planeAdMode);
        SetUpgradeButtonShakeFeedback(increaseLaunchForceBtn, !launchForceAdMode);
        SetUpgradeButtonShakeFeedback(increaseCoinMultiplierBtn, !coinMultiplierAdMode);

        if (upgradeButton != null)
        {
            int partCount = GetUpgradePartCount();
            bool canAfford = playerCoins >= currentCost && currentIndex < partCount;
            bool atMax = IsFullyUpgraded();

            if (planeAdMode)
                ApplyUpgradeButtonAdModeState(upgradeButton, upgradeButtonTransition);
            else
                ApplyUpgradeButtonState(upgradeButton, upgradeButtonTransition, canAfford, atMax);
        }

        if (increaseLaunchForceBtn != null)
        {
            bool isMaxLevel = launchForceLevel >= maxLaunchForceLevel;
            bool canAfford = !isMaxLevel && playerCoins >= GetLaunchForceClickCost();

            if (launchForceAdMode)
                ApplyUpgradeButtonAdModeState(increaseLaunchForceBtn, launchForceButtonTransition);
            else
                ApplyUpgradeButtonState(increaseLaunchForceBtn, launchForceButtonTransition, canAfford, isMaxLevel);
        }

        if (increaseCoinMultiplierBtn != null)
        {
            bool isMaxLevel = IsCoinMultiplierMax();
            bool canAfford = !isMaxLevel && playerCoins >= GetCoinMultiplierClickCost();

            if (coinMultiplierAdMode)
                ApplyUpgradeButtonAdModeState(increaseCoinMultiplierBtn, coinMultiplierButtonTransition);
            else
                ApplyUpgradeButtonState(increaseCoinMultiplierBtn, coinMultiplierButtonTransition, canAfford, isMaxLevel);
        }
    }

    private void TryShowUpgradeRewardedAd(UpgradeAdOfferType offerType)
    {
        if (isUpgrading || isShowingUpgradeAd || isWaitingForRewardedAd || !IsUpgradeAdOfferVisible(offerType))
            return;

        EnsureRewardedAdManager();
        if (RewardedAdManager.Instance == null)
            return;

        UpgradeAdOfferType savedOffer = offerType;
        Debug.Log(
            $"[UpgradeAdFlow] Tap offer={offerType} ready={RewardedAdManager.Instance.IsRewardedAdReady()} " +
            $"loading={RewardedAdManager.Instance.IsRewardedAdLoading} waiting={isWaitingForRewardedAd} showing={isShowingUpgradeAd}");

        void OnAdFinished(bool success)
        {
            Debug.Log($"[UpgradeAdFlow] Callback offer={savedOffer} success={success}");
            isShowingUpgradeAd = false;
            isWaitingForRewardedAd = false;

            if (success)
                GrantFreeUpgradeClick(savedOffer);
            else
                RefreshUpgradeAdStates();

            KeepRewardedAdReadyInBackground();
        }

        if (RewardedAdManager.Instance.IsRewardedAdReady())
        {
            isShowingUpgradeAd = true;
            RewardedAdManager.Instance.ShowRewardedAd(OnAdFinished);
            return;
        }

        // Ad isn't ready yet — notify on every tap and keep preloading.
        // Don't enter waiting state so the player can request again freely.
        ShowAdNotLoadedNotification();
        KeepRewardedAdReadyInBackground();
    }

    private void GrantFreeUpgradeClick(UpgradeAdOfferType offerType)
    {
        // Watching a rewarded ad consumes the offer. Another ad only appears after
        // the player spends coins on a paid upgrade click again.
        SetUpgradeAdEnabled(offerType, false);

        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                GrantFreePlaneUpgradeClick();
                break;
            case UpgradeAdOfferType.LaunchForce:
                GrantFreeLaunchForceUpgradeClick();
                break;
            case UpgradeAdOfferType.CoinMultiplier:
                GrantFreeCoinMultiplierUpgradeClick();
                break;
        }

        SetUpgradeAdEnabled(offerType, false);
        RefreshUpgradeAdStates();
    }

    private void GrantFreePlaneUpgradeClick()
    {
        if (isUpgrading || IsFullyUpgraded())
            return;

        AudioManager.PlayBtnSfx();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        partUpgrades.RegisterClick();

        UpdateCoinUI();
        RefreshAllUpgradeCostUI();
        UpdateSliderUI();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();

        if (clickCount >= clicksRequired)
        {
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.PlanePart);
            StartCoroutine(UpgradeSequence());
        }
        else
        {
            SaveProgress();
            UpdateButtonInteractable();
        }
    }

    private void GrantFreeLaunchForceUpgradeClick()
    {
        if (isUpgrading || launchForceLevel >= maxLaunchForceLevel || dragLauncher == null)
            return;

        AudioManager.PlayBtnSfx();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        launchForceUpgrades.RegisterClick();

        RefreshAllUpgradeCostUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceSliderUI();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();

        if (launchForceClickCount >= clicksRequired)
        {
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.LaunchForce);
            StartCoroutine(LaunchForceUpgradeSequence());
        }
        else
            SaveLaunchForceProgress();
    }

    private void GrantFreeCoinMultiplierUpgradeClick()
    {
        if (isUpgrading || IsCoinMultiplierMax())
            return;

        AudioManager.PlayBtnSfx();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        coinMultiplierUpgrades.RegisterClick();
        SaveCoinMultiplierProgress();

        RefreshAllUpgradeCostUI();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();

        // Free ad upgrades never re-offer another ad. Ads return only after a paid coin click.
        SuppressAdAfterFullUpgrade(UpgradeAdOfferType.CoinMultiplier);
    }

    private void ResolveSceneReferences()
    {
        AudioManager.Get(ref audioManager);

        if (planeUpgradeConfig == null)
            planeUpgradeConfig = FindObjectOfType<PlaneUpgradeConfig>();

        if (dragLauncher == null)
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();

        if (rubberBandVisual == null && dragLauncher != null && dragLauncher.lineRenderer != null)
            rubberBandVisual = dragLauncher.lineRenderer;

        if (planeController == null)
            planeController = FindObjectOfType<PlaneController>();
    }

    private void ResolveUIReferences()
    {
        ResolveUpgradeCostText(ref costText, upgradeButton, planeUpgradeAdVisuals);
        ResolveUpgradeCostText(ref launchForceCostText, increaseLaunchForceBtn, launchForceUpgradeAdVisuals);
        ResolveUpgradeCostText(ref coinMultiplierCostText, increaseCoinMultiplierBtn, coinMultiplierUpgradeAdVisuals);

        if (dragLauncher == null)
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();

        if (cheatCoinsButton == null)
        {
            Transform cheatButtonTransform = transform.Find("CheatCoinsButton");
            if (cheatButtonTransform == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "CheatCoinsButton")
                    {
                        cheatButtonTransform = child;
                        break;
                    }
                }
            }

            if (cheatButtonTransform != null)
                cheatCoinsButton = cheatButtonTransform.gameObject;
        }

        ResolveUpgradeAdVisuals(planeUpgradeAdVisuals, upgradeButton);
        ResolveUpgradeAdVisuals(launchForceUpgradeAdVisuals, increaseLaunchForceBtn);
        ResolveUpgradeAdVisuals(coinMultiplierUpgradeAdVisuals, increaseCoinMultiplierBtn);
        ResolveBoostButtonParent();
        ResolveBoostLevelText();
        ResolveAdNotLoadedNotificationText();
        HideAdNotLoadedNotificationInstant();
    }

    private void ResolveAdNotLoadedNotificationText()
    {
        if (adNotLoadedNotificationText != null)
            return;

        foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp == null)
                continue;

            string objectName = tmp.gameObject.name;
            string text = tmp.text != null ? tmp.text.Trim() : string.Empty;
            if (objectName == "AdNotLoaded" || objectName == "Ad Not Found" ||
                text.Equals("Ad Not Found", System.StringComparison.OrdinalIgnoreCase) ||
                text.Equals("Ad Not Loaded", System.StringComparison.OrdinalIgnoreCase))
            {
                adNotLoadedNotificationText = tmp;
                CaptureAdNotLoadedNotificationRestScale();
                return;
            }
        }
    }

    private void ShowAdNotLoadedNotification()
    {
        ResolveAdNotLoadedNotificationText();
        if (adNotLoadedNotificationText == null)
            return;

        if (adNotLoadedNotificationCoroutine != null)
        {
            StopCoroutine(adNotLoadedNotificationCoroutine);
            adNotLoadedNotificationCoroutine = null;
        }

        CaptureAdNotLoadedNotificationRestScale();
        adNotLoadedNotificationText.gameObject.SetActive(true);
        adNotLoadedNotificationText.transform.localScale = Vector3.zero;

        if (!string.IsNullOrWhiteSpace(adNotLoadedNotificationText.text))
            adNotLoadedNotificationText.text = adNotLoadedNotificationText.text.Trim();
        else
            adNotLoadedNotificationText.text = "Ad Not Found";

        if (!isActiveAndEnabled)
        {
            adNotLoadedNotificationText.transform.localScale = adNotLoadedNotificationRestScale;
            return;
        }

        adNotLoadedNotificationCoroutine = StartCoroutine(AnimateAdNotLoadedNotification());
    }

    private void CaptureAdNotLoadedNotificationRestScale()
    {
        if (adNotLoadedNotificationText == null || adNotLoadedNotificationRestScaleCaptured)
            return;

        Vector3 scale = adNotLoadedNotificationText.transform.localScale;
        if (scale.sqrMagnitude < 0.0001f)
            scale = Vector3.one;

        adNotLoadedNotificationRestScale = scale;
        adNotLoadedNotificationRestScaleCaptured = true;
    }

    private IEnumerator AnimateAdNotLoadedNotification()
    {
        Transform notificationTransform = adNotLoadedNotificationText.transform;
        Vector3 restScale = adNotLoadedNotificationRestScale;
        Vector3 overshootScale = restScale * adNotLoadedPopOvershoot;

        notificationTransform.localScale = Vector3.zero;

        float elapsed = 0f;
        float popInDuration = Mathf.Max(0.01f, adNotLoadedPopInDuration);
        while (elapsed < popInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popInDuration);
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
            notificationTransform.localScale = Vector3.LerpUnclamped(Vector3.zero, overshootScale, eased);
            yield return null;
        }

        elapsed = 0f;
        float settleDuration = popInDuration * 0.45f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            notificationTransform.localScale = Vector3.Lerp(overshootScale, restScale, t);
            yield return null;
        }

        notificationTransform.localScale = restScale;
        yield return new WaitForSecondsRealtime(adNotLoadedNotificationDuration);

        elapsed = 0f;
        float popOutDuration = Mathf.Max(0.01f, adNotLoadedPopOutDuration);
        Vector3 startScale = notificationTransform.localScale;
        while (elapsed < popOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popOutDuration);
            float eased = t * t;
            notificationTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
            yield return null;
        }

        notificationTransform.localScale = restScale;
        adNotLoadedNotificationText.gameObject.SetActive(false);
        adNotLoadedNotificationCoroutine = null;
    }

    private void HideAdNotLoadedNotificationInstant()
    {
        if (adNotLoadedNotificationCoroutine != null)
        {
            StopCoroutine(adNotLoadedNotificationCoroutine);
            adNotLoadedNotificationCoroutine = null;
        }

        if (adNotLoadedNotificationText != null)
        {
            if (adNotLoadedNotificationRestScaleCaptured)
                adNotLoadedNotificationText.transform.localScale = adNotLoadedNotificationRestScale;
            adNotLoadedNotificationText.gameObject.SetActive(false);
        }
    }

    private static void ResolveUpgradeCostText(
        ref TextMeshProUGUI costTextField,
        Button button,
        UpgradeAdVisualSet adVisuals)
    {
        if (costTextField != null || button == null)
            return;

        if (adVisuals?.coinSection != null)
        {
            costTextField = adVisuals.coinSection.GetComponent<TextMeshProUGUI>();
            if (costTextField == null)
                costTextField = adVisuals.coinSection.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (costTextField != null)
            return;

        Transform costTransform = FindUpgradeButtonChild(button.transform, "CostS", "Cost", "Costb");
        if (costTransform != null)
            costTextField = costTransform.GetComponent<TextMeshProUGUI>();
    }

    private void ResolveBoostButtonParent()
    {
        if (boostEnableBtnParent != null || boostEnableBtn == null)
            return;

        Transform parent = boostEnableBtn.transform.parent;
        if (parent == null)
            return;

        if (parent.name == "BuyBoostParent" || parent.name == "BuyBoostBtnParent" ||
            parent.name == "BuyBoostButtonParent")
            boostEnableBtnParent = parent.gameObject;
    }

    private GameObject GetBoostSlideTarget()
    {
        ResolveBoostButtonParent();
        return boostEnableBtnParent != null ? boostEnableBtnParent : boostEnableBtn?.gameObject;
    }

    private void ResolveBoostLevelText()
    {
        if (boostLevelText != null || boostEnableBtn == null)
            return;

        foreach (TextMeshProUGUI tmp in boostEnableBtn.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp == null)
                continue;

            Transform parent = tmp.transform.parent;
            if (parent != null && parent.name.Contains("Cost"))
                continue;

            string objectName = tmp.gameObject.name;
            if (objectName == "LevelText" || objectName == "Text (TMP)" || objectName == "Level" ||
                objectName == "BoostLevel")
            {
                boostLevelText = tmp;
                return;
            }
        }
    }

    private void ApplyCheatCoinsButtonVisibility()
    {
        if (cheatCoinsButton == null)
            return;

        // Cheat coins only in the editor — never on Android or iOS player builds.
#if UNITY_EDITOR
        cheatCoinsButton.SetActive(true);
#else
        cheatCoinsButton.SetActive(false);
#endif
    }

    private void RefreshAllUpgradeUI()
    {
        UpdateCoinUI();
        RefreshAllUpgradeCostUI();
        UpdateSliderUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateBoostCostUI();
        UpdateBoostLevelUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceSliderUI();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        TryPreloadUpgradeAdsIfNeeded();
        RefreshUpgradeAdStates();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUpgrading)
            return;

        ResetUpgradeAdUiForMainMenu();
        RefreshUpgradeAdStates();
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
        RefreshPlaneUpgradeCost();
        if (playerCoins < GetPlaneUpgradeClickCost())
        {
            HandleInsufficientCoinsClick(upgradeButton, UpgradeAdOfferType.PlanePart);
            return;
        }

        if (!TrySpendCoins((int)currentCost))
            return;

        AudioManager.PlayBtnSfx();
        VibrationManager.Instance.VibrateButtonClick();

        // Progress and cost update
        partUpgrades.RegisterClick();

        UpdateCoinUI();
        RefreshAllUpgradeCostUI();
        UpdateSliderUI();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();

        // Activate part if complete
        if (clickCount >= clicksRequired)
        {
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.PlanePart);
            StartCoroutine(UpgradeSequence());
        }
        else
        {
            SaveProgress();
            UpdateButtonInteractable();
            UpdateAdEligibilityAfterPaidUpgrade(UpgradeAdOfferType.PlanePart);
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
        LogUpgradeVfx($"UpgradeSequence start: partIndex={currentIndex}, focus='{GetHierarchyPath(focusPoint)}', prefab={(upgradeParticleEffect != null ? upgradeParticleEffect.name : "NULL")}");
        if (focusPoint != null)
            yield return StartCoroutine(cameraManager.TransitionToTarget(focusPoint, cameraTransitionDuration));

        // Step 2: Play upgrade VFX (unlocks part inside routine on the same frame as the effect)
        GameObject part = planeUpgradeConfig != null ? planeUpgradeConfig.GetPart(currentIndex) : null;
        AudioManager.PlayPlanePartSfx();
        yield return StartCoroutine(PlayPartUpgradeParticlesRoutine(currentIndex, part));

        partUpgrades.CompletePartUnlock();
        UpdateSliderUI();
        RefreshAllUpgradeCostUI();

        // Step 4: Transition camera back to main menu
        yield return StartCoroutine(cameraManager.TransitionToTarget(cameraManager.mainMenuPosition, cameraTransitionDuration));
        taptoplay.SetActive(true);
        upgradeButton.gameObject.SetActive(true);
        increaseLaunchForceBtn.gameObject.SetActive(true);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(true);

        if (IsFullyUpgraded())
            SetMaxStateUI();

        CleanupAllPlaneUpgradeVfx();
        GetPlaneEffects()?.RefreshFlightTrails();

        SaveProgress();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        SuppressAdAfterFullUpgrade(UpgradeAdOfferType.PlanePart);
        EndUpgradeInputBlock();
    }

    private IEnumerator LaunchForceUpgradeSequence()
    {
        BeginUpgradeInputBlock();
        increaseLaunchForceBtn.interactable = false;
        taptoplay.SetActive(false);
        yield return new WaitForSeconds(0.3f);
        HideBoostButtonInstant();
        upgradeButton.gameObject.SetActive(false);
        increaseLaunchForceBtn.gameObject.SetActive(false);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(false);

        // Step 1: Transition camera to the slingshot
        Transform focusPoint = GetSlingshotCameraFocus();
        if (focusPoint != null)
            yield return StartCoroutine(cameraManager.TransitionToTarget(focusPoint, cameraTransitionDuration));

        // Step 2: Play upgrade VFX (applies slingshot level inside routine, same as part unlock)
        GameObject slingshotTarget = GetSlingshotUpgradeTarget();
        AudioManager.PlayPlanePartSfx();
        yield return StartCoroutine(PlaySlingshotUpgradeParticlesRoutine(slingshotTarget));

        launchForceUpgrades.RefreshCost();
        UpdateLaunchForceSliderUI();
        UpdateLaunchForceLevelUI();
        RefreshAllUpgradeCostUI();

        // Step 3: Transition camera back to main menu
        yield return StartCoroutine(cameraManager.TransitionToTarget(cameraManager.mainMenuPosition, cameraTransitionDuration));
        taptoplay.SetActive(true);
        upgradeButton.gameObject.SetActive(true);
        increaseLaunchForceBtn.gameObject.SetActive(true);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(true);

        SaveLaunchForceProgress();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        SuppressAdAfterFullUpgrade(UpgradeAdOfferType.LaunchForce);
        EndUpgradeInputBlock();

        Debug.Log($"Launch force upgraded to Level {launchForceLevel}! Force: {GetLaunchForceMultiplierForLevel(launchForceLevel)}");
    }

    private GameObject GetSlingshotUpgradeTarget()
    {
        RubberBandVisual band = GetRubberBandVisual();
        if (band != null)
            return band.gameObject;

        if (dragLauncher != null && dragLauncher.restingPoint != null)
            return dragLauncher.restingPoint.gameObject;

        return null;
    }

    private GameObject GetSlingshotUpgradeVfxRoot(GameObject slingshotTarget)
    {
        if (slingshotUpgradeVfxPoint != null)
            return slingshotUpgradeVfxPoint.gameObject;

        if (slingshotTarget == null)
            return null;

        Transform namedChild = slingshotTarget.transform.Find("UpgradeSmoke");
        if (namedChild != null)
            return namedChild.gameObject;

        foreach (ParticleSystem ps in slingshotTarget.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null || ps.transform == slingshotTarget.transform)
                continue;

            return ps.gameObject;
        }

        return null;
    }

    private void CollectInSceneUpgradeVfx(
        GameObject vfxRoot,
        GameObject activationRoot,
        List<ParticleSystem> particleSystems)
    {
        if (vfxRoot == null || particleSystems == null)
            return;

        EnsureActiveForPlayback(vfxRoot, activationRoot);
        vfxRoot.SetActive(true);
        AddParticleSystemsFromRoot(vfxRoot, particleSystems);
    }

    private static void HideUpgradeVfxRoot(GameObject part, GameObject vfxRoot)
    {
        PlaneUpgradeConfig.StopAndHideUpgradeVfx(part, vfxRoot);
    }

    private void ApplySlingshotUpgradeLevel()
    {
        launchForceUpgrades.CompleteTier();

        if (dragLauncher != null)
        {
            launchForceUpgrades.ApplyToLauncher(dragLauncher);
            dragLauncher.ResetForNewLaunch();
        }

        // Persist immediately so the new color stays if anything else reloads from PlayerPrefs.
        SaveLaunchForceProgress();
        GetRubberBandVisual()?.ApplyLaunchForceLevel(launchForceLevel);
    }

    private IEnumerator PlaySlingshotUpgradeParticlesRoutine(GameObject slingshotTarget)
    {
        RubberBandVisual band = GetRubberBandVisual();

        // Preferred VFX: emissive glow along the rubber strings.
        if (band != null && band.upgradeGlowMaterial != null)
        {
            ApplySlingshotUpgradeLevel();
            yield return StartCoroutine(band.PlayUpgradeGlow());
            yield break;
        }

        if (slingshotTarget == null)
        {
            ApplySlingshotUpgradeLevel();
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        ApplySlingshotUpgradeLevel();

        GameObject vfxRoot = GetSlingshotUpgradeVfxRoot(slingshotTarget);
        var spawnedInstances = new List<GameObject>();
        var particleSystems = new List<ParticleSystem>();

        if (vfxRoot != null)
        {
            CollectInSceneUpgradeVfx(vfxRoot, slingshotTarget, particleSystems);
            LogUpgradeVfx($"Slingshot: using in-scene VFX '{vfxRoot.name}' under '{GetHierarchyPath(vfxRoot.transform)}'.");
        }
        else if (upgradeParticleEffect != null)
        {
            Transform anchor = slingshotUpgradeVfxPoint != null
                ? slingshotUpgradeVfxPoint
                : slingshotTarget.transform;
            TryAddUpgradeParticlePrefab(upgradeParticleEffect, anchor, slingshotTarget, spawnedInstances, particleSystems);
            LogUpgradeVfxWarning("Slingshot: no UpgradeSmoke child found — used spawn fallback.");
        }

        if (particleSystems.Count == 0)
        {
            // Fallback: still pulse the band if possible.
            if (band != null)
            {
                yield return StartCoroutine(band.PlayUpgradeGlow());
                yield break;
            }

            LogUpgradeVfxWarning("Slingshot: no particle systems to play.");
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        yield return null;

        var activationRoots = new List<GameObject> { slingshotTarget };
        if (vfxRoot != null)
            activationRoots.Add(vfxRoot);
        activationRoots.AddRange(spawnedInstances);

        PlayCollectedUpgradeParticles(particleSystems, activationRoots, $"Slingshot {slingshotTarget.name}");

        float waitDuration = Mathf.Max(
            particleEffectDuration,
            GetParticleDuration(particleSystems.ToArray()));

        yield return new WaitForSeconds(waitDuration > 0f ? waitDuration : particleEffectDuration);

        foreach (GameObject spawned in spawnedInstances)
        {
            if (spawned != null)
                Destroy(spawned, 2f);
        }

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null || !IsSceneInstance(ps.gameObject))
                continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        HideUpgradeVfxRoot(slingshotTarget, vfxRoot);
    }

    private Transform GetSlingshotCameraFocus()
    {
        if (cameraManager != null)
            return cameraManager.GetSlingshotCameraPosition();
        return null;
    }

    private RubberBandVisual GetRubberBandVisual()
    {
        ResolveUIReferences();
        if (rubberBandVisual != null)
            return rubberBandVisual;

        rubberBandVisual = FindObjectOfType<RubberBandVisual>();
        return rubberBandVisual;
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
            LogUpgradeVfxWarning($"Part upgrade index {index}: part GameObject is NULL (check PlaneUpgradeConfig.upgradeParts).");
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        bool wasInactive = !part.activeSelf;
        if (wasInactive && planeUpgradeConfig != null)
            planeUpgradeConfig.UnlockPart(index);

        GameObject vfxRoot = planeUpgradeConfig != null ? planeUpgradeConfig.GetUpgradeVfxRoot(index) : null;
        var spawnedInstances = new List<GameObject>();
        var particleSystems = new List<ParticleSystem>();

        LogUpgradeVfx(
            $"Part upgrade index {index}: part='{part.name}' active={part.activeSelf} (wasInactive={wasInactive}), " +
            $"vfxChild='{(vfxRoot != null ? GetHierarchyPath(vfxRoot.transform) : "NOT FOUND")}'");

        if (vfxRoot != null)
            CollectInSceneUpgradeVfx(vfxRoot, part, particleSystems);
        else if (upgradeParticleEffect != null)
        {
            Transform anchor = planeUpgradeConfig != null
                ? planeUpgradeConfig.GetVfxAnchor(index)
                : part.transform;
            if (anchor == null)
                anchor = part.transform;

            TryAddUpgradeParticlePrefab(upgradeParticleEffect, anchor, part, spawnedInstances, particleSystems);
            LogUpgradeVfxWarning($"Part upgrade index {index}: no smoke child on part — used spawn fallback.");
        }

        PlaneEffects planeEffects = GetPlaneEffects();

        if (particleSystems.Count == 0)
        {
            LogUpgradeVfxWarning($"Part upgrade index {index}: NO particle systems found on part child.");
            yield return new WaitForSeconds(particleEffectDuration);
            planeEffects?.RefreshFlightTrails();
            yield break;
        }

        var activationRoots = new List<GameObject> { part };
        if (vfxRoot != null)
            activationRoots.Add(vfxRoot);
        activationRoots.AddRange(spawnedInstances);

        yield return null;

        int playedCount = PlayCollectedUpgradeParticles(particleSystems, activationRoots, $"Part[{index}] {part.name}");

        if (playedCount == 0)
            LogUpgradeVfxWarning($"Part upgrade index {index}: particle systems were listed ({particleSystems.Count}) but none played.");

        float waitDuration = Mathf.Max(
            particleEffectDuration,
            GetParticleDuration(particleSystems.ToArray()));

        LogUpgradeVfx($"Part upgrade index {index}: waiting {waitDuration:F2}s for particles.");
        yield return new WaitForSeconds(waitDuration > 0f ? waitDuration : particleEffectDuration);

        foreach (GameObject spawned in spawnedInstances)
        {
            if (spawned != null)
                Destroy(spawned, 2f);
        }

        CleanupUpgradeVfx(particleSystems, planeEffects);
        HideUpgradeVfxRoot(part, vfxRoot);
        LogUpgradeVfx($"Part upgrade index {index}: finished and cleaned up VFX.");
    }

    private PlaneEffects GetPlaneEffects()
    {
        if (planeUpgradeConfig == null || planeUpgradeConfig.planeController == null)
            return null;

        return planeUpgradeConfig.planeController.GetComponent<PlaneEffects>();
    }

    private static void CleanupUpgradeVfx(
        List<ParticleSystem> particleSystems,
        PlaneEffects planeEffects)
    {
        if (particleSystems != null)
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps == null || !IsSceneInstance(ps.gameObject))
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        planeEffects?.RefreshFlightTrails();
    }

    private void TryAddUpgradeParticlePrefab(
        GameObject prefab,
        Transform spawnAnchor,
        GameObject activationRoot,
        List<GameObject> spawnedInstances,
        List<ParticleSystem> particleSystems)
    {
        if (prefab == null)
        {
            LogUpgradeVfxWarning("TryAddUpgradeParticlePrefab: upgradeParticleEffect prefab is not assigned on MainMenu.");
            return;
        }

        if (spawnAnchor == null)
        {
            LogUpgradeVfxWarning($"TryAddUpgradeParticlePrefab: spawn anchor is null (activationRoot='{activationRoot?.name ?? "null"}').");
            return;
        }

        if (IsSceneInstance(prefab))
        {
            LogUpgradeVfx($"TryAddUpgradeParticlePrefab: using scene instance '{prefab.name}' at anchor '{spawnAnchor.name}'.");
            EnsureActiveForPlayback(prefab, activationRoot);
            int before = particleSystems.Count;
            AddParticleSystemsFromRoot(prefab, particleSystems);
            LogUpgradeVfx($"TryAddUpgradeParticlePrefab: scene instance added {particleSystems.Count - before} particle system(s).");
            return;
        }

        Transform parentTransform = activationRoot != null ? activationRoot.transform : spawnAnchor;
        bool useMeshCenter = activationRoot != null && spawnAnchor == activationRoot.transform;

        GameObject instance = Instantiate(prefab, parentTransform, false);

        if (useMeshCenter)
            instance.transform.localPosition = GetUpgradeParticleLocalPosition(activationRoot);
        else if (spawnAnchor != parentTransform)
            instance.transform.position = spawnAnchor.position + Vector3.up * upgradeParticleYOffset;
        else
            instance.transform.localPosition = new Vector3(0f, upgradeParticleYOffset, 0f);

        instance.transform.rotation = Quaternion.identity;
        float scaleMultiplier = Mathf.Max(0.01f, upgradeParticleScale);
        instance.transform.localScale = prefab.transform.localScale * scaleMultiplier;

        instance.SetActive(false);
        PrepareSpawnedUpgradeParticleSystems(instance);
        instance.SetActive(true);
        spawnedInstances.Add(instance);

        int beforeCount = particleSystems.Count;
        AddParticleSystemsFromRoot(instance, particleSystems);
        int added = particleSystems.Count - beforeCount;

        LogUpgradeVfx(
            $"TryAddUpgradeParticlePrefab: spawned '{instance.name}' parent='{GetHierarchyPath(parentTransform)}' " +
            $"worldPos={instance.transform.position} localScale={instance.transform.localScale}, " +
            $"added {added} particle system(s).");
    }

    private static void PrepareSpawnedUpgradeParticleSystems(GameObject instance)
    {
        if (instance == null)
            return;

        foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
    }

    private int PlayCollectedUpgradeParticles(
        List<ParticleSystem> particleSystems,
        List<GameObject> activationRoots,
        string context)
    {
        if (activationRoots != null)
        {
            foreach (GameObject root in activationRoots)
            {
                if (root == null)
                    continue;

                int activated = ActivateVfxChildren(root);
                if (activated > 0)
                    LogUpgradeVfx($"{context}: activated {activated} inactive VFX child object(s) on '{root.name}'.");
            }
        }

        int played = 0;
        int skipped = 0;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
            {
                skipped++;
                continue;
            }

            if (!IsSceneInstance(ps.gameObject))
            {
                skipped++;
                LogUpgradeVfx($"{context}: skipped non-scene particle '{ps.name}'.");
                continue;
            }

            ps.gameObject.SetActive(true);
            var emission = ps.emission;
            emission.enabled = true;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
            played++;
            LogUpgradeVfx($"{context}: playing '{ps.name}' at worldPos {ps.transform.position} (path: {GetHierarchyPath(ps.transform)}).");
        }

        LogUpgradeVfx($"{context}: played {played}, skipped {skipped}, listed {particleSystems.Count}.");
        return played;
    }

    private void LogUpgradeVfx(string message)
    {
        if (!debugUpgradeVfx)
            return;

        Debug.Log($"[UpgradeVFX] {message}", this);
    }

    private void LogUpgradeVfxWarning(string message)
    {
        if (!debugUpgradeVfx)
            return;

        Debug.LogWarning($"[UpgradeVFX] {message}", this);
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
            if (ps == null || !IsSceneInstance(ps.gameObject) || particleSystems.Contains(ps))
                continue;

            particleSystems.Add(ps);
        }
    }

    private void CleanupAllPlaneUpgradeVfx()
    {
        planeUpgradeConfig?.SuppressAllUpgradeVfx();
        GetPlaneEffects()?.RefreshFlightTrails();
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

            bool isVfxChild = child.GetComponent<ParticleSystem>() != null;

            if (isVfxChild && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
                activated++;
            }
        }

        return activated;
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
        if (costText == null)
            return;

        SyncPlayerCoins();
        RefreshPlaneUpgradeCost();
        bool atMax = IsFullyUpgraded();
        bool canAfford = playerCoins >= GetPlaneUpgradeClickCost();
        string amount = atMax ? "MAX" : FormatNumber(currentCost);
        costText.text = amount;
        ApplyUpgradeCostTextColor(costText, canAfford, atMax);
    }

    private static void ApplyUpgradeCostTextColor(TextMeshProUGUI text, bool canAfford, bool atMax)
    {
        if (text == null)
            return;

        Color targetColor = !atMax && !canAfford
            ? UpgradeCostUnaffordableColor
            : UpgradeCostAffordableColor;

        text.enableVertexGradient = false;
        text.color = targetColor;
        // LuckiestGuy SDF ignores vertex color; drive the material face color instead.
        text.faceColor = targetColor;
        text.ForceMeshUpdate(true, true);
    }

    private void RefreshAllUpgradeCostUI()
    {
        SyncPlayerCoins();
        RefreshPlaneUpgradeCost();
        RefreshLaunchForceUpgradeCost();
        UpdateCostUI();
        UpdateLaunchForceCostUI();
        UpdateCoinMultiplierCostUI();
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
            bool hasEnoughCoins = playerCoins >= currentCost && !IsFullyUpgraded();
            bool planeAdMode = IsUpgradeAdOfferVisible(UpgradeAdOfferType.PlanePart);
            if (notEnoughCoinsU != null)
                notEnoughCoinsU.SetActive(!hasEnoughCoins && !IsFullyUpgraded() && !planeAdMode);
        }

        RefreshUpgradeAdStates();
    }

    private void UpdateBoostCostUI()
    {
        // Boost cost text can be reconnected later if needed.
    }

    private void UpdateBoostLevelUI()
    {
        ResolveBoostLevelText();
        if (boostLevelText == null)
            return;

        if (boostLevel > 0)
        {
            boostLevelText.gameObject.SetActive(true);
            boostLevelText.text = boostLevel >= maxBoostLevel ? "max" : $"LvL {boostLevel}";
        }
        else
        {
            boostLevelText.text = string.Empty;
            boostLevelText.gameObject.SetActive(false);
        }
    }
    
    private void UpdateLaunchForceCostUI()
    {
        if (launchForceCostText == null)
            return;

        SyncPlayerCoins();
        bool atMax = launchForceLevel >= maxLaunchForceLevel;
        RefreshLaunchForceUpgradeCost();
        bool canAfford = playerCoins >= GetLaunchForceClickCost();
        string amount = atMax ? "MAX" : FormatNumber(launchForceCurrentCost);
        launchForceCostText.text = amount;
        ApplyUpgradeCostTextColor(launchForceCostText, canAfford, atMax);
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
        return launchForceUpgrades.GetClickCost();
    }

    private void SaveLaunchForceProgress()
    {
        launchForceUpgrades.Save(dragLauncher);
    }

    private void SaveBoostProgress()
    {
        boostUpgrades.Save();
    }

    private void LoadBoostLevel()
    {
        boostUpgrades.Load();
        ApplyBoostSettings();
    }

    private void ApplyBoostSettings()
    {
        ResolveSceneReferences();

        boostUpgrades.ApplyToPlane(planeController);
        if (planeController != null && boostLevel <= 0)
            planeController.boostDuration = 0f;

        bool boostUnlocked = boostLevel > 0;
        if (PlaneBoosters != null)
            PlaneBoosters.SetActive(boostUnlocked);
        if (boostactive != null)
            boostactive.SetActive(boostUnlocked);

        UpdateBoostLevelUI();
    }
    
    private void CaptureBoostButtonRestPosition()
    {
        ResolveBoostButtonParent();
        GameObject slideTarget = GetBoostSlideTarget();
        if (slideTarget == null)
            return;

        boostButtonRect = slideTarget.GetComponent<RectTransform>();
        if (boostButtonRect == null || boostButtonRestCaptured)
            return;

        boostButtonRestPosition = boostButtonRect.anchoredPosition;
        boostButtonRestCaptured = true;
    }

    private bool HasUnlockedAtLeastOneWing()
    {
        ResolveSceneReferences();

        PlaneDamageHandler damageHandler = planeController != null
            ? planeController.GetComponent<PlaneDamageHandler>()
            : null;
        if (damageHandler == null)
            damageHandler = FindObjectOfType<PlaneDamageHandler>();

        if (damageHandler != null)
        {
            if (IsWingPartUnlocked(damageHandler.leftWing) || IsWingPartUnlocked(damageHandler.rightWing))
                return true;
        }

        // Fallback if damage handler refs aren't wired: match upgrade parts named like a wing.
        if (planeUpgradeConfig != null && planeUpgradeConfig.upgradeParts != null)
        {
            foreach (PlaneUpgradePartEntry entry in planeUpgradeConfig.upgradeParts)
            {
                if (entry?.part == null)
                    continue;
                if (entry.part.name.IndexOf("wing", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (PlaneUpgradeConfig.IsPartUnlocked(entry.part))
                    return true;
            }
        }

        return false;
    }

    private static bool IsWingPartUnlocked(GameObject wing)
    {
        return wing != null && PlaneUpgradeConfig.IsPartUnlocked(wing);
    }

    private void UpdateBoostButtonInteractable()
    {
        if (boostEnableBtn == null)
            return;

        CaptureBoostButtonRestPosition();
        if (boostButtonRect == null)
            return;

        bool hasWing = HasUnlockedAtLeastOneWing();
        bool isMaxLevel = boostLevel >= maxBoostLevel;
        bool canUpgrade = hasWing && !isMaxLevel;
        boostEnableBtn.interactable = canUpgrade;
        boostEnableBtn.transition = canUpgrade ? upgradeButtonTransition : Selectable.Transition.None;

        ButtonScaleAnimation scaleAnim = boostEnableBtn.GetComponent<ButtonScaleAnimation>();
        if (scaleAnim != null)
            scaleAnim.enabled = canUpgrade;

        if (!hasWing)
        {
            CancelBoostButtonMaxLevelHide();
            HideBoostButtonInstant();
        }
        else if (!isMaxLevel)
        {
            CancelBoostButtonMaxLevelHide();
            ShowBoostButtonSlideIn();
        }
        else
            ScheduleBoostButtonMaxLevelHide();
    }

    private void CancelBoostButtonMaxLevelHide()
    {
        if (boostButtonMaxLevelHideCoroutine == null)
            return;

        StopCoroutine(boostButtonMaxLevelHideCoroutine);
        boostButtonMaxLevelHideCoroutine = null;
    }

    private void ScheduleBoostButtonMaxLevelHide()
    {
        if (boostButtonMaxLevelHideCoroutine != null)
            return;

        GameObject slideTarget = GetBoostSlideTarget();
        if (slideTarget == null || (!boostButtonShown && !slideTarget.activeSelf))
            return;

        if (!isActiveAndEnabled)
            return;

        boostButtonMaxLevelHideCoroutine = StartCoroutine(HideBoostButtonAfterMaxLevelDelay());
    }

    private IEnumerator HideBoostButtonAfterMaxLevelDelay()
    {
        yield return new WaitForSeconds(boostMaxLevelHideDelay);
        boostButtonMaxLevelHideCoroutine = null;
        HideBoostButtonSlideOut();
    }

    private void ShowBoostButtonSlideIn()
    {
        if (boostButtonRect == null)
            return;

        GameObject slideTarget = GetBoostSlideTarget();
        if (slideTarget == null)
            return;

        slideTarget.SetActive(true);
        if (boostEnableBtn != null)
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

        GameObject slideTarget = GetBoostSlideTarget();
        if (slideTarget == null)
            return;

        if (!boostButtonShown && !slideTarget.activeSelf)
            return;

        if (boostButtonSlideCoroutine != null)
            StopCoroutine(boostButtonSlideCoroutine);

        if (!isActiveAndEnabled)
            return;

        boostButtonSlideCoroutine = StartCoroutine(AnimateBoostButton(false));
    }

    private void HideBoostButtonInstant()
    {
        CancelBoostButtonMaxLevelHide();

        if (boostButtonSlideCoroutine != null)
        {
            StopCoroutine(boostButtonSlideCoroutine);
            boostButtonSlideCoroutine = null;
        }

        boostButtonShown = false;
        GameObject slideTarget = GetBoostSlideTarget();
        if (slideTarget != null)
            slideTarget.SetActive(false);
    }

    private IEnumerator AnimateBoostButton(bool slideIn)
    {
        GameObject slideTarget = GetBoostSlideTarget();
        if (slideTarget == null)
            yield break;

        Vector2 hiddenPos = boostButtonRestPosition + new Vector2(boostSlideOffsetX, 0f);
        Vector2 start = slideIn ? hiddenPos : boostButtonRect.anchoredPosition;
        Vector2 end = slideIn ? boostButtonRestPosition : hiddenPos;

        if (slideIn)
        {
            slideTarget.SetActive(true);
            if (boostEnableBtn != null)
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
            slideTarget.SetActive(false);

        boostButtonShown = slideIn;
        boostButtonSlideCoroutine = null;
    }

    private void UpdateIncreaseLaunchForceButtonInteractable()
    {
        RefreshUpgradeAdStates();
    }

    private int GetMaxCoinMultiplierTotalClicks()
    {
        return coinMultiplierUpgrades.GetMaxTotalClicks();
    }

    private bool IsCoinMultiplierMax()
    {
        return coinMultiplierUpgrades.IsMax;
    }

    private void ClampCoinMultiplierProgress()
    {
        coinMultiplierUpgrades.Clamp();
    }

    private int GetCoinMultiplierTotalClicks()
    {
        return coinMultiplierUpgrades.GetTotalClicks();
    }

    private static string FormatCoinMultiplierDisplay(float value)
    {
        float rounded = Mathf.Round(value * 10f) / 10f;
        if (Mathf.Approximately(rounded, Mathf.Round(rounded)))
            return $"{Mathf.RoundToInt(rounded)}x";

        return $"{rounded:0.#}x";
    }

    private float GetCoinMultiplierValue()
    {
        return coinMultiplierUpgrades.GetValue();
    }

    private int GetCoinMultiplierClickCost()
    {
        return coinMultiplierUpgrades.GetClickCost();
    }

    private void SaveCoinMultiplierProgress()
    {
        coinMultiplierUpgrades.Save();
    }

    private void UpdateCoinMultiplierCostUI()
    {
        if (coinMultiplierCostText == null)
            return;

        SyncPlayerCoins();
        bool atMax = IsCoinMultiplierMax();
        bool canAfford = playerCoins >= GetCoinMultiplierClickCost();
        string amount = atMax ? "MAX" : FormatNumber(GetCoinMultiplierClickCost());
        coinMultiplierCostText.text = amount;
        ApplyUpgradeCostTextColor(coinMultiplierCostText, canAfford, atMax);
    }

    private void UpdateCoinMultiplierLevelUI()
    {
        if (coinMultiplierLevelText != null)
            coinMultiplierLevelText.text = FormatCoinMultiplierDisplay(GetCoinMultiplierValue());
    }

    private void UpdateCoinMultiplierSliderUI()
    {
        if (coinMultiplierSlider == null)
            return;

        if (IsCoinMultiplierMax())
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
        RefreshUpgradeAdStates();
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
        RefreshAllUpgradeCostUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        TryPreloadUpgradeAdsIfNeeded();
        RefreshUpgradeAdStates();
    }

    private void TryPreloadUpgradeAdsIfNeeded()
    {
        KeepRewardedAdReadyInBackground();
    }

    /// <summary>
    /// Called when the main menu is shown again after gameplay.
    /// </summary>
    public void OnReturnedToMainMenu()
    {
        FlightHUD uiManager = FindObjectOfType<FlightHUD>(true);
        if (uiManager != null)
            uiManager.CancelPendingLevelCompleteAd();

        ResetUpgradeAdUiForMainMenu();
        RefreshEconomyUI();
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
        SyncPlayerCoins();
        partUpgrades.Save(playerCoins);
    }

    private void LoadProgress()
    {
        partUpgrades.Load();
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
        partUpgrades.RefreshCost();
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
        {
            costText.text = "MAX";
            ApplyUpgradeCostTextColor(costText, false, true);
        }

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
        if (num >= 1_000_000_000f)
            return FormatCompactValue(num / 1_000_000_000f, "b");
        if (num >= 1_000_000f)
            return FormatCompactValue(num / 1_000_000f, "m");
        if (num >= 1_000f)
            return FormatCompactValue(num / 1_000f, "k");
        return Mathf.RoundToInt(num).ToString();
    }

    private static string FormatCompactValue(float value, string suffix)
    {
        float roundedToHundredths = Mathf.Round(value * 100f) / 100f;
        if (Mathf.Approximately(roundedToHundredths, Mathf.Round(roundedToHundredths)))
            return $"{Mathf.RoundToInt(roundedToHundredths)}{suffix}";

        if (Mathf.Approximately(value * 10f, Mathf.Round(value * 10f)))
            return $"{Mathf.Round(value * 10f) / 10f:0.#}{suffix}";

        return $"{roundedToHundredths:0.##}{suffix}";
    }

    public void BoostEnableBtn()
    {
        if (!HasUnlockedAtLeastOneWing() || boostLevel >= maxBoostLevel)
            return;

        TryShowBoostRewardedAd();
    }

    private void TryShowBoostRewardedAd()
    {
        if (isUpgrading || isShowingUpgradeAd || isWaitingForRewardedAd)
            return;

        if (!HasUnlockedAtLeastOneWing() || boostLevel >= maxBoostLevel)
            return;

        EnsureRewardedAdManager();
        if (RewardedAdManager.Instance == null)
            return;

        Debug.Log(
            $"[BoostAdFlow] Tap ready={RewardedAdManager.Instance.IsRewardedAdReady()} " +
            $"loading={RewardedAdManager.Instance.IsRewardedAdLoading} waiting={isWaitingForRewardedAd} showing={isShowingUpgradeAd}");

        void OnAdFinished(bool success)
        {
            Debug.Log($"[BoostAdFlow] Callback success={success}");
            isShowingUpgradeAd = false;
            isWaitingForRewardedAd = false;

            if (success)
                GrantBoostLevelFromAd();
            else
                UpdateBoostButtonInteractable();

            KeepRewardedAdReadyInBackground();
        }

        if (RewardedAdManager.Instance.IsRewardedAdReady())
        {
            isWaitingForRewardedAd = true;
            isShowingUpgradeAd = true;
            RewardedAdManager.Instance.TryShowBoostRewardedAd(OnAdFinished);
            return;
        }

        // Ad isn't ready yet — notify on every tap and keep preloading.
        ShowAdNotLoadedNotification();
        KeepRewardedAdReadyInBackground();
    }

    private void GrantBoostLevelFromAd()
    {
        if (boostLevel >= maxBoostLevel)
            return;

        AudioManager.PlayBtnSfx();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        boostLevel++;
        ApplyBoostSettings();
        SaveBoostProgress();

        UpdateBoostLevelUI();
        UpdateBoostButtonInteractable();
    }

    private void LoadLaunchForceLevel()
    {
        launchForceUpgrades.Load();
        launchForceUpgrades.ApplyToLauncher(dragLauncher);

        GetRubberBandVisual()?.ApplyLaunchForceLevel(launchForceLevel);
    }
    
    public void IncreaseLaunchForce()
    {
        if (isUpgrading)
            return;

        ResolveUIReferences();

        if (launchForceLevel >= maxLaunchForceLevel)
            return;

        int clickCost = GetLaunchForceClickCost();
        if (dragLauncher == null)
        {
            Debug.LogWarning("Launch force upgrade failed: no SimpleDragLauncher found in scene.");
            return;
        }

        SyncPlayerCoins();
        if (playerCoins < clickCost)
        {
            HandleInsufficientCoinsClick(increaseLaunchForceBtn, UpgradeAdOfferType.LaunchForce);
            return;
        }

        if (!TrySpendCoins(clickCost))
            return;

        AudioManager.PlayBtnSfx();
        VibrationManager.Instance.VibrateButtonClick();
        UpdateCoinUI();

        launchForceUpgrades.RegisterClick();

        RefreshAllUpgradeCostUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceSliderUI();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();

        if (launchForceClickCount >= clicksRequired)
        {
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.LaunchForce);
            StartCoroutine(LaunchForceUpgradeSequence());
            return;
        }

        SaveLaunchForceProgress();
        UpdateAdEligibilityAfterPaidUpgrade(UpgradeAdOfferType.LaunchForce);
    }

    private void LoadCoinMultiplierLevel()
    {
        coinMultiplierUpgrades.Load();
    }

    public void IncreaseCoinMultiplier()
    {
        if (isUpgrading)
            return;

        if (IsCoinMultiplierMax())
            return;

        int cost = GetCoinMultiplierClickCost();
        SyncPlayerCoins();
        if (playerCoins < cost)
        {
            HandleInsufficientCoinsClick(increaseCoinMultiplierBtn, UpgradeAdOfferType.CoinMultiplier);
            return;
        }

        AudioManager.PlayBtnSfx();
        VibrationManager.Instance.VibrateButtonClick();

        if (!TrySpendCoins(cost))
            return;

        UpdateCoinUI();

        coinMultiplierUpgrades.RegisterClick();
        bool batchComplete = coinMultiplierClickCount == 0;
        SaveCoinMultiplierProgress();

        RefreshAllUpgradeCostUI();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();

        if (batchComplete || IsCoinMultiplierMax())
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.CoinMultiplier);
        else
            UpdateAdEligibilityAfterPaidUpgrade(UpgradeAdOfferType.CoinMultiplier);
    }

    public void SettingBtn(){

        AudioManager.PlayBtnSfx();
        VibrationManager.Instance.VibrateButtonClick();
        SettingTab.SetActive(true);
    }
       
    public void LevelBtn()
    {
        AudioManager.PlayBtnSfx();
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

