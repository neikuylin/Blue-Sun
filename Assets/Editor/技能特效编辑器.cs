using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 技能特效编辑器 : EditorWindow
{
    private const string SkillAssetPath = "Assets/Resources/BattleSkillDatabase.asset";

    private Vector2 scroll;
    private SerializedObject skillDatabaseObject;
    private static readonly Dictionary<string, bool> SkillFoldouts = new Dictionary<string, bool>();
    private static readonly Dictionary<string, bool> WeaponHitSoundFoldouts = new Dictionary<string, bool>();

    [MenuItem("Tools/技能/技能特效")]
    private static void Open()
    {
        技能特效编辑器 window = GetWindow<技能特效编辑器>("技能特效");
        window.minSize = new Vector2(760f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleSkillDatabase skillDatabase = AssetDatabase.LoadAssetAtPath<BattleSkillDatabase>(SkillAssetPath);
        if (skillDatabase == null)
        {
            EditorGUILayout.HelpBox("未找到 BattleSkillDatabase.asset。请先创建技能库。", MessageType.Warning);
            return;
        }

        if (skillDatabaseObject == null || skillDatabaseObject.targetObject != skillDatabase)
        {
            skillDatabaseObject = new SerializedObject(skillDatabase);
        }

        EditorGUILayout.LabelField("技能特效", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里集中配置技能受击表现、飞行弹道、判定时间，以及按武器分流里的受击音效。动作、角度、技能音效仍在“技能动作栏”。", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        skillDatabaseObject.Update();
        SerializedProperty entries = skillDatabaseObject.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            DrawSkillRow(entries.GetArrayElementAtIndex(i));
        }
        EditorGUILayout.EndScrollView();

        if (skillDatabaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(skillDatabase);
            AssetDatabase.SaveAssets();
        }
    }

    private static void DrawSkillRow(SerializedProperty entry)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty skillIdProperty = entry.FindPropertyRelative("skillId");
        SerializedProperty groupProperty = entry.FindPropertyRelative("group");
        SerializedProperty requiredWeaponCategoriesProperty = entry.FindPropertyRelative("requiredWeaponCategories");
        string skillId = skillIdProperty != null ? skillIdProperty.stringValue : string.Empty;
        string foldoutKey = string.IsNullOrWhiteSpace(skillId) ? "未命名技能" : skillId;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            bool expanded = GetFoldoutState(SkillFoldouts, foldoutKey, false);
            expanded = EditorGUILayout.Foldout(expanded, string.IsNullOrWhiteSpace(skillId) ? "（未命名技能）" : skillId, true);
            SetFoldoutState(SkillFoldouts, foldoutKey, expanded);
            if (!expanded)
            {
                return;
            }

            EditorGUILayout.LabelField("分组", ResolveGroupLabel(groupProperty != null ? groupProperty.enumValueIndex : 0));
            DrawBaseEffectFields(entry);
            DrawWeaponHitSoundFields(entry, skillId, requiredWeaponCategoriesProperty);
        }
    }

    private static void DrawBaseEffectFields(SerializedProperty entry)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("技能本体特效", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("enableHitFeel"), new GUIContent("打击感"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("hitEffectPrefab"), new GUIContent("受击特效预制体"));
        DrawProjectileFields(entry);
        if (!IsProjectileEnabled(entry))
        {
            DrawResolveFrameField(entry);
            DrawExtraHitResolveFrameFields(entry);
        }
    }

    private static void DrawProjectileFields(SerializedProperty entry)
    {
        SerializedProperty useProjectile = entry.FindPropertyRelative("useProjectile");
        SerializedProperty projectilePrefab = entry.FindPropertyRelative("projectilePrefab");
        SerializedProperty projectileStartFrame = entry.FindPropertyRelative("projectileStartFrame");
        if (useProjectile == null || projectilePrefab == null || projectileStartFrame == null)
        {
            return;
        }

        useProjectile.boolValue = EditorGUILayout.Toggle("启用飞行弹道", useProjectile.boolValue);
        if (useProjectile.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(projectilePrefab, new GUIContent("飞行物体预制体"));
            projectileStartFrame.intValue = Mathf.Max(0, EditorGUILayout.IntField("飞行开始时间", Mathf.Max(0, projectileStartFrame.intValue)));
            DrawExtraProjectileStartFrameFields(entry);
            EditorGUI.indentLevel--;
        }
        else
        {
            projectilePrefab.objectReferenceValue = null;
        }
    }

    private static bool IsProjectileEnabled(SerializedProperty entry)
    {
        SerializedProperty useProjectile = entry.FindPropertyRelative("useProjectile");
        return useProjectile != null && useProjectile.boolValue;
    }

    private static void DrawExtraHitResolveFrameFields(SerializedProperty entry)
    {
        SerializedProperty hitCountProperty = entry.FindPropertyRelative("hitCount");
        SerializedProperty extraHitResolveFramesProperty = entry.FindPropertyRelative("extraHitResolveFrames");
        if (hitCountProperty == null || extraHitResolveFramesProperty == null)
        {
            return;
        }

        int requiredCount = Mathf.Max(0, Mathf.Max(1, hitCountProperty.intValue) - 1);
        while (extraHitResolveFramesProperty.arraySize < requiredCount)
        {
            int index = extraHitResolveFramesProperty.arraySize;
            extraHitResolveFramesProperty.InsertArrayElementAtIndex(index);
            extraHitResolveFramesProperty.GetArrayElementAtIndex(index).intValue = 0;
        }

        while (extraHitResolveFramesProperty.arraySize > requiredCount)
        {
            extraHitResolveFramesProperty.DeleteArrayElementAtIndex(extraHitResolveFramesProperty.arraySize - 1);
        }

        for (int i = 0; i < requiredCount; i++)
        {
            SerializedProperty frameProperty = extraHitResolveFramesProperty.GetArrayElementAtIndex(i);
            if (frameProperty == null)
            {
                continue;
            }

            frameProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField($"第{i + 2}下判定时间", Mathf.Max(0, frameProperty.intValue)));
        }
    }

    private static void DrawExtraProjectileStartFrameFields(SerializedProperty entry)
    {
        SerializedProperty hitCountProperty = entry.FindPropertyRelative("hitCount");
        SerializedProperty extraProjectileStartFramesProperty = entry.FindPropertyRelative("extraProjectileStartFrames");
        if (hitCountProperty == null || extraProjectileStartFramesProperty == null)
        {
            return;
        }

        int requiredCount = Mathf.Max(0, Mathf.Max(1, hitCountProperty.intValue) - 1);
        while (extraProjectileStartFramesProperty.arraySize < requiredCount)
        {
            extraProjectileStartFramesProperty.InsertArrayElementAtIndex(extraProjectileStartFramesProperty.arraySize);
            extraProjectileStartFramesProperty.GetArrayElementAtIndex(extraProjectileStartFramesProperty.arraySize - 1).intValue = 0;
        }

        while (extraProjectileStartFramesProperty.arraySize > requiredCount)
        {
            extraProjectileStartFramesProperty.DeleteArrayElementAtIndex(extraProjectileStartFramesProperty.arraySize - 1);
        }

        for (int i = 0; i < requiredCount; i++)
        {
            SerializedProperty frameProperty = extraProjectileStartFramesProperty.GetArrayElementAtIndex(i);
            if (frameProperty == null)
            {
                continue;
            }

            frameProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField($"第{i + 2}发飞行开始时间", Mathf.Max(0, frameProperty.intValue)));
        }
    }

    private static void DrawResolveFrameField(SerializedProperty entry)
    {
        SerializedProperty resolveFrameProperty = entry.FindPropertyRelative("resolveFrame");
        if (resolveFrameProperty == null)
        {
            return;
        }

        resolveFrameProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("判定时间", Mathf.Max(0, resolveFrameProperty.intValue)));
        EditorGUILayout.LabelField("说明", "启用飞行弹道后，结算由飞行弹道到达目标触发。");
    }

    private static void DrawWeaponHitSoundFields(SerializedProperty entry, string skillId, SerializedProperty requiredWeaponCategoriesProperty)
    {
        SerializedProperty overridesProperty = entry.FindPropertyRelative("weaponActionOverrides");
        if (overridesProperty == null || requiredWeaponCategoriesProperty == null)
        {
            return;
        }

        bool isMoveSkill = string.Equals(skillId, BattleSkillDatabase.MoveSkillId, StringComparison.Ordinal);
        List<ItemDatabase.WeaponCategory> categories = isMoveSkill
            ? GetMoveSkillWeaponCategories()
            : GetRequiredWeaponCategories(requiredWeaponCategoriesProperty);
        SyncWeaponOverrideEntries(overridesProperty, categories);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            string foldoutKey = string.IsNullOrWhiteSpace(skillId) ? "skill_weapon_hit_sounds" : skillId;
            bool expanded = GetFoldoutState(WeaponHitSoundFoldouts, foldoutKey, true);
            expanded = EditorGUILayout.Foldout(expanded, "按武器分流受击音效", true);
            SetFoldoutState(WeaponHitSoundFoldouts, foldoutKey, expanded);
            if (!expanded)
            {
                return;
            }

            if (categories.Count == 0)
            {
                EditorGUILayout.HelpBox("这个技能现在没有勾选任何“必须武器”类别，所以这里不会出现武器分流受击音效。", MessageType.Info);
                return;
            }

            for (int i = 0; i < categories.Count; i++)
            {
                SerializedProperty overrideEntry = overridesProperty.GetArrayElementAtIndex(i);
                DrawWeaponHitSoundEntry(overrideEntry, categories[i]);
            }
        }
    }

    private static void DrawWeaponHitSoundEntry(SerializedProperty entry, ItemDatabase.WeaponCategory weaponCategory)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty hitSoundProperty = entry.FindPropertyRelative("hitSound");
        SerializedProperty hitSoundPrefabProperty = entry.FindPropertyRelative("hitSoundPrefab");

        using (new EditorGUI.IndentLevelScope())
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(GetWeaponCategoryLabel(weaponCategory), EditorStyles.boldLabel);
            if (hitSoundProperty != null)
            {
                EditorGUILayout.PropertyField(hitSoundProperty, new GUIContent("受击音效"));
            }

            if (hitSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(hitSoundPrefabProperty, new GUIContent("受击音效预制体"));
            }
        }
    }

    private static void SyncWeaponOverrideEntries(SerializedProperty overridesProperty, List<ItemDatabase.WeaponCategory> categories)
    {
        Dictionary<ItemDatabase.WeaponCategory, WeaponOverrideSnapshot> existingSnapshots = new Dictionary<ItemDatabase.WeaponCategory, WeaponOverrideSnapshot>();
        for (int i = 0; i < overridesProperty.arraySize; i++)
        {
            SerializedProperty entry = overridesProperty.GetArrayElementAtIndex(i);
            SerializedProperty weaponCategoryProperty = entry != null ? entry.FindPropertyRelative("weaponCategory") : null;
            if (weaponCategoryProperty == null)
            {
                continue;
            }

            ItemDatabase.WeaponCategory category = (ItemDatabase.WeaponCategory)weaponCategoryProperty.enumValueIndex;
            if (!existingSnapshots.ContainsKey(category))
            {
                existingSnapshots.Add(category, CaptureWeaponOverrideSnapshot(entry));
            }
        }

        overridesProperty.arraySize = categories.Count;
        for (int i = 0; i < categories.Count; i++)
        {
            SerializedProperty entry = overridesProperty.GetArrayElementAtIndex(i);
            ItemDatabase.WeaponCategory category = categories[i];
            ApplyWeaponCategory(entry, category);

            if (existingSnapshots.TryGetValue(category, out WeaponOverrideSnapshot snapshot))
            {
                RestoreWeaponOverrideSnapshot(entry, snapshot);
            }
            else
            {
                ClearWeaponOverrideEntry(entry);
            }
        }
    }

    private static List<ItemDatabase.WeaponCategory> GetRequiredWeaponCategories(SerializedProperty requiredWeaponCategoriesProperty)
    {
        List<ItemDatabase.WeaponCategory> categories = new List<ItemDatabase.WeaponCategory>();
        for (int i = 0; i < requiredWeaponCategoriesProperty.arraySize; i++)
        {
            SerializedProperty element = requiredWeaponCategoriesProperty.GetArrayElementAtIndex(i);
            if (element == null)
            {
                continue;
            }

            ItemDatabase.WeaponCategory category = (ItemDatabase.WeaponCategory)element.enumValueIndex;
            if (category == ItemDatabase.WeaponCategory.None || categories.Contains(category))
            {
                continue;
            }

            categories.Add(category);
        }

        return categories;
    }

    private static List<ItemDatabase.WeaponCategory> GetMoveSkillWeaponCategories()
    {
        return new List<ItemDatabase.WeaponCategory>
        {
            ItemDatabase.WeaponCategory.None,
            ItemDatabase.WeaponCategory.OneHanded,
            ItemDatabase.WeaponCategory.TwoHanded,
            ItemDatabase.WeaponCategory.Bow,
            ItemDatabase.WeaponCategory.Staff
        };
    }

    private static void ApplyWeaponCategory(SerializedProperty entry, ItemDatabase.WeaponCategory category)
    {
        SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
        if (enabledProperty != null)
        {
            enabledProperty.boolValue = true;
        }

        SerializedProperty weaponCategoryProperty = entry.FindPropertyRelative("weaponCategory");
        if (weaponCategoryProperty != null)
        {
            weaponCategoryProperty.enumValueIndex = (int)category;
        }
    }

    private static void ClearWeaponOverrideEntry(SerializedProperty entry)
    {
        if (entry == null)
        {
            return;
        }

        SetString(entry, "raiseHandStateName", string.Empty);
        SetFloat(entry, "raiseHandYawOffset", 0f);
        SetString(entry, "targetSelectionStateName", string.Empty);
        SetFloat(entry, "targetSelectionYawOffset", 0f);
        SetString(entry, "actionStateName", string.Empty);
        SetFloat(entry, "actionYawOffset", 0f);
        SetFloat(entry, "postUseYawOffset", 0f);
        SetObject(entry, "actionSound", null);
        SetObject(entry, "actionSoundPrefab", null);
        SetObject(entry, "hitSound", null);
        SetObject(entry, "hitSoundPrefab", null);
        SetInt(entry, "soundDelayFrame", 0);
        SetBool(entry, "compensateActionMotion", false);
    }

    private static string ResolveGroupLabel(int enumValueIndex)
    {
        if (enumValueIndex == (int)BattleSkillDatabase.SkillGroup.Special)
        {
            return "特殊";
        }

        if (enumValueIndex == (int)BattleSkillDatabase.SkillGroup.Spell)
        {
            return "法术";
        }

        return "战技";
    }

    private static string GetWeaponCategoryLabel(ItemDatabase.WeaponCategory weaponCategory)
    {
        switch (weaponCategory)
        {
            case ItemDatabase.WeaponCategory.OneHanded:
                return "单手武器";
            case ItemDatabase.WeaponCategory.TwoHanded:
                return "双手武器";
            case ItemDatabase.WeaponCategory.Bow:
                return "弓箭";
            case ItemDatabase.WeaponCategory.Staff:
                return "法杖";
            default:
                return "无武器";
        }
    }

    private static bool GetFoldoutState(Dictionary<string, bool> states, string key, bool defaultValue)
    {
        if (states.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        states[key] = defaultValue;
        return defaultValue;
    }

    private static void SetFoldoutState(Dictionary<string, bool> states, string key, bool expanded)
    {
        states[key] = expanded;
    }

    private static void SetString(SerializedProperty entry, string propertyName, string value)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetFloat(SerializedProperty entry, string propertyName, float value)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetInt(SerializedProperty entry, string propertyName, int value)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetBool(SerializedProperty entry, string propertyName, bool value)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetObject(SerializedProperty entry, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private struct WeaponOverrideSnapshot
    {
        public bool enabled;
        public string raiseHandStateName;
        public float raiseHandYawOffset;
        public string targetSelectionStateName;
        public float targetSelectionYawOffset;
        public string actionStateName;
        public float actionYawOffset;
        public float postUseYawOffset;
        public UnityEngine.Object actionSound;
        public UnityEngine.Object actionSoundPrefab;
        public UnityEngine.Object hitSound;
        public UnityEngine.Object hitSoundPrefab;
        public int soundDelayFrame;
        public bool compensateActionMotion;
    }

    private static WeaponOverrideSnapshot CaptureWeaponOverrideSnapshot(SerializedProperty entry)
    {
        return new WeaponOverrideSnapshot
        {
            enabled = GetBool(entry, "enabled"),
            raiseHandStateName = GetString(entry, "raiseHandStateName"),
            raiseHandYawOffset = GetFloat(entry, "raiseHandYawOffset"),
            targetSelectionStateName = GetString(entry, "targetSelectionStateName"),
            targetSelectionYawOffset = GetFloat(entry, "targetSelectionYawOffset"),
            actionStateName = GetString(entry, "actionStateName"),
            actionYawOffset = GetFloat(entry, "actionYawOffset"),
            postUseYawOffset = GetFloat(entry, "postUseYawOffset"),
            actionSound = GetObject(entry, "actionSound"),
            actionSoundPrefab = GetObject(entry, "actionSoundPrefab"),
            hitSound = GetObject(entry, "hitSound"),
            hitSoundPrefab = GetObject(entry, "hitSoundPrefab"),
            soundDelayFrame = GetInt(entry, "soundDelayFrame"),
            compensateActionMotion = GetBool(entry, "compensateActionMotion")
        };
    }

    private static void RestoreWeaponOverrideSnapshot(SerializedProperty entry, WeaponOverrideSnapshot snapshot)
    {
        SetBool(entry, "enabled", snapshot.enabled);
        SetString(entry, "raiseHandStateName", snapshot.raiseHandStateName ?? string.Empty);
        SetFloat(entry, "raiseHandYawOffset", snapshot.raiseHandYawOffset);
        SetString(entry, "targetSelectionStateName", snapshot.targetSelectionStateName ?? string.Empty);
        SetFloat(entry, "targetSelectionYawOffset", snapshot.targetSelectionYawOffset);
        SetString(entry, "actionStateName", snapshot.actionStateName ?? string.Empty);
        SetFloat(entry, "actionYawOffset", snapshot.actionYawOffset);
        SetFloat(entry, "postUseYawOffset", snapshot.postUseYawOffset);
        SetObject(entry, "actionSound", snapshot.actionSound);
        SetObject(entry, "actionSoundPrefab", snapshot.actionSoundPrefab);
        SetObject(entry, "hitSound", snapshot.hitSound);
        SetObject(entry, "hitSoundPrefab", snapshot.hitSoundPrefab);
        SetInt(entry, "soundDelayFrame", snapshot.soundDelayFrame);
        SetBool(entry, "compensateActionMotion", snapshot.compensateActionMotion);
    }

    private static string GetString(SerializedProperty entry, string propertyName)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        return property != null ? property.stringValue : string.Empty;
    }

    private static float GetFloat(SerializedProperty entry, string propertyName)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        return property != null ? property.floatValue : 0f;
    }

    private static int GetInt(SerializedProperty entry, string propertyName)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        return property != null ? property.intValue : 0;
    }

    private static bool GetBool(SerializedProperty entry, string propertyName)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        return property != null && property.boolValue;
    }

    private static UnityEngine.Object GetObject(SerializedProperty entry, string propertyName)
    {
        SerializedProperty property = entry.FindPropertyRelative(propertyName);
        return property != null ? property.objectReferenceValue : null;
    }
}
