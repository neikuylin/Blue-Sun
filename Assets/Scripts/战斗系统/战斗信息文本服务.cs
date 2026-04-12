using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗信息文本服务
{
    public const string 中性信息颜色 = BattleInfoTextUtility.NeutralInfoColorHex;
    private const string 物理信息颜色 = BattleInfoTextUtility.PhysicalInfoColorHex;
    private const string 火焰信息颜色 = BattleInfoTextUtility.FireInfoColorHex;
    private const string 腐蚀信息颜色 = BattleInfoTextUtility.CorruptionInfoColorHex;
    private const string 寒冷信息颜色 = BattleInfoTextUtility.ColdInfoColorHex;

    private BattleInfoWindowPresenter presenter;

    public void 绑定显示器(BattleInfoWindowPresenter battleInfoWindowPresenter)
    {
        presenter = battleInfoWindowPresenter;
    }

    public void 显示消息(string message)
    {
        BattleInfoWindowPresenter nextPresenter = presenter != null
            ? presenter
            : BattleInfoWindowPresenter.FindInActiveScene();
        presenter = nextPresenter;
        if (nextPresenter == null)
        {
            return;
        }

        nextPresenter.ShowMessage(message);
    }

    public string 解析单位名(BattleUnit unit, bool richText = false)
    {
        return BattleInfoTextUtility.ResolveBattleInfoUnitName(unit, richText);
    }

    public string 解析技能名(BattleSkillDatabase.SkillEntry skill)
    {
        return BattleInfoTextUtility.ResolveBattleInfoSkillName(skill);
    }

    public string 包装颜色(string content, string colorHex)
    {
        return BattleInfoTextUtility.WrapBattleInfoColor(content, colorHex);
    }

    public string 构建伤害信息文本(CombatDamageResult damageResult)
    {
        if (damageResult == null || damageResult.components.Count == 0)
        {
            return $"{包装颜色("0", 物理信息颜色)}{包装颜色("点伤害", 中性信息颜色)}";
        }

        List<string> parts = new List<string>();
        List<DamageDisplayAllocation> allocations = 构建伤害显示分配(damageResult);
        for (int i = 0; i < allocations.Count; i++)
        {
            DamageDisplayAllocation allocation = allocations[i];
            if (allocation.displayAmount <= 0)
            {
                continue;
            }

            string attributeColorHex = 获取伤害属性颜色(allocation.attributeType);
            string amountText = 包装颜色(allocation.displayAmount.ToString(), attributeColorHex);
            string attributeText = 包装颜色(获取伤害属性显示名(allocation.attributeType), attributeColorHex);
            string suffixText = 包装颜色("伤害", attributeColorHex);
            parts.Add($"{amountText}{attributeText}{suffixText}");
        }

        if (parts.Count == 0)
        {
            return $"{包装颜色("0", 物理信息颜色)}{包装颜色("点伤害", 中性信息颜色)}";
        }

        return string.Join(包装颜色("和", 中性信息颜色), parts);
    }

    public string 构建单位死亡信息(BattleUnit unit)
    {
        if (unit == null || unit.IsAlive)
        {
            return string.Empty;
        }

        return 包装颜色($"，{解析单位名(unit, richText: true)}死亡", 中性信息颜色);
    }

    public string 构建暴击信息(bool isCritical)
    {
        return isCritical
            ? 包装颜色("触发了暴击，", 物理信息颜色)
            : string.Empty;
    }

    public string 构建范围暴击信息(bool isCritical)
    {
        return isCritical
            ? 包装颜色("触发暴击，", 物理信息颜色)
            : string.Empty;
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

    private static string 获取伤害属性显示名(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return "火焰";
            case DamageAttributeType.Corruption:
                return "腐蚀";
            case DamageAttributeType.Cold:
                return "寒冷";
            default:
                return "物理";
        }
    }

    private static string 获取伤害属性颜色(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return 火焰信息颜色;
            case DamageAttributeType.Corruption:
                return 腐蚀信息颜色;
            case DamageAttributeType.Cold:
                return 寒冷信息颜色;
            default:
                return 物理信息颜色;
        }
    }
}
