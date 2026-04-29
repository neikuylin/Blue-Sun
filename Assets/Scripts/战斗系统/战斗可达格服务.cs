using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗可达格服务
{
    private static readonly Vector2Int[] 四方向 =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    internal sealed class 可达格
    {
        public Vector2Int cell;
        public int stepCount;
        public List<Vector2Int> path;
    }

    public List<可达格> 收集可达格(BattleGrid grid, BattleUnit unit, int maxStepCount, bool ignoreAlliedOccupants)
    {
        List<可达格> result = new List<可达格>();
        if (grid == null || unit == null || maxStepCount <= 0)
        {
            return result;
        }

        Vector2Int origin = unit.currentCell;
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> distanceByCell = new Dictionary<Vector2Int, int>();

        frontier.Enqueue(origin);
        cameFrom[origin] = origin;
        distanceByCell[origin] = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            int currentDistance = distanceByCell[current];
            if (currentDistance >= maxStepCount)
            {
                continue;
            }

            for (int i = 0; i < 四方向.Length; i++)
            {
                Vector2Int next = current + 四方向[i];
                if (cameFrom.ContainsKey(next) || !grid.IsInside(next))
                {
                    continue;
                }

                bool walkable = ignoreAlliedOccupants
                    ? grid.IsWalkableIgnoringAllies(unit, next)
                    : grid.IsWalkable(unit, next);
                if (!walkable)
                {
                    continue;
                }

                int nextDistance = currentDistance + 1;
                cameFrom[next] = current;
                distanceByCell[next] = nextDistance;
                frontier.Enqueue(next);

                result.Add(new 可达格
                {
                    cell = next,
                    stepCount = nextDistance,
                    path = 构建路径(cameFrom, origin, next)
                });
            }
        }

        return result;
    }

    private static List<Vector2Int> 构建路径(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int origin, Vector2Int destination)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = destination;
        path.Add(current);

        while (current != origin)
        {
            Vector2Int previous;
            if (!cameFrom.TryGetValue(current, out previous))
            {
                return new List<Vector2Int>();
            }

            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
