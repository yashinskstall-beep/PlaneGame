using UnityEngine;

/// <summary>
/// Controls the plane's handling (turning, banking, pitching) after it exits a ramp.
/// Attach this script to the plane GameObject (which has the Rigidbody).
/// </summary>
public class PlaneController : MonoBehaviour
{
    [Header("References")]
    public PlaneRampAligner rampAligner;  // Reference to the ramp aligner script
    public CollisionMarker collisionMarker;  // Reference to the collision marker script
    public JoystickController joystick;  // Reference to the joystick controller

    [Header("Handling Settings")]
    public float turnSpeed = 3f;
    public float bankAngle = 30f;
    public float pitchSpeed = 2f;
    public float torqueResponseMultiplier = 0.5f;
    public float angularDragAmount = 0.5f;

    [Header("Input Settings")]
    public bool useKeyboardInput = true;
    public bool useJoystickInput = false;
    public float horizontalInputSensitivity = 1f;
    public float verticalInputSensitivity = 1f;
    public bool invertJoystickVertical = true;
    public bool autoLevelWhenNoInput = true;
    public float autoLevelSpeed = 1f;
    [Tooltip("If true, auto-leveling will be disabled when the plane is being dragged")]
    public bool disableAutoLevelWhenDragging = true;

    [Header("Movement Alignment")]
    public float directionAlignmentStrength = 5.0f;
    public float minSpeedForAlignment = 2.0f;

    [Header("Ground Movement Settings")]
    public float groundDragFactor = 0.98f;
    public float minGroundSpeed = 0.1f;
    public float groundAlignmentSpeed = 5.0f;
    public float groundCheckDistance = 0.5f;

    [Header("Smoothing Settings")]
    [Tooltip("How quickly input changes are smoothed (higher = snappier, lower = smoother)")]
    public float inputSmoothness = 6f;
    [Tooltip("How quickly torque is smoothed (helps prevent jerky rotation)")]
    public float torqueSmoothness = 6f;

    // Private variables
    private Rigidbody rb;
    private bool isControlling = false;
    private bool wasOnRamp = false;
    private bool exitedRamp = false;
    private bool isGrounded = false;
    private bool isBeingDragged = false;

