using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ItemDatabaseWindow : EditorWindow
{
    private const string DatabaseAssetPath = "Assets/Resources/ItemDatabase.asset";

    private ItemDatabase database;
    private BattleSkillDatabase skillDatabase;
    private ItemDatabase.ItemCategory createCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.ItemQuality createQuality = ItemDatabase.ItemQuality.Common;
    private ItemDatabase.EquipmentSlotType createEquipmentSlot = ItemDatabase.EquipmentSlotType.MainHand;
    private ItemDatabase.WeaponCategory createWeaponCategory = ItemDatabase.WeaponCategory.OneHanded;
    private float createFixedDamage;
    private readonly List<string> createGrantedSkillIds = new List<string> { string.Empty };
    private readonly List<ItemDatabase.WeaponAttributeMultiplierEntry> createWeaponAttributeMultipliers =
        new List<ItemDatabase.WeaponAttributeMultiplierEntry> { new ItemDatabase.WeaponAttributeMultiplierEntry() };
    private string newItemId = "itm_eq_mainhand_001";
    private GameObject newItemPrefab;

    private ItemDatabase.ItemCategory filterCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType filterEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
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
        skillDatabase = BattleSkillDatabase.LoadDefault();
        EnsureCreateLists();
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        skillDatabase = skillDatabase != null ? skillDatabase : BattleSkillDatabase.LoadDefault();
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

        createCategory = (ItemDatabase.ItemCategory)EditorGUILayout.Popup(
            "类别",
            (int)createCategory,
            ItemEditorLabels.CategoryLabels);
        createQuality = (ItemDatabase.ItemQuality)EditorGUILayout.Popup(
            "品质",
            (int)createQuality,
            ItemEditorLabels.QualityLabels);

        if (createCategory == ItemDatabase.ItemCategory.Equipment)
        {
            createEquipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup(
                "装备部位",
                (int)createEquipmentSlot,
                ItemEditorLabels.EquipmentSlotLabels);

            DrawWeaponCategoryPopup("武器分类", createEquipmentSlot, ref createWeaponCategory);
            DrawWeaponFields(
                createCategory,
                createWeaponCategory,
                ref createFixedDamage,
                createGrantedSkillIds,
                createWeaponAttributeMultipliers,
                skillDatabase);
        }
        else
        {
            createEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
            createWeaponCategory = ItemDatabase.WeaponCategory.None;
            createFixedDamage = 0f;
            ResetGrantedSkillList(createGrantedSkillIds);
            ResetWeaponAttributeList(createWeaponAttributeMultipliers);
        }

        newItemId = EditorGUILayout.TextField("物品ID", newItemId);
        if (!string.IsNullOrWhiteSpace(newItemId) &&
            !newItemId.StartsWith("itm_", System.StringComparison.Ordinal))
        {
            EditorGUILayout.HelpBox("物品ID必须以 itm_ 开头。", MessageType.Warning);
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

        filterCategory = (ItemDatabase.ItemCategory)EditorGUILayout.Popup(
            "筛选类别",
            (int)filterCategory,
            ItemEditorLabels.CategoryLabels);

        if (filterCategory == ItemDatabase.ItemCategory.Equipment)
        {
            filterEquipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup(
                "筛选部位",
                (int)filterEquipmentSlot,
                ItemEditorLabels.EquipmentSlotLabels);

            DrawWeaponCategoryPopup("武器分类", filterEquipmentSlot, ref filterWeaponCategory);
        }
        else
        {
            filterEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
            filterWeaponCategory = ItemDatabase.WeaponCategory.None;
        }

        bool hasAnyMatch = false;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = database.Entries[i];
            if (!MatchesFilter(entry))
            {
                continue;
            }

            hasAnyMatch = true;
            DrawEntryEditor(entry, i);
        }

        EditorGUILayout.EndScrollView();

        if (!hasAnyMatch)
        {
            EditorGUILayout.HelpBox("当前筛选下没有物品定义。", MessageType.Info);
        }
    }

    private void DrawEntryEditor(ItemDatabase.ItemEntry entry, int index)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            string originalId = entry.itemId;
            ItemDatabase.ItemCategory originalCategory = entry.category;
            ItemDatabase.ItemQuality originalQuality = entry.quality;
            ItemDatabase.EquipmentSlotType originalEquipmentSlot = entry.equipmentSlot;
            ItemDatabase.WeaponCategory originalWeaponCategory = entry.weaponCategory;
            float originalFixedDamage = entry.fixedDamage;
            List<string> originalGrantedSkillIds = CloneGrantedSkillList(entry.grantedSkillIds);
            List<ItemDatabase.WeaponAttributeMultiplierEntry> originalWeaponAttributeMultipliers = CloneWeaponAttributeList(entry.weaponAttributeMultipliers);
            GameObject originalPrefab = entry.prefab;

            EditorGUILayout.LabelField($"条目 {index + 1}", EditorStyles.boldLabel);
            entry.itemId = EditorGUILayout.TextField("物品ID", entry.itemId);
            entry.category = (ItemDatabase.ItemCategory)EditorGUILayout.Popup(
                "类别",
                (int)entry.category,
                ItemEditorLabels.CategoryLabels);
            entry.quality = (ItemDatabase.ItemQuality)EditorGUILayout.Popup(
                "品质",
                (int)entry.quality,
                ItemEditorLabels.QualityLabels);

            if (entry.category == ItemDatabase.ItemCategory.Equipment)
            {
                entry.equipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup(
                    "装备部位",
                    (int)entry.equipmentSlot,
                    ItemEditorLabels.EquipmentSlotLabels);

                DrawWeaponCategoryPopup("武器分类", entry.equipmentSlot, ref entry.weaponCategory);
                DrawWeaponFields(
                    entry.category,
                    entry.weaponCategory,
                    ref entry.fixedDamage,
                    entry.grantedSkillIds,
                    entry.weaponAttributeMultipliers,
                    skillDatabase);
            }
            else
            {
                entry.equipmentSlot = ItemDatabase.EquipmentSlotType.None;
                entry.weaponCategory = ItemDatabase.WeaponCategory.None;
                entry.fixedDamage = 0f;
                ResetGrantedSkillList(entry.grantedSkillIds);
                ResetWeaponAttributeList(entry.weaponAttributeMultipliers);
            }

            entry.weaponCategory = ItemDatabase.NormalizeWeaponCategory(entry.equipmentSlot, entry.weaponCategory);
            entry.prefab = (GameObject)EditorGUILayout.ObjectField("预制体", entry.prefab, typeof(GameObject), false);

            string validationMessage = ValidateEntry(entry, index);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存修改") && string.IsNullOrEmpty(validationMessage))
                {
                    SaveDatabase();
                }

                if (GUILayout.Button("还原"))
                {
                    entry.itemId = originalId;
                    entry.category = originalCategory;
                    entry.quality = originalQuality;
                    entry.equipmentSlot = originalEquipmentSlot;
                    entry.weaponCategory = originalWeaponCategory;
                    entry.fixedDamage = originalFixedDamage;
                    entry.grantedSkillIds = CloneGrantedSkillList(originalGrantedSkillIds);
                    entry.weaponAttributeMultipliers = CloneWeaponAttributeList(originalWeaponAttributeMultipliers);
                    entry.prefab = originalPrefab;
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("删除物品定义"))
                {
                    database.Entries.RemoveAt(index);
                    SaveDatabase();
                    GUIUtility.ExitGUI();
                }
            }
        }
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

        return !ItemDatabase.ShouldFilterWeaponCategory(filterEquipmentSlot) ||
            filterWeaponCategory == ItemDatabase.WeaponCategory.None ||
            entry.weaponCategory == filterWeaponCategory;
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
            quality = createQuality,
            equipmentSlot = createCategory == ItemDatabase.ItemCategory.Equipment
                ? createEquipmentSlot
                : ItemDatabase.EquipmentSlotType.None,
            weaponCategory = ItemDatabase.NormalizeWeaponCategory(createEquipmentSlot, createWeaponCategory),
            fixedDamage = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? createFixedDamage
                : 0f,
            grantedSkillIds = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? CloneGrantedSkillList(createGrantedSkillIds)
                : new List<string>(),
            weaponAttributeMultipliers = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? CloneWeaponAttributeList(createWeaponAttributeMultipliers)
                : new List<ItemDatabase.WeaponAttributeMultiplierEntry>(),
            prefab = newItemPrefab
        });

        SaveDatabase();
        Selection.activeObject = database;
    }

    private string ValidateEntry(ItemDatabase.ItemEntry entry, int selfIndex)
    {
        if (entry == null)
        {
            return "条目为空。";
        }

        if (string.IsNullOrWhiteSpace(entry.itemId))
        {
            return "物品ID不能为空。";
        }

        if (!entry.itemId.StartsWith("itm_", System.StringComparison.Ordinal))
        {
            return "物品ID必须以 itm_ 开头。";
        }

        if (entry.prefab == null)
        {
            return "预制体不能为空。";
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            if (i == selfIndex)
            {
                continue;
            }

            ItemDatabase.ItemEntry other = database.Entries[i];
            if (other != null && string.Equals(other.itemId, entry.itemId, System.StringComparison.Ordinal))
            {
                return "物品ID重复。";
            }
        }

        return string.Empty;
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

        string[] labels = ItemEditorLabels.GetWeaponCategoryLabels(equipmentSlot);
        int popupIndex = ItemEditorLabels.ToWeaponCategoryPopupIndex(equipmentSlot, weaponCategory);
        popupIndex = EditorGUILayout.Popup(label, popupIndex, labels);
        weaponCategory = ItemEditorLabels.FromWeaponCategoryPopupIndex(equipmentSlot, popupIndex);
    }

    private static void DrawWeaponFields(
        ItemDatabase.ItemCategory category,
        ItemDatabase.WeaponCategory weaponCategory,
        ref float fixedDamage,
        List<string> grantedSkillIds,
        List<ItemDatabase.WeaponAttributeMultiplierEntry> multipliers,
        BattleSkillDatabase skillDatabase)
    {
        if (!ItemDatabase.ShouldShowWeaponAttributeMultiplier(category, weaponCategory))
        {
            fixedDamage = 0f;
            ResetGrantedSkillList(grantedSkillIds);
            ResetWeaponAttributeList(multipliers);
            return;
        }

        fixedDamage = EditorGUILayout.FloatField("固定伤害", fixedDamage);
        DrawGrantedSkillFields(grantedSkillIds, skillDatabase);
        DrawWeaponAttributeFields(multipliers);
    }

    private static void DrawGrantedSkillFields(List<string> skillIds, BattleSkillDatabase skillDatabase)
    {
        EnsureGrantedSkillList(skillIds);
        EditorGUILayout.LabelField("装备附带技能");

        List<BattleSkillDatabase.SkillEntry> entries = skillDatabase != null ? skillDatabase.Entries : new List<BattleSkillDatabase.SkillEntry>();
        string[] options = BuildSkillOptions(entries);

        for (int i = 0; i < skillIds.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int selectedIndex = FindSkillOptionIndex(skillIds[i], entries);
                selectedIndex = EditorGUILayout.Popup(selectedIndex, options);
                skillIds[i] = selectedIndex <= 0 ? string.Empty : entries[selectedIndex - 1].skillId;

                using (new EditorGUI.DisabledScope(skillIds.Count <= 1))
                {
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        skillIds.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        if (GUILayout.Button("增加附带技能"))
        {
            skillIds.Add(string.Empty);
        }
    }

    private static string[] BuildSkillOptions(List<BattleSkillDatabase.SkillEntry> entries)
    {
        List<string> options = new List<string> { "无" };
        for (int i = 0; i < entries.Count; i++)
        {
            BattleSkillDatabase.SkillEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.skillId))
            {
                continue;
            }

            options.Add(entry.skillId);
        }

        return options.ToArray();
    }

    private static int FindSkillOptionIndex(string skillId, List<BattleSkillDatabase.SkillEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return 0;
        }

        int optionIndex = 1;
        for (int i = 0; i < entries.Count; i++)
        {
            BattleSkillDatabase.SkillEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.skillId))
            {
                continue;
            }

            if (string.Equals(entry.skillId, skillId, System.StringComparison.Ordinal))
            {
                return optionIndex;
            }

            optionIndex++;
        }

        return 0;
    }

    private static void DrawWeaponAttributeFields(List<ItemDatabase.WeaponAttributeMultiplierEntry> multipliers)
    {
        ItemDatabase.EnsureValidWeaponAttributeList(new ItemDatabase.ItemEntry
        {
            weaponAttributeMultipliers = multipliers
        });

        EditorGUILayout.LabelField("属性倍率");
        for (int i = 0; i < multipliers.Count; i++)
        {
            ItemDatabase.WeaponAttributeMultiplierEntry entry = multipliers[i];
            if (entry == null)
            {
                entry = new ItemDatabase.WeaponAttributeMultiplierEntry();
                multipliers[i] = entry;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                entry.attributeType = (ItemDatabase.WeaponAttributeType)EditorGUILayout.Popup(
                    (int)entry.attributeType,
                    ItemEditorLabels.WeaponAttributeTypeLabels,
                    GUILayout.MaxWidth(120f));
                EditorGUILayout.LabelField("=", GUILayout.Width(12f));
                entry.multiplier = EditorGUILayout.FloatField(entry.multiplier);

                using (new EditorGUI.DisabledScope(multipliers.Count <= 1))
                {
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        multipliers.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        if (GUILayout.Button("增加属性倍率"))
        {
            multipliers.Add(new ItemDatabase.WeaponAttributeMultiplierEntry());
        }
    }

    private static List<string> CloneGrantedSkillList(List<string> source)
    {
        List<string> clone = new List<string>();
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                clone.Add(source[i] ?? string.Empty);
            }
        }

        if (clone.Count == 0)
        {
            clone.Add(string.Empty);
        }

        return clone;
    }

    private static List<ItemDatabase.WeaponAttributeMultiplierEntry> CloneWeaponAttributeList(
        List<ItemDatabase.WeaponAttributeMultiplierEntry> source)
    {
        List<ItemDatabase.WeaponAttributeMultiplierEntry> clone = new List<ItemDatabase.WeaponAttributeMultiplierEntry>();
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                ItemDatabase.WeaponAttributeMultiplierEntry entry = source[i];
                clone.Add(new ItemDatabase.WeaponAttributeMultiplierEntry
                {
                    attributeType = entry != null ? entry.attributeType : ItemDatabase.WeaponAttributeType.Strength,
                    multiplier = entry != null ? entry.multiplier : 1f
                });
            }
        }

        if (clone.Count == 0)
        {
            clone.Add(new ItemDatabase.WeaponAttributeMultiplierEntry());
        }

        return clone;
    }

    private void EnsureCreateLists()
    {
        EnsureGrantedSkillList(createGrantedSkillIds);
        if (createWeaponAttributeMultipliers.Count == 0)
        {
            createWeaponAttributeMultipliers.Add(new ItemDatabase.WeaponAttributeMultiplierEntry());
        }
    }

    private static void EnsureGrantedSkillList(List<string> skillIds)
    {
        if (skillIds == null)
        {
            return;
        }

        if (skillIds.Count == 0)
        {
            skillIds.Add(string.Empty);
        }
    }

    private static void ResetGrantedSkillList(List<string> skillIds)
    {
        if (skillIds == null)
        {
            return;
        }

        skillIds.Clear();
        skillIds.Add(string.Empty);
    }

    private static void ResetWeaponAttributeList(List<ItemDatabase.WeaponAttributeMultiplierEntry> multipliers)
    {
        if (multipliers == null)
        {
            return;
        }

        multipliers.Clear();
        multipliers.Add(new ItemDatabase.WeaponAttributeMultiplierEntry());
    }

    private void SaveDatabase()
    {
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
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
}
