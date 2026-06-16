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
    public GameObject leftWing;
    public GameObject rightWing;
    public GameObject tail;
    public TextMeshProUGUI coinText;
    public Slider upgradeSlider;
    public Button upgradeButton;
    public TextMeshProUGUI costText;
    public AudioManager audioManager;
    public GameObject taptoplay;
    public Button BackBtn;
    public Button boostEnableBtn;
    public TextMeshProUGUI boostCostText;
    public GameObject PlaneBoosters;
    public GameObject notEnoughCoinsU;
    public GameObject notEnoughCoinsS;
    public Button increaseLaunchForceBtn;
    public TextMeshProUGUI launchForceCostText;
    public TextMeshProUGUI launchForceLevelText;
    public Slider launchForceSlider;
    [Header("Coin Multiplier Upgrade")]
    public Button increaseCoinMultiplierBtn;
    public TextMeshProUGUI coinMultiplierCostText;
    public TextMeshProUGUI coinMultiplierLevelText;
    public Slider coinMultiplierSlider;
    public GameObject notEnoughCoinsC;
    public SimpleDragLauncher dragLauncher;
    public GameObject boostactive;
    public GameObject SettingTab;
    public GameObject levelsPanel;
    public LevelsUI levelsUI;
    public Button levelBtn;

    [Header("Camera Focus Points")]
    public Transform leftWingFocusPoint;
    public Transform rightWingFocusPoint;
    public Transform tailFocusPoint;

    [Header("Particle Effects")]
    public GameObject upgradeParticleEffect;
    [Tooltip("Added to the spawn position Y when the upgrade particle plays.")]
    public float upgradeParticleYOffset = 0f;

    [Header("Boost Button")]
    [SerializeField] private float boostSlideOffsetX = 500f;
    [SerializeField] private float boostSlideDuration = 0.4f;

    [Header("Timing c")]
    public float cameraTransitionDuration = 1.5f;
    public float particleEffectDuration = 1.0f;


    private List<GameObject> parts;
    private List<Transform> partFocusPoints;
    private int currentIndex = 0;
    private int clickCount = 0;
    private const int clicksRequired = 5;
    private float currentCost = 10;
    private int playerCoins;
 // private AudioSource audioSource;
    private bool isUpgrading = false;
    
    // Launch force level system
    private int launchForceLevel = 1;
    private const int maxLaunchForceLevel = 3;
    private readonly float[] launchForceLevels = { 25f, 30f, 35f };
    private readonly int[] launchForceCosts = { 700, 1000, 1500 };

    private int coinMultiplierLevel = 1;
    private const int maxCoinMultiplierLevel = 11;
    private const float coinMultiplierStep = 0.1f;
    private readonly int[] coinMultiplierCosts = { 600, 900, 1200, 1800, 2500, 3500, 5000, 7000, 10000, 15000, 20000 };
    private const int BoostCost = 500;

    private RectTransform boostButtonRect;
    private Vector2 boostButtonRestPosition;
    private bool boostButtonRestCaptured;
    private bool boostButtonShown;
    private Coroutine boostButtonSlideCoroutine;

    void OnEnable()
    {
        RefreshEconomyUI();
    }

    void Start()
    {
        parts = new List<GameObject> { leftWing, rightWing, tail };
        partFocusPoints = new List<Transform> { leftWingFocusPoint, rightWingFocusPoint, tailFocusPoint };

        if (LevelProgress.ConsumeGameplayResetPending())
            ResetUpgradesForNewLevel();
        else
            InitializeFromSavedProgress();

        taptoplay.SetActive(true);
        CaptureBoostButtonRestPosition();
        if (boostEnableBtn != null)
        {
            boostEnableBtn.gameObject.SetActive(false);
            boostButtonShown = false;
        }

        RefreshAllUpgradeUI();
    }

    public string[] GetUpgradePartNames()
    {
        var names = new List<string>(3);
        if (leftWing != null)
            names.Add(leftWing.name);
        if (rightWing != null)
            names.Add(rightWing.name);
        if (tail != null)
            names.Add(tail.name);
        return names.ToArray();
    }

    public void ResetUpgradesForNewLevel()
    {
        isUpgrading = false;
        currentIndex = 0;
        clickCount = 0;
        currentCost = 10f;
        launchForceLevel = 1;
        coinMultiplierLevel = 1;

        if (parts == null)
            parts = new List<GameObject> { leftWing, rightWing, tail };

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == null)
                continue;

            parts[i].SetActive(false);
            PlayerPrefs.DeleteKey(LevelProgress.GetPartActiveKey(parts[i].name));
        }

        if (PlaneBoosters != null)
            PlaneBoosters.SetActive(false);
        if (boostactive != null)
            boostactive.SetActive(false);

        if (dragLauncher != null)
            dragLauncher.launchForceMultiplier = launchForceLevels[0];

        PlayerPrefs.SetInt("Upgrade_CurrentIndex", 0);
        PlayerPrefs.SetInt("Upgrade_ClickCount", 0);
        PlayerPrefs.SetFloat("Upgrade_CurrentCost", currentCost);
        PlayerPrefs.SetInt("LaunchForceLevel", 1);
        PlayerPrefs.SetFloat("LaunchForceMultiplier", launchForceLevels[0]);
        PlayerPrefs.SetInt("CoinMultiplierLevel", 1);
        PlayerPrefs.SetFloat("CoinMultiplier", 1f);
        PlayerPrefs.Save();

        ShowUpgradeButtons();
        HideUpgradeWarnings();
        HideBoostButtonInstant();
        SetLevelsPanelOpen(false);
        SyncPlayerCoins();
    }

    private void InitializeFromSavedProgress()
    {
        LoadProgress();
        ApplyPartStatesFromSave();

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].activeSelf)
                currentIndex = Mathf.Max(currentIndex, i + 1);
        }

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

    private void HideUpgradeWarnings()
    {
        if (notEnoughCoinsU != null)
            notEnoughCoinsU.SetActive(false);
        if (notEnoughCoinsS != null)
            notEnoughCoinsS.SetActive(false);
        if (notEnoughCoinsC != null)
            notEnoughCoinsC.SetActive(false);
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
        Debug.Log("Panel clicked");
        cameraManager.TransitionToStartCamPos();
        //audioManager.audioSource.Stop();
    }

    public void ActivateNextPart()
    {
        if (isUpgrading)
        {
            Debug.Log("Upgrade already in progress!");
            return;
        }

        //audioSource.Play();
        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        Debug.Log("audio was Played");
        if (currentIndex >= parts.Count)
        {
            Debug.Log("All parts active — Fully upgraded!");
            SetMaxStateUI();
            return;
        }

        if (!TrySpendCoins((int)currentCost))
        {
            Debug.Log("Not enough coins!");
            return;
        }

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
        isUpgrading = true;
        upgradeButton.interactable = false;
        taptoplay.SetActive(false);
        yield return new WaitForSeconds(0.3f);
        HideBoostButtonInstant();
        upgradeButton.gameObject.SetActive(false);
        increaseLaunchForceBtn.gameObject.SetActive(false);
        if (increaseCoinMultiplierBtn != null)
            increaseCoinMultiplierBtn.gameObject.SetActive(false);
        notEnoughCoinsS.SetActive(false);
        if (notEnoughCoinsC != null)
            notEnoughCoinsC.SetActive(false);
        

        // Step 1: Transition camera to the part
        if (currentIndex < partFocusPoints.Count && partFocusPoints[currentIndex] != null)
        {
            Debug.Log($"Transitioning camera to {parts[currentIndex].name}");
            yield return StartCoroutine(cameraManager.TransitionToTarget(partFocusPoints[currentIndex], cameraTransitionDuration));
        }

        // Step 2: Play particle effect
        if (upgradeParticleEffect != null && currentIndex < parts.Count)
        {
            Vector3 partPosition = parts[currentIndex].transform.position;
            
            // Adjust particle position based on part type
            if (currentIndex == 0) // Left wing
            {
                partPosition.x -= 0.3f;
            }
            else if (currentIndex == 1) // Right wing
            {
                partPosition.x += 0.3f;
            }
            else if (currentIndex == 2) // Tail
            {
                partPosition.z -= 0.4f;
            }

            partPosition.y += upgradeParticleYOffset;

            audioManager.PlanepartSFX();
            GameObject particleInstance = Instantiate(upgradeParticleEffect, partPosition, Quaternion.identity);
            Debug.Log($"Playing particle effect at {parts[currentIndex].name}");
            
            // Wait for particle effect duration
            yield return new WaitForSeconds(particleEffectDuration);
            
            // Clean up particle effect
            Destroy(particleInstance, 2f);
        }

        // Step 3: Enable the part
        if (!parts[currentIndex].activeSelf)
        {
            parts[currentIndex].SetActive(true);
            PlayerPrefs.SetInt(LevelProgress.GetPartActiveKey(parts[currentIndex].name), 1);
            Debug.Log(parts[currentIndex].name + " activated!");
        }

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
        // If all parts are now active → show MAX
        if (currentIndex >= parts.Count)
        {
            SetMaxStateUI();
        }

        SaveProgress();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        isUpgrading = false;
    }

    // -----------------------------
    // 🧠 UI Helpers
    // -----------------------------

    private void ApplyPartStatesFromSave()
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == null)
                continue;

            bool active = PlayerPrefs.GetInt(LevelProgress.GetPartActiveKey(parts[i].name), 0) == 1;
            parts[i].SetActive(active);
        }
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
            if (currentIndex >= parts.Count)
                costText.text = "MAX";
            else
                costText.text = $"{FormatNumber(currentCost)}";
        }
    }

    private void UpdateSliderUI()
    {
        if (upgradeSlider != null)
        {
            if (currentIndex >= parts.Count)
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
            // Disable button if out of coins or fully upgraded
            bool hasEnoughCoins = playerCoins >= currentCost && currentIndex < parts.Count;
            upgradeButton.interactable = hasEnoughCoins;
            
            // Show/hide not enough coins UI
            if (notEnoughCoinsU != null)
            {
                // Show warning only if not enough coins AND not fully upgraded
                notEnoughCoinsU.SetActive(!hasEnoughCoins && currentIndex < parts.Count);
            }
        }
    }

    private void UpdateBoostCostUI()
    {
        if (boostCostText != null)
        {
            boostCostText.text = BoostCost.ToString();
        }
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
        if (launchForceSlider != null)
        {
            launchForceSlider.minValue = 0;
            launchForceSlider.maxValue = 1;
            
            // Calculate slider value based on level (0 to 1)
            if (maxLaunchForceLevel > 1)
            {
                launchForceSlider.value = (float)(launchForceLevel - 1) / (maxLaunchForceLevel - 1);
            }
            else
            {
                launchForceSlider.value = 1f;
            }
        }
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
            // Disable button if not enough coins or max level reached
            bool canAffordLaunchForce = false;
            bool isMaxLevel = launchForceLevel >= maxLaunchForceLevel;
            
            if (!isMaxLevel)
            {
                int currentCostForLevel = launchForceCosts[launchForceLevel - 1];
                canAffordLaunchForce = playerCoins >= currentCostForLevel;
            }
            
            increaseLaunchForceBtn.interactable = canAffordLaunchForce;

            // Only show "not enough coins" warning if NOT at max level and can't afford
            if(notEnoughCoinsS != null){
                notEnoughCoinsS.SetActive(!canAffordLaunchForce && !isMaxLevel);
            }
        }
    }

    private float GetCoinMultiplierValue()
    {
        return 1f + (coinMultiplierLevel - 1) * coinMultiplierStep;
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

        coinMultiplierSlider.minValue = 0;
        coinMultiplierSlider.maxValue = 1;

        if (maxCoinMultiplierLevel > 1)
            coinMultiplierSlider.value = (float)(coinMultiplierLevel - 1) / (maxCoinMultiplierLevel - 1);
        else
            coinMultiplierSlider.value = 1f;
    }

    private void UpdateIncreaseCoinMultiplierButtonInteractable()
    {
        if (increaseCoinMultiplierBtn == null)
            return;

        bool isMaxLevel = coinMultiplierLevel >= maxCoinMultiplierLevel;
        bool canAfford = false;

        if (!isMaxLevel)
        {
            int cost = coinMultiplierCosts[coinMultiplierLevel - 1];
            canAfford = playerCoins >= cost;
        }

        increaseCoinMultiplierBtn.interactable = canAfford;

        if (notEnoughCoinsC != null)
            notEnoughCoinsC.SetActive(!canAfford && !isMaxLevel);
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
        SyncPlayerCoins();
        UpdateCoinUI();
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
            : PlayerPrefs.GetInt("PlayerCoins", 0);
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
            PlayerPrefs.SetInt("PlayerCoins", playerCoins);
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
        PlayerPrefs.SetInt("Upgrade_CurrentIndex", currentIndex);
        PlayerPrefs.SetInt("Upgrade_ClickCount", clickCount);
        PlayerPrefs.SetFloat("Upgrade_CurrentCost", currentCost);
        SyncPlayerCoins();
        PlayerPrefs.SetInt("PlayerCoins", playerCoins);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        currentIndex = PlayerPrefs.GetInt("Upgrade_CurrentIndex", 0);
        clickCount = PlayerPrefs.GetInt("Upgrade_ClickCount", 0);
        currentCost = PlayerPrefs.GetFloat("Upgrade_CurrentCost", 10f);
        SyncPlayerCoins();
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
            upgradeButton.interactable = false;
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
            
            Debug.Log($"Boosters enabled! {BoostCost} coins deducted.");
        }
        else
        {
            Debug.Log("Not enough coins or boosters already active!");
        }
    }

    private void LoadLaunchForceLevel()
    {
        // Load saved launch force level
        launchForceLevel = PlayerPrefs.GetInt("LaunchForceLevel", 1);
        
        // Ensure level is within bounds
        launchForceLevel = Mathf.Clamp(launchForceLevel, 1, maxLaunchForceLevel);
        
        // Set the launch force multiplier based on level
        if (dragLauncher != null)
        {
            dragLauncher.launchForceMultiplier = launchForceLevels[launchForceLevel - 1];
        }
    }
    
    public void IncreaseLaunchForce()
    {
        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();
        // Check if already at max level
        if (launchForceLevel >= maxLaunchForceLevel)
        {
            Debug.Log("Launch force already at max level!");
            return;
        }
        
        int currentCostForLevel = launchForceCosts[launchForceLevel - 1];

        if (TrySpendCoins(currentCostForLevel))
        {
            UpdateCoinUI();

            // Increase level
            launchForceLevel++;
            PlayerPrefs.SetInt("LaunchForceLevel", launchForceLevel);
            
            // Set new launch force multiplier
            dragLauncher.launchForceMultiplier = launchForceLevels[launchForceLevel - 1];
            PlayerPrefs.SetFloat("LaunchForceMultiplier", dragLauncher.launchForceMultiplier);
            PlayerPrefs.Save();

            // Update all UI
            UpdateLaunchForceCostUI();
            UpdateLaunchForceLevelUI();
            UpdateLaunchForceSliderUI();
            UpdateIncreaseLaunchForceButtonInteractable();
            UpdateButtonInteractable();
            UpdateBoostButtonInteractable();
            UpdateIncreaseCoinMultiplierButtonInteractable();

            Debug.Log($"Launch force upgraded to Level {launchForceLevel}! Force: {dragLauncher.launchForceMultiplier}");
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    private void LoadCoinMultiplierLevel()
    {
        coinMultiplierLevel = PlayerPrefs.GetInt("CoinMultiplierLevel", 1);
        coinMultiplierLevel = Mathf.Clamp(coinMultiplierLevel, 1, maxCoinMultiplierLevel);
    }

    public void IncreaseCoinMultiplier()
    {
        audioManager.btnSFX();
        VibrationManager.Instance.VibrateButtonClick();

        if (coinMultiplierLevel >= maxCoinMultiplierLevel)
        {
            Debug.Log("Coin multiplier already at max level!");
            return;
        }

        int cost = coinMultiplierCosts[coinMultiplierLevel - 1];
        if (!TrySpendCoins(cost))
        {
            Debug.Log("Not enough coins!");
            return;
        }

        UpdateCoinUI();

        coinMultiplierLevel++;
        PlayerPrefs.SetInt("CoinMultiplierLevel", coinMultiplierLevel);
        PlayerPrefs.SetFloat("CoinMultiplier", GetCoinMultiplierValue());
        PlayerPrefs.Save();

        UpdateCoinMultiplierCostUI();
        UpdateCoinMultiplierLevelUI();
        UpdateCoinMultiplierSliderUI();
        UpdateIncreaseCoinMultiplierButtonInteractable();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
        UpdateIncreaseLaunchForceButtonInteractable();

        Debug.Log($"Coin multiplier upgraded to {GetCoinMultiplierValue():0.#}x (level {coinMultiplierLevel})");
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

