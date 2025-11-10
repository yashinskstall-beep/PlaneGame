using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public PlaneController planeController;
    public GameObject boostBtn;
    public bool boostBtnActive = false;
    public GameObject boosters;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI titleText;
    public GameObject ScoreUIScreen;
    public SimpleCameraFollow cameraFollow;
    //public GameObject pointB;
    public GameObject goalScreenUI;
    public  AudioSource btnAudio;
    private bool scoreCalculated = false;
    private bool isGoalReached = false;

    void Start()
    {
        btnAudio = GetComponent<AudioSource>();
        btnAudio.Stop();
        // Initialize references if not set in the inspector
        if (planeController == null)
            planeController = FindObjectOfType<PlaneController>();

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<SimpleCameraFollow>();

        if (cameraFollow == null)
            Debug.LogWarning("SimpleCameraFollow component not found. Score UI may not work correctly.");

    }
    
    void Update()
    {
        if (planeController == null || boosters == null || boostBtn == null) return;

        if (planeController.isControlling == true && boosters.activeSelf == true )
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
        btnAudio.Stop();    
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

        int distance = (int)planeController.maxZDistance;
        int coinsEarned = distance / 10; // 1 coin for every 10 meters

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(coinsEarned);
        }

        if (finalScoreText != null)
        {
            // This text is for the ScoreScreenUI
           finalScoreText.text = $"Coins + {coinsEarned}";

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
        btnAudio.Play();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
    

    public void GoalScreen()
    {
       isGoalReached = true;

       Invoke("ScoreUI", 2f); 
    }
    
    
   
}
