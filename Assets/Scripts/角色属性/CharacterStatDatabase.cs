using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatDatabase", menuName = "角色/角色属性库")]
public sealed class CharacterStatDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "CharacterStatDatabase";

    [Serializable]
    public sealed class StatEntry
    {
        public string characterId;
        public int strength;
        public int agility;
        public int intelligence;
        public int actionPoints = 4;
        public int moveDistance;

        public int ResolveActionPoints()
        {
            return actionPoints > 0 ? actionPoints : 4;
        }

        public int ResolveMoveDistance()
        {
            return Mathf.Max(0, 2 + (agility / 3));
        }

        public int ResolveMaxHealth()
        {
            return 50 + (Mathf.Max(0, strength) * 10);
        }
    }

    [SerializeField] private List<StatEntry> entries = new List<StatEntry>();

    public List<StatEntry> Entries => entries;

    public StatEntry FindEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            StatEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public static CharacterStatDatabase LoadDefault()
    {
        return Resources.Load<CharacterStatDatabase>(DefaultResourcePath);
    }
}
