using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CharacterIdDebugWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/BattleCharacterBindings.asset";

    private Vector2 scroll;
    private SerializedObject databaseObject;

    [MenuItem("Tools/\u89d2\u8272ID/\u8c03\u8bd5\u7ed1\u5b9a\u5de5\u5177")]
    private static void Open()
    {
        CharacterIdDebugWindow window = GetWindow<CharacterIdDebugWindow>("\u89d2\u8272ID\u5de5\u5177");
        window.minSize = new Vector2(620f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleCharacterBindingDatabase database = EnsureDatabase();

        EditorGUILayout.LabelField("\u89d2\u8272ID\u8c03\u8bd5\u4e0e\u6218\u6597\u6a21\u578b\u7ed1\u5b9a", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65b0"))
            {
                if (Application.isPlaying)
                {
                    CharacterSelectionState.CaptureFromCurrentScene();
                }

                Repaint();
            }

            if (GUILayout.Button("\u540c\u6b65\u5df2\u77e5ID\u5230\u7ed1\u5b9a\u8868"))
            {
                SyncKnownIds(database);
            }
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawRuntimeState();
        EditorGUILayout.Space(8f);
        DrawCharacterSlots();
        EditorGUILayout.Space(8f);
        DrawBindingTable(database);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawRuntimeState()
    {
        EditorGUILayout.LabelField("\u8fd0\u884c\u65f6\u6355\u6349", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("\u662f\u5426\u6b63\u5728\u8fd0\u884c", Application.isPlaying ? "\u662f" : "\u5426");
        EditorGUILayout.LabelField("\u5f53\u524d\u69fd\u4f4d\u6355\u6349ID", string.IsNullOrEmpty(CharacterSelectionState.ActiveCharacterId) ? "\uff08\u7a7a\uff09" : CharacterSelectionState.ActiveCharacterId);

        IReadOnlyList<CharacterSelectionState.SlotSelection> slots = CharacterSelectionState.SlotSelections;
        EditorGUILayout.LabelField("\u5df2\u6355\u6349\u69fd\u4f4d\u6570", slots.Count.ToString());
        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSelectionState.SlotSelection slot = slots[i];
            string label = slot.slotName;
            if (slot.isMainSlot)
            {
                label += " [\u4e3b\u69fd\u4f4d]";
            }

            if (slot.isActiveSlot)
            {
                label += " [\u5f53\u524d]";
            }

            EditorGUILayout.LabelField(label, string.IsNullOrEmpty(slot.characterId) ? "\uff08\u7a7a\uff09" : slot.characterId);
        }
    }

    private static void DrawCharacterSlots()
    {
        EditorGUILayout.LabelField("\u542f\u7a0b\u573a\u666f\u69fd\u4f4d", EditorStyles.boldLabel);
        CharacterSlotView[] slots = UnityEngine.Object.FindObjectsOfType<CharacterSlotView>(true);
        if (slots.Length == 0)
        {
            EditorGUILayout.HelpBox("\u5f53\u524d\u6253\u5f00\u7684\u573a\u666f\u91cc\u6ca1\u6709\u627e\u5230 CharacterSlotView\u3002", MessageType.Info);
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
                EditorGUILayout.ObjectField("\u5bf9\u8c61", slot, typeof(CharacterSlotView), true);
                EditorGUILayout.Toggle("\u4e3b\u69fd\u4f4d", slot.isMainSlot);
                EditorGUILayout.LabelField("\u5f53\u524d\u89e3\u6790ID", string.IsNullOrEmpty(CharacterSelectionState.ResolveCharacterId(slot)) ? "\uff08\u7a7a\uff09" : CharacterSelectionState.ResolveCharacterId(slot));
                EditorGUILayout.LabelField("selectedCharacterId", string.IsNullOrEmpty(slot.selectedCharacterId) ? "\uff08\u7a7a\uff09" : slot.selectedCharacterId);
                EditorGUILayout.LabelField("slotCharacterId", string.IsNullOrEmpty(slot.slotCharacterId) ? "\uff08\u7a7a\uff09" : slot.slotCharacterId);
            }
        }
    }

    private void DrawBindingTable(BattleCharacterBindingDatabase database)
    {
        EditorGUILayout.LabelField("\u6218\u6597\u6a21\u578b\u7ed1\u5b9a", EditorStyles.boldLabel);
        if (database == null)
        {
            EditorGUILayout.HelpBox("\u7ed1\u5b9a\u8d44\u4ea7\u521b\u5efa\u5931\u8d25\u3002", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();
        SerializedProperty entries = databaseObject.FindProperty("entries");
        EnsureKnownIdsInProperty(entries);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("characterId"), new GUIContent("\u89d2\u8272ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"), new GUIContent("\u663e\u793a\u540d"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("modelPrefab"), new GUIContent("\u6a21\u578b\u9884\u5236\u4f53"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("animatorController"), new GUIContent("Animator Controller"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("cellOffset"), new GUIContent("\u683c\u5b50\u504f\u79fb"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("worldOffset"), new GUIContent("\u4e16\u754c\u504f\u79fb"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("useAutoVisualAnchor"), new GUIContent("\u81ea\u52a8\u89c6\u89c9\u951a\u70b9"));
            }
        }

        if (GUILayout.Button("\u65b0\u589e\u7a7a\u7ed1\u5b9a"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            added.FindPropertyRelative("characterId").stringValue = string.Empty;
            added.FindPropertyRelative("displayName").stringValue = string.Empty;
            added.FindPropertyRelative("modelPrefab").objectReferenceValue = null;
            added.FindPropertyRelative("animatorController").objectReferenceValue = null;
            added.FindPropertyRelative("cellOffset").vector2IntValue = Vector2Int.zero;
            added.FindPropertyRelative("worldOffset").vector3Value = Vector3.zero;
            added.FindPropertyRelative("useAutoVisualAnchor").boolValue = true;
        }

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static BattleCharacterBindingDatabase EnsureDatabase()
    {
        BattleCharacterBindingDatabase database = AssetDatabase.LoadAssetAtPath<BattleCharacterBindingDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<BattleCharacterBindingDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
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
        ids.Add("\u73a9\u5bb6");

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
