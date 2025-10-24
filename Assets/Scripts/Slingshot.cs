using UnityEngine;

public class SlingShot : MonoBehaviour
{
    [Header("Slingshot Parts")]
    public Transform leftAnchor;
    public Transform rightAnchor;
    public Transform restPosition;

    [Header("Projectile")]
    public Rigidbody projectile;

    [Header("Elastic Band")]
    public LineRenderer bandRenderer;
    [Tooltip("Number of segments in the band for a more realistic look")]
    public int bandSegments = 10;
    [Tooltip("How much the band sags when stretched")]
    public float bandSag = 0.1f;
    [Tooltip("Width of the band when relaxed")]
    public float relaxedWidth = 0.1f;
    [Tooltip("Width of the band when fully stretched")]
    public float stretchedWidth = 0.05f;
    [Tooltip("Color of the band when relaxed")]
    public Color relaxedColor = new Color(0.8f, 0.2f, 0.2f, 0.8f); // Reddish
    [Tooltip("Color of the band when stretched")]
    public Color stretchedColor = new Color(1f, 0.5f, 0.5f, 0.8f); // Lighter red

    [Header("Settings")]
    [Tooltip("Maximum stretch distance from rest position")]
    public float maxStretch = 3f;

    [Tooltip("Launch force multiplier")]
    public float launchPower = 30f;

    private bool isDragging = false;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        // Make sure the projectile starts at rest position
        if (projectile != null && restPosition != null)
        {
            projectile.isKinematic = true;
            projectile.transform.position = restPosition.position;
        }

        // Setup the line renderer for the elastic band
        if (bandRenderer != null)
        {
            // Set up enough points for a smooth curve
            bandRenderer.positionCount = bandSegments * 2 + 2;
            bandRenderer.useWorldSpace = true;
            
            // Set the material properties for a rubber band look
            bandRenderer.material.SetColor("_Color", relaxedColor);
            bandRenderer.startWidth = relaxedWidth;
            bandRenderer.endWidth = relaxedWidth;
            
            // Round caps for a smoother look
            bandRenderer.numCapVertices = 5;
            bandRenderer.numCornerVertices = 5;
        }

        UpdateBand();
    }

    void Update()
    {
        if (projectile == null || cam == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.rigidbody == projectile)
                {
                    isDragging = true;
                    projectile.isKinematic = true;
                }
            }
        }

        if (isDragging)
        {
            DragProjectile();
            UpdateBand();
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            LaunchProjectile();
            isDragging = false;
        }
    }

    void DragProjectile()
    {
        Vector3 mouseWorldPoint = cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y,
            cam.WorldToScreenPoint(restPosition.position).z)
        );

        Vector3 dir = mouseWorldPoint - restPosition.position;

        // Limit how far you can stretch
        if (dir.magnitude > maxStretch)
            dir = dir.normalized * maxStretch;

        projectile.transform.position = restPosition.position + dir;
    }

    void LaunchProjectile()
    {
        Vector3 launchDir = restPosition.position - projectile.position;
        projectile.isKinematic = false;
        projectile.AddForce(launchDir * launchPower, ForceMode.Impulse);

        // Hide the band after launch
        if (bandRenderer != null)
            bandRenderer.enabled = false;
    }

    void UpdateBand()
    {
        if (bandRenderer == null) return;

        // Calculate stretch factor (0 = relaxed, 1 = fully stretched)
        float stretchDistance = Vector3.Distance(projectile.position, restPosition.position);
        float stretchFactor = Mathf.Clamp01(stretchDistance / maxStretch);
        
        // Adjust band width and color based on stretch
        float currentWidth = Mathf.Lerp(relaxedWidth, stretchedWidth, stretchFactor);
        Color currentColor = Color.Lerp(relaxedColor, stretchedColor, stretchFactor);
        
        bandRenderer.startWidth = currentWidth;
        bandRenderer.endWidth = currentWidth;
        bandRenderer.material.SetColor("_Color", currentColor);
        
        // Create two curved paths from anchors to projectile
        Vector3[] positions = new Vector3[bandSegments * 2 + 2];
        
        // First half: Left anchor to projectile
        for (int i = 0; i <= bandSegments; i++)
        {
            float t = i / (float)bandSegments;
            positions[i] = BezierPoint(leftAnchor.position, projectile.position, t, stretchFactor);
        }
        
        // Second half: Projectile to right anchor
        for (int i = 0; i <= bandSegments; i++)
        {
            float t = i / (float)bandSegments;
            positions[i + bandSegments + 1] = BezierPoint(projectile.position, rightAnchor.position, t, stretchFactor);
        }
        
        // Apply all positions to the line renderer
        bandRenderer.SetPositions(positions);
    }
    
    // Creates a bezier curve point with sag based on stretch factor
    private Vector3 BezierPoint(Vector3 start, Vector3 end, float t, float stretchFactor)
    {
        // Calculate a control point that sags downward
        Vector3 direction = end - start;
        Vector3 midPoint = start + direction * 0.5f;
        
        // The more stretched, the more the band sags
        float sagAmount = bandSag * stretchFactor;
        Vector3 controlPoint = midPoint + Vector3.down * sagAmount * direction.magnitude;
        
        // Quadratic bezier formula
        return Vector3.Lerp(
            Vector3.Lerp(start, controlPoint, t),
            Vector3.Lerp(controlPoint, end, t),
            t
        );
    }

    private void OnDrawGizmos()
    {
        // Show the rest position in the editor
        if (restPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(restPosition.position, 0.05f);
        }
    }
}
