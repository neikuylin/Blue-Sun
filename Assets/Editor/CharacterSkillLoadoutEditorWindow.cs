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
    private int newOwnedSkillOptionIndex;

    [MenuItem("Tools/技能/技能仓库编辑器")]
    private static void Open()
    {
        CharacterSkillLoadoutEditorWindow window =
            GetWindow<CharacterSkillLoadoutEditorWindow>("技能仓库编辑器");
        window.minSize = new Vector2(820f, 560f);
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
            EditorGUILayout.HelpBox("没有可用角色。", MessageType.Warning);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        selectedCharacterIndex = EditorGUILayout.Popup("角色", selectedCharacterIndex, characterIds.ToArray());

        string characterId = characterIds[selectedCharacterIndex];
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.GetOrCreateEntry(characterId);
        int memorizedSlotCount = ResolveSkillMemorySlotCount(characterId);
        CharacterSkillLoadoutDatabase.PrepareEntryForRuntime(entry, memorizedSlotCount);

        EditorGUILayout.HelpBox(
            "技能仓库现在表示“这个角色已经拥有的全部技能顺序”。\n" +
            "仓库空格不再是额外补出来的空位，而是表示这个技能已经被放进技能栏。\n" +
            "所以：仓库空格数 + 技能栏里的技能数 = 角色拥有的技能总数。",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawOwnedSkills(entry);
        EditorGUILayout.Space(12f);
        DrawMemorizedSkills(entry, memorizedSlotCount);
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            CharacterSkillLoadoutDatabase.PrepareEntryForRuntime(entry, memorizedSlotCount);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawOwnedSkills(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        EditorGUILayout.LabelField("已拥有技能总表", EditorStyles.boldLabel);

        List<BattleSkillDatabase.SkillEntry> skills =
            skillDatabase != null ? skillDatabase.Entries : new List<BattleSkillDatabase.SkillEntry>();
        string[] options = BuildSkillOptions(skills);

        for (int i = 0; i < entry.warehouseSkillIds.Count; i++)
        {
            string ownedSkillId = entry.warehouseSkillIds[i];
            int selectedIndex = FindSkillOptionIndex(ownedSkillId, skills);
            bool isMemorized = string.IsNullOrWhiteSpace(CharacterSkillLoadoutDatabase.GetWarehouseDisplaySkillId(entry, i));

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"拥有格 {i + 1}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("当前状态", isMemorized ? "已放入技能栏" : "仍在仓库中");

                using (new EditorGUILayout.HorizontalScope())
                {
                    int newIndex = EditorGUILayout.Popup("对应技能", selectedIndex, options);
                    if (newIndex > 0)
                    {
                        string nextSkillId = skills[newIndex - 1].skillId;
                        if (!string.Equals(nextSkillId, ownedSkillId, StringComparison.Ordinal))
                        {
                            entry.warehouseSkillIds[i] = nextSkillId;
                        }
                    }

                    GUI.enabled = i > 0;
                    if (GUILayout.Button("上移", GUILayout.Width(60f)))
                    {
                        SwapOwnedSlots(entry, i, i - 1);
                        GUIUtility.ExitGUI();
                    }

                    GUI.enabled = i < entry.warehouseSkillIds.Count - 1;
                    if (GUILayout.Button("下移", GUILayout.Width(60f)))
                    {
                        SwapOwnedSlots(entry, i, i + 1);
                        GUIUtility.ExitGUI();
                    }

                    GUI.enabled = true;
                    if (GUILayout.Button("删除", GUILayout.Width(60f)))
                    {
                        RemoveOwnedSkillAt(entry, i);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            newOwnedSkillOptionIndex = Mathf.Clamp(newOwnedSkillOptionIndex, 0, Mathf.Max(0, options.Length - 1));
            newOwnedSkillOptionIndex = EditorGUILayout.Popup("新增技能", newOwnedSkillOptionIndex, options);
            GUI.enabled = newOwnedSkillOptionIndex > 0;
            if (GUILayout.Button("加入总表", GUILayout.Width(100f)))
            {
                string newSkillId = skills[newOwnedSkillOptionIndex - 1].skillId;
                if (!string.IsNullOrWhiteSpace(newSkillId))
                {
                    entry.warehouseSkillIds.Add(newSkillId);
                    entry.warehouseSkillWeights.Add(0);
                }

                newOwnedSkillOptionIndex = 0;
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }
    }

    private void DrawMemorizedSkills(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int memorizedSlotCount)
    {
        EditorGUILayout.LabelField("技能栏位", EditorStyles.boldLabel);
        List<string> ownedSkillIds = entry.warehouseSkillIds ?? new List<string>();
        string[] options = BuildMemorizedOptions(ownedSkillIds);

        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotCapacity(entry, memorizedSlotCount);
        for (int i = 0; i < memorizedSlotCount; i++)
        {
            string currentSkillId = i < entry.memorizedSkillIds.Count ? entry.memorizedSkillIds[i] : string.Empty;
            int selectedIndex = FindMemorizedOptionIndex(currentSkillId, ownedSkillIds);

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                int newIndex = EditorGUILayout.Popup($"栏位 {i + 1}", selectedIndex, options);
                string nextSkillId = newIndex <= 0 ? string.Empty : ownedSkillIds[newIndex - 1];
                if (!string.Equals(nextSkillId, currentSkillId, StringComparison.Ordinal))
                {
                    entry.memorizedSkillIds[i] = nextSkillId;
                    if (string.IsNullOrWhiteSpace(nextSkillId))
                    {
                        entry.memorizedSkillWeights[i] = 0;
                    }
                    else
                    {
                        ClearDuplicateMemorizedSelections(entry, nextSkillId, i);
                    }
                }

                if (GUILayout.Button("清空", GUILayout.Width(60f)))
                {
                    entry.memorizedSkillIds[i] = string.Empty;
                    entry.memorizedSkillWeights[i] = 0;
                }
            }
        }
    }

    private static void SwapOwnedSlots(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int sourceIndex, int targetIndex)
    {
        if (entry == null ||
            entry.warehouseSkillIds == null ||
            entry.warehouseSkillWeights == null ||
            sourceIndex < 0 ||
            targetIndex < 0 ||
            sourceIndex >= entry.warehouseSkillIds.Count ||
            targetIndex >= entry.warehouseSkillIds.Count)
        {
            return;
        }

        string skillId = entry.warehouseSkillIds[sourceIndex];
        entry.warehouseSkillIds[sourceIndex] = entry.warehouseSkillIds[targetIndex];
        entry.warehouseSkillIds[targetIndex] = skillId;

        if (sourceIndex < entry.warehouseSkillWeights.Count && targetIndex < entry.warehouseSkillWeights.Count)
        {
            int weight = entry.warehouseSkillWeights[sourceIndex];
            entry.warehouseSkillWeights[sourceIndex] = entry.warehouseSkillWeights[targetIndex];
            entry.warehouseSkillWeights[targetIndex] = weight;
        }
    }

    private static void RemoveOwnedSkillAt(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int index)
    {
        if (entry == null || entry.warehouseSkillIds == null || index < 0 || index >= entry.warehouseSkillIds.Count)
        {
            return;
        }

        string removedSkillId = entry.warehouseSkillIds[index];
        entry.warehouseSkillIds.RemoveAt(index);
        if (entry.warehouseSkillWeights != null && index < entry.warehouseSkillWeights.Count)
        {
            entry.warehouseSkillWeights.RemoveAt(index);
        }

        if (entry.memorizedSkillIds == null)
        {
            return;
        }

        for (int i = 0; i < entry.memorizedSkillIds.Count; i++)
        {
            if (!string.Equals(entry.memorizedSkillIds[i], removedSkillId, StringComparison.Ordinal))
            {
                continue;
            }

            entry.memorizedSkillIds[i] = string.Empty;
            if (entry.memorizedSkillWeights != null && i < entry.memorizedSkillWeights.Count)
            {
                entry.memorizedSkillWeights[i] = 0;
            }
        }
    }

    private static void ClearDuplicateMemorizedSelections(
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry,
        string skillId,
        int keepIndex)
    {
        if (entry == null || entry.memorizedSkillIds == null || string.IsNullOrWhiteSpace(skillId))
        {
            return;
        }

        for (int i = 0; i < entry.memorizedSkillIds.Count; i++)
        {
            if (i == keepIndex || !string.Equals(entry.memorizedSkillIds[i], skillId, StringComparison.Ordinal))
            {
                continue;
            }

            entry.memorizedSkillIds[i] = string.Empty;
            if (entry.memorizedSkillWeights != null && i < entry.memorizedSkillWeights.Count)
            {
                entry.memorizedSkillWeights[i] = 0;
            }
        }
    }

    private static string[] BuildSkillOptions(List<BattleSkillDatabase.SkillEntry> skills)
    {
        List<string> options = new List<string> { "（请选择）" };
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

    private static string[] BuildMemorizedOptions(List<string> ownedSkillIds)
    {
        List<string> options = new List<string> { "（空）" };
        for (int i = 0; i < ownedSkillIds.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(ownedSkillIds[i]))
            {
                continue;
            }

            options.Add(ownedSkillIds[i]);
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

    private static int FindMemorizedOptionIndex(string skillId, List<string> ownedSkillIds)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return 0;
        }

        for (int i = 0; i < ownedSkillIds.Count; i++)
        {
            if (string.Equals(ownedSkillIds[i], skillId, StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private static int ResolveSkillMemorySlotCount(string characterId)
    {
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;
        return statEntry != null
            ? statEntry.ResolveSkillMemorySlots()
            : CharacterStatDatabase.StatEntry.BaseSkillMemorySlots;
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
