using UnityEngine;

/// <summary>
/// Handles aligning a plane GameObject with a 3D ramp after being released.
/// Keep velocity on the ramp surface and debounce exit so strong slingshot upgrades
/// do not skip the ramp via a one-frame contact loss.
/// </summary>
public class PlaneRampAligner : MonoBehaviour
{
    [Header("References")]
    public Transform plane;
    public SimpleDragLauncher dragLauncher;
    public Transform[] ramps;

    [Header("Alignment Settings")]
    public float alignmentSpeed = 10f;
    public float minVelocityForAlignment = 1f;
    public bool alignToVelocity = true;
    public string rampTag = "RampTag";
    public bool useTagForDetection = false;

    [Header("Ramp Stick")]
    [Tooltip("Project velocity onto the ramp plane while contacting it.")]
    public bool projectVelocityOnRamp = true;

    [Tooltip("Frames without ramp contact before switching to flight. Absorbs high-force contact flicker.")]
    public int exitConfirmFrames = 4;

    [Tooltip("While confirming exit, a downward ray this long can cancel exit if the ramp is still under the plane.")]
    public float exitRaycastDistance = 1.25f;

    private Rigidbody planeRb;
    private bool isAligning = false;
    private Transform currentRamp;
    private Quaternion originalRotation;
    private int framesWithoutRampContact;
    private bool exitPending;

    /// <summary>True while stuck to the ramp, including brief exit-confirm frames.</summary>
    public bool IsAligning => isAligning || exitPending;

    /// <summary>Ramp transform currently sticking the plane, if any.</summary>
    public Transform CurrentRamp => currentRamp;

    private void Start()
    {
        if (plane == null)
            plane = transform;

        planeRb = plane.GetComponent<Rigidbody>();

        if (dragLauncher == null)
            dragLauncher = GetComponent<SimpleDragLauncher>();
    }

    private void FixedUpdate()
    {
        if (isAligning && currentRamp != null)
        {
            framesWithoutRampContact = 0;
            exitPending = false;
            AlignWithRamp();
            return;
        }

        if (!exitPending || currentRamp == null)
            return;

        framesWithoutRampContact++;

        // Still over the ramp after a bounce/tunnel flicker — stay on ramp mode.
        if (IsRampStillUnderPlane())
        {
            isAligning = true;
            exitPending = false;
            framesWithoutRampContact = 0;
            AlignWithRamp();
            return;
        }

        if (framesWithoutRampContact >= Mathf.Max(1, exitConfirmFrames))
            CompleteRampExit();
    }

    private void AlignWithRamp()
    {
        if (currentRamp == null || plane == null || planeRb == null)
            return;

        Vector3 rampNormal = currentRamp.up;
        Vector3 rampForward = currentRamp.forward;
        Quaternion targetRotation = Quaternion.LookRotation(rampForward, rampNormal);

        if (alignToVelocity && planeRb.velocity.magnitude > minVelocityForAlignment)
        {
            Vector3 projectedVelocity = Vector3.ProjectOnPlane(planeRb.velocity, rampNormal);
            if (projectedVelocity.sqrMagnitude > 0.01f)
                targetRotation = Quaternion.LookRotation(projectedVelocity.normalized, rampNormal);
        }

        plane.rotation = Quaternion.Slerp(plane.rotation, targetRotation, Time.fixedDeltaTime * alignmentSpeed);

        // Keep strong launch impulses sliding along the shed instead of leaping off.
        if (projectVelocityOnRamp && !planeRb.isKinematic)
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(planeRb.velocity, rampNormal);
            // Preserve a little speed into the surface so gravity/contact stay engaged.
            float intoSurface = Vector3.Dot(planeRb.velocity, -rampNormal);
            if (intoSurface > 0f)
                planarVelocity += -rampNormal * intoSurface;
            planeRb.velocity = planarVelocity;
        }

        Debug.DrawRay(plane.position, rampNormal * 2f, Color.green);
        Debug.DrawRay(plane.position, rampForward * 2f, Color.red);
        if (planeRb.velocity.magnitude > 0.1f)
            Debug.DrawRay(plane.position, planeRb.velocity.normalized * 2f, Color.blue);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsRamp(collision.transform))
            return;

        if (currentRamp == null)
            originalRotation = plane.rotation;

        currentRamp = collision.transform;
        isAligning = true;
        exitPending = false;
        framesWithoutRampContact = 0;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (currentRamp != null)
        {
            if (collision.transform == currentRamp)
            {
                isAligning = true;
                exitPending = false;
                framesWithoutRampContact = 0;
            }
            return;
        }

        if (IsRamp(collision.transform))
        {
            currentRamp = collision.transform;
            isAligning = true;
            exitPending = false;
            framesWithoutRampContact = 0;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (currentRamp != collision.transform)
            return;

        // Do not leave ramp mode on a single exit event — high force often flickers contact.
        isAligning = false;
        exitPending = true;
        framesWithoutRampContact = 0;
    }

    private bool IsRampStillUnderPlane()
    {
        if (plane == null || currentRamp == null)
            return false;

        Vector3 origin = plane.position + Vector3.up * 0.35f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, exitRaycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider != null &&
               (hit.collider.transform == currentRamp || hit.collider.transform.IsChildOf(currentRamp));
    }

    private void CompleteRampExit()
    {
        currentRamp = null;
        isAligning = false;
        exitPending = false;
        framesWithoutRampContact = 0;

        PlaneController planeController = plane != null ? plane.GetComponent<PlaneController>() : null;
        if (planeController != null)
        {
            planeController.ForceControl();

            if (planeController.isControlling && planeController.useJoystickInput && planeController.joystick != null)
                planeController.joystick.gameObject.SetActive(true);
        }

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        if (planeRb != null)
        {
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            planeRb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        }

        if (planeController != null)
            planeController.UseFlightPartColliders();
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }

    private System.Collections.IEnumerator SmoothlyRestoreRotation()
    {
        float elapsedTime = 0f;
        float duration = 2.5f;
        Quaternion startRotation = plane.rotation;

        while (elapsedTime < duration)
        {
            plane.rotation = Quaternion.Slerp(startRotation, originalRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        plane.rotation = originalRotation;
    }

    public bool IsRampTransform(Transform potentialRamp)
    {
        return IsRamp(potentialRamp);
    }

    private bool IsRamp(Transform potentialRamp)
    {
        if (potentialRamp == null)
            return false;

        if (useTagForDetection)
        {
            try
            {
                return potentialRamp.gameObject.CompareTag(rampTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"Tag '{rampTag}' is not defined in Unity Tags. Using fallback detection.");
                useTagForDetection = false;
            }
        }

        if (ramps != null && ramps.Length > 0)
        {
            foreach (Transform ramp in ramps)
            {
                if (ramp == potentialRamp)
                    return true;
            }
            return false;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        if (plane != null && currentRamp != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(plane.position, currentRamp.position);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(currentRamp.position, currentRamp.up * 2f);
        }
    }
}
