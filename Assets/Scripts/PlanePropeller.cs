using UnityEngine;

/// <summary>
/// Spins a propeller from slingshot pull strength, keeps spinning after launch,
/// and winds down with sound on crash. Attach to the desert airplane (or any plane with a prop).
/// </summary>
public class PlanePropeller : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The propeller mesh/transform to rotate. Leave empty to auto-find a child named Propeller/Prop.")]
    public Transform propeller;

    [Tooltip("Drag launcher used to read pull amount. Leave empty to find on this object/parents.")]
    public SimpleDragLauncher dragLauncher;

    [Tooltip("Optional. Used to detect crash / wreck state.")]
    public PlaneController planeController;

    [Header("Spin")]
    [Tooltip("Local axis the propeller spins around. Leave zero to auto-pick the thinnest mesh axis (hub).")]
    public Vector3 localSpinAxis = Vector3.zero;

    [Tooltip("Unused — kept for Inspector compatibility. Pivot is never moved at runtime (avoids Play-mode snap).")]
    public bool recenterPivotOnHub = false;

    [Tooltip("Degrees per second at light pull.")]
    public float minSpinSpeed = 180f;

    [Tooltip("Degrees per second at full slingshot pull.")]
    public float maxSpinSpeed = 2200f;

    [Tooltip("How fast spin speed eases toward the target while dragging.")]
    public float spinAcceleration = 8f;

    [Tooltip("How long the propeller takes to stop after a crash.")]
    public float crashSpinDownDuration = 1.75f;

    [Header("Sound")]
    [Tooltip("Looping propeller/engine AudioSource. Created automatically if missing.")]
    public AudioSource propellerAudio;

    [Tooltip("Looping propeller clip. Assign in Inspector (Desert plane).")]
    public AudioClip propellerClip;

    [Range(0f, 1f)] public float minVolume = 0.05f;
    [Range(0f, 1f)] public float maxVolume = 0.85f;
    [Range(0.5f, 2f)] public float minPitch = 0.75f;
    [Range(0.5f, 2f)] public float maxPitch = 1.45f;

    [Tooltip("How quickly volume/pitch ease toward the target.")]
    public float audioSmoothing = 6f;

    [Tooltip("Respect SettingsManager audio mute.")]
    public bool respectAudioSettings = true;

    [Header("Debug")]
    [Tooltip("Log propeller spin diagnostics to the Console.")]
    public bool debugPropeller = false;

    [Tooltip("Seconds between throttled propeller debug logs.")]
    public float debugLogInterval = 0.25f;

    [Tooltip("While Play is running: pause spin and record PropellerPivot + mesh position/rotation whenever you move them in Scene view. Writes Assets/Debug/propeller_manual_pose.txt")]
    public bool recordManualPose = false;

    private float currentSpinSpeed;
    private float targetSpinSpeed;
    private float crashSpinStartSpeed;
    private float crashSpinDownTimer = -1f;
    private bool hasLaunched;
    private bool isCrashing;
    private float lastPull01;
    private float nextDebugLogTime;
    private bool loggedStartup;
    private bool hubSetupDone;
    private string lastState = "none";
    private string lastLoggedState = "none";
    private float accumulatedDeltaAngle;
    private float lastAppliedDelta;
    private float lastAppliedAngle;
    private Vector3 lastEulerDelta;
    private Renderer propRenderer;
    private Vector3 lastRecordedPivotLocalPos;
    private Quaternion lastRecordedPivotLocalRot;
    private Vector3 lastRecordedMeshLocalPos;
    private Quaternion lastRecordedMeshLocalRot;
    private bool hasRecordedBaseline;
    private string poseLogPath;
    private int poseSampleIndex;
    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private bool hasRestPose;

    void Awake()
    {
        ResolveReferences();
        CacheRestPose();
        if (!recordManualPose)
            SetupHubAndAxis();
        EnsureAudioSource();
        LogStartup("Awake");
        if (recordManualPose)
            BeginManualPoseRecording();
    }

    void OnEnable()
    {
        ResetPropeller();
        loggedStartup = false;
    }

    void Update()
    {
        ResolveReferences();

        if (recordManualPose)
        {
            if (!loggedStartup)
                LogStartup("Update");
            RecordManualPoseIfChanged();
            return;
        }

        SetupHubAndAxis();

        if (!loggedStartup)
            LogStartup("Update");

        if (propeller == null)
        {
            LogPropDebugThrottled("MISSING_PROPELLER — assign Propeller/PropellerPivot in Inspector");
            return;
        }

        UpdateSpinTarget();
        ApplySpin();
        UpdateAudio();
        LogRuntimeState();
    }

    /// <summary>
    /// Call when the flight ends in a crash / landing so the prop winds down.
    /// </summary>
    public void NotifyCrash()
    {
        if (isCrashing)
            return;

        isCrashing = true;
        hasLaunched = false;
        crashSpinStartSpeed = Mathf.Max(currentSpinSpeed, targetSpinSpeed);
        crashSpinDownTimer = 0f;
        targetSpinSpeed = 0f;
        LogPropDebug(
            $"CRASH_START spinWas={crashSpinStartSpeed:F0} " +
            $"prop='{FormatTransform(propeller)}' axis={localSpinAxis}");
    }

    /// <summary>
    /// Call when resetting the plane for a new launch.
    /// </summary>
    public void ResetPropeller()
    {
        isCrashing = false;
        hasLaunched = false;
        crashSpinDownTimer = -1f;
        currentSpinSpeed = 0f;
        targetSpinSpeed = 0f;
        lastPull01 = 0f;
        accumulatedDeltaAngle = 0f;
        RestoreRestPose();
        StopAudioImmediate();
        LogPropDebug("RESET");
    }

    private void CacheRestPose()
    {
        if (propeller == null || hasRestPose)
            return;

        restLocalPosition = propeller.localPosition;
        restLocalRotation = propeller.localRotation;
        hasRestPose = true;
    }

    private void RestoreRestPose()
    {
        if (propeller == null || !hasRestPose || recordManualPose)
            return;

        propeller.localPosition = restLocalPosition;
        propeller.localRotation = restLocalRotation;
    }

    private void UpdateSpinTarget()
    {
        if (isCrashing)
        {
            if (crashSpinDownTimer < 0f)
                crashSpinDownTimer = 0f;

            crashSpinDownTimer += Time.deltaTime;
            float t = crashSpinDownDuration <= 0.01f
                ? 1f
                : Mathf.Clamp01(crashSpinDownTimer / crashSpinDownDuration);

            // Ease-out so it feels like blades losing energy.
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            currentSpinSpeed = Mathf.Lerp(crashSpinStartSpeed, 0f, eased);
            targetSpinSpeed = currentSpinSpeed;
            lastState = $"crash t={t:F2}";

            if (t >= 1f)
            {
                currentSpinSpeed = 0f;
                targetSpinSpeed = 0f;
                lastState = "crash_done";
            }

            return;
        }

        if (planeController != null && planeController.IsWreckPhysicsActive)
        {
            NotifyCrash();
            return;
        }

        bool dragging = dragLauncher != null && dragLauncher.IsDragging;
        bool released = dragLauncher != null && dragLauncher.released;

        if (dragging)
        {
            hasLaunched = false;
            lastPull01 = dragLauncher.GetPullNormalized();
            targetSpinSpeed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, lastPull01);
            lastState = $"dragging pull={lastPull01:F2}";
        }
        else if (released || hasLaunched)
        {
            // Keep the speed locked from the pull that launched the plane.
            hasLaunched = true;
            if (lastPull01 <= 0.001f && dragLauncher != null)
                lastPull01 = Mathf.Clamp01(dragLauncher.OriginalDragDistance / Mathf.Max(0.01f, dragLauncher.maxDragDistance));

            targetSpinSpeed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, Mathf.Max(lastPull01, 0.35f));
            lastState = $"flight pullLock={lastPull01:F2} released={released}";
        }
        else
        {
            targetSpinSpeed = 0f;
            lastPull01 = 0f;
            lastState = "idle";
        }

        currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, targetSpinSpeed, Time.deltaTime * spinAcceleration);
        if (targetSpinSpeed <= 0.01f && currentSpinSpeed < 5f)
            currentSpinSpeed = 0f;
    }

    private void ApplySpin()
    {
        if (Mathf.Abs(currentSpinSpeed) < 0.01f)
            return;

        Vector3 axisLocal = localSpinAxis.sqrMagnitude > 0.0001f ? localSpinAxis.normalized : Vector3.forward;
        float delta = currentSpinSpeed * Time.deltaTime;
        Quaternion before = propeller.localRotation;

        // Rotate around mesh hub (verts are offset from Transform origin).
        // Does not relocate the prop at Awake — only rotates while spinning.
        if (propRenderer != null)
        {
            Vector3 hubWorld = propRenderer.bounds.center;
            Vector3 axisWorld = propeller.TransformDirection(axisLocal);
            propeller.RotateAround(hubWorld, axisWorld, delta);
        }
        else
        {
            propeller.Rotate(axisLocal, delta, Space.Self);
        }

        float applied = Quaternion.Angle(before, propeller.localRotation);
        accumulatedDeltaAngle += applied;

        lastAppliedDelta = delta;
        lastAppliedAngle = applied;
        Vector3 beforeEuler = before.eulerAngles;
        Vector3 afterEuler = propeller.localRotation.eulerAngles;
        lastEulerDelta = new Vector3(
            Mathf.DeltaAngle(beforeEuler.x, afterEuler.x),
            Mathf.DeltaAngle(beforeEuler.y, afterEuler.y),
            Mathf.DeltaAngle(beforeEuler.z, afterEuler.z));
    }

    private void UpdateAudio()
    {
        if (propellerAudio == null)
            return;

        bool audioAllowed = !respectAudioSettings || SettingsManager.IsAudioEnabled;
        float speed01 = Mathf.InverseLerp(0f, maxSpinSpeed, Mathf.Abs(currentSpinSpeed));
        bool shouldPlay = audioAllowed && speed01 > 0.02f;

        if (shouldPlay)
        {
            if (propellerClip != null && propellerAudio.clip != propellerClip)
                propellerAudio.clip = propellerClip;

            if (propellerClip != null && !propellerAudio.isPlaying)
            {
                propellerAudio.loop = true;
                propellerAudio.Play();
                LogPropDebug($"AUDIO_PLAY clip='{propellerClip.name}'");
            }

            float targetVolume = Mathf.Lerp(minVolume, maxVolume, speed01);
            float targetPitch = Mathf.Lerp(minPitch, maxPitch, speed01);
            float audioT = Time.deltaTime * audioSmoothing;
            propellerAudio.volume = Mathf.Lerp(propellerAudio.volume, targetVolume, audioT);
            propellerAudio.pitch = Mathf.Lerp(propellerAudio.pitch, targetPitch, audioT);
            propellerAudio.mute = false;
        }
        else if (propellerAudio.isPlaying)
        {
            float audioT = Time.deltaTime * audioSmoothing;
            propellerAudio.volume = Mathf.Lerp(propellerAudio.volume, 0f, audioT);
            if (propellerAudio.volume < 0.02f)
                StopAudioImmediate();
        }
    }

    private void StopAudioImmediate()
    {
        if (propellerAudio == null)
            return;

        if (propellerAudio.isPlaying)
            LogPropDebug("AUDIO_STOP");

        propellerAudio.Stop();
        propellerAudio.volume = minVolume;
        propellerAudio.pitch = minPitch;
    }

    private void ResolveReferences()
    {
        if (planeController == null)
            planeController = GetComponent<PlaneController>() ?? GetComponentInParent<PlaneController>();

        if (dragLauncher == null)
        {
            dragLauncher = GetComponent<SimpleDragLauncher>()
                ?? GetComponentInParent<SimpleDragLauncher>()
                ?? FindObjectOfType<SimpleDragLauncher>();
        }

        if (propeller == null)
            propeller = FindPropellerTransform();

        if (propRenderer == null && propeller != null)
            propRenderer = propeller.GetComponentInChildren<Renderer>();
    }

    /// <summary>
    /// Detect spin axis only. Never moves PropellerPivot / mesh transforms
    /// (moving them caused a visible snap the moment Play started).
    /// </summary>
    private void SetupHubAndAxis()
    {
        if (hubSetupDone || propeller == null)
            return;

        propRenderer = propeller.GetComponentInChildren<Renderer>();
        if (propRenderer == null)
        {
            if (localSpinAxis.sqrMagnitude < 0.0001f)
                localSpinAxis = Vector3.forward;
            hubSetupDone = true;
            return;
        }

        if (localSpinAxis.sqrMagnitude < 0.0001f)
        {
            Vector3 meshLocalAxis = DetectHubAxisLocal(propRenderer);
            Vector3 worldAxis = propRenderer.transform.TransformDirection(meshLocalAxis);
            localSpinAxis = propeller.InverseTransformDirection(worldAxis);
            if (localSpinAxis.sqrMagnitude < 0.0001f)
                localSpinAxis = Vector3.forward;
            else
                localSpinAxis.Normalize();
        }

        LogPropDebug(
            $"HUB_SETUP (no move) pivotPos={FormatVec(propeller.position)} " +
            $"pivotLocalPos={FormatVec(propeller.localPosition)} " +
            $"meshCenter={FormatVec(propRenderer.bounds.center)} " +
            $"axis={localSpinAxis} " +
            $"distPivotToHub={Vector3.Distance(propeller.position, propRenderer.bounds.center):F3}");

        hubSetupDone = true;
    }

    private static Vector3 DetectHubAxisLocal(Renderer meshRenderer)
    {
        // Thinnest local-bounds axis is usually the propeller hub.
        Bounds lb = meshRenderer.localBounds;
        Vector3 size = lb.size;
        if (size.x <= size.y && size.x <= size.z)
            return Vector3.right;
        if (size.y <= size.x && size.y <= size.z)
            return Vector3.up;
        return Vector3.forward;
    }

    private Transform FindPropellerTransform()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        Transform namedPivot = null;
        Transform namedMesh = null;
        Transform namedAny = null;

        foreach (Transform child in children)
        {
            if (child == null || child == transform)
                continue;

            string n = child.name;
            bool isPropName =
                n.IndexOf("propeller", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.Equals("Prop", System.StringComparison.OrdinalIgnoreCase)
                || n.IndexOf("Prop_", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("rotor", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isPropName)
                continue;

            // Ignore camera-focus markers like PropellerPos (no mesh, not a pivot).
            if (n.IndexOf("PropellerPos", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            if (n.IndexOf("pivot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                namedPivot = child;
            else if (child.GetComponent<MeshFilter>() != null || child.GetComponent<Renderer>() != null)
                namedMesh ??= child;
            else
                namedAny ??= child;
        }

        // Prefer a dedicated pivot so the blade spins around its hub.
        return namedPivot != null ? namedPivot : (namedMesh != null ? namedMesh : namedAny);
    }

    private void EnsureAudioSource()
    {
        if (propellerAudio == null)
            propellerAudio = GetComponent<AudioSource>();

        if (propellerAudio == null)
            propellerAudio = gameObject.AddComponent<AudioSource>();

        propellerAudio.playOnAwake = false;
        propellerAudio.loop = true;
        propellerAudio.spatialBlend = 0f;
        if (propellerClip != null)
            propellerAudio.clip = propellerClip;
    }

    private void BeginManualPoseRecording()
    {
        string dir = System.IO.Path.Combine(Application.dataPath, "Debug");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        poseLogPath = System.IO.Path.Combine(dir, "propeller_manual_pose.txt");
        poseSampleIndex = 0;
        hasRecordedBaseline = false;

        string header =
            $"=== Propeller manual pose recording started {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n" +
            "Spin is PAUSED. In Scene view, move/rotate PropellerPivot and/or Propeller mesh.\n" +
            "Each change is appended below. Stop Play when done — I will read this file.\n\n";
        System.IO.File.WriteAllText(poseLogPath, header);

        Debug.Log(
            $"[Propeller] MANUAL RECORD ON — spin paused. Move PropellerPivot/Propeller in Scene view. " +
            $"Writing: Assets/Debug/propeller_manual_pose.txt",
            this);

        RecordManualPoseIfChanged(force: true);
    }

    private void RecordManualPoseIfChanged(bool force = false)
    {
        if (propeller == null)
            return;

        Transform meshT = null;
        if (propRenderer == null)
            propRenderer = propeller.GetComponentInChildren<Renderer>();
        if (propRenderer != null)
            meshT = propRenderer.transform;

        Vector3 pivotLocalPos = propeller.localPosition;
        Quaternion pivotLocalRot = propeller.localRotation;
        Vector3 meshLocalPos = meshT != null ? meshT.localPosition : Vector3.zero;
        Quaternion meshLocalRot = meshT != null ? meshT.localRotation : Quaternion.identity;

        bool changed = force || !hasRecordedBaseline
            || (pivotLocalPos - lastRecordedPivotLocalPos).sqrMagnitude > 1e-8f
            || Quaternion.Angle(pivotLocalRot, lastRecordedPivotLocalRot) > 0.05f
            || (meshT != null && (
                (meshLocalPos - lastRecordedMeshLocalPos).sqrMagnitude > 1e-8f
                || Quaternion.Angle(meshLocalRot, lastRecordedMeshLocalRot) > 0.05f));

        if (!changed)
            return;

        lastRecordedPivotLocalPos = pivotLocalPos;
        lastRecordedPivotLocalRot = pivotLocalRot;
        lastRecordedMeshLocalPos = meshLocalPos;
        lastRecordedMeshLocalRot = meshLocalRot;
        hasRecordedBaseline = true;
        poseSampleIndex++;

        string block =
            $"--- sample #{poseSampleIndex} t={Time.time:F2} ---\n" +
            $"PropellerPivot path={GetPath(propeller)}\n" +
            $"  localPos={FormatVecPrecise(pivotLocalPos)}\n" +
            $"  localEuler={FormatEulerPrecise(propeller.localEulerAngles)}\n" +
            $"  localRotQuat={FormatQuat(pivotLocalRot)}\n" +
            $"  worldPos={FormatVecPrecise(propeller.position)}\n" +
            $"  worldEuler={FormatEulerPrecise(propeller.eulerAngles)}\n" +
            $"  worldFwd={FormatVecPrecise(propeller.forward)} up={FormatVecPrecise(propeller.up)} right={FormatVecPrecise(propeller.right)}\n";

        if (meshT != null)
        {
            block +=
                $"PropellerMesh path={GetPath(meshT)} name='{meshT.name}'\n" +
                $"  localPos={FormatVecPrecise(meshLocalPos)}\n" +
                $"  localEuler={FormatEulerPrecise(meshT.localEulerAngles)}\n" +
                $"  localRotQuat={FormatQuat(meshLocalRot)}\n" +
                $"  worldPos={FormatVecPrecise(meshT.position)}\n" +
                $"  worldEuler={FormatEulerPrecise(meshT.eulerAngles)}\n" +
                $"  boundsCenter={FormatVecPrecise(propRenderer.bounds.center)}\n" +
                $"  boundsSize={FormatVecPrecise(propRenderer.bounds.size)}\n" +
                $"  pivotToHubDist={Vector3.Distance(propeller.position, propRenderer.bounds.center):F4}\n";
        }

        block += "\n";

        try
        {
            System.IO.File.AppendAllText(poseLogPath, block);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Propeller] Failed to write pose log: {e.Message}", this);
        }

        Debug.Log(
            $"[Propeller] MANUAL_POSE #{poseSampleIndex} " +
            $"pivotLocalPos={FormatVecPrecise(pivotLocalPos)} " +
            $"pivotLocalEuler={FormatEulerPrecise(propeller.localEulerAngles)} " +
            $"meshLocalPos={(meshT != null ? FormatVecPrecise(meshLocalPos) : "n/a")} " +
            $"meshLocalEuler={(meshT != null ? FormatEulerPrecise(meshT.localEulerAngles) : "n/a")}",
            this);
    }

    private static string FormatVecPrecise(Vector3 v)
    {
        return $"({v.x:F5}, {v.y:F5}, {v.z:F5})";
    }

    private static string FormatEulerPrecise(Vector3 euler)
    {
        return $"({euler.x:F3}, {euler.y:F3}, {euler.z:F3})";
    }

    private static string FormatQuat(Quaternion q)
    {
        return $"({q.x:F5}, {q.y:F5}, {q.z:F5}, {q.w:F5})";
    }

    private void LogStartup(string phase)
    {
        if (!debugPropeller || loggedStartup)
            return;

        loggedStartup = true;
        string propInfo = propeller != null
            ? $"{FormatTransform(propeller)} parent='{(propeller.parent != null ? propeller.parent.name : "null")}' " +
              $"hasMesh={(propeller.GetComponent<MeshFilter>() != null || propeller.GetComponent<Renderer>() != null)} " +
              $"childCount={propeller.childCount} " +
              $"localEuler={FormatEuler(propeller.localEulerAngles)} " +
              $"lossyScale={FormatVec(propeller.lossyScale)} " +
              $"fwd={FormatVec(propeller.forward)} up={FormatVec(propeller.up)} right={FormatVec(propeller.right)}"
            : "NULL";

        LogPropDebug(
            $"STARTUP[{phase}] prop={propInfo} " +
            $"axis={localSpinAxis} " +
            $"launcher={(dragLauncher != null ? dragLauncher.name : "null")} " +
            $"plane={(planeController != null ? planeController.name : "null")} " +
            $"clip={(propellerClip != null ? propellerClip.name : "null")} " +
            $"audio={(propellerAudio != null ? "ok" : "null")}");

        if (propeller != null)
            accumulatedDeltaAngle = 0f;
    }

    private void LogRuntimeState()
    {
        if (!debugPropeller)
            return;

        bool stateChanged = lastState != lastLoggedState;
        bool active = Mathf.Abs(currentSpinSpeed) > 0.5f || isCrashing || hasLaunched
            || (dragLauncher != null && dragLauncher.IsDragging);

        // Idle menu spam was drowning the console — only tick while active or on state change.
        if (!active && !stateChanged)
            return;

        if (!stateChanged && Time.time < nextDebugLogTime)
            return;

        nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
        lastLoggedState = lastState;

        bool dragging = dragLauncher != null && dragLauncher.IsDragging;
        bool released = dragLauncher != null && dragLauncher.released;
        float pullNow = dragLauncher != null ? dragLauncher.GetPullNormalized() : -1f;

        LogPropDebug(
            $"TICK state={lastState} dragging={dragging} released={released} launched={hasLaunched} crashing={isCrashing} " +
            $"pullNow={pullNow:F2} pullLock={lastPull01:F2} " +
            $"spin={currentSpinSpeed:F0}/{targetSpinSpeed:F0} cumAngle={accumulatedDeltaAngle:F1} " +
            $"axis={localSpinAxis} applyΔ={lastAppliedDelta:F2} appliedAngle={lastAppliedAngle:F2} " +
            $"eulerΔ=({lastEulerDelta.x:F1},{lastEulerDelta.y:F1},{lastEulerDelta.z:F1}) " +
            $"prop='{(propeller != null ? propeller.name : "null")}' " +
            $"localEuler={FormatEuler(propeller != null ? propeller.localEulerAngles : Vector3.zero)} " +
            $"worldFwd={FormatVec(propeller != null ? propeller.forward : Vector3.zero)} " +
            $"hubDist={(propRenderer != null && propeller != null ? Vector3.Distance(propeller.position, propRenderer.bounds.center).ToString("F3") : "n/a")} " +
            $"wreck={(planeController != null && planeController.IsWreckPhysicsActive)}");

        lastAppliedDelta = 0f;
        lastAppliedAngle = 0f;
        lastEulerDelta = Vector3.zero;
    }

    private void LogPropDebug(string message)
    {
        if (!debugPropeller)
            return;
        Debug.Log($"[Propeller] t={Time.time:F2} {message}", this);
    }

    private void LogPropDebugThrottled(string message)
    {
        if (!debugPropeller)
            return;
        if (Time.time < nextDebugLogTime)
            return;
        nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
        Debug.Log($"[Propeller] t={Time.time:F2} {message}", this);
    }

    private static string FormatTransform(Transform t)
    {
        return t != null ? $"'{t.name}' path={GetPath(t)}" : "null";
    }

    private static string GetPath(Transform t)
    {
        if (t == null)
            return "";
        string path = t.name;
        Transform p = t.parent;
        int guard = 0;
        while (p != null && guard++ < 12)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }

    private static string FormatEuler(Vector3 euler)
    {
        return $"({euler.x:F1},{euler.y:F1},{euler.z:F1})";
    }

    private static string FormatVec(Vector3 v)
    {
        return $"({v.x:F2},{v.y:F2},{v.z:F2})";
    }
}
