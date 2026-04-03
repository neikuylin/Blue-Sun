using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class GlobalActionBindingWindow : EditorWindow
{
    private const string SettingsAssetPath = "Assets/Resources/BattleAnimationSettings.asset";

    private static readonly ItemDatabase.WeaponCategory[] ScopedWeaponCategories =
    {
        ItemDatabase.WeaponCategory.None,
        ItemDatabase.WeaponCategory.OneHanded,
        ItemDatabase.WeaponCategory.TwoHanded,
        ItemDatabase.WeaponCategory.Bow,
        ItemDatabase.WeaponCategory.Staff
    };

    private SerializedObject settingsObject;
    private bool showIdle = true;
    private bool showEnterBattle = true;
    private bool showExitBattle = true;
    private bool showHitReaction;
    private bool showDodge;
    private bool showExplorationIdle = true;
    private bool showExplorationMove = true;
    private bool showMisc;
    private Vector2 scroll;

    [MenuItem("Tools/技能/全局动作")]
    private static void Open()
    {
        GlobalActionBindingWindow window = GetWindow<GlobalActionBindingWindow>("全局动作");
        window.minSize = new Vector2(860f, 520f);
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
        settingsObject.Update();

        EditorGUILayout.LabelField("全局动作", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("除探索待机和探索移动外，所有全局动作都按武器分类单独配置。没有武器也要单独配一套。这里不做回退。", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawScopedSection(ref showIdle, "待机", "idleOverrides", actionOptions);
        DrawScopedSection(ref showEnterBattle, "进战", "enterBattleOverrides", actionOptions);
        DrawScopedSection(ref showExitBattle, "退战", "exitBattleOverrides", actionOptions);
        DrawScopedSection(ref showHitReaction, "受击", "hitReactionOverrides", actionOptions);
        DrawScopedSection(ref showDodge, "闪避", "dodgeOverrides", actionOptions);
        DrawSimpleSection(ref showExplorationIdle, "探索待机", "explorationIdleStateName", "explorationIdleSound", "explorationIdleSoundPrefab", "explorationIdleCompensateMotion", actionOptions);
        DrawSimpleSection(ref showExplorationMove, "探索移动", "explorationMoveStateName", "explorationMoveSound", "explorationMoveSoundPrefab", "explorationMoveCompensateMotion", actionOptions);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            showMisc = EditorGUILayout.Foldout(showMisc, "其他", true);
            if (showMisc)
            {
                SerializedProperty idleYawOffsetProperty = settingsObject.FindProperty("idleYawOffset");
                if (idleYawOffsetProperty != null)
                {
                    idleYawOffsetProperty.floatValue = EditorGUILayout.FloatField("待机角度修正", idleYawOffsetProperty.floatValue);
                }
            }
        }

        EditorGUILayout.EndScrollView();

        if (settingsObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawScopedSection(ref bool expanded, string title, string arrayPropertyName, List<string> actionOptions)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            if (!expanded)
            {
                return;
            }

            SerializedProperty arrayProperty = settingsObject.FindProperty(arrayPropertyName);
            if (arrayProperty == null)
            {
                EditorGUILayout.HelpBox($"未找到配置字段: {arrayPropertyName}", MessageType.Error);
                return;
            }

            EnsureScopedOverrideArray(arrayProperty);

            for (int i = 0; i < ScopedWeaponCategories.Length; i++)
            {
                SerializedProperty entry = arrayProperty.GetArrayElementAtIndex(i);
                DrawScopedEntry(entry, ScopedWeaponCategories[i], actionOptions);
            }
        }
    }

    private void DrawSimpleSection(
        ref bool expanded,
        string title,
        string statePropertyName,
        string soundPropertyName,
        string soundPrefabPropertyName,
        string compensateMotionPropertyName,
        List<string> actionOptions)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            if (!expanded)
            {
                return;
            }

            SerializedProperty stateProperty = settingsObject.FindProperty(statePropertyName);
            SerializedProperty soundProperty = settingsObject.FindProperty(soundPropertyName);
            SerializedProperty soundPrefabProperty = settingsObject.FindProperty(soundPrefabPropertyName);
            SerializedProperty compensateMotionProperty = settingsObject.FindProperty(compensateMotionPropertyName);

            DrawStatePopup("动画", stateProperty, actionOptions);
            if (soundProperty != null)
            {
                EditorGUILayout.PropertyField(soundProperty, new GUIContent("音效"));
            }

            if (soundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(soundPrefabProperty, new GUIContent("音效预制体"));
            }

            if (compensateMotionProperty != null)
            {
                compensateMotionProperty.boolValue = EditorGUILayout.Toggle("位移补偿", compensateMotionProperty.boolValue);
            }
        }
    }

    private static void DrawScopedEntry(SerializedProperty entry, ItemDatabase.WeaponCategory weaponCategory, List<string> actionOptions)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty enabledProperty = entry.FindPropertyRelative("enabled");
        SerializedProperty weaponCategoryProperty = entry.FindPropertyRelative("weaponCategory");
        SerializedProperty stateProperty = entry.FindPropertyRelative("stateName");
        SerializedProperty soundProperty = entry.FindPropertyRelative("sound");
        SerializedProperty soundPrefabProperty = entry.FindPropertyRelative("soundPrefab");
        SerializedProperty compensateMotionProperty = entry.FindPropertyRelative("compensateMotion");

        if (weaponCategoryProperty != null)
        {
            weaponCategoryProperty.enumValueIndex = (int)weaponCategory;
        }

        using (new EditorGUI.IndentLevelScope())
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(GetWeaponCategoryLabel(weaponCategory), EditorStyles.boldLabel);

            if (enabledProperty != null)
            {
                enabledProperty.boolValue = EditorGUILayout.Toggle("启用", enabledProperty.boolValue);
            }

            DrawStatePopup("动画", stateProperty, actionOptions);

            if (soundProperty != null)
            {
                EditorGUILayout.PropertyField(soundProperty, new GUIContent("音效"));
            }

            if (soundPrefabProperty != null)
            {
                EditorGUILayout.PropertyField(soundPrefabProperty, new GUIContent("音效预制体"));
            }

            if (compensateMotionProperty != null)
            {
                compensateMotionProperty.boolValue = EditorGUILayout.Toggle("位移补偿", compensateMotionProperty.boolValue);
            }
        }
    }

    private static void DrawStatePopup(string label, SerializedProperty stateProperty, List<string> actionOptions)
    {
        if (stateProperty == null)
        {
            return;
        }

        int selectedIndex = FindOptionIndex(actionOptions, stateProperty.stringValue);
        int newIndex = EditorGUILayout.Popup(label, selectedIndex, actionOptions.ToArray());
        stateProperty.stringValue = newIndex <= 0 ? string.Empty : actionOptions[newIndex];
    }

    private static void EnsureScopedOverrideArray(SerializedProperty arrayProperty)
    {
        if (arrayProperty == null)
        {
            return;
        }

        int originalSize = arrayProperty.arraySize;
        while (arrayProperty.arraySize < ScopedWeaponCategories.Length)
        {
            arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
        }

        for (int i = 0; i < ScopedWeaponCategories.Length; i++)
        {
            SerializedProperty entry = arrayProperty.GetArrayElementAtIndex(i);
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
                weaponCategoryProperty.enumValueIndex = (int)ScopedWeaponCategories[i];
            }

            if (i >= originalSize)
            {
                ClearScopedEntry(entry);
            }
        }
    }

    private static void ClearScopedEntry(SerializedProperty entry)
    {
        if (entry == null)
        {
            return;
        }

        SerializedProperty stateProperty = entry.FindPropertyRelative("stateName");
        if (stateProperty != null)
        {
            stateProperty.stringValue = string.Empty;
        }

        SerializedProperty soundProperty = entry.FindPropertyRelative("sound");
        if (soundProperty != null)
        {
            soundProperty.objectReferenceValue = null;
        }

        SerializedProperty soundPrefabProperty = entry.FindPropertyRelative("soundPrefab");
        if (soundPrefabProperty != null)
        {
            soundPrefabProperty.objectReferenceValue = null;
        }

        SerializedProperty compensateMotionProperty = entry.FindPropertyRelative("compensateMotion");
        if (compensateMotionProperty != null)
        {
            compensateMotionProperty.boolValue = false;
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
