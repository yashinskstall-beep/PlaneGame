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
    public Button boostEnableBtn;
    public GameObject PlaneBoosters;
    public GameObject notEnoughCoinsU;
    public GameObject notEnoughCoinsB;

    [Header("Camera Focus Points")]
    public Transform leftWingFocusPoint;
    public Transform rightWingFocusPoint;
    public Transform tailFocusPoint;

    [Header("Particle Effects")]
    public GameObject upgradeParticleEffect;

    [Header("Timing Settings")]
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


    void Start()
    {
        parts = new List<GameObject> { leftWing, rightWing, tail };
        partFocusPoints = new List<Transform> { leftWingFocusPoint, rightWingFocusPoint, tailFocusPoint };
        //audioSource = GetComponent<AudioSource>();
        //audioSource.Stop();
        // Load saved data
        LoadProgress();

        // Load part states
        for (int i = 0; i < parts.Count; i++)
        {
            if (PlayerPrefs.GetInt(parts[i].name + "_active", 0) == 1)
                parts[i].SetActive(true);
        }

        // Set current index if not loaded
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].activeSelf)
                currentIndex = Mathf.Max(currentIndex, i + 1);
        }
        taptoplay.SetActive(true);
        if (boostEnableBtn != null)
            boostEnableBtn.gameObject.SetActive(true);

        // Update UI
        UpdateCoinUI();
        UpdateCostUI();
        UpdateSliderUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Panel clicked");
        cameraManager.TransitionToMainMenu();
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
        Debug.Log("audio was Played");
        if (currentIndex >= parts.Count)
        {
            Debug.Log("All parts active — Fully upgraded!");
            SetMaxStateUI();
            return;
        }

        if (playerCoins < currentCost)
        {
            Debug.Log("Not enough coins!");
            
            return;
        }

        // Deduct cost
        playerCoins -= (int)currentCost;
        PlayerPrefs.SetInt("PlayerCoins", playerCoins);

        // Progress and cost update
        clickCount++;
        currentCost *= 1.5f;

        UpdateCoinUI();
        UpdateCostUI();
        UpdateSliderUI();

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
        if (boostEnableBtn != null)
            boostEnableBtn.gameObject.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
        

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
            else if (currentIndex ==2)// Tail
            {
                partPosition.z-=0.4f;
            }
           
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
            PlayerPrefs.SetInt(parts[currentIndex].name + "_active", 1);
            Debug.Log(parts[currentIndex].name + " activated!");
        }

        currentIndex++;
        clickCount = 0;
        UpdateSliderUI();
        UpdateCostUI();

        // Step 4: Transition camera back to main menu
        yield return StartCoroutine(cameraManager.TransitionToTarget(cameraManager.mainMenuPosition, cameraTransitionDuration));
        taptoplay.SetActive(true);
        if (boostEnableBtn != null)
            boostEnableBtn.gameObject.SetActive(true);
        upgradeButton.gameObject.SetActive(true);
        // If all parts are now active → show MAX
        if (currentIndex >= parts.Count)
        {
            SetMaxStateUI();
        }

        SaveProgress();
        UpdateButtonInteractable();
        isUpgrading = false;
    }

    // -----------------------------
    // 🧠 UI Helpers
    // -----------------------------

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
                costText.text = $"Cost: {FormatNumber(currentCost)}";
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

    private void UpdateBoostButtonInteractable()
    {
        if (boostEnableBtn != null)
        {
            // Disable button if not enough coins or boosters already active
            bool canAffordBoost = playerCoins >= 50 && !PlaneBoosters.activeSelf;
            boostEnableBtn.interactable = canAffordBoost;
            
            // Show/hide not enough coins UI for boost
            if (notEnoughCoinsB != null)
            {
                // Show warning only if not enough coins AND boosters not already active
                notEnoughCoinsB.SetActive(!canAffordBoost && !PlaneBoosters.activeSelf);
            }
        }
    }

    public void CheatCoins()
    {
        playerCoins += 10000;
        UpdateCoinUI();
        UpdateButtonInteractable();
        UpdateBoostButtonInteractable();
    }

    // -----------------------------
    // 🧠 SAVE / LOAD SYSTEM
    // -----------------------------

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("Upgrade_CurrentIndex", currentIndex);
        PlayerPrefs.SetInt("Upgrade_ClickCount", clickCount);
        PlayerPrefs.SetFloat("Upgrade_CurrentCost", currentCost);
        PlayerPrefs.SetInt("PlayerCoins", playerCoins);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        currentIndex = PlayerPrefs.GetInt("Upgrade_CurrentIndex", 0);
        clickCount = PlayerPrefs.GetInt("Upgrade_ClickCount", 0);
        currentCost = PlayerPrefs.GetFloat("Upgrade_CurrentCost", 10f);
        playerCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
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
        // Check if player has at least 50 coins and boosters not already active
        if (playerCoins >= 50 && !PlaneBoosters.activeSelf)
        {
            // Deduct 50 coins
            playerCoins -= 50;
            PlayerPrefs.SetInt("PlayerCoins", playerCoins);
            PlayerPrefs.Save();
            
            // Update coin UI
            UpdateCoinUI();
            
            // Enable the boosters
            PlaneBoosters.SetActive(true);
            
            // Update button interactability
            UpdateBoostButtonInteractable();
            
            Debug.Log("Boosters enabled! 50 coins deducted.");
        }
        else
        {
            Debug.Log("Not enough coins or boosters already active!");
        }
    }

   
}
