using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EventEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/EventDatabase.asset";
    private const string CampSceneName = "营地";
    private const string CampCanvasName = "Canvas";
    private const string CampCharacterEventPrefix = "营地角色：";
    private const string OptionalTeammateEventPrefix = "可选队友：";

    private Vector2 scroll;
    private string newEventId = string.Empty;
    private string newEventName = string.Empty;
    private bool showCampCharacterEvents = true;
    private bool showOptionalTeammateEvents = true;
    private bool showOtherEvents = true;
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
        EditorGUILayout.HelpBox("联动规则：营地角色：xx 的 true/false 会自动同步到 可选队友：xx。", MessageType.None);
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
        DrawGroupedEntries(entriesProperty);
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

    private void DrawGroupedEntries(SerializedProperty entriesProperty)
    {
        List<int> campCharacterIndices = new List<int>();
        List<int> optionalTeammateIndices = new List<int>();
        List<int> otherIndices = new List<int>();

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = entryProperty?.FindPropertyRelative("eventId");
            string eventId = idProperty != null ? idProperty.stringValue : string.Empty;
            if (eventId.StartsWith(CampCharacterEventPrefix, StringComparison.Ordinal))
            {
                campCharacterIndices.Add(i);
            }
            else if (eventId.StartsWith(OptionalTeammateEventPrefix, StringComparison.Ordinal))
            {
                optionalTeammateIndices.Add(i);
            }
            else
            {
                otherIndices.Add(i);
            }
        }

        showCampCharacterEvents = EditorGUILayout.Foldout(showCampCharacterEvents, $"营地角色 ({campCharacterIndices.Count})", true);
        if (showCampCharacterEvents)
        {
            DrawEntryGroup(entriesProperty, campCharacterIndices);
        }

        EditorGUILayout.Space(4f);
        showOptionalTeammateEvents = EditorGUILayout.Foldout(showOptionalTeammateEvents, $"可选队友 ({optionalTeammateIndices.Count})", true);
        if (showOptionalTeammateEvents)
        {
            DrawEntryGroup(entriesProperty, optionalTeammateIndices);
        }

        EditorGUILayout.Space(4f);
        showOtherEvents = EditorGUILayout.Foldout(showOtherEvents, $"其他事件 ({otherIndices.Count})", true);
        if (showOtherEvents)
        {
            DrawEntryGroup(entriesProperty, otherIndices);
        }
    }

    private void DrawEntryGroup(SerializedProperty entriesProperty, List<int> indices)
    {
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (index < 0 || index >= entriesProperty.arraySize)
            {
                continue;
            }

            if (DrawEntry(entriesProperty, index))
            {
                GUIUtility.ExitGUI();
            }
        }
    }

    private bool DrawEntry(SerializedProperty entriesProperty, int index)
    {
        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
        if (entryProperty == null)
        {
            return false;
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
                    return true;
                }
            }

            if (enabledProperty != null)
            {
                bool previousValue = enabledProperty.boolValue;
                enabledProperty.boolValue = EditorGUILayout.Toggle("启用", enabledProperty.boolValue);
                if (enabledProperty.boolValue != previousValue)
                {
                    SyncLinkedOptionalTeammate(entriesProperty, idProperty != null ? idProperty.stringValue : string.Empty, enabledProperty.boolValue);
                }
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

        return false;
    }

    private static void SyncLinkedOptionalTeammate(SerializedProperty entriesProperty, string eventId, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(eventId) || !eventId.StartsWith(CampCharacterEventPrefix, StringComparison.Ordinal))
        {
            return;
        }

        string characterName = eventId.Substring(CampCharacterEventPrefix.Length).Trim();
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return;
        }

        string linkedEventId = OptionalTeammateEventPrefix + characterName;
        SerializedProperty linkedEntry = FindEntryById(entriesProperty, linkedEventId);
        if (linkedEntry == null)
        {
            entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
            linkedEntry = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
            linkedEntry.FindPropertyRelative("eventId").stringValue = linkedEventId;
            linkedEntry.FindPropertyRelative("displayName").stringValue = linkedEventId;
            linkedEntry.FindPropertyRelative("description").stringValue = string.Empty;
        }

        linkedEntry.FindPropertyRelative("enabled").boolValue = enabled;
    }

    private static SerializedProperty FindEntryById(SerializedProperty entriesProperty, string eventId)
    {
        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = entryProperty?.FindPropertyRelative("eventId");
            if (idProperty != null && string.Equals(idProperty.stringValue, eventId, StringComparison.Ordinal))
            {
                return entryProperty;
            }
        }

        return null;
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

                if (!entry.eventId.StartsWith(CampCharacterEventPrefix, StringComparison.Ordinal))
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
