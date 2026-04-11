using System;
using System.Collections.Generic;
using UnityEngine;

internal static class 角色选择快照服务
{
    public static void 捕获授予技能快照<T>(
        List<CharacterSlotView> orderedSlots,
        List<T> targetSnapshots,
        Func<CharacterSlotView, string> resolveCharacterId,
        Func<string, IReadOnlyList<string>> getGrantedSkills,
        Func<string, List<string>, T> createSnapshot)
    {
        targetSnapshots.Clear();
        if (orderedSlots == null)
        {
            return;
        }

        HashSet<string> seenCharacterIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            CharacterSlotView slot = orderedSlots[i];
            string characterId = resolveCharacterId != null ? resolveCharacterId(slot) : string.Empty;
            if (string.IsNullOrWhiteSpace(characterId) || !seenCharacterIds.Add(characterId))
            {
                continue;
            }

            IReadOnlyList<string> grantedSkills = getGrantedSkills != null ? getGrantedSkills(characterId) : Array.Empty<string>();
            List<string> copiedSkills = grantedSkills != null ? new List<string>(grantedSkills) : new List<string>();
            targetSnapshots.Add(createSnapshot(characterId, copiedSkills));
        }
    }

    public static void 捕获武器攻击力快照<T>(
        List<CharacterSlotView> orderedSlots,
        List<T> targetSnapshots,
        Func<CharacterSlotView, string> resolveCharacterId,
        Func<string, float> getWeaponAttackPower,
        Func<string, float, T> createSnapshot)
    {
        targetSnapshots.Clear();
        if (orderedSlots == null)
        {
            return;
        }

        HashSet<string> seenCharacterIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            CharacterSlotView slot = orderedSlots[i];
            string characterId = resolveCharacterId != null ? resolveCharacterId(slot) : string.Empty;
            if (string.IsNullOrWhiteSpace(characterId) || !seenCharacterIds.Add(characterId))
            {
                continue;
            }

            float attackPower = getWeaponAttackPower != null ? getWeaponAttackPower(characterId) : 0f;
            targetSnapshots.Add(createSnapshot(characterId, attackPower));
        }
    }
}
