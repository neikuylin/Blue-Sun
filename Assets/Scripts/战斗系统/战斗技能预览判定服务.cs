using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗技能预览判定服务
{
    private BattleGrid grid;
    private List<BattleUnit> units;
    private Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidSkillTarget;
    private Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> getDisplayedSkillRange;
    private Func<BattleSkillDatabase.SkillEntry, bool> usesContinuousCircularArea;
    private Func<BattleSkillDatabase.SkillEntry, bool> isCircularAxisAreaSkill;
    private Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, bool> isUnitInsideContinuousArea;
    private Func<BattleUnit, BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, bool> isUnitInsideCircularAxisArea;
    private Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, HashSet<Vector2Int>> collectAreaEffectCells;

    public void 初始化(
        BattleGrid battleGrid,
        List<BattleUnit> battleUnits,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> validSkillTargetChecker,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> displayedSkillRangeResolver,
        Func<BattleSkillDatabase.SkillEntry, bool> continuousCircularAreaChecker,
        Func<BattleSkillDatabase.SkillEntry, bool> circularAxisAreaChecker,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, bool> unitInsideContinuousAreaChecker,
        Func<BattleUnit, BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, bool> unitInsideCircularAxisAreaChecker,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, HashSet<Vector2Int>> areaEffectCellsCollector)
    {
        grid = battleGrid;
        units = battleUnits;
        isValidSkillTarget = validSkillTargetChecker;
        getDisplayedSkillRange = displayedSkillRangeResolver;
        usesContinuousCircularArea = continuousCircularAreaChecker;
        isCircularAxisAreaSkill = circularAxisAreaChecker;
        isUnitInsideContinuousArea = unitInsideContinuousAreaChecker;
        isUnitInsideCircularAxisArea = unitInsideCircularAxisAreaChecker;
        collectAreaEffectCells = areaEffectCellsCollector;
    }

    public List<BattleUnit> 收集悬停技能目标(
        BattleUnit caster,
        Vector2Int hoveredCell,
        BattleUnit directTarget,
        BattleSkillDatabase.SkillEntry skill)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        if (caster == null || skill == null || grid == null || units == null)
        {
            return result;
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            if (是有效悬停技能目标(caster, directTarget, skill, hoveredCell))
            {
                result.Add(directTarget);
            }

            return result;
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return result;
        }

        int displayedSkillRange = getDisplayedSkillRange != null
            ? getDisplayedSkillRange(caster, skill)
            : 0;
        if (!grid.IsCellWithinCircularRange(caster, hoveredCell, displayedSkillRange))
        {
            return result;
        }

        bool useContinuousCircularArea = usesContinuousCircularArea != null && usesContinuousCircularArea(skill);
        bool useCircularAxisArea = isCircularAxisAreaSkill != null && isCircularAxisAreaSkill(skill);
        HashSet<Vector2Int> affectedCells = null;

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            if (isValidSkillTarget == null || !isValidSkillTarget(caster, unit, skill))
            {
                continue;
            }

            if (useContinuousCircularArea)
            {
                if (isUnitInsideContinuousArea == null || !isUnitInsideContinuousArea(unit, hoveredCell, skill))
                {
                    continue;
                }
            }
            else if (useCircularAxisArea)
            {
                if (isUnitInsideCircularAxisArea == null || !isUnitInsideCircularAxisArea(caster, unit, hoveredCell, skill))
                {
                    continue;
                }
            }
            else
            {
                if (affectedCells == null)
                {
                    affectedCells = collectAreaEffectCells != null
                        ? collectAreaEffectCells(caster, hoveredCell, skill)
                        : null;
                }

                if (!是否位于区域格内(unit, affectedCells))
                {
                    continue;
                }
            }

            result.Add(unit);
        }

        return result;
    }

    private bool 是有效悬停技能目标(
        BattleUnit caster,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Vector2Int hoveredCell)
    {
        if (caster == null || target == null || skill == null || grid == null || isValidSkillTarget == null)
        {
            return false;
        }

        if (!isValidSkillTarget(caster, target, skill))
        {
            return false;
        }

        int displayedSkillRange = getDisplayedSkillRange != null
            ? getDisplayedSkillRange(caster, skill)
            : 0;

        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return grid.IsUnitWithinCircularRange(caster, target, displayedSkillRange);
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return false;
        }

        if (!grid.IsCellWithinCircularRange(caster, hoveredCell, displayedSkillRange))
        {
            return false;
        }

        if (usesContinuousCircularArea != null && usesContinuousCircularArea(skill))
        {
            return isUnitInsideContinuousArea != null &&
                isUnitInsideContinuousArea(target, hoveredCell, skill);
        }

        if (isCircularAxisAreaSkill != null && isCircularAxisAreaSkill(skill))
        {
            return isUnitInsideCircularAxisArea != null &&
                isUnitInsideCircularAxisArea(caster, target, hoveredCell, skill);
        }

        HashSet<Vector2Int> affectedCells = collectAreaEffectCells != null
            ? collectAreaEffectCells(caster, hoveredCell, skill)
            : null;
        return 是否位于区域格内(target, affectedCells);
    }

    private static bool 是否位于区域格内(BattleUnit target, HashSet<Vector2Int> areaCells)
    {
        if (target == null || areaCells == null || areaCells.Count == 0)
        {
            return false;
        }

        int unitRadius = target.FootprintRadius;
        for (int y = target.currentCell.y - unitRadius; y <= target.currentCell.y + unitRadius; y++)
        {
            for (int x = target.currentCell.x - unitRadius; x <= target.currentCell.x + unitRadius; x++)
            {
                if (areaCells.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
