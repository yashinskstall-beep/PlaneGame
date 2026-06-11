using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows a vertical transparent wall at the player's best recorded flight distance.
/// The wall base follows terrain height along its width; top is always wallHeight above local ground.
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
    [Tooltip("Height above local ground at each point along the wall.")]
    public float wallHeight = 10f;
    public float groundOffset = 0.1f;
    public Color layerColor = new Color(0.2f, 0.75f, 1f, 0.35f);
    [Tooltip("Soft fade at the top edge only. Bottom stays solid.")]
    public float edgeFade = 0.12f;

    [Header("Mesh")]
    [Tooltip("More segments = smoother wall on hills. 8–24 is usually enough.")]
    [Range(2, 48)]
    public int wallSegments = 16;

    [Header("Placement")]
    [Tooltip("Raycast down to snap each part of the wall to terrain.")]
    public bool snapToGround = true;
    public float groundRaycastHeight = 30f;
    public float groundRaycastDistance = 60f;
    public LayerMask groundLayers = ~0;

    [Header("Optional")]
    [Tooltip("Hide the wall until the player has a recorded best distance.")]
    public bool hideWhenNoRecord = true;

    private GameObject layerObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material layerMaterial;
    private Mesh wallMesh;

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

    private float SampleGroundHeight(Vector3 worldPosition)
    {
        if (!snapToGround)
            return worldPosition.y;

        Vector3 rayOrigin = worldPosition + Vector3.up * groundRaycastHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point.y + groundOffset;

        return worldPosition.y + groundOffset;
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
        Vector3 pathPoint = startPoint.position + direction * distance;
        pathPoint.y = SampleGroundHeight(pathPoint);

        layerObject.transform.position = pathPoint;
        layerObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Vector3 widthAxis = layerObject.transform.right;
        int segments = Mathf.Max(2, wallSegments);
        int vertexCount = (segments + 1) * 2;
        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var triangles = new int[segments * 6];

        float pivotY = pathPoint.y;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float localX = Mathf.Lerp(-wallWidth * 0.5f, wallWidth * 0.5f, t);
            Vector3 sampleWorld = pathPoint + widthAxis * localX;
            float groundY = SampleGroundHeight(sampleWorld);
            float localGroundY = groundY - pivotY;
            float localTopY = localGroundY + wallHeight;

            int bottomIndex = i * 2;
            int topIndex = bottomIndex + 1;

            vertices[bottomIndex] = new Vector3(localX, localGroundY, 0f);
            vertices[topIndex] = new Vector3(localX, localTopY, 0f);
            uvs[bottomIndex] = new Vector2(t, 0f);
            uvs[topIndex] = new Vector2(t, 1f);

            if (i < segments)
            {
                int tri = i * 6;
                int nextBottom = (i + 1) * 2;
                int nextTop = nextBottom + 1;

                triangles[tri] = bottomIndex;
                triangles[tri + 1] = nextTop;
                triangles[tri + 2] = topIndex;

                triangles[tri + 3] = bottomIndex;
                triangles[tri + 4] = nextBottom;
                triangles[tri + 5] = nextTop;
            }
        }

        if (wallMesh == null)
            wallMesh = new Mesh { name = "BestDistanceWallMesh" };

        wallMesh.Clear();
        wallMesh.vertices = vertices;
        wallMesh.uv = uvs;
        wallMesh.triangles = triangles;
        wallMesh.RecalculateNormals();
        wallMesh.RecalculateBounds();

        meshFilter.sharedMesh = wallMesh;

        if (layerMaterial != null)
        {
            layerMaterial.SetColor("_Color", layerColor);
            layerMaterial.SetFloat("_EdgeFade", edgeFade);
        }

        layerObject.SetActive(true);
    }

    private void CreateLayerObject()
    {
        layerObject = new GameObject("BestDistanceWall");
        layerObject.transform.SetParent(transform, false);

        meshFilter = layerObject.AddComponent<MeshFilter>();
        meshRenderer = layerObject.AddComponent<MeshRenderer>();

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

        meshRenderer.sharedMaterial = layerMaterial;
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

        if (wallMesh != null)
            Destroy(wallMesh);
    }
}
