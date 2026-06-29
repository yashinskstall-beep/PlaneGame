using UnityEngine;

/// <summary>
/// Handles visual spinning of the plane body when both wings are disabled.
/// This script rotates the body GameObject forward based on user input.
/// </summary>
public class PlaneBodySpinner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the body GameObject that will spin")]
    public GameObject bodyGameObject;
    
    [Tooltip("Reference to the damage handler to check wing status")]
    public PlaneDamageHandler damageHandler;
    
    [Header("Spin Settings")]
    [Tooltip("Speed of the forward spin rotation")]
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
    
    // Internal state
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
            if (currentSpinSpeed > 0)
            {
                currentSpinSpeed = Mathf.Max(0, currentSpinSpeed - spinDeceleration * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Checks if the body should spin (both wings disabled, body enabled)
    /// </summary>
    private bool ShouldSpin()
    {
        if (bodyGameObject == null || !bodyGameObject.activeInHierarchy)
            return false;
            
        if (damageHandler == null)
            return false;
            
        return damageHandler.AreBothWingsMissing();
    }
    
    /// <summary>
    /// Handles input for spinning
    /// </summary>
    private void HandleSpinInput()
    {
        bool hasInput = false;
        
        // Check keyboard input
        if (useKeyboardInput)
        {
            float verticalInput = Input.GetAxis("Vertical");
            float horizontalInput = Input.GetAxis("Horizontal");
            
            if (Mathf.Abs(verticalInput) > 0.1f || Mathf.Abs(horizontalInput) > 0.1f)
            {
                hasInput = true;
            }
        }
        
        // Check joystick input
        if (useJoystickInput && joystick != null)
        {
            float verticalInput = joystick.Vertical;
            float horizontalInput = joystick.Horizontal;
            
            if (Mathf.Abs(verticalInput) > 0.1f || Mathf.Abs(horizontalInput) > 0.1f)
            {
                hasInput = true;
            }
        }
        
        // Accelerate or decelerate spin based on input
        if (hasInput)
        {
            currentSpinSpeed = Mathf.Min(spinSpeed, currentSpinSpeed + spinAcceleration * Time.deltaTime);
            
            // Check if spin just started (transition from not spinning to spinning)
            if (!wasSpinning && currentSpinSpeed > 0)
            {
                ApplyHorizontalForce();
            }
            
            isSpinning = true;
        }
        else
        {
            currentSpinSpeed = Mathf.Max(0, currentSpinSpeed - spinDeceleration * Time.deltaTime);
            if (currentSpinSpeed <= 0)
            {
                isSpinning = false;
            }
        }
        
        // Update previous spinning state
        wasSpinning = isSpinning;
    }
    
    /// <summary>
    /// Applies the spin rotation to the body GameObject
    /// </summary>
    private void ApplySpin()
    {
        if (bodyGameObject == null || currentSpinSpeed <= 0)
            return;
            
        // Rotate the body forward (around its right axis for forward tumbling)
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
    }
    
    /// <summary>
    /// Returns whether the body is currently spinning
    /// </summary>
    public bool IsSpinning()
    {
        return isSpinning && currentSpinSpeed > 0;
    }
}
