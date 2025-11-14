using UnityEngine;
using System.Collections;

public class SimpleDragLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform cube;
    public Transform restingPoint;
    public Camera cam;
    [Tooltip("Optional reference to the DragRotationHandler script")]
    public DragRotationHandler rotationHandler;
    public RubberBandVisual lineRenderer;

    [Header("Settings")]
    public float maxDragDistance = 5f;
    public float minDragToLaunch = 1f; // Minimum drag distance to launch
    public float launchForceMultiplier = 10f;
    public float verticalForceMultiplier = 5f;
    public float liftDuration = 2f; // How long the lift lasts

    private Rigidbody cubeRb;
    private bool isDragging = false;
    private Vector3 dragStartPos;
    private bool isLifting = false;
    private float liftStartTime = 0f;
    private float originalDragDistance; // Store the drag distance for lift calculation

    public bool released = false;
    private Vector3 launchDir;

    [Header("Rubber SFX")]
    public AudioSource rubberSource;      // assign in inspector (or it will try to GetComponent)
    public AudioClip rubberClip;          // the looping rubber stretch clip
    public AudioSource windSource;
    [Range(0f, 1f)] public float minVolume = 0.05f;
    [Range(0f, 1f)] public float maxVolume = 0.9f;
    [Range(0.5f, 2f)] public float minPitch = 0.8f;
    [Range(0.5f, 2f)] public float maxPitch = 1.6f;
    public float fadeOutDuration = 0.2f;

    private Coroutine rubberFadeCoroutine;

    void Start()
    {
        if (!cam) cam = Camera.main;
        cubeRb = cube.GetComponent<Rigidbody>();
        cubeRb.isKinematic = true;
        cube.position = restingPoint.position;

        // Find the rotation handler if not assigned
        if (rotationHandler == null && cube != null)
        {
            rotationHandler = cube.GetComponent<DragRotationHandler>();
        }

        // Load saved launch force multiplier
        launchForceMultiplier = PlayerPrefs.GetFloat("LaunchForceMultiplier", launchForceMultiplier);

        // AudioSource fallback
        if (rubberSource == null)
        {
            rubberSource = GetComponent<AudioSource>();
        }
        if (rubberSource != null)
        {
            rubberSource.playOnAwake = false;
            rubberSource.loop = true; // we want it to loop while dragging
            if (rubberClip != null)
                rubberSource.clip = rubberClip;
            rubberSource.volume = minVolume;
            rubberSource.pitch = minPitch;
        }
    }

    void Update()
    {
        if (released == true) return;
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == cube)
                {
                    isDragging = true;
                    AndroidVibrations.StartContinuous(); // 🔸 start vibration

                    // Start rubber sound
                    StartRubberSound();
                }
            }
        }

        if (isDragging)
        {
            if (Input.GetMouseButton(0))
                DragCube();

            if (Input.GetMouseButtonUp(0))
            {
                AndroidVibrations.Stop(); // 🔸 stop vibration
                ReleaseCube();
            }
        }
    }

    void FixedUpdate()
    {
        if (isLifting)
        {
            float elapsed = Time.time - liftStartTime;

            if (elapsed < liftDuration)
            {
                // Gradually apply lift force over time
                Vector3 currentVelocity = cubeRb.velocity;

                // Use launch direction if velocity is too small
                Vector3 forwardDir = currentVelocity.magnitude > 0.1f ? currentVelocity.normalized : launchDir;

                // Lift direction becomes more forward as we gain speed
                float forwardInfluence = Mathf.Clamp01(elapsed / liftDuration);
                Vector3 liftDirection = (Vector3.up * (1f - forwardInfluence) + forwardDir * forwardInfluence).normalized;

                // Calculate force - INCREASED FORCE and removed Time.fixedDeltaTime for stronger impulse
                float verticalForce = originalDragDistance * verticalForceMultiplier * (1f - elapsed / liftDuration);

                cubeRb.AddForce(liftDirection * verticalForce, ForceMode.Force);

                Debug.Log($"Lifting... Elapsed: {elapsed:F2}, Force: {verticalForce:F2}, Direction: {liftDirection}");
            }
            else
            {
                isLifting = false;
                Debug.Log("Lift completed");
            }
        }
    }

    void DragCube()
    {
        // Raycast to ground plane
        Plane ground = new Plane(Vector3.up, restingPoint.position);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);

            // Calculate vector from resting point to mouse position
            Vector3 toMouse = worldPos - restingPoint.position;

            // Project the vector onto the plane's forward direction (z-axis of the resting point)
            Vector3 forward = restingPoint.forward;
            float dotProduct = Vector3.Dot(toMouse, forward);

            // Check if there's change in x-axis but not in z-axis (sideways drag)
            bool isSidewaysDrag = Mathf.Abs(toMouse.x) > 0.01f && Mathf.Abs(toMouse.z) < 0.01f;

            // If dragging forward or sideways, keep at resting position
            if (dotProduct > 0 || isSidewaysDrag)
            {
                // Return to resting position
                worldPos = restingPoint.position;
            }

            Vector3 dragVector = worldPos - restingPoint.position;

            float cubeHeight = cube.localScale.y * 0.5f;
            Vector3 currentPos = cube.position;
            currentPos.y = Mathf.Max(currentPos.y, cubeHeight + 0.1f);
            cube.position = currentPos;

            // Clamp distance
            if (dragVector.magnitude > maxDragDistance)
                dragVector = dragVector.normalized * maxDragDistance;

            // Move cube opposite direction of launch (dragging backward)
            cube.position = restingPoint.position + dragVector;

            // Update rubber SFX intensity based on how far back we are (0..1)
            UpdateRubberSfx(dragVector.magnitude / maxDragDistance);
        }
    }

    void ReleaseCube()
    {
        isDragging = false;

        // Fade out rubber sound on release
        StopRubberSound();
        windSource.Play();

        Vector3 dragVector = cube.position - restingPoint.position;
        float dragDistance = dragVector.magnitude;

        // Check if drag distance is sufficient
        if (dragDistance > minDragToLaunch)
        {
            // Launch the cube
            cubeRb.isKinematic = false;
            cubeRb.useGravity = true;

            originalDragDistance = dragDistance; // Store for lift calculation

            float horizontalForce = dragDistance * launchForceMultiplier;
            launchDir = (restingPoint.position - cube.position).normalized;
            launchDir.y = 0;
            launchDir = launchDir.normalized;

            cubeRb.AddForce(launchDir * horizontalForce, ForceMode.Impulse);

            if (rotationHandler != null)
            {
                rotationHandler.SetLaunchRotation();
            }

            released = true;
            lineRenderer.enabled = false;
            Debug.Log($"Launched! Drag: {dragDistance:F2}, Horizontal: {horizontalForce:F2}");
        }
        else
        {
            // Not enough drag, return to resting point
            StartCoroutine(ReturnToRest());
        }
    }

    private IEnumerator ReturnToRest()
    {
        Vector3 startPos = cube.position;
        float duration = 0.3f; // Quick animation back to start
        float elapsed = 0f;

        // Also fade out SFX while returning to rest
        if (rubberSource != null && rubberSource.isPlaying)
        {
            if (rubberFadeCoroutine != null) StopCoroutine(rubberFadeCoroutine);
            rubberFadeCoroutine = StartCoroutine(FadeRubberOut(fadeOutDuration));
        }

        while (elapsed < duration)
        {
            cube.position = Vector3.Lerp(startPos, restingPoint.position, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cube.position = restingPoint.position;
    }

    // ---------------- Rubber SFX helpers ----------------

    private void StartRubberSound()
    {
        if (rubberSource == null || rubberClip == null) return;

        if (rubberFadeCoroutine != null)
        {
            StopCoroutine(rubberFadeCoroutine);
            rubberFadeCoroutine = null;
        }

        if (!rubberSource.isPlaying)
        {
            rubberSource.loop = true;
            rubberSource.volume = minVolume;
            rubberSource.pitch = minPitch;
            rubberSource.Play();
        }
    }

    private void StopRubberSound()
    {
        if (rubberSource == null) return;

        // fade out
        if (rubberFadeCoroutine != null) StopCoroutine(rubberFadeCoroutine);
        rubberFadeCoroutine = StartCoroutine(FadeRubberOut(fadeOutDuration));
    }

    private IEnumerator FadeRubberOut(float duration)
    {
        if (rubberSource == null) yield break;

        float startVol = rubberSource.volume;
        float startPitch = rubberSource.pitch;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            rubberSource.volume = Mathf.Lerp(startVol, 0f, a);
            rubberSource.pitch  = Mathf.Lerp(startPitch, minPitch, a); // pitch returns to min while fading
            yield return null;
        }
        rubberSource.Stop();
        rubberSource.volume = minVolume;
        rubberSource.pitch  = minPitch;
        rubberFadeCoroutine = null;
    }

    /// <summary>
    /// Update the rubber SFX intensity based on normalized distance [0..1]
    /// </summary>
    /// <param name="normalized">0..1</param>
    private void UpdateRubberSfx(float normalized)
    {
        if (rubberSource == null || rubberClip == null) return;

        normalized = Mathf.Clamp01(normalized);
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, normalized);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, normalized);

        // Smooth it a bit to avoid abrupt jumps
        rubberSource.volume = Mathf.Lerp(rubberSource.volume, targetVolume, 0.2f);
        rubberSource.pitch  = Mathf.Lerp(rubberSource.pitch, targetPitch, 0.2f);
    }

    // ----------------------------------------------------

    void OnDrawGizmos()
    {
        if (!restingPoint) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(restingPoint.position, maxDragDistance);

        // Visualize the minimum launch distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(restingPoint.position, minDragToLaunch);
    }
}
