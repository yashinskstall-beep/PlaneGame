using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shows rewarded ads and reports success/failure.
/// Replace SimulateRewardedAd with your AdMob / LevelPlay / Unity Ads SDK calls.
/// </summary>
public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager Instance { get; private set; }

    [Tooltip("When true, editor and builds without an SDK still complete ads instantly for testing.")]
    [SerializeField] private bool useSimulatedAds = true;

    [SerializeField] private float simulatedAdDuration = 1.25f;

    private bool isShowingAd;

    public bool IsShowingAd => isShowingAd;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsRewardedAdReady()
    {
        if (isShowingAd)
            return false;

        // TODO: return your SDK's rewarded ad ready state here.
        return useSimulatedAds || Application.isMobilePlatform;
    }

    public void ShowRewardedAd(Action<bool> onComplete)
    {
        if (isShowingAd)
        {
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(ShowRewardedAdRoutine(onComplete));
    }

    private IEnumerator ShowRewardedAdRoutine(Action<bool> onComplete)
    {
        isShowingAd = true;

        // TODO: Replace this block with your real rewarded ad show call.
        if (useSimulatedAds)
        {
            Debug.Log("[RewardedAd] Simulated ad playing...");
            yield return new WaitForSecondsRealtime(simulatedAdDuration);
            Debug.Log("[RewardedAd] Simulated ad completed.");
            isShowingAd = false;
            onComplete?.Invoke(true);
            yield break;
        }

        Debug.LogWarning("[RewardedAd] No ad SDK wired yet. Set useSimulatedAds or integrate your ad provider.");
        isShowingAd = false;
        onComplete?.Invoke(false);
    }
}
