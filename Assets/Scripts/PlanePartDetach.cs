using UnityEngine;

/// <summary>
/// Detaches a plane part on hard impact and lets it simulate under normal PhysX.
/// </summary>
public class PlanePartDetach : MonoBehaviour
{
    [Header("Detach Settings")]
    [Tooltip("Minimum collision speed needed for this part to break off.")]
    public float detachImpactThreshold = 5f;

    [Tooltip("Small extra push away from the hit so the part clears the fuselage.")]
    public float detachmentForce = 1.4f;

    [Tooltip("How much crash speed adds to the break-off push.")]
    public float impactForceScale = 0.1f;

    [Tooltip("Max extra break-off speed added on top of the plane's inherited velocity.")]
    public float maxBreakAwaySpeed = 5f;

    [Tooltip("Extra tumble torque scale based on crash speed.")]
    public float impactTorqueScale = 0.07f;

    [Tooltip("Cascade (non-hit) parts get this fraction of the break-away push.")]
    [Range(0.1f, 1f)]
    public float cascadeForceMultiplier = 0.55f;

    [Tooltip("Mass of the detached part rigidbody.")]
    public float partMass = 0.55f;

    [Tooltip("Linear drag after the part breaks free.")]
    public float partDrag = 0.35f;

    [Tooltip("Angular drag after the part breaks free.")]
    public float partAngularDrag = 0.45f;

    [Tooltip("Radius used when spreading the break-off impulse around the hit point.")]
    public float forceRadius = 0.75f;

    [Tooltip("Check this for the main fuselage. Hitting it can cascade-detach other parts.")]
    public bool isCoreBodyPart = false;

    public JoystickController joystickController;

    private bool detached;
    public bool IsDetached => detached;

    private Rigidbody mainPlaneRb;
    private PlaneController planeController;

