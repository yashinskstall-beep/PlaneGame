using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only persistent GameObject for testing level overrides.
/// Drop this on a GameObject in your first scene — it will survive all scene loads.
/// Use the Inspector to jump between levels, lock/unlock them, and reset progress.
/// Completely stripped from release builds.
/// </summary>
public class DebugLevelSelector : MonoBehaviour
{
#if UNITY_EDITOR
    private static DebugLevelSelector instance;

    [Header("Level Override")]
    [Tooltip("Set the level index you want to jump to (0-based, matches LevelsUI.levels array).")]
    public int targetLevelIndex = 0;

    [Tooltip("Automatically unlock the target level before jumping to it.")]
    public bool autoUnlockOnJump = true;

    [Header("Info (Read Only)")]
    [SerializeField] private string currentSceneName = "";
    [SerializeField] private int currentLevelIndex = -1;

    private const string UnlockedKeyPrefix = "LevelUnlocked_";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        UpdateInfo();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateInfo();
    }

    /// <summary>
    /// Refreshes the read-only info fields shown in the Inspector.
    /// </summary>
    public void UpdateInfo()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        currentLevelIndex = FindCurrentLevelIndex();
    }

    /// <summary>
    /// Jumps to the level at targetLevelIndex.
    /// Called by the custom editor button.
    /// </summary>
    public void JumpToTargetLevel()
    {
        LevelsUI levelsUI = FindObjectOfType<LevelsUI>(true);
        if (levelsUI == null)
        {
            Debug.LogWarning("[DebugLevelSelector] No LevelsUI found in scene. Cannot determine level scenes.");
            return;
        }

        string sceneName = GetSceneNameForLevel(levelsUI, targetLevelIndex);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[DebugLevelSelector] Level {targetLevelIndex} has no scene name configured.");
            return;
        }

        if (autoUnlockOnJump && targetLevelIndex > 0)
        {
            PlayerPrefs.SetInt(UnlockedKeyPrefix + targetLevelIndex, 1);
            PlayerPrefs.Save();
        }

        Debug.Log($"[DebugLevelSelector] Jumping to level {targetLevelIndex} — scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Unlocks a specific level by index.
    /// </summary>
    public void UnlockLevel(int index)
    {
        if (index <= 0) return;
        PlayerPrefs.SetInt(UnlockedKeyPrefix + index, 1);
        PlayerPrefs.Save();
        Debug.Log($"[DebugLevelSelector] Unlocked level {index}");
    }

    /// <summary>
    /// Locks a specific level by index.
    /// </summary>
    public void LockLevel(int index)
    {
        if (index <= 0) return;
        PlayerPrefs.DeleteKey(UnlockedKeyPrefix + index);
        PlayerPrefs.Save();
        Debug.Log($"[DebugLevelSelector] Locked level {index}");
    }

    /// <summary>
    /// Returns whether a level is unlocked.
    /// </summary>
    public bool IsLevelUnlocked(int index)
    {
        if (index <= 0) return true;
        return PlayerPrefs.GetInt(UnlockedKeyPrefix + index, 0) == 1;
    }

    /// <summary>
    /// Gets the total number of levels configured in LevelsUI.
    /// </summary>
    public int GetLevelCount()
    {
        LevelsUI levelsUI = FindObjectOfType<LevelsUI>(true);
        if (levelsUI == null) return 0;

        var field = typeof(LevelsUI).GetField("levels",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (field == null) return 0;

        var array = field.GetValue(levelsUI) as System.Array;
        return array != null ? array.Length : 0;
    }

    private int FindCurrentLevelIndex()
    {
        LevelsUI levelsUI = FindObjectOfType<LevelsUI>(true);
        if (levelsUI == null) return -1;

        string activeScene = SceneManager.GetActiveScene().name;

        var field = typeof(LevelsUI).GetField("levels",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (field == null) return -1;

        var levels = field.GetValue(levelsUI) as System.Array;
        if (levels == null) return -1;

        for (int i = 0; i < levels.Length; i++)
        {
            object levelData = levels.GetValue(i);
            var sceneProp = levelData.GetType().GetField("sceneName",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (sceneProp == null) continue;

            string scene = sceneProp.GetValue(levelData) as string;
            if (!string.IsNullOrEmpty(scene) && scene == activeScene)
                return i;
        }

        return 0;
    }

    private string GetSceneNameForLevel(LevelsUI levelsUI, int index)
    {
        var field = typeof(LevelsUI).GetField("levels",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (field == null) return string.Empty;

        var levels = field.GetValue(levelsUI) as System.Array;
        if (levels == null || index < 0 || index >= levels.Length)
            return string.Empty;

        object levelData = levels.GetValue(index);
        var sceneProp = levelData.GetType().GetField("sceneName",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (sceneProp == null) return string.Empty;

        string scene = sceneProp.GetValue(levelData) as string;

        // Level 0 with no scene name means current/default scene
        if (string.IsNullOrEmpty(scene) && index == 0)
            return SceneManager.GetActiveScene().name;

        return scene ?? string.Empty;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
#endif
}
