using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ButtonScaleAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("The scale to punch to when the button is pressed.")]
    public Vector3 punchScale = new Vector3(0.5f, 0.5f, 0.5f);

    [Tooltip("The duration of the scale animation in seconds.")]
    public float animationDuration = 0.3f;

    private Vector3 originalScale;
    private Coroutine animationCoroutine;
    private Button button;

    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only animate if button is interactable
        if (button != null && !button.interactable)
            return;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateScale(punchScale));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Only animate if button is interactable
        if (button != null && !button.interactable)
            return;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
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
    }
}
