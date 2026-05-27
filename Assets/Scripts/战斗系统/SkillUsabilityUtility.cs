using System;
using UnityEngine;

public static class SkillUsabilityUtility
{
    public static readonly Color EnabledSkillColor = Color.white;
    public static readonly Color DisabledSkillColor = new Color32(100, 100, 100, 255);

    public enum 技能无法使用原因
    {
        无,
        技能不存在,
        行动力不足,
        法力不足,
        技能冷却中,
        武器不匹配
    }

    public delegate int 获取技能剩余冷却回合(string ownerCharacterId, string skillId);

    public static bool IsSkillUsable(BattleSkillDatabase skillDatabase, string ownerCharacterId, string skillId)
    {
        return !技能无法使用(skillDatabase, ownerCharacterId, skillId, null);
    }

    public static bool IsSkillUsable(BattleSkillDatabase skillDatabase, string ownerCharacterId, string skillId, BattleUnit ownerUnit)
    {
        return !技能无法使用(skillDatabase, ownerCharacterId, skillId, ownerUnit);
    }

    public static bool 技能无法使用(
        BattleSkillDatabase skillDatabase,
        string ownerCharacterId,
        string skillId,
        BattleUnit ownerUnit,
        获取技能剩余冷却回合 获取剩余冷却回合 = null)
    {
        return 技能无法使用(skillDatabase, ownerCharacterId, skillId, ownerUnit, out _, 获取剩余冷却回合);
    }

    public static bool 技能无法使用(
        BattleSkillDatabase skillDatabase,
        string ownerCharacterId,
        string skillId,
        BattleUnit ownerUnit,
        out 技能无法使用原因 reason,
        获取技能剩余冷却回合 获取剩余冷却回合 = null)
    {
        reason = 技能无法使用原因.无;

        if (string.IsNullOrWhiteSpace(skillId))
        {
            reason = 技能无法使用原因.技能不存在;
            return true;
        }

        if (string.Equals(skillId, BattleTurnSystem.ExplorationIdleSkillId, StringComparison.Ordinal) ||
            string.Equals(skillId, BattleTurnSystem.ExplorationMoveSkillId, StringComparison.Ordinal))
        {
            return false;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        if (entry == null)
        {
            reason = 技能无法使用原因.技能不存在;
            return true;
        }

        if (ownerUnit != null && !ownerUnit.CanSpendActionPoints(entry.ResolveActionPointCost()))
        {
            reason = 技能无法使用原因.行动力不足;
            return true;
        }

        if (ownerUnit != null && !ownerUnit.CanSpendMana(entry.ResolveManaCost()))
        {
            reason = 技能无法使用原因.法力不足;
            return true;
        }

        int remainingCooldownTurns = 获取剩余冷却回合 != null
            ? Mathf.Max(0, 获取剩余冷却回合(ownerCharacterId, skillId))
            : 0;
        if (remainingCooldownTurns > 0)
        {
            reason = 技能无法使用原因.技能冷却中;
            return true;
        }

        if (string.Equals(skillId, BattleSkillDatabase.MoveSkillId, StringComparison.Ordinal))
        {
            return false;
        }

        ItemDatabase.WeaponCategory equippedCategory =
            InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(ownerCharacterId);
        if (!entry.RequiresWeaponCategory(equippedCategory))
        {
            reason = 技能无法使用原因.武器不匹配;
            return true;
        }

        return false;
    }
}
