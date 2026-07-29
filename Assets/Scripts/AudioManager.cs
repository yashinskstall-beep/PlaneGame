using UnityEngine;

/// <summary>
/// Owns BGM (AudioSource on this object) and SFX (child AudioSources: buttonfx, etc.).
/// Persists across Continue so music keeps playing; call PlayBtnSfx / etc. for one-shots.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource btnsfx;
    public AudioSource Planepartfx;
    public AudioSource audioSource;
    public AudioSource markerSFX;
    public AudioSource coinSfx;
    public AudioSource boostSFX;
    [Tooltip("Rubber band stretch loop (child RubberSfxx).")]
    public AudioSource rubberSfx;
    [Tooltip("Optional wind / plane loop (child PlaneSFX).")]
    public AudioSource planeSfx;

    private bool hasInitializedSfxStops;

    public static AudioManager Get(ref AudioManager cached)
    {
        AudioManager live = Get();
        if (live != null)
            cached = live;
        return cached;
    }

    public static AudioManager Get()
    {
        if (Instance != null)
            return Instance;

        AudioManager[] found = FindObjectsOfType<AudioManager>();
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                Instance = found[i];
                return Instance;
            }
        }

        return null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            SilenceAndDestroyDuplicate();
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        ResolveChildSfxSources();

        SettingsManager.LoadSavedSettings();
        SettingsManager.ApplySavedAudioState();
        SettingsManager.ApplySavedVibrationState();
    }

    void Start()
    {
        if (Instance != this)
            return;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ResolveChildSfxSources();
        EnsureBackgroundMusicPlaying();

        if (!hasInitializedSfxStops)
        {
            hasInitializedSfxStops = true;
            if (btnsfx != null) btnsfx.Stop();
            if (Planepartfx != null) Planepartfx.Stop();
            if (coinSfx != null) coinSfx.Stop();
            if (boostSFX != null) boostSFX.Stop();
            if (markerSFX != null) markerSFX.Stop();
            if (rubberSfx != null) rubberSfx.Stop();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ResolveChildSfxSources()
    {
        if (rubberSfx == null)
            rubberSfx = FindChildAudioSource("RubberSfxx", "RubberSfx", "RubberSFX", "rubber");
        if (planeSfx == null)
            planeSfx = FindChildAudioSource("PlaneSFX", "PlaneSfx", "wind");
        if (btnsfx == null)
            btnsfx = FindChildAudioSource("buttonfx", "ButtonFX", "btnsfx");
        if (Planepartfx == null)
            Planepartfx = FindChildAudioSource("Planepartssfx", "Planepartfx");
        if (markerSFX == null)
            markerSFX = FindChildAudioSource("Markersfx", "MarkerSFX");
        if (coinSfx == null)
            coinSfx = FindChildAudioSource("CoinSFX", "CoinSfx");
        if (boostSFX == null)
            boostSFX = FindChildAudioSource("BoostSFX", "BoostSfx");
    }

    private AudioSource FindChildAudioSource(params string[] nameHints)
    {
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source == audioSource)
                continue;

            string n = source.gameObject.name;
            for (int h = 0; h < nameHints.Length; h++)
            {
                if (n.IndexOf(nameHints[h], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return source;
            }
        }

        return null;
    }

    /// <summary>Living rubber-band stretch AudioSource (under AudioManager).</summary>
    public static AudioSource GetRubberSource()
    {
        AudioManager live = Get();
        if (live == null)
            return null;

        live.ResolveChildSfxSources();
        return live.rubberSfx;
    }

    /// <summary>Living wind / plane AudioSource if present.</summary>
    public static AudioSource GetPlaneWindSource()
    {
        AudioManager live = Get();
        if (live == null)
            return null;

        live.ResolveChildSfxSources();
        return live.planeSfx;
    }

    private void SilenceAndDestroyDuplicate()
    {
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
                continue;

            source.playOnAwake = false;
            source.Stop();
            source.enabled = false;
        }

        Destroy(gameObject);
    }

    public void EnsureBackgroundMusicPlaying()
    {
        AudioManager live = Get();
        if (live == null)
            return;

        if (live.audioSource == null)
            live.audioSource = live.GetComponent<AudioSource>();

        if (live.audioSource == null)
            return;

        if (!SettingsManager.IsAudioEnabled)
        {
            live.audioSource.mute = true;
            return;
        }

        live.audioSource.mute = false;
        if (!live.audioSource.isPlaying)
            live.audioSource.Play();
    }

    private static bool CanPlaySfx()
    {
        return SettingsManager.IsAudioEnabled;
    }

    public static void PlayBtnSfx()
    {
        AudioManager live = Get();
        if (CanPlaySfx() && live != null && live.btnsfx != null)
            live.btnsfx.Play();
    }

    public static void PlayPlanePartSfx()
    {
        AudioManager live = Get();
        if (CanPlaySfx() && live != null && live.Planepartfx != null)
            live.Planepartfx.Play();
    }

    public static void PlayMarkerSfx()
    {
        AudioManager live = Get();
        if (CanPlaySfx() && live != null && live.markerSFX != null)
            live.markerSFX.Play();
    }

    public static void PlayCoinSfx()
    {
        AudioManager live = Get();
        if (CanPlaySfx() && live != null && live.coinSfx != null)
            live.coinSfx.Play();
    }

    public static void PlayBoostSfx()
    {
        AudioManager live = Get();
        if (CanPlaySfx() && live != null && live.boostSFX != null)
            live.boostSFX.Play();
    }

    // Instance wrappers so existing inspector-wired calls still reach the living manager.
    public void btnSFX() => PlayBtnSfx();
    public void PlanepartSFX() => PlayPlanePartSfx();
    public void MarkerSFX() => PlayMarkerSfx();
    public void CoinSFX() => PlayCoinSfx();
    public void BoostSFX() => PlayBoostSfx();
}
