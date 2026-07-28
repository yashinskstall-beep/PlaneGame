using UnityEngine;
using System.Collections;

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
    public float exitRaycastDistance = 1.5f;

    [Tooltip("Extra into-surface speed kept while launch-sticking (m/s).")]
    public float intoSurfaceStickSpeed = 0.75f;

    [Header("Exit Flight Align")]
    [Tooltip("After leaving the tip, blend the nose toward travel direction so flight starts aligned.")]
    public float exitAlignDuration = 0.35f;

    private Rigidbody planeRb;
    private bool isAligning = false;
    private Transform currentRamp;
    private Quaternion originalRotation;
    private int framesWithoutRampContact;
    private bool exitPending;
    private float launchStickUntil = -1f;
    private Coroutine exitAlignRoutine;

    /// <summary>True while stuck to the ramp, including brief exit-confirm frames.</summary>
    public bool IsAligning => isAligning || exitPending || IsLaunchStickHolding;

    /// <summary>Ramp transform currently sticking the plane, if any.</summary>
    public Transform CurrentRamp => currentRamp;

    private bool IsLaunchStickActive => Time.time < launchStickUntil;

    /// <summary>Launch stick only counts while the ramp is still under the plane — never past the tip.</summary>
    private bool IsLaunchStickHolding => IsLaunchStickActive && currentRamp != null && IsRampStillUnderPlane();

    private void Start()
    {
        if (plane == null)
            plane = transform;

        planeRb = plane.GetComponent<Rigidbody>();

        if (dragLauncher == null)
            dragLauncher = GetComponent<SimpleDragLauncher>();
    }

    /// <summary>
    /// Force ramp stick for a short window after launch so brief contact loss
    /// at the start of the roll does not unlock flight early.
    /// </summary>
    public void BeginLaunchStick(float duration)
    {
        launchStickUntil = Time.time + Mathf.Max(0.05f, duration);
        exitPending = false;
        framesWithoutRampContact = 0;

        if (currentRamp == null)
            TryResolveRampUnderPlane();

        if (currentRamp != null)
            isAligning = true;
    }

    private void FixedUpdate()
    {
        if (IsLaunchStickActive)
        {
            if (currentRamp == null)
                TryResolveRampUnderPlane();

            // Stick only while still over the shed. Past the tip → end stick and exit normally.
            if (currentRamp != null && IsRampStillUnderPlane())
            {
                isAligning = true;
                exitPending = false;
                framesWithoutRampContact = 0;
                AlignWithRamp();
                return;
            }

            launchStickUntil = -1f;
            if (currentRamp != null && !exitPending)
            {
                isAligning = false;
                exitPending = true;
                framesWithoutRampContact = 0;
            }
        }

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

        // Keep Rigidbody + transform in sync (writing transform alone desyncs flight exit).
        Quaternion nextRotation = Quaternion.Slerp(planeRb.rotation, targetRotation, Time.fixedDeltaTime * alignmentSpeed);
        planeRb.MoveRotation(nextRotation);

        if (projectVelocityOnRamp && !planeRb.isKinematic)
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(planeRb.velocity, rampNormal);
            float intoSurface = Vector3.Dot(planeRb.velocity, -rampNormal);
            float stick = intoSurface > 0f ? intoSurface : 0f;
            if (IsLaunchStickHolding)
                stick = Mathf.Max(stick, intoSurfaceStickSpeed);
            if (stick > 0f)
                planarVelocity += -rampNormal * stick;
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

        // Ignore flicker only while still over the ramp.
        if (IsLaunchStickActive && IsRampStillUnderPlane())
        {
            isAligning = true;
            exitPending = false;
            framesWithoutRampContact = 0;
            return;
        }

        isAligning = false;
        exitPending = true;
        framesWithoutRampContact = 0;
    }

    private bool TryResolveRampUnderPlane()
    {
        if (plane == null)
            return false;

        Vector3 origin = plane.position + Vector3.up * 0.35f;
        float distance = Mathf.Max(exitRaycastDistance, 2.5f);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider == null || !IsRamp(hit.collider.transform))
            return false;

        currentRamp = hit.collider.transform;
        return true;
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
        launchStickUntil = -1f;

        // Point the nose along travel before flight control / post-ramp boost take over.
        AlignNoseToVelocityImmediate();
        if (exitAlignRoutine != null)
            StopCoroutine(exitAlignRoutine);
        if (exitAlignDuration > 0.01f && gameObject.activeInHierarchy)
            exitAlignRoutine = StartCoroutine(SmoothlyAlignToVelocity());

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

    private void AlignNoseToVelocityImmediate()
    {
        if (planeRb == null || planeRb.isKinematic)
            return;
        if (planeRb.velocity.sqrMagnitude < 0.25f)
            return;

        Quaternion target = Quaternion.LookRotation(planeRb.velocity.normalized, Vector3.up);
        planeRb.MoveRotation(target);
        if (plane != null)
            plane.rotation = target;
    }

    private IEnumerator SmoothlyAlignToVelocity()
    {
        float duration = Mathf.Max(0.05f, exitAlignDuration);
        float elapsed = 0f;
        Quaternion startRotation = planeRb != null ? planeRb.rotation : plane.rotation;

        while (elapsed < duration)
        {
            if (plane == null)
                yield break;

            Vector3 travelDir = plane.forward;
            if (planeRb != null && planeRb.velocity.sqrMagnitude > 0.25f)
                travelDir = planeRb.velocity.normalized;

            Quaternion target = Quaternion.LookRotation(travelDir, Vector3.up);
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            Quaternion next = Quaternion.Slerp(startRotation, target, t);

            if (planeRb != null && !planeRb.isKinematic)
                planeRb.MoveRotation(next);
            else
                plane.rotation = next;

            elapsed += Time.deltaTime;
            yield return null;
        }

        exitAlignRoutine = null;
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
