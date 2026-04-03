using System;
using System.Collections.Generic;

public static class CharacterSkillListUtility
{
    private const string DefaultCharacterId = "\u73A9\u5BB6";
    private const bool EnableSkillLoadoutDebug = true;

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
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry =
            statDatabase != null ? statDatabase.FindEntry(resolvedCharacterId) : null;
        int memorySlotCount = statEntry != null
            ? statEntry.ResolveSkillMemorySlots()
            : CharacterStatDatabase.StatEntry.BaseSkillMemorySlots;
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry =
            loadoutDatabase != null ? loadoutDatabase.FindEntry(resolvedCharacterId) : null;
        if (entry != null && entry.skillIds != null)
        {
            int slotCount = System.Math.Min(memorySlotCount, entry.skillIds.Count);
            for (int i = 0; i < slotCount; i++)
            {
                TryAddSkill(result, null, entry.skillIds[i]);
            }
        }

        LogSkillLoadoutRead("BuildMemorizedSkillIds", resolvedCharacterId, result, entry, memorySlotCount);

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

        if (EnableSkillLoadoutDebug)
        {
            List<string> displayParts = new List<string>(result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                displayParts.Add($"{result[i].SkillId}({(result[i].IsGranted ? "Granted" : "Memorized")})");
            }

            ConsoleLog($"BuildDisplaySkillEntries. character={resolvedCharacterId}, entries=[{string.Join(", ", displayParts)}]");
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

    private static void LogSkillLoadoutRead(
        string context,
        string characterId,
        List<string> result,
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry,
        int memorySlotCount)
    {
        if (!EnableSkillLoadoutDebug)
        {
            return;
        }

        string loadout = "<null>";
        if (entry != null && entry.skillIds != null)
        {
            List<string> loadoutParts = new List<string>(entry.skillIds.Count);
            for (int i = 0; i < entry.skillIds.Count; i++)
            {
                loadoutParts.Add($"{i}:{(string.IsNullOrWhiteSpace(entry.skillIds[i]) ? "<empty>" : entry.skillIds[i])}");
            }

            loadout = string.Join(", ", loadoutParts);
        }

        ConsoleLog(
            $"{context}. character={characterId}, memorySlots={memorySlotCount}, memorized=[{string.Join(", ", result)}], loadout=[{loadout}]");
    }

    private static void ConsoleLog(string message)
    {
        if (!EnableSkillLoadoutDebug)
        {
            return;
        }

        UnityEngine.Debug.Log($"[SkillLoadoutDebug] {message}");
    }
}
