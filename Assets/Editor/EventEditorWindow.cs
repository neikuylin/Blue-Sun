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
    private string newEventCategoryId = EventDatabase.BackpackLevelCategoryId;
    private string newCategoryId = string.Empty;
    private string newCategoryName = string.Empty;
    private bool showCategoryPanel = true;
    private readonly Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();
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

        if (database.EnsureCategoryList())
        {
            EditorUtility.SetDirty(database);
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
        SerializedProperty categoriesProperty = databaseObject.FindProperty("categories");
        SerializedProperty entriesProperty = databaseObject.FindProperty("entries");
        DrawCategoryPanel(database, categoriesProperty, entriesProperty);
        EditorGUILayout.Space(8f);
        DrawAddPanel(database, categoriesProperty);
        EditorGUILayout.Space(8f);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawGroupedEntries(categoriesProperty, entriesProperty);
        EditorGUILayout.EndScrollView();

        databaseObject.ApplyModifiedProperties();
        ApplyCampCharacterVisibilityInEditor(database);
    }

    private void DrawCategoryPanel(EventDatabase database, SerializedProperty categoriesProperty, SerializedProperty entriesProperty)
    {
        int categoryCount = categoriesProperty != null ? categoriesProperty.arraySize : 0;
        showCategoryPanel = EditorGUILayout.Foldout(showCategoryPanel, $"事件分类 ({categoryCount})", true);
        if (!showCategoryPanel)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                newCategoryId = EditorGUILayout.TextField("新分类ID", newCategoryId);
                newCategoryName = EditorGUILayout.TextField("显示名字", newCategoryName);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newCategoryId)))
                {
                    if (GUILayout.Button("新增分类", GUILayout.Width(90f)))
                    {
                        AddCategory(database, categoriesProperty, newCategoryId.Trim(), newCategoryName.Trim());
                        newCategoryId = string.Empty;
                        newCategoryName = string.Empty;
                    }
                }
            }

            if (categoriesProperty == null || categoriesProperty.arraySize <= 0)
            {
                EditorGUILayout.HelpBox("还没有事件分类。", MessageType.Info);
                return;
            }

            for (int i = 0; i < categoriesProperty.arraySize; i++)
            {
                SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex(i);
                DrawCategoryRow(database, categoriesProperty, entriesProperty, categoryProperty, i);
            }
        }
    }

    private void DrawCategoryRow(
        EventDatabase database,
        SerializedProperty categoriesProperty,
        SerializedProperty entriesProperty,
        SerializedProperty categoryProperty,
        int index)
    {
        SerializedProperty idProperty = categoryProperty.FindPropertyRelative("categoryId");
        SerializedProperty nameProperty = categoryProperty.FindPropertyRelative("displayName");
        string oldCategoryId = idProperty != null ? idProperty.stringValue : string.Empty;
        int eventCount = CountEntriesInCategory(entriesProperty, oldCategoryId);
        string categoryId = idProperty != null ? idProperty.stringValue : string.Empty;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"分类 {index + 1}", GUILayout.Width(58f));

                if (idProperty != null)
                {
                    EditorGUI.BeginChangeCheck();
                    string newId = EditorGUILayout.TextField(idProperty.stringValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        string resolvedId = newId.Trim();
                        if (!string.IsNullOrWhiteSpace(resolvedId) && !CategoryIdExists(categoriesProperty, resolvedId, index))
                        {
                            Undo.RecordObject(database, "修改事件分类ID");
                            idProperty.stringValue = resolvedId;
                            RenameEntryCategory(entriesProperty, oldCategoryId, resolvedId);
                            categoryId = resolvedId;
                        }
                    }
                }

                if (nameProperty != null)
                {
                    EditorGUILayout.PropertyField(nameProperty, GUIContent.none);
                }

                EditorGUILayout.LabelField($"{eventCount} 个事件", GUILayout.Width(72f));
                using (new EditorGUI.DisabledScope(eventCount > 0))
                {
                    if (GUILayout.Button("删除", GUILayout.Width(56f)))
                    {
                        Undo.RecordObject(database, "删除事件分类");
                        categoriesProperty.DeleteArrayElementAtIndex(index);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            DrawStoryCategoryHint(entriesProperty, categoryId);
        }
    }

    private static string DrawStoryPopup(string label, string currentStoryId)
    {
        剧情数据库 storyDatabase = 剧情数据库.加载默认数据库();
        List<string> ids = new List<string> { string.Empty };
        List<string> names = new List<string> { "不绑定" };
        if (storyDatabase != null)
        {
            List<剧情数据库.剧情条目> entries = storyDatabase.取得剧情列表();
            for (int i = 0; i < entries.Count; i++)
            {
                剧情数据库.剧情条目 entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.剧情ID))
                {
                    continue;
                }

                ids.Add(entry.剧情ID);
                names.Add(entry.剧情ID);
            }
        }

        int currentIndex = ids.FindIndex(id => string.Equals(id, currentStoryId, StringComparison.Ordinal));
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int nextIndex = EditorGUILayout.Popup(label, currentIndex, names.ToArray());
        return ids[Mathf.Clamp(nextIndex, 0, ids.Count - 1)];
    }

    private static void DrawStoryCategoryHint(SerializedProperty entriesProperty, string categoryId)
    {
        if (!string.Equals(categoryId, EventDatabase.StoryCategoryId, StringComparison.Ordinal))
        {
            return;
        }

        if (FindEntryById(entriesProperty, 事件剧情硬编码规则.出生剧情事件ID) != null)
        {
            EditorGUILayout.HelpBox(
                "剧情大类中的每个事件都可以单独绑定一个剧情。硬编码触发目前只检查“出生剧情”这个事件。",
                MessageType.Info);
        }
    }

    private void DrawAddPanel(EventDatabase database, SerializedProperty categoriesProperty)
    {
        EditorGUILayout.LabelField("新增事件", EditorStyles.boldLabel);
        newEventId = EditorGUILayout.TextField("事件ID", newEventId);
        newEventName = EditorGUILayout.TextField("事件名字", newEventName);
        newEventCategoryId = DrawCategoryPopup("事件分类", newEventCategoryId, categoriesProperty);

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

                if (entry != null)
                {
                    entry.categoryId = ResolveCategoryForNewEvent(newEventCategoryId, newEventId);
                }

                newEventId = string.Empty;
                newEventName = string.Empty;
                EditorUtility.SetDirty(database);
            }
        }
    }

    private void DrawGroupedEntries(SerializedProperty categoriesProperty, SerializedProperty entriesProperty)
    {
        if (categoriesProperty == null)
        {
            DrawEntryGroup(entriesProperty, CollectEntriesForCategory(entriesProperty, string.Empty), null);
            return;
        }

        HashSet<int> drawnIndices = new HashSet<int>();
        for (int i = 0; i < categoriesProperty.arraySize; i++)
        {
            SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = categoryProperty?.FindPropertyRelative("categoryId");
            SerializedProperty nameProperty = categoryProperty?.FindPropertyRelative("displayName");
            string categoryId = idProperty != null ? idProperty.stringValue : string.Empty;
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                continue;
            }

            List<int> indices = CollectEntriesForCategory(entriesProperty, categoryId);
            for (int entryIndex = 0; entryIndex < indices.Count; entryIndex++)
            {
                drawnIndices.Add(indices[entryIndex]);
            }

            string displayName = nameProperty != null && !string.IsNullOrWhiteSpace(nameProperty.stringValue)
                ? nameProperty.stringValue
                : categoryId;
            bool show = GetCategoryFoldout(categoryId);
            show = EditorGUILayout.Foldout(show, $"{displayName} ({indices.Count})", true);
            categoryFoldouts[categoryId] = show;
            if (show)
            {
                DrawEntryGroup(entriesProperty, indices, categoriesProperty);
            }

            EditorGUILayout.Space(4f);
        }

        List<int> uncategorizedIndices = CollectUncategorizedEntries(entriesProperty, drawnIndices);
        if (uncategorizedIndices.Count <= 0)
        {
            return;
        }

        bool showUncategorized = GetCategoryFoldout(string.Empty);
        showUncategorized = EditorGUILayout.Foldout(showUncategorized, $"未分类 ({uncategorizedIndices.Count})", true);
        categoryFoldouts[string.Empty] = showUncategorized;
        if (showUncategorized)
        {
            DrawEntryGroup(entriesProperty, uncategorizedIndices, categoriesProperty);
        }
    }

    private void DrawEntryGroup(SerializedProperty entriesProperty, List<int> indices, SerializedProperty categoriesProperty)
    {
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (index < 0 || index >= entriesProperty.arraySize)
            {
                continue;
            }

            if (DrawEntry(entriesProperty, index, categoriesProperty))
            {
                GUIUtility.ExitGUI();
            }
        }
    }

    private bool DrawEntry(SerializedProperty entriesProperty, int index, SerializedProperty categoriesProperty)
    {
        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
        if (entryProperty == null)
        {
            return false;
        }

        SerializedProperty idProperty = entryProperty.FindPropertyRelative("eventId");
        SerializedProperty nameProperty = entryProperty.FindPropertyRelative("displayName");
        SerializedProperty categoryProperty = entryProperty.FindPropertyRelative("categoryId");
        SerializedProperty boundStoryIdProperty = entryProperty.FindPropertyRelative("boundStoryId");
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

            if (categoryProperty != null)
            {
                categoryProperty.stringValue = DrawCategoryPopup("事件分类", categoryProperty.stringValue, categoriesProperty);
            }

            if (categoryProperty != null &&
                boundStoryIdProperty != null &&
                string.Equals(categoryProperty.stringValue, EventDatabase.StoryCategoryId, StringComparison.Ordinal))
            {
                boundStoryIdProperty.stringValue = DrawStoryPopup("绑定剧情", boundStoryIdProperty.stringValue);
                DrawHardcodedStoryRuleHint(idProperty != null ? idProperty.stringValue : string.Empty, boundStoryIdProperty.stringValue);
            }

            if (descriptionProperty != null)
            {
                EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("描述"));
            }
        }

        return false;
    }

    private static void DrawHardcodedStoryRuleHint(string eventId, string boundStoryId)
    {
        if (!string.Equals(eventId, 事件剧情硬编码规则.出生剧情事件ID, StringComparison.Ordinal))
        {
            return;
        }

        string storyText = string.IsNullOrWhiteSpace(boundStoryId) ? "未绑定剧情" : boundStoryId;
        EditorGUILayout.HelpBox(
            $"硬编码逻辑：点击开始界面的“开始游戏”按钮时，如果事件“{事件剧情硬编码规则.出生剧情事件ID}”当前是勾选状态，就播放这个事件自己绑定的剧情；播放请求发出后，不修改事件“{事件剧情硬编码规则.出生剧情事件ID}”的勾选状态。当前绑定剧情：{storyText}。",
            MessageType.Info);
        EditorGUILayout.HelpBox(
            "出生剧情战斗入口硬编码数据：剧情蓝图执行“切换场景”节点进入战斗副本时，先登记节点里填写的地图模板和房间；如果当前剧情ID是“出生剧情”，就写入角色选择：玩家、库鲁斯；并给库鲁斯主手装备生成 itm_直剑。",
            MessageType.Info);
        EditorGUILayout.HelpBox(
            "给之后检查用：这里的中文说明必须和代码里的硬编码触发逻辑保持一致；如果改了代码条件，也必须同步改这段中文说明。",
            MessageType.Warning);
    }

    private static void AddCategory(EventDatabase database, SerializedProperty categoriesProperty, string categoryId, string displayName)
    {
        if (database == null || categoriesProperty == null || string.IsNullOrWhiteSpace(categoryId))
        {
            return;
        }

        if (CategoryIdExists(categoriesProperty, categoryId, -1))
        {
            Debug.LogWarning($"事件编辑器: 分类ID '{categoryId}' 已存在。");
            return;
        }

        Undo.RecordObject(database, "新增事件分类");
        int index = categoriesProperty.arraySize;
        categoriesProperty.InsertArrayElementAtIndex(index);
        SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex(index);
        categoryProperty.FindPropertyRelative("categoryId").stringValue = categoryId;
        categoryProperty.FindPropertyRelative("displayName").stringValue = string.IsNullOrWhiteSpace(displayName) ? categoryId : displayName;
    }

    private static string DrawCategoryPopup(string label, string currentCategoryId, SerializedProperty categoriesProperty)
    {
        List<string> ids = new List<string>();
        List<string> names = new List<string>();
        if (categoriesProperty != null)
        {
            for (int i = 0; i < categoriesProperty.arraySize; i++)
            {
                SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex(i);
                string id = categoryProperty.FindPropertyRelative("categoryId")?.stringValue ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                string displayName = categoryProperty.FindPropertyRelative("displayName")?.stringValue;
                ids.Add(id);
                names.Add(string.IsNullOrWhiteSpace(displayName) ? id : displayName);
            }
        }

        if (ids.Count <= 0)
        {
            EditorGUILayout.TextField(label, currentCategoryId);
            return currentCategoryId;
        }

        int currentIndex = ids.FindIndex(id => string.Equals(id, currentCategoryId, StringComparison.Ordinal));
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int nextIndex = EditorGUILayout.Popup(label, currentIndex, names.ToArray());
        return ids[Mathf.Clamp(nextIndex, 0, ids.Count - 1)];
    }

    private static string ResolveCategoryForNewEvent(string selectedCategoryId, string eventId)
    {
        if (!string.IsNullOrWhiteSpace(selectedCategoryId))
        {
            return selectedCategoryId;
        }

        return EventDatabase.ResolveDefaultCategoryId(eventId);
    }

    private bool GetCategoryFoldout(string categoryId)
    {
        if (categoryFoldouts.TryGetValue(categoryId ?? string.Empty, out bool value))
        {
            return value;
        }

        return true;
    }

    private static List<int> CollectEntriesForCategory(SerializedProperty entriesProperty, string categoryId)
    {
        List<int> indices = new List<int>();
        if (entriesProperty == null)
        {
            return indices;
        }

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            string entryCategoryId = entryProperty.FindPropertyRelative("categoryId")?.stringValue ?? string.Empty;
            if (string.Equals(entryCategoryId, categoryId, StringComparison.Ordinal))
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private static List<int> CollectUncategorizedEntries(SerializedProperty entriesProperty, HashSet<int> drawnIndices)
    {
        List<int> indices = new List<int>();
        if (entriesProperty == null)
        {
            return indices;
        }

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            if (!drawnIndices.Contains(i))
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private static int CountEntriesInCategory(SerializedProperty entriesProperty, string categoryId)
    {
        return CollectEntriesForCategory(entriesProperty, categoryId).Count;
    }

    private static bool CategoryIdExists(SerializedProperty categoriesProperty, string categoryId, int ignoredIndex)
    {
        if (categoriesProperty == null || string.IsNullOrWhiteSpace(categoryId))
        {
            return false;
        }

        for (int i = 0; i < categoriesProperty.arraySize; i++)
        {
            if (i == ignoredIndex)
            {
                continue;
            }

            SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex(i);
            string id = categoryProperty.FindPropertyRelative("categoryId")?.stringValue ?? string.Empty;
            if (string.Equals(id, categoryId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void RenameEntryCategory(SerializedProperty entriesProperty, string oldCategoryId, string newCategoryId)
    {
        if (entriesProperty == null || string.IsNullOrWhiteSpace(oldCategoryId) || string.IsNullOrWhiteSpace(newCategoryId))
        {
            return;
        }

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            SerializedProperty categoryProperty = entryProperty.FindPropertyRelative("categoryId");
            if (categoryProperty != null && string.Equals(categoryProperty.stringValue, oldCategoryId, StringComparison.Ordinal))
            {
                categoryProperty.stringValue = newCategoryId;
            }
        }
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
