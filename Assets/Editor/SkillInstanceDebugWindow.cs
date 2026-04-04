using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class SkillInstanceDebugWindow : EditorWindow
{
    private Vector2 scroll;
    private int selectedCharacterIndex;
    private string manualCharacterId = string.Empty;
    private List<string> characterIds = new List<string>();

    [MenuItem("Tools/技能/现有技能实例")]
    private static void Open()
    {
        SkillInstanceDebugWindow window = GetWindow<SkillInstanceDebugWindow>();
        window.titleContent = new GUIContent("现有技能实例");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        RebuildCharacterIds();
    }

    private void OnFocus()
    {
        RebuildCharacterIds();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("现有技能实例", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "这里读取的是运行时当前角色技能栏的技能快照，不是 BattleSkillDatabase 里的定义本体。",
            MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play 模式后再查看当前技能实例。", MessageType.Info);
            return;
        }

        DrawCharacterSelector();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(80f)))
            {
                RebuildCharacterIds();
                Repaint();
            }
        }

        List<BattleSkillPaginationBinder.SkillInstanceSnapshot> snapshots =
            BattleSkillPaginationBinder.GetSkillSnapshotsForCharacter(GetSelectedCharacterId());

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (snapshots == null || snapshots.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有可读取的技能实例。", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                BattleSkillPaginationBinder.SkillInstanceSnapshot snapshot = snapshots[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"槽位 {snapshot.index}");
                    if (snapshot.isEmpty)
                    {
                        EditorGUILayout.LabelField("状态", "空");
                        continue;
                    }

                    EditorGUILayout.LabelField("技能ID", snapshot.skillId);
                    EditorGUILayout.LabelField("技能名字", snapshot.displayName);
                    EditorGUILayout.LabelField("技能描述", snapshot.description);
                    EditorGUILayout.LabelField("使用者", snapshot.ownerCharacterId);
                    EditorGUILayout.LabelField("来源", snapshot.source);
                    EditorGUILayout.LabelField("技能倍率", snapshot.damageMultiplier.ToString("0.##"));
                    EditorGUILayout.LabelField("技能伤害", snapshot.damage.ToString());
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCharacterSelector()
    {
        if (characterIds.Count == 0)
        {
            RebuildCharacterIds();
        }

        if (characterIds.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有可选角色ID。", MessageType.Info);
            manualCharacterId = EditorGUILayout.TextField("角色ID", manualCharacterId);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        int nextIndex = EditorGUILayout.Popup("角色ID", selectedCharacterIndex, characterIds.ToArray());
        if (nextIndex != selectedCharacterIndex)
        {
            selectedCharacterIndex = nextIndex;
            manualCharacterId = characterIds[selectedCharacterIndex];
        }

        string nextManualId = EditorGUILayout.TextField("手动输入", string.IsNullOrWhiteSpace(manualCharacterId)
            ? characterIds[selectedCharacterIndex]
            : manualCharacterId);
        if (!string.Equals(nextManualId, manualCharacterId, StringComparison.Ordinal))
        {
            manualCharacterId = nextManualId;
            int foundIndex = characterIds.FindIndex(id => string.Equals(id, manualCharacterId, StringComparison.Ordinal));
            if (foundIndex >= 0)
            {
                selectedCharacterIndex = foundIndex;
            }
        }
    }

    private string GetSelectedCharacterId()
    {
        if (!string.IsNullOrWhiteSpace(manualCharacterId))
        {
            return manualCharacterId;
        }

        if (characterIds.Count == 0)
        {
            return string.Empty;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        return characterIds[selectedCharacterIndex];
    }

    private void RebuildCharacterIds()
    {
        characterIds.Clear();

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        if (statDatabase != null && statDatabase.Entries != null)
        {
            for (int i = 0; i < statDatabase.Entries.Count; i++)
            {
                CharacterStatDatabase.StatEntry entry = statDatabase.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.characterId) || characterIds.Contains(entry.characterId))
                {
                    continue;
                }

                characterIds.Add(entry.characterId);
            }
        }

        if (characterIds.Count == 0)
        {
            manualCharacterId = string.Empty;
            selectedCharacterIndex = 0;
            return;
        }

        int manualIndex = !string.IsNullOrWhiteSpace(manualCharacterId)
            ? characterIds.FindIndex(id => string.Equals(id, manualCharacterId, StringComparison.Ordinal))
            : -1;
        if (manualIndex >= 0)
        {
            selectedCharacterIndex = manualIndex;
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        manualCharacterId = characterIds[selectedCharacterIndex];
    }
}
