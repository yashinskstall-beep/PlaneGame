using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Custom editor for DebugLevelSelector.
/// Renders level buttons, lock/unlock toggles, and jump controls directly in the Inspector.
/// </summary>
[CustomEditor(typeof(DebugLevelSelector))]
public class DebugLevelSelectorEditor : Editor
{
    private const string UnlockedKeyPrefix = "LevelUnlocked_";

    public override void OnInspectorGUI()
    {
        DebugLevelSelector selector = (DebugLevelSelector)target;

        // Title
        EditorGUILayout.Space(4);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🎮  Debug Level Selector", titleStyle);
        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox(
            "This object persists across scene loads (DontDestroyOnLoad).\n" +
            "Use the controls below to jump between levels for testing.\n" +
            "Fully stripped from release builds.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // --- Current scene info ---
        string currentScene = SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField("Current Scene", currentScene, EditorStyles.boldLabel);

        EditorGUILayout.Space(8);

        // --- Find LevelsUI to get level data ---
        LevelsUI levelsUI = FindObjectOfType<LevelsUI>(true);
        SerializedProperty levelsProp = null;
        int levelCount = 0;

        if (levelsUI != null)
        {
            SerializedObject levelsObj = new SerializedObject(levelsUI);
            levelsProp = levelsObj.FindProperty("levels");
            levelCount = levelsProp != null ? levelsProp.arraySize : 0;
        }

        if (levelCount == 0)
        {
            EditorGUILayout.HelpBox(
                "No LevelsUI found in the scene, or it has no levels configured.\n" +
                "Level data will appear here once a LevelsUI component is available.",
                MessageType.Warning);

            EditorGUILayout.Space(4);
            DrawDefaultInspector();
            return;
        }

        // --- Level selector dropdown ---
        EditorGUILayout.LabelField("Select Level", EditorStyles.miniBoldLabel);

        string[] levelNames = new string[levelCount];
        for (int i = 0; i < levelCount; i++)
        {
            levelNames[i] = $"Level {i}: {GetLevelDisplayName(levelsProp, i)}";
        }

        SerializedObject so = serializedObject;
        so.Update();

        SerializedProperty targetProp = so.FindProperty("targetLevelIndex");
        if (targetProp != null)
        {
            targetProp.intValue = EditorGUILayout.Popup("Target Level", targetProp.intValue, levelNames);
        }

        SerializedProperty autoUnlockProp = so.FindProperty("autoUnlockOnJump");
        if (autoUnlockProp != null)
        {
            autoUnlockProp.boolValue = EditorGUILayout.Toggle("Auto Unlock on Jump", autoUnlockProp.boolValue);
        }

        so.ApplyModifiedProperties();

        // --- Jump button ---
        EditorGUILayout.Space(4);

        bool isPlaying = Application.isPlaying;
        EditorGUI.BeginDisabledGroup(!isPlaying);

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.5f);
        if (GUILayout.Button($"▶  Jump to {levelNames[selector.targetLevelIndex]}", GUILayout.Height(32)))
        {
            selector.JumpToTargetLevel();
        }
        GUI.backgroundColor = prevBg;

        EditorGUI.EndDisabledGroup();

        if (!isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to jump between levels.", MessageType.None);
        }

        // --- Per-level status & controls ---
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("All Levels", EditorStyles.miniBoldLabel);

        DrawSeparator();

        for (int i = 0; i < levelCount; i++)
        {
            bool isUnlocked = i == 0 || PlayerPrefs.GetInt(UnlockedKeyPrefix + i, 0) == 1;
            string displayName = GetLevelDisplayName(levelsProp, i);
            string sceneName = GetLevelSceneName(levelsProp, i);
            bool isCurrent = !string.IsNullOrEmpty(sceneName) && sceneName == currentScene;

            // Highlight current level row
            if (isCurrent)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            }

            EditorGUILayout.BeginHorizontal();

            // Level info
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
            };
            string prefix = isCurrent ? "▶ " : "   ";
            EditorGUILayout.LabelField($"{prefix}Level {i}: {displayName}", nameStyle, GUILayout.MinWidth(150));

