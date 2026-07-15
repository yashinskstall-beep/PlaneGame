using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for LevelsUI that adds editor-only level override controls.
/// Allows testers to jump to any level, unlock/lock levels, and reset all level progress
/// directly from the Inspector — without touching PlayerPrefs manually.
/// </summary>
[CustomEditor(typeof(LevelsUI))]
public class LevelsUIEditor : Editor
{
    private const string UnlockedKeyPrefix = "LevelUnlocked_";

    public override void OnInspectorGUI()
    {
        // Draw the default inspector first
        DrawDefaultInspector();

        LevelsUI levelsUI = (LevelsUI)target;

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("🛠  Editor Testing Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use these controls to override level state for testing.\n" +
            "Changes modify PlayerPrefs directly and persist between play sessions.",
            MessageType.Info);

        // --- Level unlock overview & toggles ---
        SerializedProperty levelsProp = serializedObject.FindProperty("levels");
        int levelCount = levelsProp != null ? levelsProp.arraySize : 0;

        if (levelCount == 0)
        {
            EditorGUILayout.HelpBox("No levels configured in the levels array.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Level Unlock State", EditorStyles.miniBoldLabel);

            for (int i = 0; i < levelCount; i++)
            {
                bool isUnlocked = i == 0 || PlayerPrefs.GetInt(UnlockedKeyPrefix + i, 0) == 1;
                string levelName = GetLevelDisplayName(levelsProp, i);

                EditorGUILayout.BeginHorizontal();

                // Level label
                EditorGUILayout.LabelField(
                    $"Level {i}: {levelName}",
                    isUnlocked ? EditorStyles.label : EditorStyles.miniLabel,
                    GUILayout.MinWidth(160));

                // Status badge
                GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                Color prevColor = GUI.contentColor;
                GUI.contentColor = isUnlocked ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
                EditorGUILayout.LabelField(isUnlocked ? "UNLOCKED" : "LOCKED", badgeStyle, GUILayout.Width(70));
                GUI.contentColor = prevColor;

                // Toggle button
                if (i == 0)
                {
                    // Level 0 is always unlocked
                    GUI.enabled = false;
                    GUILayout.Button("Always Unlocked", EditorStyles.miniButton, GUILayout.Width(120));
                    GUI.enabled = true;
                }
                else
                {
                    if (isUnlocked)
                    {
                        if (GUILayout.Button("Lock", EditorStyles.miniButton, GUILayout.Width(120)))
                        {
                            PlayerPrefs.DeleteKey(UnlockedKeyPrefix + i);
                            PlayerPrefs.Save();
                            Debug.Log($"[LevelsUIEditor] Locked level {i} ({levelName})");
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Unlock", EditorStyles.miniButton, GUILayout.Width(120)))
                        {
                            PlayerPrefs.SetInt(UnlockedKeyPrefix + i, 1);
                            PlayerPrefs.Save();
                            Debug.Log($"[LevelsUIEditor] Unlocked level {i} ({levelName})");
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // --- Jump to level ---
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Jump to Level", EditorStyles.miniBoldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use Jump to Level.", MessageType.None);
            }

            for (int i = 0; i < levelCount; i++)
            {
                string levelName = GetLevelDisplayName(levelsProp, i);
                string sceneName = GetLevelSceneName(levelsProp, i);
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                bool isCurrentScene = !string.IsNullOrEmpty(sceneName) && sceneName == currentScene;

                EditorGUI.BeginDisabledGroup(!Application.isPlaying || isCurrentScene);

                string buttonLabel = isCurrentScene
                    ? $"▶ Level {i}: {levelName}  (current)"
                    : $"   Level {i}: {levelName}";

                if (GUILayout.Button(buttonLabel, GUILayout.Height(24)))
                {
                    // Unlock the level so the game allows it
                    if (i > 0)
                    {
                        PlayerPrefs.SetInt(UnlockedKeyPrefix + i, 1);
                        PlayerPrefs.Save();
                    }

                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        Debug.Log($"[LevelsUIEditor] Jumping to level {i} ({levelName}) — loading scene: {sceneName}");
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    }
                    else
                    {
                        Debug.Log($"[LevelsUIEditor] Level {i} has no scene name configured. Reloading current scene.");
                        UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
                    }
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        // --- Bulk actions ---
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Bulk Actions", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Unlock All Levels", GUILayout.Height(28)))
        {
            for (int i = 1; i < levelCount; i++)
            {
                PlayerPrefs.SetInt(UnlockedKeyPrefix + i, 1);
            }
            PlayerPrefs.Save();
            Debug.Log("[LevelsUIEditor] All levels unlocked.");
        }

        if (GUILayout.Button("Lock All Levels", GUILayout.Height(28)))
        {
            for (int i = 1; i < levelCount; i++)
            {
                PlayerPrefs.DeleteKey(UnlockedKeyPrefix + i);
            }
            PlayerPrefs.Save();
            Debug.Log("[LevelsUIEditor] All levels locked (except level 0).");
        }

        EditorGUILayout.EndHorizontal();

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Reset ALL Level & Scene Progress", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                "Reset All Progress",
                "This will delete ALL level unlock keys and scene completion keys from PlayerPrefs.\n\nAre you sure?",
                "Yes, Reset Everything",
                "Cancel"))
            {
                for (int i = 1; i < levelCount; i++)
                {
                    PlayerPrefs.DeleteKey(UnlockedKeyPrefix + i);
                }

                // Also clear scene completion keys for all configured levels
                for (int i = 0; i < levelCount; i++)
                {
                    string sceneName = GetLevelSceneName(levelsProp, i);
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        PlayerPrefs.DeleteKey(sceneName + "_Completed");
                    }
                }

                PlayerPrefs.Save();
                Debug.Log("[LevelsUIEditor] All level progress has been reset.");
            }
        }
        GUI.backgroundColor = prevBg;

        // Force repaint during play mode so status stays current
        if (Application.isPlaying)
            Repaint();
    }

    /// <summary>
    /// Reads the displayName field from the serialized LevelButtonData at the given index.
    /// </summary>
    private string GetLevelDisplayName(SerializedProperty levelsProp, int index)
    {
        if (levelsProp == null || index < 0 || index >= levelsProp.arraySize)
            return "Unknown";

        SerializedProperty element = levelsProp.GetArrayElementAtIndex(index);
        SerializedProperty nameProp = element.FindPropertyRelative("displayName");
        return nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue)
            ? nameProp.stringValue
            : $"Level {index}";
    }

    /// <summary>
    /// Reads the sceneName field from the serialized LevelButtonData at the given index.
    /// </summary>
    private string GetLevelSceneName(SerializedProperty levelsProp, int index)
    {
        if (levelsProp == null || index < 0 || index >= levelsProp.arraySize)
            return string.Empty;

        SerializedProperty element = levelsProp.GetArrayElementAtIndex(index);
        SerializedProperty sceneProp = element.FindPropertyRelative("sceneName");
        return sceneProp != null ? sceneProp.stringValue : string.Empty;
    }
}
