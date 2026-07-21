using System.Collections;
using UnityEngine;

/// <summary>
/// Camera pose transitions (menu / start / upgrade focus). Session enable uses GameFlow.
/// Serialized airPlane / canvas refs stay here so existing scene wiring keeps working.
/// </summary>
public class CameraTransitionController : MonoBehaviour
{
    public Transform mainMenuPosition;
    public Transform startPosition;
    [Tooltip("Camera focus when upgrading the slingshot. Falls back to SlingshotCamPos in scene, then startPosition.")]
    public Transform slingshotCameraPosition;
    public float transitionDuration = 2.0f;

    [Header("Session refs (used by GameFlow)")]
    public GameObject GUIcanvas;
    public GameObject MainMenu;
    public GameObject airPlane;

    private Camera mainCamera;
    private bool inTransition;

    void Start()
    {
        mainCamera = Camera.main;
        ResolveSlingshotCameraPosition();
        if (mainMenuPosition != null && mainCamera != null)
        {
            mainCamera.transform.position = mainMenuPosition.position;
            mainCamera.transform.rotation = mainMenuPosition.rotation;
        }

        if (airPlane != null)
        {
            Collider col = airPlane.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
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
            GameFlow.BeginGameplay(airPlane, GUIcanvas);
            onComplete?.Invoke();
        }));
    }

    public void TransitionToMainMenuCamPos(System.Action onComplete = null)
    {
        if (inTransition || mainMenuPosition == null)
            return;

        GameFlow.EndGameplay(airPlane);
        StartCoroutine(TransitionToPosition(mainMenuPosition.position, mainMenuPosition.rotation, transitionDuration, onComplete));
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

    private System.Collections.IEnumerator TransitionToPosition(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        System.Action onComplete = null)
    {
        inTransition = true;
        float time = 0;
        Vector3 startingPos = mainCamera.transform.position;
        Quaternion startingRot = mainCamera.transform.rotation;
        if (MainMenu != null)
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
        onComplete?.Invoke();
    }
}
