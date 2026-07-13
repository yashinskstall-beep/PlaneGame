using UnityEngine;

/// <summary>
/// Handles visual spinning of the plane body when both wings are disabled.
/// Joystick/keyboard up tumbles forward; down tumbles backward.
/// </summary>
public class PlaneBodySpinner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the body GameObject that will spin")]
    public GameObject bodyGameObject;
    
    [Tooltip("Reference to the damage handler to check wing status")]
    public PlaneDamageHandler damageHandler;
    
    [Header("Spin Settings")]
    [Tooltip("Max tumble speed in degrees per second (forward or backward).")]
    public float spinSpeed = 180f; // Degrees per second
    
    [Tooltip("How quickly the spin accelerates when input is given")]
    public float spinAcceleration = 360f;
    
    [Tooltip("How quickly the spin decelerates when no input")]
    public float spinDeceleration = 540f;
    
    [Header("Force Settings")]
    [Tooltip("Forward force applied to the plane when spinning starts")]
    public float horizontalForce = 5f;
    
    [Tooltip("Direction of the force (relative to plane's orientation)")]
    public Vector3 forceDirection = Vector3.forward;
    
    [Header("Input Settings")]
    public bool useKeyboardInput = true;
    public bool useJoystickInput = false;
    
    [Tooltip("Reference to joystick controller if using joystick input")]
    public JoystickController joystick;

    [Tooltip("Optional. Used to stop spin input after landing/crash.")]
    public PlaneController planeController;

    [Tooltip("Ignore stick/axis values smaller than this when choosing spin direction.")]
    public float inputDeadzone = 0.1f;
    
    // Internal state — signed: + = forward tumble, - = backward tumble
    private float currentSpinSpeed = 0f;
    private bool isSpinning = false;
    private bool wasSpinning = false;
    private Rigidbody rb;
    
    void Start()
    {
        // Get rigidbody reference
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("PlaneBodySpinner: No Rigidbody found on plane. Horizontal force won't be applied.");
        }
        
        // Try to find references if not set
        if (damageHandler == null)
        {
            damageHandler = GetComponent<PlaneDamageHandler>();
        }

        if (planeController == null)
            planeController = GetComponent<PlaneController>();
        
        if (useJoystickInput && joystick == null)
        {
            joystick = FindObjectOfType<JoystickController>();
        }
        
        // Validate body reference
        if (bodyGameObject == null)
        {
            Debug.LogError("PlaneBodySpinner: Body GameObject reference is missing!");
        }
        
        if (damageHandler == null)
        {
            Debug.LogWarning("PlaneBodySpinner: PlaneDamageHandler reference is missing!");
        }
    }
    
    void Update()
    {
        // Only spin if both wings are missing and body is enabled
        if (ShouldSpin())
        {
            HandleSpinInput();
            ApplySpin();
        }
        else
        {
            // Gradually stop spinning if conditions are no longer met
            currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, 0f, spinDeceleration * Time.deltaTime);
            if (Mathf.Approximately(currentSpinSpeed, 0f))
                isSpinning = false;
        }
    }
    
    /// <summary>
    /// Checks if the body should spin (both wings disabled, body enabled)
    /// </summary>
    private bool ShouldSpin()
    {
        if (bodyGameObject == null || !bodyGameObject.activeInHierarchy)
            return false;

        // After ground collision StopControlling() clears this — do not keep driving the plane.
        if (planeController != null && !planeController.isControlling)
            return false;
            
        if (damageHandler == null)
            return false;
            
        return damageHandler.AreBothWingsMissing();
    }

    /// <summary>
    /// +1 = stick/key up (forward tumble), -1 = stick/key down (backward tumble),
    /// +1 for horizontal-only input, 0 = no input.
    /// </summary>
    private float GetSpinInputDirection()
    {
        float vertical = 0f;
        float horizontal = 0f;

        if (useKeyboardInput)
        {
            vertical += Input.GetAxis("Vertical");
            horizontal += Input.GetAxis("Horizontal");
        }

        if (useJoystickInput && joystick != null)
        {
            vertical += joystick.Vertical;
            horizontal += joystick.Horizontal;
        }

        if (Mathf.Abs(vertical) > inputDeadzone)
            return Mathf.Sign(vertical);

        // Left/right-only: keep previous forward tumble behavior
        if (Mathf.Abs(horizontal) > inputDeadzone)
            return 1f;

        return 0f;
    }
    
    /// <summary>
    /// Handles input for spinning
    /// </summary>
    private void HandleSpinInput()
    {
        float direction = GetSpinInputDirection();
        float targetSpeed = direction * spinSpeed;
        float rate = Mathf.Approximately(direction, 0f) ? spinDeceleration : spinAcceleration;

        currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, targetSpeed, rate * Time.deltaTime);

        bool hasSpin = Mathf.Abs(currentSpinSpeed) > 0.01f;
        if (hasSpin && !wasSpinning)
            ApplyHorizontalForce();

        isSpinning = hasSpin;
        wasSpinning = isSpinning;
    }
    
    /// <summary>
    /// Applies the spin rotation to the body GameObject
    /// </summary>
    private void ApplySpin()
    {
        if (bodyGameObject == null || Mathf.Approximately(currentSpinSpeed, 0f))
            return;
            
        // Signed speed: + = forward tumble, - = backward tumble (around local right)
        bodyGameObject.transform.Rotate(Vector3.right, currentSpinSpeed * Time.deltaTime, Space.Self);
    }
    
    /// <summary>
    /// Applies a small horizontal force to the plane when spinning starts
    /// </summary>
    private void ApplyHorizontalForce()
    {
        if (rb == null || damageHandler == null || damageHandler.IsBodyOnly())
            return;
            
        // Apply force in the plane's forward direction (Z-axis)
        Vector3 force = transform.forward * horizontalForce;
        rb.AddForce(force, ForceMode.Impulse);
        
        Debug.Log($"PlaneBodySpinner: Applied forward force of {horizontalForce} in direction {transform.forward}");
    }
    
    /// <summary>
    /// Public method to manually trigger spin (can be called from other scripts)
    /// </summary>
    public void TriggerSpin()
    {
        if (ShouldSpin())
        {
            currentSpinSpeed = spinSpeed;
            isSpinning = true;
        }
    }
    
    /// <summary>
    /// Public method to stop spin immediately
    /// </summary>
    public void StopSpin()
    {
        currentSpinSpeed = 0f;
        isSpinning = false;
        wasSpinning = false;
    }
    
    /// <summary>
    /// Returns whether the body is currently spinning
    /// </summary>
    public bool IsSpinning()
    {
        return isSpinning && Mathf.Abs(currentSpinSpeed) > 0.01f;
    }
}
