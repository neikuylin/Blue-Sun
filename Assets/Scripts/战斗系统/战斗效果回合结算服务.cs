using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗效果回合结算服务
{
    public void 处理回合持有效果(
        BattleUnit turnOwner,
        List<BattleUnit> units,
        Camera battleCamera,
        Func<int, BattleUnit> findUnitByInstanceId,
        Func<EffectDatabase.StatModifier.HealthDamageType, Color> resolveEffectDamagePopupColor)
    {
        if (turnOwner == null)
        {
            return;
        }

        EffectDatabase effectDatabase = EffectDatabase.LoadDefault();
        if (effectDatabase == null)
        {
            return;
        }

        for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            BattleUnit unit = units[unitIndex];
            if (unit == null || !unit.IsAlive || unit.ActiveEffects == null || unit.ActiveEffects.Count == 0)
            {
                continue;
            }

            for (int effectIndex = unit.ActiveEffects.Count - 1; effectIndex >= 0; effectIndex--)
            {
                BattleUnit.ActiveEffectState activeEffect = unit.ActiveEffects[effectIndex];
                if (activeEffect == null)
                {
                    continue;
                }

                EffectDatabase.EffectEntry effectEntry = effectDatabase.FindEntry(activeEffect.effectId);
                if (effectEntry == null)
                {
                    unit.RemoveActiveEffect(activeEffect);
                    continue;
                }

                if (!unit.ShouldAdvanceEffectOnTurn(turnOwner, activeEffect))
                {
                    continue;
                }

                应用回合生命修正(
                    unit,
                    activeEffect,
                    effectEntry,
                    battleCamera,
                    findUnitByInstanceId,
                    resolveEffectDamagePopupColor);
                unit.ConsumeEffectTurn(activeEffect);

                if (activeEffect.remainingTurns <= 0)
                {
                    unit.RemoveActiveEffect(activeEffect);
                }
            }

            unit.NormalizeRuntimeState();
        }
    }

    private static void 应用回合生命修正(
        BattleUnit target,
        BattleUnit.ActiveEffectState activeEffect,
        EffectDatabase.EffectEntry effectEntry,
        Camera battleCamera,
        Func<int, BattleUnit> findUnitByInstanceId,
        Func<EffectDatabase.StatModifier.HealthDamageType, Color> resolveEffectDamagePopupColor)
    {
        if (target == null || activeEffect == null || effectEntry == null || effectEntry.statModifiers == null || effectEntry.statModifiers.Count == 0)
        {
            return;
        }

        BattleUnit sourceUnit = findUnitByInstanceId != null
            ? findUnitByInstanceId(activeEffect.sourceUnitInstanceId)
            : null;
        int stackCount = Mathf.Max(1, activeEffect.stackCount);
        for (int modifierIndex = 0; modifierIndex < effectEntry.statModifiers.Count; modifierIndex++)
        {
            EffectDatabase.StatModifier modifier = effectEntry.statModifiers[modifierIndex];
            if (modifier == null ||
                (modifier.statField != EffectDatabase.CharacterStatField.TargetHealth &&
                 modifier.statField != EffectDatabase.CharacterStatField.MaxHealth))
            {
                continue;
            }

            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
            {
                int delta = 解析当前生命变化(target, sourceUnit, modifier);
                if (delta == 0)
                {
                    continue;
                }

                target.ApplyCurrentHealthDelta(delta);
                if (delta < 0)
                {
                    BattleDamageNumberPopup.ShowConfiguredText(
                        target,
                        Mathf.Abs(delta).ToString(),
                        BattleDamageNumberPopup.ConfiguredPopupKind.Damage,
                        resolveEffectDamagePopupColor != null
                            ? resolveEffectDamagePopupColor(modifier.healthDamageType)
                            : Color.white,
                        battleCamera);
                }

            }
        }
    }

    private static int 解析当前生命变化(BattleUnit target, BattleUnit sourceUnit, EffectDatabase.StatModifier modifier)
    {
        if (target == null || modifier == null)
        {
            return 0;
        }

        float percentBase = modifier.statField == EffectDatabase.CharacterStatField.MaxHealth
            ? target.GetEffectiveMaxHealth()
            : target.currentHealth;
        float rawDelta = modifier.amountMode == EffectDatabase.StatModifier.AmountMode.Percent
            ? percentBase * modifier.amount / 100f
            : modifier.amount;
        int roundedDelta = Mathf.RoundToInt(rawDelta);
        if (roundedDelta >= 0)
        {
            return roundedDelta;
        }

        float mitigatedDamage = 战斗技能基础结算服务.应用抗性(
            Mathf.Abs(roundedDelta),
            sourceUnit,
            target,
            解析效果伤害属性(modifier.healthDamageType));
        return -Mathf.RoundToInt(mitigatedDamage);
    }

    private static DamageAttributeType 解析效果伤害属性(EffectDatabase.StatModifier.HealthDamageType damageType)
    {
        switch (damageType)
        {
            case EffectDatabase.StatModifier.HealthDamageType.Fire:
                return DamageAttributeType.Fire;
            case EffectDatabase.StatModifier.HealthDamageType.Corruption:
                return DamageAttributeType.Corruption;
            case EffectDatabase.StatModifier.HealthDamageType.Cold:
                return DamageAttributeType.Cold;
            default:
                return DamageAttributeType.Physical;
        }
    }
}
