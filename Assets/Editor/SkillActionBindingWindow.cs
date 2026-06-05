using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class SkillActionBindingWindow : EditorWindow
{
    private const string SkillAssetPath = "Assets/Resources/BattleSkillDatabase.asset";

    private Vector2 scroll;
    private SerializedObject skillDatabaseObject;
    private static readonly Dictionary<string, bool> WeaponOverrideFoldouts = new Dictionary<string, bool>();

    [MenuItem("Tools/技能/技能动作栏")]
    private static void Open()
    {
        SkillActionBindingWindow window = GetWindow<SkillActionBindingWindow>("技能动作栏");
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

        List<string> actionOptions = BuildActionOptions();

        EditorGUILayout.LabelField("技能动作栏", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里仅配置每个技能自己的施放动作、角度修正、音效和位移补偿。全局待机、进战、退战、探索动作已单独拆到 Tools/技能/全局动作。", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        skillDatabaseObject.Update();
        SerializedProperty entries = skillDatabaseObject.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            DrawSkillRow(entries.GetArrayElementAtIndex(i), actionOptions);
        }
        EditorGUILayout.EndScrollView();

        if (skillDatabaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(skillDatabase);
            AssetDatabase.SaveAssets();
        }
    }

    private static void DrawSkillRow(SerializedProperty entry, List<string> actionOptions)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty skillIdProperty = entry.FindPropertyRelative("skillId");
        SerializedProperty groupProperty = entry.FindPropertyRelative("group");
        SerializedProperty requiredWeaponCategoriesProperty = entry.FindPropertyRelative("requiredWeaponCategories");

        string skillId = skillIdProperty != null ? skillIdProperty.stringValue : string.Empty;
        string groupLabel = ResolveGroupLabel(groupProperty != null ? groupProperty.enumValueIndex : 0);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(skillId) ? "（未命名技能）" : skillId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("分组", groupLabel);

            DrawWeaponOverrides(entry, skillId, requiredWeaponCategoriesProperty, actionOptions);
        }
    }

    private static void DrawWeaponOverrides(SerializedProperty entry, string skillId, SerializedProperty requiredWeaponCategoriesProperty, List<string> actionOptions)
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
            string foldoutKey = string.IsNullOrWhiteSpace(skillId) ? "skill_weapon_overrides" : skillId;
            bool expanded = GetWeaponOverrideFoldoutState(foldoutKey);
            expanded = EditorGUILayout.Foldout(expanded, "按武器分流", true);
            SetWeaponOverrideFoldoutState(foldoutKey, expanded);
            if (!expanded)
            {
                return;
            }

            if (categories.Count == 0)
            {
                EditorGUILayout.HelpBox("这个技能现在没有勾选任何“必须武器”类别，所以这里不会出现武器分流。先去技能编辑器勾选武器类别。", MessageType.Info);
                return;
            }

            if (isMoveSkill)
            {
                EditorGUILayout.HelpBox("移动技能固定按武器类别读取动作，包含无武器、单手、双手、弓箭、法杖。这里不依赖“必须武器”。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("技能动作只读取这里的武器分流。上面的技能本体动作字段已经退出使用。", MessageType.Info);
            }

            for (int i = 0; i < categories.Count; i++)
            {
                SerializedProperty overrideEntry = overridesProperty.GetArrayElementAtIndex(i);
                DrawWeaponOverrideEntry(overrideEntry, categories[i], actionOptions);
            }
        }
    }

    private static void SyncWeaponOverrideEntries(SerializedProperty overridesProperty, List<ItemDatabase.WeaponCategory> categories)
    {
        if (overridesProperty == null)
        {
            return;
        }

        Dictionary<ItemDatabase.WeaponCategory, WeaponOverrideSnapshot> existingSnapshots = new Dictionary<ItemDatabase.WeaponCategory, WeaponOverrideSnapshot>();
        for (int i = 0; i < overridesProperty.arraySize; i++)
        {
            SerializedProperty entry = overridesProperty.GetArrayElementAtIndex(i);
            if (entry == null)
            {
                continue;
            }

            SerializedProperty weaponCategoryProperty = entry.FindPropertyRelative("weaponCategory");
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
                continue;
            }

            ClearWeaponOverrideEntry(entry);
        }
    }

    private static void DrawWeaponOverrideEntry(SerializedProperty entry, ItemDatabase.WeaponCategory weaponCategory, List<string> actionOptions)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
        SerializedProperty raiseHandStateNameProperty = entry.FindPropertyRelative("raiseHandStateName");
        SerializedProperty raiseHandYawOffsetProperty = entry.FindPropertyRelative("raiseHandYawOffset");
        SerializedProperty targetSelectionStateNameProperty = entry.FindPropertyRelative("targetSelectionStateName");
        SerializedProperty targetSelectionYawOffsetProperty = entry.FindPropertyRelative("targetSelectionYawOffset");
        SerializedProperty actionStateNameProperty = entry.FindPropertyRelative("actionStateName");
        SerializedProperty actionYawOffsetProperty = entry.FindPropertyRelative("actionYawOffset");
        SerializedProperty postUseYawOffsetProperty = entry.FindPropertyRelative("postUseYawOffset");
        SerializedProperty actionSoundProperty = entry.FindPropertyRelative("actionSound");
        SerializedProperty actionSoundPrefabProperty = entry.FindPropertyRelative("actionSoundPrefab");
        SerializedProperty soundDelayFrameProperty = entry.FindPropertyRelative("soundDelayFrame");
        SerializedProperty compensateActionMotionProperty = entry.FindPropertyRelative("compensateActionMotion");

        using (new EditorGUI.IndentLevelScope())
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(GetWeaponCategoryLabel(weaponCategory), EditorStyles.boldLabel);

            if (enabledProperty != null)
            {
                enabledProperty.boolValue = EditorGUILayout.Toggle("启用", enabledProperty.boolValue);
            }

            int raiseHandIndex = FindOptionIndex(actionOptions, raiseHandStateNameProperty != null ? raiseHandStateNameProperty.stringValue : string.Empty);
            int newRaiseHandIndex = EditorGUILayout.Popup("抬手动画", raiseHandIndex, actionOptions.ToArray());
            if (raiseHandStateNameProperty != null)
            {
                raiseHandStateNameProperty.stringValue = newRaiseHandIndex <= 0 ? string.Empty : actionOptions[newRaiseHandIndex];
            }

            if (raiseHandYawOffsetProperty != null)
            {
                raiseHandYawOffsetProperty.floatValue = EditorGUILayout.FloatField("抬手角度修正", raiseHandYawOffsetProperty.floatValue);
            }

            int targetSelectionIndex = FindOptionIndex(actionOptions, targetSelectionStateNameProperty != null ? targetSelectionStateNameProperty.stringValue : string.Empty);
            int newTargetSelectionIndex = EditorGUILayout.Popup("选目标动画", targetSelectionIndex, actionOptions.ToArray());
            if (targetSelectionStateNameProperty != null)
            {
                targetSelectionStateNameProperty.stringValue = newTargetSelectionIndex <= 0 ? string.Empty : actionOptions[newTargetSelectionIndex];
            }

            if (targetSelectionYawOffsetProperty != null)
            {
                targetSelectionYawOffsetProperty.floatValue = EditorGUILayout.FloatField("选目标角度修正", targetSelectionYawOffsetProperty.floatValue);
            }

            int selectedIndex = FindOptionIndex(actionOptions, actionStateNameProperty != null ? actionStateNameProperty.stringValue : string.Empty);
            int newIndex = EditorGUILayout.Popup("绑定动作", selectedIndex, actionOptions.ToArray());
            if (actionStateNameProperty != null)
            {
                actionStateNameProperty.stringValue = newIndex <= 0 ? string.Empty : actionOptions[newIndex];
            }

            if (actionYawOffsetProperty != null)
            {
                actionYawOffsetProperty.floatValue = EditorGUILayout.FloatField("角度修正", actionYawOffsetProperty.floatValue);
            }

            if (postUseYawOffsetProperty != null)
            {
                postUseYawOffsetProperty.floatValue = EditorGUILayout.FloatField("释放后朝向偏移", postUseYawOffsetProperty.floatValue);
            }

            if (actionSoundProperty != null)
            {
                EditorGUILayout.PropertyField(actionSoundProperty, new GUIContent("技能音效"));
            }

            if (actionSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(actionSoundPrefabProperty, new GUIContent("技能音效预制体"));
            }

            if (soundDelayFrameProperty != null)
            {
                soundDelayFrameProperty.intValue = EditorGUILayout.IntField("音效延迟帧数", Mathf.Max(0, soundDelayFrameProperty.intValue));
            }

            if (compensateActionMotionProperty != null)
            {
                compensateActionMotionProperty.boolValue = EditorGUILayout.Toggle("位移补偿", compensateActionMotionProperty.boolValue);
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
        if (entry == null)
        {
            return;
        }

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

        SerializedProperty raiseHandStateNameProperty = entry.FindPropertyRelative("raiseHandStateName");
        if (raiseHandStateNameProperty != null)
        {
            raiseHandStateNameProperty.stringValue = string.Empty;
        }

        SerializedProperty raiseHandYawOffsetProperty = entry.FindPropertyRelative("raiseHandYawOffset");
        if (raiseHandYawOffsetProperty != null)
        {
            raiseHandYawOffsetProperty.floatValue = 0f;
        }

        SerializedProperty targetSelectionStateNameProperty = entry.FindPropertyRelative("targetSelectionStateName");
        if (targetSelectionStateNameProperty != null)
        {
            targetSelectionStateNameProperty.stringValue = string.Empty;
        }

        SerializedProperty targetSelectionYawOffsetProperty = entry.FindPropertyRelative("targetSelectionYawOffset");
        if (targetSelectionYawOffsetProperty != null)
        {
            targetSelectionYawOffsetProperty.floatValue = 0f;
        }

        SerializedProperty actionStateNameProperty = entry.FindPropertyRelative("actionStateName");
        if (actionStateNameProperty != null)
        {
            actionStateNameProperty.stringValue = string.Empty;
        }

        SerializedProperty actionYawOffsetProperty = entry.FindPropertyRelative("actionYawOffset");
        if (actionYawOffsetProperty != null)
        {
            actionYawOffsetProperty.floatValue = 0f;
        }

        SerializedProperty postUseYawOffsetProperty = entry.FindPropertyRelative("postUseYawOffset");
        if (postUseYawOffsetProperty != null)
        {
            postUseYawOffsetProperty.floatValue = 0f;
        }

        SerializedProperty actionSoundProperty = entry.FindPropertyRelative("actionSound");
        if (actionSoundProperty != null)
        {
            actionSoundProperty.objectReferenceValue = null;
        }

        SerializedProperty actionSoundPrefabProperty = entry.FindPropertyRelative("actionSoundPrefab");
        if (actionSoundPrefabProperty != null)
        {
            actionSoundPrefabProperty.objectReferenceValue = null;
        }

        SerializedProperty hitSoundProperty = entry.FindPropertyRelative("hitSound");
        if (hitSoundProperty != null)
        {
            hitSoundProperty.objectReferenceValue = null;
        }

        SerializedProperty hitSoundPrefabProperty = entry.FindPropertyRelative("hitSoundPrefab");
        if (hitSoundPrefabProperty != null)
        {
            hitSoundPrefabProperty.objectReferenceValue = null;
        }

        SerializedProperty soundDelayFrameProperty = entry.FindPropertyRelative("soundDelayFrame");
        if (soundDelayFrameProperty != null)
        {
            soundDelayFrameProperty.intValue = 0;
        }

        SerializedProperty compensateActionMotionProperty = entry.FindPropertyRelative("compensateActionMotion");
        if (compensateActionMotionProperty != null)
        {
            compensateActionMotionProperty.boolValue = false;
        }
    }

    private static int FindOptionIndex(List<string> options, string currentValue)
    {
        if (options == null || string.IsNullOrWhiteSpace(currentValue))
        {
            return 0;
        }

        for (int i = 1; i < options.Count; i++)
        {
            if (string.Equals(options[i], currentValue, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
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

    private static bool GetWeaponOverrideFoldoutState(string key)
    {
        if (WeaponOverrideFoldouts.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        WeaponOverrideFoldouts[key] = true;
        return true;
    }

    private static void SetWeaponOverrideFoldoutState(string key, bool expanded)
    {
        WeaponOverrideFoldouts[key] = expanded;
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
        WeaponOverrideSnapshot snapshot = new WeaponOverrideSnapshot();
        if (entry == null)
        {
            return snapshot;
        }

        SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
        if (enabledProperty != null)
        {
            snapshot.enabled = enabledProperty.boolValue;
        }

        SerializedProperty raiseHandStateNameProperty = entry.FindPropertyRelative("raiseHandStateName");
        if (raiseHandStateNameProperty != null)
        {
            snapshot.raiseHandStateName = raiseHandStateNameProperty.stringValue;
        }

        SerializedProperty raiseHandYawOffsetProperty = entry.FindPropertyRelative("raiseHandYawOffset");
        if (raiseHandYawOffsetProperty != null)
        {
            snapshot.raiseHandYawOffset = raiseHandYawOffsetProperty.floatValue;
        }

        SerializedProperty targetSelectionStateNameProperty = entry.FindPropertyRelative("targetSelectionStateName");
        if (targetSelectionStateNameProperty != null)
        {
            snapshot.targetSelectionStateName = targetSelectionStateNameProperty.stringValue;
        }

        SerializedProperty targetSelectionYawOffsetProperty = entry.FindPropertyRelative("targetSelectionYawOffset");
        if (targetSelectionYawOffsetProperty != null)
        {
            snapshot.targetSelectionYawOffset = targetSelectionYawOffsetProperty.floatValue;
        }

        SerializedProperty actionStateNameProperty = entry.FindPropertyRelative("actionStateName");
        if (actionStateNameProperty != null)
        {
            snapshot.actionStateName = actionStateNameProperty.stringValue;
        }

        SerializedProperty actionYawOffsetProperty = entry.FindPropertyRelative("actionYawOffset");
        if (actionYawOffsetProperty != null)
        {
            snapshot.actionYawOffset = actionYawOffsetProperty.floatValue;
        }

        SerializedProperty postUseYawOffsetProperty = entry.FindPropertyRelative("postUseYawOffset");
        if (postUseYawOffsetProperty != null)
        {
            snapshot.postUseYawOffset = postUseYawOffsetProperty.floatValue;
        }

        SerializedProperty actionSoundProperty = entry.FindPropertyRelative("actionSound");
        if (actionSoundProperty != null)
        {
            snapshot.actionSound = actionSoundProperty.objectReferenceValue;
        }

        SerializedProperty actionSoundPrefabProperty = entry.FindPropertyRelative("actionSoundPrefab");
        if (actionSoundPrefabProperty != null)
        {
            snapshot.actionSoundPrefab = actionSoundPrefabProperty.objectReferenceValue;
        }

        SerializedProperty hitSoundProperty = entry.FindPropertyRelative("hitSound");
        if (hitSoundProperty != null)
        {
            snapshot.hitSound = hitSoundProperty.objectReferenceValue;
        }

        SerializedProperty hitSoundPrefabProperty = entry.FindPropertyRelative("hitSoundPrefab");
        if (hitSoundPrefabProperty != null)
        {
            snapshot.hitSoundPrefab = hitSoundPrefabProperty.objectReferenceValue;
        }

        SerializedProperty soundDelayFrameProperty = entry.FindPropertyRelative("soundDelayFrame");
        if (soundDelayFrameProperty != null)
        {
            snapshot.soundDelayFrame = soundDelayFrameProperty.intValue;
        }

        SerializedProperty compensateActionMotionProperty = entry.FindPropertyRelative("compensateActionMotion");
        if (compensateActionMotionProperty != null)
        {
            snapshot.compensateActionMotion = compensateActionMotionProperty.boolValue;
        }

        return snapshot;
    }

    private static void RestoreWeaponOverrideSnapshot(SerializedProperty entry, WeaponOverrideSnapshot snapshot)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
        if (enabledProperty != null)
        {
            enabledProperty.boolValue = snapshot.enabled;
        }

        SerializedProperty raiseHandStateNameProperty = entry.FindPropertyRelative("raiseHandStateName");
        if (raiseHandStateNameProperty != null)
        {
            raiseHandStateNameProperty.stringValue = snapshot.raiseHandStateName ?? string.Empty;
        }

        SerializedProperty raiseHandYawOffsetProperty = entry.FindPropertyRelative("raiseHandYawOffset");
        if (raiseHandYawOffsetProperty != null)
        {
            raiseHandYawOffsetProperty.floatValue = snapshot.raiseHandYawOffset;
        }

        SerializedProperty targetSelectionStateNameProperty = entry.FindPropertyRelative("targetSelectionStateName");
        if (targetSelectionStateNameProperty != null)
        {
            targetSelectionStateNameProperty.stringValue = snapshot.targetSelectionStateName ?? string.Empty;
        }

        SerializedProperty targetSelectionYawOffsetProperty = entry.FindPropertyRelative("targetSelectionYawOffset");
        if (targetSelectionYawOffsetProperty != null)
        {
            targetSelectionYawOffsetProperty.floatValue = snapshot.targetSelectionYawOffset;
        }

        SerializedProperty actionStateNameProperty = entry.FindPropertyRelative("actionStateName");
        if (actionStateNameProperty != null)
        {
            actionStateNameProperty.stringValue = snapshot.actionStateName ?? string.Empty;
        }

        SerializedProperty actionYawOffsetProperty = entry.FindPropertyRelative("actionYawOffset");
        if (actionYawOffsetProperty != null)
        {
            actionYawOffsetProperty.floatValue = snapshot.actionYawOffset;
        }

        SerializedProperty postUseYawOffsetProperty = entry.FindPropertyRelative("postUseYawOffset");
        if (postUseYawOffsetProperty != null)
        {
            postUseYawOffsetProperty.floatValue = snapshot.postUseYawOffset;
        }

        SerializedProperty actionSoundProperty = entry.FindPropertyRelative("actionSound");
        if (actionSoundProperty != null)
        {
            actionSoundProperty.objectReferenceValue = snapshot.actionSound;
        }

        SerializedProperty actionSoundPrefabProperty = entry.FindPropertyRelative("actionSoundPrefab");
        if (actionSoundPrefabProperty != null)
        {
            actionSoundPrefabProperty.objectReferenceValue = snapshot.actionSoundPrefab;
        }

        SerializedProperty hitSoundProperty = entry.FindPropertyRelative("hitSound");
        if (hitSoundProperty != null)
        {
            hitSoundProperty.objectReferenceValue = snapshot.hitSound;
        }

        SerializedProperty hitSoundPrefabProperty = entry.FindPropertyRelative("hitSoundPrefab");
        if (hitSoundPrefabProperty != null)
        {
            hitSoundPrefabProperty.objectReferenceValue = snapshot.hitSoundPrefab;
        }

        SerializedProperty soundDelayFrameProperty = entry.FindPropertyRelative("soundDelayFrame");
        if (soundDelayFrameProperty != null)
        {
            soundDelayFrameProperty.intValue = snapshot.soundDelayFrame;
        }

        SerializedProperty compensateActionMotionProperty = entry.FindPropertyRelative("compensateActionMotion");
        if (compensateActionMotionProperty != null)
        {
            compensateActionMotionProperty.boolValue = snapshot.compensateActionMotion;
        }
    }

    private static List<string> BuildActionOptions()
    {
        List<string> options = new List<string> { "（空）" };
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        BattleCharacterBindingDatabase bindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        if (bindingDatabase == null)
        {
            return options;
        }

        for (int i = 0; i < bindingDatabase.Entries.Count; i++)
        {
            BattleCharacterBindingDatabase.BindingEntry binding = bindingDatabase.Entries[i];
            AnimatorController controller = binding != null ? binding.animatorController as AnimatorController : null;
            if (controller == null || controller.layers == null)
            {
                continue;
            }

            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
            {
                AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;
                if (stateMachine == null)
                {
                    continue;
                }

                ChildAnimatorState[] states = stateMachine.states;
                for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
                {
                    AnimatorState state = states[stateIndex].state;
                    if (state == null || string.IsNullOrWhiteSpace(state.name) || !seen.Add(state.name))
                    {
                        continue;
                    }

                    options.Add(state.name);
                }
            }
        }

        return options;
    }
}
