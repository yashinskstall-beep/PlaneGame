using UnityEngine;

public class PlanePartDetach : MonoBehaviour
{
    [Header("Detach Settings")]
    public float detachImpactThreshold = 5f;     // Collision strength to detach
    public float detachmentForce = 100f;         // Explosion force when detaching
    public float forceRadius = 2f;               // Radius for explosion effect
    [Tooltip("Check this for the main fuselage part. If this part detaches, all others will too.")]
    public bool isCoreBodyPart = false;
    public JoystickController joystickController;

    private bool detached = false;
    public bool IsDetached => detached;
    private Rigidbody mainPlaneRb;
   


    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void Start()
    {
        // Find the main plane rigidbody (usually on the Airplane root)
        mainPlaneRb = GetComponentInParent<Rigidbody>();
        // Ensure our part has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"{name} has no collider; it won't trigger detachment.");
        }

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        Debug.Log($"{name} now ignoring raycasts");
           
    }

    private void OnCollisionEnter(Collision collision)
    {
        // This will only be called if the part itself has a Rigidbody, 
        // but we're keeping it for flexibility.
        HandleCollision(collision);
       


    }

    public void HandleCollision(Collision collision)
    {
        if (detached) return;

        // Special handling for trees: detach the part but don't stop the plane
        if (collision.gameObject.CompareTag("Tree"))
        {
            Debug.Log($"[{name}] Collided with a tree. Detaching part without applying major impact force.");
            Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            Detach(hitPoint);
            return; // Stop further processing for tree collisions
        }

        // Normal collision handling for ground and other objects
        float impactMagnitude = collision.relativeVelocity.magnitude;
        Debug.Log($"[{name}] Handling collision with {collision.gameObject.name}. Impact: {impactMagnitude}, Threshold: {detachImpactThreshold}");

        if (impactMagnitude >= detachImpactThreshold)
        {
            if (joystickController != null && joystickController.joystickBG != null)
            {
                joystickController.joystickBG.gameObject.SetActive(false);
            }
            
            Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            Detach(hitPoint);
        }
    }

    public void Detach(Vector3 hitPoint)
    {
        if (detached) return;

        detached = true;

        // If this is the core part, trigger the chain reaction but DO NOT detach this part.
        if (isCoreBodyPart)
        {
            Debug.Log("Core body part hit. Triggering chain reaction to detach other parts.");
            PlaneController controller = GetComponentInParent<PlaneController>();
            if (controller != null)
            {
                // Use a coroutine to allow this part's logic to complete first
                controller.StartCoroutine(controller.DetachAllParts());
            }
            // The core part itself does not get physically detached. It remains the primary object.
            return; // End the method here for the core part.
        }

        // --- The rest of the logic only applies to NON-CORE parts ---

        // Stop this part from following the main plane
        transform.SetParent(null);

        // Add a rigidbody if it doesn't already have one
        Rigidbody partRb = GetComponent<Rigidbody>();
        if (partRb == null)
            partRb = gameObject.AddComponent<Rigidbody>();

        // Allow it to be affected by physics
        partRb.isKinematic = false;
        partRb.interpolation = RigidbodyInterpolation.Interpolate;
        partRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Sweep test to prevent tunneling on frame 1
        if (partRb.SweepTest(partRb.velocity, out RaycastHit hit, 0.1f)) {
            partRb.transform.position += hit.normal * 0.1f; // Move it slightly away from the hit surface
        }

        // If main plane has a rigidbody, ignore collision between them to prevent weird forces
        if (mainPlaneRb != null)
        { 
            Collider partCol = GetComponent<Collider>();
            Collider[] mainCols = mainPlaneRb.GetComponentsInChildren<Collider>();
            foreach (var c in mainCols)
            {
                if (partCol != null && c != null)
                    Physics.IgnoreCollision(partCol, c, true);
            }
        }

        // Apply small outward impulse
        partRb.AddExplosionForce(detachmentForce, hitPoint, forceRadius, 0.5f, ForceMode.Impulse);

        Debug.Log($"{name} detached due to impact!");
    }
}
