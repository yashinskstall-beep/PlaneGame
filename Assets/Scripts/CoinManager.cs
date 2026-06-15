using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [SerializeField] private int coins;
    public int cheatCoins;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        CoinManager existing = FindObjectOfType<CoinManager>(true);
        if (existing != null)
        {
            existing.InitializeSingleton();
            return;
        }

        GameObject go = new GameObject("CoinManager");
        go.AddComponent<CoinManager>();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && coins != cheatCoins)
        {
            coins = cheatCoins;
        }
    }

    private void Awake()
    {
        InitializeSingleton();
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCoins();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        coins += amount;
        cheatCoins = coins;
        SaveCoins();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (coins < amount)
            return false;

        coins -= amount;
        cheatCoins = coins;
        SaveCoins();
        return true;
    }

    public int GetCoins()
    {
        return coins;
    }

    public float GetCoinMultiplier()
    {
        int level = PlayerPrefs.GetInt("CoinMultiplierLevel", 1);
        return 1f + (level - 1) * 0.1f;
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt("PlayerCoins", coins);
        PlayerPrefs.Save();
    }

    private void LoadCoins()
    {
        coins = PlayerPrefs.GetInt("PlayerCoins", 0);
        cheatCoins = coins;
    }
}
