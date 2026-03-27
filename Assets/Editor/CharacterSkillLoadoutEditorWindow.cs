using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CharacterSkillLoadoutEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/CharacterSkillLoadoutDatabase.asset";
    private const string DefaultCharacterId = "\u73A9\u5BB6";

    private Vector2 scroll;
    private CharacterSkillLoadoutDatabase database;
    private BattleSkillDatabase skillDatabase;
    private int selectedCharacterIndex;

    [MenuItem("Tools/\u6280\u80FD/\u6280\u80FD\u4ED3\u5E93\u7F16\u8F91\u5668")]
    private static void Open()
    {
        CharacterSkillLoadoutEditorWindow window =
            GetWindow<CharacterSkillLoadoutEditorWindow>("\u6280\u80FD\u4ED3\u5E93\u7F16\u8F91\u5668");
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
            EditorGUILayout.HelpBox("\u6CA1\u6709\u53EF\u7528\u89D2\u8272 ID\u3002", MessageType.Warning);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        selectedCharacterIndex = EditorGUILayout.Popup("\u89D2\u8272", selectedCharacterIndex, characterIds.ToArray());

        string characterId = characterIds[selectedCharacterIndex];
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.GetOrCreateEntry(characterId);
        int memorySlotCount = ResolveSkillMemorySlotCount(characterId);
        EnsureWarehouseDataSize(entry, memorySlotCount);

        EditorGUILayout.HelpBox(
            string.Format(
                "\u8FD9\u4E2A\u7A97\u53E3\u73B0\u5728\u7F16\u8F91\u7684\u662F\u6280\u80FD\u4ED3\u5E93\u3002\u524D {0} \u4E2A `skillIds` \u4FDD\u7559\u7ED9\u8BB0\u5FC6\u683C\uFF0C\u540E\u9762\u624D\u662F\u4ED3\u5E93\u6280\u80FD\u3002",
                memorySlotCount),
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawWarehouseSkills(entry, memorySlotCount);
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawWarehouseSkills(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int memorySlotCount)
    {
        EditorGUILayout.LabelField("\u6280\u80FD\u4ED3\u5E93", EditorStyles.boldLabel);

        List<BattleSkillDatabase.SkillEntry> skills =
            skillDatabase != null ? skillDatabase.Entries : new List<BattleSkillDatabase.SkillEntry>();
        string[] options = BuildSkillOptions(skills);

        int warehouseCount = Mathf.Max(0, entry.skillIds.Count - memorySlotCount);
        for (int i = 0; i < warehouseCount; i++)
        {
            int skillIndex = memorySlotCount + i;
            int selectedIndex = FindSkillOptionIndex(entry.skillIds[skillIndex], skills);

            using (new EditorGUILayout.HorizontalScope())
            {
                int newIndex = EditorGUILayout.Popup(
                    string.Format("\u4ED3\u5E93\u683C {0}", i + 1),
                    selectedIndex,
                    options);
                entry.skillIds[skillIndex] = newIndex <= 0 ? string.Empty : skills[newIndex - 1].skillId;

                if (GUILayout.Button("\u5220\u9664", GUILayout.Width(60f)))
                {
                    entry.skillIds.RemoveAt(skillIndex);
                    if (skillIndex < entry.skillWeights.Count)
                    {
                        entry.skillWeights.RemoveAt(skillIndex);
                    }
                    GUI.enabled = true;
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;
            }
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("\u6DFB\u52A0\u4ED3\u5E93\u6280\u80FD", GUILayout.Height(28f)))
        {
            entry.skillIds.Add(string.Empty);
            if (entry.skillWeights != null)
            {
                entry.skillWeights.Add(0);
            }
        }
    }

    private static void EnsureWarehouseDataSize(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int memorySlotCount)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.skillIds == null)
        {
            entry.skillIds = new List<string>();
        }

        if (entry.skillWeights == null)
        {
            entry.skillWeights = new List<int>();
        }

        while (entry.skillIds.Count < memorySlotCount)
        {
            entry.skillIds.Add(string.Empty);
        }

        while (entry.skillWeights.Count < entry.skillIds.Count)
        {
            entry.skillWeights.Add(0);
        }
    }

    private static string[] BuildSkillOptions(List<BattleSkillDatabase.SkillEntry> skills)
    {
        List<string> options = new List<string> { "\uFF08\u7A7A\uFF09" };
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

        if (ids.Count == 0)
        {
            ids.Add(DefaultCharacterId);
        }

        List<string> result = new List<string>(ids);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static int ResolveSkillMemorySlotCount(string characterId)
    {
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;
        return statEntry != null
            ? statEntry.ResolveSkillMemorySlots()
            : CharacterStatDatabase.StatEntry.BaseSkillMemorySlots;
    }
}
