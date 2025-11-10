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


    private List<GameObject> parts;
    private int currentIndex = 0;
    private int clickCount = 0;
    private const int clicksRequired = 6;
    private float currentCost = 10;
    private int playerCoins;
    private AudioSource audioSource;


    void Start()
    {
        parts = new List<GameObject> { leftWing, rightWing, tail };
        audioSource = GetComponent<AudioSource>();
        audioSource.Stop();
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

        // Update UI
        UpdateCoinUI();
        UpdateCostUI();
        UpdateSliderUI();
        UpdateButtonInteractable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Panel clicked");
        cameraManager.TransitionToMainMenu();
        audioManager.audioSource.Stop();
    }

    public void ActivateNextPart()
    {

        audioSource.Play();
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
            if (!parts[currentIndex].activeSelf)
            {
                parts[currentIndex].SetActive(true);
                PlayerPrefs.SetInt(parts[currentIndex].name + "_active", 1);
                Debug.Log(parts[currentIndex].name + " activated!");
            }

            currentIndex++;
            clickCount = 0;
           // currentCost = 10f; // reset for next part
            UpdateSliderUI();
            UpdateCostUI();
        }

        // If all parts are now active → show MAX
        if (currentIndex >= parts.Count)
        {
            SetMaxStateUI();
        }

        SaveProgress();
        UpdateButtonInteractable();
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
                upgradeSlider.maxValue = clicksRequired - 1;
                upgradeSlider.value = clickCount;
            }
        }
    }

    private void UpdateButtonInteractable()
    {
        if (upgradeButton != null)
        {
            // Disable button if out of coins or fully upgraded
            upgradeButton.interactable = playerCoins >= currentCost && currentIndex < parts.Count;
        }
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
}
