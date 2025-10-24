using UnityEngine;

/// <summary>
/// Handles plane rotation based on drag direction.
/// When dragging straight back, maintains current rotation.
/// When dragging back-left, rotates to face right.
/// When dragging back-right, rotates to face left.
/// Attach to the plane GameObject.
/// </summary>
public class DragRotationHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the SimpleDragLauncher script")]
    public SimpleDragLauncher dragLauncher;
    [Tooltip("Reference point that represents the center/rest position")]
    public Transform restPosition;
    
    [Header("Rotation Settings")]
    [Tooltip("How quickly the plane rotates to match the drag direction")]
    public float rotationSpeed = 10f;
    [Tooltip("Maximum rotation angle in degrees (each side from center)")]
    [Range(0, 90)]
    public float maxRotationAngle = 45f;
    [Tooltip("How sensitive the rotation is to sideways drag")]
    [Range(0.1f, 5f)]
    public float rotationSensitivity = 1f;
    
    // Private variables
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private bool isDragging = false;
    private Vector3 dragVector;
    
    void Start()
    {
        // Store initial rotation
        initialRotation = transform.rotation;
        targetRotation = initialRotation;
        
        // Find references if not assigned
        if (dragLauncher == null)
        {
            dragLauncher = FindObjectOfType<SimpleDragLauncher>();
            if (dragLauncher == null)
            {
                Debug.LogError("DragRotationHandler: SimpleDragLauncher reference not found!");
            }
        }
        
        if (restPosition == null && dragLauncher != null)
        {
            restPosition = dragLauncher.restingPoint;
        }
    }
    
    void Update()
    {
        // Check if we're currently dragging
        CheckDragging();
        
        // Calculate rotation based on drag direction
        if (isDragging)
        {
            CalculateRotation();
        }
        
        // Apply rotation smoothly
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
    
    void CheckDragging()
    {
        // Start dragging when mouse button is pressed down
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if we hit the plane object
                if (hit.transform == transform)
                {
                    isDragging = true;
                }
            }
        }
        
        // Continue dragging when mouse button is held down
        if (isDragging && Input.GetMouseButton(0))
        {
            UpdateDragVector();
        }
        
        // Stop dragging when mouse button is released
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
        }
    }
    
    void UpdateDragVector()
    {
        if (restPosition == null) return;
        
        // Create a plane at the rest position with normal facing up
        Plane dragPlane = new Plane(Vector3.up, restPosition.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        // Raycast against this invisible plane
        if (dragPlane.Raycast(ray, out float enter))
        {
            // Get the world position where the ray hits the plane
            Vector3 hitPoint = ray.GetPoint(enter);
            
            // Calculate drag vector from rest position to hit point
            dragVector = restPosition.position - hitPoint;
            
            // We only care about the horizontal components
            dragVector.y = 0;
        }
    }
    
    void CalculateRotation()
    {
        if (dragVector.magnitude < 0.1f) return;
        
        // Get the normalized drag direction
        Vector3 dragDirection = dragVector.normalized;
        
        // Calculate the angle between the drag direction and forward vector
        // This gives us the sideways component of the drag
        Vector3 forwardDir = Vector3.forward;
        float angle = Vector3.SignedAngle(dragDirection, forwardDir, Vector3.up);
        
        // Adjust angle based on sensitivity and invert it
        // (dragging left should make plane face right, just like in SetLaunchRotation)
        float rotationAngle = -angle * rotationSensitivity;
        
        // Clamp to max rotation angle
        rotationAngle = Mathf.Clamp(rotationAngle, -maxRotationAngle, maxRotationAngle);
        
        // Create the target rotation
        targetRotation = Quaternion.Euler(0, rotationAngle, 0);
        
        // Debug info
        Debug.DrawRay(transform.position, dragDirection * 2f, Color.red);
        Debug.DrawRay(transform.position, forwardDir * 2f, Color.blue);
    }
    
    /// <summary>
    /// Call this method from SimpleDragLauncher when the plane is released
    /// to set the final rotation based on the drag direction
    /// </summary>
    public void SetLaunchRotation()
    {
        // If we have a reference to the drag launcher, get the drag vector from there
        if (dragLauncher != null && restPosition != null)
        {
            // Calculate drag vector from the plane's current position
            Vector3 launchDragVector = restPosition.position - transform.position;
            launchDragVector.y = 0; // Only care about horizontal component
            
            if (launchDragVector.magnitude < 0.1f) return;
            
            // Get the normalized drag direction
            Vector3 dragDirection = launchDragVector.normalized;
            
            // Calculate the angle between the drag direction and forward vector
            Vector3 forwardDir = Vector3.forward;
            float angle = Vector3.SignedAngle(dragDirection, forwardDir, Vector3.up);
            
            // Adjust angle based on sensitivity and invert it
            // (dragging left should make plane face right)
            float rotationAngle = -angle * rotationSensitivity;
            
            // Clamp to max rotation angle
            rotationAngle = Mathf.Clamp(rotationAngle, -maxRotationAngle, maxRotationAngle);
            
            // Set the final rotation
            transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            targetRotation = transform.rotation;
            
            Debug.Log($"Launch rotation set: {rotationAngle} degrees");
        }
        else if (dragVector.magnitude >= 0.1f)
        {
            // Fallback to our own drag vector if we don't have a reference to the launcher
            Vector3 dragDirection = dragVector.normalized;
            
            // Calculate the angle between the drag direction and forward vector
            Vector3 forwardDir = Vector3.forward;
            float angle = Vector3.SignedAngle(dragDirection, forwardDir, Vector3.up);
            
            // Adjust angle based on sensitivity and invert it
            float rotationAngle = -angle * rotationSensitivity;
            
            // Clamp to max rotation angle
            rotationAngle = Mathf.Clamp(rotationAngle, -maxRotationAngle, maxRotationAngle);
            
            // Set the final rotation
            transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            targetRotation = transform.rotation;
            
            Debug.Log($"Launch rotation set (fallback): {rotationAngle} degrees");
        }
    }
}