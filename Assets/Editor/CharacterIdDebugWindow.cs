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
            if (Application.isPlaying)
            {
                CharacterSelectionState.CaptureFromCurrentScene();
            }

            Repaint();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawRuntimeState();
        EditorGUILayout.Space(8f);
        DrawCharacterSlots();
        EditorGUILayout.EndScrollView();
    }

    private static void DrawRuntimeState()
    {
        EditorGUILayout.LabelField("运行时捕捉", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("是否正在运行", Application.isPlaying ? "是" : "否");
        EditorGUILayout.LabelField("当前槽位捕捉ID", string.IsNullOrEmpty(CharacterSelectionState.ActiveCharacterId) ? "（空）" : CharacterSelectionState.ActiveCharacterId);

        var slots = CharacterSelectionState.SlotSelections;
        EditorGUILayout.LabelField("已捕捉槽位数", slots.Count.ToString());
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            string label = slot.slotName + (slot.isMainSlot ? " [主槽位]" : "");
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
                EditorGUILayout.LabelField("当前解析ID", string.IsNullOrEmpty(CharacterSelectionState.ResolveCharacterId(slot)) ? "（空）" : CharacterSelectionState.ResolveCharacterId(slot));
                EditorGUILayout.LabelField("selectedCharacterId", string.IsNullOrEmpty(slot.selectedCharacterId) ? "（空）" : slot.selectedCharacterId);
                EditorGUILayout.LabelField("slotCharacterId", string.IsNullOrEmpty(slot.slotCharacterId) ? "（空）" : slot.slotCharacterId);
            }
        }
    }
}
