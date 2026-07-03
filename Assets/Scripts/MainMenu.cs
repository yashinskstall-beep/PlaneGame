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

    [Header("Boost Button")]
    [SerializeField] private float boostSlideOffsetX = 500f;
    [SerializeField] private float boostSlideDuration = 0.4f;
    [SerializeField] private float boostMaxLevelHideDelay = 10f;

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
    [SerializeField] private GameObject cheatCoinsButton;
    [Tooltip("When enabled, the cheat coins button is also shown on Android builds.")]
    [SerializeField] private bool showCheatCoinsOnAndroid;
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
    
    [Header("Launch Force Upgrade")]
    [SerializeField] private float[] launchForceLevels = { 25f, 30f, 35f };
    [SerializeField] private int[] launchForceCosts = { 700, 1000, 1500 };
    [Tooltip("Optional slingshot band visual. Auto-resolved from drag launcher if empty.")]
    public RubberBandVisual rubberBandVisual;

    [Header("Boost Upgrade")]
    [Tooltip("Boost level durations. Level 1 uses index 0, level 5 uses index 4.")]
    [SerializeField] private float[] boostDurations = { 2f, 2.5f, 3f, 3.5f, 4f };
    [Tooltip("Boost upgrade costs for each level.")]
    [SerializeField] private int[] boostCosts = { 500, 500, 500, 500, 500 };

    // Launch force level system
    private int launchForceLevel = 1;
    private int launchForceClickCount = 0;
    private float launchForceCurrentCost = 700f;
    private int maxLaunchForceLevel => launchForceLevels != null && launchForceLevels.Length > 0
        ? launchForceLevels.Length
        : 1;

    private int boostLevel = 0;
    private int maxBoostLevel => boostDurations != null && boostDurations.Length > 0
        ? boostDurations.Length
        : 1;

    private int coinMultiplierLevel = 1;
    private int coinMultiplierClickCount = 0;
    private const int maxCoinMultiplierLevel = 11;
    private const float coinMultiplierStep = 0.1f;
    private readonly int[] coinMultiplierCosts = { 600, 900, 1200, 1800, 2500, 3500, 5000, 7000, 10000, 15000, 20000 };

    private static readonly Color UpgradeCostAffordableColor = Color.white;
    private static readonly Color UpgradeCostUnaffordableColor = new Color(1f, 0.55f, 0.55f);

    private RectTransform boostButtonRect;
    private Vector2 boostButtonRestPosition;
    private bool boostButtonRestCaptured;
    private bool boostButtonShown;
    private Coroutine boostButtonSlideCoroutine;
    private Coroutine boostButtonMaxLevelHideCoroutine;

    void Awake()
    {
        menuPanelImage = GetComponent<Image>();
        ResolveSceneReferences();
        EnsureLaunchForceConfig();
        EnsureBoostConfig();
    }

    private void OnValidate()
    {
        EnsureLaunchForceConfig();
        EnsureBoostConfig();
    }

    private void EnsureLaunchForceConfig()
    {
        if (launchForceLevels == null || launchForceLevels.Length == 0)
            launchForceLevels = new[] { 25f, 30f, 35f };

        if (launchForceCosts == null || launchForceCosts.Length == 0)
            launchForceCosts = new[] { 700, 1000, 1500 };

        if (launchForceCosts.Length != launchForceLevels.Length)
        {
            int[] resizedCosts = new int[launchForceLevels.Length];
            for (int i = 0; i < resizedCosts.Length; i++)
                resizedCosts[i] = i < launchForceCosts.Length ? launchForceCosts[i] : launchForceCosts[launchForceCosts.Length - 1];
            launchForceCosts = resizedCosts;
        }
    }

    private float GetLaunchForceMultiplierForLevel(int level)
    {
        EnsureLaunchForceConfig();
        int index = Mathf.Clamp(level - 1, 0, launchForceLevels.Length - 1);
        return launchForceLevels[index];
    }

    private int GetLaunchForceCostForLevel(int level)
    {
        EnsureLaunchForceConfig();
        int index = Mathf.Clamp(level - 1, 0, launchForceCosts.Length - 1);
        return launchForceCosts[index];
    }

    private void EnsureBoostConfig()
    {
        if (boostDurations == null || boostDurations.Length == 0)
            boostDurations = new[] { 2f, 2.5f, 3f, 3.5f, 4f };

        if (boostCosts == null || boostCosts.Length == 0)
            boostCosts = new[] { 500, 500, 500, 500, 500 };

        if (boostCosts.Length != boostDurations.Length)
        {
            int[] resizedCosts = new int[boostDurations.Length];
            for (int i = 0; i < resizedCosts.Length; i++)
                resizedCosts[i] = i < boostCosts.Length ? boostCosts[i] : boostCosts[boostCosts.Length - 1];
            boostCosts = resizedCosts;
        }
    }

    private float GetBoostDurationForLevel(int level)
    {
        EnsureBoostConfig();
        int index = Mathf.Clamp(level - 1, 0, boostDurations.Length - 1);
        return boostDurations[index];
    }

    private int GetBoostCostForLevel(int level)
    {
        EnsureBoostConfig();
        int index = Mathf.Clamp(level - 1, 0, boostCosts.Length - 1);
        return boostCosts[index];
    }

    void OnEnable()
    {
        ResolveSceneReferences();
        ResolveUIReferences();
        CacheUpgradeButtonTransitions();
        EnsureUpgradeShakeComponents();
        ResetUpgradeAdUiForMainMenu();
        SetupRewardedAdUpgrades();
        ApplyCheatCoinsButtonVisibility();
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
        launchForceCurrentCost = GetLaunchForceCostForLevel(1);
        boostLevel = 0;
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

        ApplyBoostSettings();

        if (dragLauncher != null)
            dragLauncher.launchForceMultiplier = GetLaunchForceMultiplierForLevel(1);

        GetRubberBandVisual()?.ApplyLaunchForceLevel(1);

        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierLevelKey, 1);
        PlayerPrefs.SetInt(LevelProgress.CoinMultiplierClickCountKey, 0);
        PlayerPrefs.SetFloat(LevelProgress.CoinMultiplierValueKey, 1f);
        SaveLaunchForceProgress();
        SaveBoostProgress();
        SaveProgress();
        PlayerPrefs.Save();

        ShowUpgradeButtons();
        HideBoostButtonInstant();
        SetLevelsPanelOpen(false);
        adEligibleUpgradeButton = null;
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
        adEligibleUpgradeButton = null;
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

    private bool planeUpgradeAdRevealed;
    private bool launchForceUpgradeAdRevealed;
    private bool coinMultiplierUpgradeAdRevealed;
    private bool isShowingUpgradeAd;

    /// <summary>
    /// Only this upgrade button may show an ad — set after the player spends coins on it
    /// and can no longer afford the next click on the same button.
    /// </summary>
    private UpgradeAdOfferType? adEligibleUpgradeButton;

    private bool IsUpgradeAdRevealed(UpgradeAdOfferType offerType)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return planeUpgradeAdRevealed;
            case UpgradeAdOfferType.LaunchForce:
                return launchForceUpgradeAdRevealed;
            case UpgradeAdOfferType.CoinMultiplier:
                return coinMultiplierUpgradeAdRevealed;
            default:
                return false;
        }
    }

    private void SetUpgradeAdRevealed(UpgradeAdOfferType offerType, bool revealed)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                planeUpgradeAdRevealed = revealed;
                break;
            case UpgradeAdOfferType.LaunchForce:
                launchForceUpgradeAdRevealed = revealed;
                break;
            case UpgradeAdOfferType.CoinMultiplier:
                coinMultiplierUpgradeAdRevealed = revealed;
                break;
        }
    }

    private void ClearAllUpgradeAdOffers()
    {
        planeUpgradeAdRevealed = false;
        launchForceUpgradeAdRevealed = false;
        coinMultiplierUpgradeAdRevealed = false;
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
                return coinMultiplierLevel >= maxCoinMultiplierLevel;
            default:
                return true;
        }
    }

    private void UpdateAdEligibilityAfterPaidUpgrade(UpgradeAdOfferType offerType)
    {
        SyncPlayerCoins();
        MarkUpgradeAdsUnlocked();

        ClearAllUpgradeAdOffers();
        adEligibleUpgradeButton = null;

        if (!IsUpgradeAtMax(offerType) && !CanAffordUpgrade(offerType))
        {
            adEligibleUpgradeButton = offerType;
            SetUpgradeAdRevealed(offerType, true);
        }

        RefreshUpgradeAdStates();
    }

    /// <summary>
    /// After a full unlock (wing, slingshot level, etc.) ads stay off until the player
    /// spends coins on a normal upgrade click again.
    /// </summary>
    private void SuppressAdAfterFullUpgrade(UpgradeAdOfferType offerType)
    {
        if (adEligibleUpgradeButton == offerType)
            adEligibleUpgradeButton = null;

        SetUpgradeAdRevealed(offerType, false);
        RefreshUpgradeAdStates();
    }

    private void ClearUpgradeAdOffer(UpgradeAdOfferType offerType)
    {
        SetUpgradeAdRevealed(offerType, false);
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

    private void PlayInsufficientCoinsShake(Button button, System.Action onComplete = null)
    {
        if (button == null)
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

    private void EnsureRewardedAdManager()
    {
        if (RewardedAdManager.Instance == null)
        {
            GameObject adManagerObject = new GameObject("RewardedAdManager");
            adManagerObject.AddComponent<RewardedAdManager>();
        }
    }

    private void SetupRewardedAdUpgrades()
    {
        EnsureRewardedAdManager();
        ResolveUpgradeAdVisuals(planeUpgradeAdVisuals, upgradeButton);
        ResolveUpgradeAdVisuals(launchForceUpgradeAdVisuals, increaseLaunchForceBtn);
        ResolveUpgradeAdVisuals(coinMultiplierUpgradeAdVisuals, increaseCoinMultiplierBtn);
        ClearAllUpgradeAdOffers();
        ResetUpgradeAdVisuals();
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
        if (IsUpgradeInAdMode(offerType))
        {
            TryShowUpgradeRewardedAd(offerType);
            return;
        }

        OnInsufficientCoinsForUpgrade(button, offerType);
    }

    private void OnInsufficientCoinsForUpgrade(Button button, UpgradeAdOfferType offerType)
    {
        PlayInsufficientCoinsShake(button, () =>
        {
            SyncPlayerCoins();
            if (IsUpgradeAdBlocked(offerType) || CanAffordUpgrade(offerType))
                return;

            if (adEligibleUpgradeButton != offerType)
                return;

            SetUpgradeAdRevealed(offerType, true);
            RefreshUpgradeAdStates();
        });
    }

    private bool CanAffordUpgrade(UpgradeAdOfferType offerType)
    {
        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return playerCoins >= currentCost;
            case UpgradeAdOfferType.LaunchForce:
                return playerCoins >= GetLaunchForceClickCost();
            case UpgradeAdOfferType.CoinMultiplier:
                return playerCoins >= GetCoinMultiplierClickCost();
            default:
                return false;
        }
    }

    private bool IsUpgradeInAdMode(UpgradeAdOfferType offerType)
    {
        if (adEligibleUpgradeButton != offerType)
            return false;

        if (!IsUpgradeAdRevealed(offerType) || isUpgrading || isShowingUpgradeAd)
            return false;

        if (RewardedAdManager.Instance == null || !RewardedAdManager.Instance.IsRewardedAdReady())
            return false;

        if (IsUpgradeAdBlocked(offerType))
            return false;

        SyncPlayerCoins();

        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return !IsFullyUpgraded() && playerCoins < currentCost;
            case UpgradeAdOfferType.LaunchForce:
                return launchForceLevel < maxLaunchForceLevel && playerCoins < GetLaunchForceClickCost();
            case UpgradeAdOfferType.CoinMultiplier:
                return coinMultiplierLevel < maxCoinMultiplierLevel && playerCoins < GetCoinMultiplierClickCost();
            default:
                return false;
        }
    }

    private void RefreshUpgradeAdStates()
    {
        SyncPlayerCoins();

        if (planeUpgradeAdRevealed && (IsFullyUpgraded() || CanAffordUpgrade(UpgradeAdOfferType.PlanePart)))
        {
            planeUpgradeAdRevealed = false;
            if (adEligibleUpgradeButton == UpgradeAdOfferType.PlanePart)
                adEligibleUpgradeButton = null;
        }

        if (launchForceUpgradeAdRevealed && (launchForceLevel >= maxLaunchForceLevel || CanAffordUpgrade(UpgradeAdOfferType.LaunchForce)))
        {
            launchForceUpgradeAdRevealed = false;
            if (adEligibleUpgradeButton == UpgradeAdOfferType.LaunchForce)
                adEligibleUpgradeButton = null;
        }

        if (coinMultiplierUpgradeAdRevealed && (coinMultiplierLevel >= maxCoinMultiplierLevel || CanAffordUpgrade(UpgradeAdOfferType.CoinMultiplier)))
        {
            coinMultiplierUpgradeAdRevealed = false;
            if (adEligibleUpgradeButton == UpgradeAdOfferType.CoinMultiplier)
                adEligibleUpgradeButton = null;
        }

        bool planeAdMode = IsUpgradeInAdMode(UpgradeAdOfferType.PlanePart);
        bool launchForceAdMode = IsUpgradeInAdMode(UpgradeAdOfferType.LaunchForce);
        bool coinMultiplierAdMode = IsUpgradeInAdMode(UpgradeAdOfferType.CoinMultiplier);

        ApplyUpgradeAdVisual(planeUpgradeAdVisuals, planeAdMode);
        ApplyUpgradeAdVisual(launchForceUpgradeAdVisuals, launchForceAdMode);
        ApplyUpgradeAdVisual(coinMultiplierUpgradeAdVisuals, coinMultiplierAdMode);

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
            bool isMaxLevel = coinMultiplierLevel >= maxCoinMultiplierLevel;
            bool canAfford = !isMaxLevel && playerCoins >= GetCoinMultiplierClickCost();

            if (coinMultiplierAdMode)
                ApplyUpgradeButtonAdModeState(increaseCoinMultiplierBtn, coinMultiplierButtonTransition);
            else
                ApplyUpgradeButtonState(increaseCoinMultiplierBtn, coinMultiplierButtonTransition, canAfford, isMaxLevel);
        }
    }

    private bool IsUpgradeAdBlocked(UpgradeAdOfferType offerType)
    {
        if (isUpgrading || isShowingUpgradeAd || RewardedAdManager.Instance == null)
            return true;

        switch (offerType)
        {
            case UpgradeAdOfferType.PlanePart:
                return IsFullyUpgraded();
            case UpgradeAdOfferType.LaunchForce:
                return launchForceLevel >= maxLaunchForceLevel;
            case UpgradeAdOfferType.CoinMultiplier:
                return coinMultiplierLevel >= maxCoinMultiplierLevel;
            default:
                return true;
        }
    }

    private void TryShowUpgradeRewardedAd(UpgradeAdOfferType offerType)
    {
        if (isUpgrading || isShowingUpgradeAd || !IsUpgradeInAdMode(offerType))
            return;

        UpgradeAdOfferType savedOffer = offerType;
        isShowingUpgradeAd = true;
        SetUpgradeAdRevealed(offerType, false);
        RefreshUpgradeAdStates();

        RewardedAdManager.Instance.ShowRewardedAd(success =>
        {
            isShowingUpgradeAd = false;

            if (success)
                GrantFreeUpgradeClick(savedOffer);
            else
            {
                SetUpgradeAdRevealed(savedOffer, true);
                RefreshUpgradeAdStates();
            }
        });
    }

    private void GrantFreeUpgradeClick(UpgradeAdOfferType offerType)
    {
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

        if (adEligibleUpgradeButton == offerType
            && !IsUpgradeAtMax(offerType)
            && !CanAffordUpgrade(offerType))
        {
            SetUpgradeAdRevealed(offerType, true);
        }

        RefreshUpgradeAdStates();
    }

    private void GrantFreePlaneUpgradeClick()
    {
        if (isUpgrading || IsFullyUpgraded())
            return;

        if (audioManager != null)
            audioManager.btnSFX();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        clickCount++;
        currentCost *= 1.5f;

        UpdateCoinUI();
        UpdateCostUI();
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

        if (audioManager != null)
            audioManager.btnSFX();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        launchForceClickCount++;
        launchForceCurrentCost *= 1.5f;

        UpdateLaunchForceCostUI();
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
        if (isUpgrading || coinMultiplierLevel >= maxCoinMultiplierLevel)
            return;

        if (audioManager != null)
            audioManager.btnSFX();
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        coinMultiplierClickCount++;
        bool leveledUp = coinMultiplierClickCount >= clicksRequired;

        if (leveledUp)
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

        if (leveledUp)
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.CoinMultiplier);
    }

    private void ResolveSceneReferences()
    {
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

#if UNITY_EDITOR
        cheatCoinsButton.SetActive(true);
#elif UNITY_ANDROID
        cheatCoinsButton.SetActive(showCheatCoinsOnAndroid);
#else
        cheatCoinsButton.SetActive(false);
#endif
    }

    private void RefreshAllUpgradeUI()
    {
        UpdateCoinUI();
        UpdateCostUI();
        UpdateSliderUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateBoostCostUI();
        UpdateBoostLevelUI();
        UpdateLaunchForceCostUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceSliderUI();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateCoinMultiplierCostUI();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        RefreshUpgradeAdStates();
        RefreshUpgradeCostColors();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUpgrading)
            return;

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
            HandleInsufficientCoinsClick(upgradeButton, UpgradeAdOfferType.PlanePart);
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
        audioManager.PlanepartSFX();
        yield return StartCoroutine(PlaySlingshotUpgradeParticlesRoutine(slingshotTarget));

        launchForceClickCount = 0;
        launchForceCurrentCost = GetLaunchForceCostForLevel(launchForceLevel);
        UpdateLaunchForceSliderUI();
        UpdateLaunchForceLevelUI();
        UpdateLaunchForceCostUI();

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

    private void ApplySlingshotUpgradeLevel()
    {
        launchForceLevel = Mathf.Clamp(launchForceLevel + 1, 1, maxLaunchForceLevel);

        if (dragLauncher != null)
        {
            dragLauncher.launchForceMultiplier = GetLaunchForceMultiplierForLevel(launchForceLevel);
            dragLauncher.ResetForNewLaunch();
        }

        GetRubberBandVisual()?.ApplyLaunchForceLevel(launchForceLevel);
    }

    private IEnumerator PlaySlingshotUpgradeParticlesRoutine(GameObject slingshotTarget)
    {
        if (slingshotTarget == null)
        {
            ApplySlingshotUpgradeLevel();
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        ApplySlingshotUpgradeLevel();

        var spawnedInstances = new List<GameObject>();
        var particleSystems = new List<ParticleSystem>();

        TryAddUpgradeParticlePrefab(upgradeParticleEffect, slingshotTarget, spawnedInstances, particleSystems);
        AddParticleSystemsFromRoot(slingshotTarget, particleSystems);

        bool hasParticles = particleSystems.Count > 0;

        if (!hasParticles)
        {
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        ActivateVfxChildren(slingshotTarget);
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
            yield return new WaitForSeconds(particleEffectDuration);
            yield break;
        }

        if (!part.activeSelf && planeUpgradeConfig != null)
            planeUpgradeConfig.UnlockPart(index);

        var spawnedInstances = new List<GameObject>();
        var particleSystems = new List<ParticleSystem>();

        TryAddUpgradeParticlePrefab(upgradeParticleEffect, part, spawnedInstances, particleSystems);

        // In-scene particle systems on the part (smoke children, etc.)
        AddParticleSystemsFromRoot(part, particleSystems);

        PlaneEffects planeEffects = GetPlaneEffects();

        bool hasParticles = particleSystems.Count > 0;

        if (!hasParticles)
        {
            yield return new WaitForSeconds(particleEffectDuration);
            planeEffects?.RefreshFlightTrails();
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

        float waitDuration = Mathf.Max(
            particleEffectDuration,
            GetParticleDuration(particleSystems.ToArray()));

        yield return new WaitForSeconds(waitDuration > 0f ? waitDuration : particleEffectDuration);

        foreach (GameObject spawned in spawnedInstances)
        {
            if (spawned != null)
                Destroy(spawned, 2f);
        }

        CleanupUpgradeVfx(particleSystems, planeEffects);
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

    private void CleanupAllPlaneUpgradeVfx()
    {
        if (planeUpgradeConfig == null || planeUpgradeConfig.planeController == null)
            return;

        GameObject plane = planeUpgradeConfig.planeController.gameObject;
        PlaneEffects planeEffects = plane.GetComponent<PlaneEffects>();
        var particleSystems = new List<ParticleSystem>();

        AddParticleSystemsFromRoot(plane, particleSystems);
        CleanupUpgradeVfx(particleSystems, planeEffects);
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
        bool atMax = IsFullyUpgraded();
        bool canAfford = playerCoins >= currentCost;
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

    private void RefreshUpgradeCostColors()
    {
        SyncPlayerCoins();

        if (costText != null)
            ApplyUpgradeCostTextColor(costText, playerCoins >= currentCost, IsFullyUpgraded());

        if (launchForceCostText != null)
            ApplyUpgradeCostTextColor(launchForceCostText, playerCoins >= GetLaunchForceClickCost(), launchForceLevel >= maxLaunchForceLevel);

        if (coinMultiplierCostText != null)
            ApplyUpgradeCostTextColor(coinMultiplierCostText, playerCoins >= GetCoinMultiplierClickCost(), coinMultiplierLevel >= maxCoinMultiplierLevel);
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
            bool planeAdMode = IsUpgradeInAdMode(UpgradeAdOfferType.PlanePart);
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
        if (launchForceLevel >= maxLaunchForceLevel)
            return 0;

        return Mathf.Max(1, Mathf.RoundToInt(launchForceCurrentCost));
    }

    private void SaveLaunchForceProgress()
    {
        PlayerPrefs.SetInt(LevelProgress.GetLaunchForceLevelKey(), launchForceLevel);
        PlayerPrefs.SetInt(LevelProgress.GetLaunchForceClickCountKey(), launchForceClickCount);
        PlayerPrefs.SetFloat(LevelProgress.GetLaunchForceCurrentCostKey(), launchForceCurrentCost);

        if (dragLauncher != null)
            PlayerPrefs.SetFloat(LevelProgress.GetLaunchForceMultiplierKey(), dragLauncher.launchForceMultiplier);

        PlayerPrefs.Save();
    }

    private int GetBoostNextCost()
    {
        if (boostLevel >= maxBoostLevel)
            return 0;

        return GetBoostCostForLevel(boostLevel + 1);
    }

    private void SaveBoostProgress()
    {
        PlayerPrefs.SetInt(LevelProgress.GetBoostLevelKey(), boostLevel);
        PlayerPrefs.SetFloat(LevelProgress.GetBoostDurationKey(), boostLevel > 0 ? GetBoostDurationForLevel(boostLevel) : 0f);
        PlayerPrefs.Save();
    }

    private void LoadBoostLevel()
    {
        boostLevel = PlayerPrefs.GetInt(LevelProgress.GetBoostLevelKey(), 0);
        boostLevel = Mathf.Clamp(boostLevel, 0, maxBoostLevel);
        ApplyBoostSettings();
    }

    private void ApplyBoostSettings()
    {
        ResolveSceneReferences();

        float duration = boostLevel > 0 ? GetBoostDurationForLevel(boostLevel) : 0f;
        if (planeController != null)
            planeController.boostDuration = duration;

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

    private void UpdateBoostButtonInteractable()
    {
        if (boostEnableBtn == null)
            return;

        CaptureBoostButtonRestPosition();
        if (boostButtonRect == null)
            return;

        bool isMaxLevel = boostLevel >= maxBoostLevel;
        bool canAfford = !isMaxLevel && playerCoins >= GetBoostNextCost();
        boostEnableBtn.interactable = !isMaxLevel;
        boostEnableBtn.transition = canAfford ? upgradeButtonTransition : Selectable.Transition.None;

        ButtonScaleAnimation scaleAnim = boostEnableBtn.GetComponent<ButtonScaleAnimation>();
        if (scaleAnim != null)
            scaleAnim.enabled = canAfford;

        if (!isMaxLevel)
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

        SyncPlayerCoins();
        bool atMax = coinMultiplierLevel >= maxCoinMultiplierLevel;
        bool canAfford = playerCoins >= GetCoinMultiplierClickCost();
        string amount = atMax ? "MAX" : coinMultiplierCosts[coinMultiplierLevel - 1].ToString();
        coinMultiplierCostText.text = amount;
        ApplyUpgradeCostTextColor(coinMultiplierCostText, canAfford, atMax);
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
        UpdateCostUI();
        UpdateLaunchForceCostUI();
        UpdateCoinMultiplierCostUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        RefreshUpgradeAdStates();
        RefreshUpgradeCostColors();
    }

    /// <summary>
    /// Called when the main menu is shown again after gameplay.
    /// </summary>
    public void OnReturnedToMainMenu()
    {
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
        if (boostLevel >= maxBoostLevel)
            return;

        int cost = GetBoostNextCost();
        SyncPlayerCoins();
        if (playerCoins < cost)
        {
            PlayInsufficientCoinsShake(boostEnableBtn);
            Debug.Log("Not enough coins!");
            return;
        }

        if (!TrySpendCoins(cost))
            return;

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        UpdateCoinUI();

        boostLevel++;
        ApplyBoostSettings();
        SaveBoostProgress();

        UpdateBoostCostUI();
        UpdateBoostButtonInteractable();
        UpdateButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
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

        launchForceCurrentCost = PlayerPrefs.GetFloat(
            LevelProgress.GetLaunchForceCurrentCostKey(),
            GetLaunchForceCostForLevel(launchForceLevel));

        if (dragLauncher != null)
            dragLauncher.launchForceMultiplier = GetLaunchForceMultiplierForLevel(launchForceLevel);

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

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        UpdateCoinUI();

        launchForceClickCount++;
        launchForceCurrentCost *= 1.5f;

        UpdateLaunchForceCostUI();
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
        coinMultiplierLevel = PlayerPrefs.GetInt(LevelProgress.CoinMultiplierLevelKey, 1);
        coinMultiplierClickCount = PlayerPrefs.GetInt(LevelProgress.CoinMultiplierClickCountKey, 0);
        coinMultiplierLevel = Mathf.Clamp(coinMultiplierLevel, 1, maxCoinMultiplierLevel);
        coinMultiplierClickCount = Mathf.Clamp(coinMultiplierClickCount, 0, clicksRequired - 1);

        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
            coinMultiplierClickCount = 0;
    }

    public void IncreaseCoinMultiplier()
    {
        if (isUpgrading)
            return;

        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
            return;

        int cost = GetCoinMultiplierClickCost();
        SyncPlayerCoins();
        if (playerCoins < cost)
        {
            HandleInsufficientCoinsClick(increaseCoinMultiplierBtn, UpgradeAdOfferType.CoinMultiplier);
            return;
        }

        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();

        if (!TrySpendCoins(cost))
            return;

        UpdateCoinUI();

        coinMultiplierClickCount++;
        bool leveledUp = coinMultiplierClickCount >= clicksRequired;

        if (leveledUp)
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

        if (leveledUp)
            SuppressAdAfterFullUpgrade(UpgradeAdOfferType.CoinMultiplier);
        else
            UpdateAdEligibilityAfterPaidUpgrade(UpgradeAdOfferType.CoinMultiplier);
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

