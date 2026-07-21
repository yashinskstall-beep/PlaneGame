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
    private float originalAngularDrag;
    
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
            originalAngularDrag = rb.angularDrag;
        }
    }
    
    void Update()
    {
        // Check if any parts are disabled and apply effects
        CheckPartsStatus();
    }
    
    void CheckPartsStatus()
    {
        if (planeController == null)
            return;

        // After a crash PhysX owns drag/spin — do not keep overriding the rigidbody.
        if (planeController.IsWreckPhysicsActive)
            return;
        
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
            rb.angularDrag = originalAngularDrag;
        }
        
        bool bothWingsMissing = leftWingDisabled && rightWingDisabled;
        bool singleWingMissing = leftWingDisabled != rightWingDisabled;

        // Slightly more rotational damping with one wing keeps pitch-up from turning into roll wobble.
        if (singleWingMissing && rb != null)
            rb.angularDrag = originalAngularDrag * 1.75f;

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
    
    /// <summary>
    /// Extra torque from missing parts, applied on the plane's local roll/pitch axes.
    /// </summary>
    public Vector3 GetAdditionalDamageTorque(Transform planeTransform, float horizontalInput, float verticalInput)
    {
        if (planeTransform == null || planeController == null)
            return Vector3.zero;

        Vector3 additionalTorque = Vector3.zero;

        bool leftWingDisabled = leftWing != null && !leftWing.activeSelf;
        bool rightWingDisabled = rightWing != null && !rightWing.activeSelf;
        bool tailDisabled = tail != null && !tail.activeSelf;
        bool singleWingMissing = leftWingDisabled != rightWingDisabled;
        float pitchInput = Mathf.Abs(verticalInput);

        float tiltAmount = wingTiltStrength * 0.03f;

        if (leftWingDisabled && !rightWingDisabled)
        {
            additionalTorque += planeTransform.forward * tiltAmount;

            if (horizontalInput < 0f)
            {
                float rollBoost = -horizontalInput * tiltAmount * wingDamageRollMultiplier;
                additionalTorque += planeTransform.forward * rollBoost;
            }
        }

        if (rightWingDisabled && !leftWingDisabled)
        {
            additionalTorque -= planeTransform.forward * tiltAmount;

            if (horizontalInput > 0f)
            {
                float rollBoost = horizontalInput * tiltAmount * wingDamageRollMultiplier;
                additionalTorque -= planeTransform.forward * rollBoost;
            }
        }

        if (tailDisabled && pitchInput > 0.05f)
        {
            float pitchAdjustment = 0.35f * pitchInput * planeController.pitchSpeed * planeController.torqueResponseMultiplier;
            additionalTorque -= planeTransform.right * pitchAdjustment;
        }

        // Pitching with one wing used to fight constant roll torque and caused stumble/wobble.
        if (singleWingMissing && pitchInput > 0.05f)
            additionalTorque *= Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(pitchInput));

        return additionalTorque;
    }

    public bool HasSingleWingMissing()
    {
        return IsLeftWingMissing() != IsRightWingMissing();
    }
    
    /// <summary>
    /// True when left wing, right wing, and tail are all inactive (body only).
    /// </summary>
    public bool IsBodyOnly()
    {
        bool leftWingDisabled = leftWing == null || !leftWing.activeSelf;
        bool rightWingDisabled = rightWing == null || !rightWing.activeSelf;
        bool tailDisabled = tail == null || !tail.activeSelf;
        return leftWingDisabled && rightWingDisabled && tailDisabled;
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