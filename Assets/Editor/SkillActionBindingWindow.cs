using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class SkillActionBindingWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/BattleSkillDatabase.asset";

    private Vector2 scroll;
    private SerializedObject databaseObject;

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
        BattleSkillDatabase database = AssetDatabase.LoadAssetAtPath<BattleSkillDatabase>(AssetPath);
        if (database == null)
        {
            EditorGUILayout.HelpBox("未找到 BattleSkillDatabase.asset。请先打开技能编辑器创建技能库。", MessageType.Warning);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        List<string> actionOptions = BuildActionOptions();

        EditorGUILayout.LabelField("技能动作栏", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("把技能绑定到 Animator 状态名。动作选项来自 BattleCharacterBindings 里已绑定控制器的状态。", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        databaseObject.Update();
        SerializedProperty entries = databaseObject.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            DrawSkillRow(entry, actionOptions);
        }

        EditorGUILayout.EndScrollView();

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
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
            if (controller == null)
            {
                continue;
            }

            CollectStates(controller.layers, options, seen);
        }

        return options;
    }

    private static void CollectStates(AnimatorControllerLayer[] layers, List<string> options, HashSet<string> seen)
    {
        if (layers == null)
        {
            return;
        }

        for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            AnimatorStateMachine stateMachine = layers[layerIndex].stateMachine;
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
}
