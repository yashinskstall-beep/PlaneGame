
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Complete PlaneController:
/// Handles full airborne control, auto-leveling, gliding slowdown, boost effects,
/// ground alignment, marker placement, and detachable damage parts.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("References")]
    public PlaneRampAligner rampAligner;
    public CollisionMarker collisionMarker;
    public JoystickController joystick;
    public ParticleSystem boostA;
    public ParticleSystem boostB;
    public PlaneDamageHandler damageHandler;
    public SimpleCameraFollow cameraFollow;
    public AudioManager audioManager;
   // public CameraManager cameraManager;

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
    public bool disableAutoLevelWhenDragging = true;

    [Header("Movement Alignment")]
    public float directionAlignmentStrength = 5.0f;
    public float minSpeedForAlignment = 2.0f;

    [Header("Speed Control (Glide Behaviour)")]
    [Tooltip("Aerodynamic drag applied when gliding or climbing. Higher values cause more slowdown.")]
    public float glideDrag = 0.8f;
    [Tooltip("Aerodynamic drag applied when diving. Lower values allow for more acceleration.")]
    public float diveDrag = 0.1f;
    
    [Header("Air Resistance Settings")]
    [Tooltip("Base air resistance coefficient. Higher values cause more slowdown.")]
    public float airResistanceCoefficient = 0.05f;
    [Tooltip("How much the air resistance increases with speed (quadratic). Higher values make faster speeds have more resistance.")]
    public float velocityResistanceFactor = 0.01f;
    [Tooltip("How much the plane's orientation affects air resistance. Higher values mean more resistance when flying sideways.")]
    public float orientationResistanceFactor = 0.5f;
    
    [Header("Momentum Settings")]
    [Tooltip("How efficiently the plane converts diving speed to climbing ability. Higher values allow for better climbing.")]
    public float momentumConversionFactor = 0.8f;
    [Tooltip("Minimum speed required to start climbing effectively.")]
    public float minSpeedForClimbing = 5f;
    [Tooltip("How quickly the plane loses momentum when climbing. Lower values allow for longer climbs.")]
    public float momentumDecayRate = 0.2f;

    [Header("Ground Movement Settings")]
    public float groundDragFactor = 0.98f;
    public float minGroundSpeed = 0.1f;
    public float minZAxisSpeed = 0.05f;
    public float groundAlignmentSpeed = 5.0f;
    public float groundCheckDistance = 0.5f;
    public float minImpactForceForDamage = 10f;

    [Header("Damage Fall Settings")]
    [Tooltip("The downward force applied when both wings are disabled.")]
    public float fallDownForce = 0f; // Default value decreased from 20f

    [Header("Marker Settings")]
    public float markerYOffset = 0.5f;

    [Header("Smoothing Settings")]
    public float inputSmoothness = 4f;
    public float torqueSmoothness = 3f;

    [Header("Boost Settings")]
    public float boostAmount = 10f;
    public float boostDuration = 1.5f;
    public float returnToNormalSpeed = 2f;
    public UIManager uiManager;

    // Internal state
    private Rigidbody rb;
    public  bool isControlling = false;
    private bool wasOnRamp = false;
    private bool exitedRamp = false;
    private bool isGrounded = false;
    private bool isBeingDragged = false;
    private bool isBoosting = false;
    
    // Momentum tracking
    private float storedMomentum = 0f;
    private float maxRecentSpeed = 0f;
    private bool wasDiving = false;

    private Vector3 preBoostVelocity;
    private float smoothHorizontalInput = 0f;
    private float smoothVerticalInput = 0f;
    private Vector3 smoothTorque = Vector3.zero;
    private PlanePartDetach[] detachableParts;

    // Distance / marker tracking
    private Vector3 startPosition;
    private Vector3 maxZPosition;
    public  float maxZDistance;
    private bool markerPlaced = false;
    private GameObject placedMarker = null;
    private float lastZPosition;
    private float lastRampZPosition;
    private float timeStoppedOnRamp = 0f;
    private const float rampStopThreshold = 1f; // Time in seconds before placing marker

    void Start()
    {

       
        rb = GetComponent<Rigidbody>();
        //uiManager.btnAudio.Stop();

        if (rb != null)
        {
            rb.angularDrag = angularDragAmount;
        }

        // Make sure we have a damage handler reference
        damageHandler ??= GetComponent<PlaneDamageHandler>();
        if (damageHandler == null)
        {
            Debug.LogWarning("No PlaneDamageHandler found. Wing damage effects won't work.");
        }
        collisionMarker ??= GetComponent<CollisionMarker>() ?? gameObject.AddComponent<CollisionMarker>();
        rampAligner ??= GetComponent<PlaneRampAligner>() ?? FindObjectOfType<PlaneRampAligner>();
        cameraFollow ??= FindObjectOfType<SimpleCameraFollow>();

        if (cameraFollow == null)
        {
            Debug.LogWarning("SimpleCameraFollow component not found in the scene. Camera transitions won't work.");
        }

        if (useJoystickInput)
        {
            joystick ??= FindObjectOfType<JoystickController>();
            if (joystick != null)
                joystick.gameObject.SetActive(false);
        }

        // Initialize starting position (resting position on ramp)
        startPosition = transform.position;
        maxZDistance = 0f; // Start at 0 to measure distance traveled from resting position
        lastZPosition = transform.position.z;
        lastRampZPosition = transform.position.z;
    }

    public void InitializeDetachableParts()
    {
        detachableParts = GetComponentsInChildren<PlanePartDetach>();
        Debug.Log($"Initialized {detachableParts.Length} detachable parts.");
    }

    void FixedUpdate()
    {
        // Always update the max distance regardless of state
        // Calculate distance from the original resting position
        float currentDistance = transform.position.z - startPosition.z;
        if (currentDistance > maxZDistance)
        {
            maxZDistance = currentDistance;
            maxZPosition = transform.position;
        }
        
        bool isOnRamp = false;
        if (rampAligner != null)
        {
            var field = typeof(PlaneRampAligner).GetField("isAligning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                isOnRamp = (bool)field.GetValue(rampAligner);
        }

        CheckIfBeingDragged();

        if (wasOnRamp && !isOnRamp)
            StartControlling();

        // Check if plane stopped on ramp
        if (isOnRamp && !isBeingDragged)
            CheckIfStoppedOnRamp();

        wasOnRamp = isOnRamp;
        
        // Check for wing damage during flight
        if (isControlling && damageHandler != null)
        {
            if (damageHandler.AreBothWingsMissing())
            {
                Debug.Log("FixedUpdate: Both wings are missing, making plane fall");
                //FallWithoutWings();
                return;
            }
        }

        if (isControlling)
            ApplyPlaneHandling();
        else if (isGrounded)
            HandleGroundMovement();
    }

    void CheckIfBeingDragged()
    {
        var dragLauncher = GetComponent<SimpleDragLauncher>() ?? FindObjectOfType<SimpleDragLauncher>();
        isBeingDragged = dragLauncher != null && !dragLauncher.released;
    }

    void StartControlling()
    {
        // Always start controlling first to ensure the plane leaves the ramp
        isControlling = true;
        exitedRamp = true;
    
        Debug.Log("StartControlling");
        if (useJoystickInput && joystick != null)
            joystick.gameObject.SetActive(true);

        // Ensure gravity is on and drag is reset at the start of control
        if(rb != null) 
        {
            rb.useGravity = true;
            rb.drag = glideDrag;
        }
        
        // Start a delayed check for wing damage to ensure the plane gets off the ramp first
        StartCoroutine(CheckWingDamageAfterDelay());

        collisionMarker?.ResetCollisionState();

        // Don't reset startPosition - keep the original resting position
        maxZPosition = transform.position;
        // Don't reset maxZDistance - it accumulates from the resting position
        timeStoppedOnRamp = 0f;
        markerPlaced = false;
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

        // Apply damage effects to torque if damage handler exists
        if (damageHandler != null)
        {
            torque = damageHandler.ModifyTorqueForDamage(torque, horizontalInput, verticalInput);
        }
        
        // Smooth torque application
        smoothTorque = Vector3.Lerp(smoothTorque, torque, Time.fixedDeltaTime * torqueSmoothness);
        rb.AddTorque(smoothTorque, ForceMode.Acceleration);

        // Apply air resistance
        ApplyAirResistance();

        // Align velocity with forward direction
        if (rb.velocity.magnitude > minSpeedForAlignment)
        {
            Vector3 targetVelocity = transform.forward * rb.velocity.magnitude;
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, directionAlignmentStrength * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Smooth glide-style speed decay (natural slowdown over time)
    /// </summary>
    private void ApplyAirResistance()
    {
        if (rb == null || rb.velocity.magnitude < 0.1f) return;

        // Calculate base air resistance
        float speed = rb.velocity.magnitude;
        
        // Track the plane's orientation (pitch)
        float pitchAngle = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(transform.forward, Vector3.right),
            Vector3.ProjectOnPlane(Vector3.forward, Vector3.right),
            Vector3.right);
        
        // Detect if we're diving (positive pitch = nose down)
        bool isDiving = pitchAngle > 5f;
        
        // Store momentum when diving
        if (isDiving)
        {
            // Track maximum speed during dive
            maxRecentSpeed = Mathf.Max(maxRecentSpeed, speed);
            wasDiving = true;
            
            // Use less drag when diving to build up speed
            rb.drag = diveDrag;
        }
        else
        {
            // If we were diving but now pulling up, convert speed to momentum
            if (wasDiving && pitchAngle < -2f) // Transitioning from dive to climb
            {
                // Convert speed to stored momentum
                storedMomentum = maxRecentSpeed * momentumConversionFactor;
                wasDiving = false;
                
                // Debug.Log($"Converted dive speed {maxRecentSpeed:F1} to momentum {storedMomentum:F1}");
                maxRecentSpeed = 0f;
            }
            
            // Use normal drag when not diving
            rb.drag = glideDrag;
        }
        
        // Calculate air resistance
        float velocityResistance = speed * speed * velocityResistanceFactor;
        float dotProduct = Vector3.Dot(transform.forward.normalized, rb.velocity.normalized);
        float alignmentFactor = Mathf.Clamp01(dotProduct);
        float orientationResistance = (1f - alignmentFactor) * orientationResistanceFactor;
        float totalResistance = airResistanceCoefficient + velocityResistance + orientationResistance;
        
        // Apply resistance force
        Vector3 resistanceForce = -rb.velocity.normalized * totalResistance;
        rb.AddForce(resistanceForce, ForceMode.Acceleration);
        
        // Apply momentum-based climbing force when climbing
        ApplyMomentumClimbing(pitchAngle, speed);
    }
    
    private void ApplyMomentumClimbing(float pitchAngle, float currentSpeed)
    {
        // Only apply climbing force if we have stored momentum and are trying to climb
        if (storedMomentum > 0 && pitchAngle < -5f) // Negative pitch = nose up
        {
            // Calculate climbing force based on stored momentum
            float climbForce = storedMomentum * 0.8f;
            
            // Apply force in the forward-up direction
            Vector3 climbDirection = (transform.forward + Vector3.up).normalized;
            rb.AddForce(climbDirection * climbForce, ForceMode.Acceleration);
            
            // Gradually reduce stored momentum
            storedMomentum = Mathf.Max(0, storedMomentum - (momentumDecayRate * 0.5f * Time.fixedDeltaTime * (1 + Mathf.Abs(pitchAngle) * 0.05f)));

            
            // Debug.Log($"Climbing with momentum: {storedMomentum:F1}, Force: {climbForce:F1}");
        }
    }
   

    private void HandleGroundMovement()
    {
        if (!isGrounded || rb == null) return;

        float currentZPosition = transform.position.z;
        float zVelocity = (currentZPosition - lastZPosition) / Time.fixedDeltaTime;
        lastZPosition = currentZPosition;

        Debug.Log($"Marker Check: zVelocity={zVelocity:F2}, markerPlaced={markerPlaced}, exitedRamp={exitedRamp}");

        if (zVelocity < minZAxisSpeed && !markerPlaced && exitedRamp)
        {
            if (collisionMarker != null)
            {
                PlaceMarkerAtCurrentPosition();
                markerPlaced = true;
                exitedRamp = false;
            }
        }

        rb.velocity *= groundDragFactor;

        if (rb.velocity.magnitude < minGroundSpeed)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
            isGrounded = false;
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
            cameraFollow?.FreezePosition();
            joystick?.joystickBG?.gameObject.SetActive(false);
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce >= minImpactForceForDamage)
            {
                HashSet<PlanePartDetach> partsToNotify = new HashSet<PlanePartDetach>();

                foreach (ContactPoint contact in collision.contacts)
                {
                    PlanePartDetach closestPart = null;
                    float minDistance = float.MaxValue;

                    foreach (var part in detachableParts)
                    {
                        if (part == null) continue;

                        Collider partCollider = part.GetComponent<Collider>();
                        if (partCollider == null) continue;

                        Vector3 closestPointOnCollider = partCollider.ClosestPoint(contact.point);
                        float distance = Vector3.Distance(closestPointOnCollider, contact.point);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestPart = part;
                        }
                    }

                    if (closestPart != null)
                        partsToNotify.Add(closestPart);
                }

                foreach (var part in partsToNotify)
                    part.HandleCollision(collision);

                StopControlling();
                isGrounded = true;
                return;
            }

            StopControlling();

            if (rb != null && rb.velocity.magnitude > 0.1f)
            {
                Vector3 groundNormal = collision.contacts[0].normal;
                Vector3 projectedVelocity = Vector3.ProjectOnPlane(rb.velocity, groundNormal) * 0.7f;
                rb.velocity = projectedVelocity;
                rb.angularVelocity *= 0.5f;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                isGrounded = true;
                lastZPosition = transform.position.z;
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
            }
        }
    }

    private void CheckIfStoppedOnRamp()
    {
        if (markerPlaced || rb == null) return;

        float currentZPosition = transform.position.z;
        float zVelocity = Mathf.Abs((currentZPosition - lastRampZPosition) / Time.fixedDeltaTime);
        lastRampZPosition = currentZPosition;

        // If plane is barely moving on Z-axis
        if (zVelocity < minZAxisSpeed)
        {
            timeStoppedOnRamp += Time.fixedDeltaTime;
            Debug.Log($"Ramp Stop Check: zVelocity={zVelocity:F2}, timeStoppedOnRamp={timeStoppedOnRamp:F2}");

            // If stopped for threshold duration, place marker
            if (timeStoppedOnRamp >= rampStopThreshold)
            {
                if (collisionMarker != null)
                {
                    Debug.Log("Plane stopped on ramp - placing marker");
                    PlaceMarkerAtCurrentPosition();
                    markerPlaced = true;
                }
            }
        }
        else
        {
            // Reset timer if plane is moving
            timeStoppedOnRamp = 0f;
        }
    }

    private void PlaceMarkerAtCurrentPosition()
    {
        if (collisionMarker == null || collisionMarker.markerPrefab == null)
            return;

        Vector3 raycastStart = maxZPosition + Vector3.up * 1.0f;
        Vector3 markerPosition = maxZPosition;
        Quaternion markerRotation = Quaternion.identity;
        Vector3 groundNormal = Vector3.up;

        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, groundCheckDistance * 4f))
        {
            groundNormal = hit.normal;
            markerPosition = hit.point + groundNormal * markerYOffset;
            markerRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
        }
        else
        {
            markerPosition = transform.position + Vector3.up * markerYOffset;
        }

        GameObject marker = Instantiate(collisionMarker.markerPrefab, markerPosition, markerRotation);
        audioManager.MarkerSFX();
        placedMarker = marker;
        marker.isStatic = false;

        LandingMarker landingMarker = marker.GetComponent<LandingMarker>();
        if (landingMarker == null && System.Type.GetType("LandingMarker") != null)
            landingMarker = marker.AddComponent<LandingMarker>();

        if (landingMarker != null)
            landingMarker.markerColor = collisionMarker.markerColor;
        else
            Destroy(marker, collisionMarker.markerLifetime);

        // Transition the camera to focus on the marker
        if (cameraFollow != null)
        {
            Debug.Log("Transitioning camera to marker");
            cameraFollow.TransitionToMarker(marker.transform);
        }
        else
        {
            Debug.LogWarning("Camera follow reference is missing. Cannot transition to marker.");
        }
    }

    public void BoostButton()
    {
        //uiManager.btnAudio.Play();
        audioManager.btnSFX();
        if (!isBoosting && rb != null)
        {
            preBoostVelocity = rb.velocity;
            rb.AddForce(transform.forward * boostAmount, ForceMode.Impulse);
            boostA?.Play();
            boostB?.Play();
            StartCoroutine(ReturnToNormalSpeed());
        }
    }

    private IEnumerator ReturnToNormalSpeed()
    {
        isBoosting = true;
        yield return new WaitForSeconds(boostDuration);

        float elapsedTime = 0f;
        Vector3 currentVelocity = rb.velocity;
        float returnDuration = 1f / returnToNormalSpeed;

        while (elapsedTime < returnDuration)
        {
            rb.velocity = Vector3.Lerp(currentVelocity, preBoostVelocity, elapsedTime / returnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.velocity = preBoostVelocity;
        isBoosting = false;
        boostA?.Stop();
        boostB?.Stop();
    }

    public IEnumerator DetachAllParts()
    {
        yield return new WaitForEndOfFrame();

        foreach (var part in detachableParts)
        {
            if (part != null && !part.IsDetached)
                part.Detach(part.transform.position);
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
        joystick?.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Coroutine that checks for wing damage after a short delay to ensure the plane has left the ramp
    /// </summary>
    private IEnumerator CheckWingDamageAfterDelay()
    {
        // Wait for a short time to ensure the plane is off the ramp
        yield return new WaitForSeconds(0.5f);
        
        // Now check if both wings are missing
        if (damageHandler != null && damageHandler.AreBothWingsMissing() && isControlling)
        {
            Debug.Log("Delayed check: Both wings are disabled. Making plane fall.");
            //FallWithoutWings();
        }
    }
    
    /// <summary>
    /// Makes the plane fall straight down when both wings are missing
    /// </summary>
    // private void FallWithoutWings()
    // {
    //     isControlling = false;
    //     exitedRamp = true;

    //     if (rb != null)
    //     {
    //         rb.useGravity = true;
    //         rb.drag = 0.1f; // Minimal drag
    //         rb.angularDrag = 0.05f; // Minimal angular drag

    //         // Apply a strong downward force to simulate falling
    //         rb.AddForce(Vector3.down * fallDownForce, ForceMode.Impulse);
            
    //         // Add some random rotation to make it look more realistic
    //         rb.AddTorque(new Vector3(
    //             Random.Range(-1f, 1f),
    //             Random.Range(-1f, 1f),
    //             Random.Range(-1f, 1f)
    //         ) * 5f, ForceMode.Impulse);
    //     }

    //     Debug.Log("Both wings are missing. The plane is falling without control.");
        
    //     // Disable joystick if it's active
    //     joystick?.gameObject.SetActive(false);
    // }
}
