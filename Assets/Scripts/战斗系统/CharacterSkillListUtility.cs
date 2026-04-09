using System;
using System.Collections.Generic;

public static class CharacterSkillListUtility
{
    private const string DefaultCharacterId = "\u73A9\u5BB6";

    public readonly struct DisplaySkillEntry
    {
        public DisplaySkillEntry(string skillId, bool isGranted)
        {
            SkillId = skillId ?? string.Empty;
            IsGranted = isGranted;
        }

        public string SkillId { get; }
        public bool IsGranted { get; }
    }

    public static List<string> BuildSkillIds(string characterId)
    {
        List<DisplaySkillEntry> entries = BuildDisplaySkillEntries(characterId);
        List<string> result = new List<string>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            result.Add(entries[i].SkillId);
        }

        return result;
    }

    public static List<string> BuildMemorizedSkillIds(string characterId)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        List<string> result = new List<string>();
        CharacterSkillLoadoutDatabase loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry =
            loadoutDatabase != null ? loadoutDatabase.FindEntry(resolvedCharacterId) : null;
        CharacterSkillLoadoutDatabase.PrepareEntryForRuntime(entry, ResolveSkillMemorySlotCount(resolvedCharacterId));
        if (entry != null && entry.memorizedSkillIds != null)
        {
            for (int i = 0; i < entry.memorizedSkillIds.Count; i++)
            {
                TryAddSkill(result, null, entry.memorizedSkillIds[i]);
            }
        }

        return result;
    }

    public static List<string> BuildWarehouseSkillIds(string characterId)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        List<string> result = new List<string>();
        CharacterSkillLoadoutDatabase loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry =
            loadoutDatabase != null ? loadoutDatabase.FindEntry(resolvedCharacterId) : null;
        CharacterSkillLoadoutDatabase.PrepareEntryForRuntime(entry, ResolveSkillMemorySlotCount(resolvedCharacterId));
        if (entry != null && entry.warehouseSkillIds != null)
        {
            for (int i = 0; i < entry.warehouseSkillIds.Count; i++)
            {
                TryAddSkill(result, null, entry.warehouseSkillIds[i]);
            }
        }

        return result;
    }

    public static List<string> BuildGrantedSkillIds(string characterId)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        return InventoryShortcutRuntimeBinder.GetGrantedSkillIdsForCharacter(resolvedCharacterId);
    }

    public static List<DisplaySkillEntry> BuildDisplaySkillEntries(string characterId)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        List<DisplaySkillEntry> result = new List<DisplaySkillEntry>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        List<string> grantedSkills = BuildGrantedSkillIds(resolvedCharacterId);
        for (int i = 0; i < grantedSkills.Count; i++)
        {
            TryAddDisplaySkill(result, seen, grantedSkills[i], true);
        }

        List<string> memorizedSkills = BuildMemorizedSkillIds(resolvedCharacterId);
        for (int i = 0; i < memorizedSkills.Count; i++)
        {
            TryAddDisplaySkill(result, seen, memorizedSkills[i], false);
        }

        return result;
    }

    private static void TryAddSkill(List<string> target, HashSet<string> seen, string skillId)
    {
        if (target == null || string.IsNullOrWhiteSpace(skillId))
        {
            return;
        }

        if (seen != null && !seen.Add(skillId))
        {
            return;
        }

        target.Add(skillId);
    }

    private static void TryAddDisplaySkill(List<DisplaySkillEntry> target, HashSet<string> seen, string skillId, bool isGranted)
    {
        if (target == null || string.IsNullOrWhiteSpace(skillId) || seen == null || !seen.Add(skillId))
        {
            return;
        }

        target.Add(new DisplaySkillEntry(skillId, isGranted));
    }

    private static int ResolveSkillMemorySlotCount(string characterId)
    {
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry =
            statDatabase != null ? statDatabase.FindEntry(string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId) : null;

        return statEntry != null
            ? statEntry.ResolveSkillMemorySlots()
            : CharacterStatDatabase.StatEntry.BaseSkillMemorySlots;
    }
}
