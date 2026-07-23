using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// AdMob rewarded + interstitial ads for Aviato.
/// Editor: Google test ad units (always fill).
/// Android/iOS: production Aviato ad units.
/// </summary>
public class RewardedAdManager : MonoBehaviour
{
    private const string LogPrefix = "[RewardedAdManager]";
    public static RewardedAdManager Instance { get; private set; }

    public event Action OnRewardedAdLoaded;
    public event Action OnRewardedAdLoadFailed;

    private const string EditorTestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string EditorTestRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Header("Production Interstitial Ad IDs")]
    [SerializeField] private string androidInterstitialId = "ca-app-pub-8376488234284532/2688874526";
    [SerializeField] private string iosInterstitialId = "ca-app-pub-8376488234284532/4470941045";

    [Header("Production Rewarded Ad IDs")]
    [SerializeField] private string androidRewardedId = "ca-app-pub-8376488234284532/2092189710";
    [SerializeField] private string iosRewardedId = "ca-app-pub-8376488234284532/7543791552";

    [Header("Timing")]
    [SerializeField] private float postAdDelaySeconds = 0.5f;
    [SerializeField] private float loadRetryDelaySeconds = 2f;
    [SerializeField] private float rewardedShowWaitTimeoutSeconds = 20f;
    [SerializeField] private float backgroundPreloadIntervalSeconds = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugAds = false;

    private string interstitialId;
    private string rewardedId;

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private bool isShowingAd;
    private bool isPresentationPending;
    private bool isInterstitialLoading;
    private bool isRewardedLoading;
    private bool isInitialized;
    private bool initStarted;
    private bool preloadRewardedPending;

    private Action<bool> pendingRewardedCallback;
    private Action<bool> pendingAutoShowCallback;
    private bool rewardEarned;

    private float timeScaleBeforeAd = 1f;
    private bool pausedForAd;
    private Coroutine postAdCoroutine;
    private Coroutine interstitialRetryCoroutine;
    private Coroutine rewardedRetryCoroutine;
    private Coroutine initCoroutine;
    private Coroutine waitForRewardedShowCoroutine;

    private float backgroundPreloadTimer;
    private bool keepRewardedAdWarm = true;

    public bool IsShowingAd => isShowingAd || isPresentationPending;
    public bool IsRewardedAdLoading => isRewardedLoading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        EnsureExists();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Log($"Awake. warm={keepRewardedAdWarm} platform={Application.platform}");
        BeginAdMobInitialization();
    }

    void Update()
    {
        if (!keepRewardedAdWarm || !isInitialized || isShowingAd || isPresentationPending || pendingAutoShowCallback != null)
            return;

        backgroundPreloadTimer -= Time.unscaledDeltaTime;
        if (backgroundPreloadTimer > 0f)
            return;

        backgroundPreloadTimer = backgroundPreloadIntervalSeconds;

        if (!IsRewardedAdReady() && !isRewardedLoading)
            RequestRewarded();
    }

    public void SetKeepRewardedAdWarm(bool enabled)
    {
        keepRewardedAdWarm = enabled;

        if (enabled)
            EnsureRewardedAdPreloaded();
    }

    private void BeginAdMobInitialization()
    {
        if (initStarted || !SupportsAdMob())
            return;

        initStarted = true;
        SelectAdUnitIds();

        if (initCoroutine != null)
            StopCoroutine(initCoroutine);

        initCoroutine = StartCoroutine(InitializeAdMobWhenReady());
    }

    private IEnumerator InitializeAdMobWhenReady()
    {
        yield return null;
#if UNITY_ANDROID && !UNITY_EDITOR
        yield return new WaitForSecondsRealtime(0.25f);
#endif

        ConfigureMobileAdsSettings();

        Log($"Initializing AdMob ({GetRuntimeModeLabel()})...");
        bool initCompleted = false;

        try
        {
            MobileAds.Initialize(_ =>
            {
                initCompleted = true;
                isInitialized = true;
                Log($"AdMob initialized ({GetRuntimeModeLabel()}).");
                SafeInvoke("LoadAllAds after init", LoadAllAds);

                if (preloadRewardedPending)
                {
                    preloadRewardedPending = false;
                    SafeInvoke("Deferred RequestRewarded after init", RequestRewarded);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} MobileAds.Initialize threw: {e.Message}");
        }

        float initWait = 0f;
        while (!initCompleted && initWait < 15f)
        {
            initWait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!initCompleted)
        {
            Debug.LogError($"{LogPrefix} AdMob initialization timed out. Retrying...");
            initStarted = false;
            BeginAdMobInitialization();
        }

        initCoroutine = null;
    }

    private void ConfigureMobileAdsSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
#endif

#if UNITY_EDITOR
        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            TestDeviceIds = new List<string> { "69d6891543cce296d6693e79cd17ec9c" }
        });
