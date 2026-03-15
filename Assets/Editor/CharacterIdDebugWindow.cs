using System;
using System.Collections.Generic;
using UnityEditor;
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
        EditorGUILayout.LabelField("回合时间轴按钮绑定", EditorStyles.boldLabel);
        if (database == null)
        {
            EditorGUILayout.HelpBox("时间轴按钮资产创建失败。", MessageType.Error);
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
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("characterId"), new GUIContent("角色ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("buttonPrefab"), new GUIContent("时间轴按钮预制体"));
            }
        }

        if (GUILayout.Button("新增空时间轴绑定"))
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
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("characterId"), new GUIContent("角色ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"), new GUIContent("显示名"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("backgroundPortraitSprite"), new GUIContent("背景立绘"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("backgroundPortraitPrefab"), new GUIContent("背景立绘预制体"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("modelPrefab"), new GUIContent("模型预制体"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("animatorController"), new GUIContent("Animator Controller"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("cellOffset"), new GUIContent("格子偏移"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("worldOffset"), new GUIContent("世界偏移"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("useAutoVisualAnchor"), new GUIContent("自动视觉锚点"));
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
            added.FindPropertyRelative("cellOffset").vector2IntValue = Vector2Int.zero;
            added.FindPropertyRelative("worldOffset").vector3Value = Vector3.zero;
            added.FindPropertyRelative("useAutoVisualAnchor").boolValue = true;
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
            added.FindPropertyRelative("cellOffset").vector2IntValue = Vector2Int.zero;
            added.FindPropertyRelative("worldOffset").vector3Value = Vector3.zero;
            added.FindPropertyRelative("useAutoVisualAnchor").boolValue = true;
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
