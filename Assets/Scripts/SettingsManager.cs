using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game settings for audio and vibration toggles
/// Call SetAudioEnabled(bool) and SetVibrationEnabled(bool) from Toggle's OnValueChanged event
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("Audio Manager")]
    [SerializeField] private AudioManager audioManager;

    [Header("Setting Tab")]
    [SerializeField] private GameObject SettingsPanel;
    
    [Header("UI Switcher Toggles")]
    [SerializeField] private GameObject audioToggleObject;
    [SerializeField] private GameObject vibrationToggleObject;
    
    // PlayerPrefs keys
    private const string AUDIO_ENABLED_KEY = "AudioEnabled";
    private const string VIBRATION_ENABLED_KEY = "VibrationEnabled";

    // Static properties for easy access from other scripts
    public static bool IsAudioEnabled { get; private set; } = true;
    public static bool IsVibrationEnabled { get; private set; } = true;
    
    private bool isSyncing = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneCallbacks()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedApplySettings;
        SceneManager.sceneLoaded += OnSceneLoadedApplySettings;
        LoadSavedSettings();
    }

    private static void OnSceneLoadedApplySettings(Scene scene, LoadSceneMode mode)
    {
        LoadSavedSettings();
        ApplySavedAudioState();
        ApplySavedVibrationState();
    }

    /// <summary>
    /// Loads toggle states from PlayerPrefs. Safe to call before the settings UI exists.
    /// </summary>
    public static void LoadSavedSettings()
    {
        IsAudioEnabled = PlayerPrefs.GetInt(AUDIO_ENABLED_KEY, 1) == 1;

        if (PlayerPrefs.HasKey(VIBRATION_ENABLED_KEY))
            IsVibrationEnabled = PlayerPrefs.GetInt(VIBRATION_ENABLED_KEY, 1) == 1;
        else if (PlayerPrefs.HasKey("VibrationsEnabled"))
            IsVibrationEnabled = PlayerPrefs.GetInt("VibrationsEnabled", 1) == 1;
        else
            IsVibrationEnabled = true;
    }

    public static void ApplySavedAudioState()
    {
        AudioManager manager = FindObjectOfType<AudioManager>();
        if (manager != null)
        {
            AudioSource[] audioSources = manager.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in audioSources)
            {
                if (source == null)
                    continue;

                source.mute = !IsAudioEnabled;
                if (!IsAudioEnabled && source.isPlaying)
                    source.Stop();
            }
        }

        AudioListener.volume = IsAudioEnabled ? 1f : 0f;
        AudioListener.pause = !IsAudioEnabled;
    }

    public static void ApplySavedVibrationState()
    {
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.ApplyVibrationEnabled(IsVibrationEnabled);
    }

    private void ApplyVibrationState()
    {
        ApplySavedVibrationState();
    }

    private void Awake()
    {
        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();

        LoadSavedSettings();
        ApplySavedAudioState();
        ApplySavedVibrationState();

        isSyncing = true;
        SyncToggleVisuals();
        Invoke(nameof(ClearSyncFlag), 0.1f);
    }
    
    private void ClearSyncFlag()
    {
        isSyncing = false;
        Debug.Log("[ClearSyncFlag] Sync complete, events now enabled");
    }
    
    private void SyncToggleVisuals()
    {
        // Update audio toggle to match loaded setting
        if (audioToggleObject != null)
        {
            // Try to get any Toggle-like component (works with UISwitcher too since it inherits from Selectable)
            var toggleComponents = audioToggleObject.GetComponents<Component>();
            foreach (var component in toggleComponents)
            {
                // Use reflection to set isOn property if it exists
                var isOnProperty = component.GetType().GetProperty("isOn");
                if (isOnProperty != null && isOnProperty.CanWrite)
                {
                    isOnProperty.SetValue(component, IsAudioEnabled);
                    Debug.Log($"[SyncToggleVisuals] Set audio toggle isOn to {IsAudioEnabled}");
                    break;
                }
            }
        }
        
        // Update vibration toggle to match loaded setting
        if (vibrationToggleObject != null)
        {
            var toggleComponents = vibrationToggleObject.GetComponents<Component>();
            foreach (var component in toggleComponents)
            {
                var isOnProperty = component.GetType().GetProperty("isOn");
                if (isOnProperty != null && isOnProperty.CanWrite)
                {
                    isOnProperty.SetValue(component, IsVibrationEnabled);
                    Debug.Log($"[SyncToggleVisuals] Set vibration toggle isOn to {IsVibrationEnabled}");
                    break;
                }
            }
        }
        
        Debug.Log($"[SyncToggleVisuals] Synced toggles - Audio: {IsAudioEnabled}, Vibration: {IsVibrationEnabled}");
    }
    


    private void LoadSettings()
    {
        LoadSavedSettings();
        ApplySavedVibrationState();
        Debug.Log($"Settings Loaded - Audio: {IsAudioEnabled}, Vibration: {IsVibrationEnabled}");
    }

    /// <summary>
    /// Call this method from Audio Toggle's OnValueChanged event
    /// This version toggles the state instead of reading from the unreliable toggle.isOn property
    /// </summary>
    public void SetAudioEnabled()
    {
        // Ignore events during sync
        if (isSyncing)
        {
            Debug.Log("[SetAudioEnabled] Ignoring event during sync");
            return;
        }
        
        // Toggle the current state
        bool newState = !IsAudioEnabled;
        
        // Debug: Log the state change
        Debug.Log($"[SetAudioEnabled] Toggling from {IsAudioEnabled} to {newState}");
        
        // Log the state change
        if (newState)
            Debug.Log("Audio Toggle: OFF -> ON");
        else
            Debug.Log("Audio Toggle: ON -> OFF");

        IsAudioEnabled = newState;
        PlayerPrefs.SetInt(AUDIO_ENABLED_KEY, newState ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioState();

        Debug.Log($"Audio {(newState ? "Enabled" : "Disabled")}");
    }
    
    /// <summary>
    /// Alternative version that accepts a bool parameter (for backward compatibility)
    /// </summary>
    public void SetAudioEnabled(bool isOn)
    {
        if (isSyncing)
            return;

        if (isOn)
            Debug.Log("Audio Toggle: OFF -> ON");
        else
            Debug.Log("Audio Toggle: ON -> OFF");

        IsAudioEnabled = isOn;
        PlayerPrefs.SetInt(AUDIO_ENABLED_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioState();

        Debug.Log($"Audio {(isOn ? "Enabled" : "Disabled")}");
    }

    /// <summary>
    /// Call this method from Vibration Toggle's OnValueChanged event
    /// This version toggles the state instead of reading from the unreliable toggle.isOn property
    /// </summary>
    public void SetVibrationEnabled()
    {
        // Ignore events during sync
        if (isSyncing)
        {
            Debug.Log("[SetVibrationEnabled] Ignoring event during sync");
            return;
        }
        
        // Toggle the current state
        bool newState = !IsVibrationEnabled;
        
        // Debug: Log the state change
        Debug.Log($"[SetVibrationEnabled] Toggling from {IsVibrationEnabled} to {newState}");
        
        // Log the state change
        if (newState)
            Debug.Log("Vibration Toggle: OFF -> ON");
        else
            Debug.Log("Vibration Toggle: ON -> OFF");

        IsVibrationEnabled = newState;
        PlayerPrefs.SetInt(VIBRATION_ENABLED_KEY, newState ? 1 : 0);
        PlayerPrefs.Save();

        ApplyVibrationState();

        Debug.Log($"Vibration {(newState ? "Enabled" : "Disabled")}");
    }
    
    /// <summary>
    /// Alternative version that accepts a bool parameter (for backward compatibility)
    /// </summary>
    public void SetVibrationEnabled(bool isOn)
    {
        if (isSyncing)
            return;

        if (isOn)
            Debug.Log("Vibration Toggle: OFF -> ON");
        else
            Debug.Log("Vibration Toggle: ON -> OFF");

        IsVibrationEnabled = isOn;
        PlayerPrefs.SetInt(VIBRATION_ENABLED_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplyVibrationState();

        Debug.Log($"Vibration {(isOn ? "Enabled" : "Disabled")}");
    }

    private void ApplyAudioState()
    {
        ApplySavedAudioState();
    }

    /// <summary>
    /// Public method to toggle audio programmatically
    /// </summary>
    public void ToggleAudio()
    {
        SetAudioEnabled(!IsAudioEnabled);
    }

    /// <summary>
    /// Public method to toggle vibration programmatically
    /// </summary>
    public void ToggleVibration()
    {
        SetVibrationEnabled(!IsVibrationEnabled);
    }

    /// <summary>
    /// Reset settings to default (both enabled)
    /// </summary>
    public void ResetToDefaults()
    {
        SetAudioEnabled(true);
        SetVibrationEnabled(true);
        Debug.Log("Settings reset to defaults");
    }

    public void Close()
    {
        if (IsVibrationEnabled && VibrationManager.Instance != null)
            VibrationManager.Instance.VibrateButtonClick();

        if (IsAudioEnabled && audioManager != null)
            audioManager.btnSFX();

        if (SettingsPanel != null)
            SettingsPanel.SetActive(false);
    }
}
