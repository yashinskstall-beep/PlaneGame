using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen circle iris fade. Persists across scene loads.
/// Close (hole shrinks) -> load scene -> open (hole grows).
/// </summary>
public class UICircleFadeTransition : MonoBehaviour
{
    public static UICircleFadeTransition Instance { get; private set; }

    [SerializeField] private float closeDuration = 0.45f;
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private float maxRadius = 1.25f;
    [SerializeField] private float edgeSoftness = 0.06f;
    [SerializeField] private Color fadeColor = new Color(0.12f, 0.45f, 0.95f, 1f);

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Material fadeMaterial;
    private GameObject overlayRoot;
    private bool pendingOpenOnLoad;
    private Coroutine activeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static UICircleFadeTransition EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
            return Instance;
        }

        var go = new GameObject("UICircleFadeTransition");
        go.SetActive(true);
        return go.AddComponent<UICircleFadeTransition>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildOverlay();
        SetRadius(maxRadius);
        SetOverlayVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;

        if (fadeMaterial != null)
            Destroy(fadeMaterial);
    }

    public void PlayLoadScene(string sceneName, Action beforeLoad = null)
    {
        EnsureInstance();
        gameObject.SetActive(true);

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        pendingOpenOnLoad = true;
        activeRoutine = StartCoroutine(LoadSceneRoutine(sceneName, beforeLoad));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action beforeLoad)
    {
        SetOverlayVisible(true);
        yield return AnimateRadius(maxRadius, 0f, closeDuration);

        beforeLoad?.Invoke();
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pendingOpenOnLoad)
            return;

        pendingOpenOnLoad = false;
        gameObject.SetActive(true);

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(OpenAfterLoadRoutine());
    }

    private IEnumerator OpenAfterLoadRoutine()
    {
        SetRadius(0f);
        SetOverlayVisible(true);
        yield return AnimateRadius(0f, maxRadius, openDuration);
        SetOverlayVisible(false);
        activeRoutine = null;
    }

    private IEnumerator AnimateRadius(float from, float to, float duration)
    {
        if (fadeMaterial == null)
            yield break;

        if (duration <= 0f)
        {
            SetRadius(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            SetRadius(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetRadius(to);
    }

    private void SetRadius(float radius)
    {
        if (fadeMaterial == null)
            return;

        fadeMaterial.SetFloat(RadiusId, radius);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(visible);
    }

    private void BuildOverlay()
    {
        overlayRoot = new GameObject("Overlay");
        overlayRoot.transform.SetParent(transform, false);
        overlayRoot.layer = gameObject.layer;

        var canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        canvas.pixelPerfect = false;

        var scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        overlayRoot.AddComponent<GraphicRaycaster>();

        Shader shader = Shader.Find("UI/CircleFade");
        if (shader == null)
        {
            Debug.LogError("UICircleFadeTransition: Shader 'UI/CircleFade' not found.");
            return;
        }

        fadeMaterial = new Material(shader);
        fadeMaterial.SetFloat(SoftnessId, edgeSoftness);
        fadeMaterial.SetColor(ColorId, fadeColor);

        var imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(overlayRoot.transform, false);

        var rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = imageGo.AddComponent<Image>();
        image.material = fadeMaterial;
        image.color = Color.white;
        image.raycastTarget = true;
    }
}
