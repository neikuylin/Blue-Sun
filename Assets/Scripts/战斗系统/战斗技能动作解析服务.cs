using UnityEngine;

internal sealed class 战斗技能动作解析服务
{
    public string 解析动作状态名(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.actionStateName : string.Empty;
    }

    public string 解析目标选择状态名(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.targetSelectionStateName : string.Empty;
    }

    public string 解析抬手状态名(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.raiseHandStateName : string.Empty;
    }

    public float 解析目标选择偏航(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.targetSelectionYawOffset : 0f;
    }

    public float 解析抬手偏航(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.raiseHandYawOffset : 0f;
    }

    public float 解析动作偏航(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.actionYawOffset : 0f;
    }

    public float 解析收招偏航(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.postUseYawOffset : 0f;
    }

    public AudioClip 解析动作音效(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.actionSound : null;
    }

    public GameObject 解析动作音效预制体(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.actionSoundPrefab : null;
    }

    public int 解析音效延迟帧(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? Mathf.Max(0, overrideEntry.soundDelayFrame) : 0;
    }

    public AudioClip 解析受击音效(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.hitSound : null;
    }

    public GameObject 解析受击音效预制体(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null ? overrideEntry.hitSoundPrefab : null;
    }

    public bool 解析动作位移补偿(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = 解析动作覆盖(skill, unit);
        return overrideEntry != null && overrideEntry.compensateActionMotion;
    }

    private static BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride 解析动作覆盖(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        if (skill == null || unit == null)
        {
            return null;
        }

        ItemDatabase.WeaponCategory weaponCategory = InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(unit.characterId);
        bool isMoveSkill = string.Equals(skill.skillId, BattleSkillDatabase.MoveSkillId, System.StringComparison.Ordinal);
        if (!isMoveSkill && !skill.HasRequiredWeaponCategory(weaponCategory))
        {
            return null;
        }

        return skill.FindEnabledWeaponActionOverride(weaponCategory);
    }
}
