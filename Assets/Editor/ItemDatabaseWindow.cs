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
    private readonly ItemDatabase.WeaponDamageDistribution createWeaponDamageDistribution =
        ItemDatabase.CreateDefaultWeaponDamageDistribution();
    private float createFixedDamage;
    private int createCriticalChanceBonus;
    private int createCriticalDamageBonus;
    private float createStaffDamageMultiplier = 1f;
    private int createManaRecovery;
    private string newDescription = string.Empty;
    private readonly List<string> createGrantedSkillIds = new List<string> { string.Empty };
    private readonly List<ItemDatabase.WeaponAttributeMultiplierEntry> createWeaponAttributeMultipliers =
        new List<ItemDatabase.WeaponAttributeMultiplierEntry> { new ItemDatabase.WeaponAttributeMultiplierEntry() };
    private readonly List<ItemDatabase.WeaponResistancePenetrationEntry> createWeaponResistancePenetrations =
        new List<ItemDatabase.WeaponResistancePenetrationEntry> { new ItemDatabase.WeaponResistancePenetrationEntry() };
    private string newItemId = "itm_eq_mainhand_001";
    private string newDisplayName = "新物品";
    private GameObject newItemPrefab;
    private GameObject newWeaponModelPrefab;

    private ItemDatabase.ItemCategory filterCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType filterEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
    private ItemDatabase.WeaponCategory filterWeaponCategory = ItemDatabase.WeaponCategory.None;
    private Vector2 scroll;
    private readonly Dictionary<string, bool> entryFoldoutStates = new Dictionary<string, bool>();

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
                createWeaponDamageDistribution,
                ref createFixedDamage,
                ref createCriticalChanceBonus,
                ref createCriticalDamageBonus,
                ref createStaffDamageMultiplier,
                ref createManaRecovery,
                createGrantedSkillIds,
                createWeaponAttributeMultipliers,
                createWeaponResistancePenetrations,
                skillDatabase);
        }
        else
        {
            createEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
            createWeaponCategory = ItemDatabase.WeaponCategory.None;
            ResetWeaponDamageDistribution(createWeaponDamageDistribution);
            createFixedDamage = 0f;
            createCriticalChanceBonus = 0;
            createCriticalDamageBonus = 0;
            createStaffDamageMultiplier = 1f;
            createManaRecovery = 0;
            ResetGrantedSkillList(createGrantedSkillIds);
            ResetWeaponAttributeList(createWeaponAttributeMultipliers);
            ResetWeaponResistancePenetrationList(createWeaponResistancePenetrations);
        }

        newItemId = EditorGUILayout.TextField("物品ID", newItemId);
        newDisplayName = EditorGUILayout.TextField("物品名字", newDisplayName);
        newDescription = EditorGUILayout.TextField("文本介绍", newDescription);
        if (!string.IsNullOrWhiteSpace(newItemId) &&
            !newItemId.StartsWith("itm_", System.StringComparison.Ordinal))
        {
            EditorGUILayout.HelpBox("物品ID必须以 itm_ 开头。", MessageType.Warning);
        }

        newItemPrefab = (GameObject)EditorGUILayout.ObjectField("预制体", newItemPrefab, typeof(GameObject), false);

        if (createCategory == ItemDatabase.ItemCategory.Equipment &&
            ItemDatabase.SupportsWeaponModelPrefab(createEquipmentSlot))
        {
            newWeaponModelPrefab = (GameObject)EditorGUILayout.ObjectField("模型预制体", newWeaponModelPrefab, typeof(GameObject), false);
        }
        else
        {
            newWeaponModelPrefab = null;
        }

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
        string foldoutKey = GetEntryFoldoutKey(entry != null ? entry.itemId : string.Empty, index);
        bool isExpanded = GetFoldoutState(foldoutKey);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string headerLabel = BuildItemHeaderLabel(entry != null ? entry.itemId : string.Empty, index);
                bool nextExpanded = EditorGUILayout.Foldout(isExpanded, headerLabel, true);
                if (nextExpanded != isExpanded)
                {
                    SetFoldoutState(foldoutKey, nextExpanded);
                    isExpanded = nextExpanded;
                }
            }

            if (!isExpanded)
            {
                return;
            }

            string originalId = entry.itemId;
            string originalDisplayName = entry.displayName;
            ItemDatabase.ItemCategory originalCategory = entry.category;
            ItemDatabase.ItemQuality originalQuality = entry.quality;
            ItemDatabase.EquipmentSlotType originalEquipmentSlot = entry.equipmentSlot;
            ItemDatabase.WeaponCategory originalWeaponCategory = entry.weaponCategory;
            ItemDatabase.WeaponDamageDistribution originalWeaponDamageDistribution = CloneWeaponDamageDistribution(entry.weaponDamageDistribution);
            float originalFixedDamage = entry.fixedDamage;
            int originalCriticalChanceBonus = entry.criticalChanceBonus;
            int originalCriticalDamageBonus = entry.criticalDamageBonus;
            float originalStaffDamageMultiplier = entry.staffDamageMultiplier;
            int originalManaRecovery = entry.manaRecovery;
            string originalDescription = entry.description;
            List<string> originalGrantedSkillIds = CloneGrantedSkillList(entry.grantedSkillIds);
            List<ItemDatabase.WeaponAttributeMultiplierEntry> originalWeaponAttributeMultipliers = CloneWeaponAttributeList(entry.weaponAttributeMultipliers);
            List<ItemDatabase.WeaponResistancePenetrationEntry> originalWeaponResistancePenetrations = CloneWeaponResistancePenetrationList(entry.weaponResistancePenetrations);
            GameObject originalPrefab = entry.prefab;
            GameObject originalWeaponModelPrefab = entry.weaponModelPrefab;

            EditorGUILayout.LabelField($"条目 {index + 1}", EditorStyles.boldLabel);
            entry.itemId = EditorGUILayout.TextField("物品ID", entry.itemId);
            entry.displayName = EditorGUILayout.TextField("物品名字", entry.displayName);
            entry.description = EditorGUILayout.TextField("文本介绍", entry.description);
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
                ItemDatabase.EnsureValidWeaponDamageDistribution(entry);
                ItemDatabase.EnsureValidWeaponResistancePenetrationList(entry);
                entry.equipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup(
                    "装备部位",
                    (int)entry.equipmentSlot,
                    ItemEditorLabels.EquipmentSlotLabels);

                DrawWeaponCategoryPopup("武器分类", entry.equipmentSlot, ref entry.weaponCategory);
                DrawWeaponFields(
                    entry.category,
                    entry.weaponCategory,
                    entry.weaponDamageDistribution,
                    ref entry.fixedDamage,
                    ref entry.criticalChanceBonus,
                    ref entry.criticalDamageBonus,
                    ref entry.staffDamageMultiplier,
                    ref entry.manaRecovery,
                    entry.grantedSkillIds,
                    entry.weaponAttributeMultipliers,
                    entry.weaponResistancePenetrations,
                    skillDatabase);
            }
            else
            {
                entry.equipmentSlot = ItemDatabase.EquipmentSlotType.None;
                entry.weaponCategory = ItemDatabase.WeaponCategory.None;
                ResetWeaponDamageDistribution(entry.weaponDamageDistribution);
                entry.fixedDamage = 0f;
                entry.criticalChanceBonus = 0;
                entry.criticalDamageBonus = 0;
                entry.staffDamageMultiplier = 1f;
                entry.manaRecovery = 0;
                ResetGrantedSkillList(entry.grantedSkillIds);
                ResetWeaponAttributeList(entry.weaponAttributeMultipliers);
                ResetWeaponResistancePenetrationList(entry.weaponResistancePenetrations);
            }

            entry.weaponCategory = ItemDatabase.NormalizeWeaponCategory(entry.equipmentSlot, entry.weaponCategory);
            entry.prefab = (GameObject)EditorGUILayout.ObjectField("预制体", entry.prefab, typeof(GameObject), false);

            if (entry.category == ItemDatabase.ItemCategory.Equipment &&
                ItemDatabase.SupportsWeaponModelPrefab(entry.equipmentSlot))
            {
                entry.weaponModelPrefab = (GameObject)EditorGUILayout.ObjectField("模型预制体", entry.weaponModelPrefab, typeof(GameObject), false);
            }
            else
            {
                entry.weaponModelPrefab = null;
            }

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
                    entry.displayName = originalDisplayName;
                    entry.description = originalDescription;
                    entry.category = originalCategory;
                    entry.quality = originalQuality;
                    entry.equipmentSlot = originalEquipmentSlot;
                    entry.weaponCategory = originalWeaponCategory;
                    entry.weaponDamageDistribution = CloneWeaponDamageDistribution(originalWeaponDamageDistribution);
                    entry.fixedDamage = originalFixedDamage;
                    entry.criticalChanceBonus = originalCriticalChanceBonus;
                    entry.criticalDamageBonus = originalCriticalDamageBonus;
                    entry.staffDamageMultiplier = originalStaffDamageMultiplier;
                    entry.manaRecovery = originalManaRecovery;
                    entry.grantedSkillIds = CloneGrantedSkillList(originalGrantedSkillIds);
                    entry.weaponAttributeMultipliers = CloneWeaponAttributeList(originalWeaponAttributeMultipliers);
                    entry.weaponResistancePenetrations = CloneWeaponResistancePenetrationList(originalWeaponResistancePenetrations);
                    entry.prefab = originalPrefab;
                    entry.weaponModelPrefab = originalWeaponModelPrefab;
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("删除物品定义"))
                {
                    entryFoldoutStates.Remove(foldoutKey);
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
            !string.IsNullOrWhiteSpace(newDisplayName) &&
            newItemId.Trim().StartsWith("itm_", System.StringComparison.Ordinal) &&
            newItemPrefab != null &&
            database.FindEntry(newItemId.Trim()) == null;
    }

    private void AddEntry()
    {
        database.Entries.Add(new ItemDatabase.ItemEntry
        {
            itemId = newItemId.Trim(),
            displayName = newDisplayName.Trim(),
            description = newDescription.Trim(),
            category = createCategory,
            quality = createQuality,
            equipmentSlot = createCategory == ItemDatabase.ItemCategory.Equipment
                ? createEquipmentSlot
                : ItemDatabase.EquipmentSlotType.None,
            weaponCategory = ItemDatabase.NormalizeWeaponCategory(createEquipmentSlot, createWeaponCategory),
            weaponDamageDistribution = ItemDatabase.ShouldShowWeaponDamageDistribution(createCategory, createWeaponCategory)
                ? CloneWeaponDamageDistribution(createWeaponDamageDistribution)
                : ItemDatabase.CreateDefaultWeaponDamageDistribution(),
            fixedDamage = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? createFixedDamage
                : 0f,
            criticalChanceBonus = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? Mathf.Max(0, createCriticalChanceBonus)
                : 0,
            criticalDamageBonus = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? Mathf.Max(0, createCriticalDamageBonus)
                : 0,
            staffDamageMultiplier = ItemDatabase.ShouldShowStaffFields(createCategory, createWeaponCategory)
                ? Mathf.Max(0f, createStaffDamageMultiplier)
                : 1f,
            manaRecovery = ItemDatabase.ShouldShowStaffFields(createCategory, createWeaponCategory)
                ? Mathf.Max(0, createManaRecovery)
                : 0,
            grantedSkillIds = ItemDatabase.ShouldShowGrantedSkillList(createCategory, createWeaponCategory)
                ? CloneGrantedSkillList(createGrantedSkillIds)
                : new List<string>(),
            weaponAttributeMultipliers = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? CloneWeaponAttributeList(createWeaponAttributeMultipliers)
                : new List<ItemDatabase.WeaponAttributeMultiplierEntry>(),
            weaponResistancePenetrations = ItemDatabase.ShouldShowWeaponAttributeMultiplier(createCategory, createWeaponCategory)
                ? CloneWeaponResistancePenetrationList(createWeaponResistancePenetrations)
                : new List<ItemDatabase.WeaponResistancePenetrationEntry>(),
            prefab = newItemPrefab,
            weaponModelPrefab = createCategory == ItemDatabase.ItemCategory.Equipment &&
                ItemDatabase.SupportsWeaponModelPrefab(createEquipmentSlot)
                ? newWeaponModelPrefab
                : null
        });

        SaveDatabase();
        Selection.activeObject = database;
    }

    private bool GetFoldoutState(string key)
    {
        if (entryFoldoutStates.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        entryFoldoutStates[key] = false;
        return false;
    }

    private void SetFoldoutState(string key, bool expanded)
    {
        entryFoldoutStates[key] = expanded;
    }

    private static string GetEntryFoldoutKey(string itemId, int index)
    {
        return string.IsNullOrWhiteSpace(itemId) ? $"item_{index}" : $"item_{itemId}";
    }

    private static string BuildItemHeaderLabel(string itemId, int index)
    {
        return string.IsNullOrWhiteSpace(itemId) ? $"未命名物品 {index + 1}" : itemId;
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

        if (string.IsNullOrWhiteSpace(entry.displayName))
        {
            return "物品名字不能为空。";
        }

        if (!entry.itemId.StartsWith("itm_", System.StringComparison.Ordinal))
        {
            return "物品ID必须以 itm_ 开头。";
        }

        if (entry.prefab == null)
        {
            return "预制体不能为空。";
        }

        if (ItemDatabase.ShouldShowWeaponDamageDistribution(entry.category, entry.weaponCategory))
        {
            ItemDatabase.EnsureValidWeaponDamageDistribution(entry);
            if (entry.weaponDamageDistribution.Total != 100)
            {
                return "武器伤害属性总和必须等于 100。";
            }
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
        ItemDatabase.WeaponDamageDistribution weaponDamageDistribution,
        ref float fixedDamage,
        ref int criticalChanceBonus,
        ref int criticalDamageBonus,
        ref float staffDamageMultiplier,
        ref int manaRecovery,
        List<string> grantedSkillIds,
        List<ItemDatabase.WeaponAttributeMultiplierEntry> multipliers,
        List<ItemDatabase.WeaponResistancePenetrationEntry> resistancePenetrations,
        BattleSkillDatabase skillDatabase)
    {
        bool showWeaponFields = ItemDatabase.ShouldShowWeaponAttributeMultiplier(category, weaponCategory);
        bool showGrantedSkills = ItemDatabase.ShouldShowGrantedSkillList(category, weaponCategory);
        bool showStaffFields = ItemDatabase.ShouldShowStaffFields(category, weaponCategory);

        if (!showWeaponFields)
        {
            ResetWeaponDamageDistribution(weaponDamageDistribution);
            fixedDamage = 0f;
            criticalChanceBonus = 0;
            criticalDamageBonus = 0;
            ResetWeaponAttributeList(multipliers);
            ResetWeaponResistancePenetrationList(resistancePenetrations);
        }

        if (!showStaffFields)
        {
            staffDamageMultiplier = 1f;
            manaRecovery = 0;
        }

        if (!showGrantedSkills)
        {
            ResetGrantedSkillList(grantedSkillIds);
        }

        if (!showWeaponFields)
        {
            if (showStaffFields)
            {
                staffDamageMultiplier = Mathf.Max(0f, EditorGUILayout.FloatField("伤害倍率", staffDamageMultiplier));
                manaRecovery = Mathf.Max(0, EditorGUILayout.IntField("法力回复", manaRecovery));
            }

            if (showGrantedSkills)
            {
                DrawGrantedSkillFields(grantedSkillIds, skillDatabase);
            }

            return;
        }

        if (ItemDatabase.ShouldShowWeaponDamageDistribution(category, weaponCategory))
        {
            DrawWeaponDamageDistribution(weaponDamageDistribution);
        }

        fixedDamage = EditorGUILayout.FloatField("固定伤害", fixedDamage);
        criticalChanceBonus = Mathf.Max(0, EditorGUILayout.IntField("暴击率加成", criticalChanceBonus));
        criticalDamageBonus = Mathf.Max(0, EditorGUILayout.IntField("暴击伤害加成", criticalDamageBonus));
        DrawGrantedSkillFields(grantedSkillIds, skillDatabase);
        DrawWeaponAttributeFields(multipliers);
        DrawWeaponResistancePenetrationFields(resistancePenetrations);
    }

    private static void DrawWeaponDamageDistribution(ItemDatabase.WeaponDamageDistribution distribution)
    {
        if (distribution == null)
        {
            return;
        }

        distribution.physical = Mathf.Max(0, EditorGUILayout.IntField("物理伤害占比", distribution.physical));
        distribution.fire = Mathf.Max(0, EditorGUILayout.IntField("火焰伤害占比", distribution.fire));
        distribution.corruption = Mathf.Max(0, EditorGUILayout.IntField("腐败伤害占比", distribution.corruption));
        distribution.cold = Mathf.Max(0, EditorGUILayout.IntField("寒冷伤害占比", distribution.cold));

        int total = distribution.Total;
        EditorGUILayout.LabelField("总和", total + "%");
        if (total != 100)
        {
            EditorGUILayout.HelpBox("四项占比总和必须等于 100。", MessageType.Warning);
        }
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

    private static void DrawWeaponResistancePenetrationFields(List<ItemDatabase.WeaponResistancePenetrationEntry> resistancePenetrations)
    {
        ItemDatabase.EnsureValidWeaponResistancePenetrationList(new ItemDatabase.ItemEntry
        {
            weaponResistancePenetrations = resistancePenetrations
        });

        EditorGUILayout.LabelField("抗性穿透词条");
        for (int i = 0; i < resistancePenetrations.Count; i++)
        {
            ItemDatabase.WeaponResistancePenetrationEntry entry = resistancePenetrations[i];
            if (entry == null)
            {
                entry = new ItemDatabase.WeaponResistancePenetrationEntry();
                resistancePenetrations[i] = entry;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                entry.resistanceType = (ItemDatabase.ResistanceModifierType)EditorGUILayout.Popup(
                    (int)entry.resistanceType,
                    ItemEditorLabels.ResistanceModifierTypeLabels,
                    GUILayout.MaxWidth(140f));
                EditorGUILayout.LabelField("=", GUILayout.Width(12f));
                entry.value = Mathf.Max(0, EditorGUILayout.IntField(entry.value));

                using (new EditorGUI.DisabledScope(resistancePenetrations.Count <= 1))
                {
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        resistancePenetrations.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        if (GUILayout.Button("增加抗性穿透词条"))
        {
            resistancePenetrations.Add(new ItemDatabase.WeaponResistancePenetrationEntry());
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

    private static List<ItemDatabase.WeaponResistancePenetrationEntry> CloneWeaponResistancePenetrationList(
        List<ItemDatabase.WeaponResistancePenetrationEntry> source)
    {
        List<ItemDatabase.WeaponResistancePenetrationEntry> clone = new List<ItemDatabase.WeaponResistancePenetrationEntry>();
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                ItemDatabase.WeaponResistancePenetrationEntry entry = source[i];
                clone.Add(new ItemDatabase.WeaponResistancePenetrationEntry
                {
                    resistanceType = entry != null ? entry.resistanceType : ItemDatabase.ResistanceModifierType.Physical,
                    value = entry != null ? Mathf.Max(0, entry.value) : 0
                });
            }
        }

        if (clone.Count == 0)
        {
            clone.Add(new ItemDatabase.WeaponResistancePenetrationEntry());
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

        if (createWeaponResistancePenetrations.Count == 0)
        {
            createWeaponResistancePenetrations.Add(new ItemDatabase.WeaponResistancePenetrationEntry());
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

    private static void ResetWeaponResistancePenetrationList(List<ItemDatabase.WeaponResistancePenetrationEntry> resistancePenetrations)
    {
        if (resistancePenetrations == null)
        {
            return;
        }

        resistancePenetrations.Clear();
        resistancePenetrations.Add(new ItemDatabase.WeaponResistancePenetrationEntry());
    }

    private static ItemDatabase.WeaponDamageDistribution CloneWeaponDamageDistribution(ItemDatabase.WeaponDamageDistribution source)
    {
        if (source == null)
        {
            return ItemDatabase.CreateDefaultWeaponDamageDistribution();
        }

        return new ItemDatabase.WeaponDamageDistribution
        {
            physical = Mathf.Max(0, source.physical),
            fire = Mathf.Max(0, source.fire),
            corruption = Mathf.Max(0, source.corruption),
            cold = Mathf.Max(0, source.cold)
        };
    }

    private static void ResetWeaponDamageDistribution(ItemDatabase.WeaponDamageDistribution distribution)
    {
        if (distribution == null)
        {
            return;
        }

        distribution.physical = 100;
        distribution.fire = 0;
        distribution.corruption = 0;
        distribution.cold = 0;
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
