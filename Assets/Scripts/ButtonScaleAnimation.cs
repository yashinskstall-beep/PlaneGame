using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ButtonScaleAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("How much bigger the button pops on press (1.2 = 20% larger).")]
    public float popScaleMultiplier = 1.2f;

    [Tooltip("The duration of the scale animation in seconds.")]
    public float animationDuration = 0.3f;

    private Vector3 originalScale;
    private Coroutine animationCoroutine;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.IsInteractable())
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateScale(originalScale * popScaleMultiplier));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && !button.IsInteractable())
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateScale(originalScale));
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        float elapsed = 0f;
        Vector3 startingScale = transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            transform.localScale = Vector3.Lerp(startingScale, targetScale, progress);
            yield return null;
        }

        transform.localScale = targetScale;
        animationCoroutine = null;
    }
}

[RequireComponent(typeof(RectTransform))]
public class ButtonShakeAnimation : MonoBehaviour
{
    [Tooltip("How long the shake lasts in seconds.")]
    public float shakeDuration = 0.35f;

    [Tooltip("Maximum horizontal offset in pixels.")]
    public float shakeStrength = 12f;

    private RectTransform rect;
    private Coroutine shakeCoroutine;
    private Vector2 restPosition;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        restPosition = rect.anchoredPosition;
    }

    public void Play()
    {
        if (rect == null)
            return;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        restPosition = rect.anchoredPosition;
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float dampening = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float offsetX = Mathf.Sin(elapsed * 50f) * shakeStrength * dampening;
            rect.anchoredPosition = restPosition + new Vector2(offsetX, 0f);
            yield return null;
        }

        rect.anchoredPosition = restPosition;
        shakeCoroutine = null;
    }
}