    private float smoothHorizontalInput = 0f;
    private float smoothVerticalInput = 0f;
    private Vector3 smoothTorque = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.angularDrag = angularDragAmount;
            Debug.Log($"PlaneController: Configured Rigidbody with angular drag {angularDragAmount}");
        }
        else
        {
            Debug.LogError("PlaneController: No Rigidbody component found!");
        }

        if (collisionMarker == null)
        {
            collisionMarker = GetComponent<CollisionMarker>();
            if (collisionMarker == null)
                collisionMarker = gameObject.AddComponent<CollisionMarker>();
        }

        if (rampAligner == null)
        {
            rampAligner = GetComponent<PlaneRampAligner>();
            if (rampAligner == null)
                rampAligner = FindObjectOfType<PlaneRampAligner>();
        }

        if (useJoystickInput && joystick == null)
        {
            joystick = FindObjectOfType<JoystickController>();
        }
    }

    void FixedUpdate()
    {
        bool isOnRamp = false;
        if (rampAligner != null)
        {
            var isAligningField = typeof(PlaneRampAligner).GetField("isAligning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isAligningField != null)
                isOnRamp = (bool)isAligningField.GetValue(rampAligner);
        }

        // Check if the plane is being dragged
        CheckIfBeingDragged();

        if (wasOnRamp && !isOnRamp)
        {
            StartControlling();
        }

        wasOnRamp = isOnRamp;

        if (isControlling)
        {
            ApplyPlaneHandling();
        }
        else if (isGrounded)
        {
            HandleGroundMovement();
        }
    }

    void CheckIfBeingDragged()
    {
        // Check if there's a SimpleDragLauncher in the scene
        SimpleDragLauncher dragLauncher = GetComponent<SimpleDragLauncher>();
        if (dragLauncher == null)
        {
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();
        }

        // If we found a drag launcher and it's not released yet, the plane is being dragged
        if (dragLauncher != null)
        {
            isBeingDragged = !dragLauncher.released;
        }
        else
        {
            isBeingDragged = false;
        }
    }

    void StartControlling()
    {
        isControlling = true;
        exitedRamp = true;
        Debug.Log("PlaneController: Control started after ramp exit.");

        if (collisionMarker != null)
            collisionMarker.ResetCollisionState();
    }

    private void JoystickInput(ref float horizontalInput, ref float verticalInput)
    {
        if (useJoystickInput && joystick != null)
        {
            float rawH = joystick.Horizontal;
            float rawV = joystick.Vertical;

            const float deadzone = 0.05f;
            if (Mathf.Abs(rawH) < deadzone) rawH = 0;
            if (Mathf.Abs(rawV) < deadzone) rawV = 0;

            horizontalInput = rawH * horizontalInputSensitivity;
            float rawVertical = rawV * verticalInputSensitivity;
            verticalInput = invertJoystickVertical ? -rawVertical : rawVertical;
        }
    }

    void ApplyPlaneHandling()
    {
        if (rb == null) return;

        // Get input
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (useKeyboardInput)
        {
            horizontalInput = Input.GetAxis("Horizontal") * horizontalInputSensitivity;
            verticalInput = Input.GetAxis("Vertical") * verticalInputSensitivity;
        }

        if (useJoystickInput)
        {
            JoystickInput(ref horizontalInput, ref verticalInput);
        }

        // Smooth input
        smoothHorizontalInput = Mathf.Lerp(smoothHorizontalInput, horizontalInput, Time.fixedDeltaTime * inputSmoothness);
        smoothVerticalInput = Mathf.Lerp(smoothVerticalInput, verticalInput, Time.fixedDeltaTime * inputSmoothness);

        horizontalInput = smoothHorizontalInput;
        verticalInput = smoothVerticalInput;

        // Calculate torque
        Vector3 torque = Vector3.zero;
        torque += transform.up * (horizontalInput * turnSpeed * torqueResponseMultiplier);         // Yaw
        torque += transform.forward * (-horizontalInput * bankAngle * torqueResponseMultiplier);   // Roll
        torque += transform.right * (verticalInput * pitchSpeed * torqueResponseMultiplier);       // Pitch

        // Auto-level only when:
        // 1. Auto-leveling is enabled
        // 2. There is no horizontal or vertical input from the player
        // 3. The plane is not being dragged (if disableAutoLevelWhenDragging is true)
        bool hasInput = !Mathf.Approximately(horizontalInput, 0f) || !Mathf.Approximately(verticalInput, 0f);
        bool shouldAutoLevel = autoLevelWhenNoInput && !hasInput && !(disableAutoLevelWhenDragging && isBeingDragged);
        if (shouldAutoLevel)
        {
            Vector3 projectedUp = Vector3.ProjectOnPlane(Vector3.up, transform.forward).normalized;
            float signedAngle = Vector3.SignedAngle(transform.up, projectedUp, transform.forward);
            torque += transform.forward * (signedAngle * autoLevelSpeed * torqueResponseMultiplier);
        }

        // Smooth torque application
        smoothTorque = Vector3.Lerp(smoothTorque, torque, Time.fixedDeltaTime * torqueSmoothness);
        rb.AddTorque(smoothTorque, ForceMode.Acceleration);

        // Align velocity with forward direction
        if (rb.velocity.magnitude > minSpeedForAlignment)
        {
            Vector3 targetVelocity = transform.forward * rb.velocity.magnitude;
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, directionAlignmentStrength * Time.fixedDeltaTime);
        }
    }

    public void ForceControl()
    {
        if (!isControlling)
            StartControlling();
    }

    public void StopControlling()
    {
        isControlling = false;
    }

    private void HandleGroundMovement()
    {
        if (!isGrounded || rb == null) return;

        rb.velocity *= groundDragFactor;

        if (rb.velocity.magnitude < minGroundSpeed)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;

            if (exitedRamp && collisionMarker != null)
            {
                PlaceMarkerAtCurrentPosition();
                exitedRamp = false;
            }

            isGrounded = false;
            Debug.Log("Plane stopped moving on ground");
        }
        else
        {
            AlignToGround();
        }
    }

    private void AlignToGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * groundAlignmentSpeed);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isControlling && collision.gameObject.CompareTag("Ground"))
        {
            StopControlling();

            if (rb != null && rb.velocity.magnitude > 0.1f)
            {
                Vector3 groundNormal = collision.contacts[0].normal;
                Vector3 projectedVelocity = Vector3.ProjectOnPlane(rb.velocity, groundNormal) * 0.7f;
                rb.velocity = projectedVelocity;
                rb.angularVelocity *= 0.5f;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                isGrounded = true;
                Debug.Log("Plane landed and sliding on ground");
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (isGrounded && !isControlling && collision.gameObject.CompareTag("Ground"))
        {
            Vector3 groundNormal = collision.contacts[0].normal;
            if (rb != null && rb.velocity.magnitude > 0.1f)
            {
                rb.velocity = Vector3.ProjectOnPlane(rb.velocity, groundNormal);
                if ((rb.constraints & RigidbodyConstraints.FreezePositionY) == 0)
                    rb.constraints = RigidbodyConstraints.FreezePositionY;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (isGrounded && !isControlling && collision.gameObject.CompareTag("Ground"))
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out _, groundCheckDistance * 2f, LayerMask.GetMask("Ground")))
            {
                rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
                rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
                Debug.Log("Plane left the ground surface");
            }
        }
    }

    private void PlaceMarkerAtCurrentPosition()
    {
        if (collisionMarker == null || collisionMarker.markerPrefab == null)
        {
            Debug.LogWarning("CollisionMarker prefab missing.");
            return;
        }

        Vector3 position = transform.position;
        Vector3 normal = Vector3.up;

        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, groundCheckDistance * 2f))
        {
            position = hit.point;
            normal = hit.normal;
        }

        position += normal * 0.05f;
        GameObject marker = Instantiate(collisionMarker.markerPrefab, position, Quaternion.identity);
        marker.transform.up = normal;
        marker.isStatic = false;

        LandingMarker landingMarker = marker.GetComponent<LandingMarker>();
        if (landingMarker == null && System.Type.GetType("LandingMarker") != null)
            landingMarker = marker.AddComponent<LandingMarker>();

        if (landingMarker != null)
            landingMarker.markerColor = collisionMarker.markerColor;
        else
            Destroy(marker, collisionMarker.markerLifetime);

        Debug.Log($"Marker placed at: {position}");
    }
}
