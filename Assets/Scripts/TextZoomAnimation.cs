using UnityEngine;
using System.Collections;

public class TextZoomAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The minimum scale for the zoom-out effect.")]
    [SerializeField] private float minScale = 0.95f;

    [Tooltip("The maximum scale for the zoom-in effect.")]
    [SerializeField] private float maxScale = 1.05f;

    [Tooltip("The speed of the zoom animation.")]
    [SerializeField] private float animationSpeed = 1.0f;

    private RectTransform textTransform;
    private Vector3 initialScale;
    private Coroutine animationCoroutine;

    void Awake()
    {
        // Get the RectTransform component attached to the TextMeshPro object.
        textTransform = GetComponent<RectTransform>();
        if (textTransform == null)
        {
            Debug.LogError("TextZoomAnimation script requires a RectTransform component.", this);
            return;
        }

        // Store the initial scale to base the animation on.
        initialScale = textTransform.localScale;
    }

    void OnEnable()
    {
        // Start the animation coroutine when the object is enabled.
        if (textTransform != null)
        {
            animationCoroutine = StartCoroutine(AnimateZoom());
        }
    }

    void OnDisable()
    {
        // Stop the animation coroutine when the object is disabled.
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator AnimateZoom()
    {
        while (true) // Loop forever.
        {
            // Calculate the scale factor using a sine wave for a smooth ping-pong effect.
            float scaleFactor = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * animationSpeed) + 1) / 2f);

            // Apply the new scale.
            textTransform.localScale = initialScale * scaleFactor;

            // Wait for the next frame.
            yield return null;
        }
    }
}
