using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ItemDatabaseWindow : EditorWindow
{
    private const string DatabaseAssetPath = "Assets/Resources/ItemDatabase.asset";
    private static readonly string[] CategoryLabels = { "装备", "消耗品", "材料", "补给" };
    private static readonly string[] EquipmentSlotLabels = { "无", "主手", "副手", "头盔", "护甲", "腿甲", "护手", "鞋子", "饰品" };

    private ItemDatabase database;
    private ItemDatabase.ItemCategory createCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType createEquipmentSlot = ItemDatabase.EquipmentSlotType.MainHand;
    private string newItemId = "itm_eq_mainhand_001";
    private Sprite newItemIcon;
    private GameObject newItemPrefab;
    private ItemDatabase.ItemCategory filterCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType filterEquipmentSlot = ItemDatabase.EquipmentSlotType.MainHand;
    private Vector2 scroll;

    [MenuItem("Tools/物品/物品数据库")]
    private static void Open()
    {
        ItemDatabaseWindow window = GetWindow<ItemDatabaseWindow>();
        window.titleContent = new GUIContent("物品数据库");
        window.minSize = new Vector2(560f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        database = LoadOrCreateDatabase();
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("物品数据库加载失败。", MessageType.Error);
            return;
        }

        DrawCreatePanel();
        EditorGUILayout.Space(10f);
        DrawListPanel();
    }

    private void DrawCreatePanel()
    {
        EditorGUILayout.LabelField("新增物品", EditorStyles.boldLabel);
        createCategory = (ItemDatabase.ItemCategory)EditorGUILayout.Popup("类别", (int)createCategory, CategoryLabels);
        if (createCategory == ItemDatabase.ItemCategory.Equipment)
        {
            createEquipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup("装备部位", (int)createEquipmentSlot, EquipmentSlotLabels);
        }
        else
        {
            createEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
        }

        newItemId = EditorGUILayout.TextField("物品ID", newItemId);
        if (!string.IsNullOrWhiteSpace(newItemId) && !newItemId.StartsWith("itm_", System.StringComparison.Ordinal))
        {
            EditorGUILayout.HelpBox("物品 ID 必须以 itm_ 开头，用于和技能、角色等 ID 区分。", MessageType.Warning);
        }

        newItemIcon = (Sprite)EditorGUILayout.ObjectField("图标", newItemIcon, typeof(Sprite), false);
        newItemPrefab = (GameObject)EditorGUILayout.ObjectField("预制体", newItemPrefab, typeof(GameObject), false);

        using (new EditorGUI.DisabledScope(!CanCreateEntry()))
        {
            if (GUILayout.Button("新增物品定义"))
            {
                AddEntry();
            }
        }
    }

    private void DrawListPanel()
    {
        EditorGUILayout.LabelField("现有物品", EditorStyles.boldLabel);
        filterCategory = (ItemDatabase.ItemCategory)EditorGUILayout.Popup("筛选类别", (int)filterCategory, CategoryLabels);
        if (filterCategory == ItemDatabase.ItemCategory.Equipment)
        {
            filterEquipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup("筛选部位", (int)filterEquipmentSlot, EquipmentSlotLabels);
        }
        else
        {
            filterEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
        }

        List<ItemDatabase.ItemEntry> entries = database.FindEntries(filterCategory, filterEquipmentSlot);
        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("当前筛选下没有物品定义。", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = database.Entries.Count - 1; i >= 0; i--)
        {
            ItemDatabase.ItemEntry entry = database.Entries[i];
            if (entry == null || entry.category != filterCategory)
            {
                continue;
            }

            if (filterCategory == ItemDatabase.ItemCategory.Equipment &&
                filterEquipmentSlot != ItemDatabase.EquipmentSlotType.None &&
                entry.equipmentSlot != filterEquipmentSlot)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(entry.itemId);
                EditorGUILayout.LabelField("类别", GetCategoryLabel(entry.category));
                if (entry.category == ItemDatabase.ItemCategory.Equipment)
                {
                    EditorGUILayout.LabelField("部位", GetEquipmentSlotLabel(entry.equipmentSlot));
                }

                EditorGUILayout.ObjectField("图标", entry.icon, typeof(Sprite), false);
                EditorGUILayout.ObjectField("预制体", entry.prefab, typeof(GameObject), false);

                if (GUILayout.Button("删除物品定义"))
                {
                    database.Entries.RemoveAt(i);
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                    GUIUtility.ExitGUI();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private bool CanCreateEntry()
    {
        return database != null &&
            !string.IsNullOrWhiteSpace(newItemId) &&
            newItemId.Trim().StartsWith("itm_", System.StringComparison.Ordinal) &&
            database.FindEntry(newItemId.Trim()) == null;
    }

    private void AddEntry()
    {
        database.Entries.Add(new ItemDatabase.ItemEntry
        {
            itemId = newItemId.Trim(),
            category = createCategory,
            equipmentSlot = createCategory == ItemDatabase.ItemCategory.Equipment
                ? createEquipmentSlot
                : ItemDatabase.EquipmentSlotType.None,
            icon = newItemIcon,
            prefab = newItemPrefab
        });

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Selection.activeObject = database;
    }

    private static ItemDatabase LoadOrCreateDatabase()
    {
        ItemDatabase existing = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabaseAssetPath);
        if (existing != null)
        {
            return existing;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        ItemDatabase created = CreateInstance<ItemDatabase>();
        AssetDatabase.CreateAsset(created, DatabaseAssetPath);
        AssetDatabase.SaveAssets();
        return created;
    }
    private static string GetCategoryLabel(ItemDatabase.ItemCategory category)
    {
        return category >= 0 && (int)category < CategoryLabels.Length
            ? CategoryLabels[(int)category]
            : category.ToString();
    }

    private static string GetEquipmentSlotLabel(ItemDatabase.EquipmentSlotType slotType)
    {
        return slotType >= 0 && (int)slotType < EquipmentSlotLabels.Length
            ? EquipmentSlotLabels[(int)slotType]
            : slotType.ToString();
    }
}
