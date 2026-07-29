using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public FlightHUD uiManager;

    private bool triggered;

    public void OnTriggerEnter(Collider other)
    {
        if (triggered || other == null)
            return;

        if (!IsPlayerCollider(other))
            return;

        triggered = true;
        Debug.Log("Goal Triggered!");

        if (uiManager != null)
            uiManager.GoalScreen();
        else
            Debug.LogWarning("GoalTrigger: uiManager is not assigned.", this);
    }

    /// <summary>
    /// After ramp exit the root Player collider is disabled and part MeshColliders
    /// hit the goal instead. Forest tags those parts Player; Desert/Snow often do not.
    /// Resolve via the Rigidbody root so all scenes work.
    /// </summary>
    private static bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.CompareTag("Player"))
            return true;

        PlaneController plane = other.GetComponentInParent<PlaneController>();
        return plane != null && plane.CompareTag("Player");
    }
}
