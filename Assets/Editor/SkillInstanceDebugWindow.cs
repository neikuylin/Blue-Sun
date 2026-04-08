using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class SkillInstanceDebugWindow : EditorWindow
{
    private const string DefaultCharacterId = "玩家";

    private Vector2 scroll;
    private int selectedCharacterIndex;
    private string manualCharacterId = string.Empty;
    private List<string> characterIds = new List<string>();

    [MenuItem("Tools/技能/现有技能实例")]
    private static void Open()
    {
        SkillInstanceDebugWindow window = GetWindow<SkillInstanceDebugWindow>();
        window.titleContent = new GUIContent("现有技能实例");
        window.minSize = new Vector2(640f, 480f);
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
            "这里直接读取 CharacterSkillLoadoutDatabase 里的真实数据，不再依赖战斗运行时快照。",
            MessageType.Info);

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

        string characterId = GetSelectedCharacterId();
        CharacterSkillLoadoutDatabase database = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry =
            database != null ? database.FindEntry(characterId) : null;

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (database == null)
        {
            EditorGUILayout.HelpBox("没有读取到 CharacterSkillLoadoutDatabase。", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (entry == null)
        {
            EditorGUILayout.HelpBox("这个角色当前没有技能实例数据。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawSection("技能栏位", entry.memorizedSkillIds, entry.memorizedSkillWeights);
        EditorGUILayout.Space(10f);
        DrawSection("技能仓库", entry.warehouseSkillIds, entry.warehouseSkillWeights);

        EditorGUILayout.EndScrollView();
    }

    private static void DrawSection(string title, List<string> skillIds, List<int> skillWeights)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        int count = skillIds != null ? skillIds.Count : 0;
        if (count == 0)
        {
            EditorGUILayout.HelpBox($"{title} 当前没有任何格子数据。", MessageType.Info);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string skillId = skillIds[i];
            int weight = skillWeights != null && i < skillWeights.Count ? skillWeights[i] : 0;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"索引 {i}");
                EditorGUILayout.LabelField("技能ID", string.IsNullOrWhiteSpace(skillId) ? "（空）" : skillId);
                EditorGUILayout.LabelField("权重", weight.ToString());
            }
        }
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

        string nextManualId = EditorGUILayout.TextField(
            "手动输入",
            string.IsNullOrWhiteSpace(manualCharacterId) ? characterIds[selectedCharacterIndex] : manualCharacterId);

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
            return DefaultCharacterId;
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

        CharacterSkillLoadoutDatabase database = CharacterSkillLoadoutDatabase.LoadDefault();
        if (database != null && database.Entries != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.characterId) || characterIds.Contains(entry.characterId))
                {
                    continue;
                }

                characterIds.Add(entry.characterId);
            }
        }

        if (characterIds.Count == 0)
        {
            characterIds.Add(DefaultCharacterId);
        }

        characterIds.Sort(StringComparer.Ordinal);

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
