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
            EditorGUILayout.HelpBox("没有可用角色ID。", MessageType.Warning);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        selectedCharacterIndex = EditorGUILayout.Popup("角色", selectedCharacterIndex, characterIds.ToArray());

        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.GetOrCreateEntry(characterIds[selectedCharacterIndex]);
        List<string> slotNames = CollectJourneySkillSlotNames();
        int detectedSlotCount = Mathf.Max(1, slotNames.Count);
        EnsureSize(entry.skillIds, detectedSlotCount);

        EditorGUILayout.Space(4f);
        if (slotNames.Count > 0)
        {
            EditorGUILayout.HelpBox($"已从启程场景读取到 {slotNames.Count} 个技能格子，Tools 会按这些格子逐个显示。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("当前没有从启程场景读到技能格子，先打开启程场景后再编辑。现在会先按已有数据兜底显示。", MessageType.Warning);
            slotNames = BuildFallbackSlotNames(entry.skillIds.Count);
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
        EditorGUILayout.LabelField("共用技能槽位", EditorStyles.boldLabel);

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

                if (string.Equals(slots[i], skill.skillId, System.StringComparison.Ordinal))
                {
                    selectedIndex = s + 1;
                    break;
                }
            }

            string label = i < slotNames.Count && !string.IsNullOrWhiteSpace(slotNames[i]) ? slotNames[i] : $"第 {i + 1} 格";
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

        if (string.Equals(current.name, JourneySkillContainerChain[matchedDepth], System.StringComparison.Ordinal))
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

    private static List<string> BuildFallbackSlotNames(int count)
    {
        List<string> result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add($"第 {i + 1} 格");
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
        List<string> result = new List<string>();
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

                result.Add(entry.characterId);
            }
        }

        if (result.Count == 0)
        {
            result.Add("玩家");
        }

        return result;
    }
}
