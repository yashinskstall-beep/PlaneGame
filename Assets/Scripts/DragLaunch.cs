using UnityEngine;

public class SimpleDragLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform cube;
    public Transform restingPoint;
    public Camera cam;
    [Tooltip("Optional reference to the DragRotationHandler script")]
    public DragRotationHandler rotationHandler;

    [Header("Settings")]
    public float maxDragDistance = 5f;
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

    void HandleInput(){
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse button down");
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                 if (hit.transform == cube)
                {
                    isDragging = true;
                }
            }
        }

        if (isDragging)
        {
            if (Input.GetMouseButton(0))
                DragCube();

            if (Input.GetMouseButtonUp(0))
                ReleaseCube();
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
        cubeRb.isKinematic = false;
        cubeRb.useGravity = true;

        float cubeHeight = cube.localScale.y * 0.5f;
        Vector3 currentPos = cube.position;
        currentPos.y = Mathf.Max(currentPos.y, cubeHeight + 0.1f);
        cube.position = currentPos;
                
        // Calculate drag vector
        Vector3 dragVector = cube.position - restingPoint.position;
        float dragDistance = dragVector.magnitude;
        originalDragDistance = dragDistance; // Store for lift calculation

        // Horizontal launch force (along +Z)
        float horizontalForce = dragDistance * launchForceMultiplier;

        // Launch direction (from drag vector towards resting point)
        launchDir = (restingPoint.position - cube.position).normalized;

        // Only horizontal component from direction, Y handled separately
        launchDir.y = 0;
        launchDir = launchDir.normalized;

        // Apply the forces
        cubeRb.AddForce(launchDir * horizontalForce, ForceMode.Impulse);
       
        // Set the plane's rotation based on drag direction
        if (rotationHandler != null)
        {
            rotationHandler.SetLaunchRotation();
        }
        
        released = true;

        Debug.Log($"Launched! Drag: {dragDistance:F2}, Horizontal: {horizontalForce:F2}");
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
    }
}
