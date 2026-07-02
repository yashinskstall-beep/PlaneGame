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
    
    private PlaneController planeController;
    
    private float originalTurnSpeed;
    private float originalBankAngle;
    private float originalPitchSpeed;
    private float originalDrag;
    private float originalAngularDrag;
    
    void Start()
    {
        planeController = GetComponent<PlaneController>();
        
        if (planeController == null)
        {
            Debug.LogError("PlaneDamageHandler: No PlaneController component found!");
            return;
        }
        
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
        CheckPartsStatus();
    }

    /// <summary>
    /// Part is gone from a crash after it was unlocked — not merely locked in the upgrade shop.
    /// </summary>
    public static bool IsPartLostInFlight(GameObject part)
    {
        if (part == null || part.activeSelf)
            return false;

        return PlaneUpgradeConfig.IsPartUnlocked(part);
    }
    
    void CheckPartsStatus()
    {
        if (planeController == null)
            return;
        
        bool leftWingLost = IsPartLostInFlight(leftWing);
        bool rightWingLost = IsPartLostInFlight(rightWing);
        bool tailLost = IsPartLostInFlight(tail);
        
        planeController.turnSpeed = originalTurnSpeed;
        planeController.bankAngle = originalBankAngle;
        planeController.pitchSpeed = originalPitchSpeed;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.drag = originalDrag;
            rb.angularDrag = originalAngularDrag;
        }
        
        bool bothWingsLost = leftWingLost && rightWingLost;
        bool singleWingLost = leftWingLost != rightWingLost;

        if (singleWingLost && rb != null)
            rb.angularDrag = originalAngularDrag * 1.75f;

        if (tailLost)
            planeController.pitchSpeed *= tailDamagePitchMultiplier;

        if (rb != null)
        {
            if (leftWingLost && rightWingLost && tailLost)
            {
                rb.drag = originalDrag + allPartsMissingDrag;
            }
            else
            {
                int dragPartCount = 0;
                if (bothWingsLost)
                    dragPartCount += 2;
                if (tailLost)
                    dragPartCount++;

                float additionalDrag = additionalDragPerMissingPart * dragPartCount;
                if (bothWingsLost)
                    additionalDrag += bothWingsMissingResistance;

                rb.drag = originalDrag + additionalDrag;
            }
        }
    }
    
    public Vector3 GetAdditionalDamageTorque(Transform planeTransform, float horizontalInput, float verticalInput)
    {
        if (planeTransform == null || planeController == null)
            return Vector3.zero;

        Vector3 additionalTorque = Vector3.zero;

        bool leftWingLost = IsPartLostInFlight(leftWing);
        bool rightWingLost = IsPartLostInFlight(rightWing);
        bool tailLost = IsPartLostInFlight(tail);
        bool singleWingLost = leftWingLost != rightWingLost;
        float pitchInput = Mathf.Abs(verticalInput);

        float tiltAmount = wingTiltStrength * 0.03f;

        if (leftWingLost && !rightWingLost)
        {
            additionalTorque += planeTransform.forward * tiltAmount;

            if (horizontalInput < 0f)
            {
                float rollBoost = -horizontalInput * tiltAmount * wingDamageRollMultiplier;
                additionalTorque += planeTransform.forward * rollBoost;
            }
        }

        if (rightWingLost && !leftWingLost)
        {
            additionalTorque -= planeTransform.forward * tiltAmount;

            if (horizontalInput > 0f)
            {
                float rollBoost = horizontalInput * tiltAmount * wingDamageRollMultiplier;
                additionalTorque -= planeTransform.forward * rollBoost;
            }
        }

        if (tailLost && pitchInput > 0.05f)
        {
            float pitchAdjustment = 0.35f * pitchInput * planeController.pitchSpeed * planeController.torqueResponseMultiplier;
            additionalTorque -= planeTransform.right * pitchAdjustment;
        }

        if (singleWingLost && pitchInput > 0.05f)
            additionalTorque *= Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(pitchInput));

        return additionalTorque;
    }

    public bool HasSingleWingMissing()
    {
        return IsPartLostInFlight(leftWing) != IsPartLostInFlight(rightWing);
    }
    
    public bool IsBodyOnly()
    {
        return IsPartLostInFlight(leftWing) && IsPartLostInFlight(rightWing) && IsPartLostInFlight(tail);
    }

    public bool AreBothWingsMissing()
    {
        return IsPartLostInFlight(leftWing) && IsPartLostInFlight(rightWing);
    }
    
    public bool IsLeftWingMissing()
    {
        return IsPartLostInFlight(leftWing);
    }
    
    public bool IsRightWingMissing()
    {
        return IsPartLostInFlight(rightWing);
    }
}
