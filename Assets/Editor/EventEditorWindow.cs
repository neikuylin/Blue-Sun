using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EventEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/EventDatabase.asset";
    private const string CampSceneName = "\u8425\u5730";
    private const string CampCanvasName = "Canvas";
    private const string CampCharacterEventPrefix = "\u8425\u5730\u89d2\u8272\uff1a";

    private Vector2 scroll;
    private string newEventId = string.Empty;
    private string newEventName = string.Empty;
    private SerializedObject databaseObject;

    [MenuItem("Tools/事件/事件编辑器")]
    private static void Open()
    {
        EventEditorWindow window = GetWindow<EventEditorWindow>("事件编辑器");
        window.minSize = new Vector2(620f, 460f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EventDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("未找到 EventDatabase.asset，且自动创建失败。", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();

        EditorGUILayout.LabelField("事件编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里维护事件表。勾选表示启用，取消勾选表示禁用；后续运行时可按 eventId 读取这些状态。", MessageType.Info);
        EditorGUILayout.HelpBox("营地角色事件格式：营地角色：xx。运行到营地场景时，会自动控制 Canvas/xx 的 true/false。", MessageType.None);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存", GUILayout.Width(90f)))
            {
                databaseObject.ApplyModifiedProperties();
                SaveAsset(database);
            }
        }

        EditorGUILayout.Space(8f);
        DrawAddPanel(database);
        EditorGUILayout.Space(8f);

        SerializedProperty entriesProperty = databaseObject.FindProperty("entries");
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            DrawEntry(entriesProperty, i);
        }

        EditorGUILayout.EndScrollView();
        databaseObject.ApplyModifiedProperties();
        ApplyCampCharacterVisibilityInEditor(database);
    }

    private void DrawAddPanel(EventDatabase database)
    {
        EditorGUILayout.LabelField("新增事件", EditorStyles.boldLabel);
        newEventId = EditorGUILayout.TextField("事件ID", newEventId);
        newEventName = EditorGUILayout.TextField("事件名字", newEventName);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEventId)))
        {
            if (GUILayout.Button("新增事件"))
            {
                Undo.RecordObject(database, "新增事件");
                EventDatabase.EventEntry entry = database.GetOrCreateEntry(newEventId.Trim());
                if (entry != null && !string.IsNullOrWhiteSpace(newEventName))
                {
                    entry.displayName = newEventName.Trim();
                }

                newEventId = string.Empty;
                newEventName = string.Empty;
                EditorUtility.SetDirty(database);
            }
        }
    }

    private static void DrawEntry(SerializedProperty entriesProperty, int index)
    {
        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
        if (entryProperty == null)
        {
            return;
        }

        SerializedProperty idProperty = entryProperty.FindPropertyRelative("eventId");
        SerializedProperty nameProperty = entryProperty.FindPropertyRelative("displayName");
        SerializedProperty enabledProperty = entryProperty.FindPropertyRelative("enabled");
        SerializedProperty descriptionProperty = entryProperty.FindPropertyRelative("description");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string title = idProperty != null && !string.IsNullOrWhiteSpace(idProperty.stringValue)
                    ? idProperty.stringValue
                    : $"事件 {index + 1}";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    entriesProperty.DeleteArrayElementAtIndex(index);
                    return;
                }
            }

            if (enabledProperty != null)
            {
                enabledProperty.boolValue = EditorGUILayout.Toggle("启用", enabledProperty.boolValue);
            }

            if (idProperty != null)
            {
                EditorGUILayout.PropertyField(idProperty, new GUIContent("事件ID"));
            }

            if (nameProperty != null)
            {
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("事件名字"));
            }

            if (descriptionProperty != null)
            {
                EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("描述"));
            }
        }
    }

    private static EventDatabase EnsureDatabase()
    {
        EventDatabase database = AssetDatabase.LoadAssetAtPath<EventDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<EventDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static void SaveAsset(ScriptableObject asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyCampCharacterVisibilityInEditor(EventDatabase database)
    {
        if (Application.isPlaying || database == null || database.Entries == null)
        {
            return;
        }

        bool changed = false;
        int loadedSceneCount = SceneManager.sceneCount;
        for (int sceneIndex = 0; sceneIndex < loadedSceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded || scene.name != CampSceneName)
            {
                continue;
            }

            Transform canvas = SceneHierarchyPathUtility.Find(scene, CampCanvasName);
            if (canvas == null)
            {
                continue;
            }

            for (int i = 0; i < database.Entries.Count; i++)
            {
                EventDatabase.EventEntry entry = database.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.eventId))
                {
                    continue;
                }

                if (!entry.eventId.StartsWith(CampCharacterEventPrefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                string characterName = entry.eventId.Substring(CampCharacterEventPrefix.Length).Trim();
                if (string.IsNullOrWhiteSpace(characterName))
                {
                    continue;
                }

                Transform target = SceneHierarchyPathUtility.Find(scene, CampCanvasName + "/" + characterName);
                if (target == null)
                {
                    continue;
                }

                if (target.gameObject.activeSelf == entry.enabled)
                {
                    continue;
                }

                target.gameObject.SetActive(entry.enabled);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
