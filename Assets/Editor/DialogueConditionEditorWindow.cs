using UnityEditor;
using UnityEngine;

public sealed class DialogueConditionEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueConditionDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;

    [MenuItem("Tools/事件/对话条件编辑器")]
    private static void Open()
    {
        DialogueConditionEditorWindow window = GetWindow<DialogueConditionEditorWindow>("对话条件编辑器");
        window.minSize = new Vector2(560f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueConditionDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话条件数据库创建失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("对话条件编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("新增ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "新增对话条件");
                    database.GetOrCreateEntry(newId);
                    SaveAsset(database);
                    newId = string.Empty;
                }
            }
        }

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            DrawEntry(database, database.Entries[i], i);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawEntry(DialogueConditionDatabase database, DialogueConditionDatabase.ConditionDefinitionEntry entry, int index)
    {
        if (entry == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.id) ? $"条件 {index + 1}" : entry.id, EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除对话条件");
                    database.Entries.RemoveAt(index);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }

            string nextId = EditorGUILayout.TextField("ID", entry.id);
            if (!string.Equals(nextId, entry.id, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改对话条件ID");
                entry.id = nextId;
                SaveAsset(database);
            }

            int nextNumber = EditorGUILayout.IntField("数字", entry.number);
            if (nextNumber != entry.number)
            {
                Undo.RecordObject(database, "修改对话条件数字");
                entry.number = nextNumber;
                SaveAsset(database);
            }
        }
    }

    private static DialogueConditionDatabase EnsureDatabase()
    {
        DialogueConditionDatabase database = AssetDatabase.LoadAssetAtPath<DialogueConditionDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueConditionDatabase>();
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
