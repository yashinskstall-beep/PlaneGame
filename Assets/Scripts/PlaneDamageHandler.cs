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
    
    [Header("Air Resistance Settings")]
    [Tooltip("Additional air resistance when left wing is missing")]
    public float leftWingMissingResistance = 0.3f;
    
    [Tooltip("Additional air resistance when right wing is missing")]
    public float rightWingMissingResistance = 0.3f;
    
    [Tooltip("Additional air resistance when both wings are missing")]
    public float bothWingsMissingResistance = 1.0f;

    [Tooltip("Drag applied when all parts are missing")]
    public float allPartsMissingDrag = 0.1f;
    
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
        
        bool bothWingsMissing = leftWingDisabled && rightWingDisabled;

        // Apply tail damage effects
        if (tailDisabled)
        {
            planeController.pitchSpeed *= tailDamagePitchMultiplier;
        }

        // Apply additional drag based on missing parts
        if (rb != null)
        {
            if (leftWingDisabled && rightWingDisabled && tailDisabled)
            {
                rb.drag = originalDrag + allPartsMissingDrag;
            }
            else
            {
                // One wing missing: no extra wing drag (same as all wings attached).
                // Both wings missing: apply the full "no wings" resistance.
                int dragPartCount = 0;
                if (bothWingsMissing)
                    dragPartCount += 2;
                if (tailDisabled)
                    dragPartCount++;

                float additionalDrag = additionalDragPerMissingPart * dragPartCount;
                if (bothWingsMissing)
                    additionalDrag += bothWingsMissingResistance;

                rb.drag = originalDrag + additionalDrag;
            }
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
    
    /// <summary>
    /// Check if both wings are missing/disabled
    /// </summary>
    /// <returns>True if both wings are disabled, false otherwise</returns>
    public bool AreBothWingsMissing()
    {
        bool leftWingDisabled = leftWing != null && !leftWing.activeSelf;
        bool rightWingDisabled = rightWing != null && !rightWing.activeSelf;
        
        return leftWingDisabled && rightWingDisabled;
    }
    
    /// <summary>
    /// Check if left wing is missing/disabled
    /// </summary>
    /// <returns>True if left wing is disabled, false otherwise</returns>
    public bool IsLeftWingMissing()
    {
        return leftWing != null && !leftWing.activeSelf;
    }
    
    /// <summary>
    /// Check if right wing is missing/disabled
    /// </summary>
    /// <returns>True if right wing is disabled, false otherwise</returns>
    public bool IsRightWingMissing()
    {
        return rightWing != null && !rightWing.activeSelf;
    }
}