#endif
    }

    public bool IsRewardedAdReady()
    {
        if (isShowingAd || isPresentationPending)
            return false;

        if (!SupportsAdMob() || !isInitialized)
            return false;

        return rewardedAd != null && rewardedAd.CanShowAd();
    }

    public bool TryShowRewardedAdOrQueue(Action<bool> onComplete)
    {
        Log($"TryShowRewardedAdOrQueue called. initialized={isInitialized} ready={IsRewardedAdReady()} loading={isRewardedLoading} showing={isShowingAd} pending={isPresentationPending} hasRewarded={rewardedAd != null}");
        if (onComplete == null)
            return false;

        if (isShowingAd || isPresentationPending)
        {
            onComplete(false);
            return false;
        }

        if (!SupportsAdMob())
        {
            onComplete(false);
            return false;
        }

        if (!isInitialized)
        {
            preloadRewardedPending = true;
            pendingAutoShowCallback = onComplete;
            BeginAdMobInitialization();
            EnsureWaitForRewardedShowRoutine();
            return false;
        }

        if (IsRewardedAdReady())
        {
            ShowRewardedAd(onComplete);
            return true;
        }

        pendingAutoShowCallback = onComplete;
        RequestRewarded();
        EnsureWaitForRewardedShowRoutine();
        return false;
    }

    public void ShowRewardedAd(Action<bool> onComplete)
    {
        Log($"ShowRewardedAd called. initialized={isInitialized} ready={(rewardedAd != null && rewardedAd.CanShowAd())} loading={isRewardedLoading} showing={isShowingAd} pending={isPresentationPending}");
        if (isShowingAd || isPresentationPending)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (!SupportsAdMob())
        {
            onComplete?.Invoke(false);
            return;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning($"{LogPrefix} Rewarded ad not ready when ShowRewardedAd was called.");
            RequestRewarded();
            onComplete?.Invoke(false);
            return;
        }

        pendingAutoShowCallback = null;
        isPresentationPending = true;
        rewardEarned = false;
        pendingRewardedCallback = onComplete;

        try
        {
            Log($"Showing rewarded ad now. rewardedId={rewardedId}");
            rewardedAd.Show(reward =>
            {
                Log($"Reward earned: {reward.Type} x{reward.Amount}");
                rewardEarned = true;
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to show rewarded ad: {e.Message}");
            isPresentationPending = false;
            CompleteRewardedAd(false);
            RequestRewarded();
        }
    }

    public bool TryShowBoostRewardedAd(Action<bool> onComplete)
    {
        Log($"TryShowBoostRewardedAd. rewardedId={rewardedId} ready={IsRewardedAdReady()}");
        return TryShowRewardedAdOrQueue(onComplete);
    }

    public void ShowInterstitialAd()
    {
        if (!SupportsAdMob())
            return;

        if (IsRewardedFlowActive())
        {
            Debug.LogWarning($"{LogPrefix} Interstitial blocked because a rewarded ad is pending or showing.");
            return;
        }

        if (isShowingAd)
        {
            Debug.LogWarning($"{LogPrefix} Interstitial blocked because another ad is already showing.");
            return;
        }

        if (interstitialAd == null || !interstitialAd.CanShowAd())
        {
            Debug.LogWarning($"{LogPrefix} Interstitial ad not ready.");
            RequestInterstitial();
            return;
        }

        try
        {
            Log($"Showing interstitial ad now. interstitialId={interstitialId}");
            interstitialAd.Show();
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to show interstitial ad: {e.Message}");
            isShowingAd = false;
            RequestInterstitial();
        }
    }

    private bool IsRewardedFlowActive()
    {
        return isPresentationPending || pendingRewardedCallback != null || pendingAutoShowCallback != null;
    }

    public void PreloadRewardedAd()
    {
        EnsureRewardedAdPreloaded();
    }

    public void EnsureRewardedAdPreloaded()
    {
        if (!SupportsAdMob())
            return;

        if (!isInitialized)
        {
            preloadRewardedPending = true;
            BeginAdMobInitialization();
            return;
        }

        if (IsRewardedAdReady() || isRewardedLoading || isShowingAd || isPresentationPending)
            return;

        RequestRewarded();
    }

    public void PreloadInterstitialAd()
    {
        RequestInterstitial();
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject adManagerObject = new GameObject("RewardedAdManager");
        adManagerObject.AddComponent<RewardedAdManager>();
    }

    private void SelectAdUnitIds()
    {
#if UNITY_EDITOR
        interstitialId = EditorTestInterstitialId;
        rewardedId = EditorTestRewardedId;
#elif UNITY_ANDROID
        interstitialId = androidInterstitialId;
        rewardedId = androidRewardedId;
#elif UNITY_IOS
        interstitialId = iosInterstitialId;
        rewardedId = iosRewardedId;
#endif

        Log($"Interstitial ID: {interstitialId}");
        Log($"Rewarded ID: {rewardedId}");
    }

    private string GetRuntimeModeLabel()
    {
#if UNITY_EDITOR
        return "Editor test ads";
#elif UNITY_ANDROID
        return "Android production ads";
#elif UNITY_IOS
        return "iOS production ads";
#else
        return "unsupported platform";
#endif
    }

    private static bool SupportsAdMob()
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return false;
#endif
    }

    private void LoadAllAds()
    {
        if (!isInitialized || !SupportsAdMob())
            return;

        backgroundPreloadTimer = 0f;
        Log($"LoadAllAds requesting rewarded + interstitial.");
        RequestRewarded();
        RequestInterstitial();
    }

    private void RequestInterstitial()
    {
        if (!SupportsAdMob() || isInterstitialLoading || string.IsNullOrEmpty(interstitialId) || !isInitialized)
        {
            Log($"RequestInterstitial skipped. supports={SupportsAdMob()} loading={isInterstitialLoading} hasId={!string.IsNullOrEmpty(interstitialId)} initialized={isInitialized}");
            return;
        }

        CancelRetry(ref interstitialRetryCoroutine);
        isInterstitialLoading = true;
        interstitialAd?.Destroy();
        interstitialAd = null;

        InterstitialAd.Load(interstitialId, new AdRequest(), (ad, error) =>
        {
            SafeInvoke("Interstitial load callback", () =>
            {
                isInterstitialLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogError($"{LogPrefix} Interstitial load failed: {error?.GetMessage()}");
                    ScheduleRetry(ref interstitialRetryCoroutine, RequestInterstitial);
                    return;
                }

                interstitialAd = ad;
                RegisterInterstitialCallbacks(interstitialAd);
                Log($"Interstitial loaded.");
            });
        });
    }

    private void RequestRewarded()
    {
        if (!SupportsAdMob() || string.IsNullOrEmpty(rewardedId))
        {
            Log($"RequestRewarded skipped. supports={SupportsAdMob()} rewardedIdEmpty={string.IsNullOrEmpty(rewardedId)}");
            return;
        }

        if (!isInitialized)
        {
            preloadRewardedPending = true;
            Log($"RequestRewarded deferred until init completes.");
            return;
        }

        if (isRewardedLoading)
        {
            Log($"RequestRewarded skipped because a rewarded ad is already loading.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning($"{LogPrefix} No internet connection. Cannot load rewarded ad.");
            return;
        }

        CancelRetry(ref rewardedRetryCoroutine);
        isRewardedLoading = true;
        rewardedAd?.Destroy();
        rewardedAd = null;

        Log($"Requesting rewarded ad. reachability={Application.internetReachability}");
        RewardedAd.Load(rewardedId, new AdRequest(), (ad, error) =>
        {
            SafeInvoke("Rewarded load callback", () =>
            {
                isRewardedLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogError($"{LogPrefix} Rewarded load failed: {error?.GetMessage()}");
                    OnRewardedAdLoadFailed?.Invoke();
                    ScheduleRetry(ref rewardedRetryCoroutine, RequestRewarded);
                    return;
                }

                rewardedAd = ad;
                RegisterRewardedCallbacks(rewardedAd);
                Log($"Rewarded ad loaded.");
                OnRewardedAdLoaded?.Invoke();
                TryFulfillPendingAutoShow();
            });
        });
    }

    private void EnsureWaitForRewardedShowRoutine()
    {
        if (waitForRewardedShowCoroutine != null)
            return;

        waitForRewardedShowCoroutine = StartCoroutine(WaitForRewardedAndShowRoutine());
    }

    private IEnumerator WaitForRewardedAndShowRoutine()
    {
        float elapsed = 0f;
        Log($"WaitForRewardedAndShowRoutine started. timeout={rewardedShowWaitTimeoutSeconds}s");

        while (pendingAutoShowCallback != null && elapsed < rewardedShowWaitTimeoutSeconds)
        {
            if (IsRewardedAdReady())
            {
                Log($"WaitForRewardedAndShowRoutine detected ready ad after {elapsed:F2}s.");
                TryFulfillPendingAutoShow();
                waitForRewardedShowCoroutine = null;
                yield break;
            }

            if (!isRewardedLoading && !IsRewardedAdReady())
                RequestRewarded();

            elapsed += 0.25f;
            yield return new WaitForSecondsRealtime(0.25f);
        }

        if (pendingAutoShowCallback != null)
        {
            Debug.LogWarning($"{LogPrefix} Timed out waiting to show rewarded ad after {elapsed:F2}s.");
            TryCompletePendingAutoShow(false);
        }

        waitForRewardedShowCoroutine = null;
    }

    private void TryFulfillPendingAutoShow()
    {
        if (pendingAutoShowCallback == null || !IsRewardedAdReady())
            return;

        Log($"TryFulfillPendingAutoShow promoting queued request into ShowRewardedAd.");
        Action<bool> callback = pendingAutoShowCallback;
        pendingAutoShowCallback = null;

        if (waitForRewardedShowCoroutine != null)
        {
            StopCoroutine(waitForRewardedShowCoroutine);
            waitForRewardedShowCoroutine = null;
        }

        ShowRewardedAd(callback);
    }

    private void TryCompletePendingAutoShow(bool success)
    {
        if (pendingAutoShowCallback == null)
            return;

        Debug.LogWarning($"{LogPrefix} TryCompletePendingAutoShow success={success}");
        Action<bool> callback = pendingAutoShowCallback;
        pendingAutoShowCallback = null;

        if (waitForRewardedShowCoroutine != null)
        {
            StopCoroutine(waitForRewardedShowCoroutine);
            waitForRewardedShowCoroutine = null;
        }

        callback?.Invoke(success);
    }

    private void SafeInvoke(string context, Action action)
    {
        if (action == null)
            return;

        try
        {
            action();
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Exception in {context}: {e}");
        }
    }

    private void Log(string message)
    {
        if (debugAds)
            Debug.Log($"{LogPrefix} {message}");
    }

    private void ScheduleRetry(ref Coroutine retryCoroutine, Action requestAction)
    {
        CancelRetry(ref retryCoroutine);
        Log($"Scheduling retry in {loadRetryDelaySeconds:F1}s for {requestAction?.Method.Name}");
        retryCoroutine = StartCoroutine(RetryLoadRoutine(requestAction));
    }

    private void CancelRetry(ref Coroutine retryCoroutine)
    {
        if (retryCoroutine == null)
            return;

        StopCoroutine(retryCoroutine);
        retryCoroutine = null;
    }

    private IEnumerator RetryLoadRoutine(Action requestAction)
    {
        yield return new WaitForSecondsRealtime(loadRetryDelaySeconds);
        requestAction?.Invoke();
    }

    private void RegisterInterstitialCallbacks(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            SafeInvoke("Interstitial opened callback", () =>
            {
                Log($"Interstitial opened.");
                isShowingAd = true;
                PauseGameForAd();
            });
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Log($"Interstitial closed.");
            SafeInvoke("Interstitial closed callback", () => RunAfterAdClosed(() => RequestInterstitial()));
        };

        ad.OnAdFullScreenContentFailed += _ =>
        {
            Debug.LogError($"{LogPrefix} Interstitial failed to open full screen content.");
            SafeInvoke("Interstitial failed callback", () => RunAfterAdClosed(() => RequestInterstitial()));
        };
    }

    private void RegisterRewardedCallbacks(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            SafeInvoke("Rewarded opened callback", () =>
            {
                Log($"Rewarded ad opened.");
                isPresentationPending = false;
                isShowingAd = true;
                PauseGameForAd();
            });
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            bool earned = rewardEarned;
            Log($"Rewarded ad closed. earned={earned}");
            SafeInvoke("Rewarded closed callback", () => RunAfterAdClosed(() =>
            {
                DestroyRewardedAdInstance();
                CompleteRewardedAd(earned);
                RequestRewarded();
            }));
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            SafeInvoke("Rewarded failed callback", () =>
            {
                Debug.LogError($"{LogPrefix} Rewarded ad failed to open: {error}");
                isPresentationPending = false;
                DestroyRewardedAdInstance();
                RunAfterAdClosed(() =>
                {
                    CompleteRewardedAd(false);
                    RequestRewarded();
                });
            });
        };
    }

    private void DestroyRewardedAdInstance()
    {
        rewardedAd?.Destroy();
        rewardedAd = null;
    }

    private void RunAfterAdClosed(Action onComplete)
    {
        if (postAdCoroutine != null)
            StopCoroutine(postAdCoroutine);

        postAdCoroutine = StartCoroutine(PostAdDelayRoutine(onComplete));
    }

    private IEnumerator PostAdDelayRoutine(Action onComplete)
    {
        yield return new WaitForSecondsRealtime(postAdDelaySeconds);

        postAdCoroutine = null;
        isShowingAd = false;
        isPresentationPending = false;
        ResumeGameAfterAd();
        onComplete?.Invoke();
    }

    private void PauseGameForAd()
    {
        if (pausedForAd)
            return;

        pausedForAd = true;
        timeScaleBeforeAd = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            AudioSource[] audioSources = audioManager.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in audioSources)
            {
                if (source != null && source.isPlaying)
                    source.Pause();
            }
        }
    }

    private void ResumeGameAfterAd()
    {
        if (!pausedForAd)
            return;

        pausedForAd = false;
        Time.timeScale = timeScaleBeforeAd;

        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            AudioSource[] audioSources = audioManager.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in audioSources)
            {
                if (source != null)
                    source.UnPause();
            }
        }

        SettingsManager.ApplySavedAudioState();
    }

    private void CompleteRewardedAd(bool success)
    {
        Log($"CompleteRewardedAd success={success} rewardEarned={rewardEarned}");
        Action<bool> callback = pendingRewardedCallback;
        pendingRewardedCallback = null;
        rewardEarned = false;
        callback?.Invoke(success);
    }

    void OnDestroy()
    {
        if (postAdCoroutine != null)
        {
            StopCoroutine(postAdCoroutine);
            postAdCoroutine = null;
        }

        if (initCoroutine != null)
        {
            StopCoroutine(initCoroutine);
            initCoroutine = null;
        }

        if (waitForRewardedShowCoroutine != null)
        {
            StopCoroutine(waitForRewardedShowCoroutine);
            waitForRewardedShowCoroutine = null;
        }

        CancelRetry(ref interstitialRetryCoroutine);
        CancelRetry(ref rewardedRetryCoroutine);

        if (pausedForAd)
            ResumeGameAfterAd();

        interstitialAd?.Destroy();
        rewardedAd?.Destroy();

        if (Instance == this)
            Instance = null;
    }
}
