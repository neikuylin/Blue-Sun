using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class EnemyEquipmentEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/EnemyEquipmentDatabase.asset";

    private Vector2 scroll;
    private string newEnemyId = string.Empty;

    [MenuItem("Tools/\u6218\u6597/\u654C\u4EBA\u88C5\u5907\u7F16\u8F91\u5668")]
    private static void Open()
    {
        EnemyEquipmentEditorWindow window = GetWindow<EnemyEquipmentEditorWindow>("\u654C\u4EBA\u88C5\u5907");
        window.minSize = new Vector2(760f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EnemyEquipmentDatabase equipmentDatabase = EnsureEnemyEquipmentDatabase();
        ItemDatabase itemDatabase = ItemDatabase.LoadDefault();
        BattleBootstrap bootstrap = FindObjectOfType<BattleBootstrap>(true);

        EditorGUILayout.LabelField("\u654C\u4EBA\u88C5\u5907\u7F16\u8F91\u5668", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u4E3A\u654C\u4EBA ID \u7ED1\u5B9A\u88C5\u5907\u3002\u8FD0\u884C\u65F6\u6218\u6597\u4F1A\u6309\u89D2\u8272 ID \u8BFB\u53D6\u8FD9\u91CC\u7684\u88C5\u5907\uFF0C\u7528\u4E8E\u653B\u51FB\u529B\u548C\u9644\u5E26\u6280\u80FD\u8BA1\u7B97\u3002",
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
                    SyncSceneEnemies(equipmentDatabase, bootstrap);
                }
            }
        }

        if (itemDatabase == null)
        {
            EditorGUILayout.HelpBox("\u672A\u627E\u5230 ItemDatabase\u3002\u5148\u521B\u5EFA\u7269\u54C1\u6570\u636E\u5E93\u3002", MessageType.Error);
            return;
        }

        DrawAddPanel(equipmentDatabase);
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
            DrawEnemyEntry(equipmentDatabase, itemDatabase, enemyIds[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAddPanel(EnemyEquipmentDatabase equipmentDatabase)
    {
        EditorGUILayout.LabelField("\u65B0\u589E\u654C\u4EBA\u7ED1\u5B9A", EditorStyles.boldLabel);
        newEnemyId = EditorGUILayout.TextField("\u654C\u4EBAID", newEnemyId);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEnemyId)))
        {
            if (GUILayout.Button("\u65B0\u589E\u7A7A\u88C5\u5907\u7ED1\u5B9A"))
            {
                Undo.RecordObject(equipmentDatabase, "\u65B0\u589E\u654C\u4EBA\u88C5\u5907\u7ED1\u5B9A");
                equipmentDatabase.GetOrCreateEntry(newEnemyId.Trim());
                SaveDatabase(equipmentDatabase);
                newEnemyId = string.Empty;
            }
        }
    }

    private void DrawEnemyEntry(
        EnemyEquipmentDatabase equipmentDatabase,
        ItemDatabase itemDatabase,
        string enemyId)
    {
        EnemyEquipmentDatabase.EnemyEquipmentEntry entry = equipmentDatabase.GetOrCreateEntry(enemyId);
        EnemyEquipmentDatabase.EnsureValidItemList(entry);

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
                    equipmentDatabase.RemoveEntry(enemyId);
                    SaveDatabase(equipmentDatabase);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.BeginChangeCheck();
            for (int slotIndex = 0; slotIndex < EnemyEquipmentDatabase.SlotCount; slotIndex++)
            {
                DrawSlotPopup(itemDatabase, equipmentDatabase, entry, slotIndex);
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveDatabase(equipmentDatabase);
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

    private static void SyncSceneEnemies(EnemyEquipmentDatabase equipmentDatabase, BattleBootstrap bootstrap)
    {
        if (equipmentDatabase == null || bootstrap == null || bootstrap.enemySpawns == null)
        {
            return;
        }

        Undo.RecordObject(equipmentDatabase, "\u540C\u6B65\u573A\u666F\u654C\u4EBA\u88C5\u5907");
        for (int i = 0; i < bootstrap.enemySpawns.Count; i++)
        {
            BattleBootstrap.EnemySpawnEntry enemy = bootstrap.enemySpawns[i];
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyId))
            {
                continue;
            }

            equipmentDatabase.GetOrCreateEntry(enemy.enemyId.Trim());
        }

        SaveDatabase(equipmentDatabase);
    }

    private static EnemyEquipmentDatabase EnsureEnemyEquipmentDatabase()
    {
        EnemyEquipmentDatabase database = AssetDatabase.LoadAssetAtPath<EnemyEquipmentDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<EnemyEquipmentDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
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
}
