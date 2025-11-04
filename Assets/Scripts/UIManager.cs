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
    public GameObject ScoreUIScreen;
    public SimpleCameraFollow cameraFollow;

    void Start()
    {
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

        MaxDistance();
        
        // Check if we should show the score UI
        if (cameraFollow != null && cameraFollow.isCameraZoomedOut && ScoreUIScreen != null)
        {
            ScoreUI();
        }
    }


    public void MaxDistance()
    {
        if (distanceText != null && planeController != null)
        {
            distanceText.text = $"Distance: {planeController.maxZDistance:F0}m";
        } 
    }    
    public void ScoreUI()
    {
        if (ScoreUIScreen == null)
        {
            Debug.LogWarning("ScoreUIScreen is not assigned in UIManager");
            return;
        }
        
        if (cameraFollow != null && cameraFollow.isCameraZoomedOut)
        {
            ScoreUIScreen.SetActive(true);
            Debug.Log("Score UI activated");
        }
        else
        {
            ScoreUIScreen.SetActive(false);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    
    
   
}
