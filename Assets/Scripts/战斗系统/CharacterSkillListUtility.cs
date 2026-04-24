using System;
using System.Collections.Generic;

public static class CharacterSkillListUtility
{
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
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return new List<string>();
        }

        List<string> result = new List<string>();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = CharacterSkillRuntimeState.GetEntry(characterId, createIfMissing: false);
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
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return new List<string>();
        }

        List<string> result = new List<string>();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = CharacterSkillRuntimeState.GetEntry(characterId, createIfMissing: false);
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
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return new List<string>();
        }

        return InventoryShortcutRuntimeBinder.GetGrantedSkillIdsForCharacter(characterId);
    }

    public static List<DisplaySkillEntry> BuildDisplaySkillEntries(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return new List<DisplaySkillEntry>();
        }

        List<DisplaySkillEntry> result = new List<DisplaySkillEntry>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        List<string> grantedSkills = BuildGrantedSkillIds(characterId);
        for (int i = 0; i < grantedSkills.Count; i++)
        {
            TryAddDisplaySkill(result, seen, grantedSkills[i], true);
        }

        List<string> memorizedSkills = BuildMemorizedSkillIds(characterId);
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
}
