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
    [Tooltip("Aligns the plane to the ramp while rolling. Usually on the same object as the plane.")]
    public PlaneRampAligner rampAligner;
    [Tooltip("Spawns the landing flag prefab when the plane stops. Auto-added if missing.")]
    public CollisionMarker collisionMarker;
    [Tooltip("On-screen joystick for pitch and turn. Enable with Use Joystick Input.")]
    public JoystickController joystick;
    [Tooltip("Left booster particle effect. Plays during boost.")]
    public ParticleSystem boostA;
    [Tooltip("Right booster particle effect. Plays during boost.")]
    public ParticleSystem boostB;
    [Tooltip("Handles wing/tail damage effects on flight. Should be on the plane root.")]
    public PlaneDamageHandler damageHandler;
    [Tooltip("Camera follow script. Used to freeze camera on landing and move to the flag.")]
    public SimpleCameraFollow cameraFollow;
    [Tooltip("Plays marker, boost, and button sounds.")]
    public AudioManager audioManager;
    [Tooltip("Wind loop during flight. Stops when the plane lands.")]
    public AudioSource windSource;
    //public GameObject boostBtn;
   // public CameraManager cameraManager;

    [Header("Handling Settings")]
    [Tooltip("How fast the plane yaws left/right when you steer. Higher = snappier turns.")]
    public float turnSpeed = 8f;
    [Tooltip("How far the plane banks (rolls) when turning. Higher = more visible bank angle.")]
    public float bankAngle = 45f;
    [Tooltip("How fast the plane pitches up/down. Higher = more responsive nose control.")]
    public float pitchSpeed = 7f;
    [Tooltip("Global multiplier on all rotation torque. Raise for twitchier flight; lower for softer control.")]
    public float torqueResponseMultiplier = 2.5f;
    [Tooltip("Rigidbody angular drag. Higher = less spin/wobble after collisions or damage.")]
    public float angularDragAmount = 0.3f;

    [Header("Input Settings")]
    [Tooltip("Allow WASD / arrow keys to control the plane in flight.")]
    public bool useKeyboardInput = true;
    [Tooltip("Allow the on-screen joystick to control the plane in flight.")]
    public bool useJoystickInput = false;
    [Tooltip("Multiplier on horizontal (turn) input. Use if turns feel too weak or too strong.")]
    public float horizontalInputSensitivity = 1f;
    [Tooltip("Multiplier on vertical (pitch) input. Use if climb/dive feels too weak or too strong.")]
    public float verticalInputSensitivity = 1f;
    [Tooltip("Flip joystick up/down if pitch feels reversed on your device.")]
    public bool invertJoystickVertical = true;
    [Tooltip("Gently levels the wings when you release the controls.")]
    public bool autoLevelWhenNoInput = true;
    [Tooltip("How strongly the plane auto-levels. Higher = faster return to level flight.")]
    public float autoLevelSpeed = 1f;
    [Tooltip("Disable auto-level while the slingshot is still dragging the plane.")]
    public bool disableAutoLevelWhenDragging = true;

    [Header("Movement Alignment")]
    [Tooltip("How strongly velocity snaps to the plane nose direction. Higher = flies where it points.")]
    public float directionAlignmentStrength = 5.0f;
    [Tooltip("Speed below which nose-alignment is ignored. Prevents jitter at very low speed.")]
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
    [Tooltip("Friction while sliding on the ground after landing (0.98 = loses 2% speed per physics step).")]
    public float groundDragFactor = 0.98f;
    [Tooltip("Speed at which the plane is considered fully stopped on the ground.")]
    public float minGroundSpeed = 0.1f;
    [Tooltip("Forward slide speed threshold for placing the landing flag. Lower = flag appears sooner.")]
    public float minZAxisSpeed = 0.05f;
    [Tooltip("How fast the plane uprights to match ground slope while sliding.")]
    public float groundAlignmentSpeed = 5.0f;
    [Tooltip("Raycast distance to detect ground under the plane for alignment and markers.")]
    public float groundCheckDistance = 0.5f;
    [Tooltip("Collision force needed to detach parts (wings/tail). Lower = parts break on lighter crashes.")]
    public float minImpactForceForDamage = 10f;

    [Header("Damage Fall Settings")]
    [Tooltip("The downward force applied when both wings are disabled.")]
    public float fallDownForce = 0f; // Default value decreased from 20f

    [Header("Marker Settings")]
    [Tooltip("Height above the ground where the landing flag is spawned.")]
    public float markerYOffset = 0.5f;

    [Header("Smoothing Settings")]
    [Tooltip("Input smoothing. Higher = softer stick/keyboard response, less twitchy.")]
    public float inputSmoothness = 25f;
    [Tooltip("Rotation smoothing. Higher = gentler pitch/roll changes, less jerky.")]
    public float torqueSmoothness = 18f;
    [Tooltip("Visual rotation smoothing to hide wobbling. Higher = smoother but less responsive.")]
    public float visualRotationSmoothing = 8f;

    [Header("Boost Settings")]
    [Tooltip("Speed increase as a fraction of current speed (0.3 = +30%).")]
    public float boostSpeedMultiplier = 0.3f;
    [Tooltip("How long the boosted speed is maintained.")]
    public float boostDuration = 2f;
    [Tooltip("How many boost button presses per flight.")]
    public int maxBoostUses = 2;
    [Tooltip("UI manager for boost counter and score screen.")]
    public UIManager uiManager;

    // Internal state
    private Rigidbody rb;
    [Tooltip("True when player can steer the plane (after leaving the ramp). Read-only at runtime.")]
    public bool isControlling = false;
    private bool wasOnRamp = false;
    private bool exitedRamp = false;
    private bool isGrounded = false;
    private bool isBeingDragged = false;
    private bool isBoosting = false;
    [Tooltip("Boost uses left this flight. Reset from Max Boost Uses at start.")]
    public int boostUsesRemaining = 0;
    
    // Momentum tracking
    private float storedMomentum = 0f;
    private float maxRecentSpeed = 0f;
    private bool wasDiving = false;

    private float boostTargetSpeed;
    private Vector3 boostVelocityDirection;
    private float smoothHorizontalInput = 0f;
    private float smoothVerticalInput = 0f;
    private Vector3 smoothTorque = Vector3.zero;
    private PlanePartDetach[] detachableParts;
    
    // Visual smoothing
    private Quaternion smoothedRotation;
    private GameObject visualModel;

    // Distance / marker tracking
    private Vector3 startPosition;
    private Vector3 maxZPosition;
    [Tooltip("Furthest distance flown this run (Z axis from launch). Used for score and flag position.")]
    public float maxZDistance;
    private bool markerPlaced = false;
    private GameObject placedMarker = null;
    private float lastZPosition;
    private float lastRampZPosition;
    private float timeStoppedOnRamp = 0f;
    private const float rampStopThreshold = 1f; // Time in seconds before placing marker

    void Start()
    {

       
        rb = GetComponent<Rigidbody>();
        boostUsesRemaining = maxBoostUses;
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
        
        // Initialize visual smoothing
        smoothedRotation = transform.rotation;
        
        // Try to find a visual model child (optional - for separating physics from visuals)
        // If you have a child object with the plane mesh, assign it here
        // For now, we'll smooth the main transform
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
    
    void LateUpdate()
    {
        // Apply visual smoothing to hide wobbling
        // This runs after physics, smoothing out the visual rotation
        if (isControlling)
        {
            // Smoothly interpolate the visual rotation
            smoothedRotation = Quaternion.Slerp(smoothedRotation, transform.rotation, Time.deltaTime * visualRotationSmoothing);
            
            // Apply the smoothed rotation back to the transform
            // Note: This creates a slight visual delay but hides wobbling
            transform.rotation = smoothedRotation;
        }
        else
        {
            // When not controlling, keep smoothed rotation in sync
            smoothedRotation = transform.rotation;
        }
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
    
        Debug.Log("PlaneController.StartControlling() - isControlling set to TRUE");
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
        // 4. The plane is not upside down or at extreme roll angles (manual control in those cases)
        // 5. The plane is not on a ramp (ramp aligner handles rotation)
        bool hasInput = !Mathf.Approximately(horizontalInput, 0f) || !Mathf.Approximately(verticalInput, 0f);
        
        // Check if plane is on ramp
        bool isOnRamp = false;
        if (rampAligner != null)
        {
            var field = typeof(PlaneRampAligner).GetField("isAligning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                isOnRamp = (bool)field.GetValue(rampAligner);
        }
        
        // Check if plane is upside down or at extreme angles (roll > 90 degrees)
        float rollAngle = transform.localEulerAngles.z;
        if (rollAngle > 180f) rollAngle -= 360f; // Normalize to -180 to 180
        bool isUpsideDownOrExtreme = Mathf.Abs(rollAngle) > 90f;
        
        bool shouldAutoLevel = autoLevelWhenNoInput && !hasInput && !(disableAutoLevelWhenDragging && isBeingDragged) && !isUpsideDownOrExtreme && !isOnRamp;
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
            // Calculate pitch angle from X axis rotation (local euler angles)
            float pitchAngle = transform.localEulerAngles.x;
            // Normalize to -180 to 180 range
            if (pitchAngle > 180f) pitchAngle -= 360f;
            
            // If plane is pitched up steeply (stalling), reduce forward alignment and allow natural falling
            if (pitchAngle < -35f) // Pitched up more than 35 degrees
            {
                // Gradual stall: 35° = 0%, 40° = 50%, 45° = 100%
                float stallFactor = Mathf.InverseLerp(-35f, -45f, pitchAngle); // 0 at -35°, 1 at -45°
                
                // During stall, reduce the alignment strength instead of forcing downward
                // This lets gravity naturally pull the plane down while maintaining horizontal momentum
                float reducedAlignmentStrength = directionAlignmentStrength * (1f - stallFactor * 0.9f);
                
                Vector3 targetVelocity = transform.forward * rb.velocity.magnitude;
                rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, reducedAlignmentStrength * Time.fixedDeltaTime);
                
                // Debug log for stall behavior
                Debug.Log($"STALLING: Pitch = {pitchAngle:F1}°, StallFactor = {stallFactor:F2}, AlignmentStrength = {reducedAlignmentStrength:F2}, Speed = {rb.velocity.magnitude:F1}");
            }
            else
            {
                // Normal alignment when not stalling
                Vector3 targetVelocity = transform.forward * rb.velocity.magnitude;
                rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, directionAlignmentStrength * Time.fixedDeltaTime);
            }
        }

        ApplyBoostSpeedMaintenance();
    }

    private void ApplyBoostSpeedMaintenance()
    {
        if (!isBoosting || rb == null || boostTargetSpeed <= 0f)
            return;

        Vector3 direction = rb.velocity.sqrMagnitude > 0.01f
            ? rb.velocity.normalized
            : boostVelocityDirection;

        rb.velocity = direction * boostTargetSpeed;
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

        if (zVelocity < minZAxisSpeed && !markerPlaced)
        {
            if (collisionMarker != null)
            {
                Debug.Log($"Marker Check: zVelocity={zVelocity:F2}, markerPlaced={markerPlaced}");
                PlaceMarkerAtCurrentPosition();
                markerPlaced = true;
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
        if (collision.gameObject.CompareTag("Ground"))
        {
            // Handle ground collision regardless of isControlling state
            if (isControlling)
            {
                windSource.Stop();
                cameraFollow?.FreezePosition();
                joystick?.joystickBG?.gameObject.SetActive(false);
            }
            
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce >= minImpactForceForDamage && isControlling)
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

            if (isControlling)
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
            else
            {
                // Even if velocity is low, set isGrounded so marker can be placed
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
        VibrationManager.Instance.VibrateButtonClick();
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
        VibrationManager.Instance.VibrateButtonClick();
        
        if (!isBoosting && rb != null && boostUsesRemaining > 0)
        {
            float currentSpeed = rb.velocity.magnitude;
            boostVelocityDirection = currentSpeed > 0.1f
                ? rb.velocity.normalized
                : transform.forward;
            boostTargetSpeed = currentSpeed * (1f + boostSpeedMultiplier);

            rb.velocity = boostVelocityDirection * boostTargetSpeed;
            isBoosting = true;

            boostA?.Play();
            boostB?.Play();
            audioManager.BoostSFX();

            boostUsesRemaining--;
            Debug.Log($"Boost active: {currentSpeed:F1} -> {boostTargetSpeed:F1} for {boostDuration:F1}s. Uses left: {boostUsesRemaining}");

            if (uiManager != null)
                uiManager.UpdateBoostCounter();

            StartCoroutine(BoostDurationRoutine());
        }
        else if (boostUsesRemaining <= 0)
        {
            //boostBtn.SetActive(false);
            Debug.Log("No boost uses remaining!");
            
        }
    }

    private IEnumerator BoostDurationRoutine()
    {
        yield return new WaitForSeconds(boostDuration);

        isBoosting = false;
        boostTargetSpeed = 0f;
        boostA?.Stop();
        boostB?.Stop();

        if (boostUsesRemaining <= 0)
        {
            Debug.Log("Boost uses depleted! Deactivating boosters.");
            if (boostA != null) boostA.gameObject.SetActive(false);
            if (boostB != null) boostB.gameObject.SetActive(false);
        }
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