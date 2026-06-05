using System;
using System.Collections.Generic;
using UnityEngine;

internal enum DamageAttributeType
{
    Physical,
    Fire,
    Corruption,
    Cold
}

internal struct DamageComponent
{
    public DamageAttributeType attributeType;
    public float amount;
}

internal struct WeaponEnchantmentAttackPower
{
    public float physical;
    public float fire;
    public float corruption;
    public float cold;

    public float Total => physical + fire + corruption + cold;
}

internal sealed class CombatDamageResult
{
    public readonly List<DamageComponent> components = new List<DamageComponent>();
    public bool isCritical;
    public float totalDamage;
    public int appliedDamage;
}

internal struct DamageDisplayAllocation
{
    public DamageAttributeType attributeType;
    public float amount;
    public int displayAmount;
    public float fractionalPart;
}

internal sealed class 战斗伤害结算服务
{
    public void 应用战技单体伤害(
        BattleUnit caster,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> rollSkillHit,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int, CombatDamageResult> calculateCombatArtDamage,
        int hitIndex,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry> applyAttachedEffectsToUnit,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> showZeroDamagePopup,
        Action<BattleUnit, CombatDamageResult> showDamagePopup,
        Action<BattleUnit> playDodgeReaction,
        Action<BattleUnit, CombatDamageResult> playHitReaction,
        Action<BattleUnit> handleUnitDefeat)
    {
        if (caster == null || target == null || skill == null)
        {
            return;
        }

        if (skill.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            return;
        }

        if (rollSkillHit == null || !rollSkillHit(caster, target, skill))
        {
            playDodgeReaction?.Invoke(target);
            BattleDamageNumberPopup.ShowMiss(target, battleCamera);
            return;
        }

        CombatDamageResult damageResult = calculateCombatArtDamage != null
            ? calculateCombatArtDamage(caster, target, skill, hitIndex)
            : null;
        if (damageResult == null || damageResult.appliedDamage <= 0)
        {
            applyAttachedEffectsToUnit?.Invoke(caster, target, skill);
            showZeroDamagePopup?.Invoke(target, skill);
            return;
        }

        playHitReaction?.Invoke(target, damageResult);
        target.ApplyDamage(damageResult.appliedDamage);
        applyAttachedEffectsToUnit?.Invoke(caster, target, skill);
        showDamagePopup?.Invoke(target, damageResult);
        handleUnitDefeat?.Invoke(target);
    }

    public void 结算单体技能并显示信息(
        BattleUnit caster,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Func<BattleUnit, string> formatUnitEffectDebugText,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> calculateSkillHitChance,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> rollSkillHit,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int, CombatDamageResult> calculateSkillDamage,
        int hitIndex,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry> applyAttachedEffectsToUnit,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> showZeroDamagePopup,
        Action<BattleUnit, CombatDamageResult> showDamagePopup,
        Action<BattleUnit> playDodgeReaction,
        Action<BattleUnit, CombatDamageResult> playHitReaction,
        Action<BattleUnit> handleUnitDefeat,
        Func<BattleUnit, string> resolveBattleInfoUnitName,
        Func<BattleSkillDatabase.SkillEntry, string> resolveBattleInfoSkillName,
        Func<CombatDamageResult, string> formatBattleInfoDamageText,
        Func<BattleUnit, string> buildUnitDefeatMessage,
        Func<bool, string> buildCriticalBattleInfoText,
        Func<string, string, string> wrapBattleInfoColor,
        string neutralInfoColorHex,
        Action<string> showBattleInfoMessage)
    {
        string message = 应用单体技能伤害并生成消息(
            caster,
            target,
            skill,
            battleCamera,
            formatUnitEffectDebugText,
            calculateSkillHitChance,
            rollSkillHit,
            calculateSkillDamage,
            hitIndex,
            applyAttachedEffectsToUnit,
            showZeroDamagePopup,
            showDamagePopup,
            playDodgeReaction,
            playHitReaction,
            handleUnitDefeat,
            resolveBattleInfoUnitName,
            resolveBattleInfoSkillName,
            formatBattleInfoDamageText,
            buildUnitDefeatMessage,
            buildCriticalBattleInfoText,
            wrapBattleInfoColor,
            neutralInfoColorHex);
        showBattleInfoMessage?.Invoke(message);
    }

