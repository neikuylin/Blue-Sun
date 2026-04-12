using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗技能区域规则服务
{
    private BattleGrid grid;
    private List<BattleUnit> units;
    private Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> getDisplayedSkillRange;
    private Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidSkillTarget;

    public void 初始化(
        BattleGrid battleGrid,
        List<BattleUnit> battleUnits,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> displayedSkillRangeResolver,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> validSkillTargetChecker)
    {
        grid = battleGrid;
        units = battleUnits;
        getDisplayedSkillRange = displayedSkillRangeResolver;
        isValidSkillTarget = validSkillTargetChecker;
    }

    public bool 是圆轴区域技能(BattleSkillDatabase.SkillEntry skill)
    {
        return skill != null &&
            skill.skillType == BattleSkillDatabase.SkillType.Area &&
            skill.areaCastType == BattleSkillDatabase.AreaCastType.CircularAxis;
    }

    public bool 使用连续圆形区域(BattleSkillDatabase.SkillEntry skill)
    {
        return skill != null &&
            skill.skillType == BattleSkillDatabase.SkillType.Area &&
            !string.Equals(skill.skillId, BattleSkillDatabase.MoveSkillId, StringComparison.Ordinal) &&
            !是圆轴区域技能(skill);
    }

    public int 获取预览占地尺寸(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null)
        {
            return 0;
        }

        int width = Mathf.Max(1, skill.effectSize.x);
        int height = Mathf.Max(1, skill.effectSize.y);
        return Mathf.Max(width, height);
    }

    public float 获取连续区域半径世界(BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || skill == null)
        {
            return 0f;
        }

        return grid.GetAreaRadiusWorld(获取预览占地尺寸(skill));
    }

    public bool 是否位于连续圆形区域内(BattleUnit target, Vector2Int centerCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || target == null || skill == null || !grid.IsInside(centerCell))
        {
            return false;
        }

        Vector3 areaCenter = grid.GetWorldPosition(centerCell);
        Vector3 targetCenter = grid.GetWorldPosition(target.currentCell);
        float maxDistance = 获取连续区域半径世界(skill) + grid.GetUnitRadiusWorld(target);
        return Vector3.Distance(areaCenter, targetCenter) <= maxDistance + 0.001f;
    }

    public Vector3 解析轴向方向世界(BattleUnit caster, Vector2Int targetCell)
    {
        if (caster == null || grid == null)
        {
            return Vector3.right;
        }

        Vector3 origin = grid.GetWorldPosition(caster.currentCell);
        Vector3 target = grid.GetWorldPosition(targetCell);
        Vector3 direction = target - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction.normalized;
        }

        Vector3 forward = caster.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            return forward.normalized;
        }

        return Vector3.right;
    }

    public float 获取轴向范围世界(BattleUnit caster, BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || caster == null || skill == null || getDisplayedSkillRange == null)
        {
            return 0f;
        }

        return grid.GetCastRadiusWorld(caster, getDisplayedSkillRange(caster, skill));
    }

    public float 获取轴向宽度世界(BattleSkillDatabase.SkillEntry skill)
    {
        return grid == null || skill == null
            ? 0f
            : Mathf.Max(1, skill.axisWidth) * grid.cellSize;
    }

    public bool 是否位于圆轴区域内(BattleUnit caster, BattleUnit target, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || caster == null || target == null || skill == null)
        {
            return false;
        }

        Vector3 origin = grid.GetWorldPosition(caster.currentCell);
        Vector3 direction = 解析轴向方向世界(caster, targetCell);
        Vector3 unitCenter = grid.GetWorldPosition(target.currentCell);
        float targetRadius = grid.GetUnitRadiusWorld(target);
        float rangeWorld = 获取轴向范围世界(caster, skill);

        if (skill.circularAxisAreaType == BattleSkillDatabase.CircularAxisAreaType.Fan)
        {
            Vector3 toTarget = unitCenter - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            if (distance > rangeWorld + targetRadius + 0.001f)
            {
                return false;
            }

            float angleToTarget = Vector3.Angle(direction, toTarget);
            float extraAngle = distance <= targetRadius
                ? 180f
                : Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(targetRadius / distance, 0f, 1f));
            return angleToTarget <= (Mathf.Clamp(skill.axisAngle, 1f, 360f) * 0.5f) + extraAngle + 0.001f;
        }

        float halfWidth = 获取轴向宽度世界(skill) * 0.5f;
        Vector3 toTargetOnPlane = unitCenter - origin;
        toTargetOnPlane.y = 0f;
        float forwardDistance = Vector3.Dot(toTargetOnPlane, direction);
        Vector3 right = new Vector3(-direction.z, 0f, direction.x);
        float lateralDistance = Mathf.Abs(Vector3.Dot(toTargetOnPlane, right));
        float centerDistance = toTargetOnPlane.magnitude;

        if (forwardDistance < -targetRadius)
        {
            return false;
        }

        if (centerDistance > rangeWorld + targetRadius + 0.001f)
        {
            return false;
        }

        if (lateralDistance > halfWidth + targetRadius + 0.001f)
        {
            return false;
        }

        if (forwardDistance < 0f)
        {
            return targetRadius >= -forwardDistance;
        }

        float arcStartForward = Mathf.Sqrt(Mathf.Max(0f, (rangeWorld * rangeWorld) - (halfWidth * halfWidth)));
        if (forwardDistance <= arcStartForward)
        {
            return lateralDistance <= halfWidth + targetRadius + 0.001f;
        }

        return centerDistance <= rangeWorld + targetRadius + 0.001f;
    }

    public HashSet<Vector2Int> 收集区域效果格(BattleUnit caster, Vector2Int centerCell, BattleSkillDatabase.SkillEntry skill)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        if (grid == null || skill == null || !grid.IsInside(centerCell))
        {
            return result;
        }

        if (是圆轴区域技能(skill))
        {
            return result;
        }

        int footprintSize = 获取预览占地尺寸(skill);
        if (footprintSize <= 0)
        {
            return result;
        }

        int footprintRadius = Mathf.Max(0, footprintSize / 2);
        for (int y = centerCell.y - footprintRadius; y <= centerCell.y + footprintRadius; y++)
        {
            for (int x = centerCell.x - footprintRadius; x <= centerCell.x + footprintRadius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (grid.IsInside(cell))
                {
                    result.Add(cell);
                }
            }
        }

        return result;
    }

    public bool 是否位于区域格内(BattleUnit target, HashSet<Vector2Int> areaCells)
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

    public List<BattleUnit> 收集区域技能目标(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        List<BattleUnit> targets = new List<BattleUnit>();
        if (caster == null || skill == null || units == null)
        {
            return targets;
        }

        bool useContinuousCircularArea = 使用连续圆形区域(skill);
        bool useCircularAxisArea = 是圆轴区域技能(skill);
        HashSet<Vector2Int> affectedCells = null;
        if (!useContinuousCircularArea && !useCircularAxisArea)
        {
            affectedCells = 收集区域效果格(caster, targetCell, skill);
        }

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
                if (!是否位于连续圆形区域内(unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (useCircularAxisArea)
            {
                if (!是否位于圆轴区域内(caster, unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (!是否位于区域格内(unit, affectedCells))
            {
                continue;
            }

            targets.Add(unit);
        }

        return targets;
    }
}
