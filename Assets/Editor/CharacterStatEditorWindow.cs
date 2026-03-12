using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CharacterStatEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/CharacterStatDatabase.asset";

    private Vector2 scroll;
    private SerializedObject databaseObject;

    [MenuItem("Tools/角色属性/属性编辑器")]
    private static void Open()
    {
        CharacterStatEditorWindow window = GetWindow<CharacterStatEditorWindow>("角色属性");
        window.minSize = new Vector2(560f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        CharacterStatDatabase database = EnsureDatabase();

        EditorGUILayout.LabelField("角色ID属性绑定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            Application.isPlaying
                ? "当前为 Play Mode。修改会同时写入资产，并同步到当前战斗中的同 ID 单位。"
                : "当前只修改资产。进入 Play Mode 后可实时同步到战斗单位。",
            MessageType.Info);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }

            if (GUILayout.Button("同步已知ID"))
            {
                SyncKnownIds(database);
            }
        }

        EditorGUILayout.Space(6f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawTable(database);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTable(CharacterStatDatabase database)
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("属性库资产创建失败。", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();
        SerializedProperty entries = databaseObject.FindProperty("entries");
        EnsureKnownIdsInProperty(entries);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty id = entry.FindPropertyRelative("characterId");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(id, new GUIContent("角色ID"));
                    if (GUILayout.Button("删除", GUILayout.Width(60f)))
                    {
                        entries.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("strength"), new GUIContent("力量"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("agility"), new GUIContent("敏捷"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("intelligence"), new GUIContent("智力"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("endurance"), new GUIContent("耐力"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("actionPoints"), new GUIContent("行动力"));
                int resolvedMoveDistance = Mathf.Max(0, entry.FindPropertyRelative("agility").intValue + 3);
                EditorGUILayout.LabelField("移动距离", resolvedMoveDistance.ToString());
                bool changed = EditorGUI.EndChangeCheck();

                if (Application.isPlaying)
                {
                    DrawRuntimeSyncInfo(entry);
                }

                if (changed)
                {
                    databaseObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();

                    if (Application.isPlaying)
                    {
                        ApplyEntryToRuntime(entry);
                    }

                    GUI.changed = false;
                }
            }
        }

        if (GUILayout.Button("新增空属性"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            ResetEntry(entries.GetArrayElementAtIndex(entries.arraySize - 1), string.Empty);
        }

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static void DrawRuntimeSyncInfo(SerializedProperty entry)
    {
        string characterId = entry.FindPropertyRelative("characterId").stringValue;
        BattleUnit[] units = FindBattleUnits(characterId);
        string text = units.Length > 0
            ? "战斗内已检测到同 ID 单位，修改会立即生效。"
            : "战斗内未检测到同 ID 单位，当前只会修改资产。";
        EditorGUILayout.HelpBox(text, units.Length > 0 ? MessageType.None : MessageType.Warning);
    }

    private static void ApplyEntryToRuntime(SerializedProperty entry)
    {
        string characterId = entry.FindPropertyRelative("characterId").stringValue;
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        BattleUnit[] units = FindBattleUnits(characterId);
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            unit.strength = entry.FindPropertyRelative("strength").intValue;
            unit.SetAgility(entry.FindPropertyRelative("agility").intValue);
            unit.intelligence = entry.FindPropertyRelative("intelligence").intValue;
            unit.endurance = entry.FindPropertyRelative("endurance").intValue;
            int agility = entry.FindPropertyRelative("agility").intValue;
            int actionPoints = entry.FindPropertyRelative("actionPoints").intValue;
            unit.maxActionPoints = actionPoints > 0 ? actionPoints : 4;
            unit.moveDistance = Mathf.Max(0, agility + 3);
            unit.moveRange = unit.moveDistance;
            unit.currentActionPoints = Mathf.Min(unit.currentActionPoints, unit.maxActionPoints);
            EditorUtility.SetDirty(unit);
        }
    }

    private static BattleUnit[] FindBattleUnits(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return Array.Empty<BattleUnit>();
        }

        BattleUnit[] allUnits = UnityEngine.Object.FindObjectsOfType<BattleUnit>(true);
        List<BattleUnit> matches = new List<BattleUnit>();
        for (int i = 0; i < allUnits.Length; i++)
        {
            BattleUnit unit = allUnits[i];
            if (unit == null)
            {
                continue;
            }

            if (string.Equals(unit.characterId, characterId, StringComparison.Ordinal))
            {
                matches.Add(unit);
            }
        }

        return matches.ToArray();
    }

    private static CharacterStatDatabase EnsureDatabase()
    {
        CharacterStatDatabase database = AssetDatabase.LoadAssetAtPath<CharacterStatDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<CharacterStatDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void SyncKnownIds(CharacterStatDatabase database)
    {
        if (database == null)
        {
            return;
        }

        List<string> knownIds = CollectKnownIds(database);
        bool changed = false;

        for (int i = 0; i < knownIds.Count; i++)
        {
            if (database.FindEntry(knownIds[i]) != null)
            {
                continue;
            }

            database.Entries.Add(new CharacterStatDatabase.StatEntry
            {
                characterId = knownIds[i],
                actionPoints = 4
            });
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureKnownIdsInProperty(SerializedProperty entries)
    {
        List<string> knownIds = CollectKnownIds(null);
        for (int i = 0; i < knownIds.Count; i++)
        {
            if (ContainsCharacterId(entries, knownIds[i]))
            {
                continue;
            }

            entries.InsertArrayElementAtIndex(entries.arraySize);
            ResetEntry(entries.GetArrayElementAtIndex(entries.arraySize - 1), knownIds[i]);
        }
    }

    private static bool ContainsCharacterId(SerializedProperty entries, string characterId)
    {
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            if (string.Equals(entry.FindPropertyRelative("characterId").stringValue, characterId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ResetEntry(SerializedProperty entry, string characterId)
    {
        entry.FindPropertyRelative("characterId").stringValue = characterId;
        entry.FindPropertyRelative("strength").intValue = 0;
        entry.FindPropertyRelative("agility").intValue = 0;
        entry.FindPropertyRelative("intelligence").intValue = 0;
        entry.FindPropertyRelative("endurance").intValue = 0;
        entry.FindPropertyRelative("actionPoints").intValue = 4;
        entry.FindPropertyRelative("moveDistance").intValue = 0;
    }

    private static List<string> CollectKnownIds(CharacterStatDatabase database)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        ids.Add("玩家");

        CharacterSelectEntry[] entries = UnityEngine.Object.FindObjectsOfType<CharacterSelectEntry>(true);
        for (int i = 0; i < entries.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(entries[i].characterId))
            {
                ids.Add(entries[i].characterId);
            }
        }

        CharacterSlotView[] slots = UnityEngine.Object.FindObjectsOfType<CharacterSlotView>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(slots[i].slotCharacterId))
            {
                ids.Add(slots[i].slotCharacterId);
            }
        }

        BattleUnit[] battleUnits = UnityEngine.Object.FindObjectsOfType<BattleUnit>(true);
        for (int i = 0; i < battleUnits.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(battleUnits[i].characterId))
            {
                ids.Add(battleUnits[i].characterId);
            }
        }

        if (database != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                CharacterStatDatabase.StatEntry entry = database.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.characterId))
                {
                    ids.Add(entry.characterId);
                }
            }
        }

        List<string> result = new List<string>(ids);
        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
