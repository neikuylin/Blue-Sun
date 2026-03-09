using UnityEditor;
using UnityEngine;

public sealed class CharacterIdDebugWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Tools/角色ID/调试绑定工具")]
    private static void Open()
    {
        CharacterIdDebugWindow window = GetWindow<CharacterIdDebugWindow>("角色ID工具");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("角色 ID 调试器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (GUILayout.Button("刷新"))
        {
            Repaint();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawRuntimeState();
        EditorGUILayout.Space(8f);
        DrawCharacterEntries();
        EditorGUILayout.Space(8f);
        DrawCharacterSlots();
        EditorGUILayout.Space(8f);
        DrawBattleBindings();
        EditorGUILayout.EndScrollView();
    }

    private static void DrawRuntimeState()
    {
        EditorGUILayout.LabelField("运行时选择状态", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("是否正在运行", Application.isPlaying ? "是" : "否");
        EditorGUILayout.LabelField("主选角色 ID", string.IsNullOrEmpty(CharacterSelectionState.PrimaryCharacterId) ? "（空）" : CharacterSelectionState.PrimaryCharacterId);
        EditorGUILayout.LabelField("当前激活角色 ID", string.IsNullOrEmpty(CharacterSelectionState.ActiveCharacterId) ? "（空）" : CharacterSelectionState.ActiveCharacterId);

        var slots = CharacterSelectionState.SlotSelections;
        EditorGUILayout.LabelField("已捕获槽位数", slots.Count.ToString());
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            EditorGUILayout.LabelField(slot.slotName, string.IsNullOrEmpty(slot.characterId) ? "（空）" : slot.characterId);
        }
    }

    private static void DrawCharacterEntries()
    {
        EditorGUILayout.LabelField("启程场景角色入口", EditorStyles.boldLabel);
        CharacterSelectEntry[] entries = Object.FindObjectsOfType<CharacterSelectEntry>(true);
        if (entries.Length == 0)
        {
            EditorGUILayout.HelpBox("当前打开的场景里没有找到 CharacterSelectEntry。", MessageType.Info);
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            CharacterSelectEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.ObjectField("对象", entry, typeof(CharacterSelectEntry), true);
                string newId = EditorGUILayout.TextField("角色 ID", entry.characterId);
                if (newId != entry.characterId)
                {
                    Undo.RecordObject(entry, "修改角色 ID");
                    entry.characterId = newId;
                    EditorUtility.SetDirty(entry);
                }
            }
        }
    }

    private static void DrawCharacterSlots()
    {
        EditorGUILayout.LabelField("启程场景角色槽位", EditorStyles.boldLabel);
        CharacterSlotView[] slots = Object.FindObjectsOfType<CharacterSlotView>(true);
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
                string newId = EditorGUILayout.TextField("默认槽位 ID", slot.slotCharacterId);
                if (newId != slot.slotCharacterId)
                {
                    Undo.RecordObject(slot, "修改槽位角色 ID");
                    slot.slotCharacterId = newId;
                    EditorUtility.SetDirty(slot);
                }
            }
        }
    }

    private static void DrawBattleBindings()
    {
        EditorGUILayout.LabelField("20x20 战斗绑定", EditorStyles.boldLabel);
        BattleBootstrap bootstrap = Object.FindObjectOfType<BattleBootstrap>(true);
        if (bootstrap == null)
        {
            EditorGUILayout.HelpBox("当前打开的场景里没有找到 BattleBootstrap。", MessageType.Info);
            return;
        }

        SerializedObject serializedObject = new SerializedObject(bootstrap);
        SerializedProperty bindings = serializedObject.FindProperty("playerBindings");
        if (bindings != null)
        {
            EditorGUILayout.PropertyField(bindings, true);
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("选中 BattleBootstrap"))
        {
            Selection.activeObject = bootstrap.gameObject;
            EditorGUIUtility.PingObject(bootstrap.gameObject);
        }
    }
}
