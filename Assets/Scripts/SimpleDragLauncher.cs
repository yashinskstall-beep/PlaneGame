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
    [Tooltip("While on the ramp, aim the launch along the ramp surface instead of flattening Y. Stops high upgrades from skipping the ramp.")]
    public bool aimLaunchAlongRamp = true;

    [Header("Post-Launch Climb")]
    [Tooltip("After leaving the ramp, gently lift the plane for more height. No instant upward impulse.")]
    public bool enablePostLaunchLift = true;
    [Tooltip("Climb strength scaled by pull distance. Higher = more altitude.")]
    public float verticalForceMultiplier = 14f;
    [Tooltip("How long the smooth climb lasts after leaving the ramp.")]
    public float liftDuration = 2.75f;
    [Tooltip("Wait until the plane leaves the ramp before climbing, so lift does not fight the shed.")]
    public bool waitUntilOffRamp = true;

    private Rigidbody cubeRb;
    private bool isDragging = false;
    public bool IsDragging => isDragging;
    private Vector3 dragStartPos;
    private bool isLifting = false;
    private float liftStartTime = -1f;
    private float originalDragDistance; // Store the drag distance for lift calculation
    private CollisionDetectionMode cachedCollisionDetection = CollisionDetectionMode.Discrete;

    public bool released = false;
    private Vector3 launchDir;
    private bool dragEnabled;

    public bool DragEnabled => dragEnabled;
    /// <summary>Last successful launch pull distance (world units).</summary>
    public float OriginalDragDistance => originalDragDistance;

    /// <summary>Current slingshot pull amount from 0 (rest) to 1 (max drag).</summary>
    public float GetPullNormalized()
    {
        if (cube == null || restingPoint == null || maxDragDistance <= 0.01f)
            return 0f;

        float distance = Vector3.Distance(cube.position, restingPoint.position);
        return Mathf.Clamp01(distance / maxDragDistance);
    }

    public void SetDragEnabled(bool enabled)
    {
        dragEnabled = enabled;
        if (!enabled)
            ResetToRestingState();
    }

    public void ResetForNewLaunch()
    {
        ResetToRestingState();
    }

    public void ResetToRestingState()
    {
        StopAllCoroutines();
        rubberFadeCoroutine = null;

        released = false;
        isDragging = false;
        isLifting = false;
        liftStartTime = -1f;

        if (rubberSource != null && rubberSource.isPlaying)
            rubberSource.Stop();

        if (windSource != null && windSource.isPlaying)
            windSource.Stop();

        if (cubeRb != null)
        {
            if (!cubeRb.isKinematic)
            {
                cubeRb.velocity = Vector3.zero;
                cubeRb.angularVelocity = Vector3.zero;
            }

            cubeRb.collisionDetectionMode = cachedCollisionDetection;
            cubeRb.isKinematic = true;
            cubeRb.useGravity = false;
        }

        if (cube != null && restingPoint != null)
        {
            cube.position = restingPoint.position;
            cube.rotation = restingPoint.rotation;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = true;

        if (rotationHandler != null)
            rotationHandler.ResetToRestPose();

        PlaneEffects planeEffects = cube != null ? cube.GetComponent<PlaneEffects>() : null;
        if (planeEffects != null)
            planeEffects.RefreshFlightTrails();

        cube?.GetComponent<PlaneController>()?.UseRampColliders();
        cube?.GetComponentInChildren<PlanePropeller>(true)?.ResetPropeller();
    }

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
        cachedCollisionDetection = cubeRb.collisionDetectionMode;
        cubeRb.isKinematic = true;
        cube.position = restingPoint.position;

        // On ramp: root collider only until we leave the ramp.
        cube.GetComponent<PlaneController>()?.UseRampColliders();

        // Find the rotation handler if not assigned
        if (rotationHandler == null && cube != null)
        {
            rotationHandler = cube.GetComponent<DragRotationHandler>();
        }

        // Scene-scoped key (same as MainMenu / LevelProgress). Legacy global key as fallback.
        string sceneKey = LevelProgress.GetLaunchForceMultiplierKey();
        if (PlayerPrefs.HasKey(sceneKey))
            launchForceMultiplier = PlayerPrefs.GetFloat(sceneKey, launchForceMultiplier);
        else if (PlayerPrefs.HasKey("LaunchForceMultiplier"))
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
        if (!dragEnabled || released)
            return;

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
                    VibrationManager.Instance.StartContinuous(); // 🔸 start vibration (Android & iOS)

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
                VibrationManager.Instance.Stop(); // 🔸 stop vibration (Android & iOS)
                ReleaseCube();
            }
        }
    }

    void FixedUpdate()
    {
        if (!isLifting || cubeRb == null || cubeRb.isKinematic)
            return;

        // Hold climb until the plane is free of the ramp so contact projection cannot cancel lift.
        if (waitUntilOffRamp)
        {
            PlaneRampAligner rampAligner = cube != null ? cube.GetComponent<PlaneRampAligner>() : null;
            if (rampAligner != null && rampAligner.IsAligning)
                return;
        }

        if (liftStartTime < 0f)
            liftStartTime = Time.time;

        float elapsed = Time.time - liftStartTime;
        if (elapsed >= liftDuration)
        {
            isLifting = false;
            return;
        }

        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, liftDuration));
        // Smooth bell curve: zero at start/end, peak in the middle — no sudden shove.
        float envelope = Mathf.Sin(t * Mathf.PI);

        float strength = originalDragDistance * verticalForceMultiplier * envelope;
        if (strength <= 0.01f)
            return;

        Vector3 forwardDir = cubeRb.velocity.sqrMagnitude > 0.25f
            ? cubeRb.velocity.normalized
            : launchDir;
        if (forwardDir.sqrMagnitude < 0.0001f)
            forwardDir = Vector3.forward;
        // Keep climb biased upward early, then ease toward travel direction.
        float upBlend = Mathf.Lerp(0.9f, 0.4f, t);
        Vector3 liftDirection = (Vector3.up * upBlend + forwardDir * (1f - upBlend)).normalized;

        cubeRb.AddForce(liftDirection * strength, ForceMode.Acceleration);
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

        Vector3 dragVector = cube.position - restingPoint.position;
        float dragDistance = dragVector.magnitude;

        // Check if drag distance is sufficient
        if (dragDistance > minDragToLaunch)
        {
            // Launch the cube — still on ramp, so keep part MeshColliders off.
            cube.GetComponent<PlaneController>()?.UseRampColliders();
            cubeRb.isKinematic = false;
            cubeRb.useGravity = true;
            // High upgrade impulses skip Discrete contacts; Continuous keeps the plane on the ramp.
            cubeRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            originalDragDistance = dragDistance; // Store for lift calculation

            float horizontalForce = dragDistance * launchForceMultiplier;
            launchDir = (restingPoint.position - cube.position).normalized;
            launchDir = ResolveLaunchDirectionAlongRamp(launchDir);

            cubeRb.AddForce(launchDir * horizontalForce, ForceMode.Impulse);

            // Flat yaw snap fights the ramp pitch and helps strong launches skip the shed.
            if (rotationHandler != null && !aimLaunchAlongRamp)
                rotationHandler.SetLaunchRotation();

            released = true;
            lineRenderer.enabled = false;

            // Smooth altitude after ramp exit (not an instant upward impulse).
            if (enablePostLaunchLift)
            {
                isLifting = true;
                liftStartTime = waitUntilOffRamp ? -1f : Time.time;
            }
        }
        else
        {
            // Not enough drag, return to resting point
            StartCoroutine(ReturnToRest());
        }
    }

    private Vector3 ResolveLaunchDirectionAlongRamp(Vector3 desiredDir)
    {
        if (desiredDir.sqrMagnitude < 0.0001f)
            desiredDir = Vector3.forward;
        else
            desiredDir.Normalize();

        if (!aimLaunchAlongRamp || cube == null)
        {
            desiredDir.y = 0f;
            return desiredDir.sqrMagnitude > 0.0001f ? desiredDir.normalized : Vector3.forward;
        }

        PlaneRampAligner rampAligner = cube.GetComponent<PlaneRampAligner>();
        Transform ramp = rampAligner != null ? rampAligner.CurrentRamp : null;

        // Fallback: short ray under the plane for the shed / ramp surface.
        if (ramp == null && Physics.Raycast(cube.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3f))
        {
            if (rampAligner == null || rampAligner.IsRampTransform(hit.collider.transform))
                ramp = hit.collider.transform;
        }

        if (ramp != null)
        {
            Vector3 alongRamp = Vector3.ProjectOnPlane(desiredDir, ramp.up);
            if (alongRamp.sqrMagnitude > 0.0001f)
                return alongRamp.normalized;

            alongRamp = Vector3.ProjectOnPlane(ramp.forward, ramp.up);
            if (alongRamp.sqrMagnitude > 0.0001f)
                return alongRamp.normalized;
        }

        desiredDir.y = 0f;
        return desiredDir.sqrMagnitude > 0.0001f ? desiredDir.normalized : Vector3.forward;
    }

    private IEnumerator ReturnToRest()
    {
        Vector3 startPos = cube.position;
        Quaternion startRot = cube.rotation;
        Quaternion restRot = restingPoint != null ? restingPoint.rotation : startRot;
        float duration = 0.3f; // Quick animation back to start
        float elapsed = 0f;

        if (rotationHandler != null)
            rotationHandler.SetTargetToRestPose();

        // Also fade out SFX while returning to rest
        if (rubberSource != null && rubberSource.isPlaying)
        {
            if (rubberFadeCoroutine != null) StopCoroutine(rubberFadeCoroutine);
            rubberFadeCoroutine = StartCoroutine(FadeRubberOut(fadeOutDuration));
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cube.position = Vector3.Lerp(startPos, restingPoint.position, t);
            cube.rotation = Quaternion.Slerp(startRot, restRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cube.position = restingPoint.position;
        cube.rotation = restRot;
        if (rotationHandler != null)
            rotationHandler.ResetToRestPose();
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
