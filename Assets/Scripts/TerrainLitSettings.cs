using UnityEngine;

/// <summary>
/// Adjust metallic and smoothness on a terrain's URP Terrain Lit material template.
/// Attach to any Terrain that uses the TerrainLit material.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Terrain))]
public class TerrainLitSettings : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Leave empty to use the Terrain's Material Template.")]
    [SerializeField] private Material terrainLitMaterial;

    [Header("All Terrain Layers")]
    [Range(0f, 1f)]
    [SerializeField] private float metallic = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float smoothness = 0f;

    [Header("Per Layer Overrides")]
    [SerializeField] private bool usePerLayerOverrides;

    [Range(0f, 1f)] [SerializeField] private float metallic0;
    [Range(0f, 1f)] [SerializeField] private float metallic1;
    [Range(0f, 1f)] [SerializeField] private float metallic2;
    [Range(0f, 1f)] [SerializeField] private float metallic3;

    [Range(0f, 1f)] [SerializeField] private float smoothness0;
    [Range(0f, 1f)] [SerializeField] private float smoothness1;
    [Range(0f, 1f)] [SerializeField] private float smoothness2;
    [Range(0f, 1f)] [SerializeField] private float smoothness3;

    private Terrain terrain;

    private void OnEnable()
    {
        terrain = GetComponent<Terrain>();
        Apply();
    }

    private void OnValidate()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        Apply();
    }

    public void Apply()
    {
        Material material = terrainLitMaterial != null
            ? terrainLitMaterial
            : terrain != null ? terrain.materialTemplate : null;

        if (material == null)
            return;

        if (usePerLayerOverrides)
        {
            SetLayerValues(material, 0, metallic0, smoothness0);
            SetLayerValues(material, 1, metallic1, smoothness1);
            SetLayerValues(material, 2, metallic2, smoothness2);
            SetLayerValues(material, 3, metallic3, smoothness3);
        }
        else
        {
            for (int i = 0; i < 4; i++)
                SetLayerValues(material, i, metallic, smoothness);
        }

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
    }

    private static void SetLayerValues(Material material, int layerIndex, float metallicValue, float smoothnessValue)
    {
        string metallicProperty = $"_Metallic{layerIndex}";
        string smoothnessProperty = $"_Smoothness{layerIndex}";

        if (material.HasProperty(metallicProperty))
            material.SetFloat(metallicProperty, metallicValue);

        if (material.HasProperty(smoothnessProperty))
            material.SetFloat(smoothnessProperty, smoothnessValue);
    }
}
