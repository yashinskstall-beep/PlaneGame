#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TerrainMatteApplierEditor
{
    [MenuItem("Tools/Plane Game/Remove Terrain Reflections")]
    static void RemoveTerrainReflections()
    {
        int layerCount = FixTerrainLayerAssets();
        int terrainCount = FixSceneTerrains();
        AssetDatabase.SaveAssets();

        Debug.Log($"Terrain matte applied: {layerCount} terrain layer asset(s), {terrainCount} terrain(s) in open scene(s).");
    }

    static int FixTerrainLayerAssets()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:TerrainLayer");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
                continue;

            layer.metallic = 0f;
            layer.smoothness = 0f;
            layer.specular = Color.black;
            layer.normalMapTexture = null;
            EditorUtility.SetDirty(layer);
            count++;
        }

        return count;
    }

    static int FixSceneTerrains()
    {
        int count = 0;
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);

        foreach (Terrain terrain in terrains)
        {
            TerrainMatteApplier.ApplyToTerrain(terrain);
            EditorUtility.SetDirty(terrain);
            count++;
        }

        return count;
    }
}
#endif
