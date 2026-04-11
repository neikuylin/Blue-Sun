using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class DialogueGroupEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueGroupDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;
    private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/事件/对话组编辑器")]
    private static void Open()
    {
        DialogueGroupEditorWindow window = GetWindow<DialogueGroupEditorWindow>("对话组编辑器");
        window.minSize = new Vector2(920f, 620f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueGroupDatabase database = EnsureDatabase();
        DialogueContentDatabase contentDatabase = DialogueContentDatabase.LoadDefault();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话组数据库创建失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("对话组编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("新增组ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "新增对话组");
                    database.GetOrCreateEntry(newId);
                    SaveAsset(database);
                    foldoutStates[newId.Trim()] = true;
                    newId = string.Empty;
                }
            }
        }

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            DrawEntry(database, database.Entries[i], i, contentDatabase);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(
        DialogueGroupDatabase database,
        DialogueGroupDatabase.DialogueGroupEntry entry,
        int index,
        DialogueContentDatabase contentDatabase)
    {
        if (entry == null)
        {
            return;
        }

        DialogueGroupDatabase.EnsureEntry(entry);
        string foldoutKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isExpanded = GetFoldoutState(foldoutKey);
                string title = string.IsNullOrWhiteSpace(entry.id) ? $"对话组 {index + 1}" : entry.id;
                bool nextExpanded = EditorGUILayout.Foldout(isExpanded, title, true);
                if (nextExpanded != isExpanded)
                {
                    foldoutStates[foldoutKey] = nextExpanded;
                }

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除对话组");
                    database.Entries.RemoveAt(index);
                    foldoutStates.Remove(foldoutKey);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }

            if (!GetFoldoutState(foldoutKey))
            {
                return;
            }

            string nextId = EditorGUILayout.TextField("组ID", entry.id);
            if (!string.Equals(nextId, entry.id, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改对话组ID");
                string oldKey = foldoutKey;
                entry.id = nextId;
                string newKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
                bool expanded = GetFoldoutState(oldKey);
                foldoutStates.Remove(oldKey);
                foldoutStates[newKey] = expanded;
                SaveAsset(database);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("内容ID顺序", EditorStyles.boldLabel);
            DrawContentIdList(database, entry.contentIds, contentDatabase);
        }
    }

    private void DrawContentIdList(
        DialogueGroupDatabase database,
        List<string> contentIds,
        DialogueContentDatabase contentDatabase)
    {
        if (contentIds == null)
        {
            return;
        }

        for (int i = 0; i < contentIds.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string currentValue = contentIds[i] ?? string.Empty;
                string nextValue = EditorGUILayout.TextField($"内容ID {i + 1}", currentValue);
                if (!string.Equals(nextValue, currentValue, System.StringComparison.Ordinal))
                {
                    Undo.RecordObject(database, "修改对话组内容ID");
                    contentIds[i] = nextValue;
                    SaveAsset(database);
                }

                bool exists = contentDatabase != null && contentDatabase.FindEntry(nextValue) != null;
                GUIStyle stateStyle = new GUIStyle(EditorStyles.miniLabel);
                stateStyle.normal.textColor = exists ? new Color(0.2f, 0.65f, 0.25f) : new Color(0.8f, 0.2f, 0.2f);
                GUILayout.Label(exists ? "已存在" : "未找到", stateStyle, GUILayout.Width(48f));

                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("上移", GUILayout.Width(48f)))
                    {
                        Undo.RecordObject(database, "上移对话组内容ID");
                        string value = contentIds[i - 1];
                        contentIds[i - 1] = contentIds[i];
                        contentIds[i] = value;
                        SaveAsset(database);
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(i >= contentIds.Count - 1))
                {
                    if (GUILayout.Button("下移", GUILayout.Width(48f)))
                    {
                        Undo.RecordObject(database, "下移对话组内容ID");
                        string value = contentIds[i + 1];
                        contentIds[i + 1] = contentIds[i];
                        contentIds[i] = value;
                        SaveAsset(database);
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("删除", GUILayout.Width(48f)))
                {
                    Undo.RecordObject(database, "删除对话组内容ID");
                    contentIds.RemoveAt(i);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUILayout.Button("新增内容ID", GUILayout.Width(100f)))
        {
            Undo.RecordObject(database, "新增对话组内容ID");
            contentIds.Add(string.Empty);
            SaveAsset(database);
        }
    }

    private bool GetFoldoutState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        if (foldoutStates.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        foldoutStates[key] = true;
        return true;
    }

    private static DialogueGroupDatabase EnsureDatabase()
    {
        DialogueGroupDatabase database = AssetDatabase.LoadAssetAtPath<DialogueGroupDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueGroupDatabase>();
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
