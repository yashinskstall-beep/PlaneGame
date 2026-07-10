using System.Collections;
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
    public Color relaxedColor = Color.white;  // Pure white at game start
    public Color stretchedColor = Color.white;
    
    [Header("Level Materials")]
    [Tooltip("Materials for each launch force level. Level 1 = white, Level 2 = brown, Level 3 = red.")]
    public Material[] levelMaterials = new Material[3];

    [Header("Level Colors")]
    [Tooltip("Optional relaxed colors per launch force level.")]
    public Color[] relaxedColorsByLevel;
    [Tooltip("Optional stretched colors per launch force level.")]
    public Color[] stretchedColorsByLevel;
    
    [Header("References")]
    public MainMenu mainMenu;
    
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
    private int currentLevel = 1;

    private static readonly Color[] DefaultRelaxedColors =
    {
        Color.white,                                    // Level 1 — glowy white at start
        new Color(0.55f, 0.35f, 0.18f, 1f),              // Level 2 — brown rope
        new Color(0.85f, 0.12f, 0.1f, 1f)                // Level 3 — red
    };

    private static readonly Color[] DefaultStretchedColors =
    {
        Color.white,
        new Color(0.7f, 0.48f, 0.28f, 1f),
        new Color(1f, 0.35f, 0.3f, 1f)
    };

    private void Awake()
    {
        // Get or add LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log("Added LineRenderer component automatically");
        }
        
        // Find MainMenu if not assigned
        if (mainMenu == null)
        {
            mainMenu = FindObjectOfType<MainMenu>();
        }
        
        // Initialize the LineRenderer
        SetupLineRenderer();
        
        // Get the main camera
        mainCamera = Camera.main;

        // Ensure the line renderer is visible from the start
        lineRenderer.enabled = true;
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

    private void Start()
    {
        int savedLevel = PlayerPrefs.GetInt(LevelProgress.GetLaunchForceLevelKey(), 0);
        if (savedLevel <= 0)
            savedLevel = PlayerPrefs.GetInt("LaunchForceLevel", 1);

        ApplyLaunchForceLevel(savedLevel);
    }

    private void Update()
    {
        if (planeObject == null || leftAnchor == null || rightAnchor == null)
        {
            Debug.LogWarning("Missing required references!");
            return;
        }
        
        // Handle input for dragging
        HandleInput();
        
        // Update the rubber band visual
        UpdateRubberBand();
    }
    
    /// <summary>
    /// Applies the launch-force band look for the given level.
    /// Call this from MainMenu on load and after upgrades — do not poll PlayerPrefs here,
    /// or a mid-upgrade visual change will snap back until save completes.
    /// </summary>
    public void ApplyLaunchForceLevel(int level)
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        int materialCount = levelMaterials != null ? levelMaterials.Length : 0;
        int maxLevel = Mathf.Max(1, materialCount > 0 ? materialCount : DefaultRelaxedColors.Length);
        level = Mathf.Clamp(level, 1, maxLevel);
        currentLevel = level;

        relaxedColor = GetLevelColor(relaxedColorsByLevel, DefaultRelaxedColors, level);
        stretchedColor = GetLevelColor(stretchedColorsByLevel, DefaultStretchedColors, level);

        Material targetMaterial = null;
        if (levelMaterials != null && levelMaterials.Length >= level)
            targetMaterial = levelMaterials[level - 1];
        else if (levelMaterials != null && levelMaterials.Length > 0)
            targetMaterial = levelMaterials[levelMaterials.Length - 1];

        if (targetMaterial != null)
            lineRenderer.material = targetMaterial;

        // RopeLine shader ignores vertex colors; keep them white so they don't darken anything.
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
    }

    public IEnumerator PlayUpgradePulse(float duration = 0.35f)
    {
        if (lineRenderer == null)
            yield break;

        Color start = relaxedColor;
        Color pulse = Color.Lerp(start, Color.white, 0.65f);
        float half = Mathf.Max(0.05f, duration * 0.5f);
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            Color c = Color.Lerp(start, pulse, elapsed / half);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            Color c = Color.Lerp(pulse, relaxedColor, elapsed / half);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            yield return null;
        }

        lineRenderer.startColor = relaxedColor;
        lineRenderer.endColor = relaxedColor;
    }

    private static Color GetLevelColor(Color[] customColors, Color[] defaults, int level)
    {
        if (customColors != null && customColors.Length >= level)
            return customColors[level - 1];

        if (defaults != null && defaults.Length >= level)
            return defaults[level - 1];

        return defaults != null && defaults.Length > 0 ? defaults[0] : Color.red;
    }
    
    private void HandleInput()
    {
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
