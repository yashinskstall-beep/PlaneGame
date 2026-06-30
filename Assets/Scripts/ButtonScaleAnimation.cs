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

    [Tooltip("Play haptic pulses while the button shakes.")]
    public bool playHaptics = true;

    [Tooltip("Seconds between haptic pulses during a shake.")]
    public float hapticPulseInterval = 0.09f;

    private RectTransform rect;
    private Coroutine shakeCoroutine;
    private Vector2 restPosition;
    private bool restPositionCaptured;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        CaptureRestPosition();
    }

    void OnEnable()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (shakeCoroutine == null)
            CaptureRestPosition();
    }

    private void CaptureRestPosition()
    {
        if (rect == null)
            return;

        restPosition = rect.anchoredPosition;
        restPositionCaptured = true;
    }

    public void Play()
    {
        if (rect == null)
            return;

        if (!restPositionCaptured)
            CaptureRestPosition();

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            rect.anchoredPosition = restPosition;
        }

        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private void PlayShakeHaptic()
    {
        if (!playHaptics || VibrationManager.Instance == null)
            return;

        VibrationManager.Instance.VibrateDenied();
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        float nextHapticTime = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (playHaptics && elapsed >= nextHapticTime)
            {
                PlayShakeHaptic();
                nextHapticTime = elapsed + hapticPulseInterval;
            }

            float dampening = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float offsetX = Mathf.Sin(elapsed * 50f) * shakeStrength * dampening;
            rect.anchoredPosition = restPosition + new Vector2(offsetX, 0f);
            yield return null;
        }

        rect.anchoredPosition = restPosition;
        shakeCoroutine = null;
    }
}
