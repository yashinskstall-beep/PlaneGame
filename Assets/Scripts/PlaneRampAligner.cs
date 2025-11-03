using UnityEngine;

/// <summary>
/// Handles aligning a plane GameObject with a 3D ramp after being released
/// Attach this script to the same GameObject as the SimpleDragLauncher or to the plane itself
/// </summary>
public class PlaneRampAligner : MonoBehaviour
{
    [Header("References")]
    public Transform plane;        // The plane GameObject to align
    public SimpleDragLauncher dragLauncher; // Reference to the drag launcher script
    public Transform[] ramps;     // Optional: Specific ramps to detect (if not using tags)

    [Header("Alignment Settings")]
    public float alignmentSpeed = 5f;       // How quickly the plane aligns with the ramp
    public float minVelocityForAlignment = 1f; // Minimum velocity needed to align with direction of travel
    public bool alignToVelocity = true;     // Whether to align the forward direction with velocity
    public string rampTag = "RampTag";      // Tag to identify ramp objects (optional)
    public bool useTagForDetection = false; // Whether to use tag for detection or check all collisions

    private Rigidbody planeRb;
    private bool isAligning = false;
    private Transform currentRamp; // The ramp we're currently in contact with
    private Quaternion originalRotation; // Store the original rotation of the plane

    private void Start()
    {
        // If no plane is assigned, use this GameObject
        if (plane == null)
            plane = transform;

        // Get the rigidbody component
        planeRb = plane.GetComponent<Rigidbody>();
        
        // If no drag launcher is assigned, try to find one on this GameObject
        if (dragLauncher == null)
            dragLauncher = GetComponent<SimpleDragLauncher>();
    }

    private void FixedUpdate()
    {
        // Only align if we're in contact with a ramp and the plane has been released
        if (isAligning && currentRamp != null && dragLauncher != null && dragLauncher.released)
        {
            AlignWithRamp();
        }
    }

    private void AlignWithRamp()
    {
        if (currentRamp == null || plane == null || planeRb == null) return;

        // Get the ramp's up direction (normal to the ramp surface)
        Vector3 rampNormal = currentRamp.up;
        
        // First, create a rotation that aligns the plane's up with the ramp surface normal
        Quaternion targetRotation = Quaternion.FromToRotation(plane.up, rampNormal) * plane.rotation;
        
        // If we should also align with velocity direction
        if (alignToVelocity && planeRb.velocity.magnitude > minVelocityForAlignment)
        {
            // Project the velocity onto the ramp surface to get a forward direction
            Vector3 projectedVelocity = Vector3.ProjectOnPlane(planeRb.velocity, rampNormal).normalized;
            
            if (projectedVelocity.magnitude > 0.1f) // Make sure we have a valid direction
            {
                // Create a rotation that looks in the direction of travel while keeping the up aligned with the ramp
                targetRotation = Quaternion.LookRotation(projectedVelocity, rampNormal);
            }
        }
        
        // Smoothly interpolate to the target rotation
        plane.rotation = Quaternion.Slerp(plane.rotation, targetRotation, Time.deltaTime * alignmentSpeed);
        
        // Debug visualization
        Debug.DrawRay(plane.position, rampNormal * 2f, Color.green);
        Debug.DrawRay(plane.position, planeRb.velocity.normalized * 2f, Color.blue);
    }

    // Called when the plane collider enters another collider
    private void OnCollisionEnter(Collision collision)
    {
        // Only process if the plane has been released
        if (dragLauncher == null || !dragLauncher.released) return;
        
        // Check if this is a ramp we should align with
        if (IsRamp(collision.transform))
        {
            // Store the original rotation when first contacting a ramp
            if (currentRamp == null)
            {
                originalRotation = plane.rotation;
                Debug.Log("Stored original plane rotation");
            }
            
            currentRamp = collision.transform;
            isAligning = true;
            Debug.Log($"Plane contacted ramp: {collision.gameObject.name}");
        }
    }
    
    // Called when the plane collider stays in contact with another collider
    private void OnCollisionStay(Collision collision)
    {
        // If we're already aligning with a ramp, don't change to a new one
        if (currentRamp == null && dragLauncher != null && dragLauncher.released)
        {
            if (IsRamp(collision.transform))
            {
                currentRamp = collision.transform;
                isAligning = true;
            }
        }
    }
    
    // Called when the plane collider exits another collider
    private void OnCollisionExit(Collision collision)
    {
        // If we're leaving the current ramp, stop aligning
        if (currentRamp == collision.transform)
        {
            currentRamp = null;
            isAligning = false;
            
            // Restore the original rotation when leaving the ramp
            StartCoroutine(SmoothlyRestoreRotation());
            
            // Notify the PlaneController that we've exited a ramp
            PlaneController planeController = plane.GetComponent<PlaneController>();
            if (planeController != null)
            {
                planeController.ForceControl();
            }
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // this is a fix for taking input on flight mode
            Debug.Log("Raycast ignored");
            
            Debug.Log("Plane left the ramp, restoring original rotation");

            // Remove all rotation constraints
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
            Debug.Log("Plane RB rotation unfrozen");

            gameObject.GetComponent<Collider>().enabled = false;
        
            
        }
    } 
    
    // Coroutine to smoothly restore the original rotation
    private System.Collections.IEnumerator SmoothlyRestoreRotation()
    {
        float elapsedTime = 0f;
        float duration = 2.5f; // Time to restore rotation (adjust as needed)
        Quaternion startRotation = plane.rotation;
        
        while (elapsedTime < duration)
        {
            // Smoothly interpolate from current rotation to original rotation
            plane.rotation = Quaternion.Slerp(startRotation, originalRotation, elapsedTime / duration);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end exactly at the original rotation
        plane.rotation = originalRotation;
    }
    
    // Helper method to determine if a transform is a ramp we should align with
    private bool IsRamp(Transform potentialRamp)
    {
        // If using tag detection and the tag exists on this object
        if (useTagForDetection)
        {
            try
            {
                return potentialRamp.gameObject.CompareTag(rampTag);
            }
            catch (UnityException)
            {
                // Tag doesn't exist, fall back to other methods
                Debug.LogWarning($"Tag '{rampTag}' is not defined in Unity Tags. Using fallback detection.");
                useTagForDetection = false; // Disable tag detection for future calls
            }
        }
        
        // If specific ramps are assigned, check if this is one of them
        if (ramps != null && ramps.Length > 0)
        {
            foreach (Transform ramp in ramps)
            {
                if (ramp == potentialRamp)
                    return true;
            }
            return false;
        }
        
        // If no specific detection method is available, accept any collision as a ramp
        // You might want to add additional checks here based on your game's needs
        return true;
    }

    // Visualize the alignment in the editor
    private void OnDrawGizmos()
    {
        if (plane != null && currentRamp != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(plane.position, currentRamp.position);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(currentRamp.position, currentRamp.up * 2f);
        }
    }
}
