using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public sealed class BattleSkillEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/BattleSkillDatabase.asset";
    private static readonly ItemDatabase.WeaponCategory[] RequiredWeaponCategories =
    {
        ItemDatabase.WeaponCategory.OneHanded,
        ItemDatabase.WeaponCategory.TwoHanded,
        ItemDatabase.WeaponCategory.Bow
    };

    private static readonly string[] SkillGroupLabels =
    {
        "\u7279\u6b8a",
        "\u6218\u6280",
        "\u6cd5\u672f"
    };

    private static readonly string[] SkillTypeLabels =
    {
        "\u70b9\u9009\u6280\u80fd",
        "\u8303\u56f4\u6280\u80fd"
    };

    private static readonly string[] CastTargetLabels =
    {
        "\u81ea\u5df1\uff08\u5f53\u524d\u56de\u5408\u89d2\u8272\uff09",
        "\u654c\u4eba",
        "\u961f\u53cb\uff08\u4e0d\u542b\u5f53\u524d\u56de\u5408\u89d2\u8272\uff09",
        "\u5168\u90e8\uff08\u6240\u6709\u6709ID\u7684\u6218\u573a\u5355\u4f4d\uff09"
    };

    private static readonly string[] AreaCastTypeLabels =
    {
        "落点",
        "圆轴"
    };

    private static readonly string[] CircularAxisAreaTypeLabels =
    {
        "射线",
        "扇形"
    };

    private static readonly string[] DamageTypeLabels =
    {
        "物理",
        "火焰",
        "腐败",
        "寒冷"
    };

    private Vector2 scroll;
    private SerializedObject databaseObject;
    private readonly Dictionary<string, bool> entryFoldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/\u6280\u80fd/\u6280\u80fd\u7f16\u8f91\u5668")]
    private static void Open()
    {
        BattleSkillEditorWindow window = GetWindow<BattleSkillEditorWindow>("\u6280\u80fd\u7f16\u8f91\u5668");
        window.minSize = new Vector2(680f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleSkillDatabase database = EnsureDatabase();

        EditorGUILayout.LabelField("\u6218\u6597\u6280\u80fd\u914d\u7f6e", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u6280\u80fd\u5206\u7ec4\u56fa\u5b9a\u4e3a\uff1a\u7279\u6b8a\u3001\u6218\u6280\u3001\u6cd5\u672f\u3002\u79fb\u52a8\u6280\u80fd\u4fdd\u7559\u6280\u80fdID\u201c\u79fb\u52a8\u201d\uff0c\u5e76\u5f52\u7c7b\u4e3a\u201c\u7279\u6b8a + \u8303\u56f4\u6280\u80fd\u201d\u3002",
            MessageType.Info);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65b0"))
            {
                Repaint();
            }

            if (GUILayout.Button("\u8865\u9f50\u9ed8\u8ba4\u79fb\u52a8\u6280\u80fd"))
            {
                EnsureDefaultMoveSkill(database);
            }
        }

        EditorGUILayout.Space(6f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawEntries(database);
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntries(BattleSkillDatabase database)
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("\u6280\u80fd\u5e93\u8d44\u6e90\u521b\u5efa\u5931\u8d25\u3002", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();
        SerializedProperty entries = databaseObject.FindProperty("entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty group = entry.FindPropertyRelative("group");
            SerializedProperty skillType = entry.FindPropertyRelative("skillType");
            SerializedProperty useMoveDistanceAsRange = entry.FindPropertyRelative("useMoveDistanceAsRange");
            SerializedProperty skillIdProperty = entry.FindPropertyRelative("skillId");
            string foldoutKey = GetEntryFoldoutKey(skillIdProperty != null ? skillIdProperty.stringValue : string.Empty, i);
            bool isExpanded = GetFoldoutState(foldoutKey);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string headerLabel = BuildSkillHeaderLabel(skillIdProperty != null ? skillIdProperty.stringValue : string.Empty, i);
                    bool nextExpanded = EditorGUILayout.Foldout(isExpanded, headerLabel, true);
                    if (nextExpanded != isExpanded)
                    {
                        SetFoldoutState(foldoutKey, nextExpanded);
                        isExpanded = nextExpanded;
                    }
                    if (GUILayout.Button("\u5220\u9664", GUILayout.Width(60f)))
                    {
                        entryFoldoutStates.Remove(foldoutKey);
                        entries.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                if (!isExpanded)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(skillIdProperty, new GUIContent("\u6280\u80fdID"));
                DrawNormalAttackExtraRuleHint(skillIdProperty);

                group.enumValueIndex = EditorGUILayout.Popup("\u5206\u7ec4", group.enumValueIndex, SkillGroupLabels);
                skillType.enumValueIndex = EditorGUILayout.Popup("\u6280\u80fd\u7c7b\u578b", skillType.enumValueIndex, SkillTypeLabels);

                SerializedProperty castTarget = entry.FindPropertyRelative("castTarget");
                castTarget.enumValueIndex = EditorGUILayout.Popup("\u65bd\u6cd5\u5bf9\u8c61", castTarget.enumValueIndex, CastTargetLabels);

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("description"), new GUIContent("\u6280\u80fd\u63cf\u8ff0"));
                BattleSkillDatabase.SkillGroup currentGroup = (BattleSkillDatabase.SkillGroup)group.enumValueIndex;
                DrawCountField(entry.FindPropertyRelative("castCount"), "施法次数");
                DrawCountField(entry.FindPropertyRelative("hitCount"), "命中次数");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("icon"), new GUIContent("\u6280\u80fd\u56fe\u6807"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("noDamage"), new GUIContent("\u65e0\u4f24\u5bb3"));
                if (currentGroup == BattleSkillDatabase.SkillGroup.Spell)
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("attributeMultiplier"), new GUIContent("\u5c5e\u6027\u500d\u7387"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("fixedDamage"), new GUIContent("\u56fa\u5b9a\u4f24\u5bb3"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("hitRateModifier"), new GUIContent("\u547D\u4E2D\u7387\u4FEE\u6B63\uff08%\uff09"));
                    SerializedProperty damageType = entry.FindPropertyRelative("damageType");
                    damageType.enumValueIndex = EditorGUILayout.Popup("\u4f24\u5bb3\u7c7b\u578b", damageType.enumValueIndex, DamageTypeLabels);
                    entry.FindPropertyRelative("damageMultiplier").floatValue = 1f;
                }
                else if (currentGroup == BattleSkillDatabase.SkillGroup.CombatArt)
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("damageMultiplier"), new GUIContent("\u4f24\u5bb3\u500d\u7387"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("hitRateModifier"), new GUIContent("\u547D\u4E2D\u7387\u4FEE\u6B63\uff08%\uff09"));
                    entry.FindPropertyRelative("attributeMultiplier").floatValue = 1f;
                    entry.FindPropertyRelative("fixedDamage").intValue = 0;
                    entry.FindPropertyRelative("damageType").enumValueIndex = (int)BattleSkillDatabase.DamageType.Physical;
                }
                else
                {
                    entry.FindPropertyRelative("damageMultiplier").floatValue = 1f;
                    entry.FindPropertyRelative("attributeMultiplier").floatValue = 1f;
                    entry.FindPropertyRelative("fixedDamage").intValue = 0;
                    entry.FindPropertyRelative("hitRateModifier").intValue = 0;
                    entry.FindPropertyRelative("damageType").enumValueIndex = (int)BattleSkillDatabase.DamageType.Physical;
                }

                bool rangeFromMoveDistance = EditorGUILayout.Toggle("\u5c04\u7a0b\u53d6\u79fb\u52a8\u8ddd\u79bb", useMoveDistanceAsRange.boolValue);
                useMoveDistanceAsRange.boolValue = rangeFromMoveDistance;
                if (!rangeFromMoveDistance)
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("range"), new GUIContent("\u5c04\u7a0b"));
                }
                else
                {
                    EditorGUILayout.LabelField("\u5c04\u7a0b", "\u53d6\u81ea\u89d2\u8272\u7684\u79fb\u52a8\u8ddd\u79bb");
                }

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("cooldownTurns"), new GUIContent("\u51b7\u5374\u65f6\u95f4\uff08\u56de\u5408\uff09"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("manaCost"), new GUIContent("\u9b54\u6cd5\u6d88\u8017\uff08MP\uff09"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("actionPointCost"), new GUIContent("\u884c\u52a8\u529b\u6d88\u8017\uff08AP\uff09"));
                DrawAttachedEffects(entry.FindPropertyRelative("attachedEffects"));
                if (currentGroup == BattleSkillDatabase.SkillGroup.CombatArt)
                {
                    DrawRequiredWeaponCategories(entry.FindPropertyRelative("requiredWeaponCategories"));
                }
                else
                {
                    entry.FindPropertyRelative("requiredWeaponCategories").ClearArray();
                }

                if ((BattleSkillDatabase.SkillType)skillType.enumValueIndex == BattleSkillDatabase.SkillType.Area)
                {
                    SerializedProperty areaCastType = entry.FindPropertyRelative("areaCastType");
                    SerializedProperty circularAxisAreaType = entry.FindPropertyRelative("circularAxisAreaType");
                    SerializedProperty axisWidth = entry.FindPropertyRelative("axisWidth");
                    SerializedProperty axisAngle = entry.FindPropertyRelative("axisAngle");
                    SerializedProperty effectSize = entry.FindPropertyRelative("effectSize");
                    areaCastType.enumValueIndex = EditorGUILayout.Popup("施法类型", areaCastType.enumValueIndex, AreaCastTypeLabels);
                    if ((BattleSkillDatabase.AreaCastType)areaCastType.enumValueIndex == BattleSkillDatabase.AreaCastType.CircularAxis)
                    {
                        circularAxisAreaType.enumValueIndex = EditorGUILayout.Popup("作用范围分类", circularAxisAreaType.enumValueIndex, CircularAxisAreaTypeLabels);
                        EditorGUILayout.LabelField("作用范围", "长度/半径等于射程");
                        if ((BattleSkillDatabase.CircularAxisAreaType)circularAxisAreaType.enumValueIndex == BattleSkillDatabase.CircularAxisAreaType.Ray)
                        {
                            axisWidth.intValue = Mathf.Max(1, EditorGUILayout.IntField("宽度", Mathf.Max(1, axisWidth.intValue)));
                        }
                        else
                        {
                            axisAngle.floatValue = Mathf.Clamp(EditorGUILayout.FloatField("角度", axisAngle.floatValue), 1f, 360f);
                            EditorGUILayout.HelpBox("180° = 半圆，360° = 全圆。扇形作用范围直接等于射程。", MessageType.Info);
                        }
                    }
                    else
                    {
                        Vector2Int size = effectSize.vector2IntValue;
                        int width = EditorGUILayout.IntField("\u4f5c\u7528\u8303\u56f4\u5bbd", Mathf.Max(1, size.x));
                        int height = EditorGUILayout.IntField("\u4f5c\u7528\u8303\u56f4\u9ad8", Mathf.Max(1, size.y));
                        effectSize.vector2IntValue = new Vector2Int(width, height);

                        Vector2Int updatedSize = effectSize.vector2IntValue;
                        EditorGUILayout.LabelField("\u4f5c\u7528\u8303\u56f4", $"{Mathf.Max(1, updatedSize.x)}x{Mathf.Max(1, updatedSize.y)}");
                    }
                }
            }
        }

        if (GUILayout.Button("\u65b0\u589e\u6280\u80fd"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            ResetEntry(entries.GetArrayElementAtIndex(entries.arraySize - 1));
        }

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static void ResetEntry(SerializedProperty entry)
    {
        entry.FindPropertyRelative("skillId").stringValue = string.Empty;
        entry.FindPropertyRelative("description").stringValue = string.Empty;
        entry.FindPropertyRelative("enableHitFeel").boolValue = false;
        entry.FindPropertyRelative("resolveFrame").intValue = 0;
        entry.FindPropertyRelative("castCount").intValue = 1;
        entry.FindPropertyRelative("hitCount").intValue = 1;
        entry.FindPropertyRelative("extraHitResolveFrames").ClearArray();
        entry.FindPropertyRelative("group").enumValueIndex = (int)BattleSkillDatabase.SkillGroup.CombatArt;
        entry.FindPropertyRelative("skillType").enumValueIndex = (int)BattleSkillDatabase.SkillType.Target;
        entry.FindPropertyRelative("castTarget").enumValueIndex = (int)BattleSkillDatabase.CastTarget.Enemy;
        entry.FindPropertyRelative("icon").objectReferenceValue = null;
        entry.FindPropertyRelative("hitEffectPrefab").objectReferenceValue = null;
        entry.FindPropertyRelative("useProjectile").boolValue = false;
        entry.FindPropertyRelative("projectilePrefab").objectReferenceValue = null;
        entry.FindPropertyRelative("projectileStartFrame").intValue = 0;
        entry.FindPropertyRelative("extraProjectileStartFrames").ClearArray();
        entry.FindPropertyRelative("noDamage").boolValue = false;
        entry.FindPropertyRelative("damageMultiplier").floatValue = 1f;
        entry.FindPropertyRelative("attributeMultiplier").floatValue = 1f;
        entry.FindPropertyRelative("fixedDamage").intValue = 0;
        entry.FindPropertyRelative("hitRateModifier").intValue = 0;
        entry.FindPropertyRelative("damageType").enumValueIndex = (int)BattleSkillDatabase.DamageType.Physical;
        entry.FindPropertyRelative("actionPointCost").intValue = 1;
        entry.FindPropertyRelative("manaCost").intValue = 0;
        entry.FindPropertyRelative("cooldownTurns").intValue = 0;
        entry.FindPropertyRelative("useMoveDistanceAsRange").boolValue = false;
        entry.FindPropertyRelative("range").intValue = 1;
        entry.FindPropertyRelative("areaCastType").enumValueIndex = (int)BattleSkillDatabase.AreaCastType.ImpactPoint;
        entry.FindPropertyRelative("circularAxisAreaType").enumValueIndex = (int)BattleSkillDatabase.CircularAxisAreaType.Ray;
        entry.FindPropertyRelative("axisWidth").intValue = 3;
        entry.FindPropertyRelative("axisAngle").floatValue = 180f;
        entry.FindPropertyRelative("effectSize").vector2IntValue = new Vector2Int(1, 1);
        entry.FindPropertyRelative("attachedEffects").ClearArray();
        entry.FindPropertyRelative("requiredWeaponCategories").ClearArray();
        entry.FindPropertyRelative("weaponActionOverrides").ClearArray();
    }

    private bool GetFoldoutState(string key)
    {
        if (entryFoldoutStates.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        entryFoldoutStates[key] = false;
        return false;
    }

    private void SetFoldoutState(string key, bool expanded)
    {
        entryFoldoutStates[key] = expanded;
    }

    private static string GetEntryFoldoutKey(string skillId, int index)
    {
        return $"skill_{index}";
    }

    private static string BuildSkillHeaderLabel(string skillId, int index)
    {
        return string.IsNullOrWhiteSpace(skillId) ? $"未命名技能 {index + 1}" : skillId;
    }

    private static void DrawAttachedEffects(SerializedProperty attachedEffectsProperty)
    {
        if (attachedEffectsProperty == null)
        {
            return;
        }

        EffectDatabase effectDatabase = EffectDatabase.LoadDefault();
        List<EffectDatabase.EffectEntry> effectEntries = GetValidEffectEntries(effectDatabase);

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("附加效果");

        if (attachedEffectsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("当前没有附加效果。可从效果编辑器中配置好的效果里选择。", MessageType.None);
        }

        for (int i = 0; i < attachedEffectsProperty.arraySize; i++)
        {
            SerializedProperty attachedEffectProperty = attachedEffectsProperty.GetArrayElementAtIndex(i);
            SerializedProperty effectIdProperty = attachedEffectProperty.FindPropertyRelative("effectId");
            SerializedProperty durationTurnsProperty = attachedEffectProperty.FindPropertyRelative("durationTurns");
            SerializedProperty applyChancePercentProperty = attachedEffectProperty.FindPropertyRelative("applyChancePercent");
            using (new EditorGUILayout.VerticalScope("box"))
            {
                string label = "效果 " + (i + 1);
                string currentEffectId = effectIdProperty != null ? effectIdProperty.stringValue : string.Empty;

                if (effectEntries == null || effectEntries.Count == 0)
                {
                    EditorGUILayout.LabelField(label, "没有可用效果");
                    effectIdProperty.stringValue = string.Empty;
                }
                else
                {
                    int selectedIndex = ResolveAttachedEffectIndex(effectEntries, currentEffectId);
                    string[] popupOptions = BuildAttachedEffectOptions(effectEntries);
                    int nextIndex = EditorGUILayout.Popup(label, selectedIndex, popupOptions);
                    effectIdProperty.stringValue = ResolveAttachedEffectIdByIndex(effectEntries, nextIndex);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (durationTurnsProperty != null)
                    {
                        durationTurnsProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("持续回合", Mathf.Max(0, durationTurnsProperty.intValue)));
                    }

                    if (applyChancePercentProperty != null)
                    {
                        applyChancePercentProperty.intValue = Mathf.Clamp(EditorGUILayout.IntField("附着概率(%)", Mathf.Clamp(applyChancePercentProperty.intValue, 0, 100)), 0, 100);
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("删除", GUILayout.Width(60f)))
                    {
                        attachedEffectsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(effectEntries == null || effectEntries.Count == 0))
            {
                if (GUILayout.Button("新增附加效果", GUILayout.Width(120f)))
                {
                    int index = attachedEffectsProperty.arraySize;
                    attachedEffectsProperty.InsertArrayElementAtIndex(index);
                    SerializedProperty attachedEffectProperty = attachedEffectsProperty.GetArrayElementAtIndex(index);
                    attachedEffectProperty.FindPropertyRelative("effectId").stringValue = ResolveAttachedEffectIdByIndex(effectEntries, 0);
                    attachedEffectProperty.FindPropertyRelative("durationTurns").intValue = 1;
                    SerializedProperty applyChancePercentProperty = attachedEffectProperty.FindPropertyRelative("applyChancePercent");
                    if (applyChancePercentProperty != null)
                    {
                        applyChancePercentProperty.intValue = 100;
                    }
                }
            }
        }
    }

    private static string[] BuildAttachedEffectOptions(List<EffectDatabase.EffectEntry> effectEntries)
    {
        string[] options = new string[effectEntries.Count];
        for (int i = 0; i < effectEntries.Count; i++)
        {
            EffectDatabase.EffectEntry entry = effectEntries[i];
            options[i] = entry.effectId;
        }

        return options;
    }

    private static int ResolveAttachedEffectIndex(List<EffectDatabase.EffectEntry> effectEntries, string effectId)
    {
        if (effectEntries == null || effectEntries.Count == 0 || string.IsNullOrWhiteSpace(effectId))
        {
            return 0;
        }

        for (int i = 0; i < effectEntries.Count; i++)
        {
            EffectDatabase.EffectEntry entry = effectEntries[i];
            if (entry != null && string.Equals(entry.effectId, effectId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    private static string ResolveAttachedEffectIdByIndex(List<EffectDatabase.EffectEntry> effectEntries, int index)
    {
        if (effectEntries == null || effectEntries.Count == 0)
        {
            return string.Empty;
        }

        int safeIndex = Mathf.Clamp(index, 0, effectEntries.Count - 1);
        EffectDatabase.EffectEntry entry = effectEntries[safeIndex];
        return entry.effectId;
    }

    private static List<EffectDatabase.EffectEntry> GetValidEffectEntries(EffectDatabase effectDatabase)
    {
        List<EffectDatabase.EffectEntry> result = new List<EffectDatabase.EffectEntry>();
        if (effectDatabase == null || effectDatabase.Entries == null)
        {
            return result;
        }

        for (int i = 0; i < effectDatabase.Entries.Count; i++)
        {
            EffectDatabase.EffectEntry entry = effectDatabase.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.effectId))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private static void DrawRequiredWeaponCategories(SerializedProperty requiredCategoriesProperty)
    {
        if (requiredCategoriesProperty == null)
        {
            return;
        }

        EditorGUILayout.LabelField("必须武器");
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < RequiredWeaponCategories.Length; i++)
            {
                ItemDatabase.WeaponCategory category = RequiredWeaponCategories[i];
                bool enabled = ContainsWeaponCategory(requiredCategoriesProperty, category);
                bool toggled = EditorGUILayout.ToggleLeft(GetWeaponCategoryLabel(category), enabled, GUILayout.Width(100f));
                if (toggled == enabled)
                {
                    continue;
                }

                SetWeaponCategoryEnabled(requiredCategoriesProperty, category, toggled);
            }
        }
    }

    private static bool ContainsWeaponCategory(SerializedProperty property, ItemDatabase.WeaponCategory category)
    {
        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element != null && element.enumValueIndex == (int)category)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetWeaponCategoryEnabled(SerializedProperty property, ItemDatabase.WeaponCategory category, bool enabled)
    {
        if (enabled)
        {
            if (ContainsWeaponCategory(property, category))
            {
                return;
            }

            int index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            property.GetArrayElementAtIndex(index).enumValueIndex = (int)category;
            return;
        }

        for (int i = property.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element != null && element.enumValueIndex == (int)category)
            {
                property.DeleteArrayElementAtIndex(i);
            }
        }
    }

    private static string GetWeaponCategoryLabel(ItemDatabase.WeaponCategory category)
    {
        switch (category)
        {
            case ItemDatabase.WeaponCategory.OneHanded:
                return "单手武器";
            case ItemDatabase.WeaponCategory.TwoHanded:
                return "双手武器";
            case ItemDatabase.WeaponCategory.Bow:
                return "弓箭";
            default:
                return category.ToString();
        }
    }

    private static BattleSkillDatabase EnsureDatabase()
    {
        BattleSkillDatabase database = AssetDatabase.LoadAssetAtPath<BattleSkillDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<BattleSkillDatabase>();
        database.Entries.Add(CreateDefaultMoveSkill());
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureDefaultMoveSkill(BattleSkillDatabase database)
    {
        if (database == null)
        {
            return;
        }

        BattleSkillDatabase.SkillEntry moveSkill = database.FindEntry(BattleSkillDatabase.MoveSkillId);
        if (moveSkill != null)
        {
            moveSkill.group = BattleSkillDatabase.SkillGroup.Special;
            moveSkill.skillType = BattleSkillDatabase.SkillType.Area;
            moveSkill.useMoveDistanceAsRange = true;
            moveSkill.effectSize = new Vector2Int(3, 3);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return;
        }

        database.Entries.Add(CreateDefaultMoveSkill());
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private static BattleSkillDatabase.SkillEntry CreateDefaultMoveSkill()
    {
        return new BattleSkillDatabase.SkillEntry
        {
            skillId = BattleSkillDatabase.MoveSkillId,
            description = string.Empty,
            group = BattleSkillDatabase.SkillGroup.Special,
            skillType = BattleSkillDatabase.SkillType.Area,
            castTarget = BattleSkillDatabase.CastTarget.Self,
            icon = null,
            noDamage = true,
            damageMultiplier = 1f,
            actionPointCost = 1,
            manaCost = 0,
            cooldownTurns = 0,
            useMoveDistanceAsRange = true,
            range = 0,
            resolveFrame = 0,
            castCount = 1,
            hitCount = 1,
            effectSize = new Vector2Int(3, 3)
        };
    }

    private static void DrawCountField(SerializedProperty property, string label)
    {
        if (property == null)
        {
            return;
        }

        property.intValue = Mathf.Max(1, EditorGUILayout.IntField(label, Mathf.Max(1, property.intValue)));
    }

    private static void DrawNormalAttackExtraRuleHint(SerializedProperty skillIdProperty)
    {
        if (skillIdProperty == null ||
            !string.Equals(skillIdProperty.stringValue, "普通攻击", System.StringComparison.Ordinal))
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "普通攻击额外规则：\n" +
            "施法次数：选择目标次数，目标选满后统一播放动画和结算。\n" +
            "命中次数：每次动画命中点内结算的伤害段数。\n" +
            "双持普通攻击：第1段读取主手武器，第2段读取副手武器；缺少对应武器会在 Console 警告。",
            MessageType.Info);
    }

}

