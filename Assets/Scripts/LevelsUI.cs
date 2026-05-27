using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Level button visuals, unlock state, and level selection.
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
        public string displayName = "Toy Plane";
        public string speedLabel = "20 \nKM/H";
        [Tooltip("Leave empty to stay in the current scene. Set a name (e.g. Desert) to load another scene.")]
        public string sceneName;
        public bool unlockedByDefault;

        [Header("Runtime test")]
        public bool locked;
        public bool unlocked;
    }

    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private LevelButtonData[] levels;
    [SerializeField] private Sprite unlockedButtonSprite;
    [SerializeField] private Sprite lockedButtonSprite;
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

        string scene = levels[index].sceneName;
        if (!string.IsNullOrEmpty(scene) && scene != SceneManager.GetActiveScene().name)
        {
            LevelProgress.ResetGameplayProgress();
            SceneManager.LoadScene(scene);
            return;
        }

        if (mainMenu != null)
            mainMenu.CloseLevelsPanel();

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
            uiManager.ResetGoalReached();
    }

    private void Update()
    {
        if (!refreshInUpdate || levels == null || !IsPanelVisible())
            return;

        for (int i = 0; i < levels.Length; i++)
            ApplyVisuals(levels[i], GetVisualState(i));
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

        if (data.unlocked)
            return true;
        if (data.locked)
            return false;

        if (index == 0 || data.unlockedByDefault)
            return true;

        return PlayerPrefs.GetInt(UnlockedKeyPrefix + index, 0) == 1;
    }

    private void ApplyVisuals(LevelButtonData data, bool unlocked)
    {
        if (data.button == null)
            return;

        var background = data.button.GetComponent<Image>();
        if (background != null)
            background.sprite = unlocked ? unlockedButtonSprite : lockedButtonSprite;

        if (data.nameText != null)
        {
            data.nameText.gameObject.SetActive(unlocked);
            if (unlocked)
                data.nameText.text = data.displayName;
        }

        if (data.speedText != null)
        {
            data.speedText.gameObject.SetActive(true);
            data.speedText.text = data.speedLabel;
        }
    }
}
