using UnityEngine;

/// <summary>
/// Landing-flag prefab holder and placement. PlaneController decides when; this places the marker.
/// </summary>
public class LandingFlagPlacer : MonoBehaviour
{
    [Header("Marker Settings")]
    [Tooltip("The prefab to spawn as a marker - REQUIRED")]
    public GameObject markerPrefab;

    [Tooltip("How long markers stay visible (in seconds). 0 = keep until LandingMarker manages it.")]
    public float markerLifetime = 10f;

    [Tooltip("Height offset above ground to prevent z-fighting")]
    public float markerOffset = 0.05f;

    [Tooltip("Only mark collisions with objects tagged as 'Ground' (legacy collision path)")]
    public bool onlyMarkGroundCollisions = true;

    [Header("Marker Appearance")]
    [Tooltip("Color to apply to the marker if it has a renderer")]
    public Color markerColor = Color.red;

    private bool hasCollided;

    private void Start()
    {
        if (markerPrefab == null)
            Debug.LogError("No marker prefab assigned to LandingFlagPlacer on " + gameObject.name);
    }

    public void ResetCollisionState()
    {
        hasCollided = false;
    }

    /// <summary>
    /// Places an upright landing flag at a surface point. Returns the spawned marker, or null.
    /// </summary>
    public GameObject PlaceLandingMarker(Vector3 surfacePoint, Vector3 surfaceNormal, float yOffset)
    {
        if (markerPrefab == null)
        {
            Debug.LogError("Cannot place marker: No marker prefab assigned to LandingFlagPlacer.");
            return null;
        }

        if (surfaceNormal.sqrMagnitude < 0.001f)
            surfaceNormal = Vector3.up;
        else
            surfaceNormal.Normalize();

        GameObject marker = Instantiate(markerPrefab, surfacePoint + Vector3.up * yOffset, Quaternion.identity);
        marker.isStatic = false;

        float baseOffset = GetMarkerBaseOffset(marker);
        marker.transform.position = surfacePoint + Vector3.up * (yOffset + baseOffset);

        LandingMarker landingMarker = marker.GetComponent<LandingMarker>();
        if (landingMarker == null)
            landingMarker = marker.AddComponent<LandingMarker>();

        if (landingMarker != null)
            landingMarker.markerColor = markerColor;
        else if (markerLifetime > 0f)
            Destroy(marker, markerLifetime);

        hasCollided = true;
        return marker;
    }

    private static float GetMarkerBaseOffset(GameObject marker)
    {
        Renderer markerRenderer = marker.GetComponentInChildren<Renderer>();
        if (markerRenderer == null)
            return 0f;
        return -markerRenderer.localBounds.min.y;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Placement is driven by PlaneController when the flight ends.
    }
}
