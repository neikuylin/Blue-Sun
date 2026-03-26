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
        public int physicalResistance;
        public int fireResistance;
        public int corruptionResistance;
        public int coldResistance;
        public int physicalResistancePenetration;
        public int fireResistancePenetration;
        public int corruptionResistancePenetration;
        public int coldResistancePenetration;
        public int criticalChance = 20;
        public int criticalDamage = 150;

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

        public int ResolvePhysicalResistance()
        {
            return ResolveResistanceValue(physicalResistance);
        }

        public int ResolveFireResistance()
        {
            return ResolveResistanceValue(fireResistance);
        }

        public int ResolveCorruptionResistance()
        {
            return ResolveResistanceValue(corruptionResistance);
        }

        public int ResolveColdResistance()
        {
            return ResolveResistanceValue(coldResistance);
        }

        public int ResolvePhysicalResistancePenetration()
        {
            return ResolveResistancePenetrationValue(physicalResistancePenetration);
        }

        public int ResolveFireResistancePenetration()
        {
            return ResolveResistancePenetrationValue(fireResistancePenetration);
        }

        public int ResolveCorruptionResistancePenetration()
        {
            return ResolveResistancePenetrationValue(corruptionResistancePenetration);
        }

        public int ResolveColdResistancePenetration()
        {
            return ResolveResistancePenetrationValue(coldResistancePenetration);
        }

        public int ResolveCriticalChance()
        {
            return ResolveCriticalChanceValue(criticalChance);
        }

        public int ResolveCriticalDamage()
        {
            return ResolveCriticalDamageValue(criticalDamage);
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

    public static int ResolveResistanceValue(int resistance)
    {
        return Mathf.Max(0, resistance);
    }

    public static int ResolveResistancePenetrationValue(int penetration)
    {
        return Mathf.Max(0, penetration);
    }

    public static int ResolveCriticalChanceValue(int criticalChance)
    {
        return Mathf.Max(0, criticalChance);
    }

    public static int ResolveCriticalDamageValue(int criticalDamage)
    {
        return criticalDamage > 0 ? criticalDamage : 150;
    }

    public static CharacterStatDatabase LoadDefault()
    {
        return Resources.Load<CharacterStatDatabase>(DefaultResourcePath);
    }
}
