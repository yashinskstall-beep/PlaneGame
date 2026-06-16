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

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        if (mainMenu == null)
            mainMenu = FindObjectOfType<MainMenu>();

        RefreshAllButtons();
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
        if (!GetVisualState(index))
            return;

        if (IsActiveLevel(index))
        {
            LoadSceneWithTransition(SceneManager.GetActiveScene().name, resetProgress: false);
            return;
        }

        string scene = levels[index].sceneName;
        if (!string.IsNullOrEmpty(scene))
        {
            LoadSceneWithTransition(scene, resetProgress: true);
            return;
        }

        if (mainMenu != null)
            mainMenu.CloseLevelsPanel();
    }

    private void LoadSceneWithTransition(string sceneName, bool resetProgress)
    {
        UICircleFadeTransition.EnsureInstance().PlayLoadScene(sceneName, () =>
        {
            if (mainMenu != null)
                mainMenu.CloseLevelsPanel();

            if (resetProgress)
                LevelProgress.ResetGameplayProgress(mainMenu != null ? mainMenu.GetUpgradePartNames() : null);
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
            string scene = levels[i].sceneName;
            if (!string.IsNullOrEmpty(scene) && scene == activeScene)
                return i;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (string.IsNullOrEmpty(levels[i].sceneName) && GetVisualState(i))
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
            ApplyVisuals(levels[i], GetVisualState(i));
    }

    private bool GetVisualState(int index)
    {
        if (index < 0 || index >= levels.Length)
            return false;

        var data = levels[index];

        if (index == 0 || data.unlockedByDefault)
            return true;

        return PlayerPrefs.GetInt(UnlockedKeyPrefix + index, 0) == 1;
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

    private void ApplyVisuals(LevelButtonData data, bool unlocked)
    {
        if (data.button == null)
            return;

        var colors = data.button.colors;
        colors.disabledColor = colors.normalColor;
        data.button.colors = colors;
        data.button.interactable = unlocked;

        var background = data.button.GetComponent<Image>();
        if (unlocked && background != null && unlockedButtonSprite != null)
            background.sprite = unlockedButtonSprite;

        TextMeshProUGUI nameLabel = ResolveNameText(data);
        if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(unlocked);
            if (unlocked)
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
            lockObject.SetActive(!unlocked);
    }

    private GameObject ResolveLockIcon(LevelButtonData data)
    {
        if (data.button == null)
            return null;

        Transform lockTransform = data.button.transform.Find("Lock");
        return lockTransform != null ? lockTransform.gameObject : null;
    }
}
