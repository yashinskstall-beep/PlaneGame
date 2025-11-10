using UnityEngine;

/// <summary>
/// Handles visual trail effects for the plane.
/// Attach this script to the plane GameObject.
/// </summary>
public class PlaneEffects : MonoBehaviour
{
    [Header("Trail Effects")]
    public TrailRenderer[] wingTrails;
    public float minSpeedForTrail = 5f;
    public Color trailColorSlow = Color.white;
    public Color trailColorFast = Color.cyan;
    public float maxSpeedForColorChange = 15f;

    [Header("Trail Dimensions")]
    public float trailWidth = 0.1f;
    [Range(0.1f, 5.0f)]
    public float trailLifetime = 1.0f;

    // References
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Initialize trails
        if (wingTrails != null)
        {
            foreach (TrailRenderer trail in wingTrails)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                    trail.widthMultiplier = trailWidth;
                    trail.time = trailLifetime;
                }
            }
        }
    }

    void Update()
    {
        if (rb == null) return;

        float speed = rb.velocity.magnitude;

        // Handle trails
        UpdateTrails(speed);
    }

    void UpdateTrails(float speed)
    {
        if (wingTrails == null || wingTrails.Length == 0) return;

        bool shouldEmit = speed > minSpeedForTrail;

        foreach (TrailRenderer trail in wingTrails)
        {
            if (trail != null)
            {
                // Set trail emission
                trail.emitting = shouldEmit;

                // Update trail color based on speed
                if (shouldEmit)
                {
                    float speedFactor = Mathf.Clamp01((speed - minSpeedForTrail) / (maxSpeedForColorChange - minSpeedForTrail));
                    trail.startColor = Color.Lerp(trailColorSlow, trailColorFast, speedFactor);
                }

                // Always update width and lifetime in case they were changed in the inspector
                trail.widthMultiplier = trailWidth;
                trail.time = trailLifetime;
            }
        }
    }

    // Public method to be called when the plane crashes
    public void OnCrash()
    {
        // Stop trails
        if (wingTrails != null)
        {
            foreach (TrailRenderer trail in wingTrails)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                }
            }
        }
    }
}
