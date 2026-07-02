using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [SerializeField] private int coins;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        CoinManager[] managers = FindObjectsOfType<CoinManager>(true);
        foreach (CoinManager manager in managers)
        {
            if (manager.transform.parent == null)
            {
                manager.InitializeSingleton();
                return;
            }
        }

        GameObject go = new GameObject("CoinManager");
        go.AddComponent<CoinManager>();
    }

    private void Awake()
    {
        InitializeSingleton();
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        if (transform.parent != null)
            return;

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCoins();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        coins += amount;
        SaveCoins();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (coins < amount)
            return false;

        coins -= amount;
        SaveCoins();
        return true;
    }

    public int GetCoins()
    {
        return coins;
    }

    public float GetCoinMultiplier()
    {
        return LevelProgress.GetCoinMultiplierValue();
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(LevelProgress.CoinsKey, coins);
        PlayerPrefs.Save();
    }

    private void LoadCoins()
    {
        coins = PlayerPrefs.GetInt(LevelProgress.CoinsKey, 0);
    }
}
