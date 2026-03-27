using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CharacterSkillLoadoutEditorWindow : EditorWindow
{
    private static readonly string[] JourneySkillContainerChain =
    {
        "角色页面",
        "技能栏位",
        "技能格子区域"
    };

    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/CharacterSkillLoadoutDatabase.asset";
    private const string DefaultCharacterId = "玩家";

    private Vector2 scroll;
    private CharacterSkillLoadoutDatabase database;
    private BattleSkillDatabase skillDatabase;
    private int selectedCharacterIndex;

    [MenuItem("Tools/技能/技能栏位编辑器")]
    private static void Open()
    {
        CharacterSkillLoadoutEditorWindow window = GetWindow<CharacterSkillLoadoutEditorWindow>("技能栏位编辑器");
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
        int memorySlotCount = ResolveSkillMemorySlotCount(characterId);
        EnsureSize(entry.skillIds, memorySlotCount);
        CharacterSkillLoadoutDatabase.EnsureSlotDataSize(entry, memorySlotCount);

        List<string> slotNames = CollectJourneySkillSlotNames();
        if (slotNames.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有从启程场景读取到技能格。会按角色属性中的技能记忆格数量显示编辑项。", MessageType.Warning);
        }
        else if (slotNames.Count < memorySlotCount)
        {
            EditorGUILayout.HelpBox(
                $"角色属性配置了 {memorySlotCount} 个技能记忆格，但启程场景当前只放了 {slotNames.Count} 个技能格。超出的格子需要在启程场景中补齐。",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"当前角色技能记忆格: {memorySlotCount}。启程场景技能格数量: {slotNames.Count}。",
                MessageType.Info);
        }

        if (slotNames.Count < memorySlotCount)
        {
            slotNames.AddRange(BuildFallbackSlotNames(slotNames.Count, memorySlotCount - slotNames.Count));
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSharedSlots(entry.skillIds, slotNames);
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawSharedSlots(List<string> slots, List<string> slotNames)
    {
        EditorGUILayout.LabelField("技能记忆格", EditorStyles.boldLabel);

        List<BattleSkillDatabase.SkillEntry> skills = skillDatabase != null ? skillDatabase.Entries : new List<BattleSkillDatabase.SkillEntry>();
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

        for (int i = 0; i < slots.Count; i++)
        {
            int selectedIndex = 0;
            for (int s = 0; s < skills.Count; s++)
            {
                BattleSkillDatabase.SkillEntry skill = skills[s];
                if (skill == null)
                {
                    continue;
                }

                if (string.Equals(slots[i], skill.skillId, StringComparison.Ordinal))
                {
                    selectedIndex = s + 1;
                    break;
                }
            }

            string label = i < slotNames.Count && !string.IsNullOrWhiteSpace(slotNames[i])
                ? slotNames[i]
                : $"第{i + 1}格";
            int newIndex = EditorGUILayout.Popup(label, selectedIndex, options.ToArray());
            slots[i] = newIndex <= 0 ? string.Empty : skills[newIndex - 1].skillId;
        }
    }

    private static List<string> CollectJourneySkillSlotNames()
    {
        List<string> result = new List<string>();
        Transform container = FindJourneySkillContainer();
        if (container == null)
        {
            return result;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child != null)
            {
                result.Add(child.name);
            }
        }

        return result;
    }

    private static Transform FindJourneySkillContainer()
    {
        RectTransform boundContainer = JourneySkillGridBinding.FindInActiveScene();
        if (boundContainer != null)
        {
            return boundContainer;
        }

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindContainerRecursive(roots[i] != null ? roots[i].transform : null, 0);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static Transform FindContainerRecursive(Transform current, int matchedDepth)
    {
        if (current == null)
        {
            return null;
        }

        if (string.Equals(current.name, JourneySkillContainerChain[matchedDepth], StringComparison.Ordinal))
        {
            if (matchedDepth == JourneySkillContainerChain.Length - 1)
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform nested = FindContainerRecursive(current.GetChild(i), matchedDepth + 1);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform nested = FindContainerRecursive(current.GetChild(i), matchedDepth);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static List<string> BuildFallbackSlotNames(int startIndex, int count)
    {
        List<string> result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add($"第{startIndex + i + 1}格");
        }

        return result;
    }

    private static void EnsureSize(List<string> list, int size)
    {
        while (list.Count < size)
        {
            list.Add(string.Empty);
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
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
