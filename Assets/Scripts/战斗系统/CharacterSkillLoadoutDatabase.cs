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
            return entry;
        }

        entry = new CharacterSkillEntry
        {
            characterId = resolvedCharacterId
        };
        EnsureMemorizedSlotCapacity(entry, CharacterStatDatabase.StatEntry.BaseSkillMemorySlots);
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

    public static void EnsureWarehouseSlotCapacity(CharacterSkillEntry entry, int size)
    {
        if (entry == null)
        {
            return;
        }

        MigrateLegacyIfNeeded(entry, CharacterStatDatabase.StatEntry.BaseSkillMemorySlots);
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

    public static void TrimTrailingWarehouseEmptySlots(CharacterSkillEntry entry)
    {
        if (entry == null || entry.warehouseSkillIds == null)
        {
            return;
        }

        while (entry.warehouseSkillIds.Count > 0 &&
               string.IsNullOrWhiteSpace(entry.warehouseSkillIds[entry.warehouseSkillIds.Count - 1]))
        {
            entry.warehouseSkillIds.RemoveAt(entry.warehouseSkillIds.Count - 1);
            if (entry.warehouseSkillWeights != null && entry.warehouseSkillWeights.Count > 0)
            {
                entry.warehouseSkillWeights.RemoveAt(entry.warehouseSkillWeights.Count - 1);
            }
        }
    }

    private static void MigrateLegacyIfNeeded(CharacterSkillEntry entry, int memorySlotCount)
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

        if ((entry.legacySkillIds == null || entry.legacySkillIds.Count == 0) ||
            entry.memorizedSkillIds.Count > 0 ||
            entry.warehouseSkillIds.Count > 0)
        {
            EnsureStringListSize(entry.memorizedSkillIds, memorySlotCount);
            EnsureIntListSize(entry.memorizedSkillWeights, memorySlotCount);
            return;
        }

        int memorizedCount = Math.Max(0, Math.Min(memorySlotCount, entry.legacySkillIds.Count));
        for (int i = 0; i < memorizedCount; i++)
        {
            entry.memorizedSkillIds.Add(entry.legacySkillIds[i]);
            entry.memorizedSkillWeights.Add(i < entry.legacySkillWeights.Count ? entry.legacySkillWeights[i] : 0);
        }

        EnsureStringListSize(entry.memorizedSkillIds, memorySlotCount);
        EnsureIntListSize(entry.memorizedSkillWeights, memorySlotCount);

        for (int i = memorizedCount; i < entry.legacySkillIds.Count; i++)
        {
            entry.warehouseSkillIds.Add(entry.legacySkillIds[i]);
            entry.warehouseSkillWeights.Add(i < entry.legacySkillWeights.Count ? entry.legacySkillWeights[i] : 0);
        }

        TrimTrailingWarehouseEmptySlots(entry);
        entry.legacySkillIds.Clear();
        entry.legacySkillWeights.Clear();
    }

    public static CharacterSkillLoadoutDatabase LoadDefault()
    {
        return Resources.Load<CharacterSkillLoadoutDatabase>(DefaultResourcePath);
    }
}
