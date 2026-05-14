using UnityEditor;
using UnityEngine;

public sealed class RoomEnemyPresetEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string PresetAssetPath = ResourceFolder + "/RoomEnemyPresetDatabase.asset";
    private const string StatAssetPath = ResourceFolder + "/CharacterStatDatabase.asset";
    private const string RoomTypeAssetPath = ResourceFolder + "/RoomTypeDatabase.asset";
    private const string DefaultPresetId = "房间预设";
    private const string DefaultEnemyId = "假人";
    private const string EncounterBattleRoomTypeId = RoomTypeDatabase.EncounterBattleTypeId;

    private Vector2 scroll;
    private string newPresetId = DefaultPresetId;
    private static bool resistanceFoldout = true;
    private static bool resistancePenetrationFoldout = true;

    [MenuItem("Tools/战斗/遭遇战编辑器")]
    private static void Open()
    {
        RoomEnemyPresetEditorWindow window = GetWindow<RoomEnemyPresetEditorWindow>("遭遇战编辑器");
        window.minSize = new Vector2(760f, 560f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        RoomEnemyPresetDatabase presetDatabase = EnsurePresetDatabase();
        CharacterStatDatabase statDatabase = EnsureStatDatabase();
        EnsureRoomTypeDatabase();
        EditorGUILayout.LabelField("遭遇战编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "维护遭遇战房间预设。这里编辑的是预设资产，运行时会通过房间节点上的 encounterPresetId 读取它们。敌人站位已改到格子编辑器里的敌人出生位配置。",
            MessageType.Info);

        SerializedObject databaseObject = presetDatabase != null ? new SerializedObject(presetDatabase) : null;
        if (databaseObject != null)
        {
            databaseObject.Update();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (databaseObject != null && GUILayout.Button("保存预设"))
            {
                databaseObject.ApplyModifiedProperties();
                SaveAsset(presetDatabase);
            }

            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }

        }

        DrawAddPanel(presetDatabase);
        EditorGUILayout.Space(8f);

        if (presetDatabase == null)
        {
            EditorGUILayout.HelpBox("房间敌人预设库创建失败。", MessageType.Error);
            return;
        }

        if (presetDatabase.Entries == null || presetDatabase.Entries.Count == 0)
        {
            EditorGUILayout.HelpBox("当前还没有房间敌人预设。", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        SerializedProperty entries = databaseObject.FindProperty("entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            DrawPresetEntry(entries.GetArrayElementAtIndex(i), presetDatabase, statDatabase);
        }

        databaseObject.ApplyModifiedProperties();

        EditorGUILayout.EndScrollView();
    }

    private void DrawAddPanel(RoomEnemyPresetDatabase presetDatabase)
    {
        EditorGUILayout.LabelField("新增预设", EditorStyles.boldLabel);
        newPresetId = EditorGUILayout.TextField("预设ID", newPresetId);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newPresetId)))
        {
            if (GUILayout.Button("新增空预设"))
            {
                Undo.RecordObject(presetDatabase, "新增房间敌人预设");
                RoomEnemyPresetDatabase.RoomEnemyPresetEntry entry = presetDatabase.GetOrCreateEntry(newPresetId.Trim());
                entry.roomTypeId = EncounterBattleRoomTypeId;
                RoomEnemyPresetDatabase.EnsureValidEnemyList(entry);
                newPresetId = string.Empty;
                EditorUtility.SetDirty(presetDatabase);
            }
        }
    }

    private void DrawPresetEntry(
        SerializedProperty presetProperty,
        RoomEnemyPresetDatabase presetDatabase,
        CharacterStatDatabase statDatabase)
    {
        SerializedProperty presetIdProperty = presetProperty.FindPropertyRelative("presetId");
        SerializedProperty roomTypeIdProperty = presetProperty.FindPropertyRelative("roomTypeId");
        SerializedProperty enemiesProperty = presetProperty.FindPropertyRelative("enemies");
        roomTypeIdProperty.stringValue = EncounterBattleRoomTypeId;
        string presetId = presetIdProperty.stringValue;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string presetTitle = string.IsNullOrWhiteSpace(presetId) ? "未命名预设" : presetId;
                presetProperty.isExpanded = EditorGUILayout.Foldout(presetProperty.isExpanded, presetTitle, true);

                using (new EditorGUI.DisabledScope(!presetProperty.isExpanded))
                {
                    if (GUILayout.Button("删除预设", GUILayout.Width(88f)))
                    {
                        Undo.RecordObject(presetDatabase, "删除房间敌人预设");
                        presetDatabase.RemoveEntry(presetId);
                        EditorUtility.SetDirty(presetDatabase);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (!presetProperty.isExpanded)
            {
                int enemyCount = enemiesProperty != null ? enemiesProperty.arraySize : 0;
                EditorGUILayout.LabelField($"房间类型：{RoomTypeDatabase.EncounterBattleTypeName}    敌人数量：{enemyCount}");
                return;
            }

            EditorGUILayout.PropertyField(presetIdProperty, new GUIContent("预设ID"));
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("房间类型", RoomTypeDatabase.EncounterBattleTypeName);
            }

            if (enemiesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("当前预设没有敌人。", MessageType.Info);
            }

            for (int i = 0; i < enemiesProperty.arraySize; i++)
            {
                DrawEnemyEntry(enemiesProperty, i, statDatabase);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新增敌人"))
                {
                    AddEnemyToPreset(enemiesProperty, statDatabase);
                }

                if (GUILayout.Button("导入默认敌人"))
                {
                    AddEnemyToPreset(enemiesProperty, statDatabase, DefaultEnemyId);
                }
            }
        }
    }

    private static void DrawEnemyEntry(SerializedProperty enemiesProperty, int index, CharacterStatDatabase statDatabase)
    {
        SerializedProperty entry = enemiesProperty.GetArrayElementAtIndex(index);
        SerializedProperty enemyIdProperty = entry.FindPropertyRelative("enemyId");
        SerializedProperty teamProperty = entry.FindPropertyRelative("team");
        SerializedProperty isPlayerControlledProperty = entry.FindPropertyRelative("isPlayerControlled");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string title = string.IsNullOrWhiteSpace(enemyIdProperty.stringValue)
                    ? $"敌人 {index + 1}"
                    : enemyIdProperty.stringValue;
                entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, title, true);

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    enemiesProperty.DeleteArrayElementAtIndex(index);
                    return;
                }
            }

            if (!entry.isExpanded)
            {
                string teamSummary = teamProperty != null ? teamProperty.enumDisplayNames[teamProperty.enumValueIndex] : string.Empty;
                EditorGUILayout.LabelField($"阵营：{teamSummary}");
                return;
            }

            EditorGUILayout.PropertyField(enemyIdProperty, new GUIContent("敌人ID"));
            EditorGUILayout.PropertyField(teamProperty, new GUIContent("阵营"));
            EditorGUILayout.PropertyField(isPlayerControlledProperty, new GUIContent("玩家控制"));

            string resolvedEnemyId = enemyIdProperty.stringValue.Trim();
            if (!string.IsNullOrWhiteSpace(resolvedEnemyId))
            {
                DrawStatEditor(statDatabase, EnsureStatEntry(statDatabase, resolvedEnemyId));
            }
        }
    }

    private static void DrawStatEditor(CharacterStatDatabase statDatabase, CharacterStatDatabase.StatEntry statEntry)
    {
        if (statEntry == null)
        {
            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int strength = EditorGUILayout.IntField("力量", statEntry.strength);
        int agility = EditorGUILayout.IntField("敏捷", statEntry.agility);
        int intelligence = EditorGUILayout.IntField("智力", statEntry.intelligence);
        int actionPoints = EditorGUILayout.IntField("行动力", statEntry.actionPoints);
        resistanceFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resistanceFoldout, "抗性");
        int physicalResistance = statEntry.ResolvePhysicalResistance();
        int fireResistance = statEntry.ResolveFireResistance();
        int corruptionResistance = statEntry.ResolveCorruptionResistance();
        int coldResistance = statEntry.ResolveColdResistance();
        if (resistanceFoldout)
        {
            physicalResistance = EditorGUILayout.IntField("物理抗性", physicalResistance);
            fireResistance = EditorGUILayout.IntField("火焰抗性", fireResistance);
            corruptionResistance = EditorGUILayout.IntField("腐败抗性", corruptionResistance);
            coldResistance = EditorGUILayout.IntField("寒冷抗性", coldResistance);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        resistancePenetrationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resistancePenetrationFoldout, "抗性穿透");
        int physicalResistancePenetration = statEntry.ResolvePhysicalResistancePenetration();
        int fireResistancePenetration = statEntry.ResolveFireResistancePenetration();
        int corruptionResistancePenetration = statEntry.ResolveCorruptionResistancePenetration();
        int coldResistancePenetration = statEntry.ResolveColdResistancePenetration();
        if (resistancePenetrationFoldout)
        {
            physicalResistancePenetration = EditorGUILayout.IntField("物理抗性穿透", physicalResistancePenetration);
            fireResistancePenetration = EditorGUILayout.IntField("火焰抗性穿透", fireResistancePenetration);
            corruptionResistancePenetration = EditorGUILayout.IntField("腐败抗性穿透", corruptionResistancePenetration);
            coldResistancePenetration = EditorGUILayout.IntField("寒冷抗性穿透", coldResistancePenetration);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        int criticalChance = EditorGUILayout.IntField("暴击率", statEntry.ResolveCriticalChance());
        int criticalDamage = EditorGUILayout.IntField("暴击伤害", statEntry.ResolveCriticalDamage());
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("HP", CharacterStatDatabase.ResolveMaxHealthFromStrength(strength));
        EditorGUILayout.IntField("MP", CharacterStatDatabase.ResolveMaxManaFromIntelligence(intelligence));
        EditorGUILayout.LabelField("移动距离", CharacterStatDatabase.ResolveMoveDistanceFromAgility(agility).ToString());
        EditorGUILayout.LabelField("物理抗性", CharacterStatDatabase.ResolveResistanceValue(physicalResistance) + "%");
        EditorGUILayout.LabelField("火焰抗性", CharacterStatDatabase.ResolveResistanceValue(fireResistance) + "%");
        EditorGUILayout.LabelField("腐败抗性", CharacterStatDatabase.ResolveResistanceValue(corruptionResistance) + "%");
        EditorGUILayout.LabelField("寒冷抗性", CharacterStatDatabase.ResolveResistanceValue(coldResistance) + "%");
        EditorGUILayout.LabelField("物理抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(physicalResistancePenetration) + "%");
        EditorGUILayout.LabelField("火焰抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(fireResistancePenetration) + "%");
        EditorGUILayout.LabelField("腐败抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(corruptionResistancePenetration) + "%");
        EditorGUILayout.LabelField("寒冷抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(coldResistancePenetration) + "%");
        EditorGUILayout.LabelField("暴击率", CharacterStatDatabase.ResolveCriticalChanceValue(criticalChance) + "%");
        EditorGUILayout.LabelField("暴击伤害", CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamage) + "%");
        EditorGUI.EndDisabledGroup();

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        statEntry.strength = strength;
        statEntry.agility = agility;
        statEntry.intelligence = intelligence;
        statEntry.actionPoints = actionPoints;
        statEntry.physicalResistance = CharacterStatDatabase.ResolveResistanceValue(physicalResistance);
        statEntry.fireResistance = CharacterStatDatabase.ResolveResistanceValue(fireResistance);
        statEntry.corruptionResistance = CharacterStatDatabase.ResolveResistanceValue(corruptionResistance);
        statEntry.coldResistance = CharacterStatDatabase.ResolveResistanceValue(coldResistance);
        statEntry.physicalResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(physicalResistancePenetration);
        statEntry.fireResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(fireResistancePenetration);
        statEntry.corruptionResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(corruptionResistancePenetration);
        statEntry.coldResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(coldResistancePenetration);
        statEntry.criticalChance = CharacterStatDatabase.ResolveCriticalChanceValue(criticalChance);
        statEntry.criticalDamage = CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamage);
        EditorUtility.SetDirty(statDatabase);
    }

    private static void AddEnemyToPreset(SerializedProperty enemiesProperty, CharacterStatDatabase statDatabase, string enemyId = "")
    {
        enemiesProperty.InsertArrayElementAtIndex(enemiesProperty.arraySize);
        SerializedProperty added = enemiesProperty.GetArrayElementAtIndex(enemiesProperty.arraySize - 1);
        added.FindPropertyRelative("enemyId").stringValue = enemyId;
        added.FindPropertyRelative("team").enumValueIndex = (int)BattleTeam.Enemy;
        added.FindPropertyRelative("isPlayerControlled").boolValue = false;

        if (!string.IsNullOrWhiteSpace(enemyId))
        {
            EnsureStatEntry(statDatabase, enemyId);
        }
    }

    private static CharacterStatDatabase.StatEntry EnsureStatEntry(CharacterStatDatabase statDatabase, string enemyId)
    {
        if (statDatabase == null || string.IsNullOrWhiteSpace(enemyId))
        {
            return null;
        }

        CharacterStatDatabase.StatEntry existing = statDatabase.FindEntry(enemyId);
        if (existing != null)
        {
            return existing;
        }

        CharacterStatDatabase.StatEntry created = new CharacterStatDatabase.StatEntry
        {
            characterId = enemyId,
            actionPoints = 4,
            physicalResistancePenetration = 0,
            fireResistancePenetration = 0,
            corruptionResistancePenetration = 0,
            coldResistancePenetration = 0,
            criticalChance = 20,
            criticalDamage = 150
        };
        statDatabase.Entries.Add(created);
        EditorUtility.SetDirty(statDatabase);
        return created;
    }

    private static RoomEnemyPresetDatabase EnsurePresetDatabase()
    {
        RoomEnemyPresetDatabase database = AssetDatabase.LoadAssetAtPath<RoomEnemyPresetDatabase>(PresetAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<RoomEnemyPresetDatabase>();
        AssetDatabase.CreateAsset(database, PresetAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static RoomTypeDatabase EnsureRoomTypeDatabase()
    {
        RoomTypeDatabase database = AssetDatabase.LoadAssetAtPath<RoomTypeDatabase>(RoomTypeAssetPath);
        if (database != null)
        {
            RoomTypeDatabase.RoomTypeEntry encounterBattle = database.GetOrCreateEntry(RoomTypeDatabase.EncounterBattleTypeId);
            RoomTypeDatabase.RoomTypeEntry chest = database.GetOrCreateEntry(RoomTypeDatabase.ChestTypeId);
            bool changed = false;
            if (encounterBattle != null && !string.Equals(encounterBattle.displayName, RoomTypeDatabase.EncounterBattleTypeName, System.StringComparison.Ordinal))
            {
                encounterBattle.displayName = RoomTypeDatabase.EncounterBattleTypeName;
                changed = true;
            }

            if (chest != null && !string.Equals(chest.displayName, RoomTypeDatabase.ChestTypeName, System.StringComparison.Ordinal))
            {
                chest.displayName = RoomTypeDatabase.ChestTypeName;
                changed = true;
            }

            if (changed)
            {
                SaveAsset(database);
            }

            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<RoomTypeDatabase>();
        RoomTypeDatabase.RoomTypeEntry created = database.GetOrCreateEntry(RoomTypeDatabase.EncounterBattleTypeId);
        if (created != null)
        {
            created.displayName = RoomTypeDatabase.EncounterBattleTypeName;
        }

        RoomTypeDatabase.RoomTypeEntry createdChest = database.GetOrCreateEntry(RoomTypeDatabase.ChestTypeId);
        if (createdChest != null)
        {
            createdChest.displayName = RoomTypeDatabase.ChestTypeName;
        }

        AssetDatabase.CreateAsset(database, RoomTypeAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static CharacterStatDatabase EnsureStatDatabase()
    {
        CharacterStatDatabase database = AssetDatabase.LoadAssetAtPath<CharacterStatDatabase>(StatAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<CharacterStatDatabase>();
        AssetDatabase.CreateAsset(database, StatAssetPath);
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
