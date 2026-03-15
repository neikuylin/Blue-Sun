using System;
using System.Collections.Generic;

public static class CharacterSkillListUtility
{
    private const string DefaultCharacterId = "\u73A9\u5BB6";

    public static List<string> BuildSkillIds(string characterId)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        CharacterSkillLoadoutDatabase loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry =
            loadoutDatabase != null ? loadoutDatabase.FindEntry(resolvedCharacterId) : null;
        if (entry != null && entry.skillIds != null)
        {
            for (int i = 0; i < entry.skillIds.Count; i++)
            {
                TryAddSkill(result, seen, entry.skillIds[i]);
            }
        }

        List<string> liveGrantedSkills = InventoryShortcutRuntimeBinder.GetGrantedSkillIdsForCharacter(resolvedCharacterId);
        for (int i = 0; i < liveGrantedSkills.Count; i++)
        {
            TryAddSkill(result, seen, liveGrantedSkills[i]);
        }

        IReadOnlyList<string> capturedGrantedSkills = CharacterSelectionState.GetCapturedGrantedSkills(resolvedCharacterId);
        for (int i = 0; i < capturedGrantedSkills.Count; i++)
        {
            TryAddSkill(result, seen, capturedGrantedSkills[i]);
        }

        return result;
    }

    private static void TryAddSkill(List<string> target, HashSet<string> seen, string skillId)
    {
        if (target == null || seen == null || string.IsNullOrWhiteSpace(skillId) || !seen.Add(skillId))
        {
            return;
        }

        target.Add(skillId);
    }
}
