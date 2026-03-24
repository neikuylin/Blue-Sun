using System;
using UnityEngine;

public static class SkillUsabilityUtility
{
    public static readonly Color EnabledSkillColor = Color.white;
    public static readonly Color DisabledSkillColor = new Color32(100, 100, 100, 255);

    public static bool IsSkillUsable(BattleSkillDatabase skillDatabase, string ownerCharacterId, string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        if (string.Equals(skillId, BattleTurnSystem.ExplorationIdleSkillId, StringComparison.Ordinal) ||
            string.Equals(skillId, BattleTurnSystem.ExplorationMoveSkillId, StringComparison.Ordinal))
        {
            return true;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        if (entry == null)
        {
            return false;
        }

        ItemDatabase.WeaponCategory equippedCategory =
            InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(ownerCharacterId);
        return entry.RequiresWeaponCategory(equippedCategory);
    }
}
