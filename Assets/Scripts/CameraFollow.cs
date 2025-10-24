using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target;          // The cube or plane to follow

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 3f, -6f); // How far behind/above the target
    public float followSpeed = 5f;                    // How quickly the camera catches up
    public float rotationSmoothness = 5f;             // How smoothly the camera rotates
    
    [Header("Landing Camera Settings")]
    public float speedThresholdForSlowdown = 5.0f;     // Speed at which camera starts to slow down
    public float speedThresholdForStop = 1.0f;         // Speed at which camera stops completely
    public float cameraSlowdownFactor = 0.8f;          // How quickly camera slows down with plane
    
    [Header("References")]
    public SimpleDragLauncher dragLauncher;
    
    // Private variables
    private Vector3 cameraVelocity = Vector3.zero;      // For smooth damp
    private Vector3 lastGoodCameraPosition;            // Last position when plane was moving well
    private Quaternion lastGoodCameraRotation;         // Last rotation when plane was moving well
    private float maxPlaneSpeed = 0f;                  // Track the max speed the plane reached
    private bool hasStartedFollowing = false;          // Whether we've started following the plane
    private bool hasFrozenCamera = false;              // Whether we've frozen the camera position
    
    void Start()
    {
        if (target != null)
        {
            // Initialize with the default offset
            lastGoodCameraPosition = target.position + offset;
            lastGoodCameraRotation = Quaternion.LookRotation(target.position - lastGoodCameraPosition);
        }
    }

    void FixedUpdate()
    {
        if (!target || dragLauncher == null) return;

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
            
            // If plane is moving well, update the good camera position and rotation
            if (currentSpeed > speedThresholdForSlowdown && hasStartedFollowing)
            {
                // Normal camera following
                Vector3 targetPosition = target.position + offset;
                
                // Use smooth damp for more controlled camera movement
                float smoothTime = 0.1f; // Lower value = faster response
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, smoothTime);
                
                // Look at target
                transform.LookAt(target);
                
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
                
                // Look at target
                transform.LookAt(target);
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
                
                Debug.Log("Camera frozen at final position");
            }
            
            // If camera is frozen, maintain the same relative position to the target
            if (hasFrozenCamera)
            {
                // Keep the same relative position to the target
                Vector3 directionToTarget = (target.position - lastGoodCameraPosition).normalized;
                float distanceToTarget = Vector3.Distance(lastGoodCameraPosition, target.position);
                
                // Position camera at the same distance and angle from target
                transform.position = target.position - directionToTarget * distanceToTarget;
                // Keep looking at target
                transform.LookAt(target);
            }
        }
    }

}
