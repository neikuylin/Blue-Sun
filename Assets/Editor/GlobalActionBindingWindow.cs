using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class GlobalActionBindingWindow : EditorWindow
{
    private const string SettingsAssetPath = "Assets/Resources/BattleAnimationSettings.asset";

    private SerializedObject settingsObject;
    private bool showIdle = true;
    private bool showEnterBattle = true;
    private bool showExitBattle = true;
    private bool showCombatArtLeftAim = false;
    private bool showCombatArtRightAim = false;
    private bool showHitReaction = false;
    private bool showDodge = false;
    private bool showExplorationIdle = true;
    private bool showExplorationMove = true;
    private bool showMisc = false;

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
        EditorGUILayout.HelpBox("按动作分组折叠。每组里包含动画、音效、音效预制体和位移补偿。", MessageType.Info);

        settingsObject.Update();

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

        DrawActionSection(
            ref showIdle,
            "待机",
            actionOptions,
            idleStateNameProperty,
            "待机动画",
            idleSoundProperty,
            "待机音效",
            idleSoundPrefabProperty,
            "待机音效预制体",
            null,
            null);

        DrawActionSection(
            ref showEnterBattle,
            "进战",
            actionOptions,
            enterBattleStateNameProperty,
            "进战动画",
            enterBattleSoundProperty,
            "进战音效",
            enterBattleSoundPrefabProperty,
            "进战音效预制体",
            enterBattleCompensateMotionProperty,
            "进战位移补偿");

        DrawActionSection(
            ref showExitBattle,
            "退战",
            actionOptions,
            exitBattleStateNameProperty,
            "退战动画",
            exitBattleSoundProperty,
            "退战音效",
            exitBattleSoundPrefabProperty,
            "退战音效预制体",
            exitBattleCompensateMotionProperty,
            "退战位移补偿");

        DrawActionSection(
            ref showCombatArtLeftAim,
            "战技左瞄准",
            actionOptions,
            combatArtLeftAimStateNameProperty,
            "战技左转身瞄准动画",
            combatArtLeftAimSoundProperty,
            "战技左瞄准音效",
            combatArtLeftAimSoundPrefabProperty,
            "战技左瞄准音效预制体",
            combatArtLeftAimCompensateMotionProperty,
            "战技左瞄准位移补偿");

        DrawActionSection(
            ref showCombatArtRightAim,
            "战技右瞄准",
            actionOptions,
            combatArtRightAimStateNameProperty,
            "战技右转身瞄准动画",
            combatArtRightAimSoundProperty,
            "战技右瞄准音效",
            combatArtRightAimSoundPrefabProperty,
            "战技右瞄准音效预制体",
            combatArtRightAimCompensateMotionProperty,
            "战技右瞄准位移补偿");

        DrawActionSection(
            ref showHitReaction,
            "受击",
            actionOptions,
            hitReactionStateNameProperty,
            "受击动画",
            hitReactionSoundProperty,
            "受击音效",
            hitReactionSoundPrefabProperty,
            "受击音效预制体",
            hitReactionCompensateMotionProperty,
            "受击位移补偿");

        DrawActionSection(
            ref showDodge,
            "闪避",
            actionOptions,
            dodgeStateNameProperty,
            "闪避动画",
            dodgeSoundProperty,
            "闪避音效",
            dodgeSoundPrefabProperty,
            "闪避音效预制体",
            dodgeCompensateMotionProperty,
            "闪避位移补偿");

        DrawActionSection(
            ref showExplorationIdle,
            "探索待机",
            actionOptions,
            explorationIdleStateNameProperty,
            "探索待机动画",
            explorationIdleSoundProperty,
            "探索待机音效",
            explorationIdleSoundPrefabProperty,
            "探索待机音效预制体",
            explorationIdleCompensateMotionProperty,
            "探索待机位移补偿");

        DrawActionSection(
            ref showExplorationMove,
            "探索移动",
            actionOptions,
            explorationMoveStateNameProperty,
            "探索移动动画",
            explorationMoveSoundProperty,
            "探索移动音效",
            explorationMoveSoundPrefabProperty,
            "探索移动音效预制体",
            explorationMoveCompensateMotionProperty,
            "探索移动位移补偿");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            showMisc = EditorGUILayout.Foldout(showMisc, "其他", true);
            if (showMisc && idleYawOffsetProperty != null)
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

    private static void DrawActionSection(
        ref bool expanded,
        string title,
        List<string> actionOptions,
        SerializedProperty stateProperty,
        string stateLabel,
        SerializedProperty soundProperty,
        string soundLabel,
        SerializedProperty soundPrefabProperty,
        string soundPrefabLabel,
        SerializedProperty compensateMotionProperty,
        string compensateMotionLabel)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            if (!expanded)
            {
                return;
            }

            int selectedIndex = FindOptionIndex(actionOptions, stateProperty != null ? stateProperty.stringValue : string.Empty);
            int newIndex = EditorGUILayout.Popup(stateLabel, selectedIndex, actionOptions.ToArray());
            if (stateProperty != null)
            {
                stateProperty.stringValue = newIndex <= 0 ? string.Empty : actionOptions[newIndex];
            }

            if (soundProperty != null)
            {
                EditorGUILayout.PropertyField(soundProperty, new GUIContent(soundLabel));
            }

            if (soundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(soundPrefabProperty, new GUIContent(soundPrefabLabel));
            }

            if (compensateMotionProperty != null && !string.IsNullOrWhiteSpace(compensateMotionLabel))
            {
                compensateMotionProperty.boolValue = EditorGUILayout.Toggle(compensateMotionLabel, compensateMotionProperty.boolValue);
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