            // Status
            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            Color prevContent = GUI.contentColor;
            GUI.contentColor = isUnlocked ? new Color(0.1f, 0.75f, 0.1f) : new Color(0.9f, 0.25f, 0.25f);
            EditorGUILayout.LabelField(isUnlocked ? "✓ UNLOCKED" : "✗ LOCKED", statusStyle, GUILayout.Width(80));
            GUI.contentColor = prevContent;

            // Lock/Unlock toggle
            if (i == 0)
            {
                GUI.enabled = false;
                GUILayout.Button("Default", EditorStyles.miniButton, GUILayout.Width(70));
                GUI.enabled = true;
            }
            else
            {
                if (isUnlocked)
                {
                    if (GUILayout.Button("Lock", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        selector.LockLevel(i);
                    }
                }
                else
                {
                    if (GUILayout.Button("Unlock", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        selector.UnlockLevel(i);
                    }
                }
            }

            // Quick jump button
            EditorGUI.BeginDisabledGroup(!isPlaying || isCurrent);
            if (GUILayout.Button("Go", EditorStyles.miniButtonRight, GUILayout.Width(35)))
            {
                so.Update();
                if (targetProp != null)
                {
                    targetProp.intValue = i;
                }
                so.ApplyModifiedProperties();
                selector.targetLevelIndex = i;
                selector.JumpToTargetLevel();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Scene name subtitle
            if (!string.IsNullOrEmpty(sceneName))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Scene: {sceneName}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            if (isCurrent)
            {
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(2);
        }

        DrawSeparator();

        // --- Bulk actions ---
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Bulk Actions", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Unlock All", GUILayout.Height(26)))
        {
            for (int i = 1; i < levelCount; i++)
            {
                PlayerPrefs.SetInt(UnlockedKeyPrefix + i, 1);
            }
            PlayerPrefs.Save();
            Debug.Log("[DebugLevelSelector] All levels unlocked.");
        }

        if (GUILayout.Button("Lock All", GUILayout.Height(26)))
        {
            for (int i = 1; i < levelCount; i++)
            {
                PlayerPrefs.DeleteKey(UnlockedKeyPrefix + i);
            }
            PlayerPrefs.Save();
            Debug.Log("[DebugLevelSelector] All levels locked (except level 0).");
        }

        EditorGUILayout.EndHorizontal();

        prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("🗑  Reset ALL Progress", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog(
                "Reset All Progress",
                "This will delete ALL level unlock keys and scene completion keys from PlayerPrefs.\n\nAre you sure?",
                "Yes, Reset",
                "Cancel"))
            {
                for (int i = 1; i < levelCount; i++)
                    PlayerPrefs.DeleteKey(UnlockedKeyPrefix + i);

                for (int i = 0; i < levelCount; i++)
                {
                    string scene = GetLevelSceneName(levelsProp, i);
                    if (!string.IsNullOrEmpty(scene))
                        PlayerPrefs.DeleteKey(scene + "_Completed");
                }

                PlayerPrefs.Save();
                Debug.Log("[DebugLevelSelector] All progress reset.");
            }
        }
        GUI.backgroundColor = prevBg;

        // Repaint during play mode
        if (isPlaying)
            Repaint();
    }

    private void DrawSeparator()
    {
        EditorGUILayout.Space(2);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(2);
    }

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

    private string GetLevelSceneName(SerializedProperty levelsProp, int index)
    {
        if (levelsProp == null || index < 0 || index >= levelsProp.arraySize)
            return string.Empty;

        SerializedProperty element = levelsProp.GetArrayElementAtIndex(index);
        SerializedProperty sceneProp = element.FindPropertyRelative("sceneName");
        return sceneProp != null ? sceneProp.stringValue : string.Empty;
    }
}