    private void Awake()
    {
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycast >= 0)
            gameObject.layer = ignoreRaycast;
    }

    private void Start()
    {
        mainPlaneRb = GetComponentInParent<Rigidbody>();
        planeController = GetComponentInParent<PlaneController>();

        if (GetComponent<Collider>() == null)
            Debug.LogWarning($"{name} has no collider; it won't trigger detachment.");
    }

    private void OnCollisionEnter(Collision collision)
    {
        VibrationManager.Instance?.VibrateButtonClick();
        HandleCollision(collision);
    }

    public void HandleCollision(Collision collision)
    {
        if (detached || collision == null)
            return;

        float impactMagnitude = collision.relativeVelocity.magnitude;
        Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 impactVelocity = collision.relativeVelocity;

        // Tree hits: break the part free, but leave plane flight handling to PlaneController.
        if (collision.gameObject.CompareTag("Tree"))
        {
            Detach(hitPoint, impactMagnitude, impactVelocity);
            return;
        }

        Debug.Log(
            $"[{name}] Handling collision with {collision.gameObject.name}. " +
            $"Impact: {impactMagnitude:F2}, Threshold: {detachImpactThreshold:F2}");

        if (impactMagnitude < detachImpactThreshold)
            return;

        if (joystickController != null && joystickController.joystickBG != null)
            joystickController.joystickBG.gameObject.SetActive(false);

        Detach(hitPoint, impactMagnitude, impactVelocity);
    }

    public void Detach(Vector3 hitPoint)
    {
        Detach(hitPoint, detachImpactThreshold, Vector3.zero);
    }

    public void Detach(Vector3 hitPoint, float impactMagnitude, Vector3 impactVelocity)
    {
        if (detached)
            return;

        detached = true;

        // Core fuselage stays on the main plane rigidbody and cascades other parts.
        if (isCoreBodyPart)
        {
            Debug.Log("Core body part hit. Triggering chain reaction to detach other parts.");
            if (planeController == null)
                planeController = GetComponentInParent<PlaneController>();

            if (planeController != null)
                planeController.StartCoroutine(planeController.DetachAllParts(hitPoint, impactMagnitude, impactVelocity));

            return;
        }

        ActivatePhysicsPart(hitPoint, impactMagnitude, impactVelocity);
        Debug.Log($"{name} detached due to impact {impactMagnitude:F1}!");
    }

    /// <summary>
    /// Used by cascade detach so non-core parts get the same crash impulse as the original hit.
    /// </summary>
    public void DetachFromCascade(Vector3 hitPoint, float impactMagnitude, Vector3 impactVelocity)
    {
        if (detached || isCoreBodyPart)
            return;

        detached = true;
        ActivatePhysicsPart(hitPoint, impactMagnitude, impactVelocity, cascadeForceMultiplier);
        Debug.Log($"{name} cascade-detached due to impact {impactMagnitude:F1}!");
    }

    private void ActivatePhysicsPart(
        Vector3 hitPoint,
        float impactMagnitude,
        Vector3 impactVelocity,
        float forceMultiplier = 1f)
    {
        if (mainPlaneRb == null)
            mainPlaneRb = GetComponentInParent<Rigidbody>();

        // Inherit most of the plane motion so parts don't look rocketed away from the wreck.
        Vector3 inheritVelocity = mainPlaneRb != null ? mainPlaneRb.velocity * 0.95f : Vector3.zero;
        Vector3 inheritAngular = mainPlaneRb != null ? mainPlaneRb.angularVelocity * 0.75f : Vector3.zero;

        // Unparent so this mesh becomes its own physics object.
        transform.SetParent(null, true);

        PrepareCollidersForPhysics();

        Rigidbody partRb = GetComponent<Rigidbody>();
        if (partRb == null)
            partRb = gameObject.AddComponent<Rigidbody>();

        partRb.isKinematic = false;
        partRb.useGravity = true;
        partRb.mass = Mathf.Max(0.05f, partMass);
        partRb.drag = partDrag;
        partRb.angularDrag = partAngularDrag;
        partRb.interpolation = RigidbodyInterpolation.Interpolate;
        partRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        partRb.constraints = RigidbodyConstraints.None;
        partRb.velocity = inheritVelocity;
        partRb.angularVelocity = inheritAngular;

        // Avoid fighting the fuselage for a moment after break-off.
        IgnoreCollisionWithMainPlane();

        Vector3 outward = transform.position - hitPoint;
        if (outward.sqrMagnitude < 0.0001f)
            outward = Random.onUnitSphere;
        outward.Normalize();

        // Soft separation only — mostly keep the inherited crash motion.
        float breakAway = (detachmentForce + impactMagnitude * impactForceScale) * Mathf.Clamp01(forceMultiplier);
        breakAway = Mathf.Clamp(breakAway, 0f, maxBreakAwaySpeed);

        partRb.AddForce(outward * breakAway, ForceMode.VelocityChange);

        float torque = impactMagnitude * impactTorqueScale * Mathf.Clamp01(forceMultiplier);
        torque = Mathf.Min(torque, 3f);
        Vector3 torqueAxis = Vector3.Cross(outward, Vector3.up);
        if (torqueAxis.sqrMagnitude < 0.0001f)
            torqueAxis = Random.onUnitSphere;
        partRb.AddTorque(torqueAxis.normalized * torque, ForceMode.VelocityChange);
    }

    private void PrepareCollidersForPhysics()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col == null)
                continue;

            if (col is MeshCollider meshCollider)
            {
                // Dynamic rigidbodies need convex mesh colliders.
                if (!meshCollider.convex)
                    meshCollider.convex = true;
                meshCollider.enabled = meshCollider.convex;
            }
            else
            {
                col.enabled = true;
            }
        }
    }

    private void IgnoreCollisionWithMainPlane()
    {
        if (mainPlaneRb == null)
            return;

        Collider[] partCols = GetComponentsInChildren<Collider>(true);
        Collider[] mainCols = mainPlaneRb.GetComponentsInChildren<Collider>(true);

        foreach (Collider partCol in partCols)
        {
            if (partCol == null)
                continue;

            foreach (Collider mainCol in mainCols)
            {
                if (mainCol == null || mainCol.transform.IsChildOf(transform))
                    continue;

                Physics.IgnoreCollision(partCol, mainCol, true);
            }
        }
    }
}
