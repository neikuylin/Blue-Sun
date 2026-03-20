using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class RoomEnemyPresetEditorWindow : EditorWindow
{
    private const int EnemyFootprintSize = 3;
    private const string ResourceFolder = "Assets/Resources";
    private const string PresetAssetPath = ResourceFolder + "/RoomEnemyPresetDatabase.asset";
    private const string StatAssetPath = ResourceFolder + "/CharacterStatDatabase.asset";
    private const string DefaultPresetId = "房间预设";
    private const string DefaultEnemyId = "假人";

    private Vector2 scroll;
    private string newPresetId = DefaultPresetId;

    [MenuItem("Tools/战斗/房间敌人编辑器")]
    private static void Open()
    {
        RoomEnemyPresetEditorWindow window = GetWindow<RoomEnemyPresetEditorWindow>("房间敌人编辑器");
        window.minSize = new Vector2(760f, 560f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        RoomEnemyPresetDatabase presetDatabase = EnsurePresetDatabase();
        CharacterStatDatabase statDatabase = EnsureStatDatabase();
        BattleBootstrap bootstrap = FindObjectOfType<BattleBootstrap>(true);

        EditorGUILayout.LabelField("房间敌人编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "维护房间敌人预设。这里编辑的是预设资产，不会实时修改场景；你可以从当前 BattleBootstrap 抓取一份，也可以把某套预设应用到当前场景。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }

            using (new EditorGUI.DisabledScope(bootstrap == null))
            {
                if (GUILayout.Button("从当前场景创建新预设"))
                {
                    CreatePresetFromScene(presetDatabase, bootstrap, statDatabase);
                }
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
        SerializedObject databaseObject = new SerializedObject(presetDatabase);
        databaseObject.Update();
        SerializedProperty entries = databaseObject.FindProperty("entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            DrawPresetEntry(entries.GetArrayElementAtIndex(i), presetDatabase, statDatabase, bootstrap);
        }

        if (databaseObject.ApplyModifiedProperties())
        {
            SaveAsset(presetDatabase);
        }

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
                RoomEnemyPresetDatabase.EnsureValidEnemyList(entry);
                SaveAsset(presetDatabase);
                newPresetId = string.Empty;
            }
        }
    }

    private void DrawPresetEntry(
        SerializedProperty presetProperty,
        RoomEnemyPresetDatabase presetDatabase,
        CharacterStatDatabase statDatabase,
        BattleBootstrap bootstrap)
    {
        SerializedProperty presetIdProperty = presetProperty.FindPropertyRelative("presetId");
        SerializedProperty enemiesProperty = presetProperty.FindPropertyRelative("enemies");
        string presetId = presetIdProperty.stringValue;
        string overlapMessage = BuildOverlapMessage(enemiesProperty);
        bool hasOverlap = !string.IsNullOrEmpty(overlapMessage);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(presetIdProperty, new GUIContent("预设ID"));

                if (GUILayout.Button("抓取当前场景", GUILayout.Width(100f)))
                {
                    CaptureSceneIntoPreset(presetDatabase, presetIdProperty.stringValue, bootstrap, statDatabase);
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(hasOverlap))
                {
                    if (GUILayout.Button("应用到当前场景", GUILayout.Width(112f)))
                    {
                        ApplyPresetToScene(enemiesProperty, bootstrap, statDatabase);
                    }
                }

                if (GUILayout.Button("删除预设", GUILayout.Width(88f)))
                {
                    Undo.RecordObject(presetDatabase, "删除房间敌人预设");
                    presetDatabase.RemoveEntry(presetId);
                    SaveAsset(presetDatabase);
                    GUIUtility.ExitGUI();
                }
            }

            if (hasOverlap)
            {
                EditorGUILayout.HelpBox(overlapMessage, MessageType.Error);
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

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string title = string.IsNullOrWhiteSpace(enemyIdProperty.stringValue)
                    ? $"敌人 {index + 1}"
                    : enemyIdProperty.stringValue;
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    enemiesProperty.DeleteArrayElementAtIndex(index);
                    return;
                }
            }

            EditorGUILayout.PropertyField(enemyIdProperty, new GUIContent("敌人ID"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("spawnCell"), new GUIContent("出生格"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("team"), new GUIContent("阵营"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("isPlayerControlled"), new GUIContent("玩家控制"));

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
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("HP", CharacterStatDatabase.ResolveMaxHealthFromStrength(strength));
        EditorGUILayout.IntField("MP", CharacterStatDatabase.ResolveMaxManaFromIntelligence(intelligence));
        EditorGUILayout.LabelField("移动距离", CharacterStatDatabase.ResolveMoveDistanceFromAgility(agility).ToString());
        EditorGUI.EndDisabledGroup();

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        statEntry.strength = strength;
        statEntry.agility = agility;
        statEntry.intelligence = intelligence;
        statEntry.actionPoints = actionPoints;
        SaveAsset(statDatabase);
    }

    private static void AddEnemyToPreset(SerializedProperty enemiesProperty, CharacterStatDatabase statDatabase, string enemyId = "")
    {
        enemiesProperty.InsertArrayElementAtIndex(enemiesProperty.arraySize);
        SerializedProperty added = enemiesProperty.GetArrayElementAtIndex(enemiesProperty.arraySize - 1);
        added.FindPropertyRelative("enemyId").stringValue = enemyId;
        added.FindPropertyRelative("spawnCell").vector2IntValue = new Vector2Int(13, 12 + (enemiesProperty.arraySize - 1) * 2);
        added.FindPropertyRelative("team").enumValueIndex = (int)BattleTeam.Enemy;
        added.FindPropertyRelative("isPlayerControlled").boolValue = false;

        if (!string.IsNullOrWhiteSpace(enemyId))
        {
            EnsureStatEntry(statDatabase, enemyId);
        }
    }

    private static void CreatePresetFromScene(
        RoomEnemyPresetDatabase presetDatabase,
        BattleBootstrap bootstrap,
        CharacterStatDatabase statDatabase)
    {
        if (presetDatabase == null || bootstrap == null)
        {
            return;
        }

        string presetId = BuildScenePresetId(bootstrap.gameObject.scene.name, presetDatabase);
        Undo.RecordObject(presetDatabase, "从当前场景创建房间敌人预设");
        RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = presetDatabase.GetOrCreateEntry(presetId);
        ReplacePresetEnemies(preset, bootstrap, statDatabase);
        SaveAsset(presetDatabase);
    }

    private static void CaptureSceneIntoPreset(
        RoomEnemyPresetDatabase presetDatabase,
        string presetId,
        BattleBootstrap bootstrap,
        CharacterStatDatabase statDatabase)
    {
        if (presetDatabase == null || bootstrap == null || string.IsNullOrWhiteSpace(presetId))
        {
            return;
        }

        Undo.RecordObject(presetDatabase, "抓取当前场景到房间敌人预设");
        RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = presetDatabase.GetOrCreateEntry(presetId.Trim());
        ReplacePresetEnemies(preset, bootstrap, statDatabase);
        SaveAsset(presetDatabase);
    }

    private static void ReplacePresetEnemies(
        RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset,
        BattleBootstrap bootstrap,
        CharacterStatDatabase statDatabase)
    {
        if (preset == null)
        {
            return;
        }

        RoomEnemyPresetDatabase.EnsureValidEnemyList(preset);
        preset.enemies.Clear();

        if (bootstrap == null || bootstrap.enemySpawns == null)
        {
            return;
        }

        for (int i = 0; i < bootstrap.enemySpawns.Count; i++)
        {
            BattleBootstrap.EnemySpawnEntry source = bootstrap.enemySpawns[i];
            if (source == null || string.IsNullOrWhiteSpace(source.enemyId))
            {
                continue;
            }

            preset.enemies.Add(RoomEnemyPresetDatabase.CloneEnemy(source));
            EnsureStatEntry(statDatabase, source.enemyId.Trim());
        }
    }

    private static void ApplyPresetToScene(
        SerializedProperty enemiesProperty,
        BattleBootstrap bootstrap,
        CharacterStatDatabase statDatabase)
    {
        if (bootstrap == null || enemiesProperty == null)
        {
            return;
        }

        string overlapMessage = BuildOverlapMessage(enemiesProperty);
        if (!string.IsNullOrEmpty(overlapMessage))
        {
            Debug.LogError("RoomEnemyPresetEditorWindow: " + overlapMessage);
            EditorUtility.DisplayDialog("出生格冲突", overlapMessage, "确定");
            return;
        }

        Undo.RecordObject(bootstrap, "应用房间敌人预设");
        if (bootstrap.enemySpawns == null)
        {
            bootstrap.enemySpawns = new System.Collections.Generic.List<BattleBootstrap.EnemySpawnEntry>();
        }

        bootstrap.enemySpawns.Clear();
        for (int i = 0; i < enemiesProperty.arraySize; i++)
        {
            SerializedProperty entry = enemiesProperty.GetArrayElementAtIndex(i);
            string enemyId = entry.FindPropertyRelative("enemyId").stringValue.Trim();
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                continue;
            }

            bootstrap.enemySpawns.Add(new BattleBootstrap.EnemySpawnEntry
            {
                enemyId = enemyId,
                spawnCell = entry.FindPropertyRelative("spawnCell").vector2IntValue,
                team = (BattleTeam)entry.FindPropertyRelative("team").enumValueIndex,
                isPlayerControlled = entry.FindPropertyRelative("isPlayerControlled").boolValue
            });
            EnsureStatEntry(statDatabase, enemyId);
        }

        EditorUtility.SetDirty(bootstrap);
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        }
    }

    private static string BuildScenePresetId(string sceneName, RoomEnemyPresetDatabase presetDatabase)
    {
        string baseId = string.IsNullOrWhiteSpace(sceneName) ? DefaultPresetId : sceneName + "_房间敌人";
        if (presetDatabase.FindEntry(baseId) == null)
        {
            return baseId;
        }

        int suffix = 2;
        while (presetDatabase.FindEntry(baseId + "_" + suffix) != null)
        {
            suffix++;
        }

        return baseId + "_" + suffix;
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
            actionPoints = 4
        };
        statDatabase.Entries.Add(created);
        SaveAsset(statDatabase);
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

    private static string BuildOverlapMessage(SerializedProperty enemiesProperty)
    {
        if (enemiesProperty == null || enemiesProperty.arraySize <= 1)
        {
            return null;
        }

        for (int i = 0; i < enemiesProperty.arraySize; i++)
        {
            SerializedProperty a = enemiesProperty.GetArrayElementAtIndex(i);
            string enemyA = a.FindPropertyRelative("enemyId").stringValue.Trim();
            if (string.IsNullOrWhiteSpace(enemyA))
            {
                continue;
            }

            Vector2Int cellA = a.FindPropertyRelative("spawnCell").vector2IntValue;
            for (int j = i + 1; j < enemiesProperty.arraySize; j++)
            {
                SerializedProperty b = enemiesProperty.GetArrayElementAtIndex(j);
                string enemyB = b.FindPropertyRelative("enemyId").stringValue.Trim();
                if (string.IsNullOrWhiteSpace(enemyB))
                {
                    continue;
                }

                Vector2Int cellB = b.FindPropertyRelative("spawnCell").vector2IntValue;
                if (!FootprintsOverlap(cellA, cellB, EnemyFootprintSize))
                {
                    continue;
                }

                return $"出生格冲突：'{enemyA}' ({cellA.x}, {cellA.y}) 与 '{enemyB}' ({cellB.x}, {cellB.y}) 的 {EnemyFootprintSize}x{EnemyFootprintSize} 占地重叠。请先调整出生格，再应用到场景。";
            }
        }

        return null;
    }

    private static bool FootprintsOverlap(Vector2Int a, Vector2Int b, int footprintSize)
    {
        int radius = Mathf.Max(0, footprintSize / 2);
        return Mathf.Abs(a.x - b.x) <= radius * 2 && Mathf.Abs(a.y - b.y) <= radius * 2;
    }
}
