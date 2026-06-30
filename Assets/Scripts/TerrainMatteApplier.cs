using UnityEngine;

/// <summary>
/// Forces terrain to render without glossy specular highlights.
/// </summary>
public static class TerrainMatteApplier
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnSceneLoad()
    {
        ApplyToAllTerrains();
    }

    public static void ApplyToAllTerrains()
    {
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in terrains)
            ApplyToTerrain(terrain);
    }

    public static void ApplyToTerrain(Terrain terrain)
    {
        if (terrain == null)
            return;

        Material material = terrain.materialTemplate;
        if (material == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            string metallicId = "_Metallic" + i;
            string smoothnessId = "_Smoothness" + i;

            if (material.HasProperty(metallicId))
                material.SetFloat(metallicId, 0f);

            if (material.HasProperty(smoothnessId))
                material.SetFloat(smoothnessId, 0f);
        }

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0f);

        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0f);

        if (material.HasProperty("_SpecularHighlights"))
            material.SetFloat("_SpecularHighlights", 0f);

        if (material.HasProperty("_EnvironmentReflections"))
            material.SetFloat("_EnvironmentReflections", 0f);

        if (material.HasProperty("_GlossyReflections"))
            material.SetFloat("_GlossyReflections", 0f);

        if (material.HasProperty("_EnableInstancedPerPixelNormal"))
            material.SetFloat("_EnableInstancedPerPixelNormal", 0f);
    }
}
