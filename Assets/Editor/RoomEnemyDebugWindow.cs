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

    [MenuItem("Tools/\u6218\u6597/\u623F\u95F4\u654C\u4EBA\u8C03\u8BD5\u5668")]
    private static void Open()
    {
        RoomEnemyDebugWindow window = GetWindow<RoomEnemyDebugWindow>("\u623F\u95F4\u654C\u4EBA");
        window.minSize = new Vector2(720f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleBootstrap bootstrap = FindBootstrapInScene();
        CharacterStatDatabase statDatabase = EnsureStatDatabase();

        EditorGUILayout.LabelField("\u5F53\u524D\u623F\u95F4\u654C\u4EBA\u8C03\u8BD5\u5668", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u7F16\u8F91\u5F53\u524D\u573A\u666F BattleBootstrap \u7684\u623F\u95F4\u654C\u4EBA\u5217\u8868\u3002\u65B0\u589E\u654C\u4EBA\u65F6\uFF0C\u4F1A\u540C\u65F6\u786E\u4FDD\u89D2\u8272\u5C5E\u6027\u5E93\u4E2D\u5B58\u5728\u540C ID \u6761\u76EE\u3002",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65B0"))
            {
                Repaint();
            }

            using (new EditorGUI.DisabledScope(bootstrap == null))
            {
                if (GUILayout.Button("\u4FDD\u5B58\u573A\u666F"))
                {
                    SaveScene(bootstrap);
                }
            }
        }

        if (bootstrap == null)
        {
            EditorGUILayout.HelpBox("\u5F53\u524D\u6253\u5F00\u573A\u666F\u6CA1\u6709\u627E\u5230 BattleBootstrap\u3002", MessageType.Warning);
            return;
        }

        if (statDatabase == null)
        {
            EditorGUILayout.HelpBox("\u52A0\u8F7D\u6216\u521B\u5EFA CharacterStatDatabase \u5931\u8D25\u3002", MessageType.Error);
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
        EditorGUILayout.LabelField("\u65B0\u589E\u654C\u4EBA", EditorStyles.boldLabel);
        newEnemyId = EditorGUILayout.TextField("\u521B\u5EFAID", newEnemyId);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEnemyId)))
        {
            if (GUILayout.Button("\u6DFB\u52A0\u5230\u623F\u95F4"))
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
            EditorGUILayout.HelpBox("BattleBootstrap.enemySpawns \u5E8F\u5217\u5316\u5931\u8D25\u3002", MessageType.Error);
            return;
        }

        if (enemySpawns.arraySize == 0)
        {
            EditorGUILayout.HelpBox("\u5F53\u524D\u623F\u95F4\u654C\u4EBA\u5217\u8868\u4E3A\u7A7A\u3002\u8FD0\u884C\u65F6\u4F1A\u56DE\u9000\u751F\u6210\u9ED8\u8BA4\u654C\u4EBA\u3002", MessageType.Info);
            if (GUILayout.Button("\u5BFC\u5165\u9ED8\u8BA4\u654C\u4EBA"))
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
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(enemyId) ? $"\u654C\u4EBA {i + 1}" : enemyId, EditorStyles.boldLabel);
                    if (GUILayout.Button("\u5220\u9664", GUILayout.Width(72f)))
                    {
                        enemySpawns.DeleteArrayElementAtIndex(i);
                        bootstrapObject.ApplyModifiedProperties();
                        MarkBootstrapDirty(bootstrap);
                        return;
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enemyIdProperty, new GUIContent("\u654C\u4EBAID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("spawnCell"), new GUIContent("\u51FA\u751F\u683C"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("team"), new GUIContent("\u9635\u8425"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("isPlayerControlled"), new GUIContent("\u73A9\u5BB6\u64CD\u63A7"));
                bool enemyChanged = EditorGUI.EndChangeCheck();

                string resolvedEnemyId = enemyIdProperty.stringValue.Trim();
                if (!string.IsNullOrWhiteSpace(resolvedEnemyId))
                {
                    statEntry = EnsureStatEntry(statDatabase, resolvedEnemyId);
                    DrawStatEditor(statDatabase, statEntry);
                }
                else
                {
                    EditorGUILayout.HelpBox("\u8BF7\u5148\u586B\u5199\u654C\u4EBAID\uFF0C\u5C5E\u6027\u624D\u80FD\u7ED1\u5B9A\u5230\u5BF9\u5E94 ID \u6761\u76EE\u3002", MessageType.Warning);
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
            EditorGUILayout.HelpBox("\u52A0\u8F7D\u6216\u521B\u5EFA\u5C5E\u6027\u6761\u76EE\u5931\u8D25\u3002", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("\u5C5E\u6027", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int strength = EditorGUILayout.IntField("\u529B\u91CF", statEntry.strength);
        int agility = EditorGUILayout.IntField("\u654F\u6377", statEntry.agility);
        int intelligence = EditorGUILayout.IntField("\u667A\u529B", statEntry.intelligence);
        int endurance = EditorGUILayout.IntField("\u8010\u529B", statEntry.endurance);
        int actionPoints = EditorGUILayout.IntField("\u884C\u52A8\u529B", statEntry.actionPoints);
        EditorGUILayout.LabelField("\u79FB\u52A8\u8DDD\u79BB", Mathf.Max(0, agility + 3).ToString());
        bool statChanged = EditorGUI.EndChangeCheck();

        if (!statChanged)
        {
            return;
        }

        statEntry.strength = strength;
        statEntry.agility = agility;
        statEntry.intelligence = intelligence;
        statEntry.endurance = endurance;
        statEntry.actionPoints = actionPoints;
        EditorUtility.SetDirty(statDatabase);
        AssetDatabase.SaveAssets();
    }

    private void AddEnemyEntry(BattleBootstrap bootstrap, CharacterStatDatabase statDatabase, string enemyId)
    {
        if (bootstrap == null || string.IsNullOrWhiteSpace(enemyId))
        {
            return;
        }

        Undo.RecordObject(bootstrap, "\u65B0\u589E\u623F\u95F4\u654C\u4EBA");

        if (bootstrap.enemySpawns == null)
        {
            bootstrap.enemySpawns = new System.Collections.Generic.List<BattleBootstrap.EnemySpawnEntry>();
        }

        BattleBootstrap.EnemySpawnEntry entry = new BattleBootstrap.EnemySpawnEntry
        {
            enemyId = enemyId,
            spawnCell = CalculateNextSpawnCell(bootstrap),
            team = BattleTeam.Enemy,
            isPlayerControlled = false
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
            characterId = enemyId,
            actionPoints = 4
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
