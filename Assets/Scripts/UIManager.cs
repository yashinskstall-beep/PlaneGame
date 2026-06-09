using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public CameraManager cameraManager;
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

    private bool scoreCalculated = false;
    private bool isGoalReached = false;

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

        UpdateBackButton();
        UpdateBoostCounter();
        boostBtn.SetActive(false);
    }
    
    void Update()
    {
        UpdateBackButton();

        if (planeController == null || boosters == null || boostBtn == null) return;

        // Check if boost uses are depleted
        if (planeController.boostUsesRemaining <= 0)
        {
            boostBtn.SetActive(false);
        }
        else if (planeController.isControlling == true && boosters.activeSelf == true )
        {
           boostBtn.SetActive(true);

        }else{
            boostBtn.SetActive(false);          
        }

      
        // Check if we should show the score UI
       CheckScoreUI();
    }

   

    private void CheckScoreUI()
    {
        if (cameraFollow != null && cameraFollow.isCameraZoomedOut && !scoreCalculated)
        {
           
            Invoke("ScoreUI",3f);
        }
    }
 
    public void ScoreUI()
    {
        //btnAudio.Stop();    
        if (ScoreUIScreen == null)
        {
            Debug.LogWarning("ScoreUIScreen is not assigned in UIManager");
            return;
        }
        
        if (distanceText != null && planeController != null)
        {
            distanceText.text = $"Distance: {planeController.maxZDistance:F0}m";
        } 

        if (scoreCalculated) return; // Prevent multiple calculations
        scoreCalculated = true;

        int distance = Mathf.RoundToInt(planeController.maxZDistance);
        BestDistanceLayer.RecordFlightDistance(planeController.maxZDistance);

        float coinMultiplier = CoinManager.Instance != null
            ? CoinManager.Instance.GetCoinMultiplier()
            : 1f;
        int coinsEarned = Mathf.RoundToInt(distance * 2 * coinMultiplier);

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(coinsEarned);
        }

        if (finalScoreText != null)
        {
            // This text is for the ScoreScreenUI - animate the counter
            StartCoroutine(AnimateCoinCounter(coinsEarned));
        }
          // 👇 Change title text based on goal status
        if (titleText != null)
        {
            titleText.text = isGoalReached ? "Congratulations!" : "Nice Flight!";
        }

        ScoreUIScreen.SetActive(true);
        Debug.Log("Score UI activated");
    }

    public void RestartGame()
    {
        audioManager.btnSFX();
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
        if (ScoreUIScreen != null)
            ScoreUIScreen.SetActive(false);

        if (thisCanvas != null)
            thisCanvas.SetActive(false);

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(true);

        var mainMenu = FindObjectOfType<MainMenu>();
        if (mainMenu != null)
            mainMenu.SetLevelsPanelOpen(true);
    }

    public void GoalScreen()
    {
        isGoalReached = true;
        LevelsUI.UnlockLevel(desertLevelIndex);
        Invoke(nameof(ScoreUI), 2f);
    }

    public void ResetGoalReached()
    {
        isGoalReached = false;
    }

    private IEnumerator AnimateCoinCounter(int targetCoins)
    {
        float duration = 1.5f; // Animation duration in seconds
        float elapsed = 0f;
        int currentCount = 0;
        audioManager.CoinSFX();

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

    public void OnGameplayStarted()
    {
        scoreCalculated = false;

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
    }

    public void OnBackBtnClick()
    {
        if (thisCanvas != null)
            thisCanvas.SetActive(false);

        cameraManager.TransitionToMainMenuCamPos(() =>
        {
            if (mainMenuCanvas != null)
                mainMenuCanvas.SetActive(true);
            else
                Debug.LogError("mainMenuCanvas is not assigned in the UIManager inspector!");
        });
    }

}
