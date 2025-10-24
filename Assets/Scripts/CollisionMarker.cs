using UnityEngine;

/// <summary>
/// Creates visual markers at plane collision points using a custom prefab
/// </summary>
public class CollisionMarker : MonoBehaviour
{
    [Header("Marker Settings")]
    [Tooltip("The prefab to spawn as a marker - REQUIRED")]
    public GameObject markerPrefab;  // The prefab to spawn as a marker
    
    [Tooltip("How long markers stay visible (in seconds)")]
    public float markerLifetime = 10f;  
    
    [Tooltip("Height offset above ground to prevent z-fighting")]
    public float markerOffset = 0.05f;  
    
    [Tooltip("Only mark collisions with objects tagged as 'Ground'")]
    public bool onlyMarkGroundCollisions = true;  
    
    [Header("Marker Appearance")]
    [Tooltip("Color to apply to the marker if it has a renderer")]
    public Color markerColor = Color.red;  
    
    // Internal tracking
    private bool hasCollided = false;  // Track if we've already placed a marker
    
    private void Start()
    {
        // Validate that a marker prefab is assigned
        if (markerPrefab == null)
        {
            Debug.LogError("No marker prefab assigned to CollisionMarker component on " + gameObject.name + ". Please assign a prefab in the inspector.");
        }
    }
    
    /// <summary>
    /// Reset collision state when the plane is launched again
    /// </summary>
    public void ResetCollisionState()
    {
        hasCollided = false;
    }
    
    /// <summary>
    /// Called when this object collides with another collider
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // This method is now disabled because the PlaneController handles marker placement
        // when the plane comes to a complete stop, not on initial collision
        
        // Uncomment the code below if you want to revert to placing markers on collision
        /*
        // Only place a marker on the first collision after a launch
        if (hasCollided) return;
        
        // Check if we should only mark ground collisions
        if (onlyMarkGroundCollisions && !collision.gameObject.CompareTag("Ground"))
        {
            return;
        }
        
        // Get the collision point
        ContactPoint contact = collision.GetContact(0);
        Vector3 position = contact.point;
        
        // Add a small offset to prevent z-fighting with the ground
        position.y += markerOffset;
        
        // Place the marker
        PlaceMarker(position, contact.normal);
        
        // Mark that we've collided
        hasCollided = true;
        */
    }
    
    /// <summary>
    /// Places a marker at the specified position using the custom prefab
    /// </summary>
    private void PlaceMarker(Vector3 position, Vector3 normal)
    {
        // Check if we have a marker prefab
        if (markerPrefab == null)
        {
            Debug.LogError("Cannot place marker: No marker prefab assigned to CollisionMarker component.");
            return;
        }
        
        // Add a small offset to prevent z-fighting with the ground
        position += normal * markerOffset;
        
        // Instantiate the custom prefab
        GameObject marker = Instantiate(markerPrefab, position, Quaternion.identity);
        
        // Orient the marker to align with the surface normal
        marker.transform.up = normal;
        
        // Make sure the marker is visible in the scene hierarchy
        marker.isStatic = false;
        
        // Try to apply the color to any renderers in the prefab
        ApplyColorToMarker(marker);
        
        // Destroy the marker after the lifetime
        if (markerLifetime > 0)
        {
            Destroy(marker, markerLifetime);
        }
        
        Debug.Log($"Placed custom marker prefab at {position}");
    }
    
    /// <summary>
    /// Applies the marker color to any renderers in the marker prefab
    /// </summary>
    private void ApplyColorToMarker(GameObject marker)
    {
        // Try to find any renderers in the marker or its children
        Renderer[] renderers = marker.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            // Skip if the renderer doesn't have a material
            if (renderer.material == null) continue;
            
            // Create a new material instance to avoid shared material issues
            Material newMaterial = new Material(renderer.material);
            
            // Try to set the color if the material has a color property
            if (newMaterial.HasProperty("_Color"))
            {
                newMaterial.color = markerColor;
            }
            
            // Apply the new material
            renderer.material = newMaterial;
        }
    }
}
