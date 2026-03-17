using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class EnemyEquipmentEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string EquipmentAssetPath = AssetFolder + "/EnemyEquipmentDatabase.asset";
    private const string SkillLoadoutAssetPath = AssetFolder + "/CharacterSkillLoadoutDatabase.asset";
    private const int DefaultSkillSlotCount = 6;

    private Vector2 scroll;
    private string newEnemyId = string.Empty;

    [MenuItem("Tools/\u6218\u6597/\u654C\u4EBA\u88C5\u5907\u6280\u80FD\u7F16\u8F91\u5668")]
    private static void Open()
    {
        EnemyEquipmentEditorWindow window = GetWindow<EnemyEquipmentEditorWindow>("\u654C\u4EBA\u88C5\u5907\u6280\u80FD");
        window.minSize = new Vector2(760f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EnemyEquipmentDatabase equipmentDatabase = EnsureEnemyEquipmentDatabase();
        CharacterSkillLoadoutDatabase skillLoadoutDatabase = EnsureSkillLoadoutDatabase();
        ItemDatabase itemDatabase = ItemDatabase.LoadDefault();
        BattleSkillDatabase battleSkillDatabase = BattleSkillDatabase.LoadDefault();
        BattleBootstrap bootstrap = FindObjectOfType<BattleBootstrap>(true);

        EditorGUILayout.LabelField("\u654C\u4EBA\u88C5\u5907\u6280\u80FD\u7F16\u8F91\u5668", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u4E3A\u654C\u4EBA ID \u7ED1\u5B9A\u88C5\u5907\u548C\u6280\u80FD\u680F\u4F4D\u3002\u8FD0\u884C\u65F6\u6218\u6597\u4F1A\u6309\u89D2\u8272 ID \u8BFB\u53D6\u8FD9\u4E9B\u914D\u7F6E\u3002",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65B0"))
            {
                Repaint();
            }

            using (new EditorGUI.DisabledScope(bootstrap == null))
            {
                if (GUILayout.Button("\u540C\u6B65\u5F53\u524D\u573A\u666F\u654C\u4EBA"))
                {
                    SyncSceneEnemies(equipmentDatabase, skillLoadoutDatabase, bootstrap);
                }
            }
        }

        if (itemDatabase == null)
        {
            EditorGUILayout.HelpBox("\u672A\u627E\u5230 ItemDatabase\u3002\u5148\u521B\u5EFA\u7269\u54C1\u6570\u636E\u5E93\u3002", MessageType.Error);
            return;
        }

        DrawAddPanel(equipmentDatabase, skillLoadoutDatabase);
        EditorGUILayout.Space(8f);

        List<string> enemyIds = CollectEnemyIds(equipmentDatabase, bootstrap);
        if (enemyIds.Count == 0)
        {
            EditorGUILayout.HelpBox("\u5F53\u524D\u6CA1\u6709\u53EF\u7F16\u8F91\u7684\u654C\u4EBA ID\u3002", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < enemyIds.Count; i++)
        {
            DrawEnemyEntry(equipmentDatabase, skillLoadoutDatabase, itemDatabase, battleSkillDatabase, enemyIds[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAddPanel(
        EnemyEquipmentDatabase equipmentDatabase,
        CharacterSkillLoadoutDatabase skillLoadoutDatabase)
    {
        EditorGUILayout.LabelField("\u65B0\u589E\u654C\u4EBA\u7ED1\u5B9A", EditorStyles.boldLabel);
        newEnemyId = EditorGUILayout.TextField("\u654C\u4EBAID", newEnemyId);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEnemyId)))
        {
            if (GUILayout.Button("\u65B0\u589E\u7A7A\u88C5\u5907\u7ED1\u5B9A"))
            {
                Undo.RecordObject(equipmentDatabase, "\u65B0\u589E\u654C\u4EBA\u88C5\u5907\u7ED1\u5B9A");
                Undo.RecordObject(skillLoadoutDatabase, "\u65B0\u589E\u654C\u4EBA\u6280\u80FD\u7ED1\u5B9A");
                equipmentDatabase.GetOrCreateEntry(newEnemyId.Trim());
                skillLoadoutDatabase.GetOrCreateEntry(newEnemyId.Trim());
                SaveDatabase(equipmentDatabase);
                SaveDatabase(skillLoadoutDatabase);
                newEnemyId = string.Empty;
            }
        }
    }

    private void DrawEnemyEntry(
        EnemyEquipmentDatabase equipmentDatabase,
        CharacterSkillLoadoutDatabase skillLoadoutDatabase,
        ItemDatabase itemDatabase,
        BattleSkillDatabase battleSkillDatabase,
        string enemyId)
    {
        EnemyEquipmentDatabase.EnemyEquipmentEntry entry = equipmentDatabase.GetOrCreateEntry(enemyId);
        EnemyEquipmentDatabase.EnsureValidItemList(entry);
        CharacterSkillLoadoutDatabase.CharacterSkillEntry skillEntry = skillLoadoutDatabase.GetOrCreateEntry(enemyId);
        CharacterSkillLoadoutDatabase.EnsureSlotDataSize(skillEntry, DefaultSkillSlotCount);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(enemyId, EditorStyles.boldLabel);

                if (GUILayout.Button("\u6E05\u7A7A\u88C5\u5907", GUILayout.Width(96f)))
                {
                    Undo.RecordObject(equipmentDatabase, "\u6E05\u7A7A\u654C\u4EBA\u88C5\u5907");
                    for (int i = 0; i < entry.itemIds.Count; i++)
                    {
                        entry.itemIds[i] = string.Empty;
                    }

                    SaveDatabase(equipmentDatabase);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("\u5220\u9664\u7ED1\u5B9A", GUILayout.Width(96f)))
                {
                    Undo.RecordObject(equipmentDatabase, "\u5220\u9664\u654C\u4EBA\u88C5\u5907\u7ED1\u5B9A");
                    Undo.RecordObject(skillLoadoutDatabase, "\u5220\u9664\u654C\u4EBA\u6280\u80FD\u7ED1\u5B9A");
                    equipmentDatabase.RemoveEntry(enemyId);
                    RemoveSkillEntry(skillLoadoutDatabase, enemyId);
                    SaveDatabase(equipmentDatabase);
                    SaveDatabase(skillLoadoutDatabase);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.BeginChangeCheck();
            for (int slotIndex = 0; slotIndex < EnemyEquipmentDatabase.SlotCount; slotIndex++)
            {
                DrawSlotPopup(itemDatabase, equipmentDatabase, entry, slotIndex);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("\u6280\u80FD\u680F\u4F4D", EditorStyles.boldLabel);
            DrawSkillSlots(skillLoadoutDatabase, battleSkillDatabase, skillEntry);

            if (EditorGUI.EndChangeCheck())
            {
                SaveDatabase(equipmentDatabase);
                SaveDatabase(skillLoadoutDatabase);
            }
        }
    }

    private static void DrawSlotPopup(
        ItemDatabase itemDatabase,
        EnemyEquipmentDatabase equipmentDatabase,
        EnemyEquipmentDatabase.EnemyEquipmentEntry entry,
        int slotIndex)
    {
        ItemDatabase.EquipmentSlotType slotType = EnemyEquipmentDatabase.SlotTypes[slotIndex];
        List<ItemDatabase.ItemEntry> candidates = BuildCompatibleItems(itemDatabase, slotType);
        string[] options = BuildItemOptions(candidates);

        string currentItemId = slotIndex < entry.itemIds.Count ? entry.itemIds[slotIndex] : string.Empty;
        int selectedIndex = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            ItemDatabase.ItemEntry candidate = candidates[i];
            if (candidate != null && string.Equals(candidate.itemId, currentItemId, StringComparison.Ordinal))
            {
                selectedIndex = i + 1;
                break;
            }
        }

        int newIndex = EditorGUILayout.Popup(EnemyEquipmentDatabase.SlotLabels[slotIndex], selectedIndex, options);
        if (newIndex != selectedIndex)
        {
            Undo.RecordObject(equipmentDatabase, "\u7F16\u8F91\u654C\u4EBA\u88C5\u5907");
        }

        string nextItemId = newIndex <= 0 ? string.Empty : candidates[newIndex - 1].itemId;
        entry.itemIds[slotIndex] = nextItemId;
    }

    private static void DrawSkillSlots(
        CharacterSkillLoadoutDatabase skillLoadoutDatabase,
        BattleSkillDatabase battleSkillDatabase,
        CharacterSkillLoadoutDatabase.CharacterSkillEntry skillEntry)
    {
        if (skillEntry == null)
        {
            return;
        }

        List<BattleSkillDatabase.SkillEntry> skills = battleSkillDatabase != null
            ? battleSkillDatabase.Entries
            : new List<BattleSkillDatabase.SkillEntry>();
        string[] options = BuildSkillOptions(skills);

        for (int i = 0; i < skillEntry.skillIds.Count; i++)
        {
            string currentSkillId = skillEntry.skillIds[i];
            int selectedIndex = 0;
            for (int s = 0; s < skills.Count; s++)
            {
                BattleSkillDatabase.SkillEntry skill = skills[s];
                if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
                {
                    continue;
                }

                if (string.Equals(skill.skillId, currentSkillId, StringComparison.Ordinal))
                {
                    selectedIndex = s + 1;
                    break;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                int newIndex = EditorGUILayout.Popup($"\u6280\u80FD{i + 1}", selectedIndex, options);
                int currentWeight = CharacterSkillLoadoutDatabase.GetSkillWeightAt(skillEntry, i);
                int newWeight = EditorGUILayout.IntField("\u6743\u91CD", currentWeight, GUILayout.Width(180f));

                if (newIndex != selectedIndex || newWeight != currentWeight)
                {
                    Undo.RecordObject(skillLoadoutDatabase, "\u7F16\u8F91\u654C\u4EBA\u6280\u80FD");
                }

                skillEntry.skillIds[i] = newIndex <= 0 ? string.Empty : skills[newIndex - 1].skillId;
                skillEntry.skillWeights[i] = newWeight;
            }
        }
    }

    private static List<ItemDatabase.ItemEntry> BuildCompatibleItems(
        ItemDatabase itemDatabase,
        ItemDatabase.EquipmentSlotType slotType)
    {
        List<ItemDatabase.ItemEntry> result = new List<ItemDatabase.ItemEntry>();
        if (itemDatabase == null)
        {
            return result;
        }

        List<ItemDatabase.ItemEntry> entries = itemDatabase.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = entries[i];
            if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
            {
                continue;
            }

            if (!IsEquipmentSlotCompatible(entry.equipmentSlot, slotType))
            {
                continue;
            }

            result.Add(entry);
        }

        result.Sort((left, right) =>
        {
            string leftName = ResolveItemLabel(left);
            string rightName = ResolveItemLabel(right);
            return string.Compare(leftName, rightName, StringComparison.Ordinal);
        });
        return result;
    }

    private static bool IsEquipmentSlotCompatible(
        ItemDatabase.EquipmentSlotType itemSlot,
        ItemDatabase.EquipmentSlotType targetSlot)
    {
        if (itemSlot == ItemDatabase.EquipmentSlotType.None || targetSlot == ItemDatabase.EquipmentSlotType.None)
        {
            return false;
        }

        if (itemSlot == targetSlot)
        {
            return true;
        }

        if (itemSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return targetSlot == ItemDatabase.EquipmentSlotType.MainHand ||
                targetSlot == ItemDatabase.EquipmentSlotType.OffHand ||
                targetSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand;
        }

        if (targetSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return itemSlot == ItemDatabase.EquipmentSlotType.MainHand ||
                itemSlot == ItemDatabase.EquipmentSlotType.OffHand ||
                itemSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand;
        }

        return false;
    }

    private static string[] BuildItemOptions(List<ItemDatabase.ItemEntry> candidates)
    {
        string[] options = new string[candidates.Count + 1];
        options[0] = "(\u7A7A)";
        for (int i = 0; i < candidates.Count; i++)
        {
            options[i + 1] = ResolveItemLabel(candidates[i]);
        }

        return options;
    }

    private static string[] BuildSkillOptions(List<BattleSkillDatabase.SkillEntry> skills)
    {
        List<string> options = new List<string> { "(\u7A7A)" };
        if (skills != null)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                BattleSkillDatabase.SkillEntry skill = skills[i];
                if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
                {
                    continue;
                }

                options.Add(skill.skillId);
            }
        }

        return options.ToArray();
    }

    private static string ResolveItemLabel(ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return "(\u7A7A)";
        }

        if (!string.IsNullOrWhiteSpace(entry.displayName))
        {
            return $"{entry.displayName} ({entry.itemId})";
        }

        return entry.itemId ?? string.Empty;
    }

    private static List<string> CollectEnemyIds(EnemyEquipmentDatabase equipmentDatabase, BattleBootstrap bootstrap)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        if (equipmentDatabase != null && equipmentDatabase.Entries != null)
        {
            for (int i = 0; i < equipmentDatabase.Entries.Count; i++)
            {
                EnemyEquipmentDatabase.EnemyEquipmentEntry entry = equipmentDatabase.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.characterId))
                {
                    ids.Add(entry.characterId);
                }
            }
        }

        if (bootstrap != null && bootstrap.enemySpawns != null)
        {
            for (int i = 0; i < bootstrap.enemySpawns.Count; i++)
            {
                BattleBootstrap.EnemySpawnEntry enemy = bootstrap.enemySpawns[i];
                if (enemy != null && !string.IsNullOrWhiteSpace(enemy.enemyId))
                {
                    ids.Add(enemy.enemyId);
                }
            }
        }

        List<string> result = new List<string>(ids);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void SyncSceneEnemies(
        EnemyEquipmentDatabase equipmentDatabase,
        CharacterSkillLoadoutDatabase skillLoadoutDatabase,
        BattleBootstrap bootstrap)
    {
        if (equipmentDatabase == null || skillLoadoutDatabase == null || bootstrap == null || bootstrap.enemySpawns == null)
        {
            return;
        }

        Undo.RecordObject(equipmentDatabase, "\u540C\u6B65\u573A\u666F\u654C\u4EBA\u88C5\u5907");
        Undo.RecordObject(skillLoadoutDatabase, "\u540C\u6B65\u573A\u666F\u654C\u4EBA\u6280\u80FD");
        for (int i = 0; i < bootstrap.enemySpawns.Count; i++)
        {
            BattleBootstrap.EnemySpawnEntry enemy = bootstrap.enemySpawns[i];
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyId))
            {
                continue;
            }

            equipmentDatabase.GetOrCreateEntry(enemy.enemyId.Trim());
            CharacterSkillLoadoutDatabase.EnsureSlotDataSize(
                skillLoadoutDatabase.GetOrCreateEntry(enemy.enemyId.Trim()),
                DefaultSkillSlotCount);
        }

        SaveDatabase(equipmentDatabase);
        SaveDatabase(skillLoadoutDatabase);
    }

    private static EnemyEquipmentDatabase EnsureEnemyEquipmentDatabase()
    {
        EnemyEquipmentDatabase database = AssetDatabase.LoadAssetAtPath<EnemyEquipmentDatabase>(EquipmentAssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<EnemyEquipmentDatabase>();
        AssetDatabase.CreateAsset(database, EquipmentAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static CharacterSkillLoadoutDatabase EnsureSkillLoadoutDatabase()
    {
        CharacterSkillLoadoutDatabase database = AssetDatabase.LoadAssetAtPath<CharacterSkillLoadoutDatabase>(SkillLoadoutAssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<CharacterSkillLoadoutDatabase>();
        AssetDatabase.CreateAsset(database, SkillLoadoutAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void SaveDatabase(EnemyEquipmentDatabase equipmentDatabase)
    {
        if (equipmentDatabase == null)
        {
            return;
        }

        EditorUtility.SetDirty(equipmentDatabase);
        AssetDatabase.SaveAssets();
    }

    private static void SaveDatabase(CharacterSkillLoadoutDatabase skillLoadoutDatabase)
    {
        if (skillLoadoutDatabase == null)
        {
            return;
        }

        EditorUtility.SetDirty(skillLoadoutDatabase);
        AssetDatabase.SaveAssets();
    }

    private static void RemoveSkillEntry(CharacterSkillLoadoutDatabase skillLoadoutDatabase, string characterId)
    {
        if (skillLoadoutDatabase == null || string.IsNullOrWhiteSpace(characterId) || skillLoadoutDatabase.Entries == null)
        {
            return;
        }

        for (int i = skillLoadoutDatabase.Entries.Count - 1; i >= 0; i--)
        {
            CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = skillLoadoutDatabase.Entries[i];
            if (entry == null || !string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            skillLoadoutDatabase.Entries.RemoveAt(i);
            return;
        }
    }
}
