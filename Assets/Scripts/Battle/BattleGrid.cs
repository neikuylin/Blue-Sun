using System.Collections.Generic;
using UnityEngine;

public class BattleGrid : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public float overlayY = 0.02f;

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

    public bool IsWalkable(Vector2Int cell)
    {
        return IsInside(cell) && !occupants.ContainsKey(cell);
    }

    public void RegisterUnit(BattleUnit unit)
    {
        occupants[unit.currentCell] = unit;
    }

    public void MoveUnit(BattleUnit unit, Vector2Int destination)
    {
        occupants.Remove(unit.currentCell);
        occupants[destination] = unit;
        unit.SetCell(destination, GetWorldPosition(destination));
    }

    public void RemoveUnit(BattleUnit unit)
    {
        if (occupants.ContainsKey(unit.currentCell) && occupants[unit.currentCell] == unit)
        {
            occupants.Remove(unit.currentCell);
        }
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

    public void HighlightReachable(Vector2Int origin, int moveRange)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (ManhattanDistance(origin, cell) <= moveRange && IsWalkable(cell))
                {
                    SetColor(cell, reachableColor);
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
                    SetColor(cell, attackColor);
                }
            }
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

    private Color GetIdleColor(Vector2Int cell)
    {
        return ((cell.x + cell.y) % 2 == 0) ? idleColorA : idleColorB;
    }
}
