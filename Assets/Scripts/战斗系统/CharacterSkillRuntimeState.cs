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
}
