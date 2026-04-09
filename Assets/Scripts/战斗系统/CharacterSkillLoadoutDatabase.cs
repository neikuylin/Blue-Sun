using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CharacterSkillLoadoutDatabase", menuName = "战斗/角色技能栏数据库")]
public sealed class CharacterSkillLoadoutDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "CharacterSkillLoadoutDatabase";

    [Serializable]
    public sealed class CharacterSkillEntry
    {
        public string characterId = string.Empty;
        public List<string> memorizedSkillIds = new List<string>();
        public List<int> memorizedSkillWeights = new List<int>();
        public List<string> warehouseSkillIds = new List<string>();
        public List<int> warehouseSkillWeights = new List<int>();

        [FormerlySerializedAs("skillIds")]
        [HideInInspector] public List<string> legacySkillIds = new List<string>();
        [FormerlySerializedAs("skillWeights")]
        [HideInInspector] public List<int> legacySkillWeights = new List<int>();
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
        MigrateLegacyIfNeeded(entry, size);
        EnsureStringListSize(entry.memorizedSkillIds, size);
        EnsureIntListSize(entry.memorizedSkillWeights, size);
    }

    public static void EnsureMemorizedSlotMinSize(CharacterSkillEntry entry, int size)
    {
        if (entry == null)
        {
            return;
        }

        MigrateLegacyIfNeeded(entry, size);
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

    public static void PrepareEntryForRuntime(CharacterSkillEntry entry, int memorySlotCount)
    {
        if (entry == null)
        {
            return;
        }

        MigrateLegacyIfNeeded(entry, memorySlotCount);
        NormalizeOwnedAndMemorizedSkills(entry, memorySlotCount);
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

    public static string GetWarehouseDisplaySkillId(CharacterSkillEntry entry, int index)
    {
        if (entry == null || entry.warehouseSkillIds == null || index < 0 || index >= entry.warehouseSkillIds.Count)
        {
            return string.Empty;
        }

        string ownedSkillId = entry.warehouseSkillIds[index];
        if (string.IsNullOrWhiteSpace(ownedSkillId))
        {
            return string.Empty;
        }

        int ownedOccurrenceIndex = CountOwnedSkillOccurrencesThroughIndex(entry, index, ownedSkillId);
        int memorizedOccurrenceCount = CountMemorizedSkillOccurrences(entry, ownedSkillId);
        return memorizedOccurrenceCount >= ownedOccurrenceIndex ? string.Empty : ownedSkillId;
    }

    public static bool IsWarehouseSlotOccupiedByMemorized(CharacterSkillEntry entry, int index)
    {
        if (entry == null || entry.warehouseSkillIds == null || index < 0 || index >= entry.warehouseSkillIds.Count)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(GetWarehouseDisplaySkillId(entry, index));
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

    private static void MigrateLegacyIfNeeded(CharacterSkillEntry entry, int memorySlotCount)
    {
        if (entry == null)
        {
            return;
        }

        EnsureListsInitialized(entry);

        if ((entry.legacySkillIds == null || entry.legacySkillIds.Count == 0) ||
            entry.memorizedSkillIds.Count > 0 ||
            entry.warehouseSkillIds.Count > 0)
        {
            return;
        }

        int memorizedCount = Math.Max(0, Math.Min(memorySlotCount, entry.legacySkillIds.Count));
        for (int i = 0; i < memorizedCount; i++)
        {
            entry.memorizedSkillIds.Add(entry.legacySkillIds[i]);
            entry.memorizedSkillWeights.Add(i < entry.legacySkillWeights.Count ? entry.legacySkillWeights[i] : 0);
        }

        for (int i = memorizedCount; i < entry.legacySkillIds.Count; i++)
        {
            entry.warehouseSkillIds.Add(entry.legacySkillIds[i]);
            entry.warehouseSkillWeights.Add(i < entry.legacySkillWeights.Count ? entry.legacySkillWeights[i] : 0);
        }

        entry.legacySkillIds.Clear();
        entry.legacySkillWeights.Clear();
    }

    private static void NormalizeOwnedAndMemorizedSkills(CharacterSkillEntry entry, int memorySlotCount)
    {
        EnsureListsInitialized(entry);

        List<string> ownedSkillIds = new List<string>();
        List<int> ownedSkillWeights = new List<int>();

        AppendOwnedSkills(ownedSkillIds, ownedSkillWeights, entry.warehouseSkillIds, entry.warehouseSkillWeights);
        AppendOwnedSkills(ownedSkillIds, ownedSkillWeights, entry.memorizedSkillIds, entry.memorizedSkillWeights);

        entry.warehouseSkillIds.Clear();
        entry.warehouseSkillWeights.Clear();
        entry.warehouseSkillIds.AddRange(ownedSkillIds);
        entry.warehouseSkillWeights.AddRange(ownedSkillWeights);

        EnsureMemorizedSlotCapacity(entry, Math.Max(0, memorySlotCount));
        SanitizeMemorizedSkills(entry);
    }

    private static void AppendOwnedSkills(
        List<string> ownedSkillIds,
        List<int> ownedSkillWeights,
        List<string> sourceSkillIds,
        List<int> sourceWeights)
    {
        if (ownedSkillIds == null || ownedSkillWeights == null || sourceSkillIds == null)
        {
            return;
        }

        for (int i = 0; i < sourceSkillIds.Count; i++)
        {
            string skillId = sourceSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId) || ownedSkillIds.Contains(skillId))
            {
                continue;
            }

            ownedSkillIds.Add(skillId);
            ownedSkillWeights.Add(sourceWeights != null && i < sourceWeights.Count ? sourceWeights[i] : 0);
        }
    }

    private static void SanitizeMemorizedSkills(CharacterSkillEntry entry)
    {
        if (entry == null || entry.memorizedSkillIds == null)
        {
            return;
        }

        HashSet<string> ownedSkillSet = new HashSet<string>(entry.warehouseSkillIds ?? new List<string>(), StringComparer.Ordinal);
        HashSet<string> memorizedSkillSet = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < entry.memorizedSkillIds.Count; i++)
        {
            string skillId = entry.memorizedSkillIds[i];
            bool shouldClear = string.IsNullOrWhiteSpace(skillId) ||
                               !ownedSkillSet.Contains(skillId) ||
                               !memorizedSkillSet.Add(skillId);

            if (!shouldClear)
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

    private static int CountOwnedSkillOccurrencesThroughIndex(CharacterSkillEntry entry, int index, string skillId)
    {
        if (entry == null || entry.warehouseSkillIds == null || string.IsNullOrWhiteSpace(skillId))
        {
            return 0;
        }

        int count = 0;
        int lastIndex = Math.Min(index, entry.warehouseSkillIds.Count - 1);
        for (int i = 0; i <= lastIndex; i++)
        {
            if (string.Equals(entry.warehouseSkillIds[i], skillId, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountMemorizedSkillOccurrences(CharacterSkillEntry entry, string skillId)
    {
        if (entry == null || entry.memorizedSkillIds == null || string.IsNullOrWhiteSpace(skillId))
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < entry.memorizedSkillIds.Count; i++)
        {
            if (string.Equals(entry.memorizedSkillIds[i], skillId, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    public static CharacterSkillLoadoutDatabase LoadDefault()
    {
        return Resources.Load<CharacterSkillLoadoutDatabase>(DefaultResourcePath);
    }
}
