using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class CharacterIdDebugWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string BindingAssetPath = AssetFolder + "/BattleCharacterBindings.asset";
    private const string TimelineAssetPath = AssetFolder + "/TurnTimelineButtonDatabase.asset";

    private Vector2 scroll;
    private SerializedObject bindingDatabaseObject;
    private SerializedObject timelineDatabaseObject;

    [MenuItem("Tools/角色ID/调试绑定工具")]
    private static void Open()
    {
        CharacterIdDebugWindow window = GetWindow<CharacterIdDebugWindow>("角色ID工具");
        window.minSize = new Vector2(680f, 620f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleCharacterBindingDatabase bindingDatabase = EnsureBindingDatabase();
        TurnTimelineButtonDatabase timelineDatabase = EnsureTimelineDatabase();

        EditorGUILayout.LabelField("角色ID调试与绑定工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                if (Application.isPlaying)
                {
                    CharacterSelectionState.CaptureFromCurrentScene();
                }

                Repaint();
            }

            if (GUILayout.Button("同步已知ID到绑定表"))
            {
                SyncKnownIds(bindingDatabase);
            }
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawRuntimeState();
        EditorGUILayout.Space(8f);
        DrawCharacterSlots();
        EditorGUILayout.Space(8f);
        DrawTimelineBindingTable(timelineDatabase);
        EditorGUILayout.Space(8f);
        DrawBindingTable(bindingDatabase);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawRuntimeState()
    {
        EditorGUILayout.LabelField("运行时捕捉", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("是否正在运行", Application.isPlaying ? "是" : "否");
        EditorGUILayout.LabelField("当前槽位捕捉ID", string.IsNullOrEmpty(CharacterSelectionState.ActiveCharacterId) ? "（空）" : CharacterSelectionState.ActiveCharacterId);

        IReadOnlyList<CharacterSelectionState.SlotSelection> slots = CharacterSelectionState.SlotSelections;
        EditorGUILayout.LabelField("已捕捉槽位数", slots.Count.ToString());
        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSelectionState.SlotSelection slot = slots[i];
            string label = slot.slotName;
            if (slot.isMainSlot)
            {
                label += " [主槽位]";
            }

            if (slot.isActiveSlot)
            {
                label += " [当前]";
            }

            EditorGUILayout.LabelField(label, string.IsNullOrEmpty(slot.characterId) ? "（空）" : slot.characterId);
        }
    }

    private static void DrawCharacterSlots()
    {
        EditorGUILayout.LabelField("启程场景槽位", EditorStyles.boldLabel);
        CharacterSlotView[] slots = UnityEngine.Object.FindObjectsOfType<CharacterSlotView>(true);
        if (slots.Length == 0)
        {
            EditorGUILayout.HelpBox("当前打开的场景里没有找到 CharacterSlotView。", MessageType.Info);
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.ObjectField("对象", slot, typeof(CharacterSlotView), true);
                EditorGUILayout.Toggle("主槽位", slot.isMainSlot);
                EditorGUILayout.LabelField("当前解析ID", string.IsNullOrEmpty(CharacterSelectionState.ResolveCharacterId(slot)) ? "（空）" : CharacterSelectionState.ResolveCharacterId(slot));
                EditorGUILayout.LabelField("selectedCharacterId", string.IsNullOrEmpty(slot.selectedCharacterId) ? "（空）" : slot.selectedCharacterId);
                EditorGUILayout.LabelField("slotCharacterId", string.IsNullOrEmpty(slot.slotCharacterId) ? "（空）" : slot.slotCharacterId);
            }
        }
    }

    private void DrawTimelineBindingTable(TurnTimelineButtonDatabase database)
    {
        EditorGUILayout.LabelField("回合时间轴头像绑定", EditorStyles.boldLabel);
        if (database == null)
        {
            EditorGUILayout.HelpBox("时间轴头像资产创建失败。", MessageType.Error);
            return;
        }

        if (timelineDatabaseObject == null || timelineDatabaseObject.targetObject != database)
        {
            timelineDatabaseObject = new SerializedObject(database);
        }

        timelineDatabaseObject.Update();
        SerializedProperty entries = timelineDatabaseObject.FindProperty("entries");
        EnsureKnownTimelineIdsInProperty(entries);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string characterId = entry.FindPropertyRelative("characterId").stringValue;
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(characterId) ? $"时间轴头像 {i + 1}" : characterId, EditorStyles.boldLabel);

                    if (GUILayout.Button("删除绑定", GUILayout.Width(88f)))
                    {
                        entries.DeleteArrayElementAtIndex(i);
                        timelineDatabaseObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(database);
                        AssetDatabase.SaveAssets();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("characterId"), new GUIContent("角色ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("buttonPrefab"), new GUIContent("时间轴头像预制体"));
            }
        }

        if (GUILayout.Button("新增空时间轴头像绑定"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            added.FindPropertyRelative("characterId").stringValue = string.Empty;
            added.FindPropertyRelative("buttonPrefab").objectReferenceValue = null;
        }

        if (timelineDatabaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawBindingTable(BattleCharacterBindingDatabase database)
    {
        EditorGUILayout.LabelField("战斗模型绑定", EditorStyles.boldLabel);
        if (database == null)
        {
            EditorGUILayout.HelpBox("绑定资产创建失败。", MessageType.Error);
            return;
        }

        if (bindingDatabaseObject == null || bindingDatabaseObject.targetObject != database)
        {
            bindingDatabaseObject = new SerializedObject(database);
        }

        bindingDatabaseObject.Update();
        SerializedProperty entries = bindingDatabaseObject.FindProperty("entries");
        EnsureKnownIdsInProperty(entries);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string characterId = entry.FindPropertyRelative("characterId").stringValue;
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(characterId) ? $"绑定 {i + 1}" : characterId, EditorStyles.boldLabel);

                    if (GUILayout.Button("删除绑定", GUILayout.Width(88f)))
                    {
                        entries.DeleteArrayElementAtIndex(i);
                        bindingDatabaseObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(database);
                        AssetDatabase.SaveAssets();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("characterId"), new GUIContent("角色ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"), new GUIContent("显示名"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("backgroundPortraitSprite"), new GUIContent("背景立绘"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("backgroundPortraitPrefab"), new GUIContent("背景立绘预制体"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("modelPrefab"), new GUIContent("模型预制体"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("animatorController"), new GUIContent("Animator Controller"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("modelScale"), new GUIContent("模型缩放"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("useAutoVisualAnchor"), new GUIContent("自动视觉锚点"));
                DrawAnimationBindings(entry);
                DrawAnimatorBindingStatus(entry);
            }
        }

        if (GUILayout.Button("新增空绑定"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            added.FindPropertyRelative("characterId").stringValue = string.Empty;
            added.FindPropertyRelative("displayName").stringValue = string.Empty;
            added.FindPropertyRelative("backgroundPortraitSprite").objectReferenceValue = null;
            added.FindPropertyRelative("backgroundPortraitPrefab").objectReferenceValue = null;
            added.FindPropertyRelative("modelPrefab").objectReferenceValue = null;
            added.FindPropertyRelative("animatorController").objectReferenceValue = null;
            added.FindPropertyRelative("modelScale").vector3Value = Vector3.one;
            added.FindPropertyRelative("useAutoVisualAnchor").boolValue = true;
            added.FindPropertyRelative("idleStateName").stringValue = string.Empty;
            added.FindPropertyRelative("enterBattleStateName").stringValue = string.Empty;
            added.FindPropertyRelative("moveStateName").stringValue = string.Empty;
            added.FindPropertyRelative("hitReactionStateName").stringValue = string.Empty;
            added.FindPropertyRelative("dodgeStateName").stringValue = string.Empty;
            added.FindPropertyRelative("combatArtLeftAimStateName").stringValue = string.Empty;
            added.FindPropertyRelative("combatArtRightAimStateName").stringValue = string.Empty;
        }

        if (bindingDatabaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static BattleCharacterBindingDatabase EnsureBindingDatabase()
    {
        BattleCharacterBindingDatabase database = AssetDatabase.LoadAssetAtPath<BattleCharacterBindingDatabase>(BindingAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureAssetFolder();
        database = CreateInstance<BattleCharacterBindingDatabase>();
        AssetDatabase.CreateAsset(database, BindingAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static TurnTimelineButtonDatabase EnsureTimelineDatabase()
    {
        TurnTimelineButtonDatabase database = AssetDatabase.LoadAssetAtPath<TurnTimelineButtonDatabase>(TimelineAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureAssetFolder();
        database = CreateInstance<TurnTimelineButtonDatabase>();
        AssetDatabase.CreateAsset(database, TimelineAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureAssetFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static void SyncKnownIds(BattleCharacterBindingDatabase database)
    {
        if (database == null)
        {
            return;
        }

        List<string> knownIds = CollectKnownIds(database);
        bool changed = false;
        foreach (string id in knownIds)
        {
            if (database.FindBinding(id) != null)
            {
                continue;
            }

            database.Entries.Add(new BattleCharacterBindingDatabase.BindingEntry
            {
                characterId = id,
                displayName = id,
                useAutoVisualAnchor = true
            });
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureKnownTimelineIdsInProperty(SerializedProperty entries)
    {
        List<string> knownIds = CollectKnownIds(null);
        for (int i = 0; i < knownIds.Count; i++)
        {
            if (ContainsCharacterId(entries, knownIds[i]))
            {
                continue;
            }

            entries.InsertArrayElementAtIndex(entries.arraySize);
            SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            added.FindPropertyRelative("characterId").stringValue = knownIds[i];
            added.FindPropertyRelative("buttonPrefab").objectReferenceValue = null;
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
            SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            added.FindPropertyRelative("characterId").stringValue = knownIds[i];
            added.FindPropertyRelative("displayName").stringValue = knownIds[i];
            added.FindPropertyRelative("backgroundPortraitSprite").objectReferenceValue = null;
            added.FindPropertyRelative("backgroundPortraitPrefab").objectReferenceValue = null;
            added.FindPropertyRelative("modelPrefab").objectReferenceValue = null;
            added.FindPropertyRelative("animatorController").objectReferenceValue = null;
            added.FindPropertyRelative("modelScale").vector3Value = Vector3.one;
            added.FindPropertyRelative("useAutoVisualAnchor").boolValue = true;
            added.FindPropertyRelative("idleStateName").stringValue = string.Empty;
            added.FindPropertyRelative("enterBattleStateName").stringValue = string.Empty;
            added.FindPropertyRelative("moveStateName").stringValue = string.Empty;
            added.FindPropertyRelative("hitReactionStateName").stringValue = string.Empty;
            added.FindPropertyRelative("dodgeStateName").stringValue = string.Empty;
            added.FindPropertyRelative("combatArtLeftAimStateName").stringValue = string.Empty;
            added.FindPropertyRelative("combatArtRightAimStateName").stringValue = string.Empty;
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

    private static void DrawAnimatorBindingStatus(SerializedProperty entry)
    {
        SerializedProperty modelPrefabProperty = entry.FindPropertyRelative("modelPrefab");
        SerializedProperty animatorControllerProperty = entry.FindPropertyRelative("animatorController");

        GameObject modelPrefab = modelPrefabProperty.objectReferenceValue as GameObject;
        RuntimeAnimatorController controller = animatorControllerProperty.objectReferenceValue as RuntimeAnimatorController;

        Animator prefabAnimator = modelPrefab != null ? modelPrefab.GetComponentInChildren<Animator>(true) : null;
        Avatar avatar = prefabAnimator != null ? prefabAnimator.avatar : null;

        if (modelPrefab == null)
        {
            EditorGUILayout.HelpBox("未绑定模型预制体。运行时会退回占位模型，也不会应用动画控制器。", MessageType.Info);
            return;
        }

        if (prefabAnimator == null)
        {
            EditorGUILayout.HelpBox("当前模型预制体里没有 Animator。运行时会自动补一个 Animator，但如果模型导入设置没有 Avatar，Humanoid 动画仍然不会正常播放。", MessageType.Warning);
        }
        else if (avatar == null)
        {
            EditorGUILayout.HelpBox("模型里找到了 Animator，但 Avatar 为空。Humanoid 控制器通常无法正常驱动这个模型。", MessageType.Warning);
        }

        if (controller == null)
        {
            EditorGUILayout.HelpBox("未绑定 Animator Controller。", MessageType.Info);
            return;
        }

        AnimatorController animatorController = controller as AnimatorController;
        if (animatorController == null)
        {
            EditorGUILayout.HelpBox($"已绑定控制器：{controller.name}", MessageType.None);
            return;
        }

        if (animatorController.layers == null || animatorController.layers.Length == 0)
        {
            EditorGUILayout.HelpBox($"控制器 {controller.name} 没有动画层。", MessageType.Warning);
            return;
        }

        int stateCount = 0;
        for (int i = 0; i < animatorController.layers.Length; i++)
        {
            if (animatorController.layers[i].stateMachine != null)
            {
                stateCount += animatorController.layers[i].stateMachine.states.Length;
            }
        }

        EditorGUILayout.HelpBox($"控制器已绑定：{controller.name}，层数 {animatorController.layers.Length}，状态数 {stateCount}。", MessageType.None);
    }

    private static void DrawAnimationBindings(SerializedProperty entry)
    {
        RuntimeAnimatorController controller = entry.FindPropertyRelative("animatorController").objectReferenceValue as RuntimeAnimatorController;
        AnimatorController animatorController = controller as AnimatorController;
        if (animatorController == null)
        {
            return;
        }

        List<string> stateNames = CollectAnimatorStateNames(animatorController);
        string defaultStateName = ResolveDefaultStateName(animatorController);
        entry.FindPropertyRelative("idleStateName").stringValue = defaultStateName;

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("动画绑定", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("待机状态（默认状态）", string.IsNullOrWhiteSpace(defaultStateName) ? "（空）" : defaultStateName);

        DrawAnimationStatePopup(entry.FindPropertyRelative("enterBattleStateName"), "入场动画", stateNames);
        DrawAnimationStatePopup(entry.FindPropertyRelative("moveStateName"), "移动动画", stateNames);
        DrawAnimationStatePopup(entry.FindPropertyRelative("hitReactionStateName"), "受击动画", stateNames);
        DrawAnimationStatePopup(entry.FindPropertyRelative("dodgeStateName"), "闪避动画", stateNames);
        DrawAnimationStatePopup(entry.FindPropertyRelative("combatArtLeftAimStateName"), "左瞄准动画", stateNames);
        DrawAnimationStatePopup(entry.FindPropertyRelative("combatArtRightAimStateName"), "右瞄准动画", stateNames);
    }

    private static void DrawAnimationStatePopup(SerializedProperty property, string label, List<string> stateNames)
    {
        string currentValue = property.stringValue;
        string[] options = new string[stateNames.Count + 1];
        options[0] = "（空）";
        int selectedIndex = 0;
        for (int i = 0; i < stateNames.Count; i++)
        {
            string option = stateNames[i];
            options[i + 1] = option;
            if (string.Equals(option, currentValue, StringComparison.Ordinal))
            {
                selectedIndex = i + 1;
            }
        }

        int nextIndex = EditorGUILayout.Popup(label, selectedIndex, options);
        property.stringValue = nextIndex <= 0 ? string.Empty : options[nextIndex];
    }

    private static string ResolveDefaultStateName(AnimatorController controller)
    {
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
        {
            return string.Empty;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine == null || stateMachine.defaultState == null)
        {
            return string.Empty;
        }

        return stateMachine.defaultState.name ?? string.Empty;
    }

    private static List<string> CollectAnimatorStateNames(AnimatorController controller)
    {
        List<string> stateNames = new List<string>();
        if (controller == null || controller.layers == null)
        {
            return stateNames;
        }

        HashSet<string> uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < controller.layers.Length; i++)
        {
            AnimatorStateMachine stateMachine = controller.layers[i].stateMachine;
            if (stateMachine == null)
            {
                continue;
            }

            CollectAnimatorStateNamesRecursive(stateMachine, uniqueNames, stateNames);
        }

        stateNames.Sort(StringComparer.Ordinal);
        return stateNames;
    }

    private static void CollectAnimatorStateNamesRecursive(
        AnimatorStateMachine stateMachine,
        HashSet<string> uniqueNames,
        List<string> stateNames)
    {
        if (stateMachine == null)
        {
            return;
        }

        ChildAnimatorState[] childStates = stateMachine.states;
        for (int i = 0; i < childStates.Length; i++)
        {
            AnimatorState state = childStates[i].state;
            if (state == null || string.IsNullOrWhiteSpace(state.name))
            {
                continue;
            }

            if (uniqueNames.Add(state.name))
            {
                stateNames.Add(state.name);
            }
        }

        ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
        for (int i = 0; i < childStateMachines.Length; i++)
        {
            CollectAnimatorStateNamesRecursive(childStateMachines[i].stateMachine, uniqueNames, stateNames);
        }
    }

    private static List<string> CollectKnownIds(BattleCharacterBindingDatabase database)
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

        if (database != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                BattleCharacterBindingDatabase.BindingEntry entry = database.Entries[i];
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
