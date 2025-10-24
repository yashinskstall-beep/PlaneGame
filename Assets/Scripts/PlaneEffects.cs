using UnityEngine;

/// <summary>
/// Handles visual and audio effects for the plane.
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
    
    [Header("Particle Effects")]
    public ParticleSystem engineEffect;
    public ParticleSystem speedEffect;
    public float minSpeedForParticles = 10f;
    
    [Header("Audio")]
    public AudioSource engineAudio;
    public float minPitch = 0.5f;
    public float maxPitch = 1.5f;
    public float maxSpeedForPitch = 20f;
    
    // References
    private Rigidbody rb;
    private PlaneController planeController;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        planeController = GetComponent<PlaneController>();
        
        // Initialize trails to off
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
        
        // Initialize particles
        if (engineEffect != null)
        {
            engineEffect.Play();
        }
        
        if (speedEffect != null)
        {
            speedEffect.Stop();
        }
    }
    
    void Update()
    {
        float speed = rb.velocity.magnitude;
        
        // Handle trails
        UpdateTrails(speed);
        
        // Handle particles
        UpdateParticles(speed);
        
        // Handle audio
        UpdateAudio(speed);
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
            }
        }
    }
    
    void UpdateParticles(float speed)
    {
        // Engine effect is always on but varies intensity with speed
        if (engineEffect != null)
        {
            var emission = engineEffect.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(0.5f, 2f, Mathf.Clamp01(speed / maxSpeedForPitch));
        }
        
        // Speed effect only shows at high speeds
        if (speedEffect != null)
        {
            if (speed > minSpeedForParticles && !speedEffect.isPlaying)
            {
                speedEffect.Play();
            }
            else if (speed <= minSpeedForParticles && speedEffect.isPlaying)
            {
                speedEffect.Stop();
            }
            
            if (speedEffect.isPlaying)
            {
                var emission = speedEffect.emission;
                emission.rateOverTimeMultiplier = Mathf.Lerp(0.5f, 3f, Mathf.Clamp01((speed - minSpeedForParticles) / (maxSpeedForPitch - minSpeedForParticles)));
            }
        }
    }
    
    void UpdateAudio(float speed)
    {
        if (engineAudio != null)
        {
            // Adjust pitch based on speed
            float pitchFactor = Mathf.Clamp01(speed / maxSpeedForPitch);
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, pitchFactor);
            
            // Ensure audio is playing
            if (!engineAudio.isPlaying)
            {
                engineAudio.Play();
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
        
        // Stop speed effect
        if (speedEffect != null && speedEffect.isPlaying)
        {
            speedEffect.Stop();
        }
        
        // Reduce engine effect
        if (engineEffect != null)
        {
            var emission = engineEffect.emission;
            emission.rateOverTimeMultiplier = 0.2f;
        }
        
        // Change engine sound
        if (engineAudio != null)
        {
            engineAudio.pitch = minPitch * 0.7f;
            engineAudio.volume = engineAudio.volume * 0.5f;
        }
    }
}
