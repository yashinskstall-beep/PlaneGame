using UnityEngine;

/// <summary>
/// Handles the plane's behavior when parts are disabled.
/// This script should be attached to the main Airplane GameObject.
/// </summary>
public class PlaneDamageHandler : MonoBehaviour
{
    [Header("Part References")]
    public GameObject leftWing;
    public GameObject rightWing;
    public GameObject tail;
    
    [Header("Damage Effect Settings")]
    [Tooltip("How much faster the plane will roll when a wing is disabled")]
    public float wingDamageRollMultiplier = 2.0f;
    
    [Tooltip("How much faster the plane will pitch down when the tail is disabled")]
    public float tailDamagePitchMultiplier = 2.0f;
    
    [Tooltip("Additional drag applied when parts are missing")]
    public float additionalDragPerMissingPart = 0.5f;
    
    [Tooltip("How much the plane tilts to the side when a wing is missing")]
    public float wingTiltStrength = 10.0f;
    
    // Reference to the plane controller
    private PlaneController planeController;
    
    // Store original values to restore if parts are re-enabled
    private float originalTurnSpeed;
    private float originalBankAngle;
    private float originalPitchSpeed;
    private float originalDrag;
    
    void Start()
    {
        // Get the plane controller reference
        planeController = GetComponent<PlaneController>();
        
        if (planeController == null)
        {
            Debug.LogError("PlaneDamageHandler: No PlaneController component found!");
            return;
        }
        
        // Store original values
        originalTurnSpeed = planeController.turnSpeed;
        originalBankAngle = planeController.bankAngle;
        originalPitchSpeed = planeController.pitchSpeed;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            originalDrag = rb.drag;
        }
    }
    
    void Update()
    {
        // Check if any parts are disabled and apply effects
        CheckPartsStatus();
    }
    
    void CheckPartsStatus()
    {
        if (planeController == null) return;
        
        bool leftWingDisabled = leftWing != null && !leftWing.activeSelf;
        bool rightWingDisabled = rightWing != null && !rightWing.activeSelf;
        bool tailDisabled = tail != null && !tail.activeSelf;
        
        // Reset to original values first
        planeController.turnSpeed = originalTurnSpeed;
        planeController.bankAngle = originalBankAngle;
        planeController.pitchSpeed = originalPitchSpeed;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.drag = originalDrag;
        }
        
        // Count disabled parts for drag calculation
        int disabledPartCount = 0;
        
        // Apply left wing damage effects
        if (leftWingDisabled)
        {
            // When left wing is disabled, turning left (negative input) will cause faster roll
            disabledPartCount++;
            
            // We'll handle tilting in ModifyTorqueForDamage instead of applying torque directly here
            // This prevents competing forces that can cause shaking
        }
        
        // Apply right wing damage effects
        if (rightWingDisabled)
        {
            // When right wing is disabled, turning right (positive input) will cause faster roll
            disabledPartCount++;
            
            // We'll handle tilting in ModifyTorqueForDamage instead of applying torque directly here
            // This prevents competing forces that can cause shaking
        }
        
        // Apply tail damage effects
        if (tailDisabled)
        {
            // When tail is disabled, pitching will be more unstable and tend to pitch down
            planeController.pitchSpeed *= tailDamagePitchMultiplier;
            disabledPartCount++;
        }
        
        // Apply additional drag based on missing parts
        if (rb != null && disabledPartCount > 0)
        {
            rb.drag = originalDrag + (additionalDragPerMissingPart * disabledPartCount);
        }
    }
    
    // This method will be called by the PlaneController to modify the torque based on damaged parts
    public Vector3 ModifyTorqueForDamage(Vector3 originalTorque, float horizontalInput, float verticalInput)
    {
        Vector3 modifiedTorque = originalTorque;
        
        bool leftWingDisabled = leftWing != null && !leftWing.activeSelf;
        bool rightWingDisabled = rightWing != null && !rightWing.activeSelf;
        bool tailDisabled = tail != null && !tail.activeSelf;
        
        // If left wing is disabled
        if (leftWingDisabled)
        {
            // Add constant tilt to the left (positive around z-axis) - use a smaller value to reduce shaking
            float tiltAmount = wingTiltStrength * 0.05f; // Reduced from 0.1f to 0.05f for smoother effect
            modifiedTorque.z += tiltAmount;
            
            // If trying to turn left (negative input), increase roll effect but cap it to prevent extreme values
            if (horizontalInput < 0)
            {
                // Apply multiplier but with a maximum cap to prevent extreme rotation
                float multipliedTorque = modifiedTorque.z * wingDamageRollMultiplier;
                modifiedTorque.z = Mathf.Clamp(multipliedTorque, -wingTiltStrength * 0.5f, wingTiltStrength * 0.5f);
            }
        }
        
        // If right wing is disabled
        if (rightWingDisabled)
        {
            // Add constant tilt to the right (negative around z-axis) - use a smaller value to reduce shaking
            float tiltAmount = wingTiltStrength * 0.05f; // Reduced from 0.1f to 0.05f for smoother effect
            modifiedTorque.z -= tiltAmount;
            
            // If trying to turn right (positive input), increase roll effect but cap it to prevent extreme values
            if (horizontalInput > 0)
            {
                // Apply multiplier but with a maximum cap to prevent extreme rotation
                float multipliedTorque = modifiedTorque.z * wingDamageRollMultiplier;
                modifiedTorque.z = Mathf.Clamp(multipliedTorque, -wingTiltStrength * 0.5f, wingTiltStrength * 0.5f);
            }
        }
        
        // If tail is disabled and trying to pitch
        if (tailDisabled)
        {
            // Add additional downward pitch (around right axis) but clamp it to prevent extreme values
            float pitchAdjustment = 0.5f * Mathf.Abs(verticalInput) * planeController.pitchSpeed * planeController.torqueResponseMultiplier;
            modifiedTorque.x -= pitchAdjustment;
            // Clamp to prevent extreme pitch values
            modifiedTorque.x = Mathf.Clamp(modifiedTorque.x, -wingTiltStrength * 0.5f, wingTiltStrength * 0.5f);
        }
        
        return modifiedTorque;
    }
}
