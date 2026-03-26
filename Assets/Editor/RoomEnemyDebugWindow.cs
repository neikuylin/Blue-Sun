using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class RoomEnemyDebugWindow : EditorWindow
{
    private const int EnemyFootprintSize = 3;
    private const string StatAssetFolder = "Assets/Resources";
    private const string StatAssetPath = StatAssetFolder + "/CharacterStatDatabase.asset";
    private const string DefaultEnemyId = "\u5047\u4EBA";

    private Vector2 scroll;
    private string newEnemyId = "NewEnemy";
    private static bool resistanceFoldout = true;
    private static bool resistancePenetrationFoldout = true;

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
        string overlapMessage = BuildOverlapMessage(bootstrap);

        EditorGUILayout.LabelField("\u5F53\u524D\u623F\u95F4\u654C\u4EBA\u8C03\u8BD5\u5668", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u7F16\u8F91\u5F53\u524D\u573A\u666F BattleBootstrap \u7684\u623F\u95F4\u654C\u4EBA\u5217\u8868\u3002\u65B0\u589E\u654C\u4EBA\u65F6\uFF0C\u4F1A\u540C\u65F6\u786E\u4FDD\u89D2\u8272\u5C5E\u6027\u5E93\u4E2D\u5B58\u5728\u540C ID \u6761\u76EE\u3002",
            MessageType.Info);

        if (!string.IsNullOrEmpty(overlapMessage))
        {
            EditorGUILayout.HelpBox(overlapMessage, MessageType.Error);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65B0"))
            {
                Repaint();
            }

            using (new EditorGUI.DisabledScope(bootstrap == null || !CanSaveScene() || !string.IsNullOrEmpty(overlapMessage)))
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
                        Vector2Int spawnCell = entry.FindPropertyRelative("spawnCell").vector2IntValue;
                        BattleTeam team = (BattleTeam)entry.FindPropertyRelative("team").enumValueIndex;
                        bool isPlayerControlled = entry.FindPropertyRelative("isPlayerControlled").boolValue;
                        RemoveRuntimeEnemy(enemyId, spawnCell, team, isPlayerControlled);
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
        int actionPoints = EditorGUILayout.IntField("\u884C\u52A8\u529B", statEntry.actionPoints);
        int hitRate = EditorGUILayout.IntField("\u547D\u4E2D", statEntry.ResolveHitRate());
        resistanceFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resistanceFoldout, "\u6297\u6027");
        int physicalResistance = statEntry.ResolvePhysicalResistance();
        int fireResistance = statEntry.ResolveFireResistance();
        int corruptionResistance = statEntry.ResolveCorruptionResistance();
        int coldResistance = statEntry.ResolveColdResistance();
        if (resistanceFoldout)
        {
            physicalResistance = EditorGUILayout.IntField("\u7269\u7406\u6297\u6027", physicalResistance);
            fireResistance = EditorGUILayout.IntField("\u706B\u7130\u6297\u6027", fireResistance);
            corruptionResistance = EditorGUILayout.IntField("\u8150\u8D25\u6297\u6027", corruptionResistance);
            coldResistance = EditorGUILayout.IntField("\u5BD2\u51B7\u6297\u6027", coldResistance);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        resistancePenetrationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resistancePenetrationFoldout, "\u6297\u6027\u7A7F\u900F");
        int physicalResistancePenetration = statEntry.ResolvePhysicalResistancePenetration();
        int fireResistancePenetration = statEntry.ResolveFireResistancePenetration();
        int corruptionResistancePenetration = statEntry.ResolveCorruptionResistancePenetration();
        int coldResistancePenetration = statEntry.ResolveColdResistancePenetration();
        if (resistancePenetrationFoldout)
        {
            physicalResistancePenetration = EditorGUILayout.IntField("\u7269\u7406\u6297\u6027\u7A7F\u900F", physicalResistancePenetration);
            fireResistancePenetration = EditorGUILayout.IntField("\u706B\u7130\u6297\u6027\u7A7F\u900F", fireResistancePenetration);
            corruptionResistancePenetration = EditorGUILayout.IntField("\u8150\u8D25\u6297\u6027\u7A7F\u900F", corruptionResistancePenetration);
            coldResistancePenetration = EditorGUILayout.IntField("\u5BD2\u51B7\u6297\u6027\u7A7F\u900F", coldResistancePenetration);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        int criticalChance = EditorGUILayout.IntField("\u66B4\u51FB\u7387", statEntry.ResolveCriticalChance());
        int criticalDamage = EditorGUILayout.IntField("\u66B4\u51FB\u4F24\u5BB3", statEntry.ResolveCriticalDamage());
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("\u6700\u7EC8\u547D\u4E2D", CharacterStatDatabase.ResolveHitRateValue(hitRate) + "%");
        EditorGUILayout.IntField("HP", CharacterStatDatabase.ResolveMaxHealthFromStrength(strength));
        EditorGUILayout.IntField("MP", CharacterStatDatabase.ResolveMaxManaFromIntelligence(intelligence));
        EditorGUILayout.LabelField("\u79FB\u52A8\u8DDD\u79BB", CharacterStatDatabase.ResolveMoveDistanceFromAgility(agility).ToString());
        EditorGUILayout.LabelField("\u95EA\u907F", CharacterStatDatabase.ResolveDodgeRateFromAgility(agility) + "%");
        EditorGUILayout.LabelField("\u7269\u7406\u6297\u6027", CharacterStatDatabase.ResolveResistanceValue(physicalResistance) + "%");
        EditorGUILayout.LabelField("\u706B\u7130\u6297\u6027", CharacterStatDatabase.ResolveResistanceValue(fireResistance) + "%");
        EditorGUILayout.LabelField("\u8150\u8D25\u6297\u6027", CharacterStatDatabase.ResolveResistanceValue(corruptionResistance) + "%");
        EditorGUILayout.LabelField("\u5BD2\u51B7\u6297\u6027", CharacterStatDatabase.ResolveResistanceValue(coldResistance) + "%");
        EditorGUILayout.LabelField("\u7269\u7406\u6297\u6027\u7A7F\u900F", CharacterStatDatabase.ResolveResistancePenetrationValue(physicalResistancePenetration) + "%");
        EditorGUILayout.LabelField("\u706B\u7130\u6297\u6027\u7A7F\u900F", CharacterStatDatabase.ResolveResistancePenetrationValue(fireResistancePenetration) + "%");
        EditorGUILayout.LabelField("\u8150\u8D25\u6297\u6027\u7A7F\u900F", CharacterStatDatabase.ResolveResistancePenetrationValue(corruptionResistancePenetration) + "%");
        EditorGUILayout.LabelField("\u5BD2\u51B7\u6297\u6027\u7A7F\u900F", CharacterStatDatabase.ResolveResistancePenetrationValue(coldResistancePenetration) + "%");
        EditorGUILayout.LabelField("\u66B4\u51FB\u7387", CharacterStatDatabase.ResolveCriticalChanceValue(criticalChance) + "%");
        EditorGUILayout.LabelField("\u66B4\u51FB\u4F24\u5BB3", CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamage) + "%");
        EditorGUI.EndDisabledGroup();
        bool statChanged = EditorGUI.EndChangeCheck();

        if (!statChanged)
        {
            return;
        }

        statEntry.strength = strength;
        statEntry.agility = agility;
        statEntry.intelligence = intelligence;
        statEntry.actionPoints = actionPoints;
        statEntry.hitRate = CharacterStatDatabase.ResolveHitRateValue(hitRate);
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
            actionPoints = 4,
            hitRate = 100
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

    private static void RemoveRuntimeEnemy(string enemyId, Vector2Int spawnCell, BattleTeam team, bool isPlayerControlled)
    {
        if (!Application.isPlaying || string.IsNullOrWhiteSpace(enemyId))
        {
            return;
        }

        BattleUnit[] runtimeUnits = Object.FindObjectsOfType<BattleUnit>(true);
        BattleUnit bestMatch = null;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < runtimeUnits.Length; i++)
        {
            BattleUnit unit = runtimeUnits[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            if (!string.Equals(unit.characterId, enemyId, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (unit.team != team || unit.isPlayerControlled != isPlayerControlled)
            {
                continue;
            }

            int distance = Mathf.Abs(unit.currentCell.x - spawnCell.x) + Mathf.Abs(unit.currentCell.y - spawnCell.y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = unit;
                if (distance == 0)
                {
                    break;
                }
            }
        }

        if (bestMatch == null)
        {
            return;
        }

        BattleTurnSystem turnSystem = Object.FindObjectOfType<BattleTurnSystem>(true);
        if (turnSystem != null)
        {
            turnSystem.RemoveUnitFromBattle(bestMatch);
            return;
        }

        bestMatch.gameObject.SetActive(false);
    }

    private static void MarkBootstrapDirty(BattleBootstrap bootstrap)
    {
        if (bootstrap == null)
        {
            return;
        }

        EditorUtility.SetDirty(bootstrap);
        if (!CanSaveScene())
        {
            return;
        }

        string overlapMessage = BuildOverlapMessage(bootstrap);
        if (!string.IsNullOrEmpty(overlapMessage))
        {
            Debug.LogError("RoomEnemyDebugWindow: " + overlapMessage);
            return;
        }

        SaveScene(bootstrap);
    }

    private static void SaveScene(BattleBootstrap bootstrap)
    {
        if (bootstrap == null)
        {
            return;
        }

        if (!CanSaveScene())
        {
            return;
        }

        string overlapMessage = BuildOverlapMessage(bootstrap);
        if (!string.IsNullOrEmpty(overlapMessage))
        {
            Debug.LogError("RoomEnemyDebugWindow: " + overlapMessage);
            EditorUtility.DisplayDialog("出生格冲突", overlapMessage, "确定");
            return;
        }

        EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static bool CanSaveScene()
    {
        return !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static string BuildOverlapMessage(BattleBootstrap bootstrap)
    {
        if (bootstrap == null || bootstrap.enemySpawns == null || bootstrap.enemySpawns.Count <= 1)
        {
            return null;
        }

        for (int i = 0; i < bootstrap.enemySpawns.Count; i++)
        {
            BattleBootstrap.EnemySpawnEntry a = bootstrap.enemySpawns[i];
            if (a == null || string.IsNullOrWhiteSpace(a.enemyId))
            {
                continue;
            }

            for (int j = i + 1; j < bootstrap.enemySpawns.Count; j++)
            {
                BattleBootstrap.EnemySpawnEntry b = bootstrap.enemySpawns[j];
                if (b == null || string.IsNullOrWhiteSpace(b.enemyId))
                {
                    continue;
                }

                if (!FootprintsOverlap(a.spawnCell, b.spawnCell, EnemyFootprintSize))
                {
                    continue;
                }

                return $"出生格冲突：'{a.enemyId}' ({a.spawnCell.x}, {a.spawnCell.y}) 与 '{b.enemyId}' ({b.spawnCell.x}, {b.spawnCell.y}) 的 {EnemyFootprintSize}x{EnemyFootprintSize} 占地重叠。请先调整出生格，再保存场景。";
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
