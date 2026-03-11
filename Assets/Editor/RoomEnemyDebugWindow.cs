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

    [MenuItem("Tools/Battle/Room Enemy Debugger")]
    private static void Open()
    {
        RoomEnemyDebugWindow window = GetWindow<RoomEnemyDebugWindow>("Room Enemies");
        window.minSize = new Vector2(720f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleBootstrap bootstrap = FindBootstrapInScene();
        CharacterStatDatabase statDatabase = EnsureStatDatabase();

        EditorGUILayout.LabelField("Current Room Enemy Debugger", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Edit the current scene BattleBootstrap room-enemy list. Adding an enemy also ensures a matching stat entry exists for the same ID.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh"))
            {
                Repaint();
            }

            using (new EditorGUI.DisabledScope(bootstrap == null))
            {
                if (GUILayout.Button("Save Scene"))
                {
                    SaveScene(bootstrap);
                }
            }
        }

        if (bootstrap == null)
        {
            EditorGUILayout.HelpBox("No BattleBootstrap was found in the currently open scene.", MessageType.Warning);
            return;
        }

        if (statDatabase == null)
        {
            EditorGUILayout.HelpBox("Failed to create or load CharacterStatDatabase.", MessageType.Error);
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
        EditorGUILayout.LabelField("Add Enemy", EditorStyles.boldLabel);
        newEnemyId = EditorGUILayout.TextField("Create ID", newEnemyId);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newEnemyId)))
        {
            if (GUILayout.Button("Add To Room"))
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
            EditorGUILayout.HelpBox("BattleBootstrap.enemySpawns failed to serialize.", MessageType.Error);
            return;
        }

        if (enemySpawns.arraySize == 0)
        {
            EditorGUILayout.HelpBox("The room enemy list is empty. Runtime will fall back to the default enemy.", MessageType.Info);
            if (GUILayout.Button("Import Default Enemy"))
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
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(enemyId) ? $"Enemy {i + 1}" : enemyId, EditorStyles.boldLabel);
                    if (GUILayout.Button("Delete", GUILayout.Width(72f)))
                    {
                        enemySpawns.DeleteArrayElementAtIndex(i);
                        bootstrapObject.ApplyModifiedProperties();
                        MarkBootstrapDirty(bootstrap);
                        return;
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enemyIdProperty, new GUIContent("Enemy ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("spawnCell"), new GUIContent("Spawn Cell"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("team"), new GUIContent("Team"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("isPlayerControlled"), new GUIContent("Player Controlled"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxHealth"), new GUIContent("Health"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("moveRange"), new GUIContent("Move Range"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("attackRange"), new GUIContent("Attack Range"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("attackDamage"), new GUIContent("Attack Damage"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("footprintSize"), new GUIContent("Footprint Size"));
                bool enemyChanged = EditorGUI.EndChangeCheck();

                string resolvedEnemyId = enemyIdProperty.stringValue.Trim();
                if (!string.IsNullOrWhiteSpace(resolvedEnemyId))
                {
                    statEntry = EnsureStatEntry(statDatabase, resolvedEnemyId);
                    DrawStatEditor(statDatabase, statEntry);
                }
                else
                {
                    EditorGUILayout.HelpBox("Fill in Enemy ID first so the stats can bind to the same ID entry.", MessageType.Warning);
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
            EditorGUILayout.HelpBox("Failed to create or load a stat entry.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int strength = EditorGUILayout.IntField("Strength", statEntry.strength);
        int agility = EditorGUILayout.IntField("Agility", statEntry.agility);
        int intelligence = EditorGUILayout.IntField("Intelligence", statEntry.intelligence);
        int endurance = EditorGUILayout.IntField("Endurance", statEntry.endurance);
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

        Undo.RecordObject(bootstrap, "Add Room Enemy");

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
