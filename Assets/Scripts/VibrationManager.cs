using UnityEngine;

/// <summary>
/// Singleton manager for handling device vibrations.
/// Can be called from any script using VibrationManager.Instance.Vibrate()
/// </summary>
public class VibrationManager : MonoBehaviour
{
    // Singleton instance
    private static VibrationManager instance;
    public static VibrationManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find existing instance
                instance = FindObjectOfType<VibrationManager>();
                
                // Create new instance if none exists
                if (instance == null)
                {
                    GameObject go = new GameObject("VibrationManager");
                    instance = go.AddComponent<VibrationManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("Vibration Settings")]
    [Tooltip("Enable or disable vibrations globally")]
    public bool vibrationsEnabled = true;
    
    [Header("Vibration Durations (milliseconds)")]
    public long buttonClickDuration = 50;
    public long shortVibrationDuration = 100;
    public long mediumVibrationDuration = 200;
    public long longVibrationDuration = 400;

    private void Awake()
    {
        // Ensure only one instance exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Vibrate for button click (short vibration)
    /// </summary>
    public void VibrateButtonClick()
    {
        if (!vibrationsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(buttonClickDuration);
#endif
    }

    /// <summary>
    /// Short vibration
    /// </summary>
    public void VibrateShort()
    {
        if (!vibrationsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(shortVibrationDuration);
#endif
    }

    /// <summary>
    /// Medium vibration
    /// </summary>
    public void VibrateMedium()
    {
        if (!vibrationsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(mediumVibrationDuration);
#endif
    }

    /// <summary>
    /// Long vibration
    /// </summary>
    public void VibrateLong()
    {
        if (!vibrationsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(longVibrationDuration);
#endif
    }

    /// <summary>
    /// Custom duration vibration
    /// </summary>
    /// <param name="milliseconds">Duration in milliseconds</param>
    public void VibrateCustom(long milliseconds)
    {
        if (!vibrationsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(milliseconds);
#endif
    }

    /// <summary>
    /// Internal method to trigger vibration on Android
    /// </summary>
    private void Vibrate(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Vibration failed: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// Cancel any ongoing vibration
    /// </summary>
    public void CancelVibration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    {
                        vibrator.Call("cancel");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Cancel vibration failed: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// Toggle vibrations on/off
    /// </summary>
    public void ToggleVibrations(bool enabled)
    {
        vibrationsEnabled = enabled;
        PlayerPrefs.SetInt("VibrationsEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load vibration settings
    /// </summary>
    private void Start()
    {
        vibrationsEnabled = PlayerPrefs.GetInt("VibrationsEnabled", 1) == 1;
    }
}
