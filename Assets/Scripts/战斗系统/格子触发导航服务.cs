using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class 格子触发导航服务
{
    private MonoBehaviour 协程宿主;
    private BattleGrid grid;
    private Func<BattleUnit, Vector2Int, bool> 尝试移动到格子;
    private Coroutine pendingRoutine;

    public void 初始化(
        MonoBehaviour 协程宿主,
        BattleGrid grid,
        Func<BattleUnit, Vector2Int, bool> 尝试移动到格子)
    {
        清除();
        this.协程宿主 = 协程宿主;
        this.grid = grid;
        this.尝试移动到格子 = 尝试移动到格子;
    }

    public bool 尝试移动到触发格并执行(
        BattleUnit unit,
        IReadOnlyList<Vector2Int> triggerCells,
        Action 到达后触发)
    {
        if (unit == null || triggerCells == null || triggerCells.Count == 0 || grid == null || 协程宿主 == null || 尝试移动到格子 == null)
        {
            return false;
        }

        Vector2Int currentCell = 解析单位当前格(unit);
        if (包含格子(triggerCells, currentCell))
        {
            到达后触发?.Invoke();
            return true;
        }

        Vector2Int targetCell;
        if (!尝试寻找最近可达触发格(unit, triggerCells, out targetCell))
        {
            return false;
        }

        if (!尝试移动到格子(unit, targetCell))
        {
            return false;
        }

        清除();
        pendingRoutine = 协程宿主.StartCoroutine(等待移动到触发格后执行(unit, triggerCells, 到达后触发));
        return true;
    }

    public void 清除()
    {
        if (pendingRoutine != null && 协程宿主 != null)
        {
            协程宿主.StopCoroutine(pendingRoutine);
        }

        pendingRoutine = null;
    }

    private IEnumerator 等待移动到触发格后执行(
        BattleUnit unit,
        IReadOnlyList<Vector2Int> triggerCells,
        Action 到达后触发)
    {
        while (unit != null && unit.IsMoving)
        {
            yield return null;
        }

        pendingRoutine = null;
        if (unit == null || grid == null)
        {
            yield break;
        }

        Vector2Int currentCell = 解析单位当前格(unit);
        if (包含格子(triggerCells, currentCell))
        {
            到达后触发?.Invoke();
        }
    }

    private bool 尝试寻找最近可达触发格(
        BattleUnit unit,
        IReadOnlyList<Vector2Int> triggerCells,
        out Vector2Int targetCell)
    {
        targetCell = default;
        if (unit == null || triggerCells == null || grid == null)
        {
            return false;
        }

        bool found = false;
        int bestPathLength = int.MaxValue;
        int bestManhattanDistance = int.MaxValue;
        Vector2Int currentCell = 解析单位当前格(unit);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        for (int i = 0; i < triggerCells.Count; i++)
        {
            Vector2Int candidate = triggerCells[i];
            if (!visited.Add(candidate))
            {
                continue;
            }

            if (candidate == currentCell)
            {
                targetCell = candidate;
                return true;
            }

            if (!grid.IsWalkableIgnoringAllies(unit, candidate))
            {
                continue;
            }

            List<Vector2Int> path = grid.FindPathIgnoringAllies(unit, candidate);
            if (path == null || path.Count <= 1)
            {
                continue;
            }

            int pathLength = path.Count - 1;
            int manhattanDistance = grid.ManhattanDistance(currentCell, candidate);
            if (found &&
                (pathLength > bestPathLength ||
                 (pathLength == bestPathLength && manhattanDistance >= bestManhattanDistance)))
            {
                continue;
            }

            found = true;
            bestPathLength = pathLength;
            bestManhattanDistance = manhattanDistance;
            targetCell = candidate;
        }

        return found;
    }

    private Vector2Int 解析单位当前格(BattleUnit unit)
    {
        if (unit == null)
        {
            return default;
        }

        return grid != null && unit.IsMoving ? grid.WorldToCell(unit.transform.position) : unit.currentCell;
    }

    private static bool 包含格子(IReadOnlyList<Vector2Int> cells, Vector2Int target)
    {
        if (cells == null)
        {
            return false;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == target)
            {
                return true;
            }
        }

        return false;
    }
}
