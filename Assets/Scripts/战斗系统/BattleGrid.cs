using System.Collections.Generic;
using UnityEngine;

public class BattleGrid : MonoBehaviour
{
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public float overlayY = -0.05f;

    private readonly Dictionary<Vector2Int, BattleUnit> occupants = new Dictionary<Vector2Int, BattleUnit>();
    private readonly Dictionary<Vector2Int, Renderer> cellRenderers = new Dictionary<Vector2Int, Renderer>();

    private Material overlayMaterial;
    private Color idleColorA = new Color(0.18f, 0.26f, 0.22f, 0.12f);
    private Color idleColorB = new Color(0.22f, 0.32f, 0.26f, 0.12f);
    private Color reachableColor = new Color(0.20f, 0.70f, 1.00f, 0.45f);
    private Color attackColor = new Color(1.00f, 0.25f, 0.20f, 0.50f);
    private Color activeColor = new Color(1.00f, 0.90f, 0.20f, 0.60f);

    public void BuildVisuals()
    {
        overlayMaterial = new Material(Shader.Find("Sprites/Default"));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = "Cell_" + x + "_" + y;
                tile.transform.SetParent(transform, false);
                tile.transform.position = GetWorldPosition(cell, overlayY);
                tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                tile.transform.localScale = Vector3.one * (cellSize * 0.95f);
                tile.GetComponent<Collider>().enabled = false;

                Renderer renderer = tile.GetComponent<Renderer>();
                renderer.material = new Material(overlayMaterial);
                renderer.material.color = GetIdleColor(cell);

                cellRenderers[cell] = renderer;
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int cell, float y = 0f)
    {
        return new Vector3(cell.x * cellSize, y, cell.y * cellSize);
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / cellSize);
        int y = Mathf.RoundToInt(worldPosition.z / cellSize);
        return new Vector2Int(x, y);
    }

    public Plane GetInteractionPlane()
    {
        return new Plane(Vector3.up, Vector3.zero);
    }

    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public bool IsWalkable(BattleUnit unit, Vector2Int centerCell)
    {
        int radius = unit != null ? unit.FootprintRadius : 0;
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!IsInside(cell))
                {
                    return false;
                }

                BattleUnit occupant = GetUnitAt(cell);
                if (occupant != null && occupant != unit)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsFootprintInside(BattleUnit unit, Vector2Int centerCell)
    {
        int radius = unit != null ? unit.FootprintRadius : 0;
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                if (!IsInside(new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void RegisterUnit(BattleUnit unit)
    {
        SetOccupancy(unit, unit.currentCell, true);
    }

    public float MoveUnit(BattleUnit unit, Vector2Int destination)
    {
        List<Vector2Int> path = FindPath(unit, destination);
        if (path == null || path.Count == 0)
        {
            return 0f;
        }

        SetOccupancy(unit, unit.currentCell, false);
        SetOccupancy(unit, destination, true);

        List<Vector3> worldPositions = new List<Vector3>();
        for (int i = 1; i < path.Count; i++)
        {
            worldPositions.Add(GetWorldPosition(path[i]));
        }

        return unit.MoveAlongPath(worldPositions, destination);
    }

    public void RemoveUnit(BattleUnit unit)
    {
        SetOccupancy(unit, unit.currentCell, false);
    }

    public BattleUnit GetUnitAt(Vector2Int cell)
    {
        BattleUnit unit;
        occupants.TryGetValue(cell, out unit);
        return unit;
    }

    public int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public List<Vector2Int> FindPath(BattleUnit unit, Vector2Int destination)
    {
        if (unit == null || !IsInside(destination) || !IsWalkable(unit, destination))
        {
            return null;
        }

        Vector2Int origin = unit.currentCell;
        if (origin == destination)
        {
            return new List<Vector2Int> { origin };
        }

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        frontier.Enqueue(origin);
        cameFrom[origin] = origin;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int next = current + CardinalDirections[i];
                if (cameFrom.ContainsKey(next) || !IsInside(next) || !IsWalkable(unit, next))
                {
                    continue;
                }

                cameFrom[next] = current;
                if (next == destination)
                {
                    return BuildPath(cameFrom, origin, destination);
                }

                frontier.Enqueue(next);
            }
        }

        return null;
    }

    public void ResetHighlights()
    {
        foreach (KeyValuePair<Vector2Int, Renderer> pair in cellRenderers)
        {
            pair.Value.sharedMaterial.color = GetIdleColor(pair.Key);
            pair.Value.material.color = GetIdleColor(pair.Key);
        }
    }

    public void HighlightActive(Vector2Int cell)
    {
        SetColor(cell, activeColor);
    }

    public void HighlightFootprint(BattleUnit unit, Color color)
    {
        int radius = unit.FootprintRadius;
        for (int y = unit.currentCell.y - radius; y <= unit.currentCell.y + radius; y++)
        {
            for (int x = unit.currentCell.x - radius; x <= unit.currentCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInside(cell))
                {
                    SetColor(cell, color);
                }
            }
        }
    }

    public void HighlightAttackTargets(BattleUnit activeUnit)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                BattleUnit target = GetUnitAt(cell);
                if (target == null || target.team == activeUnit.team)
                {
                    continue;
                }

                if (ManhattanDistance(activeUnit.currentCell, cell) <= activeUnit.attackRange)
                {
                    HighlightFootprint(target, attackColor);
                }
            }
        }
    }

    public void HighlightOccupiedCells(BattleUnit ignoredUnit, Color color)
    {
        HashSet<BattleUnit> highlightedUnits = new HashSet<BattleUnit>();
        foreach (KeyValuePair<Vector2Int, BattleUnit> pair in occupants)
        {
            BattleUnit occupant = pair.Value;
            if (occupant == null || occupant == ignoredUnit || highlightedUnits.Contains(occupant))
            {
                continue;
            }

            highlightedUnits.Add(occupant);
            HighlightFootprint(occupant, color);
        }
    }

    public void HighlightOccupiedCellsWithinRange(BattleUnit activeUnit, int range, Color color)
    {
        if (activeUnit == null)
        {
            return;
        }

        HashSet<BattleUnit> highlightedUnits = new HashSet<BattleUnit>();
        foreach (KeyValuePair<Vector2Int, BattleUnit> pair in occupants)
        {
            BattleUnit occupant = pair.Value;
            if (occupant == null || occupant == activeUnit || highlightedUnits.Contains(occupant))
            {
                continue;
            }

            if (ManhattanDistance(activeUnit.currentCell, occupant.currentCell) > range)
            {
                continue;
            }

            highlightedUnits.Add(occupant);
            HighlightFootprint(occupant, color);
        }
    }

    private void SetColor(Vector2Int cell, Color color)
    {
        Renderer renderer;
        if (cellRenderers.TryGetValue(cell, out renderer))
        {
            renderer.material.color = color;
        }
    }

    public void HighlightReachable(BattleUnit unit, int range)
    {
        Vector2Int origin = unit.currentCell;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (cell == origin)
                {
                    continue;
                }

                List<Vector2Int> path = FindPath(unit, cell);
                if (path != null && path.Count > 1 && path.Count - 1 <= range)
                {
                    HighlightFootprintAt(cell, unit.footprintSize, reachableColor);
                }
            }
        }
    }

    public void HighlightFootprintAt(Vector2Int centerCell, int footprintSize, Color color)
    {
        int radius = Mathf.Max(0, footprintSize / 2);
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInside(cell))
                {
                    SetColor(cell, color);
                }
            }
        }
    }

    public void HighlightPartialFootprint(BattleUnit unit, Vector2Int centerCell, Color color)
    {
        int radius = unit != null ? unit.FootprintRadius : 0;
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!IsInside(cell))
                {
                    continue;
                }

                BattleUnit occupant = GetUnitAt(cell);
                if (occupant != null && occupant != unit)
                {
                    continue;
                }

                SetColor(cell, color);
            }
        }
    }

    private void SetOccupancy(BattleUnit unit, Vector2Int centerCell, bool occupied)
    {
        int radius = unit.FootprintRadius;
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!IsInside(cell))
                {
                    continue;
                }

                if (occupied)
                {
                    occupants[cell] = unit;
                }
                else if (occupants.ContainsKey(cell) && occupants[cell] == unit)
                {
                    occupants.Remove(cell);
                }
            }
        }
    }

    private Color GetIdleColor(Vector2Int cell)
    {
        return ((cell.x + cell.y) % 2 == 0) ? idleColorA : idleColorB;
    }

    private static List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int origin, Vector2Int destination)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = destination;
        path.Add(current);

        while (current != origin)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
