using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public Transform mainMenuPosition;
    public Transform startPosition;
    [Tooltip("Camera focus when upgrading the slingshot. Falls back to SlingshotCamPos in scene, then startPosition.")]
    public Transform slingshotCameraPosition;
    public float transitionDuration = 2.0f;
    private Camera mainCamera;
    private bool inTransition = false;
    public bool Atstart = false; // This can be removed if no other script uses it.
    public GameObject GUIcanvas;
    public GameObject MainMenu;
    public GameObject airPlane;
    private bool gameStarted = false;

    void Start()
    {
        mainCamera = Camera.main;
        ResolveSlingshotCameraPosition();
        if (mainMenuPosition != null)
        {
            mainCamera.transform.position = mainMenuPosition.position;
            mainCamera.transform.rotation = mainMenuPosition.rotation;
        }
        airPlane.gameObject.GetComponent<Collider>().enabled = false;
    }

    public Transform GetSlingshotCameraPosition()
    {
        ResolveSlingshotCameraPosition();
        if (slingshotCameraPosition != null)
            return slingshotCameraPosition;
        return startPosition;
    }

    private void ResolveSlingshotCameraPosition()
    {
        if (slingshotCameraPosition != null)
            return;

        GameObject found = GameObject.Find("SlingshotCamPos");
        if (found != null)
            slingshotCameraPosition = found.transform;
    }

    public void TransitionToStartCamPos(System.Action onComplete = null)
    {
        if (inTransition || startPosition == null)
            return;

        MainMenu mainMenu = MainMenu != null ? MainMenu.GetComponent<MainMenu>() : null;
        if (mainMenu != null && mainMenu.IsUpgrading)
            return;

        StartCoroutine(TransitionToPosition(startPosition.position, startPosition.rotation, transitionDuration, () =>
        {
            BeginGameplay();
            onComplete?.Invoke();
        }));
    }

    public void TransitionToMainMenuCamPos(System.Action onComplete = null)
    {
        if (inTransition || mainMenuPosition == null)
            return;

        gameStarted = false;
        EndGameplay();
        StartCoroutine(TransitionToPosition(mainMenuPosition.position, mainMenuPosition.rotation, transitionDuration, onComplete));
    }

    private void EndGameplay()
    {
        if (airPlane == null)
            return;

        Collider col = airPlane.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        PlaneController planeController = airPlane.GetComponent<PlaneController>();
        if (planeController != null)
            planeController.StopControlling();

        SimpleDragLauncher launcher = airPlane.GetComponent<SimpleDragLauncher>();
        if (launcher == null)
            launcher = airPlane.GetComponentInChildren<SimpleDragLauncher>();

        if (launcher != null)
            launcher.SetDragEnabled(false);
    }

    private void BeginGameplay()
    {
        gameStarted = true;
        Atstart = true;

        if (GUIcanvas != null)
            GUIcanvas.SetActive(true);

        if (airPlane != null)
            airPlane.GetComponent<Collider>().enabled = true;

        SimpleDragLauncher launcher = airPlane != null ? airPlane.GetComponent<SimpleDragLauncher>() : null;
        if (launcher == null && airPlane != null)
            launcher = airPlane.GetComponentInChildren<SimpleDragLauncher>();

        if (launcher != null)
        {
            launcher.SetDragEnabled(true);
            launcher.ResetForNewLaunch();
        }

        PlaneController planeController = airPlane != null ? airPlane.GetComponent<PlaneController>() : null;
        if (planeController != null)
        {
            planeController.InitializeDetachableParts();
            planeController.UseRampColliders();
        }
        else
            Debug.LogWarning("PlaneController not found on airplane object!");

        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
            uiManager.OnGameplayStarted();

        StartCoroutine(ResetAtStartFlag());
    }

    public IEnumerator TransitionToTarget(Transform target, float duration)
    {
        if (target == null)
        {
            Debug.LogWarning("Target transform is null!");
            yield break;
        }

        inTransition = true;
        float time = 0;
        Vector3 startingPos = mainCamera.transform.position;
        Quaternion startingRot = mainCamera.transform.rotation;

        while (time < duration)
        {
            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startingPos, target.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startingRot, target.rotation, t);

            time += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = target.position;
        mainCamera.transform.rotation = target.rotation;
        inTransition = false;
    }

    IEnumerator TransitionToPosition(Vector3 targetPosition, Quaternion targetRotation, float duration, System.Action onComplete = null)
    {
        inTransition = true;
        float time = 0;
        Vector3 startingPos = mainCamera.transform.position;
        Quaternion startingRot = mainCamera.transform.rotation;
        MainMenu.SetActive(false);

        while (time < duration)
        {
            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startingPos, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startingRot, targetRotation, t);

            time += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        inTransition = false;

        // Invoke the callback if it exists
        onComplete?.Invoke();
    }

    IEnumerator ResetAtStartFlag()
    {
        // Wait for one frame.
        yield return null;
        Atstart = false;
    }
}
