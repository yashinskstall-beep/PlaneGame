using UnityEngine;

/// <summary>
/// Session start/stop for a flight. Camera scripts call this; they do not own plane/UI enabling.
/// </summary>
public static class GameFlow
{
    public static bool IsGameplayActive { get; private set; }

    public static void BeginGameplay(GameObject airPlane, GameObject guiCanvas)
    {
        IsGameplayActive = true;

        if (guiCanvas != null)
            guiCanvas.SetActive(true);

        if (airPlane == null)
            return;

        Collider col = airPlane.GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        SimpleDragLauncher launcher = airPlane.GetComponent<SimpleDragLauncher>()
            ?? airPlane.GetComponentInChildren<SimpleDragLauncher>();
        if (launcher != null)
        {
            launcher.SetDragEnabled(true);
            launcher.ResetForNewLaunch();
        }

        PlaneController planeController = airPlane.GetComponent<PlaneController>();
        if (planeController != null)
        {
            planeController.InitializeDetachableParts();
            planeController.UseRampColliders();
        }
        else
            Debug.LogWarning("GameFlow: PlaneController not found on airplane.");

        FlightHUD uiManager = Object.FindObjectOfType<FlightHUD>();
        if (uiManager != null)
            uiManager.OnGameplayStarted();
    }

    public static void EndGameplay(GameObject airPlane)
    {
        IsGameplayActive = false;

        if (airPlane == null)
            return;

        Collider col = airPlane.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        PlaneController planeController = airPlane.GetComponent<PlaneController>();
        if (planeController != null)
            planeController.StopControlling();

        SimpleDragLauncher launcher = airPlane.GetComponent<SimpleDragLauncher>()
            ?? airPlane.GetComponentInChildren<SimpleDragLauncher>();
        if (launcher != null)
            launcher.SetDragEnabled(false);
    }
}
