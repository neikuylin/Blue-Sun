using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CharacterSkillLoadoutEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/CharacterSkillLoadoutDatabase.asset";
    private const string DefaultCharacterId = "玩家";

    private Vector2 scroll;
    private CharacterSkillLoadoutDatabase database;
    private BattleSkillDatabase skillDatabase;
    private int selectedCharacterIndex;

    [MenuItem("Tools/技能/技能仓库编辑器")]
    private static void Open()
    {
        CharacterSkillLoadoutEditorWindow window =
            GetWindow<CharacterSkillLoadoutEditorWindow>("技能仓库编辑器");
        window.minSize = new Vector2(720f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        database = EnsureDatabase();
        skillDatabase = BattleSkillDatabase.LoadDefault();

        List<string> characterIds = CollectCharacterIds();
        if (characterIds.Count == 0)
        {
            EditorGUILayout.HelpBox("没有可用角色 ID。", MessageType.Warning);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        selectedCharacterIndex = EditorGUILayout.Popup("角色", selectedCharacterIndex, characterIds.ToArray());

        string characterId = characterIds[selectedCharacterIndex];
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.GetOrCreateEntry(characterId);
        EnsureWarehouseEditorData(entry);

        EditorGUILayout.HelpBox(
            "这个窗口现在只编辑技能仓库，不再碰技能栏位数据。最下面永远多出一个空格，用来直接新增技能。",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawWarehouseSkills(entry);
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawWarehouseSkills(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        EditorGUILayout.LabelField("技能仓库", EditorStyles.boldLabel);

        List<BattleSkillDatabase.SkillEntry> skills =
            skillDatabase != null ? skillDatabase.Entries : new List<BattleSkillDatabase.SkillEntry>();
        string[] options = BuildSkillOptions(skills);

        int actualCount = entry.warehouseSkillIds != null ? entry.warehouseSkillIds.Count : 0;
        int displayCount = actualCount + 1;

        for (int i = 0; i < displayCount; i++)
        {
            bool isAppendSlot = i == actualCount;
            string currentSkillId = !isAppendSlot && i < entry.warehouseSkillIds.Count
                ? entry.warehouseSkillIds[i]
                : string.Empty;
            int selectedIndex = FindSkillOptionIndex(currentSkillId, skills);

            using (new EditorGUILayout.HorizontalScope())
            {
                int newIndex = EditorGUILayout.Popup(
                    isAppendSlot ? "新增空格" : $"仓库格 {i + 1}",
                    selectedIndex,
                    options);

                if (isAppendSlot)
                {
                    if (newIndex > 0)
                    {
                        entry.warehouseSkillIds.Add(skills[newIndex - 1].skillId);
                        entry.warehouseSkillWeights.Add(0);
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    entry.warehouseSkillIds[i] = newIndex <= 0 ? string.Empty : skills[newIndex - 1].skillId;

                    if (GUILayout.Button("删除", GUILayout.Width(60f)))
                    {
                        entry.warehouseSkillIds.RemoveAt(i);
                        if (entry.warehouseSkillWeights != null && i < entry.warehouseSkillWeights.Count)
                        {
                            entry.warehouseSkillWeights.RemoveAt(i);
                        }
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }

    private static void EnsureWarehouseEditorData(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.warehouseSkillIds == null)
        {
            entry.warehouseSkillIds = new List<string>();
        }

        if (entry.warehouseSkillWeights == null)
        {
            entry.warehouseSkillWeights = new List<int>();
        }

        while (entry.warehouseSkillWeights.Count < entry.warehouseSkillIds.Count)
        {
            entry.warehouseSkillWeights.Add(0);
        }

        while (entry.warehouseSkillWeights.Count > entry.warehouseSkillIds.Count)
        {
            entry.warehouseSkillWeights.RemoveAt(entry.warehouseSkillWeights.Count - 1);
        }
    }

    private static string[] BuildSkillOptions(List<BattleSkillDatabase.SkillEntry> skills)
    {
        List<string> options = new List<string> { "（空）" };
        for (int i = 0; i < skills.Count; i++)
        {
            BattleSkillDatabase.SkillEntry skill = skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
            {
                continue;
            }

            options.Add(skill.skillId);
        }

        return options.ToArray();
    }

    private static int FindSkillOptionIndex(string skillId, List<BattleSkillDatabase.SkillEntry> skills)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return 0;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            BattleSkillDatabase.SkillEntry skill = skills[i];
            if (skill != null && string.Equals(skill.skillId, skillId, StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private static CharacterSkillLoadoutDatabase EnsureDatabase()
    {
        CharacterSkillLoadoutDatabase asset = AssetDatabase.LoadAssetAtPath<CharacterSkillLoadoutDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        asset = CreateInstance<CharacterSkillLoadoutDatabase>();
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    private static List<string> CollectCharacterIds()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        if (statDatabase != null)
        {
            for (int i = 0; i < statDatabase.Entries.Count; i++)
            {
                CharacterStatDatabase.StatEntry entry = statDatabase.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.characterId))
                {
                    continue;
                }

                ids.Add(entry.characterId);
            }
        }

        CharacterSkillLoadoutDatabase loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        if (loadoutDatabase != null && loadoutDatabase.Entries != null)
        {
            for (int i = 0; i < loadoutDatabase.Entries.Count; i++)
            {
                CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = loadoutDatabase.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.characterId))
                {
                    continue;
                }

                ids.Add(entry.characterId);
            }
        }

        if (ids.Count == 0)
        {
            ids.Add(DefaultCharacterId);
        }

        List<string> result = new List<string>(ids);
        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
