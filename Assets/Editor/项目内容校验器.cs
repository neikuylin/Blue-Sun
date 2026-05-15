using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 项目内容校验器 : EditorWindow
{
    private const string StartScenePath = "Assets/Scenes/开始界面.unity";
    private static readonly List<校验结果> lastResults = new List<校验结果>();
    private Vector2 scrollPosition;

    private enum 严重级别
    {
        错误,
        警告
    }

    private struct 校验结果
    {
        public 严重级别 level;
        public string message;
    }

    [MenuItem("Tools/校验/项目内容校验器")]
    private static void Open()
    {
        项目内容校验器 window = GetWindow<项目内容校验器>("项目内容校验器");
        window.Show();
        RunValidation();
    }

    [MenuItem("Tools/校验/运行项目内容校验")]
    private static void RunValidationMenu()
    {
        RunValidation();
        PrintResultsToConsole();
    }

    [MenuItem("Tools/校验/自动去重格子模板可行走格")]
    private static void DeduplicateGridTemplateWalkableCellsMenu()
    {
        格子模板数据库 database = 格子模板数据库.LoadDefault();
        if (database == null)
        {
            Debug.LogError("格子模板可行走格去重：缺少 Resources 数据库：格子模板数据库");
            return;
        }

        int removedCount = DeduplicateGridTemplateWalkableCells(database);
        if (removedCount <= 0)
        {
            EditorUtility.DisplayDialog("格子模板可行走格去重", "没有发现重复可行走格。", "确定");
            return;
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        RunValidation();
        EditorUtility.DisplayDialog("格子模板可行走格去重", $"已移除 {removedCount} 个重复可行走格。", "确定");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("运行校验"))
        {
            RunValidation();
        }

        int errorCount = CountResults(严重级别.错误);
        int warningCount = CountResults(严重级别.警告);
        EditorGUILayout.LabelField($"错误: {errorCount}    警告: {warningCount}");

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < lastResults.Count; i++)
        {
            校验结果 result = lastResults[i];
            MessageType messageType = result.level == 严重级别.错误 ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(result.message, messageType);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void RunValidation()
    {
        lastResults.Clear();

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        BattleCharacterBindingDatabase bindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        ItemDatabase itemDatabase = ItemDatabase.LoadDefault();
        BattleSkillDatabase skillDatabase = BattleSkillDatabase.LoadDefault();
        EffectDatabase effectDatabase = EffectDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase skillLoadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        RoomEnemyPresetDatabase enemyPresetDatabase = RoomEnemyPresetDatabase.LoadDefault();
        EnemyEquipmentDatabase enemyEquipmentDatabase = EnemyEquipmentDatabase.LoadDefault();
        MapTemplateDatabase mapTemplateDatabase = MapTemplateDatabase.LoadDefault();
        格子模板数据库 gridTemplateDatabase = 格子模板数据库.LoadDefault();
        RoomTypeDatabase roomTypeDatabase = RoomTypeDatabase.LoadDefault();
        EventDatabase eventDatabase = EventDatabase.LoadDefault();

        ValidateRequiredDatabase(statDatabase, nameof(CharacterStatDatabase));
        ValidateRequiredDatabase(bindingDatabase, nameof(BattleCharacterBindingDatabase));
        ValidateRequiredDatabase(itemDatabase, nameof(ItemDatabase));
        ValidateRequiredDatabase(skillDatabase, nameof(BattleSkillDatabase));
        ValidateRequiredDatabase(effectDatabase, nameof(EffectDatabase));
        ValidateRequiredDatabase(skillLoadoutDatabase, nameof(CharacterSkillLoadoutDatabase));
        ValidateRequiredDatabase(enemyPresetDatabase, nameof(RoomEnemyPresetDatabase));
        ValidateRequiredDatabase(enemyEquipmentDatabase, nameof(EnemyEquipmentDatabase));
        ValidateRequiredDatabase(mapTemplateDatabase, nameof(MapTemplateDatabase));
        ValidateRequiredDatabase(gridTemplateDatabase, nameof(格子模板数据库));
        ValidateRequiredDatabase(roomTypeDatabase, nameof(RoomTypeDatabase));
        ValidateRequiredDatabase(eventDatabase, nameof(EventDatabase));

        ValidateBuildScenes();
        ValidateCharacterStats(statDatabase);
        ValidateCharacterBindings(bindingDatabase, statDatabase);
        ValidateItems(itemDatabase, skillDatabase);
        ValidateSkills(skillDatabase, effectDatabase);
        ValidateSkillLoadouts(skillLoadoutDatabase, statDatabase, skillDatabase);
        ValidateEnemyEquipment(enemyEquipmentDatabase, statDatabase, itemDatabase);
        ValidateEnemyPresets(enemyPresetDatabase, statDatabase, roomTypeDatabase);
        ValidateGridTemplates(gridTemplateDatabase);
        ValidateMapTemplates(mapTemplateDatabase, enemyPresetDatabase, gridTemplateDatabase, roomTypeDatabase);
        ValidateEvents(eventDatabase);
    }

    private static void ValidateBuildScenes()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            AddError("构建场景列表为空。");
            return;
        }

        bool containsStartScene = false;
        string firstEnabledScene = string.Empty;
        for (int i = 0; i < scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = scenes[i];
            if (scene == null || string.IsNullOrWhiteSpace(scene.path))
            {
                continue;
            }

            if (scene.enabled && string.IsNullOrEmpty(firstEnabledScene))
            {
                firstEnabledScene = scene.path;
            }

            if (string.Equals(scene.path, StartScenePath, StringComparison.Ordinal))
            {
                containsStartScene = true;
            }
        }

        if (!containsStartScene)
        {
            AddError($"Build Settings 缺少开始界面：{StartScenePath}");
        }

        if (!string.Equals(firstEnabledScene, StartScenePath, StringComparison.Ordinal))
        {
            AddError($"Build Settings 第一个启用场景应为 {StartScenePath}，当前是 {firstEnabledScene}");
        }
    }

    private static void ValidateCharacterStats(CharacterStatDatabase database)
    {
        if (database == null || database.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            CharacterStatDatabase.StatEntry entry = database.Entries[i];
            string context = $"CharacterStatDatabase 第 {i + 1} 项";
            if (entry == null)
            {
                AddError($"{context} 为空。");
                continue;
            }

            string id = NormalizeId(entry.characterId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"{context} characterId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"CharacterStatDatabase 存在重复角色ID：{id}");
            }
        }
    }

    private static void ValidateCharacterBindings(BattleCharacterBindingDatabase database, CharacterStatDatabase statDatabase)
    {
        if (database == null || database.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            BattleCharacterBindingDatabase.BindingEntry entry = database.Entries[i];
            string context = $"BattleCharacterBindings 第 {i + 1} 项";
            if (entry == null)
            {
                AddError($"{context} 为空。");
                continue;
            }

            string id = NormalizeId(entry.characterId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"{context} characterId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"BattleCharacterBindings 存在重复角色ID：{id}");
            }

            if (statDatabase != null && statDatabase.FindEntry(id) == null)
            {
                AddError($"角色绑定 '{id}' 缺少 CharacterStatDatabase 数据。");
            }

            if (entry.modelPrefab == null)
            {
                AddError($"角色绑定 '{id}' 缺少 modelPrefab。");
            }
        }
    }

    private static void ValidateItems(ItemDatabase itemDatabase, BattleSkillDatabase skillDatabase)
    {
        if (itemDatabase == null || itemDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < itemDatabase.Entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = itemDatabase.Entries[i];
            string context = $"ItemDatabase 第 {i + 1} 项";
            if (entry == null)
            {
                AddError($"{context} 为空。");
                continue;
            }

            string id = NormalizeId(entry.itemId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"{context} itemId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"ItemDatabase 存在重复物品ID：{id}");
            }

            if (entry.prefab == null)
            {
                AddWarning($"物品 '{id}' 缺少 prefab，背包图标可能无法解析。");
            }

            if (entry.category == ItemDatabase.ItemCategory.Equipment &&
                entry.weaponCategory != ItemDatabase.WeaponCategory.None &&
                !IsWeaponDamageDistributionValid(entry.weaponDamageDistribution))
            {
                AddError($"武器 '{id}' 的伤害分布无效，四项必须非负且总和为 100。");
            }

            ValidateSkillIdList($"物品 '{id}' grantedSkillIds", entry.grantedSkillIds, skillDatabase);
        }
    }

    private static void ValidateSkills(BattleSkillDatabase skillDatabase, EffectDatabase effectDatabase)
    {
        if (skillDatabase == null || skillDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < skillDatabase.Entries.Count; i++)
        {
            BattleSkillDatabase.SkillEntry entry = skillDatabase.Entries[i];
            string context = $"BattleSkillDatabase 第 {i + 1} 项";
            if (entry == null)
            {
                AddError($"{context} 为空。");
                continue;
            }

            string id = NormalizeId(entry.skillId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"{context} skillId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"BattleSkillDatabase 存在重复技能ID：{id}");
            }

            if (entry.cooldownTurns > 0)
            {
                AddWarning($"技能 '{id}' 配置了 cooldownTurns={entry.cooldownTurns}，但当前运行时尚未实现技能冷却。");
            }

            if (entry.attachedEffects == null)
            {
                AddWarning($"技能 '{id}' attachedEffects 列表为空引用。");
            }
            else
            {
                for (int e = 0; e < entry.attachedEffects.Count; e++)
                {
                    BattleSkillDatabase.SkillEntry.AttachedEffectEntry effect = entry.attachedEffects[e];
                    if (effect == null || string.IsNullOrWhiteSpace(effect.effectId))
                    {
                        continue;
                    }

                    if (effectDatabase != null && effectDatabase.FindEntry(effect.effectId.Trim()) == null)
                    {
                        AddError($"技能 '{id}' 引用了不存在的效果ID：{effect.effectId}");
                    }
                }
            }

            if (entry.attachedEffectIds != null)
            {
                for (int e = 0; e < entry.attachedEffectIds.Count; e++)
                {
                    string effectId = NormalizeId(entry.attachedEffectIds[e]);
                    if (string.IsNullOrEmpty(effectId))
                    {
                        continue;
                    }

                    if (effectDatabase != null && effectDatabase.FindEntry(effectId) == null)
                    {
                        AddError($"技能 '{id}' 的旧效果列表引用了不存在的效果ID：{effectId}");
                    }
                }
            }
        }
    }

    private static void ValidateSkillLoadouts(
        CharacterSkillLoadoutDatabase loadoutDatabase,
        CharacterStatDatabase statDatabase,
        BattleSkillDatabase skillDatabase)
    {
        if (loadoutDatabase == null || loadoutDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < loadoutDatabase.Entries.Count; i++)
        {
            CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = loadoutDatabase.Entries[i];
            if (entry == null)
            {
                AddError($"CharacterSkillLoadoutDatabase 第 {i + 1} 项为空。");
                continue;
            }

            string id = NormalizeId(entry.characterId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"CharacterSkillLoadoutDatabase 第 {i + 1} 项 characterId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"CharacterSkillLoadoutDatabase 存在重复角色ID：{id}");
            }

            if (statDatabase != null && statDatabase.FindEntry(id) == null)
            {
                AddError($"技能配置角色 '{id}' 缺少 CharacterStatDatabase 数据。");
            }

            ValidateSkillIdList($"角色 '{id}' memorizedSkillIds", entry.memorizedSkillIds, skillDatabase);
            ValidateSkillIdList($"角色 '{id}' warehouseSkillIds", entry.warehouseSkillIds, skillDatabase);
        }
    }

    private static void ValidateEnemyEquipment(
        EnemyEquipmentDatabase equipmentDatabase,
        CharacterStatDatabase statDatabase,
        ItemDatabase itemDatabase)
    {
        if (equipmentDatabase == null || equipmentDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < equipmentDatabase.Entries.Count; i++)
        {
            EnemyEquipmentDatabase.EnemyEquipmentEntry entry = equipmentDatabase.Entries[i];
            if (entry == null)
            {
                AddError($"EnemyEquipmentDatabase 第 {i + 1} 项为空。");
                continue;
            }

            string id = NormalizeId(entry.characterId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"EnemyEquipmentDatabase 第 {i + 1} 项 characterId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"EnemyEquipmentDatabase 存在重复角色ID：{id}");
            }

            if (statDatabase != null && statDatabase.FindEntry(id) == null)
            {
                AddError($"敌人装备角色 '{id}' 缺少 CharacterStatDatabase 数据。");
            }

            if (entry.itemIds == null)
            {
                continue;
            }

            for (int itemIndex = 0; itemIndex < entry.itemIds.Count; itemIndex++)
            {
                string itemId = NormalizeId(entry.itemIds[itemIndex]);
                if (string.IsNullOrEmpty(itemId))
                {
                    continue;
                }

                ItemDatabase.ItemEntry item = itemDatabase != null ? itemDatabase.FindEntry(itemId) : null;
                if (item == null)
                {
                    AddError($"敌人装备 '{id}' 引用了不存在的物品ID：{itemId}");
                }
                else if (item.category != ItemDatabase.ItemCategory.Equipment)
                {
                    AddError($"敌人装备 '{id}' 的物品 '{itemId}' 不是装备。");
                }
            }
        }
    }

    private static void ValidateEnemyPresets(
        RoomEnemyPresetDatabase presetDatabase,
        CharacterStatDatabase statDatabase,
        RoomTypeDatabase roomTypeDatabase)
    {
        if (presetDatabase == null || presetDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < presetDatabase.Entries.Count; i++)
        {
            RoomEnemyPresetDatabase.RoomEnemyPresetEntry entry = presetDatabase.Entries[i];
            if (entry == null)
            {
                AddError($"RoomEnemyPresetDatabase 第 {i + 1} 项为空。");
                continue;
            }

            string id = NormalizeId(entry.presetId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"RoomEnemyPresetDatabase 第 {i + 1} 项 presetId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"RoomEnemyPresetDatabase 存在重复预设ID：{id}");
            }

            ValidateRoomType($"遭遇预设 '{id}'", entry.roomTypeId, roomTypeDatabase);

            if (entry.enemies == null)
            {
                continue;
            }

            for (int enemyIndex = 0; enemyIndex < entry.enemies.Count; enemyIndex++)
            {
                RoomEnemyPresetDatabase.PresetEnemyEntry enemy = entry.enemies[enemyIndex];
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyId))
                {
                    AddError($"遭遇预设 '{id}' 第 {enemyIndex + 1} 个敌人ID为空。");
                    continue;
                }

                string enemyId = enemy.enemyId.Trim();
                if (statDatabase != null && statDatabase.FindEntry(enemyId) == null)
                {
                    AddError($"遭遇预设 '{id}' 引用了不存在的敌人角色ID：{enemyId}");
                }
            }
        }
    }

    private static void ValidateGridTemplates(格子模板数据库 database)
    {
        if (database == null || database.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            格子模板数据库.格子模板条目 entry = database.Entries[i];
            if (entry == null)
            {
                AddError($"格子模板数据库 第 {i + 1} 项为空。");
                continue;
            }

            string id = NormalizeId(entry.templateId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"格子模板数据库 第 {i + 1} 项 templateId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"格子模板数据库 存在重复模板ID：{id}");
            }

            if (entry.width <= 0 || entry.height <= 0)
            {
                AddError($"格子模板 '{id}' 宽高必须大于 0。");
            }

            if (!entry.hasDefaultPlayerSpawn)
            {
                AddError($"格子模板 '{id}' 缺少玩家默认出生点。");
            }
            else
            {
                ValidateGridSpawnCell($"格子模板 '{id}' 玩家默认出生点", entry.defaultPlayerSpawnCell.ToVector2Int(), entry);
            }

            ValidateOptionalPlayerSpawn($"格子模板 '{id}' 东门玩家出生点", entry.hasEastDoorPlayerSpawn, entry.eastDoorPlayerSpawnCell, entry);
            ValidateOptionalPlayerSpawn($"格子模板 '{id}' 南门玩家出生点", entry.hasSouthDoorPlayerSpawn, entry.southDoorPlayerSpawnCell, entry);
            ValidateOptionalPlayerSpawn($"格子模板 '{id}' 西门玩家出生点", entry.hasWestDoorPlayerSpawn, entry.westDoorPlayerSpawnCell, entry);
            ValidateOptionalPlayerSpawn($"格子模板 '{id}' 北门玩家出生点", entry.hasNorthDoorPlayerSpawn, entry.northDoorPlayerSpawnCell, entry);
            ValidateWalkableCells(id, entry);
            ValidateEnemySpawnSlots(id, entry);
        }
    }

    private static void ValidateMapTemplates(
        MapTemplateDatabase mapDatabase,
        RoomEnemyPresetDatabase presetDatabase,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase)
    {
        if (mapDatabase == null || mapDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> templateIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < mapDatabase.Entries.Count; i++)
        {
            MapTemplateDatabase.MapTemplateEntry template = mapDatabase.Entries[i];
            if (template == null)
            {
                AddError($"MapTemplateDatabase 第 {i + 1} 项为空。");
                continue;
            }

            string templateId = NormalizeId(template.templateId);
            if (string.IsNullOrEmpty(templateId))
            {
                AddError($"MapTemplateDatabase 第 {i + 1} 项 templateId 为空。");
                continue;
            }

            if (!templateIds.Add(templateId))
            {
                AddError($"MapTemplateDatabase 存在重复模板ID：{templateId}");
            }

            ValidateMapNodes(templateId, template, presetDatabase, gridDatabase, roomTypeDatabase);
        }
    }

    private static void ValidateMapNodes(
        string templateId,
        MapTemplateDatabase.MapTemplateEntry template,
        RoomEnemyPresetDatabase presetDatabase,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase)
    {
        if (template.nodes == null || template.nodes.Count == 0)
        {
            AddError($"地图模板 '{templateId}' 没有节点。");
            return;
        }

        HashSet<string> nodeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                AddError($"地图模板 '{templateId}' 第 {i + 1} 个节点为空。");
                continue;
            }

            string nodeId = NormalizeId(node.nodeId);
            if (string.IsNullOrEmpty(nodeId))
            {
                AddError($"地图模板 '{templateId}' 第 {i + 1} 个节点ID为空。");
                continue;
            }

            if (!nodeIds.Add(nodeId))
            {
                AddError($"地图模板 '{templateId}' 存在重复节点ID：{nodeId}");
            }
        }

        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
            {
                continue;
            }

            string nodeId = node.nodeId.Trim();
            string context = $"地图模板 '{templateId}' 节点 '{nodeId}'";
            ValidateRoomType(context, node.roomTypeId, roomTypeDatabase);
            bool requiresBattleGridTemplate = RoomTypeDatabase.RequiresBattleGridTemplate(node.roomTypeId);
            bool requiresEncounterPreset = RoomTypeDatabase.RequiresEncounterPreset(node.roomTypeId);

            格子模板数据库.格子模板条目 gridTemplate = null;
            if (string.IsNullOrWhiteSpace(node.battleGridTemplateId))
            {
                if (requiresBattleGridTemplate)
                {
                    AddError($"{context} 缺少 battleGridTemplateId。");
                }
            }
            else
            {
                gridTemplate = FindGridTemplate(gridDatabase, node.battleGridTemplateId.Trim());
                if (gridTemplate == null)
                {
                    AddError($"{context} 引用了不存在的格子模板：{node.battleGridTemplateId}");
                }
            }

            RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = null;
            if (string.IsNullOrWhiteSpace(node.encounterPresetId))
            {
                if (requiresEncounterPreset)
                {
                    AddError($"{context} 缺少 encounterPresetId。");
                }
            }
            else
            {
                preset = FindEnemyPreset(presetDatabase, node.encounterPresetId.Trim());
                if (preset == null)
                {
                    AddError($"{context} 引用了不存在的遭遇预设：{node.encounterPresetId}");
                }
            }

            ValidateNodeConnections(templateId, nodeId, node, nodeIds);
            ValidateEncounterSpawnBinding(context, preset, gridTemplate);
        }
    }

    private static void ValidateNodeConnections(string templateId, string nodeId, MapTemplateDatabase.MapNodeEntry node, HashSet<string> nodeIds)
    {
        if (node.connections == null)
        {
            return;
        }

        HashSet<MapTemplateDatabase.ConnectionDirection> usedDirections = new HashSet<MapTemplateDatabase.ConnectionDirection>();
        for (int i = 0; i < node.connections.Count; i++)
        {
            MapTemplateDatabase.MapConnectionEntry connection = node.connections[i];
            if (connection == null || string.IsNullOrWhiteSpace(connection.targetNodeId))
            {
                AddError($"地图模板 '{templateId}' 节点 '{nodeId}' 第 {i + 1} 条连接目标为空。");
                continue;
            }

            if (!nodeIds.Contains(connection.targetNodeId.Trim()))
            {
                AddError($"地图模板 '{templateId}' 节点 '{nodeId}' 连接到不存在的节点：{connection.targetNodeId}");
            }

            if (!usedDirections.Add(connection.direction))
            {
                AddError($"地图模板 '{templateId}' 节点 '{nodeId}' 存在重复方向连接：{connection.direction}");
            }
        }
    }

    private static void ValidateEncounterSpawnBinding(
        string context,
        RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset,
        格子模板数据库.格子模板条目 gridTemplate)
    {
        if (preset == null || preset.enemies == null || preset.enemies.Count == 0)
        {
            return;
        }

        if (gridTemplate == null || gridTemplate.enemySpawnSlots == null || gridTemplate.enemySpawnSlots.Count == 0)
        {
            AddError($"{context} 的遭遇预设有敌人，但格子模板缺少敌人出生槽。");
            return;
        }

        HashSet<int> boundIndices = new HashSet<int>();
        for (int i = 0; i < gridTemplate.enemySpawnSlots.Count; i++)
        {
            格子模板数据库.EnemySpawnSlot slot = gridTemplate.enemySpawnSlots[i];
            if (slot == null || slot.encounterEnemyIndex < 0)
            {
                continue;
            }

            if (slot.encounterEnemyIndex >= preset.enemies.Count)
            {
                AddError($"{context} 的敌人出生槽 '{slot.slotName}' 绑定了越界敌人索引 {slot.encounterEnemyIndex}。");
                continue;
            }

            if (!boundIndices.Add(slot.encounterEnemyIndex))
            {
                AddError($"{context} 的敌人索引 {slot.encounterEnemyIndex} 被多个出生槽绑定。");
            }
        }

        for (int i = 0; i < preset.enemies.Count; i++)
        {
            if (!boundIndices.Contains(i))
            {
                AddError($"{context} 的遭遇预设第 {i + 1} 个敌人没有出生槽绑定。");
            }
        }
    }

    private static void ValidateEnemySpawnSlots(string templateId, 格子模板数据库.格子模板条目 entry)
    {
        if (entry.enemySpawnSlots == null)
        {
            return;
        }

        HashSet<int> indices = new HashSet<int>();
        for (int i = 0; i < entry.enemySpawnSlots.Count; i++)
        {
            格子模板数据库.EnemySpawnSlot slot = entry.enemySpawnSlots[i];
            if (slot == null)
            {
                AddError($"格子模板 '{templateId}' 第 {i + 1} 个敌人出生槽为空。");
                continue;
            }

            ValidateGridSpawnCell($"格子模板 '{templateId}' 敌人出生槽 '{slot.slotName}'", slot.cell.ToVector2Int(), entry);
            if (slot.encounterEnemyIndex >= 0 && !indices.Add(slot.encounterEnemyIndex))
            {
                AddError($"格子模板 '{templateId}' 敌人出生索引重复：{slot.encounterEnemyIndex}");
            }
        }
    }

    private static void ValidateWalkableCells(string templateId, 格子模板数据库.格子模板条目 entry)
    {
        if (entry.walkableCells == null)
        {
            return;
        }

        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        for (int i = 0; i < entry.walkableCells.Count; i++)
        {
            Vector2Int cell = entry.walkableCells[i].ToVector2Int();
            ValidateGridCell($"格子模板 '{templateId}' 可行走格第 {i + 1} 项", cell, entry);
            if (!cells.Add(cell))
            {
                AddError($"格子模板 '{templateId}' 存在重复可行走格：{cell}");
            }
        }
    }

    private static void ValidateOptionalPlayerSpawn(
        string context,
        bool enabled,
        格子模板数据库.CellPosition cell,
        格子模板数据库.格子模板条目 entry)
    {
        if (!enabled)
        {
            return;
        }

        ValidateGridSpawnCell(context, cell.ToVector2Int(), entry);
    }

    private static void ValidateGridSpawnCell(string context, Vector2Int cell, 格子模板数据库.格子模板条目 entry)
    {
        ValidateGridCell(context, cell, entry);
        if (!IsCellExplicitlyWalkable(cell, entry))
        {
            AddError($"{context} 不在可行走格列表中：{cell}");
        }
    }

    private static void ValidateGridCell(string context, Vector2Int cell, 格子模板数据库.格子模板条目 entry)
    {
        if (entry == null)
        {
            return;
        }

        if (cell.x < 0 || cell.x >= entry.width || cell.y < 0 || cell.y >= entry.height)
        {
            AddError($"{context} 坐标越界：{cell}");
        }
    }

    private static bool IsCellExplicitlyWalkable(Vector2Int cell, 格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.walkableCells == null || entry.walkableCells.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < entry.walkableCells.Count; i++)
        {
            if (entry.walkableCells[i].ToVector2Int() == cell)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateSkillIdList(string context, List<string> skillIds, BattleSkillDatabase skillDatabase)
    {
        if (skillIds == null)
        {
            return;
        }

        for (int i = 0; i < skillIds.Count; i++)
        {
            string skillId = NormalizeId(skillIds[i]);
            if (string.IsNullOrEmpty(skillId))
            {
                continue;
            }

            if (skillDatabase != null && skillDatabase.FindEntry(skillId) == null)
            {
                AddError($"{context} 引用了不存在的技能ID：{skillId}");
            }
        }
    }

    private static void ValidateRoomType(string context, string roomTypeId, RoomTypeDatabase roomTypeDatabase)
    {
        string id = NormalizeId(roomTypeId);
        if (string.IsNullOrEmpty(id))
        {
            AddError($"{context} 缺少 roomTypeId。");
            return;
        }

        if (roomTypeDatabase != null && roomTypeDatabase.FindEntry(id) == null)
        {
            AddError($"{context} 引用了不存在的房间类型：{id}");
        }
    }

    private static void ValidateEvents(EventDatabase eventDatabase)
    {
        if (eventDatabase == null || eventDatabase.Entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < eventDatabase.Entries.Count; i++)
        {
            EventDatabase.EventEntry entry = eventDatabase.Entries[i];
            string context = $"EventDatabase 第 {i + 1} 项";
            if (entry == null)
            {
                AddError($"{context} 为空。");
                continue;
            }

            string id = NormalizeId(entry.eventId);
            if (string.IsNullOrEmpty(id))
            {
                AddError($"{context} eventId 为空。");
                continue;
            }

            if (!ids.Add(id))
            {
                AddError($"EventDatabase 存在重复事件ID：{id}");
            }
        }
    }

    private static 格子模板数据库.格子模板条目 FindGridTemplate(格子模板数据库 database, string templateId)
    {
        if (database == null || database.Entries == null || string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        string id = templateId.Trim();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            格子模板数据库.格子模板条目 entry = database.Entries[i];
            if (entry != null && string.Equals(entry.templateId, id, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static RoomEnemyPresetDatabase.RoomEnemyPresetEntry FindEnemyPreset(
        RoomEnemyPresetDatabase database,
        string presetId)
    {
        if (database == null || database.Entries == null || string.IsNullOrWhiteSpace(presetId))
        {
            return null;
        }

        string id = presetId.Trim();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            RoomEnemyPresetDatabase.RoomEnemyPresetEntry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.presetId, id, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static void ValidateRequiredDatabase(UnityEngine.Object database, string databaseName)
    {
        if (database == null)
        {
            AddError($"缺少 Resources 数据库：{databaseName}");
        }
    }

    private static int DeduplicateGridTemplateWalkableCells(格子模板数据库 database)
    {
        if (database == null || database.Entries == null)
        {
            return 0;
        }

        int removedCount = 0;
        for (int entryIndex = 0; entryIndex < database.Entries.Count; entryIndex++)
        {
            格子模板数据库.格子模板条目 entry = database.Entries[entryIndex];
            if (entry == null || entry.walkableCells == null)
            {
                continue;
            }

            HashSet<Vector2Int> seenCells = new HashSet<Vector2Int>();
            for (int i = 0; i < entry.walkableCells.Count;)
            {
                Vector2Int cell = entry.walkableCells[i].ToVector2Int();
                if (seenCells.Add(cell))
                {
                    i++;
                    continue;
                }

                entry.walkableCells.RemoveAt(i);
                removedCount++;
            }
        }

        return removedCount;
    }

    private static bool IsWeaponDamageDistributionValid(ItemDatabase.WeaponDamageDistribution distribution)
    {
        if (distribution == null)
        {
            return false;
        }

        return distribution.physical >= 0 &&
            distribution.fire >= 0 &&
            distribution.corruption >= 0 &&
            distribution.cold >= 0 &&
            distribution.Total == 100;
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static void AddError(string message)
    {
        lastResults.Add(new 校验结果
        {
            level = 严重级别.错误,
            message = message
        });
    }

    private static void AddWarning(string message)
    {
        lastResults.Add(new 校验结果
        {
            level = 严重级别.警告,
            message = message
        });
    }

    private static int CountResults(严重级别 level)
    {
        int count = 0;
        for (int i = 0; i < lastResults.Count; i++)
        {
            if (lastResults[i].level == level)
            {
                count++;
            }
        }

        return count;
    }

    private static void PrintResultsToConsole()
    {
        if (lastResults.Count == 0)
        {
            Debug.Log("项目内容校验：通过。");
            return;
        }

        for (int i = 0; i < lastResults.Count; i++)
        {
            校验结果 result = lastResults[i];
            if (result.level == 严重级别.错误)
            {
                Debug.LogError("项目内容校验：" + result.message);
            }
            else
            {
                Debug.LogWarning("项目内容校验：" + result.message);
            }
        }
    }
}
