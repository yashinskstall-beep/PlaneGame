using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Level button visuals, unlock state, and level selection (2 playable levels).
/// Coming-soon slots are static UI in the scene, not listed here.
/// Opening/closing the Levels Panel is handled by MainMenu only.
/// </summary>
public class LevelsUI : MonoBehaviour
{
    private static LevelsUI instance;
    private static bool startupSceneResolved;
    private const string UnlockedKeyPrefix = "LevelUnlocked_";

    [System.Serializable]
    public class LevelButtonData
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI speedText;
        [Tooltip("Optional. Shown while locked; leave empty to find child named Lock.")]
        public GameObject lockIcon;
        public string displayName = "Toy Plane";
        public string speedLabel = "20 \nKM/H";
        [Tooltip("Leave empty to stay in the current scene. Set a name (e.g. Desert) to load another scene.")]
        public string sceneName;
        [Tooltip("Part object names in the target scene, used to clear unlock save when switching levels.")]
        public string[] upgradePartNamesForReset;
        public bool unlockedByDefault;
    }

    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private LevelButtonData[] levels;
    [SerializeField] private Sprite unlockedButtonSprite;
    [SerializeField] private bool refreshInUpdate = true;

    private void Awake()
    {
        instance = this;
        SetupButtonListeners();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapHighestUnlockedLevel()
    {
        TryLoadHighestUnlockedLevelOnStartup();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        if (mainMenu == null)
            mainMenu = FindObjectOfType<MainMenu>();

        TryLoadHighestUnlockedLevelOnStartup();
        RefreshAllButtons();
    }

    private static void TryLoadHighestUnlockedLevelOnStartup()
    {
        if (startupSceneResolved)
            return;

        LevelsUI levelsUI = instance ?? FindObjectOfType<LevelsUI>(true);
        if (levelsUI == null)
            return;

        string targetScene = levelsUI.GetHighestUnlockedSceneName();
        if (string.IsNullOrEmpty(targetScene))
        {
            startupSceneResolved = true;
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == targetScene)
        {
            startupSceneResolved = true;
            return;
        }

        startupSceneResolved = true;
        SceneManager.LoadScene(targetScene);
    }

    private void SetupButtonListeners()
    {
        if (levels == null)
            return;

        for (int i = 0; i < levels.Length; i++)
        {
            int index = i;
            if (levels[i].button == null)
                continue;

            levels[i].button.onClick.RemoveAllListeners();
            levels[i].button.onClick.AddListener(() => OnLevelClicked(index));
        }
    }

    private void OnLevelClicked(int index)
    {
        if (!IsLevelSelectable(index))
            return;

        if (IsActiveLevel(index))
        {
            LoadSceneWithTransition(SceneManager.GetActiveScene().name, resetProgress: false, null);
            return;
        }

        string scene = ResolveSceneNameForLevel(index);
        if (!string.IsNullOrEmpty(scene))
        {
            LoadSceneWithTransition(scene, resetProgress: true, levels[index].upgradePartNamesForReset);
            return;
        }

        if (mainMenu != null)
            mainMenu.CloseLevelsPanel();
    }

    private void LoadSceneWithTransition(string sceneName, bool resetProgress, string[] upgradePartNamesForReset)
    {
        UICircleFadeTransition.EnsureInstance().PlayLoadScene(sceneName, () =>
        {
            if (mainMenu != null)
                mainMenu.CloseLevelsPanel();

            if (resetProgress)
                LevelProgress.ResetGameplayProgressForScene(sceneName, upgradePartNamesForReset);
        });
    }

    private bool IsActiveLevel(int index)
    {
        return index == GetActiveLevelIndex();
    }

    private int GetActiveLevelIndex()
    {
        if (levels == null || levels.Length == 0)
            return -1;

        string activeScene = SceneManager.GetActiveScene().name;

        for (int i = 0; i < levels.Length; i++)
        {
            string scene = ResolveSceneNameForLevel(i);
            if (!string.IsNullOrEmpty(scene) && scene == activeScene)
                return i;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (string.IsNullOrEmpty(levels[i].sceneName) && IsLevelSelectable(i))
                return i;
        }

        return 0;
    }

    private void Update()
    {
        if (!refreshInUpdate || levels == null || !IsPanelVisible())
            return;

        RefreshAllButtons();
    }

    private bool IsPanelVisible()
    {
        return transform.parent != null && transform.parent.gameObject.activeInHierarchy;
    }

    public static bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex <= 0)
            return true;

        return PlayerPrefs.GetInt(UnlockedKeyPrefix + levelIndex, 0) == 1;
    }

    public static void UnlockLevel(int levelIndex)
    {
        if (levelIndex <= 0)
            return;

        PlayerPrefs.SetInt(UnlockedKeyPrefix + levelIndex, 1);
        PlayerPrefs.Save();

        if (instance != null)
            instance.RefreshAllButtons();
    }

    public void RefreshAllButtons()
    {
        if (levels == null)
            return;

        for (int i = 0; i < levels.Length; i++)
            ApplyVisuals(levels[i], i);
    }

    private bool IsLevelUnlockedVisually(int index)
    {
        if (index < 0 || index >= levels.Length)
            return false;

        LevelButtonData data = levels[index];

        if (index == 0 || data.unlockedByDefault)
            return true;

        return PlayerPrefs.GetInt(UnlockedKeyPrefix + index, 0) == 1;
    }

    private bool IsLevelSelectable(int index)
    {
        if (!IsLevelUnlockedVisually(index))
            return false;

        return index == GetLatestUnlockedLevelIndex();
    }

    private int GetLatestUnlockedLevelIndex()
    {
        if (levels == null || levels.Length == 0)
            return -1;

        for (int i = levels.Length - 1; i >= 0; i--)
        {
            if (IsLevelUnlockedVisually(i))
                return i;
        }

        return 0;
    }

    private string GetHighestUnlockedSceneName()
    {
        if (levels == null || levels.Length == 0)
            return GetDefaultSceneNameForBuildIndex(0);

        for (int i = levels.Length - 1; i >= 0; i--)
        {
            if (!IsLevelUnlockedVisually(i))
                continue;

            string scene = ResolveSceneNameForLevel(i);
            if (!string.IsNullOrEmpty(scene))
                return scene;
        }

        return ResolveSceneNameForLevel(0);
    }

    private string ResolveSceneNameForLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
            return string.Empty;

        if (!string.IsNullOrEmpty(levels[index].sceneName))
            return levels[index].sceneName;

        if (index == 0)
            return GetDefaultSceneNameForBuildIndex(0);

        return string.Empty;
    }

    private static string GetDefaultSceneNameForBuildIndex(int buildIndex)
    {
        if (SceneManager.sceneCountInBuildSettings <= buildIndex)
            return SceneManager.GetActiveScene().name;

        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        return string.IsNullOrEmpty(scenePath)
            ? SceneManager.GetActiveScene().name
            : Path.GetFileNameWithoutExtension(scenePath);
    }

    private TextMeshProUGUI ResolveNameText(LevelButtonData data)
    {
        if (data.nameText != null)
            return data.nameText;

        if (data.button == null)
            return null;

        Transform nameTransform = data.button.transform.Find("Name");
        if (nameTransform != null)
            return nameTransform.GetComponent<TextMeshProUGUI>();

        foreach (var tmp in data.button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp != data.speedText)
                return tmp;
        }

        return null;
    }

    private void ApplyVisuals(LevelButtonData data, int index)
    {
        if (data.button == null)
            return;

        bool isUnlocked = IsLevelUnlockedVisually(index);
        bool isSelectable = IsLevelSelectable(index);

        var colors = data.button.colors;
        colors.disabledColor = colors.normalColor;
        data.button.colors = colors;
        data.button.interactable = isSelectable;

        var background = data.button.GetComponent<Image>();
        if (isUnlocked && background != null && unlockedButtonSprite != null)
            background.sprite = unlockedButtonSprite;

        TextMeshProUGUI nameLabel = ResolveNameText(data);
        if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(isUnlocked);
            if (isUnlocked)
                nameLabel.text = data.displayName;
        }

        if (data.speedText != null)
        {
            data.speedText.gameObject.SetActive(true);
            data.speedText.text = data.speedLabel;
        }

        GameObject lockObject = data.lockIcon != null
            ? data.lockIcon
            : ResolveLockIcon(data);
        if (lockObject != null)
            lockObject.SetActive(!isUnlocked);
    }

    private GameObject ResolveLockIcon(LevelButtonData data)
    {
        if (data.button == null)
            return null;

        Transform lockTransform = data.button.transform.Find("Lock");
        return lockTransform != null ? lockTransform.gameObject : null;
    }
}
