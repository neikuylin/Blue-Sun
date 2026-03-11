using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class RoomEnemyDebugWindow : EditorWindow
{
    private const string StatAssetFolder = "Assets/Resources";
    private const string StatAssetPath = StatAssetFolder + "/CharacterStatDatabase.asset";
    private const string DefaultEnemyId = "\u5047\u4EBA";

    private Vector2 scroll;
    private string newEnemyId = "NewEnemy";

    [MenuItem("Tools/战斗/房间敌人调试器")]
    private static void Open()
    {
        RoomEnemyDebugWindow window = GetWindow<RoomEnemyDebugWindow>("房间敌人");
        window.minSize = new Vector2(720f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleBootstrap bootstrap = FindBootstrapInScene();
        CharacterStatDatabase statDatabase = EnsureStatDatabase();

        EditorGUILayout.LabelField("当前房间敌人调试器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "编辑当前场景 BattleBootstrap 的房间敌人列表。新增敌人时，会同时确保角色属性库中存在同 ID 条目。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }

            using (new EditorGUI.DisabledScope(bootstrap == null))
            {
                if (GUILayout.Button("保存场景"))
                {
                    SaveScene(bootstrap);
                }
            }
        }

        if (bootstrap == null)
        {
            EditorGUILayout.HelpBox("当前打开场景没有找到 BattleBootstrap。", MessageType.Warning);
            return;
        }

        if (statDatabase == null)
        {
            EditorGUILayout.HelpBox("加载或创建 CharacterStatDatabase 失败。", MessageType.Error);
            return;
        }

        DrawAddPanel(bootstrap, statDatabase);
        EditorGUILayout.Space(8f);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawEnemyList(bootstrap, statDatabase);
        EditorGUILayout.EndScrollView();
    }

    private void DrawAddPanel(BattleBootstrap bootstrap, CharacterStatDatabase statDatabase)
    {
        EditorGUILayout.LabelField("新增敌人", EditorStyles.boldLabel);
        newEnemyId = EditorGUILayout.TextField("创建ID", newEnemyId);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEnemyId)))
        {
            if (GUILayout.Button("添加到房间"))
            {
                AddEnemyEntry(bootstrap, statDatabase, newEnemyId.Trim());
                newEnemyId = string.Empty;
            }
        }
    }

    private void DrawEnemyList(BattleBootstrap bootstrap, CharacterStatDatabase statDatabase)
    {
        SerializedObject bootstrapObject = new SerializedObject(bootstrap);
        SerializedProperty enemySpawns = bootstrapObject.FindProperty("enemySpawns");

        if (enemySpawns == null)
        {
            EditorGUILayout.HelpBox("BattleBootstrap.enemySpawns 序列化失败。", MessageType.Error);
            return;
        }

        if (enemySpawns.arraySize == 0)
        {
            EditorGUILayout.HelpBox("当前房间敌人列表为空。运行时会回退生成默认敌人。", MessageType.Info);
            if (GUILayout.Button("导入默认敌人"))
            {
                AddEnemyEntry(bootstrap, statDatabase, DefaultEnemyId);
            }

            return;
        }

        for (int i = 0; i < enemySpawns.arraySize; i++)
        {
            SerializedProperty entry = enemySpawns.GetArrayElementAtIndex(i);
            SerializedProperty enemyIdProperty = entry.FindPropertyRelative("enemyId");
            string enemyId = enemyIdProperty.stringValue;
            CharacterStatDatabase.StatEntry statEntry = EnsureStatEntry(statDatabase, enemyId);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(enemyId) ? $"敌人 {i + 1}" : enemyId, EditorStyles.boldLabel);
                    if (GUILayout.Button("删除", GUILayout.Width(72f)))
                    {
                        enemySpawns.DeleteArrayElementAtIndex(i);
                        bootstrapObject.ApplyModifiedProperties();
                        MarkBootstrapDirty(bootstrap);
                        return;
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enemyIdProperty, new GUIContent("敌人ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("spawnCell"), new GUIContent("出生格"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("team"), new GUIContent("阵营"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("isPlayerControlled"), new GUIContent("玩家操控"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxHealth"), new GUIContent("生命值"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("moveRange"), new GUIContent("移动范围"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("attackRange"), new GUIContent("攻击范围"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("attackDamage"), new GUIContent("攻击伤害"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("footprintSize"), new GUIContent("占地尺寸"));
                bool enemyChanged = EditorGUI.EndChangeCheck();

                string resolvedEnemyId = enemyIdProperty.stringValue.Trim();
                if (!string.IsNullOrWhiteSpace(resolvedEnemyId))
                {
                    statEntry = EnsureStatEntry(statDatabase, resolvedEnemyId);
                    DrawStatEditor(statDatabase, statEntry);
                }
                else
                {
                    EditorGUILayout.HelpBox("请先填写敌人ID，属性才能绑定到同 ID 条目。", MessageType.Warning);
                }

                if (enemyChanged)
                {
                    bootstrapObject.ApplyModifiedProperties();
                    MarkBootstrapDirty(bootstrap);
                }
            }
        }

        if (bootstrapObject.ApplyModifiedProperties())
        {
            MarkBootstrapDirty(bootstrap);
        }
    }

    private void DrawStatEditor(CharacterStatDatabase statDatabase, CharacterStatDatabase.StatEntry statEntry)
    {
        if (statEntry == null)
        {
            EditorGUILayout.HelpBox("加载或创建属性条目失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int strength = EditorGUILayout.IntField("力量", statEntry.strength);
        int agility = EditorGUILayout.IntField("敏捷", statEntry.agility);
        int intelligence = EditorGUILayout.IntField("智力", statEntry.intelligence);
        int endurance = EditorGUILayout.IntField("耐力", statEntry.endurance);
        bool statChanged = EditorGUI.EndChangeCheck();

        if (!statChanged)
        {
            return;
        }

        statEntry.strength = strength;
        statEntry.agility = agility;
        statEntry.intelligence = intelligence;
        statEntry.endurance = endurance;
        EditorUtility.SetDirty(statDatabase);
        AssetDatabase.SaveAssets();
    }

    private void AddEnemyEntry(BattleBootstrap bootstrap, CharacterStatDatabase statDatabase, string enemyId)
    {
        if (bootstrap == null || string.IsNullOrWhiteSpace(enemyId))
        {
            return;
        }

        Undo.RecordObject(bootstrap, "新增房间敌人");

        if (bootstrap.enemySpawns == null)
        {
            bootstrap.enemySpawns = new System.Collections.Generic.List<BattleBootstrap.EnemySpawnEntry>();
        }

        BattleBootstrap.EnemySpawnEntry entry = new BattleBootstrap.EnemySpawnEntry
        {
            enemyId = enemyId,
            spawnCell = CalculateNextSpawnCell(bootstrap),
            team = BattleTeam.Enemy,
            isPlayerControlled = false,
            maxHealth = 12,
            moveRange = 3,
            attackRange = 1,
            attackDamage = 2,
            footprintSize = 3
        };

        bootstrap.enemySpawns.Add(entry);
        EnsureStatEntry(statDatabase, enemyId);
        MarkBootstrapDirty(bootstrap);
        Repaint();
    }

    private static Vector2Int CalculateNextSpawnCell(BattleBootstrap bootstrap)
    {
        Vector2Int baseCell = bootstrap != null ? bootstrap.enemySpawnCell : new Vector2Int(13, 12);
        if (bootstrap == null || bootstrap.enemySpawns == null || bootstrap.enemySpawns.Count == 0)
        {
            return baseCell;
        }

        int offset = bootstrap.enemySpawns.Count;
        return baseCell + new Vector2Int(offset * 2, 0);
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
            characterId = enemyId
        };
        statDatabase.Entries.Add(created);
        EditorUtility.SetDirty(statDatabase);
        AssetDatabase.SaveAssets();
        return created;
    }

    private static CharacterStatDatabase EnsureStatDatabase()
    {
        CharacterStatDatabase database = AssetDatabase.LoadAssetAtPath<CharacterStatDatabase>(StatAssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(StatAssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<CharacterStatDatabase>();
        AssetDatabase.CreateAsset(database, StatAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static BattleBootstrap FindBootstrapInScene()
    {
        return FindObjectOfType<BattleBootstrap>(true);
    }

    private static void MarkBootstrapDirty(BattleBootstrap bootstrap)
    {
        if (bootstrap == null)
        {
            return;
        }

        EditorUtility.SetDirty(bootstrap);
        SaveScene(bootstrap);
    }

    private static void SaveScene(BattleBootstrap bootstrap)
    {
        if (bootstrap == null)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
}
