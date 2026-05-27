using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public Transform mainMenuPosition;
    public Transform startPosition;
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
        if (mainMenuPosition != null)
        {
            mainCamera.transform.position = mainMenuPosition.position;
            mainCamera.transform.rotation = mainMenuPosition.rotation;
        }
        airPlane.gameObject.GetComponent<Collider>().enabled = false;
    }

    public void TransitionToStartCamPos(System.Action onComplete = null)
    {
        if (inTransition || startPosition == null)
            return;

        Debug.Log("Transitioning to start position");
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
        Debug.Log("Transitioning to main menu position");
        StartCoroutine(TransitionToPosition(mainMenuPosition.position, mainMenuPosition.rotation, transitionDuration, onComplete));
    }

    private void BeginGameplay()
    {
        gameStarted = true;
        Atstart = true;

        if (GUIcanvas != null)
            GUIcanvas.SetActive(true);

        if (airPlane != null)
            airPlane.GetComponent<Collider>().enabled = true;

        PlaneController planeController = airPlane != null ? airPlane.GetComponent<PlaneController>() : null;
        if (planeController != null)
            planeController.InitializeDetachableParts();
        else
            Debug.LogWarning("PlaneController not found on airplane object!");

        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
            uiManager.OnGameplayStarted();

        Debug.Log("Gameplay started, in-game UI enabled.");
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
