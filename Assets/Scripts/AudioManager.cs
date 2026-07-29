using UnityEngine;

/// <summary>
/// Owns BGM (AudioSource on this object) and SFX (child AudioSources: buttonfx, etc.).
/// Scene-local — one AudioManager per scene, destroyed with the scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
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

    /// <summary>Finds the AudioManager in the active scene (optional cache refresh).</summary>
    public static AudioManager Get(ref AudioManager cached)
    {
        AudioManager live = Get();
        if (live != null)
            cached = live;
        return cached != null ? cached : live;
    }

    /// <summary>Finds the AudioManager in the active scene.</summary>
    public static AudioManager Get()
    {
        return FindObjectOfType<AudioManager>();
    }

    void Awake()
    {
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
            if (planeSfx != null) planeSfx.Stop();
        }
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

    public static AudioSource GetRubberSource()
    {
        AudioManager live = Get();
        if (live == null)
            return null;

        live.ResolveChildSfxSources();
        return live.rubberSfx;
    }

    public static AudioSource GetPlaneWindSource()
    {
        AudioManager live = Get();
        if (live == null)
            return null;

        live.ResolveChildSfxSources();
        return live.planeSfx;
    }

    public static void StopFlightLoops()
    {
        AudioManager live = Get();
        if (live == null)
            return;

        live.ResolveChildSfxSources();

        if (live.planeSfx != null && live.planeSfx.isPlaying)
            live.planeSfx.Stop();

        if (live.rubberSfx != null && live.rubberSfx.isPlaying)
            live.rubberSfx.Stop();
    }

    public void EnsureBackgroundMusicPlaying()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            return;

        if (!SettingsManager.IsAudioEnabled)
        {
            audioSource.mute = true;
            return;
        }

        audioSource.mute = false;
        if (!audioSource.isPlaying)
            audioSource.Play();
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

    public void btnSFX() => PlayBtnSfx();
    public void PlanepartSFX() => PlayPlanePartSfx();
    public void MarkerSFX() => PlayMarkerSfx();
    public void CoinSFX() => PlayCoinSfx();
    public void BoostSFX() => PlayBoostSfx();
}
