using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Animates a text pop-up with scale and fade effects
/// </summary>
public class TextPopUpAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Fade in duration in seconds")]
    public float fadeInDuration = 0.3f;
    
    [Tooltip("Time to hold at full opacity")]
    public float holdDuration = 1f;
    
    [Tooltip("Fade out duration in seconds")]
    public float fadeOutDuration = 0.5f;
    
    [Header("Scale Settings")]
    [Tooltip("Starting scale multiplier")]
    public float startScale = 0.5f;
    
    [Tooltip("Maximum scale during pop")]
    public float popScale = 1.2f;
    
    [Tooltip("Final scale after settling")]
    public float finalScale = 1f;
    
    [Tooltip("Duration of the pop effect")]
    public float popDuration = 0.2f;
    
    [Header("Movement Settings")]
    [Tooltip("Enable upward movement during animation")]
    public bool enableMovement = false;
    
    [Tooltip("Distance to move upward")]
    public float moveDistance = 50f;
    
    [Header("Options")]
    [Tooltip("Auto-disable GameObject after animation completes")]
    public bool autoDisable = true;
    
    [Tooltip("Play animation automatically on enable")]
    public bool playOnEnable = true;
    
    [Header("References")]
    [Tooltip("Text component to animate (auto-detected if not assigned)")]
    public TextMeshProUGUI textComponent;
    
    [Tooltip("CanvasGroup for fade effects (auto-detected if not assigned)")]
    public CanvasGroup canvasGroup;
    
    private Vector3 initialScale;
    private Vector3 initialPosition;
    private Coroutine animationCoroutine;
    private bool isInitialized = false;
    
    void Awake()
    {
        Initialize();
    }
    
    void OnEnable()
    {
        // Ensure initialization
        if (!isInitialized)
            Initialize();
            
        // Start animation when enabled if playOnEnable is true
        if (playOnEnable)
            PlayAnimation();
    }
    
    private void Initialize()
    {
        // Auto-detect components if not assigned
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // Add CanvasGroup if it doesn't exist
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Store initial values
        initialScale = transform.localScale;
        initialPosition = transform.localPosition;
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Plays the pop-up animation
    /// </summary>
    public void PlayAnimation()
    {
        // Stop any existing animation
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        // Reset to initial state
        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        canvasGroup.alpha = 0f;
        
        // Start the animation
        animationCoroutine = StartCoroutine(AnimatePopUp());
    }
    
    /// <summary>
    /// Main animation coroutine
    /// </summary>
    private IEnumerator AnimatePopUp()
    {
        float elapsed = 0f;
        float totalDuration = fadeInDuration + popDuration + holdDuration + fadeOutDuration;
        
        // Phase 1: Fade in and scale from start to pop
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            
            // Smooth fade in
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            
            // Scale from startScale to popScale with bounce
            float scaleValue = Mathf.Lerp(startScale, popScale, EaseOutBack(t));
            transform.localScale = initialScale * scaleValue;
            
            // Move up if enabled
            if (enableMovement)
            {
                float moveT = t * 0.3f; // Only move 30% during fade in
                transform.localPosition = initialPosition + Vector3.up * (moveDistance * moveT);
            }
            
            yield return null;
        }
        
        // Phase 2: Settle from pop to final scale
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            
            // Scale from popScale to finalScale
            float scaleValue = Mathf.Lerp(popScale, finalScale, EaseOutElastic(t));
            transform.localScale = initialScale * scaleValue;
            
            // Continue movement
            if (enableMovement)
            {
                float moveT = 0.3f + (t * 0.3f); // Move from 30% to 60%
                transform.localPosition = initialPosition + Vector3.up * (moveDistance * moveT);
            }
            
            yield return null;
        }
        
        // Ensure final scale
        transform.localScale = initialScale * finalScale;
        canvasGroup.alpha = 1f;
        
        // Phase 3: Hold at peak
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            
            // Continue movement during hold
            if (enableMovement)
            {
                float t = Mathf.Clamp01(elapsed / holdDuration);
                float moveT = 0.6f + (t * 0.2f); // Move from 60% to 80%
                transform.localPosition = initialPosition + Vector3.up * (moveDistance * moveT);
            }
            
            yield return null;
        }
        
        // Phase 4: Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            
            // Smooth fade out
            canvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t);
            
            // Final movement
            if (enableMovement)
            {
                float moveT = 0.8f + (t * 0.2f); // Move from 80% to 100%
                transform.localPosition = initialPosition + Vector3.up * (moveDistance * moveT);
            }
            
            yield return null;
        }
        
        // Ensure fully faded out
        canvasGroup.alpha = 0f;
        
        // Auto-disable if enabled
        if (autoDisable)
        {
            gameObject.SetActive(false);
        }
        
        animationCoroutine = null;
    }
    
    // Easing functions for smooth animations
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    private float EaseOutElastic(float t)
    {
        if (t == 0f || t == 1f) return t;
        
        float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
    
    /// <summary>
    /// Stops the current animation
    /// </summary>
    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Resets the text to its initial state
    /// </summary>
    public void ResetToInitial()
    {
        StopAnimation();
        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Sets the text content
    /// </summary>
    public void SetText(string text)
    {
        if (textComponent != null)
            textComponent.text = text;
    }
    
    /// <summary>
    /// Plays animation with custom text
    /// </summary>
    public void PlayWithText(string text)
    {
        SetText(text);
        PlayAnimation();
    }
}
