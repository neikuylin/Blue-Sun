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
        window.minSize = new Vector2(900f, 620f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueEventDatabase database = EnsureDatabase();
        DialogueContentDatabase contentDatabase = DialogueContentDatabase.LoadDefault();
        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        DialogueConditionDatabase conditionDatabase = DialogueConditionDatabase.LoadDefault();
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
            DrawEntry(database, database.Entries[i], i, contentDatabase, eventDatabase, conditionDatabase);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(
        DialogueEventDatabase database,
        DialogueEventDatabase.DialogueEventEntry entry,
        int index,
        DialogueContentDatabase contentDatabase,
        EventDatabase eventDatabase,
        DialogueConditionDatabase conditionDatabase)
    {
        if (entry == null)
        {
            return;
        }

        DialogueEventDatabase.EnsureEntry(entry);
        string foldoutKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
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

            if (!GetFoldoutState(foldoutKey))
            {
                return;
            }

            EditorGUILayout.LabelField("对话", EditorStyles.boldLabel);
            string nextId = EditorGUILayout.TextField("ID", entry.id);
            if (!string.Equals(nextId, entry.id, System.StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "修改对话事件ID");
                string oldKey = foldoutKey;
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
            DrawTriggerEventList(database, entry.trigger.eventIds, GetEventIds(eventDatabase));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("条件", EditorStyles.boldLabel);
            DrawConditionList(database, entry.condition.eventIds, GetConditionIds(conditionDatabase));
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

    private void DrawTriggerEventList(
        DialogueEventDatabase database,
        List<DialogueEventDatabase.TriggerEventEntry> triggers,
        List<string> sourceIds)
    {
        if (triggers == null)
        {
            return;
        }

        for (int i = 0; i < triggers.Count; i++)
        {
            DialogueEventDatabase.TriggerEventEntry trigger = triggers[i];
            if (trigger == null)
            {
                continue;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string nextEventId = DrawIdPopup($"事件编辑器ID {i + 1}", trigger.eventId, sourceIds);
                if (!string.Equals(nextEventId, trigger.eventId, System.StringComparison.Ordinal))
                {
                    Undo.RecordObject(database, "修改触发事件ID");
                    trigger.eventId = nextEventId;
                    SaveAsset(database);
                }

                bool nextExpectedValue = EditorGUILayout.Toggle(trigger.expectedValue, GUILayout.Width(20f));
                if (nextExpectedValue != trigger.expectedValue)
                {
                    Undo.RecordObject(database, "修改触发布尔值");
                    trigger.expectedValue = nextExpectedValue;
                    SaveAsset(database);
                }

                EditorGUILayout.LabelField(trigger.expectedValue ? "True" : "False", GUILayout.Width(40f));

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除触发事件");
                    triggers.RemoveAt(i);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUILayout.Button("新增事件编辑器ID", GUILayout.Width(130f)))
        {
            Undo.RecordObject(database, "新增触发事件");
            triggers.Add(new DialogueEventDatabase.TriggerEventEntry
            {
                eventId = string.Empty,
                expectedValue = true
            });
            SaveAsset(database);
        }
    }

    private void DrawConditionList(
        DialogueEventDatabase database,
        List<DialogueEventDatabase.ConditionEntry> conditions,
        List<string> sourceIds)
    {
        if (conditions == null)
        {
            return;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            DialogueEventDatabase.ConditionEntry condition = conditions[i];
            if (condition == null)
            {
                continue;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string nextConditionId = DrawIdPopup($"条件ID {i + 1}", condition.eventId, sourceIds);
                if (!string.Equals(nextConditionId, condition.eventId, System.StringComparison.Ordinal))
                {
                    Undo.RecordObject(database, "修改条件ID");
                    condition.eventId = nextConditionId;
                    SaveAsset(database);
                }

                int nextNumber = EditorGUILayout.IntField(condition.number, GUILayout.Width(60f));
                if (nextNumber != condition.number)
                {
                    Undo.RecordObject(database, "修改条件数字");
                    condition.number = nextNumber;
                    SaveAsset(database);
                }

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "删除条件");
                    conditions.RemoveAt(i);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUILayout.Button("新增条件ID", GUILayout.Width(100f)))
        {
            Undo.RecordObject(database, "新增条件");
            conditions.Add(new DialogueEventDatabase.ConditionEntry
            {
                eventId = string.Empty,
                number = 0
            });
            SaveAsset(database);
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

    private static List<string> GetConditionIds(DialogueConditionDatabase database)
    {
        List<string> result = new List<string>();
        if (database == null || database.Entries == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            DialogueConditionDatabase.ConditionDefinitionEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            result.Add(entry.id);
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
