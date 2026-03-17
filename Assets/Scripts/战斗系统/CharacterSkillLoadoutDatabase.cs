using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillLoadoutDatabase", menuName = "战斗/角色技能栏数据库")]
public sealed class CharacterSkillLoadoutDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "CharacterSkillLoadoutDatabase";

    [Serializable]
    public sealed class CharacterSkillEntry
    {
        public string characterId = string.Empty;
        public List<string> skillIds = new List<string>();
        public List<int> skillWeights = new List<int>();
    }

    [SerializeField] private List<CharacterSkillEntry> entries = new List<CharacterSkillEntry>();

    public List<CharacterSkillEntry> Entries => entries;

    public CharacterSkillEntry FindEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSkillEntry entry = entries[i];
            if (entry != null && string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public CharacterSkillEntry GetOrCreateEntry(string characterId)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? "玩家" : characterId;
        CharacterSkillEntry entry = FindEntry(resolvedCharacterId);
        if (entry != null)
        {
            return entry;
        }

        entry = new CharacterSkillEntry
        {
            characterId = resolvedCharacterId
        };
        EnsureSlotDataSize(entry, 6);
        entries.Add(entry);
        return entry;
    }

    public static void EnsureSlotDataSize(CharacterSkillEntry entry, int size)
    {
        if (entry == null)
        {
            return;
        }

        EnsureStringListSize(entry.skillIds, size);
        EnsureIntListSize(entry.skillWeights, size);
    }

    public static int GetSkillWeightAt(CharacterSkillEntry entry, int index)
    {
        if (entry == null || entry.skillWeights == null || index < 0 || index >= entry.skillWeights.Count)
        {
            return 0;
        }

        return entry.skillWeights[index];
    }

    private static void EnsureStringListSize(List<string> values, int size)
    {
        if (values == null)
        {
            return;
        }

        while (values.Count < size)
        {
            values.Add(string.Empty);
        }

        while (values.Count > size)
        {
            values.RemoveAt(values.Count - 1);
        }
    }

    private static void EnsureIntListSize(List<int> values, int size)
    {
        if (values == null)
        {
            return;
        }

        while (values.Count < size)
        {
            values.Add(0);
        }

        while (values.Count > size)
        {
            values.RemoveAt(values.Count - 1);
        }
    }

    public static CharacterSkillLoadoutDatabase LoadDefault()
    {
        return Resources.Load<CharacterSkillLoadoutDatabase>(DefaultResourcePath);
    }
}
