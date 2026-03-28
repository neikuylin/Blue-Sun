using UnityEditor;
using UnityEngine;

public sealed class RoomTypeEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/RoomTypeDatabase.asset";

    private Vector2 scroll;
    private string newRoomTypeId = string.Empty;
    private string newRoomTypeName = string.Empty;
    private SerializedObject databaseObject;

    [MenuItem("Tools/战斗/房间类型编辑器")]
    private static void Open()
    {
        RoomTypeEditorWindow window = GetWindow<RoomTypeEditorWindow>("房间类型编辑器");
        window.minSize = new Vector2(560f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        RoomTypeDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("房间类型库创建失败。", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        EnsureDefaultEntries(database);
        databaseObject.Update();

        EditorGUILayout.LabelField("房间类型编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里只维护房间类型定义。先支持新增类型 ID 和类型名字，后续其他房间系统直接引用。", MessageType.Info);
        EditorGUILayout.Space(6f);

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

    private void DrawAddPanel(RoomTypeDatabase database)
    {
        EditorGUILayout.LabelField("新增房间类型", EditorStyles.boldLabel);
        newRoomTypeId = EditorGUILayout.TextField("类型ID", newRoomTypeId);
        newRoomTypeName = EditorGUILayout.TextField("类型名字", newRoomTypeName);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newRoomTypeId) || string.IsNullOrWhiteSpace(newRoomTypeName)))
        {
            if (GUILayout.Button("新增类型"))
            {
                Undo.RecordObject(database, "新增房间类型");
                RoomTypeDatabase.RoomTypeEntry entry = database.GetOrCreateEntry(newRoomTypeId.Trim());
                entry.displayName = newRoomTypeName.Trim();
                SaveAsset(database);
                newRoomTypeId = string.Empty;
                newRoomTypeName = string.Empty;
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

        SerializedProperty idProperty = entryProperty.FindPropertyRelative("roomTypeId");
        SerializedProperty nameProperty = entryProperty.FindPropertyRelative("displayName");
        bool isEncounterBattle = string.Equals(idProperty.stringValue, RoomTypeDatabase.EncounterBattleTypeId, System.StringComparison.Ordinal);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"房间类型 {index + 1}", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(isEncounterBattle))
                {
                    if (GUILayout.Button("删除", GUILayout.Width(72f)))
                    {
                        entriesProperty.DeleteArrayElementAtIndex(index);
                        return;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(isEncounterBattle))
            {
                EditorGUILayout.PropertyField(idProperty, new GUIContent("类型ID"));
            }

            EditorGUILayout.PropertyField(nameProperty, new GUIContent("类型名字"));

            if (isEncounterBattle)
            {
                EditorGUILayout.HelpBox("遭遇战房间是默认创建的类型，遭遇战编辑器会固定归属到它。", MessageType.Info);
            }
        }
    }

    private static RoomTypeDatabase EnsureDatabase()
    {
        RoomTypeDatabase database = AssetDatabase.LoadAssetAtPath<RoomTypeDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<RoomTypeDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EnsureDefaultEntries(database);
        return database;
    }

    private static void EnsureDefaultEntries(RoomTypeDatabase database)
    {
        if (database == null)
        {
            return;
        }

        bool changed = false;
        RoomTypeDatabase.RoomTypeEntry encounterBattle = database.GetOrCreateEntry(RoomTypeDatabase.EncounterBattleTypeId);
        if (encounterBattle != null && !string.Equals(encounterBattle.displayName, RoomTypeDatabase.EncounterBattleTypeName, System.StringComparison.Ordinal))
        {
            encounterBattle.displayName = RoomTypeDatabase.EncounterBattleTypeName;
            changed = true;
        }

        if (changed)
        {
            SaveAsset(database);
        }
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
