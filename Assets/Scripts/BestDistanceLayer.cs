using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows a vertical transparent wall at the player's best recorded flight distance.
/// Visible on the next run after a scene reload.
/// </summary>
[DisallowMultipleComponent]
public class BestDistanceLayer : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Launch point (Point A). Auto-finds a GameObject named PointA if empty.")]
    public Transform startPoint;

    [Tooltip("Finish line (Point B). Used for direction and max length. Auto-finds PointB if empty.")]
    public Transform finishPoint;

    [Header("Wall Appearance")]
    public float wallWidth = 14f;
    public float wallHeight = 10f;
    public float groundOffset = 0.1f;
    public Color layerColor = new Color(0.2f, 0.75f, 1f, 0.35f);
    public float edgeFade = 0.12f;

    [Header("Placement")]
    [Tooltip("Raycast down to snap the wall base to terrain.")]
    public bool snapToGround = true;
    public float groundRaycastHeight = 30f;
    public float groundRaycastDistance = 60f;

    [Header("Optional")]
    [Tooltip("Hide the wall until the player has a recorded best distance.")]
    public bool hideWhenNoRecord = true;

    private GameObject layerObject;
    private Material layerMaterial;

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        RefreshLayer();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneManager.GetActiveScene().name)
            RefreshLayer();
    }

    public static void RecordFlightDistance(float distance)
    {
        BestDistanceRecord.TryUpdateBest(distance);

        BestDistanceLayer layer = FindObjectOfType<BestDistanceLayer>();
        if (layer != null)
            layer.RefreshLayer();
    }

    public void RefreshLayer()
    {
        ResolveReferences();

        float bestDistance = BestDistanceRecord.GetBestDistance();
        if (bestDistance <= 0f)
        {
            if (hideWhenNoRecord)
                HideLayer();
            return;
        }

        float maxPathLength = GetMaxPathLength();
        float displayDistance = maxPathLength > 0f
            ? Mathf.Clamp(bestDistance, 0.1f, maxPathLength)
            : bestDistance;

        BuildOrUpdateWall(displayDistance);
    }

    private void ResolveReferences()
    {
        if (startPoint == null)
        {
            GameObject pointA = GameObject.Find("PointA");
            if (pointA != null)
                startPoint = pointA.transform;
        }

        if (finishPoint == null)
        {
            GameObject pointB = GameObject.Find("PointB");
            if (pointB != null)
                finishPoint = pointB.transform;
        }
    }

    private float GetMaxPathLength()
    {
        if (startPoint == null || finishPoint == null)
            return 0f;

        Vector3 delta = finishPoint.position - startPoint.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    private Vector3 GetPathDirection()
    {
        if (startPoint != null && finishPoint != null)
        {
            Vector3 dir = finishPoint.position - startPoint.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        return Vector3.forward;
    }

    private Vector3 GetGroundPositionAtDistance(float distance)
    {
        Vector3 direction = GetPathDirection();
        Vector3 point = startPoint.position + direction * distance;

        if (snapToGround)
        {
            Vector3 rayOrigin = point + Vector3.up * groundRaycastHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRaycastDistance))
                point.y = hit.point.y;
        }

        point.y += groundOffset;
        return point;
    }

    private void BuildOrUpdateWall(float distance)
    {
        if (startPoint == null)
        {
            Debug.LogWarning("BestDistanceLayer: startPoint is not assigned and PointA was not found.");
            HideLayer();
            return;
        }

        if (layerObject == null)
            CreateLayerObject();

        Vector3 direction = GetPathDirection();
        Vector3 groundPoint = GetGroundPositionAtDistance(distance);
        Vector3 center = groundPoint + Vector3.up * (wallHeight * 0.5f);

        layerObject.transform.position = center;
        layerObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        layerObject.transform.localScale = new Vector3(wallWidth, wallHeight, 1f);

        if (layerMaterial != null)
        {
            layerMaterial.SetColor("_Color", layerColor);
            layerMaterial.SetFloat("_EdgeFade", edgeFade);
        }

        layerObject.SetActive(true);
    }

    private void CreateLayerObject()
    {
        layerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        layerObject.name = "BestDistanceWall";
        layerObject.transform.SetParent(transform, false);

        Collider collider = layerObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Shader shader = Shader.Find("Custom/BestDistanceLayer");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        layerMaterial = new Material(shader);
        layerMaterial.SetColor("_Color", layerColor);
        layerMaterial.SetFloat("_EdgeFade", edgeFade);

        if (shader.name.Contains("Universal Render Pipeline/Unlit"))
        {
            layerMaterial.SetFloat("_Surface", 1f);
            layerMaterial.SetFloat("_Blend", 0f);
            layerMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            layerMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            layerMaterial.SetFloat("_ZWrite", 0f);
            layerMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            layerMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        layerObject.GetComponent<MeshRenderer>().sharedMaterial = layerMaterial;
    }

    private void HideLayer()
    {
        if (layerObject != null)
            layerObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (layerMaterial != null)
            Destroy(layerMaterial);
    }
}
