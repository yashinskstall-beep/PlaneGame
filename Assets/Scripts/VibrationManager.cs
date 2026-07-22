using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Singleton manager for handling device vibrations.
/// Can be called from any script using VibrationManager.Instance.Vibrate()
/// </summary>
public class VibrationManager : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    // iOS Haptic Feedback imports
    [DllImport("__Internal")]
    private static extern void _TriggerImpactLight();
    
    [DllImport("__Internal")]
    private static extern void _TriggerImpactMedium();
    
    [DllImport("__Internal")]
    private static extern void _TriggerImpactHeavy();
    
    [DllImport("__Internal")]
    private static extern void _TriggerSelection();
    
    [DllImport("__Internal")]
    private static extern void _TriggerNotificationSuccess();
    
    [DllImport("__Internal")]
    private static extern void _TriggerNotificationWarning();
    
    [DllImport("__Internal")]
    private static extern void _TriggerNotificationError();
    
    [DllImport("__Internal")]
    private static extern void _StartContinuousHaptic();
    
    [DllImport("__Internal")]
    private static extern void _StopContinuousHaptic();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static bool isVibrating = false;
#endif

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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        // DontDestroyOnLoad only works on root objects.
        if (transform.parent != null)
            transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        SettingsManager.LoadSavedSettings();
        ApplyVibrationEnabled(SettingsManager.IsVibrationEnabled);

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidVibrator();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeAndroidVibrator()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Vibrator initialization failed: {e.Message}");
        }
    }
#endif

    /// <summary>
    /// Vibrate for button click (short vibration)
    /// </summary>
    public void ApplyVibrationEnabled(bool enabled)
    {
        vibrationsEnabled = enabled;
        if (!enabled)
            CancelVibration();
    }

    private bool CanVibrate()
    {
        return vibrationsEnabled && SettingsManager.IsVibrationEnabled;
    }

    public void VibrateButtonClick()
    {
        if (!CanVibrate()) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(buttonClickDuration);
#elif UNITY_IOS && !UNITY_EDITOR
        _TriggerSelection();
#endif
    }

    /// <summary>
    /// Short vibration
    /// </summary>
    public void VibrateShort()
    {
        if (!CanVibrate()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(shortVibrationDuration);
#elif UNITY_IOS && !UNITY_EDITOR
        _TriggerImpactLight();
#endif
    }

    /// <summary>
    /// Error-style haptic for denied actions (e.g. not enough coins).
    /// </summary>
    public void VibrateDenied()
    {
        if (!CanVibrate()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(80);
#elif UNITY_IOS && !UNITY_EDITOR
        _TriggerNotificationError();
#endif
    }

    /// <summary>
    /// Medium vibration
    /// </summary>
    public void VibrateMedium()
    {
        if (!CanVibrate()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(mediumVibrationDuration);
#elif UNITY_IOS && !UNITY_EDITOR
        _TriggerImpactMedium();
#endif
    }

    /// <summary>
    /// Long vibration
    /// </summary>
    public void VibrateLong()
    {
        if (!CanVibrate()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(longVibrationDuration);
#elif UNITY_IOS && !UNITY_EDITOR
        _TriggerImpactHeavy();
#endif
    }

    /// <summary>
    /// Custom duration vibration
    /// </summary>
    /// <param name="milliseconds">Duration in milliseconds</param>
    public void VibrateCustom(long milliseconds)
    {
        if (!CanVibrate()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Vibrate(milliseconds);
#elif UNITY_IOS && !UNITY_EDITOR
        // Map duration to appropriate iOS haptic feedback
        if (milliseconds <= 100)
            _TriggerImpactLight();
        else if (milliseconds <= 250)
            _TriggerImpactMedium();
        else
            _TriggerImpactHeavy();
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
    /// Start continuous vibration (for dragging)
    /// </summary>
    public void StartContinuous()
    {
        if (!CanVibrate()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null || isVibrating) return;
        
        try
        {
            // Pattern: delay, vibrate, sleep, repeat index (0 means repeat forever)
            long[] pattern = { 0, 50, 50 };
            vibrator.Call("vibrate", pattern, 0);
            isVibrating = true;
            Debug.Log("Continuous vibration started (Android)");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Start continuous vibration failed: {e.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        _StartContinuousHaptic();
        Debug.Log("Continuous haptic started (iOS)");
#endif
    }
    
    /// <summary>
    /// Stop continuous vibration
    /// </summary>
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null || !isVibrating) return;
        
        try
        {
            vibrator.Call("cancel");
            isVibrating = false;
            Debug.Log("Continuous vibration stopped (Android)");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Stop continuous vibration failed: {e.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        _StopContinuousHaptic();
        Debug.Log("Continuous haptic stopped (iOS)");
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
#elif UNITY_IOS && !UNITY_EDITOR
        // Stop continuous haptic if running
        _StopContinuousHaptic();
#endif
    }

    /// <summary>
    /// Toggle vibrations on/off
    /// </summary>
    public void ToggleVibrations(bool enabled)
    {
        ApplyVibrationEnabled(enabled);
    }
}
