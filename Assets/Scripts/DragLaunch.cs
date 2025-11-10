using UnityEngine;

public class SimpleDragLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform cube;
    public Transform restingPoint;
    public Camera cam;
    [Tooltip("Optional reference to the DragRotationHandler script")]
    public DragRotationHandler rotationHandler;
  //  public CameraManager cameraManager;
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

    }

    void Update()
    {
        if(released == true)return; 
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
                float verticalForce = originalDragDistance * verticalForceMultiplier * (1f - elapsed/liftDuration);
                
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
        }
    }

    void ReleaseCube()
    {
        isDragging = false;

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

    private System.Collections.IEnumerator ReturnToRest()
    {
        Vector3 startPos = cube.position;
        float duration = 0.3f; // Quick animation back to start
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cube.position = Vector3.Lerp(startPos, restingPoint.position, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cube.position = restingPoint.position;
    }

    // void OnTriggerEnter(Collider other) //this is for vertical lift 
    // {
    //     if (released && !cubeRb.isKinematic && !isLifting)
    //     {
    //         isLifting = true;
    //         liftStartTime = Time.time;
    //         Debug.Log($"Lift started! Duration: {liftDuration}s, Drag Distance: {originalDragDistance:F2}");
    //     }
    // }

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
