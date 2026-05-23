using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗敌方执行服务
{
    public float? 尝试移动到格子(
        MonoBehaviour host,
        BattleUnit unit,
        Vector2Int destination,
        BattleGrid grid,
        Func<string, BattleSkillDatabase.SkillEntry> resolveSkill,
        Func<BattleSkillDatabase.SkillEntry, int> getSkillManaCost,
        Func<BattleUnit, List<Vector2Int>, BattleSkillDatabase.SkillEntry, int> getMoveActionPointCost,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> getMoveMaxRange,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, string> resolveSkillActionStateName,
        Func<BattleUnit, string> resolveIdleStateName,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, bool> resolveSkillCompensateActionMotion,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, float, IEnumerator> playTrackedSkillAudioRoutine)
    {
        if (unit == null || grid == null || destination == unit.currentCell)
        {
            return null;
        }

        BattleSkillDatabase.SkillEntry moveSkill = resolveSkill != null ? resolveSkill(BattleSkillDatabase.MoveSkillId) : null;
        int moveManaCost = getSkillManaCost != null ? getSkillManaCost(moveSkill) : 0;
        if (!unit.CanSpendMana(moveManaCost))
        {
            return null;
        }

        List<Vector2Int> path = grid.FindPath(unit, destination);
        if (path == null || path.Count <= 1)
        {
            return null;
        }

        int moveActionPointCost = getMoveActionPointCost != null ? getMoveActionPointCost(unit, path, moveSkill) : 0;
        if (!unit.CanSpendActionPoints(moveActionPointCost))
        {
            return null;
        }

        int maxMoveRange = getMoveMaxRange != null ? getMoveMaxRange(unit, moveSkill) : 0;
        if (path.Count - 1 > maxMoveRange)
        {
            return null;
        }

        grid.ResetHighlights();
        float moveDuration = grid.MoveUnit(unit, destination);
        if (moveSkill != null)
        {
            if (host != null && playTrackedSkillAudioRoutine != null)
            {
                host.StartCoroutine(playTrackedSkillAudioRoutine(unit, moveSkill, moveDuration));
            }

            unit.PlayTimedAnimation(
                unit.GetMoveAnimationStateName(resolveSkillActionStateName != null ? resolveSkillActionStateName(moveSkill, unit) : string.Empty),
                moveDuration,
                unit.GetIdleAnimationStateName(resolveIdleStateName != null ? resolveIdleStateName(unit) : string.Empty),
                resolveSkillCompensateActionMotion != null && resolveSkillCompensateActionMotion(moveSkill, unit));
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        return moveDuration;
    }

    public bool 可以使用技能(BattleUnit caster, 战斗敌方回合服务.技能选项 choice, Func<BattleSkillDatabase.SkillEntry, int> getSkillActionPointCost, Func<BattleSkillDatabase.SkillEntry, int> getSkillManaCost)
    {
        if (caster == null || choice == null || choice.skill == null)
        {
            return false;
        }

        return caster.CanSpendActionPoints(getSkillActionPointCost != null ? getSkillActionPointCost(choice.skill) : 0) &&
            caster.CanSpendMana(getSkillManaCost != null ? getSkillManaCost(choice.skill) : 0);
    }

    public bool 是有效敌方技能目标(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill, Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidSkillTarget)
    {
        return target != null &&
            target.IsAlive &&
            caster != null &&
            target.team != caster.team &&
            isValidSkillTarget != null &&
            isValidSkillTarget(caster, target, skill);
    }

    public bool 可以在目标处施放(
        BattleUnit caster,
        Vector2Int targetCell,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Func<BattleUnit, Vector2Int, BattleUnit, BattleSkillDatabase.SkillEntry, List<Vector2Int>, bool> canCastSkillAt)
    {
        return canCastSkillAt != null && canCastSkillAt(caster, targetCell, target, skill, null);
    }

    public bool 可以从格子施放(
        BattleUnit caster,
        Vector2Int castCell,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        BattleGrid grid,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, int> getSkillRange,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidSkillTarget)
    {
        if (caster == null || target == null || skill == null || grid == null)
        {
            return false;
        }

        int skillRange = getSkillRange != null ? getSkillRange(skill, caster) : 0;
        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return 单位在格子施法范围内(caster, castCell, target, skillRange, grid) &&
                isValidSkillTarget != null &&
                isValidSkillTarget(caster, target, skill);
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Area)
        {
            Vector3 castPosition = grid.GetWorldPosition(castCell);
            Vector3 targetPosition = grid.GetWorldPosition(target.currentCell);
            float maxDistance = grid.GetCastRadiusWorld(caster, skillRange) + grid.GetUnitRadiusWorld(target);
            return Vector3.Distance(castPosition, targetPosition) <= maxDistance + 0.001f;
        }

        return false;
    }

    public IEnumerator 执行动作(
        BattleUnit caster,
        战斗敌方回合服务.技能动作 action,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, string, IEnumerator> executeTargetSkillRoutine,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, string, IEnumerator> executeAreaSkillRoutine)
    {
        if (caster == null || action.choice == null || action.choice.skill == null)
        {
            yield break;
        }

        if (action.choice.skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            if (action.targetUnit != null && executeTargetSkillRoutine != null)
            {
                yield return executeTargetSkillRoutine(caster, action.targetUnit, action.choice.skill, action.choice.skillSource);
            }

            yield break;
        }

        if (executeAreaSkillRoutine != null)
        {
            yield return executeAreaSkillRoutine(caster, action.targetCell, action.choice.skill, action.choice.skillSource);
        }
    }

    private static bool 单位在格子施法范围内(BattleUnit caster, Vector2Int castCell, BattleUnit target, int range, BattleGrid grid)
    {
        if (caster == null || target == null || grid == null)
        {
            return false;
        }

        int casterRadius = caster.FootprintRadius;
        int targetRadius = target.FootprintRadius;
        int clampedRange = Mathf.Max(0, range);

        for (int casterY = castCell.y - casterRadius; casterY <= castCell.y + casterRadius; casterY++)
        {
            for (int casterX = castCell.x - casterRadius; casterX <= castCell.x + casterRadius; casterX++)
            {
                Vector2Int casterFootprintCell = new Vector2Int(casterX, casterY);
                if (!grid.IsInside(casterFootprintCell))
                {
                    continue;
                }

                for (int targetY = target.currentCell.y - targetRadius; targetY <= target.currentCell.y + targetRadius; targetY++)
                {
                    for (int targetX = target.currentCell.x - targetRadius; targetX <= target.currentCell.x + targetRadius; targetX++)
                    {
                        Vector2Int targetFootprintCell = new Vector2Int(targetX, targetY);
                        if (!grid.IsInside(targetFootprintCell))
                        {
                            continue;
                        }

                        if (grid.ManhattanDistance(casterFootprintCell, targetFootprintCell) <= clampedRange)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
