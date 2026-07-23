using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform airplaneTarget;  // The airplane to follow initially
    private Transform target;         // The current target (can be airplane or marker)
    private bool markerSpawned = false; // Flag to check if marker is spawned
    public bool isCameraZoomedOut = false;
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 2.2f, -1.8f); // How far behind/above the target
    public float followSpeed = 5f;                    // How quickly the camera catches up
    public float rotationSmoothness = 5f;             // How smoothly the camera rotates
    public float rotationSmoothnessWhenSpinning = 1.5f; // Rotation smoothness when plane is spinning (lower = smoother)
    public float frozenZoomAmount = 5f;               // How much to zoom out when camera freezes
    public float zoomSmoothness = 2f;                 // How smoothly to zoom out
    [Tooltip("Tilt the camera up from looking at the plane (degrees). Distance stays the same; higher = more forward view.")]
    public float forwardViewPitchDegrees = 14f;
    
    [Header("Landing Camera Settings")]
    public float speedThresholdForSlowdown = 5.0f;     // Speed at which camera starts to slow down
    public float speedThresholdForStop = 1.0f;         // Speed at which camera stops completely
    public float cameraSlowdownFactor = 0.8f;          // How quickly camera slows down with plane
    
    [Header("References")]
    public SimpleDragLauncher dragLauncher;
    
    // Private variables
    private Vector3 cameraVelocity = Vector3.zero;     // For smooth damp
    private Vector3 lastGoodCameraPosition;            // Last position when plane was moving well
    private Quaternion lastGoodCameraRotation;         // Last rotation when plane was moving well
    private float maxPlaneSpeed = 0f;                  // Track the max speed the plane reached
    private bool hasStartedFollowing = false;          // Whether we've started following the plane
    private bool hasFrozenCamera = false;              // Whether we've frozen the camera position
    private bool isPositionFrozen = false;             // A flag to lock the camera's position on command
    private bool isPlaneControlling = false;           // Whether the plane is in controlling mode
    private Vector3 fixedOffset;                       // Fixed offset to maintain during controlling
    
    // Marker transition variables
    private bool isTransitioningToMarker = false;      // Whether we're transitioning to marker
    private Vector3 markerTransitionStartPos;          // Starting position for transition
    private Quaternion markerTransitionStartRot;       // Starting rotation for transition
    private float markerTransitionProgress = 0f;       // Progress of transition (0 to 1)
    public float markerTransitionDuration = 2f;        // Duration of transition in seconds
    public float markerZoomOutDistance = 15f;          // How far to zoom out when viewing marker
    public Vector3 markerCameraOffset = new Vector3(0f, 10f, -12f); // Camera offset when viewing marker
    
    public void ResetForMenu()
    {
        if (airplaneTarget != null)
            target = airplaneTarget;

        markerSpawned = false;
        isTransitioningToMarker = false;
        isPositionFrozen = false;
        isCameraZoomedOut = false;
        hasStartedFollowing = false;
        hasFrozenCamera = false;
        isPlaneControlling = false;
    }

    void Start()
    {
        // Start by following the airplane
        target = airplaneTarget;
        isCameraZoomedOut = false;

        if (target != null)
        {
            // Initialize with the default offset
            lastGoodCameraPosition = target.position + offset;
            lastGoodCameraRotation = Quaternion.LookRotation(target.position - lastGoodCameraPosition);
            fixedOffset = offset; // Initialize fixed offset
        }
        else
        {
            Debug.LogError("Airplane Target is not assigned in the CameraFollow script!");
        }
    }

    /// <summary>
    /// Call this method from another script when the marker is spawned.
    /// </summary>
    /// <param name="markerTarget">The transform of the spawned marker.</param>
    public void TransitionToMarker(Transform markerTarget)
    {
        if (markerTarget == null)
        {
            Debug.LogWarning("TransitionToMarker called with a null target.");
            return;
        }

        target = markerTarget;
        markerSpawned = true;

        // Initiate smooth transition to the marker
        isTransitioningToMarker = true;
        markerTransitionStartPos = transform.position;
        markerTransitionStartRot = transform.rotation;
        markerTransitionProgress = 0f;
        isPositionFrozen = false; // Unfreeze to allow the transition to happen
    }

    public void FreezePosition()
    {
        isPositionFrozen = true;
    }
    
    /// <summary>
    /// Return to following the airplane instead of the marker
    /// </summary>
    public void ReturnToPlane()
    {
        if (airplaneTarget == null)
        {
            Debug.LogWarning("Cannot return to plane: airplaneTarget is null.");
            return;
        }
        
        // Switch target back to the airplane
        target = airplaneTarget;
        
        // Reset marker flags
        markerSpawned = false;
        isTransitioningToMarker = false;
        isPositionFrozen = false;
        isCameraZoomedOut = false;
        
        // Reset controlling flag so it can be recaptured
        isPlaneControlling = false;
    }

    void FixedUpdate()
    {
        // If a marker has been spawned, focus on it
        if (markerSpawned && target != airplaneTarget)
        {
            HandleMarkerCamera();
        }
        // Otherwise, follow the airplane
        else
        {
            // Reset marker flag if we're not focusing on a marker anymore
            if (markerSpawned && target == airplaneTarget)
            {
                markerSpawned = false;
                isTransitioningToMarker = false;
                isPositionFrozen = false;
                isCameraZoomedOut = false;
            }
            
            HandleAirplaneCamera();
        }
    }

    private void HandleMarkerCamera()
    {
        if (target == null) return;

        // Handle marker transition
        if (isTransitioningToMarker)
        {
            markerTransitionProgress += Time.fixedDeltaTime / markerTransitionDuration;

            if (markerTransitionProgress >= 1f)
            {
                markerTransitionProgress = 1f;
                isTransitioningToMarker = false;
                isPositionFrozen = true; // Freeze after transition completes
            }

            // Calculate target position for camera (above and behind marker)
            Vector3 targetCameraPos = target.position + markerCameraOffset;
            
            // Calculate target rotation (looking at marker)
            Quaternion targetRotation = Quaternion.LookRotation(target.position - targetCameraPos);

            // Smooth transition using easing
            float easedProgress = Mathf.SmoothStep(0f, 1f, markerTransitionProgress);
            transform.position = Vector3.Lerp(markerTransitionStartPos, targetCameraPos, easedProgress);
            transform.rotation = Quaternion.Slerp(markerTransitionStartRot, targetRotation, easedProgress);
            
            // Apply zoom out effect as we transition
            float zoomFactor = Mathf.Lerp(1f, markerZoomOutDistance / Vector3.Distance(markerTransitionStartPos, target.position), easedProgress);
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Vector3 zoomedOutPos = target.position - directionToTarget * Vector3.Distance(transform.position, target.position) * zoomFactor;
            transform.position = Vector3.Lerp(transform.position, zoomedOutPos, Time.fixedDeltaTime * zoomSmoothness);
        }
        // If the position is frozen, just look at the current target and do nothing else.
        else if (isPositionFrozen)
        {
            transform.LookAt(target);
            
            // Apply a slight floating/orbiting motion to make the camera more dynamic
            float time = Time.time * 0.5f;
            Vector3 orbitOffset = new Vector3(
                Mathf.Sin(time) * 2f,
                Mathf.Cos(time * 0.6f) * 1f,
                Mathf.Sin(time * 0.4f) * 2f
            );
            
            // Calculate base position (above and behind marker)
            Vector3 basePosition = target.position + markerCameraOffset;
            
            // Apply the orbit offset
            transform.position = Vector3.Lerp(transform.position, basePosition + orbitOffset, Time.fixedDeltaTime);
        }
        isCameraZoomedOut = true;
    }

    private void HandleAirplaneCamera()
    {
        if (airplaneTarget == null) return;
        target = airplaneTarget;

        if (target == null) return;

        // If the position is frozen, just look at the current target and do nothing else.
        if (isPositionFrozen)
        {
            transform.LookAt(target);
            return;
        }
        
        // Check if the plane is in controlling mode
        PlaneController planeController = target.GetComponent<PlaneController>();
        if (planeController != null)
        {
            if (!isPlaneControlling && planeController.isControlling)
            {
                isPlaneControlling = true;
                fixedOffset = transform.position - target.position;
            }
            else if (isPlaneControlling && !planeController.isControlling)
            {
                isPlaneControlling = false;
            }
        }
        else
        {
            Debug.LogWarning("PlaneController component not found on target!");
        }

        if (dragLauncher == null) return;

        if (dragLauncher.released)
        {
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb == null) return;
            
            float currentSpeed = targetRb.velocity.magnitude;
            
            // Track max speed for reference
            if (currentSpeed > maxPlaneSpeed)
            {
                maxPlaneSpeed = currentSpeed;
                hasStartedFollowing = true;
            }
            
            // Calculate what percentage of max speed we're at
            float speedRatio = maxPlaneSpeed > 0 ? currentSpeed / maxPlaneSpeed : 0;
            
            // If the plane is in controlling mode, maintain a fixed distance
            if (isPlaneControlling)
            {
                // Check if plane has wing damage causing spinning
                PlaneDamageHandler damageHandler = target.GetComponent<PlaneDamageHandler>();
                bool isSpinning = false;
                if (damageHandler != null)
                {
                    // Check if only one wing is missing (causes spinning)
                    isSpinning = damageHandler.IsLeftWingMissing() != damageHandler.IsRightWingMissing();
                }
                
                // Use the fixed offset captured when controlling started
                Vector3 targetPosition = target.position + fixedOffset;
                
                // Use smooth damp for more controlled camera movement
                float smoothTime = 0.1f; // Lower value = faster response
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, smoothTime);
                
                // Handle rotation: if spinning, keep rotation stable; otherwise follow normally
                if (isSpinning)
                {
                    // When spinning, maintain the current camera rotation (don't follow plane's rotation)
                    // Camera keeps plane centered via position following, but rotation stays still
                    // No rotation update - camera maintains its current orientation
                }
                else
                {
                    // Same distance — only tip the aim up so more course is visible ahead.
                    AimForForwardView(target, 3f);
                    lastGoodCameraRotation = transform.rotation;
                }
                
                // Store this good position
                lastGoodCameraPosition = transform.position;
                
                // Reset frozen flag if we're moving again
                hasFrozenCamera = false;
            }
            // If plane is moving well and not in controlling mode, update the good camera position and rotation
            else if (currentSpeed > speedThresholdForSlowdown && hasStartedFollowing)
            {
                // Normal camera following
                Vector3 targetPosition = target.position + offset;
                
                // Use smooth damp for more controlled camera movement
                float smoothTime = 0.1f; // Lower value = faster response
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, smoothTime);
                
                AimForForwardView(target, rotationSmoothness);
                
                // Store this good position and rotation
                lastGoodCameraPosition = transform.position;
                lastGoodCameraRotation = transform.rotation;
                
                // Reset frozen flag if we're moving again
                hasFrozenCamera = false;
            }
            // If plane is slowing down but still moving
            else if (currentSpeed <= speedThresholdForSlowdown && currentSpeed > speedThresholdForStop && hasStartedFollowing)
            {
                // Calculate a position that gradually slows down with the plane
                Vector3 targetPosition = target.position + offset;
                
                // Gradually reduce camera movement as plane slows
                float slowdownFactor = Mathf.Lerp(1.0f, 0.1f, 1.0f - speedRatio) * cameraSlowdownFactor;
                float smoothTime = 0.1f / slowdownFactor; // Higher value = slower response
                
                // Move camera with reduced speed
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, smoothTime);
                
                AimForForwardView(target, rotationSmoothness);
            }
            // If plane has nearly stopped
            else if (currentSpeed <= speedThresholdForStop && hasStartedFollowing && !hasFrozenCamera)
            {
                // Freeze the camera at its last good position and rotation
                hasFrozenCamera = true;
                
                // Just to be safe, update the last good position one more time
                // but maintain the same relative angle to the target
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                
                // Slightly adjust position to keep the same distance from target
                lastGoodCameraPosition = target.position - directionToTarget * distanceToTarget;
            }
            
            // If camera is frozen, zoom out
            if (hasFrozenCamera)
            {
                // Calculate the direction from the target to the last good camera position
                Vector3 directionFromTarget = (lastGoodCameraPosition - target.position).normalized;

                // Calculate the new zoomed-out position by adding the zoom amount
                float targetDistance = Vector3.Distance(lastGoodCameraPosition, target.position) + frozenZoomAmount;
                Vector3 zoomedOutPosition = target.position + directionFromTarget * targetDistance;

                // Smoothly move the camera to the zoomed-out position
                transform.position = Vector3.Lerp(transform.position, zoomedOutPosition, Time.deltaTime * zoomSmoothness);

                // Keep looking toward the plane with the same forward tilt
                AimForForwardView(target, zoomSmoothness);
            }
        }
    }

    /// <summary>
    /// Keeps chase distance unchanged; only rotates the camera up so more forward scenery is visible.
    /// </summary>
    private void AimForForwardView(Transform plane, float rotSpeed)
    {
        if (plane == null)
            return;

        Vector3 toPlane = plane.position - transform.position;
        if (toPlane.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookAtPlane = Quaternion.LookRotation(toPlane.normalized, Vector3.up);
        Quaternion pitchedUp = lookAtPlane * Quaternion.Euler(-forwardViewPitchDegrees, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, pitchedUp, Time.fixedDeltaTime * Mathf.Max(0.1f, rotSpeed));
    }

}
