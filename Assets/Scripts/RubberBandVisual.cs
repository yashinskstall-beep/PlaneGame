using UnityEngine;

/// <summary>
/// Creates a visual rubber band effect for a plane when dragging it back
/// Attach this script to any GameObject with a LineRenderer component
/// </summary>
public class RubberBandVisual : MonoBehaviour
{
    [Header("Connection Points")]
    public Transform leftAnchor;    // Left anchor point for the rubber band
    public Transform rightAnchor;   // Right anchor point for the rubber band
    public Transform planeObject;   // The plane GameObject to interact with
    public Transform restPosition;  // The resting/center position

    [Header("Visual Settings")]
    [Range(3, 30)]
    public int bandSegments = 10;   // Number of segments in the band (higher = smoother)
    [Range(0.01f, 1f)]
    public float bandSag = 0.1f;    // How much the band sags when stretched
    [Range(0.01f, 0.2f)]
    public float relaxedWidth = 0.05f;      // Width when not stretched
    [Range(0.005f, 0.1f)]
    public float stretchedWidth = 0.025f;   // Width when fully stretched
    [Range(1f, 10f)]
    public float maxStretchDistance = 5f;  // Distance considered "fully stretched"
    
    [Header("Colors")]
    public Color relaxedColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);  // Color when not stretched (reddish)
    public Color stretchedColor = new Color(1f, 0.5f, 0.5f, 0.8f);  // Color when fully stretched (lighter red)
    
    // Optional reference point for calculating sag direction
    public Transform referenceUp;
    
    // Direction to sag (defaults to world down if referenceUp is not set)
    public Vector3 sagDirection = Vector3.down;
    
    private LineRenderer lineRenderer;
    private Vector3[] positions;
    
    // Dragging state
    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 dragStartPos;
    private float dragDistance;

    private void Awake()
    {
        // Get or add LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log("Added LineRenderer component automatically");
        }
        
        // Initialize the LineRenderer
        SetupLineRenderer();
        
        // Get the main camera
        mainCamera = Camera.main;
    }
    
    private void SetupLineRenderer()
    {
        if (lineRenderer == null) return;
        
        // Set up enough points for a smooth curve
        // We need points for both sides of the band (left to plane, plane to right)
        lineRenderer.positionCount = bandSegments + 2; // +2 for the two halves
        lineRenderer.useWorldSpace = true;
        
        // Set the material properties for a rubber band look
        lineRenderer.startWidth = relaxedWidth;
        lineRenderer.endWidth = relaxedWidth;
        
        // Round caps for a smoother look
        lineRenderer.numCapVertices = 5;
        lineRenderer.numCornerVertices = 5;
        
        // Initialize positions array
        positions = new Vector3[bandSegments + 2];
    }

    private void Update()
    {
        if (planeObject == null || leftAnchor == null || rightAnchor == null || lineRenderer == null) return;
        
        // Handle mouse input for dragging
        HandleInput();
        
        // Update the sag direction if we have a reference transform
        if (referenceUp != null)
        {
            sagDirection = -referenceUp.up; // Sag in the opposite direction of the reference's up
        }
        
        // Only show the rubber band when dragging
        lineRenderer.enabled = isDragging;
        
        if (isDragging)
        {
            UpdateRubberBand();
        }
    }
    
    private void HandleInput()
    {
        // Start dragging when mouse button is pressed down
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if we hit the plane object
                if (hit.transform == planeObject)
                {
                    isDragging = true;
                    dragStartPos = planeObject.position;
                }
            }
        }
        
        // Continue dragging when mouse button is held down
        if (isDragging && Input.GetMouseButton(0))
        {
            DragPlane();
        }
        
        // Stop dragging when mouse button is released
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
        }
    }
    
    private void DragPlane()
    {
        // Create a plane at the rest position with normal facing up
        Plane dragPlane = new Plane(Vector3.up, restPosition.position);
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Raycast against this invisible plane
        if (dragPlane.Raycast(ray, out float enter))
        {
            // Get the world position where the ray hits the plane
            Vector3 hitPoint = ray.GetPoint(enter);
            
            // Calculate drag vector from rest position to hit point
            Vector3 dragVector = hitPoint - restPosition.position;
            
            // Limit the drag distance if needed
            if (dragVector.magnitude > maxStretchDistance)
            {
                dragVector = dragVector.normalized * maxStretchDistance;
            }
            
            // Move the plane to the drag position
            planeObject.position = restPosition.position + dragVector;
            
            // Store the drag distance for stretch calculations
            dragDistance = dragVector.magnitude;
        }
    }
    
    /// <summary>
    /// Updates the rubber band visual between the anchors and the plane
    /// </summary>
    private void UpdateRubberBand()
    {
        // Calculate stretch factor (0 = relaxed, 1 = fully stretched)
        float stretchFactor = Mathf.Clamp01(dragDistance / maxStretchDistance);
        
        // Adjust band width and color based on stretch
        float currentWidth = Mathf.Lerp(relaxedWidth, stretchedWidth, stretchFactor);
        Color currentColor = Color.Lerp(relaxedColor, stretchedColor, stretchFactor);
        
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;
        
        // Set the color - works with both built-in and URP/HDRP
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        
        // Create curved paths for both sides of the rubber band
        int halfSegments = bandSegments / 2;
        
        // First half: Left anchor to plane
        for (int i = 0; i <= halfSegments; i++)
        {
            float t = i / (float)halfSegments;
            positions[i] = CreateBezierPoint(leftAnchor.position, planeObject.position, t, stretchFactor);
        }
        
        // Second half: Plane to right anchor
        for (int i = 0; i <= halfSegments; i++)
        {
            float t = i / (float)halfSegments;
            positions[i + halfSegments + 1] = CreateBezierPoint(planeObject.position, rightAnchor.position, t, stretchFactor);
        }
        
        // Apply all positions to the line renderer
        lineRenderer.SetPositions(positions);
    }
    
    /// <summary>
    /// Creates a point along a bezier curve with sag based on stretch factor
    /// </summary>
    private Vector3 CreateBezierPoint(Vector3 start, Vector3 end, float t, float stretchFactor)
    {
        // Calculate a control point that sags
        Vector3 direction = end - start;
        Vector3 midPoint = start + direction * 0.5f;
        
        // The more stretched, the more the band sags
        float sagAmount = bandSag * stretchFactor;
        
        // Use the sag direction (either from reference or default)
        Vector3 controlPoint = midPoint + sagDirection * sagAmount * direction.magnitude;
        
        // Quadratic bezier formula
        return Vector3.Lerp(
            Vector3.Lerp(start, controlPoint, t),
            Vector3.Lerp(controlPoint, end, t),
            t
        );
    }
    
    /// <summary>
    /// Manually set the plane position (useful for external control)
    /// </summary>
    public void SetPlanePosition(Vector3 position)
    {
        if (planeObject != null)
        {
            planeObject.position = position;
            dragDistance = Vector3.Distance(position, restPosition.position);
            UpdateRubberBand();
        }
    }
    
    /// <summary>
    /// Force the rubber band to be visible or hidden
    /// </summary>
    public void SetBandVisibility(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }
    
    /// <summary>
    /// Show the connection in the editor
    /// </summary>
    private void OnDrawGizmos()
    {
        if (leftAnchor != null && rightAnchor != null && planeObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftAnchor.position, planeObject.position);
            Gizmos.DrawLine(planeObject.position, rightAnchor.position);
            
            // Draw the rest position
            if (restPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(restPosition.position, 0.2f);
            }
        }
    }
}
