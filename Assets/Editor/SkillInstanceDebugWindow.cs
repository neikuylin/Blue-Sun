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
    private readonly List<string> characterIds = new List<string>();

    [MenuItem("Tools/技能/现有技能实例")]
    private static void Open()
    {
        SkillInstanceDebugWindow window = GetWindow<SkillInstanceDebugWindow>();
        window.titleContent = new GUIContent("现有技能实例");
        window.minSize = new Vector2(720f, 520f);
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
            "这里直接读取技能装配资源。上面是技能栏位，下面是技能仓库固定格的当前摆放结果。",
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
            EditorGUILayout.HelpBox("这个角色当前没有技能数据。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawMemorizedSection(entry);
        EditorGUILayout.Space(10f);
        DrawWarehouseSection(entry);

        EditorGUILayout.EndScrollView();
    }

    private static void DrawMemorizedSection(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        EditorGUILayout.LabelField("技能栏位", EditorStyles.boldLabel);

        int count = entry.memorizedSkillIds != null ? entry.memorizedSkillIds.Count : 0;
        if (count == 0)
        {
            EditorGUILayout.HelpBox("当前没有技能栏位数据。", MessageType.Info);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string skillId = entry.memorizedSkillIds[i];
            int weight = entry.memorizedSkillWeights != null && i < entry.memorizedSkillWeights.Count
                ? entry.memorizedSkillWeights[i]
                : 0;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"栏位 {i + 1}");
                EditorGUILayout.LabelField("技能", string.IsNullOrWhiteSpace(skillId) ? "（空）" : skillId);
                EditorGUILayout.LabelField("权重", weight.ToString());
            }
        }
    }

    private static void DrawWarehouseSection(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        EditorGUILayout.LabelField("技能仓库固定格", EditorStyles.boldLabel);

        int count = entry.warehouseSkillIds != null ? entry.warehouseSkillIds.Count : 0;
        if (count == 0)
        {
            EditorGUILayout.HelpBox("这个角色当前还没有拥有任何技能。", MessageType.Info);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string warehouseSkillId = entry.warehouseSkillIds[i];
            int weight = entry.warehouseSkillWeights != null && i < entry.warehouseSkillWeights.Count
                ? entry.warehouseSkillWeights[i]
                : 0;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"仓库格 {i + 1}");
                EditorGUILayout.LabelField("当前技能", string.IsNullOrWhiteSpace(warehouseSkillId) ? "（空）" : warehouseSkillId);
                EditorGUILayout.LabelField("状态", string.IsNullOrWhiteSpace(warehouseSkillId) ? "空格" : "已放技能");
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
            EditorGUILayout.HelpBox("当前没有可选角色。", MessageType.Info);
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
