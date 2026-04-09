using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class DialogueEventEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueEventDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;
    private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/事件/对话事件编辑器")]
    private static void Open()
    {
        DialogueEventEditorWindow window = GetWindow<DialogueEventEditorWindow>("对话事件编辑器");
        window.minSize = new Vector2(820f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueEventDatabase database = EnsureDatabase();
        DialogueContentDatabase contentDatabase = DialogueContentDatabase.LoadDefault();
        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话事件数据库创建失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("对话事件编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("新增ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "新增对话事件");
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
            DrawEntry(database, database.Entries[i], i, contentDatabase, eventDatabase);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(
        DialogueEventDatabase database,
        DialogueEventDatabase.DialogueEventEntry entry,
        int index,
        DialogueContentDatabase contentDatabase,
        EventDatabase eventDatabase)
    {
        if (entry == null)
        {
            return;
        }

        DialogueEventDatabase.EnsureEntry(entry);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string foldoutKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
                bool isExpanded = GetFoldoutState(foldoutKey);
                string title = string.IsNullOrWhiteSpace(entry.id) ? $"事件 {index + 1}" : entry.id;
                bool nextExpanded = EditorGUILayout.Foldout(isExpanded, title, true);
                if (nextExpanded != isExpanded)
                {
                    foldoutStates[foldoutKey] = nextExpanded;
                }
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除对话事件");
                    database.Entries.RemoveAt(index);
                    foldoutStates.Remove(foldoutKey);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }

            string currentFoldoutKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
            if (!GetFoldoutState(currentFoldoutKey))
            {
                return;
            }

            string nextId = EditorGUILayout.TextField("ID", entry.id);
            if (!string.Equals(nextId, entry.id, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改对话事件ID");
                string oldKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
                entry.id = nextId;
                string newKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
                bool expanded = GetFoldoutState(oldKey);
                foldoutStates.Remove(oldKey);
                foldoutStates[newKey] = expanded;
                SaveAsset(database);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("表现", EditorStyles.boldLabel);
            entry.presentation.dialogueContentId = DrawIdPopup(
                "对话内容ID",
                entry.presentation.dialogueContentId,
                GetDialogueContentIds(contentDatabase));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("触发", EditorStyles.boldLabel);
            DrawButtonList(database, entry.trigger.buttons);
            DrawEventIdList(database, "事件编辑器ID", entry.trigger.eventIds, GetEventIds(eventDatabase));
            DrawEventIdList(database, "条件", entry.trigger.conditionEventIds, GetEventIds(eventDatabase));
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

    private void DrawButtonList(DialogueEventDatabase database, List<GameObject> buttons)
    {
        if (buttons == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject current = buttons[i];
            GameObject next = (GameObject)EditorGUILayout.ObjectField($"按钮 {i + 1}", current, typeof(GameObject), true);
            if (next != current)
            {
                Undo.RecordObject(database, "修改按钮绑定");
                buttons[i] = next;
                SaveAsset(database);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新增按钮", GUILayout.Width(88f)))
            {
                Undo.RecordObject(database, "新增按钮绑定");
                buttons.Add(null);
                SaveAsset(database);
            }

            using (new EditorGUI.DisabledScope(buttons.Count == 0))
            {
                if (GUILayout.Button("删除最后一个按钮", GUILayout.Width(130f)))
                {
                    Undo.RecordObject(database, "删除按钮绑定");
                    buttons.RemoveAt(buttons.Count - 1);
                    SaveAsset(database);
                }
            }
        }
    }

    private void DrawEventIdList(DialogueEventDatabase database, string label, List<string> targetList, List<string> sourceIds)
    {
        if (targetList == null)
        {
            return;
        }

        for (int i = 0; i < targetList.Count; i++)
        {
            string nextValue = DrawIdPopup($"{label} {i + 1}", targetList[i], sourceIds);
            if (!string.Equals(nextValue, targetList[i], System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, $"修改{label}");
                targetList[i] = nextValue;
                SaveAsset(database);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button($"新增{label}", GUILayout.Width(100f)))
            {
                Undo.RecordObject(database, $"新增{label}");
                targetList.Add(string.Empty);
                SaveAsset(database);
            }

            using (new EditorGUI.DisabledScope(targetList.Count == 0))
            {
                if (GUILayout.Button($"删除最后一个{label}", GUILayout.Width(140f)))
                {
                    Undo.RecordObject(database, $"删除{label}");
                    targetList.RemoveAt(targetList.Count - 1);
                    SaveAsset(database);
                }
            }
        }
    }

    private static List<string> GetDialogueContentIds(DialogueContentDatabase database)
    {
        List<string> result = new List<string>();
        if (database == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            DialogueContentDatabase.DialogueContentEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            result.Add(entry.id);
        }

        return result;
    }

    private static List<string> GetEventIds(EventDatabase database)
    {
        List<string> result = new List<string>();
        if (database == null || database.Entries == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            EventDatabase.EventEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.eventId))
            {
                continue;
            }

            result.Add(entry.eventId);
        }

        return result;
    }

    private static DialogueEventDatabase EnsureDatabase()
    {
        DialogueEventDatabase database = AssetDatabase.LoadAssetAtPath<DialogueEventDatabase>(AssetPath);
        if (database != null)
        {
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
