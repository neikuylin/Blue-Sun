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
    private static bool resistanceFoldout = true;
    private static bool resistancePenetrationFoldout = true;

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
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("actionPoints"), new GUIContent("行动力"));
                SerializedProperty hitRateProperty = entry.FindPropertyRelative("hitRate");
                SerializedProperty physicalResistanceProperty = entry.FindPropertyRelative("physicalResistance");
                SerializedProperty fireResistanceProperty = entry.FindPropertyRelative("fireResistance");
                SerializedProperty corruptionResistanceProperty = entry.FindPropertyRelative("corruptionResistance");
                SerializedProperty coldResistanceProperty = entry.FindPropertyRelative("coldResistance");
                SerializedProperty physicalResistancePenetrationProperty = entry.FindPropertyRelative("physicalResistancePenetration");
                SerializedProperty fireResistancePenetrationProperty = entry.FindPropertyRelative("fireResistancePenetration");
                SerializedProperty corruptionResistancePenetrationProperty = entry.FindPropertyRelative("corruptionResistancePenetration");
                SerializedProperty coldResistancePenetrationProperty = entry.FindPropertyRelative("coldResistancePenetration");
                SerializedProperty criticalChanceProperty = entry.FindPropertyRelative("criticalChance");
                SerializedProperty criticalDamageProperty = entry.FindPropertyRelative("criticalDamage");
                if (hitRateProperty != null)
                {
                    int displayedHitRate = CharacterStatDatabase.ResolveHitRateValue(hitRateProperty.intValue);
                    int editedHitRate = EditorGUILayout.IntField("命中", displayedHitRate);
                    hitRateProperty.intValue = Mathf.Max(0, editedHitRate);
                }
                resistanceFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resistanceFoldout, "抗性");
                if (resistanceFoldout)
                {
                    if (physicalResistanceProperty != null)
                    {
                        physicalResistanceProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("物理抗性", CharacterStatDatabase.ResolveResistanceValue(physicalResistanceProperty.intValue)));
                    }
                    if (fireResistanceProperty != null)
                    {
                        fireResistanceProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("火焰抗性", CharacterStatDatabase.ResolveResistanceValue(fireResistanceProperty.intValue)));
                    }
                    if (corruptionResistanceProperty != null)
                    {
                        corruptionResistanceProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("腐败抗性", CharacterStatDatabase.ResolveResistanceValue(corruptionResistanceProperty.intValue)));
                    }
                    if (coldResistanceProperty != null)
                    {
                        coldResistanceProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("寒冷抗性", CharacterStatDatabase.ResolveResistanceValue(coldResistanceProperty.intValue)));
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                resistancePenetrationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resistancePenetrationFoldout, "抗性穿透");
                if (resistancePenetrationFoldout)
                {
                    if (physicalResistancePenetrationProperty != null)
                    {
                        physicalResistancePenetrationProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("物理抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(physicalResistancePenetrationProperty.intValue)));
                    }
                    if (fireResistancePenetrationProperty != null)
                    {
                        fireResistancePenetrationProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("火焰抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(fireResistancePenetrationProperty.intValue)));
                    }
                    if (corruptionResistancePenetrationProperty != null)
                    {
                        corruptionResistancePenetrationProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("腐败抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(corruptionResistancePenetrationProperty.intValue)));
                    }
                    if (coldResistancePenetrationProperty != null)
                    {
                        coldResistancePenetrationProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("寒冷抗性穿透", CharacterStatDatabase.ResolveResistancePenetrationValue(coldResistancePenetrationProperty.intValue)));
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                if (criticalChanceProperty != null)
                {
                    criticalChanceProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("暴击率", CharacterStatDatabase.ResolveCriticalChanceValue(criticalChanceProperty.intValue)));
                }
                if (criticalDamageProperty != null)
                {
                    criticalDamageProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("暴击伤害", CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamageProperty.intValue)));
                }
                int strength = entry.FindPropertyRelative("strength").intValue;
                int agility = entry.FindPropertyRelative("agility").intValue;
                int intelligence = entry.FindPropertyRelative("intelligence").intValue;
                int resolvedHitRate = CharacterStatDatabase.ResolveHitRateValue(hitRateProperty != null ? hitRateProperty.intValue : 100);
                int resolvedPhysicalResistance = CharacterStatDatabase.ResolveResistanceValue(physicalResistanceProperty != null ? physicalResistanceProperty.intValue : 0);
                int resolvedFireResistance = CharacterStatDatabase.ResolveResistanceValue(fireResistanceProperty != null ? fireResistanceProperty.intValue : 0);
                int resolvedCorruptionResistance = CharacterStatDatabase.ResolveResistanceValue(corruptionResistanceProperty != null ? corruptionResistanceProperty.intValue : 0);
                int resolvedColdResistance = CharacterStatDatabase.ResolveResistanceValue(coldResistanceProperty != null ? coldResistanceProperty.intValue : 0);
                int resolvedPhysicalResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(physicalResistancePenetrationProperty != null ? physicalResistancePenetrationProperty.intValue : 0);
                int resolvedFireResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(fireResistancePenetrationProperty != null ? fireResistancePenetrationProperty.intValue : 0);
                int resolvedCorruptionResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(corruptionResistancePenetrationProperty != null ? corruptionResistancePenetrationProperty.intValue : 0);
                int resolvedColdResistancePenetration = CharacterStatDatabase.ResolveResistancePenetrationValue(coldResistancePenetrationProperty != null ? coldResistancePenetrationProperty.intValue : 0);
                int resolvedCriticalChance = CharacterStatDatabase.ResolveCriticalChanceValue(criticalChanceProperty != null ? criticalChanceProperty.intValue : 20);
                int resolvedCriticalDamage = CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamageProperty != null ? criticalDamageProperty.intValue : 150);
                int resolvedMaxHealth = CharacterStatDatabase.ResolveMaxHealthFromStrength(strength);
                int resolvedMaxMana = CharacterStatDatabase.ResolveMaxManaFromIntelligence(intelligence);
                int resolvedMoveDistance = CharacterStatDatabase.ResolveMoveDistanceFromAgility(agility);
                int resolvedDodgeRate = CharacterStatDatabase.ResolveDodgeRateFromAgility(agility);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField("最终命中", resolvedHitRate + "%");
                EditorGUILayout.IntField("HP", resolvedMaxHealth);
                EditorGUILayout.IntField("MP", resolvedMaxMana);
                EditorGUILayout.LabelField("移动距离", resolvedMoveDistance.ToString());
                EditorGUILayout.LabelField("闪避", resolvedDodgeRate + "%");
                EditorGUILayout.LabelField("物理抗性", resolvedPhysicalResistance + "%");
                EditorGUILayout.LabelField("火焰抗性", resolvedFireResistance + "%");
                EditorGUILayout.LabelField("腐败抗性", resolvedCorruptionResistance + "%");
                EditorGUILayout.LabelField("寒冷抗性", resolvedColdResistance + "%");
                EditorGUILayout.LabelField("物理抗性穿透", resolvedPhysicalResistancePenetration + "%");
                EditorGUILayout.LabelField("火焰抗性穿透", resolvedFireResistancePenetration + "%");
                EditorGUILayout.LabelField("腐败抗性穿透", resolvedCorruptionResistancePenetration + "%");
                EditorGUILayout.LabelField("寒冷抗性穿透", resolvedColdResistancePenetration + "%");
                EditorGUILayout.LabelField("暴击率", resolvedCriticalChance + "%");
                EditorGUILayout.LabelField("暴击伤害", resolvedCriticalDamage + "%");
                EditorGUI.EndDisabledGroup();
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
            SerializedProperty hitRateProperty = entry.FindPropertyRelative("hitRate");
            SerializedProperty physicalResistanceProperty = entry.FindPropertyRelative("physicalResistance");
            SerializedProperty fireResistanceProperty = entry.FindPropertyRelative("fireResistance");
            SerializedProperty corruptionResistanceProperty = entry.FindPropertyRelative("corruptionResistance");
            SerializedProperty coldResistanceProperty = entry.FindPropertyRelative("coldResistance");
            SerializedProperty criticalChanceProperty = entry.FindPropertyRelative("criticalChance");
            SerializedProperty criticalDamageProperty = entry.FindPropertyRelative("criticalDamage");
            if (hitRateProperty != null)
            {
                SerializedObject unitObject = new SerializedObject(unit);
                unitObject.Update();
                SerializedProperty unitHitRate = unitObject.FindProperty("hitRate");
                if (unitHitRate != null)
                {
                    unitHitRate.intValue = CharacterStatDatabase.ResolveHitRateValue(hitRateProperty.intValue);
                    unitObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            ApplyUnitStatProperty(unit, "physicalResistance", CharacterStatDatabase.ResolveResistanceValue(physicalResistanceProperty != null ? physicalResistanceProperty.intValue : 0));
            ApplyUnitStatProperty(unit, "fireResistance", CharacterStatDatabase.ResolveResistanceValue(fireResistanceProperty != null ? fireResistanceProperty.intValue : 0));
            ApplyUnitStatProperty(unit, "corruptionResistance", CharacterStatDatabase.ResolveResistanceValue(corruptionResistanceProperty != null ? corruptionResistanceProperty.intValue : 0));
            ApplyUnitStatProperty(unit, "coldResistance", CharacterStatDatabase.ResolveResistanceValue(coldResistanceProperty != null ? coldResistanceProperty.intValue : 0));
            ApplyUnitStatProperty(unit, "physicalResistancePenetration", CharacterStatDatabase.ResolveResistancePenetrationValue(entry.FindPropertyRelative("physicalResistancePenetration") != null ? entry.FindPropertyRelative("physicalResistancePenetration").intValue : 0));
            ApplyUnitStatProperty(unit, "fireResistancePenetration", CharacterStatDatabase.ResolveResistancePenetrationValue(entry.FindPropertyRelative("fireResistancePenetration") != null ? entry.FindPropertyRelative("fireResistancePenetration").intValue : 0));
            ApplyUnitStatProperty(unit, "corruptionResistancePenetration", CharacterStatDatabase.ResolveResistancePenetrationValue(entry.FindPropertyRelative("corruptionResistancePenetration") != null ? entry.FindPropertyRelative("corruptionResistancePenetration").intValue : 0));
            ApplyUnitStatProperty(unit, "coldResistancePenetration", CharacterStatDatabase.ResolveResistancePenetrationValue(entry.FindPropertyRelative("coldResistancePenetration") != null ? entry.FindPropertyRelative("coldResistancePenetration").intValue : 0));
            ApplyUnitStatProperty(unit, "criticalChance", CharacterStatDatabase.ResolveCriticalChanceValue(criticalChanceProperty != null ? criticalChanceProperty.intValue : 20));
            ApplyUnitStatProperty(unit, "criticalDamage", CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamageProperty != null ? criticalDamageProperty.intValue : 150));
            unit.maxHealth = CharacterStatDatabase.ResolveMaxHealthFromStrength(unit.strength);
            unit.maxMana = CharacterStatDatabase.ResolveMaxManaFromIntelligence(unit.intelligence);
            unit.currentHealth = Mathf.Min(unit.currentHealth, unit.maxHealth);
            unit.currentMana = Mathf.Min(unit.currentMana, unit.maxMana);
            int agility = entry.FindPropertyRelative("agility").intValue;
            int actionPoints = entry.FindPropertyRelative("actionPoints").intValue;
            unit.maxActionPoints = actionPoints > 0 ? actionPoints : 4;
            unit.moveDistance = CharacterStatDatabase.ResolveMoveDistanceFromAgility(agility);
            unit.moveRange = unit.moveDistance;
            unit.currentActionPoints = Mathf.Min(unit.currentActionPoints, unit.maxActionPoints);
            EditorUtility.SetDirty(unit);
        }
    }

    private static void ApplyUnitStatProperty(BattleUnit unit, string propertyName, int value)
    {
        if (unit == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        SerializedObject unitObject = new SerializedObject(unit);
        unitObject.Update();
        SerializedProperty property = unitObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.intValue = value;
        unitObject.ApplyModifiedPropertiesWithoutUndo();
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
                actionPoints = 4,
                hitRate = 100,
                physicalResistancePenetration = 0,
                fireResistancePenetration = 0,
                corruptionResistancePenetration = 0,
                coldResistancePenetration = 0,
                criticalChance = 20,
                criticalDamage = 150
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
        entry.FindPropertyRelative("actionPoints").intValue = 4;
        SerializedProperty hitRateProperty = entry.FindPropertyRelative("hitRate");
        if (hitRateProperty != null)
        {
            hitRateProperty.intValue = 100;
        }
        SerializedProperty physicalResistanceProperty = entry.FindPropertyRelative("physicalResistance");
        if (physicalResistanceProperty != null)
        {
            physicalResistanceProperty.intValue = 0;
        }
        SerializedProperty fireResistanceProperty = entry.FindPropertyRelative("fireResistance");
        if (fireResistanceProperty != null)
        {
            fireResistanceProperty.intValue = 0;
        }
        SerializedProperty corruptionResistanceProperty = entry.FindPropertyRelative("corruptionResistance");
        if (corruptionResistanceProperty != null)
        {
            corruptionResistanceProperty.intValue = 0;
        }
        SerializedProperty coldResistanceProperty = entry.FindPropertyRelative("coldResistance");
        if (coldResistanceProperty != null)
        {
            coldResistanceProperty.intValue = 0;
        }
        SerializedProperty physicalResistancePenetrationProperty = entry.FindPropertyRelative("physicalResistancePenetration");
        if (physicalResistancePenetrationProperty != null)
        {
            physicalResistancePenetrationProperty.intValue = 0;
        }
        SerializedProperty fireResistancePenetrationProperty = entry.FindPropertyRelative("fireResistancePenetration");
        if (fireResistancePenetrationProperty != null)
        {
            fireResistancePenetrationProperty.intValue = 0;
        }
        SerializedProperty corruptionResistancePenetrationProperty = entry.FindPropertyRelative("corruptionResistancePenetration");
        if (corruptionResistancePenetrationProperty != null)
        {
            corruptionResistancePenetrationProperty.intValue = 0;
        }
        SerializedProperty coldResistancePenetration2Property = entry.FindPropertyRelative("coldResistancePenetration");
        if (coldResistancePenetration2Property != null)
        {
            coldResistancePenetration2Property.intValue = 0;
        }
        SerializedProperty criticalChanceProperty = entry.FindPropertyRelative("criticalChance");
        if (criticalChanceProperty != null)
        {
            criticalChanceProperty.intValue = 20;
        }
        SerializedProperty criticalDamageProperty = entry.FindPropertyRelative("criticalDamage");
        if (criticalDamageProperty != null)
        {
            criticalDamageProperty.intValue = 150;
        }
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
