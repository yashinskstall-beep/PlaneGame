using UnityEngine;

/// <summary>
/// Visual marker for plane landing points
/// </summary>
public class LandingMarker : MonoBehaviour
{
    [Header("Appearance")]
    public Color markerColor = Color.red;
    public float pulseSpeed = 1.0f;
    public float pulseAmount = 0.2f;
    public bool rotateMarker = true;
    public float rotationSpeed = 30f;
    
    [Header("Lifetime")]
    public bool fadeOut = true;
    public float fadeDelay = 5f;
    public float fadeDuration = 2f;
    
    // Private variables
    private float creationTime;
    private Renderer[] renderers;
    private float baseScale;
    
    private void Start()
    {
        // Store creation time
        creationTime = Time.time;
        
        // Get all renderers
        renderers = GetComponentsInChildren<Renderer>();
        
        // Set the color
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                renderer.material.color = markerColor;
            }
        }
        
        // Store the base scale
        baseScale = transform.localScale.x;
    }
    
    private void Update()
    {
        // Apply pulsing effect
        if (pulseSpeed > 0)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = Vector3.one * baseScale * pulse;
        }
        
        // Apply rotation
        if (rotateMarker)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        // Handle fading
        if (fadeOut)
        {
            float timeSinceCreation = Time.time - creationTime;
            
            // Start fading after the delay
            if (timeSinceCreation > fadeDelay)
            {
                float fadeProgress = (timeSinceCreation - fadeDelay) / fadeDuration;
                
                // Apply fade to all renderers
                if (fadeProgress < 1f)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        Color color = renderer.material.color;
                        color.a = 1f - fadeProgress;
                        renderer.material.color = color;
                    }
                }
                else
                {
                    // Destroy the marker when fully faded
                    Destroy(gameObject);
                }
            }
        }
    }
}
