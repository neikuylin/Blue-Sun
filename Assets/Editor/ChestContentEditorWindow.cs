using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ChestContentEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string DatabaseAssetPath = ResourceFolder + "/ChestContentDatabase.asset";

    private 宝箱内容数据库 database;
    private ItemDatabase itemDatabase;
    private Vector2 groupListScroll;
    private Vector2 itemListScroll;
    private int selectedGroupIndex = -1;
    private string newGroupId = "chest_group_001";

    [MenuItem("Tools/地图/宝箱内容")]
    private static void Open()
    {
        ChestContentEditorWindow window = GetWindow<ChestContentEditorWindow>();
        window.titleContent = new GUIContent("宝箱内容");
        window.minSize = new Vector2(880f, 540f);
        window.Show();
        window.Focus();
    }

    private void OnEnable()
    {
        database = LoadOrCreateDatabase();
        itemDatabase = ItemDatabase.LoadDefault();
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        itemDatabase = itemDatabase != null ? itemDatabase : ItemDatabase.LoadDefault();
        if (database == null)
        {
            EditorGUILayout.HelpBox("宝箱内容库加载失败。", MessageType.Error);
            return;
        }

        if (database.Groups == null)
        {
            EditorGUILayout.HelpBox("宝箱内容组列表为空引用。", MessageType.Error);
            return;
        }

        EnsureSelectedGroupIndex();
        DrawToolbar();
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawGroupList();
            DrawSelectedGroup();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                SaveDatabase();
            }

            GUILayout.Space(8f);
            GUILayout.Label("新内容组ID", GUILayout.Width(78f));
            newGroupId = GUILayout.TextField(newGroupId, EditorStyles.toolbarTextField, GUILayout.Width(180f));

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newGroupId)))
            {
                if (GUILayout.Button("新增内容组", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                {
                    AddGroup();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("格子编辑器里的宝箱物件会选择这里保存的ID", EditorStyles.miniLabel);
        }
    }

    private void DrawGroupList()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(260f), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("内容组", EditorStyles.boldLabel);
            if (database.Groups.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有内容组。", MessageType.Info);
            }

            groupListScroll = EditorGUILayout.BeginScrollView(groupListScroll);
            for (int i = 0; i < database.Groups.Count; i++)
            {
                宝箱内容数据库.宝箱内容组 group = database.Groups[i];
                string groupId = group != null && !string.IsNullOrWhiteSpace(group.内容组ID)
                    ? group.内容组ID.Trim()
                    : $"未命名内容组 {i + 1}";
                string typeName = group != null && group.生成类型 == 宝箱内容数据库.宝箱物品生成类型.随机物品
                    ? "随机"
                    : "指定";
                string label = $"{groupId}  [{typeName}]";

                bool selected = selectedGroupIndex == i;
                if (GUILayout.Button(label, selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(30f)))
                {
                    selectedGroupIndex = i;
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawSelectedGroup()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            if (selectedGroupIndex < 0 || selectedGroupIndex >= database.Groups.Count)
            {
                EditorGUILayout.HelpBox("请选择或新增一个内容组。", MessageType.Info);
                return;
            }

            宝箱内容数据库.宝箱内容组 group = database.Groups[selectedGroupIndex];
            if (group == null)
            {
                group = new 宝箱内容数据库.宝箱内容组();
                database.Groups[selectedGroupIndex] = group;
            }

            EditorGUILayout.LabelField("内容组配置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            group.内容组ID = EditorGUILayout.TextField("内容组ID", group.内容组ID);
            group.生成类型 = (宝箱内容数据库.宝箱物品生成类型)EditorGUILayout.Popup(
                "生成类型",
                (int)group.生成类型,
                new[] { "指定物品", "随机物品" });

            DrawGroupValidation(group, selectedGroupIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新增物品", GUILayout.Width(90f)))
                {
                    group.物品列表 ??= new List<宝箱内容数据库.宝箱物品条目>();
                    group.物品列表.Add(new 宝箱内容数据库.宝箱物品条目());
                    MarkDirty();
                }

                using (new EditorGUI.DisabledScope(selectedGroupIndex <= 0))
                {
                    if (GUILayout.Button("上移内容组", GUILayout.Width(90f)))
                    {
                        SwapGroups(selectedGroupIndex, selectedGroupIndex - 1);
                        selectedGroupIndex--;
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(selectedGroupIndex >= database.Groups.Count - 1))
                {
                    if (GUILayout.Button("下移内容组", GUILayout.Width(90f)))
                    {
                        SwapGroups(selectedGroupIndex, selectedGroupIndex + 1);
                        selectedGroupIndex++;
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("删除内容组", GUILayout.Width(90f)))
                {
                    database.Groups.RemoveAt(selectedGroupIndex);
                    EnsureSelectedGroupIndex();
                    MarkDirty();
                    GUIUtility.ExitGUI();
                }
            }

            if (group.生成类型 == 宝箱内容数据库.宝箱物品生成类型.指定物品)
            {
                EditorGUILayout.HelpBox("指定物品：按下方列表从上到下全部生成。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("随机物品：从下方有效物品里随机抽一个生成。", MessageType.Info);
            }

            DrawItemList(group);

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }
        }
    }

    private void DrawItemList(宝箱内容数据库.宝箱内容组 group)
    {
        if (group.物品列表 == null)
        {
            group.物品列表 = new List<宝箱内容数据库.宝箱物品条目>();
        }

        itemListScroll = EditorGUILayout.BeginScrollView(itemListScroll, GUILayout.ExpandHeight(true));
        for (int i = 0; i < group.物品列表.Count; i++)
        {
            宝箱内容数据库.宝箱物品条目 item = group.物品列表[i];
            if (item == null)
            {
                item = new 宝箱内容数据库.宝箱物品条目();
                group.物品列表[i] = item;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"物品 {i + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(i <= 0))
                    {
                        if (GUILayout.Button("上移", GUILayout.Width(48f)))
                        {
                            SwapItems(group, i, i - 1);
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(i >= group.物品列表.Count - 1))
                    {
                        if (GUILayout.Button("下移", GUILayout.Width(48f)))
                        {
                            SwapItems(group, i, i + 1);
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(48f)))
                    {
                        group.物品列表.RemoveAt(i);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                }

                item.分类筛选 = (ItemDatabase.ItemCategory)EditorGUILayout.Popup(
                    "分类",
                    (int)item.分类筛选,
                    ItemEditorLabels.CategoryLabels);

                List<ItemDatabase.ItemEntry> options = GetFilteredItems(item.分类筛选);
                int selectedIndex = FindOptionIndex(item.物品ID, options);
                selectedIndex = EditorGUILayout.Popup("物品ID", selectedIndex, BuildItemOptionLabels(options));
                item.物品ID = selectedIndex <= 0 ? string.Empty : options[selectedIndex - 1].itemId;
                item.数量 = Mathf.Max(1, EditorGUILayout.IntField("数量", Mathf.Max(1, item.数量)));

                if (itemDatabase != null && itemDatabase.FindEntry(item.物品ID) == null)
                {
                    EditorGUILayout.HelpBox("物品为空或不存在。运行时遇到这个物品会中断当前宝箱生成。", MessageType.Warning);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGroupValidation(宝箱内容数据库.宝箱内容组 group, int selfIndex)
    {
        if (string.IsNullOrWhiteSpace(group.内容组ID))
        {
            EditorGUILayout.HelpBox("内容组ID不能为空。", MessageType.Warning);
        }

        for (int i = 0; i < database.Groups.Count; i++)
        {
            if (i == selfIndex)
            {
                continue;
            }

            宝箱内容数据库.宝箱内容组 other = database.Groups[i];
            string otherId = other != null && !string.IsNullOrWhiteSpace(other.内容组ID) ? other.内容组ID.Trim() : string.Empty;
            string currentId = !string.IsNullOrWhiteSpace(group.内容组ID) ? group.内容组ID.Trim() : string.Empty;
            if (string.Equals(otherId, currentId, System.StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox("内容组ID重复。格子编辑器选择时会无法区分。", MessageType.Warning);
                return;
            }
        }
    }

    private void AddGroup()
    {
        string groupId = newGroupId.Trim();
        database.Groups.Add(new 宝箱内容数据库.宝箱内容组
        {
            内容组ID = groupId,
            生成类型 = 宝箱内容数据库.宝箱物品生成类型.指定物品,
            物品列表 = new List<宝箱内容数据库.宝箱物品条目>()
        });

        selectedGroupIndex = database.Groups.Count - 1;
        newGroupId = BuildNextGroupId(groupId);
        MarkDirty();
    }

    private void SwapGroups(int from, int to)
    {
        宝箱内容数据库.宝箱内容组 group = database.Groups[from];
        database.Groups[from] = database.Groups[to];
        database.Groups[to] = group;
        MarkDirty();
    }

    private void SwapItems(宝箱内容数据库.宝箱内容组 group, int from, int to)
    {
        宝箱内容数据库.宝箱物品条目 item = group.物品列表[from];
        group.物品列表[from] = group.物品列表[to];
        group.物品列表[to] = item;
        MarkDirty();
    }

    private List<ItemDatabase.ItemEntry> GetFilteredItems(ItemDatabase.ItemCategory category)
    {
        List<ItemDatabase.ItemEntry> result = new List<ItemDatabase.ItemEntry>();
        if (itemDatabase == null || itemDatabase.Entries == null)
        {
            return result;
        }

        for (int i = 0; i < itemDatabase.Entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = itemDatabase.Entries[i];
            if (entry != null && entry.category == category && !string.IsNullOrWhiteSpace(entry.itemId))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static string[] BuildItemOptionLabels(List<ItemDatabase.ItemEntry> entries)
    {
        List<string> labels = new List<string> { "无" };
        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = entries[i];
            string displayName = string.IsNullOrWhiteSpace(entry.displayName) ? "未命名" : entry.displayName;
            labels.Add($"{entry.itemId} - {displayName}");
        }

        return labels.ToArray();
    }

    private static int FindOptionIndex(string itemId, List<ItemDatabase.ItemEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].itemId, itemId, System.StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private void EnsureSelectedGroupIndex()
    {
        if (database == null || database.Groups == null || database.Groups.Count == 0)
        {
            selectedGroupIndex = -1;
            return;
        }

        selectedGroupIndex = Mathf.Clamp(selectedGroupIndex, 0, database.Groups.Count - 1);
    }

    private static string BuildNextGroupId(string current)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return "chest_group_001";
        }

        return current.Trim() + "_next";
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(database);
    }

    private void SaveDatabase()
    {
        MarkDirty();
        AssetDatabase.SaveAssets();
    }

    private static 宝箱内容数据库 LoadOrCreateDatabase()
    {
        宝箱内容数据库 existing = AssetDatabase.LoadAssetAtPath<宝箱内容数据库>(DatabaseAssetPath);
        if (existing != null)
        {
            return existing;
        }

        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        宝箱内容数据库 created = CreateInstance<宝箱内容数据库>();
        AssetDatabase.CreateAsset(created, DatabaseAssetPath);
        AssetDatabase.SaveAssets();
        return created;
    }
}
