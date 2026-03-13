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
        entries.Add(entry);
        return entry;
    }

    public static CharacterSkillLoadoutDatabase LoadDefault()
    {
        return Resources.Load<CharacterSkillLoadoutDatabase>(DefaultResourcePath);
    }
}
