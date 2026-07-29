using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FlightHUD : MonoBehaviour
{
    public CameraTransitionController cameraManager;
    public PlaneController planeController;
    public GameObject boostBtn;
    public Button BackBtn;
    public bool boostBtnActive = false;
    public GameObject boosters;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI titleText;
    public GameObject ScoreUIScreen;
    public SimpleCameraFollow cameraFollow;
    public SimpleDragLauncher dragLauncher;
    public AudioManager audioManager;
    //public GameObject pointB;
    public GameObject goalScreenUI;
    // public  AudioSource btnAudio;
    public TextMeshProUGUI boostCounterText;
    public GameObject thisCanvas;
    public GameObject mainMenuCanvas;
    [SerializeField] private int desertLevelIndex = 1;

    [Header("Use Boost Button")]
    [SerializeField] private float boostSlideOffsetX = 500f;
    [SerializeField] private float boostSlideDuration = 0.4f;

    private bool scoreCalculated = false;
    private bool scoreUIScheduled = false;
    private bool isGoalReached = false;
    private Coroutine scoreUIScheduleCoroutine;
    private Coroutine coinCounterCoroutine;
    private RectTransform useBoostButtonRect;
    private Vector2 useBoostButtonRestPosition;
    private bool useBoostButtonRestCaptured;
    private bool useBoostButtonShown;
    private bool useBoostButtonWantsShown;
    private Coroutine useBoostButtonSlideCoroutine;

    void Start()
    {
        //btnAudio = GetComponent<AudioSource>();
        //btnAudio.Stop();
        // Initialize references if not set in the inspector
        if (planeController == null)
            planeController = FindObjectOfType<PlaneController>();

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<SimpleCameraFollow>();

        if (cameraFollow == null)
            Debug.LogWarning("SimpleCameraFollow component not found. Score UI may not work correctly.");

        if (dragLauncher == null)
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();

        ResolveSceneReferences();
        UpdateBackButton();
        UpdateBoostCounter();
        CaptureUseBoostButtonRestPosition();
        HideUseBoostButtonInstant();
        RewardedAdManager.EnsureExists();
        RewardedAdManager.Instance?.SetKeepRewardedAdWarm(true);
        RewardedAdManager.Instance?.EnsureRewardedAdPreloaded();
    }
    
    void Update()
    {
        UpdateBackButton();
        CheckScoreUI();

        if (planeController == null || boostBtn == null)
            return;

        UpdateUseBoostButtonVisibility();
    }

   

    private void ResolveSceneReferences()
    {
        if (planeController == null)
            planeController = FindObjectOfType<PlaneController>();

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<SimpleCameraFollow>();

        if (dragLauncher == null)
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();

        AudioManager.Get(ref audioManager);

        if (boosters == null && planeController != null)
        {
            Transform boostersTransform = planeController.transform.Find("Boosters");
            if (boostersTransform != null)
                boosters = boostersTransform.gameObject;
        }
    }

    private void CheckScoreUI()
    {
        if (scoreCalculated || scoreUIScheduled)
            return;

        ResolveSceneReferences();

        if (cameraFollow != null && cameraFollow.isCameraZoomedOut)
            ScheduleScoreUI(3f);
    }

    public void OnLandingMarkerPlaced()
    {
        if (scoreCalculated || scoreUIScheduled)
            return;

        ScheduleScoreUI(3f);
    }

    private void ScheduleScoreUI(float delaySeconds)
    {
        if (scoreCalculated || scoreUIScheduled)
            return;

        scoreUIScheduled = true;
        CancelInvoke(nameof(ScoreUI));

        if (scoreUIScheduleCoroutine != null)
            StopCoroutine(scoreUIScheduleCoroutine);

        scoreUIScheduleCoroutine = StartCoroutine(ShowScoreAfterDelay(delaySeconds));
    }

    private void CancelScoreUISchedule()
    {
        CancelInvoke(nameof(ScoreUI));

        if (scoreUIScheduleCoroutine != null)
        {
            StopCoroutine(scoreUIScheduleCoroutine);
            scoreUIScheduleCoroutine = null;
        }

        scoreUIScheduled = false;
    }

    private IEnumerator ShowScoreAfterDelay(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        scoreUIScheduleCoroutine = null;
        scoreUIScheduled = false;
        ScoreUI();
    }

    public void ScoreUI()
    {
        if (scoreCalculated)
            return;

        ResolveSceneReferences();

        if (ScoreUIScreen == null)
        {
            Debug.LogWarning("ScoreUIScreen is not assigned in FlightHUD");
            return;
        }

        if (planeController == null)
        {
            Debug.LogWarning("PlaneController is not assigned in FlightHUD");
            return;
        }

        if (distanceText != null)
            distanceText.text = $"Distance: {planeController.maxZDistance:F0}m";

        int distance = Mathf.RoundToInt(planeController.maxZDistance);
        int coinsEarned = FlightRewards.CalculateCoins(distance, planeController.LastFlightWasMisfire);

        if (titleText != null)
        {
            if (planeController.LastFlightWasMisfire)
                titleText.text = "Try Again!";
            else
                titleText.text = isGoalReached ? "Congratulations!" : "Nice Flight!";
        }

        ScoreUIScreen.SetActive(true);
        scoreCalculated = true;
        CancelScoreUISchedule();

        FlightRewards.AwardCoins(coinsEarned);
        RefreshMainMenuCoins();

        BestDistanceLayer.RecordFlightDistance(planeController.maxZDistance);

        CancelPendingLevelCompleteAd();

        if (finalScoreText != null)
            coinCounterCoroutine = StartCoroutine(AnimateCoinCounter(coinsEarned, showLevelCompleteAd: isGoalReached));
        else if (isGoalReached)
            ShowLevelCompleteAd();
    }

    private void RefreshMainMenuCoins()
    {
        MainMenu mainMenu = FindObjectOfType<MainMenu>(true);
        if (mainMenu != null)
            mainMenu.OnReturnedToMainMenu();
    }

    public void RestartGame()
    {
        AudioManager.PlayBtnSfx();
        VibrationManager.Instance.VibrateButtonClick();

        if (isGoalReached)
            Invoke(nameof(ShowLevelsPanel), 0.5f);
        else
            Invoke(nameof(LoadCurrentScene), 0.5f);
    }

    private void LoadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ShowLevelsPanel()
    {
        CancelPendingLevelCompleteAd();

        if (ScoreUIScreen != null)
            ScoreUIScreen.SetActive(false);

        if (thisCanvas != null)
            thisCanvas.SetActive(false);

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(true);

        var mainMenu = FindObjectOfType<MainMenu>(true);
        if (mainMenu != null)
        {
            mainMenu.OnReturnedToMainMenu();
            mainMenu.SetLevelsPanelOpen(true);
        }
    }

    public void GoalScreen()
    {
        isGoalReached = true;
        FlightRewards.OnGoalReached(desertLevelIndex);
        ScheduleScoreUI(2f);
    }

    private void ShowLevelCompleteAd()
    {
        if (!isGoalReached)
            return;

        if (ScoreUIScreen == null || !ScoreUIScreen.activeInHierarchy)
            return;

        RewardedAdManager.EnsureExists();
        RewardedAdManager.Instance?.ShowInterstitialAd();
    }

    public void CancelPendingLevelCompleteAd()
    {
        if (coinCounterCoroutine != null)
        {
            StopCoroutine(coinCounterCoroutine);
            coinCounterCoroutine = null;
        }
    }

    public void ResetGoalReached()
    {
        isGoalReached = false;
    }

    private IEnumerator AnimateCoinCounter(int targetCoins, bool showLevelCompleteAd)
    {
        float duration = 1.5f; // Animation duration in seconds
        float elapsed = 0f;
        int currentCount = 0;
        AudioManager.PlayCoinSfx();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Use easing for smoother animation (ease-out)
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            currentCount = Mathf.RoundToInt(Mathf.Lerp(0, targetCoins, easedProgress));
            finalScoreText.text = $"Coins + {currentCount}";
            
            yield return null;
        }

        // Ensure we end exactly at the target value
        finalScoreText.text = $"Coins + {targetCoins}";
        coinCounterCoroutine = null;

        if (showLevelCompleteAd)
            ShowLevelCompleteAd();
    }

    /// <summary>
    /// Updates the boost counter display with the current remaining boost uses
    /// </summary>
    public void UpdateBoostCounter()
    {
        if (boostCounterText != null && planeController != null)
        {
            boostCounterText.text = $"{planeController.boostUsesRemaining}X";
        }
    }

    private void UpdateBackButton()
    {
        if (BackBtn == null || dragLauncher == null)
            return;

        bool showBack = !dragLauncher.IsDragging && !dragLauncher.released;
        if (BackBtn.gameObject.activeSelf != showBack)
            BackBtn.gameObject.SetActive(showBack);
    }

    private void CaptureUseBoostButtonRestPosition()
    {
        if (boostBtn == null)
            return;

        useBoostButtonRect = boostBtn.GetComponent<RectTransform>();
        if (useBoostButtonRect == null || useBoostButtonRestCaptured)
            return;

        useBoostButtonRestPosition = useBoostButtonRect.anchoredPosition;
        useBoostButtonRestCaptured = true;
    }

    private void UpdateUseBoostButtonVisibility()
    {
        if (boostBtn == null)
            return;

        CaptureUseBoostButtonRestPosition();
        if (useBoostButtonRect == null)
            return;

        bool boostersActive = boosters == null || boosters.activeSelf;
        bool canShowUseBoost = planeController.boostUsesRemaining > 0
            && planeController.isControlling
            && boostersActive;

        if (canShowUseBoost == useBoostButtonWantsShown)
            return;

        useBoostButtonWantsShown = canShowUseBoost;

        if (canShowUseBoost)
            ShowUseBoostButtonSlideIn();
        else
            HideUseBoostButtonSlideOut();
    }

    private void ShowUseBoostButtonSlideIn()
    {
        if (useBoostButtonRect == null)
            return;

        if (useBoostButtonSlideCoroutine != null)
            StopCoroutine(useBoostButtonSlideCoroutine);

        useBoostButtonSlideCoroutine = StartCoroutine(AnimateUseBoostButton(true));
    }

    private void HideUseBoostButtonSlideOut()
    {
        if (useBoostButtonRect == null)
            return;

        if (useBoostButtonSlideCoroutine != null)
            StopCoroutine(useBoostButtonSlideCoroutine);

        useBoostButtonSlideCoroutine = StartCoroutine(AnimateUseBoostButton(false));
    }

    private void HideUseBoostButtonInstant()
    {
        if (useBoostButtonSlideCoroutine != null)
        {
            StopCoroutine(useBoostButtonSlideCoroutine);
            useBoostButtonSlideCoroutine = null;
        }

        useBoostButtonShown = false;
        useBoostButtonWantsShown = false;
        if (boostBtn != null)
            boostBtn.SetActive(false);
    }

    private IEnumerator AnimateUseBoostButton(bool slideIn)
    {
        Vector2 hiddenPos = useBoostButtonRestPosition + new Vector2(boostSlideOffsetX, 0f);
        Vector2 start = slideIn ? hiddenPos : useBoostButtonRect.anchoredPosition;
        Vector2 end = slideIn ? useBoostButtonRestPosition : hiddenPos;

        if (slideIn)
        {
            boostBtn.SetActive(true);
            useBoostButtonRect.anchoredPosition = hiddenPos;
        }

        float elapsed = 0f;
        while (elapsed < boostSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / boostSlideDuration);
            useBoostButtonRect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        useBoostButtonRect.anchoredPosition = end;

        if (!slideIn)
            boostBtn.SetActive(false);

        useBoostButtonShown = slideIn;
        useBoostButtonSlideCoroutine = null;
    }

    public void OnGameplayStarted()
    {
        ResolveSceneReferences();
        scoreCalculated = false;
        CancelScoreUISchedule();
        CancelPendingLevelCompleteAd();

        if (ScoreUIScreen != null)
            ScoreUIScreen.SetActive(false);

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        if (thisCanvas != null)
            thisCanvas.SetActive(true);

        if (dragLauncher != null)
            dragLauncher.ResetForNewLaunch();

        UpdateBoostCounter();
        UpdateBackButton();
        HideUseBoostButtonInstant();
    }

    public void OnBackBtnClick()
    {
        CancelPendingLevelCompleteAd();

        if (thisCanvas != null)
            thisCanvas.SetActive(false);

        cameraManager.TransitionToMainMenuCamPos(() =>
        {
            if (mainMenuCanvas != null)
                mainMenuCanvas.SetActive(true);
            else
                Debug.LogError("mainMenuCanvas is not assigned in the FlightHUD inspector!");

            RefreshMainMenuCoins();
        });
    }

}