    public void 应用战技范围伤害(
        BattleUnit caster,
        Vector2Int targetCell,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, List<BattleUnit>> collectAreaSkillTargets,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> rollSkillHit,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int, CombatDamageResult> calculateCombatArtDamage,
        int hitIndex,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry> applyAttachedEffectsToUnit,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> showZeroDamagePopup,
        Action<BattleUnit, CombatDamageResult> showDamagePopup,
        Action<BattleUnit> playDodgeReaction,
        Action<BattleUnit, CombatDamageResult> playHitReaction,
        Action<BattleUnit> handleUnitDefeat)
    {
        if (caster == null || skill == null)
        {
            return;
        }

        if (skill.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            return;
        }

        List<BattleUnit> targets = collectAreaSkillTargets != null
            ? collectAreaSkillTargets(caster, targetCell, skill)
            : null;
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            if (rollSkillHit == null || !rollSkillHit(caster, unit, skill))
            {
                playDodgeReaction?.Invoke(unit);
                BattleDamageNumberPopup.ShowMiss(unit, battleCamera);
                continue;
            }

            CombatDamageResult damageResult = calculateCombatArtDamage != null
                ? calculateCombatArtDamage(caster, unit, skill, hitIndex)
                : null;
            if (damageResult == null || damageResult.appliedDamage <= 0)
            {
                applyAttachedEffectsToUnit?.Invoke(caster, unit, skill);
                showZeroDamagePopup?.Invoke(unit, skill);
                continue;
            }

            playHitReaction?.Invoke(unit, damageResult);
            unit.ApplyDamage(damageResult.appliedDamage);
            applyAttachedEffectsToUnit?.Invoke(caster, unit, skill);
            showDamagePopup?.Invoke(unit, damageResult);
            handleUnitDefeat?.Invoke(unit);
        }
    }

    public void 结算范围技能并显示信息(
        BattleUnit caster,
        Vector2Int targetCell,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, List<BattleUnit>> collectAreaSkillTargets,
        Func<BattleUnit, string> formatUnitEffectDebugText,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> calculateSkillHitChance,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> rollSkillHit,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int, CombatDamageResult> calculateSkillDamage,
        int hitIndex,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry> applyAttachedEffectsToUnit,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> showZeroDamagePopup,
        Action<BattleUnit, CombatDamageResult> showDamagePopup,
        Action<BattleUnit> playDodgeReaction,
        Action<BattleUnit, CombatDamageResult> playHitReaction,
        Action<BattleUnit> handleUnitDefeat,
        Func<BattleUnit, string> resolveBattleInfoUnitName,
        Func<BattleSkillDatabase.SkillEntry, string> resolveBattleInfoSkillName,
        Func<CombatDamageResult, string> formatBattleInfoDamageText,
        Func<BattleUnit, string> buildUnitDefeatMessage,
        Func<bool, string> buildCriticalBattleInfoText,
        Func<string, string, string> wrapBattleInfoColor,
        string neutralInfoColorHex,
        Action<string> showBattleInfoMessage)
    {
        string message = 应用范围技能伤害并生成消息(
            caster,
            targetCell,
            skill,
            battleCamera,
            collectAreaSkillTargets,
            formatUnitEffectDebugText,
            calculateSkillHitChance,
            rollSkillHit,
            calculateSkillDamage,
            hitIndex,
            applyAttachedEffectsToUnit,
            showZeroDamagePopup,
            showDamagePopup,
            playDodgeReaction,
            playHitReaction,
            handleUnitDefeat,
            resolveBattleInfoUnitName,
            resolveBattleInfoSkillName,
            formatBattleInfoDamageText,
            buildUnitDefeatMessage,
            buildCriticalBattleInfoText,
            wrapBattleInfoColor,
            neutralInfoColorHex);
        showBattleInfoMessage?.Invoke(message);
    }

    private static string 应用单体技能伤害并生成消息(
        BattleUnit caster,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Func<BattleUnit, string> formatUnitEffectDebugText,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> calculateSkillHitChance,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> rollSkillHit,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int, CombatDamageResult> calculateSkillDamage,
        int hitIndex,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry> applyAttachedEffectsToUnit,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> showZeroDamagePopup,
        Action<BattleUnit, CombatDamageResult> showDamagePopup,
        Action<BattleUnit> playDodgeReaction,
        Action<BattleUnit, CombatDamageResult> playHitReaction,
        Action<BattleUnit> handleUnitDefeat,
        Func<BattleUnit, string> resolveBattleInfoUnitName,
        Func<BattleSkillDatabase.SkillEntry, string> resolveBattleInfoSkillName,
        Func<CombatDamageResult, string> formatBattleInfoDamageText,
        Func<BattleUnit, string> buildUnitDefeatMessage,
        Func<bool, string> buildCriticalBattleInfoText,
        Func<string, string, string> wrapBattleInfoColor,
        string neutralInfoColorHex)
    {
        if (caster == null || target == null || skill == null)
        {
            return string.Empty;
        }

        string casterName = resolveBattleInfoUnitName != null ? resolveBattleInfoUnitName(caster) : caster.unitName;
        string targetName = resolveBattleInfoUnitName != null ? resolveBattleInfoUnitName(target) : target.unitName;
        string skillName = resolveBattleInfoSkillName != null ? resolveBattleInfoSkillName(skill) : skill.skillId;

        if (rollSkillHit == null || !rollSkillHit(caster, target, skill))
        {
            playDodgeReaction?.Invoke(target);
            BattleDamageNumberPopup.ShowMiss(target, battleCamera);
            return $"{casterName}对{targetName}使用了{skillName}，被{targetName}闪避了";
        }

        CombatDamageResult damageResult = calculateSkillDamage != null
            ? calculateSkillDamage(caster, target, skill, hitIndex)
            : null;
        if (damageResult == null || damageResult.appliedDamage <= 0)
        {
            applyAttachedEffectsToUnit?.Invoke(caster, target, skill);
            showZeroDamagePopup?.Invoke(target, skill);
            return $"{casterName}对{targetName}使用了{skillName}";
        }

        playHitReaction?.Invoke(target, damageResult);
        target.ApplyDamage(damageResult.appliedDamage);
        applyAttachedEffectsToUnit?.Invoke(caster, target, skill);
        showDamagePopup?.Invoke(target, damageResult);
        handleUnitDefeat?.Invoke(target);
        string criticalText = buildCriticalBattleInfoText != null ? buildCriticalBattleInfoText(damageResult.isCritical) : string.Empty;
        string damageText = formatBattleInfoDamageText != null ? formatBattleInfoDamageText(damageResult) : damageResult.appliedDamage.ToString();
        string deathText = buildUnitDefeatMessage != null ? buildUnitDefeatMessage(target) : string.Empty;
        string message = $"{casterName}对{targetName}使用了{skillName}，{criticalText}对{targetName}造成{damageText}{deathText}";
        return wrapBattleInfoColor != null
            ? wrapBattleInfoColor(message, neutralInfoColorHex)
            : message;
    }

    private static string 应用范围技能伤害并生成消息(
        BattleUnit caster,
        Vector2Int targetCell,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, List<BattleUnit>> collectAreaSkillTargets,
        Func<BattleUnit, string> formatUnitEffectDebugText,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> calculateSkillHitChance,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> rollSkillHit,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int, CombatDamageResult> calculateSkillDamage,
        int hitIndex,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry> applyAttachedEffectsToUnit,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> showZeroDamagePopup,
        Action<BattleUnit, CombatDamageResult> showDamagePopup,
        Action<BattleUnit> playDodgeReaction,
        Action<BattleUnit, CombatDamageResult> playHitReaction,
        Action<BattleUnit> handleUnitDefeat,
        Func<BattleUnit, string> resolveBattleInfoUnitName,
        Func<BattleSkillDatabase.SkillEntry, string> resolveBattleInfoSkillName,
        Func<CombatDamageResult, string> formatBattleInfoDamageText,
        Func<BattleUnit, string> buildUnitDefeatMessage,
        Func<bool, string> buildCriticalBattleInfoText,
        Func<string, string, string> wrapBattleInfoColor,
        string neutralInfoColorHex)
    {
        if (caster == null || skill == null)
        {
            return string.Empty;
        }

        string casterName = resolveBattleInfoUnitName != null ? resolveBattleInfoUnitName(caster) : caster.unitName;
        string skillName = resolveBattleInfoSkillName != null ? resolveBattleInfoSkillName(skill) : skill.skillId;
        List<string> hitTargets = new List<string>();
        List<string> missedTargets = new List<string>();
        List<BattleUnit> targets = collectAreaSkillTargets != null
            ? collectAreaSkillTargets(caster, targetCell, skill)
            : null;
        if (targets == null)
        {
            return $"{casterName}在{targetCell}使用了{skillName}";
        }

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            string unitName = resolveBattleInfoUnitName != null ? resolveBattleInfoUnitName(unit) : unit.unitName;
            if (rollSkillHit == null || !rollSkillHit(caster, unit, skill))
            {
                playDodgeReaction?.Invoke(unit);
                BattleDamageNumberPopup.ShowMiss(unit, battleCamera);
                missedTargets.Add(unitName);
                continue;
            }

            CombatDamageResult damageResult = calculateSkillDamage != null
                ? calculateSkillDamage(caster, unit, skill, hitIndex)
                : null;
            if (damageResult == null || damageResult.appliedDamage <= 0)
            {
                applyAttachedEffectsToUnit?.Invoke(caster, unit, skill);
                showZeroDamagePopup?.Invoke(unit, skill);
                hitTargets.Add($"{unitName}获得了效果");
                continue;
            }

            playHitReaction?.Invoke(unit, damageResult);
            unit.ApplyDamage(damageResult.appliedDamage);
            applyAttachedEffectsToUnit?.Invoke(caster, unit, skill);
            showDamagePopup?.Invoke(unit, damageResult);
            handleUnitDefeat?.Invoke(unit);
            string criticalText = buildCriticalBattleInfoText != null ? buildCriticalBattleInfoText(damageResult.isCritical) : string.Empty;
            string damageText = formatBattleInfoDamageText != null ? formatBattleInfoDamageText(damageResult) : damageResult.appliedDamage.ToString();
            string deathText = buildUnitDefeatMessage != null ? buildUnitDefeatMessage(unit) : string.Empty;
            hitTargets.Add($"{unitName}{criticalText}受到{damageText}{deathText}");
        }

        if (hitTargets.Count > 0)
        {
            string message = wrapBattleInfoColor != null
                ? wrapBattleInfoColor($"{casterName}使用了{skillName}，命中了{string.Join("、", hitTargets)}", neutralInfoColorHex)
                : $"{casterName}使用了{skillName}，命中了{string.Join("、", hitTargets)}";
            if (missedTargets.Count > 0)
            {
                message += wrapBattleInfoColor != null
                    ? wrapBattleInfoColor($"，被{string.Join("、", missedTargets)}闪避了", neutralInfoColorHex)
                    : $"，被{string.Join("、", missedTargets)}闪避了";
            }

            return message;
        }

        if (missedTargets.Count > 0)
        {
            return $"{casterName}对{string.Join("、", missedTargets)}使用了{skillName}，被{string.Join("、", missedTargets)}闪避了";
        }

        return $"{casterName}在{targetCell}使用了{skillName}";
    }
}
