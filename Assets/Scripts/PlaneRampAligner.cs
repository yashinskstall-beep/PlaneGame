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
    public float alignmentSpeed = 10f;      // How quickly the plane aligns with the ramp (increased for better responsiveness)
    public float minVelocityForAlignment = 1f; // Minimum velocity needed to align with direction of travel
    public bool alignToVelocity = true;     // Whether to align the forward direction with velocity
    public string rampTag = "RampTag";      // Tag to identify ramp objects (optional)
    public bool useTagForDetection = false; // Whether to use tag for detection or check all collisions

    private Rigidbody planeRb;
    private bool isAligning = false;
    private Transform currentRamp; // The ramp we're currently in contact with
    private Quaternion originalRotation; // Store the original rotation of the plane

    /// <summary>True while the plane is contact-aligned to a ramp.</summary>
    public bool IsAligning => isAligning;

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
        // Align if we're in contact with a ramp, regardless of release state
        if (isAligning && currentRamp != null)
        {
            AlignWithRamp();
        }
    }

    private void AlignWithRamp()
    {
        if (currentRamp == null || plane == null || planeRb == null) return;

        // Get the ramp's up direction (normal to the ramp surface)
        Vector3 rampNormal = currentRamp.up;
        
        // Get the ramp's forward direction (direction plane should face)
        Vector3 rampForward = currentRamp.forward;
        
        // Create a rotation that aligns the plane with the ramp
        // Plane's up should match ramp's up, and plane's forward should match ramp's forward
        Quaternion targetRotation = Quaternion.LookRotation(rampForward, rampNormal);
        
        // If we should also align with velocity direction AND plane is moving
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
        plane.rotation = Quaternion.Slerp(plane.rotation, targetRotation, Time.fixedDeltaTime * alignmentSpeed);
        
        // Debug visualization
        Debug.DrawRay(plane.position, rampNormal * 2f, Color.green);
        Debug.DrawRay(plane.position, rampForward * 2f, Color.red);
        if (planeRb.velocity.magnitude > 0.1f)
            Debug.DrawRay(plane.position, planeRb.velocity.normalized * 2f, Color.blue);
    }

    // Called when the plane collider enters another collider
    private void OnCollisionEnter(Collision collision)
    {
        // Check if this is a ramp we should align with
        if (IsRamp(collision.transform))
        {
            // Store the original rotation when first contacting a ramp
            if (currentRamp == null)
            {
                originalRotation = plane.rotation;
            }
            
            currentRamp = collision.transform;
            isAligning = true;
        }
    }
    
    // Called when the plane collider stays in contact with another collider
    private void OnCollisionStay(Collision collision)
    {
        // If we're already aligning with a ramp, don't change to a new one
        if (currentRamp == null)
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
            // StartCoroutine(SmoothlyRestoreRotation()); // DISABLED: Don't restore original rotation
            
            // Notify the PlaneController that we've exited a ramp
            PlaneController planeController = plane.GetComponent<PlaneController>();
            if (planeController != null)
            {
                // ForceControl refuses grounded / marker-placed states so a crash
                // cannot be turned back into flight by leaving a collider.
                planeController.ForceControl();

                if (planeController.isControlling && planeController.useJoystickInput && planeController.joystick != null)
                {
                    planeController.joystick.gameObject.SetActive(true);
                }
            }
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // this is a fix for taking input on flight mode
            
            // Remove all rotation constraints
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

            // Switch from root sphere → part MeshColliders for realistic flight collisions.
            if (planeController != null)
                planeController.UseFlightPartColliders();
            else
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
