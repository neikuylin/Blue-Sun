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
        public int hitRate = 100;

        public int ResolveActionPoints()
        {
            return actionPoints > 0 ? actionPoints : 4;
        }

        public int ResolveHitRate()
        {
            return ResolveHitRateValue(hitRate);
        }

        public int ResolveMoveDistance()
        {
            return ResolveMoveDistanceFromAgility(agility);
        }

        public int ResolveMaxHealth()
        {
            return ResolveMaxHealthFromStrength(strength);
        }

        public int ResolveMaxMana()
        {
            return ResolveMaxManaFromIntelligence(intelligence);
        }

        public int ResolveDodgeRate()
        {
            return ResolveDodgeRateFromAgility(agility);
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

    public static int ResolveMoveDistanceFromAgility(int agility)
    {
        return Mathf.Max(0, 2 + (agility / 3));
    }

    public static int ResolveMaxHealthFromStrength(int strength)
    {
        return 30 + (Mathf.Max(0, strength) * 6);
    }

    public static int ResolveMaxManaFromIntelligence(int intelligence)
    {
        return 5 + Mathf.Max(0, intelligence);
    }

    public static int ResolveHitRateValue(int hitRate)
    {
        return hitRate > 0 ? hitRate : 100;
    }

    public static int ResolveDodgeRateFromAgility(int agility)
    {
        return 10 + Mathf.Max(0, agility);
    }

    public static CharacterStatDatabase LoadDefault()
    {
        return Resources.Load<CharacterStatDatabase>(DefaultResourcePath);
    }
}
