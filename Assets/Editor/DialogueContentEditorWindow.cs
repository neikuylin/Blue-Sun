using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class DialogueContentEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueContentDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;
    private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/事件/对话内容编辑器")]
    private static void Open()
    {
        DialogueContentEditorWindow window = GetWindow<DialogueContentEditorWindow>("对话内容编辑器");
        window.minSize = new Vector2(860f, 620f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueContentDatabase database = EnsureDatabase();
        DialogueRoleNameDatabase roleNameDatabase = DialogueRoleNameDatabase.LoadDefault();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话内容数据库创建失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("对话内容编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("新增对话ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "新增对话内容");
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
            DrawEntry(database, database.Entries[i], i, roleNameDatabase);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(
        DialogueContentDatabase database,
        DialogueContentDatabase.DialogueContentEntry entry,
        int index,
        DialogueRoleNameDatabase roleNameDatabase)
    {
        if (entry == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            string foldoutKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isExpanded = GetFoldoutState(foldoutKey);
                string title = string.IsNullOrWhiteSpace(entry.id) ? $"内容 {index + 1}" : entry.id;
                bool nextExpanded = EditorGUILayout.Foldout(isExpanded, title, true);
                if (nextExpanded != isExpanded)
                {
                    foldoutStates[foldoutKey] = nextExpanded;
                }

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除对话内容");
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

            string nextId = EditorGUILayout.TextField("对话ID", entry.id);
            if (!string.Equals(nextId, entry.id, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改对话ID");
                string oldKey = foldoutKey;
                entry.id = nextId;
                string newKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
                bool expanded = GetFoldoutState(oldKey);
                foldoutStates.Remove(oldKey);
                foldoutStates[newKey] = expanded;
                SaveAsset(database);
            }

            entry.roleNameId = DrawIdPopup(
                "角色名字",
                entry.roleNameId,
                GetRoleNameIds(roleNameDatabase));

            Sprite nextPortraitSprite = (Sprite)EditorGUILayout.ObjectField("立绘 Sprite2D", entry.portraitSprite2D, typeof(Sprite), false);
            if (nextPortraitSprite != entry.portraitSprite2D)
            {
                Undo.RecordObject(database, "修改立绘");
                entry.portraitSprite2D = nextPortraitSprite;
                SaveAsset(database);
            }

            DialogueContentDatabase.DialogueViewSide nextViewSide =
                (DialogueContentDatabase.DialogueViewSide)EditorGUILayout.EnumPopup("视角", entry.viewSide);
            if (nextViewSide != entry.viewSide)
            {
                Undo.RecordObject(database, "修改视角");
                entry.viewSide = nextViewSide;
                SaveAsset(database);
            }

            string nextContent = EditorGUILayout.TextArea(entry.content, GUILayout.MinHeight(90f));
            if (!string.Equals(nextContent, entry.content, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改对话内容");
                entry.content = nextContent;
                SaveAsset(database);
            }
        }
    }

    private bool GetFoldoutState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        bool expanded;
        if (foldoutStates.TryGetValue(key, out expanded))
        {
            return expanded;
        }

        foldoutStates[key] = true;
        return true;
    }

    private string DrawIdPopup(string label, string currentValue, List<string> values)
    {
        List<string> options = new List<string> { string.Empty };
        if (values != null)
        {
            options.AddRange(values);
        }

        int currentIndex = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], currentValue, System.StringComparison.Ordinal))
            {
                currentIndex = i;
                break;
            }
        }

        string[] displayOptions = new string[options.Count];
        for (int i = 0; i < options.Count; i++)
        {
            displayOptions[i] = string.IsNullOrWhiteSpace(options[i]) ? "None" : options[i];
        }

        int nextIndex = EditorGUILayout.Popup(label, currentIndex, displayOptions);
        if (nextIndex < 0 || nextIndex >= options.Count)
        {
            return currentValue;
        }

        return options[nextIndex];
    }

    private static List<string> GetRoleNameIds(DialogueRoleNameDatabase database)
    {
        List<string> result = new List<string>();
        if (database == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            DialogueRoleNameDatabase.RoleNameEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            result.Add(entry.id);
        }

        return result;
    }

    private static DialogueContentDatabase EnsureDatabase()
    {
        DialogueContentDatabase database = AssetDatabase.LoadAssetAtPath<DialogueContentDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueContentDatabase>();
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
