using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗技能基础结算服务
{
    private readonly int minHitChancePercent;
    private readonly int maxHitChancePercent;

    public 战斗技能基础结算服务(int minHitChancePercent, int maxHitChancePercent)
    {
        this.minHitChancePercent = minHitChancePercent;
        this.maxHitChancePercent = maxHitChancePercent;
    }

    public CombatDamageResult 计算技能伤害(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill, string skillSource = null)
    {
        if (skill == null)
        {
            return null;
        }

        switch (skill.group)
        {
            case BattleSkillDatabase.SkillGroup.CombatArt:
                return 计算战技伤害(caster, target, skill, skillSource);
            case BattleSkillDatabase.SkillGroup.Spell:
                return 计算法术伤害(caster, target, skill);
            default:
                return null;
        }
    }

    public CombatDamageResult 计算战技伤害(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill, string skillSource)
    {
        if (caster == null || target == null || skill == null)
        {
            return null;
        }

        float baseAttackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(caster.characterId, skillSource);
        ItemDatabase.WeaponCategory weaponCategory = 解析武器类型(caster.characterId, skillSource);
        WeaponEnchantmentAttackPower enchantmentAttackPower = caster.GetWeaponEnchantmentAttackPower(weaponCategory);
        float attackPower = baseAttackPower + enchantmentAttackPower.Total;
        if (attackPower <= 0f)
        {
            return null;
        }

        float damage = attackPower * Mathf.Max(0f, skill.damageMultiplier);
        if (damage <= 0f)
        {
            return null;
        }

        CombatDamageResult result = new CombatDamageResult();
        int totalCriticalChance = caster.CriticalChance + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalChanceBonus(caster.characterId, skillSource);
        int totalCriticalDamage = caster.CriticalDamage + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalDamageBonus(caster.characterId, skillSource);
        result.isCritical = 判定暴击(totalCriticalChance);
        if (result.isCritical)
        {
            damage *= Mathf.Max(0f, totalCriticalDamage) / 100f;
        }

        float damageMultiplier = Mathf.Max(0f, skill.damageMultiplier);
        float criticalMultiplier = result.isCritical ? Mathf.Max(0f, totalCriticalDamage) / 100f : 1f;
        构建伤害分量(
            result.components,
            baseAttackPower,
            enchantmentAttackPower,
            damageMultiplier,
            criticalMultiplier,
            InventoryShortcutRuntimeBinder.GetCharacterWeaponDamageDistribution(caster.characterId, skillSource),
            caster,
            target,
            skillSource);
        for (int i = 0; i < result.components.Count; i++)
        {
            result.totalDamage += result.components[i].amount;
        }

        result.appliedDamage = Mathf.Max(0, Mathf.RoundToInt(result.totalDamage));
        return result;
    }

    public CombatDamageResult 计算法术伤害(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return null;
        }

        float intelligence = Mathf.Max(0, caster.GetEffectiveIntelligence());
        float baseDamage = Mathf.Max(0, skill.fixedDamage) +
            (Mathf.Max(0f, skill.attributeMultiplier) * intelligence);
        float damage = baseDamage * InventoryShortcutRuntimeBinder.GetCharacterStaffDamageMultiplier(caster.characterId);
        if (damage <= 0f)
        {
            return null;
        }

        CombatDamageResult result = new CombatDamageResult();
        构建法术伤害分量(result.components, damage, skill, caster, target);
        for (int i = 0; i < result.components.Count; i++)
        {
            result.totalDamage += result.components[i].amount;
        }

        result.appliedDamage = Mathf.Max(0, Mathf.RoundToInt(result.totalDamage));
        return result;
    }

    public bool 判定技能命中(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return false;
        }

        int hitChance = 计算技能命中率(caster, target, skill);
        if (hitChance >= maxHitChancePercent)
        {
            return true;
        }

        if (hitChance <= minHitChancePercent)
        {
            return false;
        }

        return Random.Range(0, maxHitChancePercent) < hitChance;
    }

    public int 计算技能命中率(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return minHitChancePercent;
        }

        return Mathf.Clamp(
            caster.HitRate + skill.ResolveHitRateModifier() - target.DodgeRate,
            minHitChancePercent,
            maxHitChancePercent);
    }

    public void 应用附加效果到单位(
        BattleUnit caster,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Color effectPopupColor)
    {
        if (caster == null || target == null || skill == null)
        {
            return;
        }

        skill.EnsureAttachedEffectsMigrated();
        if (skill.attachedEffects == null || skill.attachedEffects.Count == 0)
        {
            return;
        }

        for (int i = 0; i < skill.attachedEffects.Count; i++)
        {
            BattleSkillDatabase.SkillEntry.AttachedEffectEntry attachedEffect = skill.attachedEffects[i];
            if (attachedEffect == null || string.IsNullOrWhiteSpace(attachedEffect.effectId) || attachedEffect.durationTurns <= 0)
            {
                continue;
            }

            int applyChancePercent = Mathf.Clamp(attachedEffect.applyChancePercent, 0, 100);
            if (applyChancePercent <= 0)
            {
                continue;
            }

            if (applyChancePercent < 100 && Random.Range(0, 100) >= applyChancePercent)
            {
                continue;
            }

            EffectDatabase.EffectEntry appliedEffectEntry;
            if (!target.ApplyAttachedEffect(attachedEffect.effectId, attachedEffect.durationTurns, caster, out appliedEffectEntry))
            {
                continue;
            }

            if (appliedEffectEntry == null || string.IsNullOrWhiteSpace(appliedEffectEntry.effectId))
            {
                continue;
            }

            BattleDamageNumberPopup.ShowConfiguredText(
                target,
                "+" + 解析效果调试名称(appliedEffectEntry),
                BattleDamageNumberPopup.ConfiguredPopupKind.Effect,
                effectPopupColor,
                battleCamera);

        }
    }

    public void 应用附加效果到单位列表(
        BattleUnit caster,
        List<BattleUnit> targets,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Color effectPopupColor)
    {
        if (targets == null || targets.Count == 0)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit target = targets[i];
            if (target != null && target.IsAlive)
            {
                应用附加效果到单位(caster, target, skill, battleCamera, effectPopupColor);
            }
        }
    }

    public static string 格式化单位效果调试文本(BattleUnit unit)
    {
        if (unit == null || unit.ActiveEffects == null || unit.ActiveEffects.Count == 0)
        {
            return "无";
        }

        EffectDatabase database = EffectDatabase.LoadDefault();
        List<string> parts = new List<string>();
        for (int i = 0; i < unit.ActiveEffects.Count; i++)
        {
            BattleUnit.ActiveEffectState activeEffect = unit.ActiveEffects[i];
            if (activeEffect == null || string.IsNullOrWhiteSpace(activeEffect.effectId))
            {
                continue;
            }

            string effectName = activeEffect.effectId;
            if (database != null)
            {
                EffectDatabase.EffectEntry effectEntry = database.FindEntry(activeEffect.effectId);
                if (effectEntry != null)
                {
                    effectName = 解析效果调试名称(effectEntry);
                }
            }

            string stackText = activeEffect.stackCount > 1 ? $" x{activeEffect.stackCount}" : string.Empty;
            parts.Add($"{effectName}({Mathf.Max(0, activeEffect.remainingTurns)}回合{stackText})");
        }

        return parts.Count > 0 ? string.Join("，", parts) : "无";
    }

    public static string 解析效果调试名称(EffectDatabase.EffectEntry effectEntry)
    {
        if (effectEntry == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(effectEntry.displayName)
            ? effectEntry.displayName
            : effectEntry.effectId;
    }

    public static float 应用抗性(float damage, BattleUnit caster, BattleUnit target, DamageAttributeType attributeType, string skillSource = null)
    {
        if (damage <= 0f)
        {
            return 0f;
        }

        int resistance = 解析抗性(target, attributeType);
        int penetration = 解析抗性穿透(caster, attributeType, skillSource);
        int finalResistance = Mathf.Max(0, resistance - penetration);
        float multiplier = 1f - (Mathf.Clamp(finalResistance, 0, 100) / 100f);
        return Mathf.Max(0f, damage * multiplier);
    }

    private bool 判定暴击(int criticalChance)
    {
        criticalChance = Mathf.Max(0, criticalChance);
        if (criticalChance >= maxHitChancePercent)
        {
            return true;
        }

        if (criticalChance <= minHitChancePercent)
        {
            return false;
        }

        return Random.Range(0, maxHitChancePercent) < criticalChance;
    }

    private void 构建伤害分量(
        List<DamageComponent> components,
        float baseAttackPower,
        WeaponEnchantmentAttackPower enchantmentAttackPower,
        float damageMultiplier,
        float criticalMultiplier,
        ItemDatabase.WeaponDamageDistribution distribution,
        BattleUnit caster,
        BattleUnit target,
        string skillSource = null)
    {
        components.Clear();
        if ((baseAttackPower <= 0f && enchantmentAttackPower.Total <= 0f) || distribution == null)
        {
            return;
        }

        int distributionTotal = Mathf.Max(0, distribution.Total);
        if (distributionTotal <= 0)
        {
            return;
        }

        float finalMultiplier = Mathf.Max(0f, damageMultiplier) * Mathf.Max(0f, criticalMultiplier);
        添加伤害分量(components, DamageAttributeType.Physical, baseAttackPower, distribution.physical, distributionTotal, enchantmentAttackPower.physical, finalMultiplier, caster, target, skillSource);
        添加伤害分量(components, DamageAttributeType.Fire, baseAttackPower, distribution.fire, distributionTotal, enchantmentAttackPower.fire, finalMultiplier, caster, target, skillSource);
        添加伤害分量(components, DamageAttributeType.Corruption, baseAttackPower, distribution.corruption, distributionTotal, enchantmentAttackPower.corruption, finalMultiplier, caster, target, skillSource);
        添加伤害分量(components, DamageAttributeType.Cold, baseAttackPower, distribution.cold, distributionTotal, enchantmentAttackPower.cold, finalMultiplier, caster, target, skillSource);
    }

    private void 添加伤害分量(
        List<DamageComponent> components,
        DamageAttributeType attributeType,
        float baseAttackPower,
        int distributionValue,
        int distributionTotal,
        float enchantmentAttackPower,
        float finalMultiplier,
        BattleUnit caster,
        BattleUnit target,
        string skillSource = null)
    {
        if (components == null || finalMultiplier <= 0f || distributionTotal <= 0)
        {
            return;
        }

        float amount = ((Mathf.Max(0f, baseAttackPower) * distributionValue / distributionTotal) + enchantmentAttackPower) * finalMultiplier;
        添加固定伤害分量(components, attributeType, amount, caster, target, skillSource);
    }

    private void 添加固定伤害分量(
        List<DamageComponent> components,
        DamageAttributeType attributeType,
        float amount,
        BattleUnit caster,
        BattleUnit target,
        string skillSource = null)
    {
        if (components == null || amount <= 0f)
        {
            return;
        }

        float mitigatedAmount = 应用抗性(amount, caster, target, attributeType, skillSource);
        if (mitigatedAmount <= 0f)
        {
            return;
        }

        components.Add(new DamageComponent
        {
            attributeType = attributeType,
            amount = mitigatedAmount
        });
    }

    private static ItemDatabase.WeaponCategory 解析武器类型(string characterId, string skillSource)
    {
        return string.IsNullOrWhiteSpace(skillSource)
            ? InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(characterId)
            : InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(characterId, skillSource);
    }

    private void 构建法术伤害分量(
        List<DamageComponent> components,
        float totalDamage,
        BattleSkillDatabase.SkillEntry skill,
        BattleUnit caster,
        BattleUnit target)
    {
        components.Clear();
        if (totalDamage <= 0f || skill == null)
        {
            return;
        }

        添加固定伤害分量(
            components,
            转换法术伤害类型(skill.damageType),
            totalDamage,
            caster,
            target);
    }

    private static DamageAttributeType 转换法术伤害类型(BattleSkillDatabase.DamageType damageType)
    {
        switch (damageType)
        {
            case BattleSkillDatabase.DamageType.Fire:
                return DamageAttributeType.Fire;
            case BattleSkillDatabase.DamageType.Corruption:
                return DamageAttributeType.Corruption;
            case BattleSkillDatabase.DamageType.Cold:
                return DamageAttributeType.Cold;
            default:
                return DamageAttributeType.Physical;
        }
    }

    private static int 解析抗性穿透(BattleUnit caster, DamageAttributeType attributeType, string skillSource)
    {
        if (caster == null)
        {
            return 0;
        }

        int basePenetration;
        ItemDatabase.ResistanceModifierType resistanceType;
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                basePenetration = caster.FireResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Fire;
                break;
            case DamageAttributeType.Corruption:
                basePenetration = caster.CorruptionResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Corruption;
                break;
            case DamageAttributeType.Cold:
                basePenetration = caster.ColdResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Cold;
                break;
            default:
                basePenetration = caster.PhysicalResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Physical;
                break;
        }

        int weaponPenetration = skillSource == null
            ? InventoryShortcutRuntimeBinder.GetCharacterWeaponResistancePenetration(caster.characterId, resistanceType)
            : InventoryShortcutRuntimeBinder.GetCharacterWeaponResistancePenetration(caster.characterId, skillSource, resistanceType);
        return basePenetration + weaponPenetration;
    }

    private static int 解析抗性(BattleUnit target, DamageAttributeType attributeType)
    {
        if (target == null)
        {
            return 0;
        }

        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return target.FireResistance;
            case DamageAttributeType.Corruption:
                return target.CorruptionResistance;
            case DamageAttributeType.Cold:
                return target.ColdResistance;
            default:
                return target.PhysicalResistance;
        }
    }
}
