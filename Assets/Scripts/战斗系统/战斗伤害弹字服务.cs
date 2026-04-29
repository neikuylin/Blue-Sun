using System.Collections.Generic;
using System.Text;
using UnityEngine;

internal sealed class 战斗伤害弹字服务
{
    public void 显示伤害弹字(
        BattleUnit target,
        CombatDamageResult damageResult,
        Camera battleCamera,
        Color physicalDamageColor,
        Color fireDamageColor,
        Color corruptionDamageColor,
        Color coldDamageColor)
    {
        if (target == null || damageResult == null)
        {
            return;
        }

        List<BattleDamageNumberPopup.DamageSegment> segments = 构建伤害分段(
            damageResult,
            physicalDamageColor,
            fireDamageColor,
            corruptionDamageColor,
            coldDamageColor);
        if (damageResult.isCritical)
        {
            string criticalDamageText = 构建暴击伤害文本(segments, damageResult.appliedDamage);
            if (!string.IsNullOrWhiteSpace(criticalDamageText))
            {
                BattleDamageNumberPopup.ShowConfiguredText(
                    target,
                    "<color=#FFD700>暴击</color>\n" + criticalDamageText,
                    BattleDamageNumberPopup.ConfiguredPopupKind.Damage,
                    Color.white,
                    battleCamera);
                return;
            }
        }

        if (segments.Count > 0)
        {
            BattleDamageNumberPopup.ShowSegments(target, segments, battleCamera);
            return;
        }

        if (damageResult.appliedDamage > 0)
        {
            BattleDamageNumberPopup.Show(target, damageResult.appliedDamage, battleCamera);
        }
    }

    public void 显示零伤害弹字(
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Camera battleCamera,
        Color physicalDamageColor)
    {
        if (target == null || skill == null || skill.noDamage)
        {
            return;
        }

        BattleDamageNumberPopup.ShowConfiguredText(
            target,
            "0",
            BattleDamageNumberPopup.ConfiguredPopupKind.Damage,
            physicalDamageColor,
            battleCamera);
    }

    private List<BattleDamageNumberPopup.DamageSegment> 构建伤害分段(
        CombatDamageResult damageResult,
        Color physicalDamageColor,
        Color fireDamageColor,
        Color corruptionDamageColor,
        Color coldDamageColor)
    {
        List<BattleDamageNumberPopup.DamageSegment> segments = new List<BattleDamageNumberPopup.DamageSegment>();
        if (damageResult == null)
        {
            return segments;
        }

        List<DamageDisplayAllocation> allocations = 构建伤害显示分配(damageResult);
        for (int i = 0; i < allocations.Count; i++)
        {
            DamageDisplayAllocation allocation = allocations[i];
            if (allocation.displayAmount <= 0)
            {
                continue;
            }

            segments.Add(new BattleDamageNumberPopup.DamageSegment
            {
                text = allocation.displayAmount.ToString(),
                color = 解析伤害颜色(
                    allocation.attributeType,
                    physicalDamageColor,
                    fireDamageColor,
                    corruptionDamageColor,
                    coldDamageColor)
            });
        }

        return segments;
    }

    private static string 构建暴击伤害文本(IList<BattleDamageNumberPopup.DamageSegment> segments, int appliedDamage)
    {
        if (segments != null && segments.Count > 0)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < segments.Count; i++)
            {
                BattleDamageNumberPopup.DamageSegment segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment.text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("<color=#FFFFFF>+</color>");
                }

                builder.Append("<color=#");
                builder.Append(ColorUtility.ToHtmlStringRGB(segment.color));
                builder.Append(">");
                builder.Append(segment.text);
                builder.Append("</color>");
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }
        }

        return appliedDamage > 0 ? appliedDamage.ToString() : string.Empty;
    }

    private static List<DamageDisplayAllocation> 构建伤害显示分配(CombatDamageResult damageResult)
    {
        List<DamageDisplayAllocation> allocations = new List<DamageDisplayAllocation>();
        if (damageResult == null)
        {
            return allocations;
        }

        int totalAssigned = 0;
        for (int i = 0; i < damageResult.components.Count; i++)
        {
            DamageComponent component = damageResult.components[i];
            if (component.amount <= 0f)
            {
                continue;
            }

            int baseAmount = Mathf.FloorToInt(component.amount);
            allocations.Add(new DamageDisplayAllocation
            {
                attributeType = component.attributeType,
                amount = component.amount,
                displayAmount = baseAmount,
                fractionalPart = component.amount - baseAmount
            });
            totalAssigned += baseAmount;
        }

        int delta = Mathf.Max(0, damageResult.appliedDamage) - totalAssigned;
        if (delta <= 0 || allocations.Count == 0)
        {
            return allocations;
        }

        allocations.Sort(比较伤害显示分配增量);
        for (int i = 0; i < delta; i++)
        {
            int index = i % allocations.Count;
            DamageDisplayAllocation allocation = allocations[index];
            allocation.displayAmount += 1;
            allocations[index] = allocation;
        }

        allocations.Sort(比较伤害显示分配输出);
        return allocations;
    }

    private static Color 解析伤害颜色(
        DamageAttributeType attributeType,
        Color physicalDamageColor,
        Color fireDamageColor,
        Color corruptionDamageColor,
        Color coldDamageColor)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return fireDamageColor;
            case DamageAttributeType.Corruption:
                return corruptionDamageColor;
            case DamageAttributeType.Cold:
                return coldDamageColor;
            default:
                return physicalDamageColor;
        }
    }

    private static int 比较伤害显示分配增量(DamageDisplayAllocation left, DamageDisplayAllocation right)
    {
        int fractionalComparison = right.fractionalPart.CompareTo(left.fractionalPart);
        if (fractionalComparison != 0)
        {
            return fractionalComparison;
        }

        int priorityComparison = 获取伤害属性显示优先级(left.attributeType).CompareTo(获取伤害属性显示优先级(right.attributeType));
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return right.amount.CompareTo(left.amount);
    }

    private static int 比较伤害显示分配输出(DamageDisplayAllocation left, DamageDisplayAllocation right)
    {
        return 获取伤害属性显示优先级(left.attributeType).CompareTo(获取伤害属性显示优先级(right.attributeType));
    }

    private static int 获取伤害属性显示优先级(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Physical:
                return 0;
            case DamageAttributeType.Fire:
                return 1;
            case DamageAttributeType.Corruption:
                return 2;
            case DamageAttributeType.Cold:
                return 3;
            default:
                return int.MaxValue;
        }
    }
}
