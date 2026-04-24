using System;
using System.Collections.Generic;
using UnityEngine;

internal static class CharacterSkillRuntimeState
{
    private const string DefaultCharacterId = "玩家";

    private static readonly Dictionary<string, CharacterSkillLoadoutDatabase.CharacterSkillEntry> entriesByCharacterId =
        new Dictionary<string, CharacterSkillLoadoutDatabase.CharacterSkillEntry>(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        entriesByCharacterId.Clear();
    }

    public static CharacterSkillLoadoutDatabase.CharacterSkillEntry GetEntry(string characterId, bool createIfMissing = true)
    {
        string resolvedCharacterId = NormalizeCharacterId(characterId);
        if (entriesByCharacterId.TryGetValue(resolvedCharacterId, out CharacterSkillLoadoutDatabase.CharacterSkillEntry existingEntry))
        {
            return existingEntry;
        }

        CharacterSkillLoadoutDatabase.CharacterSkillEntry runtimeEntry = CloneFromDefault(resolvedCharacterId);
        if (runtimeEntry != null)
        {
            entriesByCharacterId[resolvedCharacterId] = runtimeEntry;
            return runtimeEntry;
        }

        if (!createIfMissing)
        {
            return null;
        }

        runtimeEntry = new CharacterSkillLoadoutDatabase.CharacterSkillEntry
        {
            characterId = resolvedCharacterId
        };
        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(runtimeEntry, 0);
        CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(runtimeEntry, 0);
        entriesByCharacterId[resolvedCharacterId] = runtimeEntry;
        return runtimeEntry;
    }

    public static void CaptureSaveData(SaveGameData.SkillSave target)
    {
        if (target == null)
        {
            return;
        }

        target.entries.Clear();
        List<string> characterIds = CollectSaveCharacterIds();
        for (int i = 0; i < characterIds.Count; i++)
        {
            CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = GetEntry(characterIds[i], createIfMissing: false);
            if (entry == null)
            {
                continue;
            }

            target.entries.Add(new SaveGameData.CharacterSkillSave
            {
                characterId = NormalizeCharacterId(entry.characterId),
                memorizedSkillIds = entry.memorizedSkillIds != null ? new List<string>(entry.memorizedSkillIds) : new List<string>(),
                memorizedSkillWeights = entry.memorizedSkillWeights != null ? new List<int>(entry.memorizedSkillWeights) : new List<int>(),
                warehouseSkillIds = entry.warehouseSkillIds != null ? new List<string>(entry.warehouseSkillIds) : new List<string>(),
                warehouseSkillWeights = entry.warehouseSkillWeights != null ? new List<int>(entry.warehouseSkillWeights) : new List<int>()
            });
        }
    }

    public static void ApplySaveData(SaveGameData.SkillSave source)
    {
        entriesByCharacterId.Clear();
        if (source == null || source.entries == null)
        {
            return;
        }

        for (int i = 0; i < source.entries.Count; i++)
        {
            SaveGameData.CharacterSkillSave savedEntry = source.entries[i];
            if (savedEntry == null || string.IsNullOrWhiteSpace(savedEntry.characterId))
            {
                continue;
            }

            CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = new CharacterSkillLoadoutDatabase.CharacterSkillEntry
            {
                characterId = NormalizeCharacterId(savedEntry.characterId),
                memorizedSkillIds = savedEntry.memorizedSkillIds != null ? new List<string>(savedEntry.memorizedSkillIds) : new List<string>(),
                memorizedSkillWeights = savedEntry.memorizedSkillWeights != null ? new List<int>(savedEntry.memorizedSkillWeights) : new List<int>(),
                warehouseSkillIds = savedEntry.warehouseSkillIds != null ? new List<string>(savedEntry.warehouseSkillIds) : new List<string>(),
                warehouseSkillWeights = savedEntry.warehouseSkillWeights != null ? new List<int>(savedEntry.warehouseSkillWeights) : new List<int>()
            };

            CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(entry, entry.memorizedSkillIds.Count);
            CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(entry, entry.warehouseSkillIds.Count);
            entriesByCharacterId[entry.characterId] = entry;
        }
    }

    public static void ResetSaveData()
    {
        entriesByCharacterId.Clear();
    }

    private static CharacterSkillLoadoutDatabase.CharacterSkillEntry CloneFromDefault(string characterId)
    {
        CharacterSkillLoadoutDatabase database = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry sourceEntry =
            database != null ? database.FindEntry(characterId) : null;
        if (sourceEntry == null)
        {
            return null;
        }

        CharacterSkillLoadoutDatabase.CharacterSkillEntry clone = new CharacterSkillLoadoutDatabase.CharacterSkillEntry
        {
            characterId = NormalizeCharacterId(sourceEntry.characterId),
            memorizedSkillIds = sourceEntry.memorizedSkillIds != null
                ? new List<string>(sourceEntry.memorizedSkillIds)
                : new List<string>(),
            memorizedSkillWeights = sourceEntry.memorizedSkillWeights != null
                ? new List<int>(sourceEntry.memorizedSkillWeights)
                : new List<int>(),
            warehouseSkillIds = sourceEntry.warehouseSkillIds != null
                ? new List<string>(sourceEntry.warehouseSkillIds)
                : new List<string>(),
            warehouseSkillWeights = sourceEntry.warehouseSkillWeights != null
                ? new List<int>(sourceEntry.warehouseSkillWeights)
                : new List<int>()
        };

        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(clone, clone.memorizedSkillIds.Count);
        CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(clone, clone.warehouseSkillIds.Count);
        return clone;
    }

    private static string NormalizeCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId.Trim();
    }

    private static List<string> CollectSaveCharacterIds()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string characterId in entriesByCharacterId.Keys)
        {
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                ids.Add(NormalizeCharacterId(characterId));
            }
        }

        CharacterSkillLoadoutDatabase database = CharacterSkillLoadoutDatabase.LoadDefault();
        if (database != null && database.Entries != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.characterId))
                {
                    continue;
                }

                ids.Add(NormalizeCharacterId(entry.characterId));
            }
        }

        List<string> result = new List<string>(ids);
        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
