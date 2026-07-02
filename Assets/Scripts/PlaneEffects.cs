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

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        RefreshFlightTrails();
    }

    void Update()
    {
        if (rb == null)
            return;

        UpdateTrails(rb.velocity.magnitude);
    }

    void UpdateTrails(float speed)
    {
        if (wingTrails == null || wingTrails.Length == 0)
            return;

        bool shouldEmit = speed > minSpeedForTrail;

        foreach (TrailRenderer trail in wingTrails)
        {
            if (trail == null)
                continue;

            ApplyFlightTrailSettings(trail, shouldEmit);

            if (shouldEmit)
            {
                float speedFactor = Mathf.Clamp01(
                    (speed - minSpeedForTrail) / (maxSpeedForColorChange - minSpeedForTrail));
                trail.startColor = Color.Lerp(trailColorSlow, trailColorFast, speedFactor);
            }
        }
    }

    public void OnCrash()
    {
        RefreshFlightTrails();
    }

    public void ResetFlightTrails()
    {
        RefreshFlightTrails();
    }

    /// <summary>
    /// Re-applies flight trail settings and disables any extra scene trails
    /// (e.g. tail Wingtrail used for upgrade FX) that are not in wingTrails.
    /// Call after part unlocks or slingshot resets so trails don't inherit bad state.
    /// </summary>
    public void RefreshFlightTrails()
    {
        if (wingTrails != null)
        {
            foreach (TrailRenderer trail in wingTrails)
            {
                if (trail == null)
                    continue;

                EnsureManagedTrailActive(trail);
                ApplyFlightTrailSettings(trail, false);
            }
        }

        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
        {
            if (trail == null || IsManagedFlightTrail(trail))
                continue;

            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
        }
    }

    private void EnsureManagedTrailActive(TrailRenderer trail)
    {
        if (trail == null)
            return;

        trail.enabled = true;
        if (!trail.gameObject.activeSelf && trail.transform.parent != null
            && trail.transform.parent.gameObject.activeInHierarchy)
        {
            trail.gameObject.SetActive(true);
        }
    }

    public bool IsManagedFlightTrail(TrailRenderer trail)
    {
        if (trail == null || wingTrails == null)
            return false;

        foreach (TrailRenderer wingTrail in wingTrails)
        {
            if (wingTrail == trail)
                return true;
        }

        return false;
    }

    private void ApplyFlightTrailSettings(TrailRenderer trail, bool emitting)
    {
        if (trail == null)
            return;

        trail.enabled = true;
        trail.widthMultiplier = trailWidth;
        trail.time = trailLifetime;
        trail.emitting = emitting;

        if (!emitting)
            trail.Clear();
    }
}
