using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class SkillActionBindingWindow : EditorWindow
{
    private const string SkillAssetPath = "Assets/Resources/BattleSkillDatabase.asset";
    private static readonly ItemDatabase.WeaponCategory[] MoveWeaponCategories =
    {
        ItemDatabase.WeaponCategory.None,
        ItemDatabase.WeaponCategory.OneHanded,
        ItemDatabase.WeaponCategory.TwoHanded,
        ItemDatabase.WeaponCategory.Bow
    };

    private Vector2 scroll;
    private SerializedObject skillDatabaseObject;
    private static bool showMoveWeaponOverrides = true;

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
        SerializedProperty actionStateNameProperty = entry.FindPropertyRelative("actionStateName");
        SerializedProperty actionYawOffsetProperty = entry.FindPropertyRelative("actionYawOffset");
        SerializedProperty actionSoundProperty = entry.FindPropertyRelative("actionSound");
        SerializedProperty actionSoundPrefabProperty = entry.FindPropertyRelative("actionSoundPrefab");
        SerializedProperty soundDelayFrameProperty = entry.FindPropertyRelative("soundDelayFrame");
        SerializedProperty enableHitFeelProperty = entry.FindPropertyRelative("enableHitFeel");
        SerializedProperty compensateActionMotionProperty = entry.FindPropertyRelative("compensateActionMotion");
        SerializedProperty groupProperty = entry.FindPropertyRelative("group");

        string skillId = skillIdProperty != null ? skillIdProperty.stringValue : string.Empty;
        string currentAction = actionStateNameProperty != null ? actionStateNameProperty.stringValue : string.Empty;
        int selectedIndex = FindOptionIndex(actionOptions, currentAction);
        string groupLabel = ResolveGroupLabel(groupProperty != null ? groupProperty.enumValueIndex : 0);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(skillId) ? "（未命名技能）" : skillId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("分组", groupLabel);
            int newIndex = EditorGUILayout.Popup("绑定动作", selectedIndex, actionOptions.ToArray());
            if (actionStateNameProperty != null)
            {
                actionStateNameProperty.stringValue = newIndex <= 0 ? string.Empty : actionOptions[newIndex];
            }

            if (actionYawOffsetProperty != null)
            {
                actionYawOffsetProperty.floatValue = EditorGUILayout.FloatField("角度修正", actionYawOffsetProperty.floatValue);
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
            if (enableHitFeelProperty != null)
            {
                enableHitFeelProperty.boolValue = EditorGUILayout.Toggle("打击感", enableHitFeelProperty.boolValue);
            }

            if (compensateActionMotionProperty != null)
            {
                compensateActionMotionProperty.boolValue = EditorGUILayout.Toggle("位移补偿", compensateActionMotionProperty.boolValue);
            }

            if (IsMoveSkill(skillId))
            {
                DrawMoveWeaponOverrides(entry, actionOptions);
            }
        }
    }

    private static void DrawMoveWeaponOverrides(SerializedProperty entry, List<string> actionOptions)
    {
        SerializedProperty overridesProperty = entry.FindPropertyRelative("weaponActionOverrides");
        if (overridesProperty == null)
        {
            return;
        }

        EnsureMoveOverrideEntries(overridesProperty);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            showMoveWeaponOverrides = EditorGUILayout.Foldout(showMoveWeaponOverrides, "移动按武器分流", true);
            if (!showMoveWeaponOverrides)
            {
                return;
            }

            EditorGUILayout.HelpBox("移动技能只读取这里的武器分类动作，不回退到上面的默认动作。", MessageType.Info);

            for (int i = 0; i < MoveWeaponCategories.Length; i++)
            {
                SerializedProperty overrideEntry = overridesProperty.GetArrayElementAtIndex(i);
                DrawMoveOverrideEntry(overrideEntry, MoveWeaponCategories[i], actionOptions);
            }
        }
    }

    private static void EnsureMoveOverrideEntries(SerializedProperty overridesProperty)
    {
        int originalSize = overridesProperty.arraySize;
        while (overridesProperty.arraySize < MoveWeaponCategories.Length)
        {
            overridesProperty.InsertArrayElementAtIndex(overridesProperty.arraySize);
        }

        for (int i = 0; i < MoveWeaponCategories.Length; i++)
        {
            SerializedProperty entry = overridesProperty.GetArrayElementAtIndex(i);
            if (entry == null)
            {
                continue;
            }

            SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
            if (enabledProperty != null)
            {
                enabledProperty.boolValue = true;
            }

            SerializedProperty weaponCategoryProperty = entry.FindPropertyRelative("weaponCategory");
            if (weaponCategoryProperty != null)
            {
                weaponCategoryProperty.enumValueIndex = (int)MoveWeaponCategories[i];
            }

            if (i >= originalSize)
            {
                ClearMoveOverrideEntry(entry);
            }
        }
    }

    private static void ClearMoveOverrideEntry(SerializedProperty entry)
    {
        if (entry == null)
        {
            return;
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

    private static void DrawMoveOverrideEntry(SerializedProperty entry, ItemDatabase.WeaponCategory weaponCategory, List<string> actionOptions)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
        SerializedProperty actionStateNameProperty = entry.FindPropertyRelative("actionStateName");
        SerializedProperty actionYawOffsetProperty = entry.FindPropertyRelative("actionYawOffset");
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
            default:
                return "无武器";
        }
    }

    private static bool IsMoveSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        string normalized = skillId.Trim();
        return string.Equals(normalized, BattleSkillDatabase.MoveSkillId, StringComparison.Ordinal) ||
            normalized.Contains(BattleSkillDatabase.MoveSkillId, StringComparison.Ordinal);
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
