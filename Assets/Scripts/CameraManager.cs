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

    void Update()
    {
        if (!gameStarted && transform.position == startPosition.position)
        {
            gameStarted = true;
            Atstart = true; // Set for any other script that might need it for one frame.
            GUIcanvas.SetActive(true);
            airPlane.gameObject.GetComponent<Collider>().enabled = true;
            Debug.Log("Game started, plane collider enabled.");

            // Initialize detachable parts now that the plane is fully set up
            PlaneController planeController = airPlane.GetComponent<PlaneController>();
            if (planeController != null)
            {
                planeController.InitializeDetachableParts();
            }
            else
            {
                Debug.LogWarning("PlaneController not found on airplane object!");
            }

            StartCoroutine(ResetAtStartFlag());
        }

        
    }


    public void TransitionToMainMenu()
    {
        if ( !inTransition)
        {
            Debug.Log("Transitioning to start position");
            if (startPosition != null)
            {
                StartCoroutine(TransitionToPosition(startPosition.position, startPosition.rotation, transitionDuration));
            }
        }
    }

    IEnumerator TransitionToPosition(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        inTransition = true;
        float time = 0;
        Vector3 startingPos = mainCamera.transform.position;
        Quaternion startingRot = mainCamera.transform.rotation;
        MainMenu.SetActive(false);

        while (time < duration)
        {
            // Calculate the interpolation factor (0 to 1)
            float t = time / duration;

            // Apply a smooth step curve for ease-in and ease-out
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startingPos, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startingRot, targetRotation, t);

            time += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        inTransition = false;
    }

    IEnumerator ResetAtStartFlag()
    {
        // Wait for one frame.
        yield return null;
        Atstart = false;
    }
}
