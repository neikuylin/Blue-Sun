using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillLoadoutDatabase", menuName = "战斗/角色技能栏数据库")]
public sealed class CharacterSkillLoadoutDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "CharacterSkillLoadoutDatabase";
    private const string DefaultCharacterId = "玩家";

    [Serializable]
    public sealed class CharacterSkillEntry
    {
        public string characterId = string.Empty;
        public List<string> memorizedSkillIds = new List<string>();
        public List<int> memorizedSkillWeights = new List<int>();
        public List<string> warehouseSkillIds = new List<string>();
        public List<int> warehouseSkillWeights = new List<int>();
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
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        CharacterSkillEntry entry = FindEntry(resolvedCharacterId);
        if (entry != null)
        {
            EnsureListsInitialized(entry);
            return entry;
        }

        entry = new CharacterSkillEntry
        {
            characterId = resolvedCharacterId
        };
        EnsureListsInitialized(entry);
        entries.Add(entry);
        return entry;
    }

    public static void EnsureMemorizedSlotCapacity(CharacterSkillEntry entry, int size)
    {
        if (entry == null)
        {
            return;
        }

        EnsureListsInitialized(entry);
        EnsureStringListSize(entry.memorizedSkillIds, size);
        EnsureIntListSize(entry.memorizedSkillWeights, size);
    }

    public static void EnsureMemorizedSlotMinSize(CharacterSkillEntry entry, int size)
    {
        if (entry == null)
        {
            return;
        }

        EnsureListsInitialized(entry);
        EnsureStringListMinSize(entry.memorizedSkillIds, size);
        EnsureIntListMinSize(entry.memorizedSkillWeights, size);
    }

    public static void EnsureWarehouseSlotCapacity(CharacterSkillEntry entry, int size)
    {
        if (entry == null)
        {
            return;
        }

        EnsureListsInitialized(entry);
        EnsureStringListMinSize(entry.warehouseSkillIds, size);
        EnsureIntListMinSize(entry.warehouseSkillWeights, size);
    }

    public static int GetMemorizedSkillWeightAt(CharacterSkillEntry entry, int index)
    {
        if (entry == null || entry.memorizedSkillWeights == null || index < 0 || index >= entry.memorizedSkillWeights.Count)
        {
            return 0;
        }

        return entry.memorizedSkillWeights[index];
    }

    public static int GetWarehouseSkillWeightAt(CharacterSkillEntry entry, int index)
    {
        if (entry == null || entry.warehouseSkillWeights == null || index < 0 || index >= entry.warehouseSkillWeights.Count)
        {
            return 0;
        }

        return entry.warehouseSkillWeights[index];
    }

    public static CharacterSkillLoadoutDatabase LoadDefault()
    {
        return Resources.Load<CharacterSkillLoadoutDatabase>(DefaultResourcePath);
    }

    public static string DescribeEntry(CharacterSkillEntry entry)
    {
        if (entry == null)
        {
            return "entry=null";
        }

        return $"characterId={entry.characterId}, memorized=[{FormatSkillList(entry.memorizedSkillIds)}], warehouse=[{FormatSkillList(entry.warehouseSkillIds)}]";
    }

    public static string DescribeDatabaseEntry(string characterId)
    {
        CharacterSkillLoadoutDatabase database = LoadDefault();
        if (database == null)
        {
            return "database=null";
        }

        CharacterSkillEntry entry = database.FindEntry(characterId);
        return $"dbInstance={database.GetInstanceID()}, {DescribeEntry(entry)}";
    }

    private static void EnsureListsInitialized(CharacterSkillEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.memorizedSkillIds == null)
        {
            entry.memorizedSkillIds = new List<string>();
        }

        if (entry.memorizedSkillWeights == null)
        {
            entry.memorizedSkillWeights = new List<int>();
        }

        if (entry.warehouseSkillIds == null)
        {
            entry.warehouseSkillIds = new List<string>();
        }

        if (entry.warehouseSkillWeights == null)
        {
            entry.warehouseSkillWeights = new List<int>();
        }
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

    private static void EnsureStringListMinSize(List<string> values, int size)
    {
        if (values == null)
        {
            return;
        }

        while (values.Count < size)
        {
            values.Add(string.Empty);
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

    private static void EnsureIntListMinSize(List<int> values, int size)
    {
        if (values == null)
        {
            return;
        }

        while (values.Count < size)
        {
            values.Add(0);
        }
    }

    private static string FormatSkillList(List<string> values)
    {
        if (values == null)
        {
            return "null";
        }

        if (values.Count == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            string skillId = values[i];
            parts[i] = string.IsNullOrWhiteSpace(skillId) ? $"#{i}:<empty>" : $"#{i}:{skillId}";
        }

        return string.Join(", ", parts);
    }
}
