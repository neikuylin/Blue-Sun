using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class SkillActionBindingWindow : EditorWindow
{
    private const string SkillAssetPath = "Assets/Resources/BattleSkillDatabase.asset";
    private const string SettingsAssetPath = "Assets/Resources/BattleAnimationSettings.asset";

    private Vector2 scroll;
    private SerializedObject skillDatabaseObject;
    private SerializedObject settingsObject;

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
        BattleAnimationSettings settings = EnsureSettings();
        if (skillDatabase == null)
        {
            EditorGUILayout.HelpBox("未找到 BattleSkillDatabase.asset。请先创建技能库。", MessageType.Warning);
            return;
        }

        if (skillDatabaseObject == null || skillDatabaseObject.targetObject != skillDatabase)
        {
            skillDatabaseObject = new SerializedObject(skillDatabase);
        }

        if (settingsObject == null || settingsObject.targetObject != settings)
        {
            settingsObject = new SerializedObject(settings);
        }

        List<string> actionOptions = BuildActionOptions();

        EditorGUILayout.LabelField("技能动作栏", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("顶部配置全局待机、进战、战技左转身瞄准、战技右转身瞄准、受击、闪避动作和待机角度修正。下方每个技能只绑定自己的施放动作和角度修正。", MessageType.Info);

        settingsObject.Update();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("全局动作", EditorStyles.boldLabel);
            SerializedProperty idleStateNameProperty = settingsObject.FindProperty("idleStateName");
            SerializedProperty idleSoundProperty = settingsObject.FindProperty("idleSound");
            SerializedProperty enterBattleStateNameProperty = settingsObject.FindProperty("enterBattleStateName");
            SerializedProperty enterBattleSoundProperty = settingsObject.FindProperty("enterBattleSound");
            SerializedProperty combatArtLeftAimStateNameProperty = settingsObject.FindProperty("combatArtLeftAimStateName");
            SerializedProperty combatArtLeftAimSoundProperty = settingsObject.FindProperty("combatArtLeftAimSound");
            SerializedProperty combatArtRightAimStateNameProperty = settingsObject.FindProperty("combatArtRightAimStateName");
            SerializedProperty combatArtRightAimSoundProperty = settingsObject.FindProperty("combatArtRightAimSound");
            SerializedProperty hitReactionStateNameProperty = settingsObject.FindProperty("hitReactionStateName");
            SerializedProperty hitReactionSoundProperty = settingsObject.FindProperty("hitReactionSound");
            SerializedProperty dodgeStateNameProperty = settingsObject.FindProperty("dodgeStateName");
            SerializedProperty dodgeSoundProperty = settingsObject.FindProperty("dodgeSound");
            SerializedProperty idleYawOffsetProperty = settingsObject.FindProperty("idleYawOffset");

            string currentIdle = idleStateNameProperty != null ? idleStateNameProperty.stringValue : string.Empty;
            string currentEnterBattle = enterBattleStateNameProperty != null ? enterBattleStateNameProperty.stringValue : string.Empty;
            string currentCombatArtLeftAim = combatArtLeftAimStateNameProperty != null ? combatArtLeftAimStateNameProperty.stringValue : string.Empty;
            string currentCombatArtRightAim = combatArtRightAimStateNameProperty != null ? combatArtRightAimStateNameProperty.stringValue : string.Empty;
            string currentHitReaction = hitReactionStateNameProperty != null ? hitReactionStateNameProperty.stringValue : string.Empty;
            string currentDodge = dodgeStateNameProperty != null ? dodgeStateNameProperty.stringValue : string.Empty;

            int selectedIdleIndex = FindOptionIndex(actionOptions, currentIdle);
            int selectedEnterBattleIndex = FindOptionIndex(actionOptions, currentEnterBattle);
            int selectedCombatArtLeftAimIndex = FindOptionIndex(actionOptions, currentCombatArtLeftAim);
            int selectedCombatArtRightAimIndex = FindOptionIndex(actionOptions, currentCombatArtRightAim);
            int selectedHitReactionIndex = FindOptionIndex(actionOptions, currentHitReaction);
            int selectedDodgeIndex = FindOptionIndex(actionOptions, currentDodge);

            int newIdleIndex = EditorGUILayout.Popup("待机动画", selectedIdleIndex, actionOptions.ToArray());
            if (idleStateNameProperty != null)
            {
                idleStateNameProperty.stringValue = newIdleIndex <= 0 ? string.Empty : actionOptions[newIdleIndex];
            }
            if (idleSoundProperty != null)
            {
                EditorGUILayout.PropertyField(idleSoundProperty, new GUIContent("待机音效"));
            }

            int newEnterBattleIndex = EditorGUILayout.Popup("进战动画", selectedEnterBattleIndex, actionOptions.ToArray());
            if (enterBattleStateNameProperty != null)
            {
                enterBattleStateNameProperty.stringValue = newEnterBattleIndex <= 0 ? string.Empty : actionOptions[newEnterBattleIndex];
            }
            if (enterBattleSoundProperty != null)
            {
                EditorGUILayout.PropertyField(enterBattleSoundProperty, new GUIContent("进战音效"));
            }

            int newCombatArtLeftAimIndex = EditorGUILayout.Popup("战技左转身瞄准动画", selectedCombatArtLeftAimIndex, actionOptions.ToArray());
            if (combatArtLeftAimStateNameProperty != null)
            {
                combatArtLeftAimStateNameProperty.stringValue = newCombatArtLeftAimIndex <= 0 ? string.Empty : actionOptions[newCombatArtLeftAimIndex];
            }
            if (combatArtLeftAimSoundProperty != null)
            {
                EditorGUILayout.PropertyField(combatArtLeftAimSoundProperty, new GUIContent("战技左瞄准音效"));
            }

            int newCombatArtRightAimIndex = EditorGUILayout.Popup("战技右转身瞄准动画", selectedCombatArtRightAimIndex, actionOptions.ToArray());
            if (combatArtRightAimStateNameProperty != null)
            {
                combatArtRightAimStateNameProperty.stringValue = newCombatArtRightAimIndex <= 0 ? string.Empty : actionOptions[newCombatArtRightAimIndex];
            }
            if (combatArtRightAimSoundProperty != null)
            {
                EditorGUILayout.PropertyField(combatArtRightAimSoundProperty, new GUIContent("战技右瞄准音效"));
            }

            int newHitReactionIndex = EditorGUILayout.Popup("受击动画", selectedHitReactionIndex, actionOptions.ToArray());
            if (hitReactionStateNameProperty != null)
            {
                hitReactionStateNameProperty.stringValue = newHitReactionIndex <= 0 ? string.Empty : actionOptions[newHitReactionIndex];
            }
            if (hitReactionSoundProperty != null)
            {
                EditorGUILayout.PropertyField(hitReactionSoundProperty, new GUIContent("受击音效"));
            }

            int newDodgeIndex = EditorGUILayout.Popup("闪避动画", selectedDodgeIndex, actionOptions.ToArray());
            if (dodgeStateNameProperty != null)
            {
                dodgeStateNameProperty.stringValue = newDodgeIndex <= 0 ? string.Empty : actionOptions[newDodgeIndex];
            }
            if (dodgeSoundProperty != null)
            {
                EditorGUILayout.PropertyField(dodgeSoundProperty, new GUIContent("闪避音效"));
            }

            if (idleYawOffsetProperty != null)
            {
                idleYawOffsetProperty.floatValue = EditorGUILayout.FloatField("待机角度修正", idleYawOffsetProperty.floatValue);
            }
        }

        if (settingsObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

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
        }
    }

    private static BattleAnimationSettings EnsureSettings()
    {
        BattleAnimationSettings settings = AssetDatabase.LoadAssetAtPath<BattleAnimationSettings>(SettingsAssetPath);
        if (settings != null)
        {
            return settings;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        settings = CreateInstance<BattleAnimationSettings>();
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return settings;
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
