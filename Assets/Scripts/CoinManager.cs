using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [SerializeField] private int coins;
    public int cheatCoins;

    private void OnValidate()
    {
        if (Application.isPlaying && coins != cheatCoins)
        {
            coins = cheatCoins;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCoins();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        cheatCoins = coins;
        SaveCoins();
    }

    public int GetCoins()
    {
        return coins;
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
