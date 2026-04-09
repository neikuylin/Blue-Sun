using UnityEditor;
using UnityEngine;

public sealed class DialogueEventEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueEventDatabase.asset";

    private Vector2 scroll;
    private string newEventId = "dlg_evt_001";
    private string newEventName = "新对话事件";
    private SerializedObject databaseObject;

    [MenuItem("Tools/事件/对话事件编辑器")]
    private static void Open()
    {
        DialogueEventEditorWindow window = GetWindow<DialogueEventEditorWindow>("对话事件编辑器");
        window.minSize = new Vector2(760f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueEventDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话事件数据库创建失败。", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();

        EditorGUILayout.LabelField("对话事件编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里只定义事件词条和展示字段，不接运行时逻辑。可先把 eventId、dialogueId、触发类型、场景名、目标对象这些内容整理出来。", MessageType.Info);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存", GUILayout.Width(88f)))
            {
                databaseObject.ApplyModifiedProperties();
                SaveAsset(database);
            }

            if (GUILayout.Button("刷新", GUILayout.Width(88f)))
            {
                Repaint();
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

        if (databaseObject.ApplyModifiedProperties())
        {
            SaveAsset(database);
        }
    }

    private void DrawAddPanel(DialogueEventDatabase database)
    {
        EditorGUILayout.LabelField("新增事件词条", EditorStyles.boldLabel);
        newEventId = EditorGUILayout.TextField("事件ID", newEventId);
        newEventName = EditorGUILayout.TextField("显示名字", newEventName);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEventId)))
        {
            if (GUILayout.Button("新增对话事件"))
            {
                Undo.RecordObject(database, "新增对话事件");
                DialogueEventDatabase.DialogueEventEntry entry = database.GetOrCreateEntry(newEventId);
                if (entry != null && !string.IsNullOrWhiteSpace(newEventName))
                {
                    entry.displayName = newEventName.Trim();
                }

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

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SerializedProperty eventIdProperty = entryProperty.FindPropertyRelative("eventId");
                string title = eventIdProperty != null && !string.IsNullOrWhiteSpace(eventIdProperty.stringValue)
                    ? eventIdProperty.stringValue
                    : $"对话事件 {index + 1}";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    entriesProperty.DeleteArrayElementAtIndex(index);
                    return;
                }
            }

            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("enabled"), new GUIContent("启用"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("eventId"), new GUIContent("事件ID"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("displayName"), new GUIContent("显示名字"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("dialogueId"), new GUIContent("关联对话ID"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("triggerType"), new GUIContent("触发类型"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("sceneName"), new GUIContent("场景名"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("targetId"), new GUIContent("目标ID/对象名"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("speakerId"), new GUIContent("发起角色ID"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("playOnce"), new GUIContent("只播放一次"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("description"), new GUIContent("说明"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("tags"), new GUIContent("标签"), true);
        }
    }

    private static DialogueEventDatabase EnsureDatabase()
    {
        DialogueEventDatabase database = AssetDatabase.LoadAssetAtPath<DialogueEventDatabase>(AssetPath);
        if (database != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                DialogueEventDatabase.EnsureEntry(database.Entries[i]);
            }

            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueEventDatabase>();
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
}
