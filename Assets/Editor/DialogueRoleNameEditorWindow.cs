using UnityEditor;
using UnityEngine;

public sealed class DialogueRoleNameEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueRoleNameDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;

    [MenuItem("Tools/事件/对话角色名字编辑器")]
    private static void Open()
    {
        DialogueRoleNameEditorWindow window = GetWindow<DialogueRoleNameEditorWindow>("对话角色名字编辑器");
        window.minSize = new Vector2(520f, 460f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueRoleNameDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话角色名字数据库创建失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("对话角色名字编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("新增ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "新增角色名字ID");
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

    private static void DrawEntry(DialogueRoleNameDatabase database, DialogueRoleNameDatabase.RoleNameEntry entry, int index)
    {
        if (entry == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.id) ? $"名字 {index + 1}" : entry.id, EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除角色名字ID");
                    database.Entries.RemoveAt(index);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }

            string nextId = EditorGUILayout.TextField("ID", entry.id);
            if (!string.Equals(nextId, entry.id, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改角色名字ID");
                entry.id = nextId;
                SaveAsset(database);
            }
        }
    }

    private static DialogueRoleNameDatabase EnsureDatabase()
    {
        DialogueRoleNameDatabase database = AssetDatabase.LoadAssetAtPath<DialogueRoleNameDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueRoleNameDatabase>();
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
