using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ItemDatabaseWindow : EditorWindow
{
    private const string DatabaseAssetPath = "Assets/Resources/ItemDatabase.asset";

    private static readonly string[] CategoryLabels = { "装备", "消耗品", "材料", "补给" };
    private static readonly string[] EquipmentSlotLabels = { "无", "主手", "副手", "主副手", "头盔", "胸甲", "腿甲", "手套", "鞋子", "饰品" };
    private static readonly string[] WeaponCategoryLabels = { "无", "单手武器", "双手武器" };
    private static readonly string[] MainOrOffHandWeaponCategoryLabels = { "无", "单手武器" };

    private ItemDatabase database;
    private ItemDatabase.ItemCategory createCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType createEquipmentSlot = ItemDatabase.EquipmentSlotType.MainHand;
    private ItemDatabase.WeaponCategory createWeaponCategory = ItemDatabase.WeaponCategory.OneHanded;
    private string newItemId = "itm_eq_mainhand_001";
    private GameObject newItemPrefab;
    private ItemDatabase.ItemCategory filterCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType filterEquipmentSlot = ItemDatabase.EquipmentSlotType.MainHand;
    private ItemDatabase.WeaponCategory filterWeaponCategory = ItemDatabase.WeaponCategory.None;
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
            DrawWeaponCategoryPopup("武器分类", createEquipmentSlot, ref createWeaponCategory);
        }
        else
        {
            createEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
            createWeaponCategory = ItemDatabase.WeaponCategory.None;
        }

        newItemId = EditorGUILayout.TextField("物品ID", newItemId);
        if (!string.IsNullOrWhiteSpace(newItemId) && !newItemId.StartsWith("itm_", System.StringComparison.Ordinal))
        {
            EditorGUILayout.HelpBox("物品 ID 必须以 itm_ 开头，用于和技能、角色等 ID 区分。", MessageType.Warning);
        }

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
            DrawWeaponCategoryPopup("武器分类", filterEquipmentSlot, ref filterWeaponCategory);
        }
        else
        {
            filterEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
            filterWeaponCategory = ItemDatabase.WeaponCategory.None;
        }

        List<ItemDatabase.ItemEntry> entries = database.FindEntries(filterCategory, filterEquipmentSlot, filterWeaponCategory);
        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("当前筛选下没有物品定义。", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = database.Entries.Count - 1; i >= 0; i--)
        {
            ItemDatabase.ItemEntry entry = database.Entries[i];
            if (!MatchesFilter(entry))
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
                    if (ItemDatabase.ShouldFilterWeaponCategory(entry.equipmentSlot))
                    {
                        EditorGUILayout.LabelField("武器分类", GetWeaponCategoryLabel(entry.weaponCategory));
                    }
                }

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

    private bool MatchesFilter(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.category != filterCategory)
        {
            return false;
        }

        if (filterCategory != ItemDatabase.ItemCategory.Equipment)
        {
            return true;
        }

        if (filterEquipmentSlot != ItemDatabase.EquipmentSlotType.None &&
            entry.equipmentSlot != filterEquipmentSlot)
        {
            return false;
        }

        if (ItemDatabase.ShouldFilterWeaponCategory(filterEquipmentSlot) &&
            filterWeaponCategory != ItemDatabase.WeaponCategory.None &&
            entry.weaponCategory != filterWeaponCategory)
        {
            return false;
        }

        return true;
    }

    private bool CanCreateEntry()
    {
        return database != null &&
            !string.IsNullOrWhiteSpace(newItemId) &&
            newItemId.Trim().StartsWith("itm_", System.StringComparison.Ordinal) &&
            newItemPrefab != null &&
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
            weaponCategory = ResolveStoredWeaponCategory(createEquipmentSlot, createWeaponCategory),
            prefab = newItemPrefab
        });

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Selection.activeObject = database;
    }

    private static void DrawWeaponCategoryPopup(
        string label,
        ItemDatabase.EquipmentSlotType equipmentSlot,
        ref ItemDatabase.WeaponCategory weaponCategory)
    {
        if (!ItemDatabase.ShouldFilterWeaponCategory(equipmentSlot))
        {
            weaponCategory = ItemDatabase.WeaponCategory.None;
            return;
        }

        if (equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            int popupIndex = weaponCategory == ItemDatabase.WeaponCategory.OneHanded ? 1 : 0;
            popupIndex = EditorGUILayout.Popup(label, popupIndex, MainOrOffHandWeaponCategoryLabels);
            weaponCategory = popupIndex == 1
                ? ItemDatabase.WeaponCategory.OneHanded
                : ItemDatabase.WeaponCategory.None;
            return;
        }

        weaponCategory = (ItemDatabase.WeaponCategory)EditorGUILayout.Popup(label, (int)weaponCategory, WeaponCategoryLabels);
    }

    private static ItemDatabase.WeaponCategory ResolveStoredWeaponCategory(
        ItemDatabase.EquipmentSlotType equipmentSlot,
        ItemDatabase.WeaponCategory weaponCategory)
    {
        if (!ItemDatabase.ShouldFilterWeaponCategory(equipmentSlot))
        {
            return ItemDatabase.WeaponCategory.None;
        }

        if (equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand &&
            weaponCategory == ItemDatabase.WeaponCategory.TwoHanded)
        {
            return ItemDatabase.WeaponCategory.OneHanded;
        }

        return weaponCategory;
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

    private static string GetWeaponCategoryLabel(ItemDatabase.WeaponCategory weaponCategory)
    {
        return weaponCategory >= 0 && (int)weaponCategory < WeaponCategoryLabels.Length
            ? WeaponCategoryLabels[(int)weaponCategory]
            : weaponCategory.ToString();
    }
}
