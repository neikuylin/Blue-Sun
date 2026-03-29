using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class GlobalActionBindingWindow : EditorWindow
{
    private const string SettingsAssetPath = "Assets/Resources/BattleAnimationSettings.asset";

    private SerializedObject settingsObject;

    [MenuItem("Tools/技能/全局动作")]
    private static void Open()
    {
        GlobalActionBindingWindow window = GetWindow<GlobalActionBindingWindow>("全局动作");
        window.minSize = new Vector2(760f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleAnimationSettings settings = EnsureSettings();
        if (settingsObject == null || settingsObject.targetObject != settings)
        {
            settingsObject = new SerializedObject(settings);
        }

        List<string> actionOptions = BuildActionOptions();

        EditorGUILayout.LabelField("全局动作", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里单独配置全局待机、进战、退战、战技左转身瞄准、战技右转身瞄准、受击、闪避、探索待机、探索移动动作和待机角度修正。", MessageType.Info);

        settingsObject.Update();

        using (new EditorGUILayout.VerticalScope("box"))
        {
            SerializedProperty idleStateNameProperty = settingsObject.FindProperty("idleStateName");
            SerializedProperty idleSoundProperty = settingsObject.FindProperty("idleSound");
            SerializedProperty idleSoundPrefabProperty = settingsObject.FindProperty("idleSoundPrefab");
            SerializedProperty enterBattleStateNameProperty = settingsObject.FindProperty("enterBattleStateName");
            SerializedProperty enterBattleSoundProperty = settingsObject.FindProperty("enterBattleSound");
            SerializedProperty enterBattleSoundPrefabProperty = settingsObject.FindProperty("enterBattleSoundPrefab");
            SerializedProperty enterBattleCompensateMotionProperty = settingsObject.FindProperty("enterBattleCompensateMotion");
            SerializedProperty exitBattleStateNameProperty = settingsObject.FindProperty("exitBattleStateName");
            SerializedProperty exitBattleSoundProperty = settingsObject.FindProperty("exitBattleSound");
            SerializedProperty exitBattleSoundPrefabProperty = settingsObject.FindProperty("exitBattleSoundPrefab");
            SerializedProperty exitBattleCompensateMotionProperty = settingsObject.FindProperty("exitBattleCompensateMotion");
            SerializedProperty combatArtLeftAimStateNameProperty = settingsObject.FindProperty("combatArtLeftAimStateName");
            SerializedProperty combatArtLeftAimSoundProperty = settingsObject.FindProperty("combatArtLeftAimSound");
            SerializedProperty combatArtLeftAimSoundPrefabProperty = settingsObject.FindProperty("combatArtLeftAimSoundPrefab");
            SerializedProperty combatArtLeftAimCompensateMotionProperty = settingsObject.FindProperty("combatArtLeftAimCompensateMotion");
            SerializedProperty combatArtRightAimStateNameProperty = settingsObject.FindProperty("combatArtRightAimStateName");
            SerializedProperty combatArtRightAimSoundProperty = settingsObject.FindProperty("combatArtRightAimSound");
            SerializedProperty combatArtRightAimSoundPrefabProperty = settingsObject.FindProperty("combatArtRightAimSoundPrefab");
            SerializedProperty combatArtRightAimCompensateMotionProperty = settingsObject.FindProperty("combatArtRightAimCompensateMotion");
            SerializedProperty hitReactionStateNameProperty = settingsObject.FindProperty("hitReactionStateName");
            SerializedProperty hitReactionSoundProperty = settingsObject.FindProperty("hitReactionSound");
            SerializedProperty hitReactionSoundPrefabProperty = settingsObject.FindProperty("hitReactionSoundPrefab");
            SerializedProperty hitReactionCompensateMotionProperty = settingsObject.FindProperty("hitReactionCompensateMotion");
            SerializedProperty dodgeStateNameProperty = settingsObject.FindProperty("dodgeStateName");
            SerializedProperty dodgeSoundProperty = settingsObject.FindProperty("dodgeSound");
            SerializedProperty dodgeSoundPrefabProperty = settingsObject.FindProperty("dodgeSoundPrefab");
            SerializedProperty dodgeCompensateMotionProperty = settingsObject.FindProperty("dodgeCompensateMotion");
            SerializedProperty explorationIdleStateNameProperty = settingsObject.FindProperty("explorationIdleStateName");
            SerializedProperty explorationIdleSoundProperty = settingsObject.FindProperty("explorationIdleSound");
            SerializedProperty explorationIdleSoundPrefabProperty = settingsObject.FindProperty("explorationIdleSoundPrefab");
            SerializedProperty explorationIdleCompensateMotionProperty = settingsObject.FindProperty("explorationIdleCompensateMotion");
            SerializedProperty explorationMoveStateNameProperty = settingsObject.FindProperty("explorationMoveStateName");
            SerializedProperty explorationMoveSoundProperty = settingsObject.FindProperty("explorationMoveSound");
            SerializedProperty explorationMoveSoundPrefabProperty = settingsObject.FindProperty("explorationMoveSoundPrefab");
            SerializedProperty explorationMoveCompensateMotionProperty = settingsObject.FindProperty("explorationMoveCompensateMotion");
            SerializedProperty idleYawOffsetProperty = settingsObject.FindProperty("idleYawOffset");

            string currentIdle = idleStateNameProperty != null ? idleStateNameProperty.stringValue : string.Empty;
            string currentEnterBattle = enterBattleStateNameProperty != null ? enterBattleStateNameProperty.stringValue : string.Empty;
            string currentExitBattle = exitBattleStateNameProperty != null ? exitBattleStateNameProperty.stringValue : string.Empty;
            string currentCombatArtLeftAim = combatArtLeftAimStateNameProperty != null ? combatArtLeftAimStateNameProperty.stringValue : string.Empty;
            string currentCombatArtRightAim = combatArtRightAimStateNameProperty != null ? combatArtRightAimStateNameProperty.stringValue : string.Empty;
            string currentHitReaction = hitReactionStateNameProperty != null ? hitReactionStateNameProperty.stringValue : string.Empty;
            string currentDodge = dodgeStateNameProperty != null ? dodgeStateNameProperty.stringValue : string.Empty;
            string currentExplorationIdle = explorationIdleStateNameProperty != null ? explorationIdleStateNameProperty.stringValue : string.Empty;
            string currentExplorationMove = explorationMoveStateNameProperty != null ? explorationMoveStateNameProperty.stringValue : string.Empty;

            int selectedIdleIndex = FindOptionIndex(actionOptions, currentIdle);
            int selectedEnterBattleIndex = FindOptionIndex(actionOptions, currentEnterBattle);
            int selectedExitBattleIndex = FindOptionIndex(actionOptions, currentExitBattle);
            int selectedCombatArtLeftAimIndex = FindOptionIndex(actionOptions, currentCombatArtLeftAim);
            int selectedCombatArtRightAimIndex = FindOptionIndex(actionOptions, currentCombatArtRightAim);
            int selectedHitReactionIndex = FindOptionIndex(actionOptions, currentHitReaction);
            int selectedDodgeIndex = FindOptionIndex(actionOptions, currentDodge);
            int selectedExplorationIdleIndex = FindOptionIndex(actionOptions, currentExplorationIdle);
            int selectedExplorationMoveIndex = FindOptionIndex(actionOptions, currentExplorationMove);

            int newIdleIndex = EditorGUILayout.Popup("待机动画", selectedIdleIndex, actionOptions.ToArray());
            if (idleStateNameProperty != null)
            {
                idleStateNameProperty.stringValue = newIdleIndex <= 0 ? string.Empty : actionOptions[newIdleIndex];
            }
            if (idleSoundProperty != null)
            {
                EditorGUILayout.PropertyField(idleSoundProperty, new GUIContent("待机音效"));
            }
            if (idleSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(idleSoundPrefabProperty, new GUIContent("待机音效预制体"));
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
            if (enterBattleSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(enterBattleSoundPrefabProperty, new GUIContent("进战音效预制体"));
            }
            if (enterBattleCompensateMotionProperty != null)
            {
                enterBattleCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("进战位移补偿", enterBattleCompensateMotionProperty.boolValue);
            }

            int newExitBattleIndex = EditorGUILayout.Popup("退战动画", selectedExitBattleIndex, actionOptions.ToArray());
            if (exitBattleStateNameProperty != null)
            {
                exitBattleStateNameProperty.stringValue = newExitBattleIndex <= 0 ? string.Empty : actionOptions[newExitBattleIndex];
            }
            if (exitBattleSoundProperty != null)
            {
                EditorGUILayout.PropertyField(exitBattleSoundProperty, new GUIContent("退战音效"));
            }
            if (exitBattleSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(exitBattleSoundPrefabProperty, new GUIContent("退战音效预制体"));
            }
            if (exitBattleCompensateMotionProperty != null)
            {
                exitBattleCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("退战位移补偿", exitBattleCompensateMotionProperty.boolValue);
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
            if (combatArtLeftAimSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(combatArtLeftAimSoundPrefabProperty, new GUIContent("战技左瞄准音效预制体"));
            }
            if (combatArtLeftAimCompensateMotionProperty != null)
            {
                combatArtLeftAimCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("战技左瞄准位移补偿", combatArtLeftAimCompensateMotionProperty.boolValue);
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
            if (combatArtRightAimSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(combatArtRightAimSoundPrefabProperty, new GUIContent("战技右瞄准音效预制体"));
            }
            if (combatArtRightAimCompensateMotionProperty != null)
            {
                combatArtRightAimCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("战技右瞄准位移补偿", combatArtRightAimCompensateMotionProperty.boolValue);
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
            if (hitReactionSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(hitReactionSoundPrefabProperty, new GUIContent("受击音效预制体"));
            }
            if (hitReactionCompensateMotionProperty != null)
            {
                hitReactionCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("受击位移补偿", hitReactionCompensateMotionProperty.boolValue);
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
            if (dodgeSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(dodgeSoundPrefabProperty, new GUIContent("闪避音效预制体"));
            }
            if (dodgeCompensateMotionProperty != null)
            {
                dodgeCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("闪避位移补偿", dodgeCompensateMotionProperty.boolValue);
            }

            int newExplorationIdleIndex = EditorGUILayout.Popup("探索待机动画", selectedExplorationIdleIndex, actionOptions.ToArray());
            if (explorationIdleStateNameProperty != null)
            {
                explorationIdleStateNameProperty.stringValue = newExplorationIdleIndex <= 0 ? string.Empty : actionOptions[newExplorationIdleIndex];
            }
            if (explorationIdleSoundProperty != null)
            {
                EditorGUILayout.PropertyField(explorationIdleSoundProperty, new GUIContent("探索待机音效"));
            }
            if (explorationIdleSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(explorationIdleSoundPrefabProperty, new GUIContent("探索待机音效预制体"));
            }
            if (explorationIdleCompensateMotionProperty != null)
            {
                explorationIdleCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("探索待机位移补偿", explorationIdleCompensateMotionProperty.boolValue);
            }

            int newExplorationMoveIndex = EditorGUILayout.Popup("探索移动动画", selectedExplorationMoveIndex, actionOptions.ToArray());
            if (explorationMoveStateNameProperty != null)
            {
                explorationMoveStateNameProperty.stringValue = newExplorationMoveIndex <= 0 ? string.Empty : actionOptions[newExplorationMoveIndex];
            }
            if (explorationMoveSoundProperty != null)
            {
                EditorGUILayout.PropertyField(explorationMoveSoundProperty, new GUIContent("探索移动音效"));
            }
            if (explorationMoveSoundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(explorationMoveSoundPrefabProperty, new GUIContent("探索移动音效预制体"));
            }
            if (explorationMoveCompensateMotionProperty != null)
            {
                explorationMoveCompensateMotionProperty.boolValue = EditorGUILayout.Toggle("探索移动位移补偿", explorationMoveCompensateMotionProperty.boolValue);
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
