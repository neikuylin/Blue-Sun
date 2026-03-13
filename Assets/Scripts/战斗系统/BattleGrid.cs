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

    private static readonly Vector2Int[] OutlineDirections =
    {
        Vector2Int.right,
        Vector2Int.up,
        Vector2Int.left,
        Vector2Int.down
    };

    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public float overlayY = -0.05f;

    private readonly Dictionary<Vector2Int, BattleUnit> occupants = new Dictionary<Vector2Int, BattleUnit>();

    private Material fillMaterialTemplate;
    private Material lineMaterialTemplate;
    private Transform boardVisualRoot;
    private Transform highlightRoot;
    private Transform hoverHighlightRoot;
    private int highlightLayerOrder;
    private GameObject hoverOverlayObject;
    private MeshRenderer hoverOverlayRenderer;
    private readonly List<LineRenderer> hoverOutlineRenderers = new List<LineRenderer>();
    private HashSet<Vector2Int> hoverOverlayCells;

    private Color reachableColor = new Color(0.20f, 0.70f, 1.00f, 0.12f);
    private Color reachableOutlineColor = new Color(0.20f, 0.70f, 1.00f, 0.57f);
    private Color attackColor = new Color(1.00f, 0.25f, 0.20f, 0.26f);
    private Color activeColor = new Color(1.00f, 0.90f, 0.20f, 0.30f);
    private Color boardOutlineColor = new Color(0.85f, 0.95f, 0.90f, 0.70f);

    private struct Edge
    {
        public readonly Vector2Int start;
        public readonly Vector2Int end;

        public Edge(Vector2Int start, Vector2Int end)
        {
            this.start = start;
            this.end = end;
        }
    }

    private struct EdgeKey
    {
        public readonly Vector2Int a;
        public readonly Vector2Int b;

        public EdgeKey(Vector2Int start, Vector2Int end)
        {
            if (start.x < end.x || (start.x == end.x && start.y <= end.y))
            {
                a = start;
                b = end;
            }
            else
            {
                a = end;
                b = start;
            }
        }
    }

    public void BuildVisuals()
    {
        EnsureVisualRoots();
        ClearChildren(boardVisualRoot);
        ClearChildren(highlightRoot);
        ClearHoveredFootprint();
        ClearChildren(hoverHighlightRoot);

        fillMaterialTemplate = new Material(Shader.Find("Sprites/Default"));
        lineMaterialTemplate = new Material(Shader.Find("Sprites/Default"));

        CreateBoardOutline();
        highlightLayerOrder = 0;
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
        EnsureVisualRoots();
        ClearChildren(highlightRoot);
        highlightLayerOrder = 0;
    }

    public void HighlightActive(Vector2Int cell)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int> { cell };
        CreateOverlay(cells, activeColor, "Active");
    }

    public void HighlightFootprint(BattleUnit unit, Color color)
    {
        if (unit == null)
        {
            return;
        }

        CreateOverlay(CollectFootprintCells(unit.currentCell, unit.footprintSize), color, "Footprint");
    }

    public void SetHoveredFootprint(BattleUnit unit, Color color)
    {
        if (unit == null)
        {
            ClearHoveredFootprint();
            return;
        }

        SetHoveredFootprint(CollectFootprintCells(unit.currentCell, unit.footprintSize), color);
    }

    public void ClearHoveredFootprint()
    {
        hoverOverlayCells = null;
        hoverOverlayRenderer = null;
        hoverOutlineRenderers.Clear();

        if (hoverOverlayObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(hoverOverlayObject);
        }
        else
        {
            DestroyImmediate(hoverOverlayObject);
        }

        hoverOverlayObject = null;
    }

    public void HighlightAttackTargets(BattleUnit activeUnit)
    {
        if (activeUnit == null)
        {
            return;
        }

        HashSet<Vector2Int> attackReachableCells = CollectCellsWithinRange(activeUnit, activeUnit.attackRange);
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        HashSet<BattleUnit> highlightedUnits = new HashSet<BattleUnit>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                BattleUnit target = GetUnitAt(cell);
                if (target == null || target.team == activeUnit.team || highlightedUnits.Contains(target))
                {
                    continue;
                }

                if (HasAnyFootprintCellInRange(target, attackReachableCells))
                {
                    highlightedUnits.Add(target);
                    AddFootprintCells(cells, target.currentCell, target.footprintSize);
                }
            }
        }

        CreateOverlay(cells, attackColor, "AttackTargets");
    }

    public void HighlightOccupiedCells(BattleUnit ignoredUnit, Color color)
    {
        HashSet<BattleUnit> highlightedUnits = new HashSet<BattleUnit>();
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, BattleUnit> pair in occupants)
        {
            BattleUnit occupant = pair.Value;
            if (occupant == null || occupant == ignoredUnit || highlightedUnits.Contains(occupant))
            {
                continue;
            }

            highlightedUnits.Add(occupant);
            AddFootprintCells(cells, occupant.currentCell, occupant.footprintSize);
        }

        CreateOverlay(cells, color, "Occupied");
    }

    public void HighlightOccupiedCellsWithinRange(BattleUnit activeUnit, int range, Color color)
    {
        if (activeUnit == null)
        {
            return;
        }

        HashSet<BattleUnit> highlightedUnits = new HashSet<BattleUnit>();
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
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
            AddFootprintCells(cells, occupant.currentCell, occupant.footprintSize);
        }

        CreateOverlay(cells, color, "OccupiedInRange");
    }

    public void HighlightOccupiedUnitsWithinRange(BattleUnit activeUnit, int range, Color selfColor, Color allyColor, Color enemyColor)
    {
        if (activeUnit == null)
        {
            return;
        }

        HashSet<Vector2Int> reachableCells = CollectCellsWithinRange(activeUnit, range);
        HashSet<BattleUnit> highlightedUnits = new HashSet<BattleUnit>();
        HashSet<Vector2Int> selfCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> allyCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> enemyCells = new HashSet<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, BattleUnit> pair in occupants)
        {
            BattleUnit occupant = pair.Value;
            if (occupant == null || !occupant.IsAlive || highlightedUnits.Contains(occupant))
            {
                continue;
            }

            if (!HasAnyFootprintCellInRange(occupant, reachableCells))
            {
                continue;
            }

            highlightedUnits.Add(occupant);
            if (occupant == activeUnit)
            {
                AddFootprintCells(selfCells, occupant.currentCell, occupant.footprintSize);
            }
            else if (occupant.team == activeUnit.team)
            {
                AddFootprintCells(allyCells, occupant.currentCell, occupant.footprintSize);
            }
            else
            {
                AddFootprintCells(enemyCells, occupant.currentCell, occupant.footprintSize);
            }
        }

        CreateOverlay(selfCells, selfColor, "OccupiedSelf");
        CreateOverlay(allyCells, allyColor, "OccupiedAlly");
        CreateOverlay(enemyCells, enemyColor, "OccupiedEnemy");
    }

    public void HighlightReachable(BattleUnit unit, int range)
    {
        if (unit == null)
        {
            return;
        }

        HashSet<Vector2Int> cells = CollectReachableCells(unit, range);
        CreateOverlay(cells, reachableColor, reachableOutlineColor, "Reachable");
    }

    public void HighlightRange(BattleUnit unit, int range)
    {
        if (unit == null)
        {
            return;
        }

        HashSet<Vector2Int> cells = CollectCellsWithinRange(unit, range);
        CreateOverlay(cells, reachableColor, reachableOutlineColor, "Range");
    }

    public void HighlightFootprintAt(Vector2Int centerCell, int footprintSize, Color color)
    {
        CreateOverlay(CollectFootprintCells(centerCell, footprintSize), color, "FootprintAt");
    }

    public void HighlightPartialFootprint(int footprintSize, Vector2Int centerCell, Color color)
    {
        int radius = Mathf.Max(0, footprintSize / 2);
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
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
                if (occupant != null)
                {
                    continue;
                }

                cells.Add(cell);
            }
        }

        CreateOverlay(cells, color, "PartialFootprint");
    }

    private void EnsureVisualRoots()
    {
        if (boardVisualRoot == null)
        {
            Transform existing = transform.Find("BoardVisuals");
            if (existing != null)
            {
                boardVisualRoot = existing;
            }
            else
            {
                GameObject root = new GameObject("BoardVisuals");
                root.transform.SetParent(transform, false);
                boardVisualRoot = root.transform;
            }
        }

        if (highlightRoot == null)
        {
            Transform existing = transform.Find("HighlightVisuals");
            if (existing != null)
            {
                highlightRoot = existing;
            }
            else
            {
                GameObject root = new GameObject("HighlightVisuals");
                root.transform.SetParent(transform, false);
                highlightRoot = root.transform;
            }
        }

        if (hoverHighlightRoot == null)
        {
            Transform existing = transform.Find("HoverHighlightVisuals");
            if (existing != null)
            {
                hoverHighlightRoot = existing;
            }
            else
            {
                GameObject root = new GameObject("HoverHighlightVisuals");
                root.transform.SetParent(transform, false);
                hoverHighlightRoot = root.transform;
            }
        }
    }

    private void CreateBoardOutline()
    {
        GameObject outlineObject = new GameObject("BoardOutline");
        outlineObject.transform.SetParent(boardVisualRoot, false);

        LineRenderer line = outlineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = new Material(lineMaterialTemplate);
        line.sharedMaterial.color = boardOutlineColor;
        line.loop = true;
        line.useWorldSpace = false;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 20;
        line.numCapVertices = 20;
        line.widthMultiplier = cellSize * 0.12f;
        line.alignment = LineAlignment.View;

        float minX = -0.5f * cellSize;
        float maxX = (width - 0.5f) * cellSize;
        float minZ = -0.5f * cellSize;
        float maxZ = (height - 0.5f) * cellSize;
        float y = overlayY - 0.01f;

        line.positionCount = 4;
        line.SetPosition(0, new Vector3(minX, y, minZ));
        line.SetPosition(1, new Vector3(maxX, y, minZ));
        line.SetPosition(2, new Vector3(maxX, y, maxZ));
        line.SetPosition(3, new Vector3(minX, y, maxZ));
    }

    private void CreateOverlay(HashSet<Vector2Int> cells, Color color, string name)
    {
        CreateOverlay(cells, color, ResolveOutlineColor(color), name);
    }

    private void CreateOverlay(HashSet<Vector2Int> cells, Color fillColor, Color outlineColor, string name)
    {
        if (cells == null || cells.Count == 0)
        {
            return;
        }

        EnsureVisualRoots();

        GameObject overlay = new GameObject(name + "_" + highlightLayerOrder);
        overlay.transform.SetParent(highlightRoot, false);

        MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = overlay.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = BuildFillMesh(cells, overlayY + (highlightLayerOrder * 0.002f));
        meshRenderer.sharedMaterial = new Material(fillMaterialTemplate);
        meshRenderer.sharedMaterial.color = fillColor;
        meshRenderer.sortingOrder = highlightLayerOrder * 10;

        List<List<Vector2Int>> loops = BuildBoundaryLoops(cells);
        for (int i = 0; i < loops.Count; i++)
        {
            CreateOutlineLoop(
                overlay.transform,
                loops[i],
                outlineColor,
                overlayY + 0.001f + (highlightLayerOrder * 0.002f),
                (highlightLayerOrder * 10) + 1);
        }

        highlightLayerOrder++;
    }

    private void SetHoveredFootprint(HashSet<Vector2Int> cells, Color fillColor)
    {
        if (cells == null || cells.Count == 0)
        {
            ClearHoveredFootprint();
            return;
        }

        EnsureVisualRoots();
        Color outlineColor = ResolveOutlineColor(fillColor);

        if (hoverOverlayObject == null || hoverOverlayCells == null || !hoverOverlayCells.SetEquals(cells))
        {
            ClearHoveredFootprint();

            hoverOverlayObject = new GameObject("HoveredTarget");
            hoverOverlayObject.transform.SetParent(hoverHighlightRoot, false);

            MeshFilter meshFilter = hoverOverlayObject.AddComponent<MeshFilter>();
            hoverOverlayRenderer = hoverOverlayObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildFillMesh(cells, overlayY + 0.02f);
            hoverOverlayRenderer.sharedMaterial = new Material(fillMaterialTemplate);
            hoverOverlayRenderer.sortingOrder = 10000;

            List<List<Vector2Int>> loops = BuildBoundaryLoops(cells);
            for (int i = 0; i < loops.Count; i++)
            {
                LineRenderer line = CreateOutlineLoopRenderer(
                    hoverOverlayObject.transform,
                    loops[i],
                    outlineColor,
                    overlayY + 0.021f,
                    10001);
                if (line != null)
                {
                    hoverOutlineRenderers.Add(line);
                }
            }

            hoverOverlayCells = new HashSet<Vector2Int>(cells);
        }

        if (hoverOverlayRenderer != null && hoverOverlayRenderer.sharedMaterial != null)
        {
            hoverOverlayRenderer.sharedMaterial.color = fillColor;
        }

        for (int i = 0; i < hoverOutlineRenderers.Count; i++)
        {
            LineRenderer line = hoverOutlineRenderers[i];
            if (line != null && line.sharedMaterial != null)
            {
                line.sharedMaterial.color = outlineColor;
            }
        }
    }

    private Mesh BuildFillMesh(HashSet<Vector2Int> cells, float y)
    {
        List<Vector3> vertices = new List<Vector3>(cells.Count * 4);
        List<int> triangles = new List<int>(cells.Count * 6);

        foreach (Vector2Int cell in cells)
        {
            float minX = (cell.x - 0.5f) * cellSize;
            float maxX = (cell.x + 0.5f) * cellSize;
            float minZ = (cell.y - 0.5f) * cellSize;
            float maxZ = (cell.y + 0.5f) * cellSize;

            int vertexStart = vertices.Count;
            vertices.Add(new Vector3(minX, y, minZ));
            vertices.Add(new Vector3(maxX, y, minZ));
            vertices.Add(new Vector3(maxX, y, maxZ));
            vertices.Add(new Vector3(minX, y, maxZ));

            triangles.Add(vertexStart + 0);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 0);
            triangles.Add(vertexStart + 3);
            triangles.Add(vertexStart + 2);
        }

        Mesh mesh = new Mesh
        {
            name = "BattleGridOverlay"
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private List<List<Vector2Int>> BuildBoundaryLoops(HashSet<Vector2Int> cells)
    {
        Dictionary<EdgeKey, Edge> boundaryEdges = new Dictionary<EdgeKey, Edge>();
        foreach (Vector2Int cell in cells)
        {
            Vector2Int bottomLeft = new Vector2Int(cell.x, cell.y);
            Vector2Int bottomRight = new Vector2Int(cell.x + 1, cell.y);
            Vector2Int topRight = new Vector2Int(cell.x + 1, cell.y + 1);
            Vector2Int topLeft = new Vector2Int(cell.x, cell.y + 1);

            ToggleEdge(boundaryEdges, new Edge(bottomLeft, bottomRight));
            ToggleEdge(boundaryEdges, new Edge(bottomRight, topRight));
            ToggleEdge(boundaryEdges, new Edge(topRight, topLeft));
            ToggleEdge(boundaryEdges, new Edge(topLeft, bottomLeft));
        }

        Dictionary<Vector2Int, List<Edge>> edgesByStart = new Dictionary<Vector2Int, List<Edge>>();
        foreach (Edge edge in boundaryEdges.Values)
        {
            List<Edge> list;
            if (!edgesByStart.TryGetValue(edge.start, out list))
            {
                list = new List<Edge>();
                edgesByStart[edge.start] = list;
            }

            list.Add(edge);
        }

        List<List<Vector2Int>> loops = new List<List<Vector2Int>>();
        HashSet<EdgeKey> visited = new HashSet<EdgeKey>();
        foreach (Edge edge in boundaryEdges.Values)
        {
            EdgeKey key = new EdgeKey(edge.start, edge.end);
            if (visited.Contains(key))
            {
                continue;
            }

            List<Vector2Int> loop = new List<Vector2Int>();
            Edge current = edge;
            loop.Add(current.start);

            while (true)
            {
                loop.Add(current.end);
                visited.Add(new EdgeKey(current.start, current.end));

                if (current.end == loop[0])
                {
                    break;
                }

                List<Edge> nextCandidates;
                if (!edgesByStart.TryGetValue(current.end, out nextCandidates))
                {
                    break;
                }

                Edge? next = null;
                for (int i = 0; i < nextCandidates.Count; i++)
                {
                    Edge candidate = nextCandidates[i];
                    if (visited.Contains(new EdgeKey(candidate.start, candidate.end)))
                    {
                        continue;
                    }

                    next = candidate;
                    break;
                }

                if (!next.HasValue)
                {
                    break;
                }

                current = next.Value;
            }

            if (loop.Count > 2)
            {
                loops.Add(SimplifyLoop(loop));
            }
        }

        return loops;
    }

    private static void ToggleEdge(Dictionary<EdgeKey, Edge> edges, Edge edge)
    {
        EdgeKey key = new EdgeKey(edge.start, edge.end);
        if (edges.ContainsKey(key))
        {
            edges.Remove(key);
        }
        else
        {
            edges[key] = edge;
        }
    }

    private List<Vector2Int> SimplifyLoop(List<Vector2Int> loop)
    {
        if (loop == null || loop.Count <= 3)
        {
            return loop;
        }

        List<Vector2Int> simplified = new List<Vector2Int>();
        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int previous = loop[(i - 1 + loop.Count) % loop.Count];
            Vector2Int current = loop[i];
            Vector2Int next = loop[(i + 1) % loop.Count];

            Vector2Int incoming = current - previous;
            Vector2Int outgoing = next - current;
            if (incoming == outgoing)
            {
                continue;
            }

            simplified.Add(current);
        }

        return simplified;
    }

    private static Color ResolveOutlineColor(Color fillColor)
    {
        Color lineColor = fillColor;
        lineColor.a = Mathf.Clamp01(fillColor.a + 0.35f);
        return lineColor;
    }

    private void CreateOutlineLoop(Transform parent, List<Vector2Int> loop, Color lineColor, float y, int sortingOrder)
    {
        CreateOutlineLoopRenderer(parent, loop, lineColor, y, sortingOrder);
    }

    private LineRenderer CreateOutlineLoopRenderer(Transform parent, List<Vector2Int> loop, Color lineColor, float y, int sortingOrder)
    {
        if (loop == null || loop.Count < 2)
        {
            return null;
        }

        GameObject lineObject = new GameObject("Outline");
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = new Material(lineMaterialTemplate);
        line.sharedMaterial.color = lineColor;
        line.sortingOrder = sortingOrder;
        line.loop = true;
        line.useWorldSpace = false;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 18;
        line.numCapVertices = 18;
        line.widthMultiplier = cellSize * 0.12f;
        line.alignment = LineAlignment.View;
        line.positionCount = loop.Count;

        for (int i = 0; i < loop.Count; i++)
        {
            line.SetPosition(i, GridCornerToWorld(loop[i], y));
        }

        return line;
    }

    private Vector3 GridCornerToWorld(Vector2Int corner, float y)
    {
        return new Vector3((corner.x - 0.5f) * cellSize, y, (corner.y - 0.5f) * cellSize);
    }

    private HashSet<Vector2Int> CollectFootprintCells(Vector2Int centerCell, int footprintSize)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        AddFootprintCells(cells, centerCell, footprintSize);
        return cells;
    }

    private HashSet<Vector2Int> CollectReachableCells(BattleUnit unit, int range)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        if (unit == null)
        {
            return cells;
        }

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
                    AddFootprintCells(cells, cell, unit.footprintSize);
                }
            }
        }

        return cells;
    }

    private HashSet<Vector2Int> CollectCellsWithinRange(Vector2Int centerCell, int range)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        int clampedRange = Mathf.Max(0, range);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (ManhattanDistance(centerCell, cell) <= clampedRange)
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    private HashSet<Vector2Int> CollectCellsWithinRange(BattleUnit unit, int range)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        if (unit == null)
        {
            return cells;
        }

        int radius = unit.FootprintRadius;
        int clampedRange = Mathf.Max(0, range);
        for (int y = unit.currentCell.y - radius; y <= unit.currentCell.y + radius; y++)
        {
            for (int x = unit.currentCell.x - radius; x <= unit.currentCell.x + radius; x++)
            {
                Vector2Int footprintCell = new Vector2Int(x, y);
                if (!IsInside(footprintCell))
                {
                    continue;
                }

                HashSet<Vector2Int> fromCell = CollectCellsWithinRange(footprintCell, clampedRange);
                foreach (Vector2Int cell in fromCell)
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    private bool HasAnyFootprintCellInRange(BattleUnit unit, HashSet<Vector2Int> reachableCells)
    {
        if (unit == null || reachableCells == null || reachableCells.Count == 0)
        {
            return false;
        }

        int radius = unit.FootprintRadius;
        for (int y = unit.currentCell.y - radius; y <= unit.currentCell.y + radius; y++)
        {
            for (int x = unit.currentCell.x - radius; x <= unit.currentCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (reachableCells.Contains(cell))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsCellWithinRange(BattleUnit unit, Vector2Int cell, int range)
    {
        if (unit == null || !IsInside(cell))
        {
            return false;
        }

        HashSet<Vector2Int> reachableCells = CollectCellsWithinRange(unit, range);
        return reachableCells.Contains(cell);
    }

    public bool IsUnitWithinRange(BattleUnit source, BattleUnit target, int range)
    {
        if (source == null || target == null)
        {
            return false;
        }

        HashSet<Vector2Int> reachableCells = CollectCellsWithinRange(source, range);
        return HasAnyFootprintCellInRange(target, reachableCells);
    }

    private void AddFootprintCells(HashSet<Vector2Int> cells, Vector2Int centerCell, int footprintSize)
    {
        int radius = Mathf.Max(0, footprintSize / 2);
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInside(cell))
                {
                    cells.Add(cell);
                }
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

    private static void ClearChildren(Transform target)
    {
        if (target == null)
        {
            return;
        }

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            GameObject child = target.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Object.Destroy(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }
    }
}
