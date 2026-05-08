using System.Collections.Generic;
using UnityEngine;

public class BattleGrid : MonoBehaviour
{
    private const string Below3DNoDepthSpriteMaterialResourcePath = "渲染层级_低于3D不写深度Sprite材质";
    private const int GridOutlineSortingOrderBase = -9000;
    private const int GridOutlineSortingOrderRelativeLimit = 999;
    private const int FootprintOverlaySortingOrder = -10005;
    private const int DoorExitHalfWidth = 1;
    private const int DoorExitLength = 10;

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
    private HashSet<Vector2Int> validCells;
    private readonly HashSet<Vector2Int> doorExitCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, MapTemplateDatabase.ConnectionDirection> doorExitEntryDirections =
        new Dictionary<Vector2Int, MapTemplateDatabase.ConnectionDirection>();
    private readonly Dictionary<Vector2Int, Vector2Int> doorExitAutoDestinations = new Dictionary<Vector2Int, Vector2Int>();
    private readonly Dictionary<Vector2Int, DoorExitNavigation> doorExitNavigations =
        new Dictionary<Vector2Int, DoorExitNavigation>();
    private readonly Dictionary<Vector2Int, MapTemplateDatabase.ConnectionDirection> doorExitTransitionDirections =
        new Dictionary<Vector2Int, MapTemplateDatabase.ConnectionDirection>();
    private readonly Dictionary<MapTemplateDatabase.ConnectionDirection, Vector2Int> doorExitDefaultTargetCells =
        new Dictionary<MapTemplateDatabase.ConnectionDirection, Vector2Int>();

    private Material fillMaterialTemplate;
    private Material lineMaterialTemplate;
    private Material gridOutlineMaterialTemplate;
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
        gridOutlineMaterialTemplate = Resources.Load<Material>(Below3DNoDepthSpriteMaterialResourcePath);

        highlightLayerOrder = 0;
    }

    public struct DoorExitDefinition
    {
        public readonly MapTemplateDatabase.ConnectionDirection direction;
        public readonly Vector2Int coreCell;

        public DoorExitDefinition(MapTemplateDatabase.ConnectionDirection direction, Vector2Int coreCell)
        {
            this.direction = direction;
            this.coreCell = coreCell;
        }
    }

    private struct DoorExitNavigation
    {
        public readonly MapTemplateDatabase.ConnectionDirection direction;
        public readonly Vector2Int destination;

        public DoorExitNavigation(MapTemplateDatabase.ConnectionDirection direction, Vector2Int destination)
        {
            this.direction = direction;
            this.destination = destination;
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

    public void SetValidCells(IEnumerable<Vector2Int> cells)
    {
        if (cells == null)
        {
            validCells = null;
            return;
        }

        if (validCells == null)
        {
            validCells = new HashSet<Vector2Int>();
        }
        else
        {
            validCells.Clear();
        }

        foreach (Vector2Int cell in cells)
        {
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            {
                continue;
            }

            validCells.Add(cell);
        }

    }

    public bool IsInside(Vector2Int cell)
    {
        if (doorExitCells.Contains(cell))
        {
            return true;
        }

        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
        {
            return false;
        }

        return validCells == null || validCells.Contains(cell);
    }

    public void EnableCell(Vector2Int cell)
    {
        if (validCells == null || cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
        {
            return;
        }

        validCells.Add(cell);
    }

    public void DisableCell(Vector2Int cell)
    {
        if (validCells == null)
        {
            return;
        }

        validCells.Remove(cell);
    }

    public void ClearDoorExitCells()
    {
        doorExitCells.Clear();
        doorExitEntryDirections.Clear();
        doorExitAutoDestinations.Clear();
        doorExitNavigations.Clear();
        doorExitTransitionDirections.Clear();
        doorExitDefaultTargetCells.Clear();
    }

    public void SetDoorExitDefinitions(IEnumerable<DoorExitDefinition> definitions)
    {
        ClearDoorExitCells();
        if (definitions == null)
        {
            return;
        }

        foreach (DoorExitDefinition definition in definitions)
        {
            Vector2Int forward;
            Vector2Int side;
            if (!TryResolveDoorExitAxes(definition.direction, out forward, out side))
            {
                continue;
            }

            doorExitCells.Add(definition.coreCell);
            for (int depth = 1; depth <= DoorExitLength; depth++)
            {
                for (int offset = -DoorExitHalfWidth; offset <= DoorExitHalfWidth; offset++)
                {
                    Vector2Int cell = definition.coreCell + (forward * depth) + (side * offset);
                    Vector2Int deepestCell = definition.coreCell + (forward * DoorExitLength) + (side * offset);
                    doorExitCells.Add(cell);
                    doorExitNavigations[cell] = new DoorExitNavigation(definition.direction, deepestCell);

                    if (depth == 1)
                    {
                        doorExitEntryDirections[cell] = definition.direction;
                        doorExitAutoDestinations[cell] = deepestCell;
                    }

                    if (depth == DoorExitLength)
                    {
                        doorExitTransitionDirections[cell] = definition.direction;
                        if (offset == 0)
                        {
                            doorExitDefaultTargetCells[definition.direction] = cell;
                        }
                    }
                }
            }
        }
    }

    public bool TryGetDoorExitDefaultTargetCell(
        MapTemplateDatabase.ConnectionDirection direction,
        out Vector2Int cell)
    {
        return doorExitDefaultTargetCells.TryGetValue(direction, out cell);
    }

    public bool TryGetDoorExitEntry(
        Vector2Int cell,
        out MapTemplateDatabase.ConnectionDirection direction,
        out Vector2Int autoDestination)
    {
        autoDestination = default;
        if (!doorExitEntryDirections.TryGetValue(cell, out direction))
        {
            return false;
        }

        return doorExitAutoDestinations.TryGetValue(cell, out autoDestination);
    }

    public bool TryGetDoorExitNavigation(
        Vector2Int cell,
        out MapTemplateDatabase.ConnectionDirection direction,
        out Vector2Int destination)
    {
        DoorExitNavigation navigation;
        if (doorExitNavigations.TryGetValue(cell, out navigation))
        {
            direction = navigation.direction;
            destination = navigation.destination;
            return true;
        }

        direction = default;
        destination = default;
        return false;
    }

    public bool TryGetDoorExitTransition(
        Vector2Int cell,
        out MapTemplateDatabase.ConnectionDirection direction)
    {
        return doorExitTransitionDirections.TryGetValue(cell, out direction);
    }

    public bool IsWalkable(BattleUnit unit, Vector2Int centerCell)
    {
        return IsWalkableInternal(unit, centerCell, false, false);
    }

    public bool IsWalkableIgnoringAllies(BattleUnit unit, Vector2Int centerCell)
    {
        return IsWalkableInternal(unit, centerCell, true, false);
    }

    private bool IsWalkableInternal(BattleUnit unit, Vector2Int centerCell, bool ignoreAlliedOccupants, bool allowDestinationOnAllies)
    {
        if (doorExitCells.Contains(centerCell))
        {
            return true;
        }

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
                    bool isDestinationCell = cell == centerCell;
                    if (ignoreAlliedOccupants &&
                        unit != null &&
                        occupant.team == unit.team &&
                        occupant.isPlayerControlled == unit.isPlayerControlled)
                    {
                        if (allowDestinationOnAllies || !isDestinationCell)
                        {
                            continue;
                        }
                    }

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
        return MoveUnitAlongResolvedPath(unit, destination, path);
    }

    public float MoveUnitIgnoringAllies(BattleUnit unit, Vector2Int destination)
    {
        List<Vector2Int> path = FindPathIgnoringAllies(unit, destination);
        return MoveUnitAlongResolvedPath(unit, destination, path);
    }

    public float RedirectMovingUnitIgnoringAllies(BattleUnit unit, Vector2Int destination)
    {
        if (unit == null)
        {
            return 0f;
        }

        Vector2Int currentWorldCell = WorldToCell(unit.transform.position);
        if (!IsInside(currentWorldCell))
        {
            currentWorldCell = unit.currentCell;
        }

        unit.CancelMovement();
        SetOccupancy(unit, unit.currentCell, false);
        unit.currentCell = currentWorldCell;
        SetOccupancy(unit, currentWorldCell, true);

        List<Vector2Int> path = FindPathIgnoringAllies(unit, destination);
        return MoveUnitAlongResolvedPath(unit, destination, path);
    }

    private float MoveUnitAlongResolvedPath(BattleUnit unit, Vector2Int destination, List<Vector2Int> path)
    {
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
        return FindPathInternal(unit, destination, false);
    }

    public List<Vector2Int> FindPathIgnoringAllies(BattleUnit unit, Vector2Int destination)
    {
        return FindPathInternal(unit, destination, true);
    }

    private List<Vector2Int> FindPathInternal(BattleUnit unit, Vector2Int destination, bool ignoreAlliedOccupants)
    {
        if (unit == null || !IsInside(destination))
        {
            return null;
        }

        bool destinationWalkable = ignoreAlliedOccupants
            ? IsWalkableIgnoringAllies(unit, destination)
            : IsWalkable(unit, destination);
        if (!destinationWalkable)
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
                bool nextWalkable = ignoreAlliedOccupants
                    ? IsWalkableIgnoringAllies(unit, next)
                    : IsWalkable(unit, next);
                if (cameFrom.ContainsKey(next) || !IsInside(next) || !nextWalkable)
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

        ApplyHoveredFootprint(CollectFootprintCells(unit.currentCell, unit.footprintSize), color);
    }

    public void SetHoveredFootprint(HashSet<Vector2Int> cells, Color color)
    {
        if (cells == null || cells.Count == 0)
        {
            ClearHoveredFootprint();
            return;
        }

        ApplyHoveredFootprint(cells, color);
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

            if (!IsUnitWithinCircularRange(activeUnit, occupant, range))
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

    public void HighlightCircularRange(BattleUnit unit, int range)
    {
        if (unit == null)
        {
            return;
        }

        CreateCircleOverlay(
            GetWorldPosition(unit.currentCell),
            GetCastRadiusWorld(unit, range),
            reachableColor,
            reachableOutlineColor,
            "CircularRange");
    }

    public void HighlightCircleAt(Vector2Int centerCell, float radiusWorld, Color color)
    {
        if (!IsInside(centerCell))
        {
            return;
        }

        CreateCircleOverlay(
            GetWorldPosition(centerCell),
            Mathf.Max(0f, radiusWorld),
            color,
            ResolveOutlineColor(color),
            "Circle");
    }

    public void HighlightAxisRay(Vector3 origin, Vector3 direction, float lengthWorld, float widthWorld, Color color)
    {
        CreateClippedRayOverlay(
            origin,
            direction,
            Mathf.Max(0f, lengthWorld),
            Mathf.Max(cellSize, widthWorld),
            color,
            ResolveOutlineColor(color),
            "AxisRay");
    }

    public void HighlightAxisFan(Vector3 origin, Vector3 direction, float radiusWorld, float angleDegrees, Color color)
    {
        CreateSectorOverlay(
            origin,
            direction,
            Mathf.Max(0f, radiusWorld),
            Mathf.Clamp(angleDegrees, 1f, 360f),
            color,
            ResolveOutlineColor(color),
            "AxisFan");
    }

    public float GetUnitRadiusWorld(BattleUnit unit)
    {
        if (unit == null)
        {
            return cellSize * 0.5f;
        }

        return (unit.FootprintRadius + 0.5f) * cellSize;
    }

    public float GetAreaRadiusWorld(int footprintSize)
    {
        int radius = Mathf.Max(0, footprintSize / 2);
        return (radius + 0.5f) * cellSize;
    }

    public float GetCastRadiusWorld(BattleUnit unit, int range)
    {
        return Mathf.Max(0, range) * cellSize + GetUnitRadiusWorld(unit);
    }

    public bool IsUnitWithinCircularRange(BattleUnit source, BattleUnit target, int range)
    {
        if (source == null || target == null)
        {
            return false;
        }

        Vector3 sourcePosition = GetWorldPosition(source.currentCell);
        Vector3 targetPosition = GetWorldPosition(target.currentCell);
        float maxDistance = GetCastRadiusWorld(source, range) + GetUnitRadiusWorld(target);
        return Vector3.Distance(sourcePosition, targetPosition) <= maxDistance + 0.001f;
    }

    public bool IsCellWithinCircularRange(BattleUnit source, Vector2Int cell, int range)
    {
        if (source == null || !IsInside(cell))
        {
            return false;
        }

        Vector3 sourcePosition = GetWorldPosition(source.currentCell);
        Vector3 targetPosition = GetWorldPosition(cell);
        return Vector3.Distance(sourcePosition, targetPosition) <= GetCastRadiusWorld(source, range) + 0.001f;
    }

    public void HighlightFootprintAt(Vector2Int centerCell, int footprintSize, Color color)
    {
        CreateOverlay(CollectFootprintCells(centerCell, footprintSize), color, "FootprintAt");
    }

    public void HighlightCells(HashSet<Vector2Int> cells, Color color)
    {
        CreateOverlay(cells, color, "Cells");
    }

    public void HighlightPartialCells(HashSet<Vector2Int> cells, BattleUnit ignoredUnit, Color color)
    {
        if (cells == null || cells.Count == 0)
        {
            return;
        }

        HashSet<Vector2Int> visibleCells = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            if (!IsInside(cell))
            {
                continue;
            }

            BattleUnit occupant = GetUnitAt(cell);
            if (occupant != null && occupant != ignoredUnit)
            {
                continue;
            }

            visibleCells.Add(cell);
        }

        CreateOverlay(visibleCells, color, "PartialCells");
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
        meshRenderer.sortingOrder = FootprintOverlaySortingOrder;

        List<List<Vector2Int>> loops = BuildBoundaryLoops(cells);
        for (int i = 0; i < loops.Count; i++)
        {
            CreateOutlineLoop(
                overlay.transform,
                loops[i],
                outlineColor,
                overlayY + 0.001f + (highlightLayerOrder * 0.002f),
                FootprintOverlaySortingOrder,
                true);
        }

        highlightLayerOrder++;
    }

    private void CreateCircleOverlay(Vector3 center, float radiusWorld, Color fillColor, Color outlineColor, string name)
    {
        if (radiusWorld <= 0.001f)
        {
            return;
        }

        EnsureVisualRoots();

        GameObject overlay = new GameObject(name + "_" + highlightLayerOrder);
        overlay.transform.SetParent(highlightRoot, false);

        MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = overlay.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = BuildCircleFillMesh(center, radiusWorld, overlayY + (highlightLayerOrder * 0.002f));
        meshRenderer.sharedMaterial = new Material(fillMaterialTemplate);
        meshRenderer.sharedMaterial.color = fillColor;
        meshRenderer.sortingOrder = highlightLayerOrder * 10;

        CreateCircleOutline(
            overlay.transform,
            center,
            radiusWorld,
            outlineColor,
            overlayY + 0.001f + (highlightLayerOrder * 0.002f),
            (highlightLayerOrder * 10) + 1);

        highlightLayerOrder++;
    }

    private void CreateClippedRayOverlay(Vector3 origin, Vector3 direction, float lengthWorld, float widthWorld, Color fillColor, Color outlineColor, string name)
    {
        if (lengthWorld <= 0.001f || widthWorld <= 0.001f)
        {
            return;
        }

        EnsureVisualRoots();

        GameObject overlay = new GameObject(name + "_" + highlightLayerOrder);
        overlay.transform.SetParent(highlightRoot, false);

        MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = overlay.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = BuildClippedRayFillMesh(origin, direction, lengthWorld, widthWorld * 0.5f, overlayY + (highlightLayerOrder * 0.002f));
        meshRenderer.sharedMaterial = new Material(fillMaterialTemplate);
        meshRenderer.sharedMaterial.color = fillColor;
        meshRenderer.sortingOrder = highlightLayerOrder * 10;

        CreateClippedRayOutline(
            overlay.transform,
            origin,
            direction,
            lengthWorld,
            widthWorld * 0.5f,
            outlineColor,
            overlayY + 0.001f + (highlightLayerOrder * 0.002f),
            (highlightLayerOrder * 10) + 1);

        highlightLayerOrder++;
    }

    private void CreateSectorOverlay(Vector3 origin, Vector3 direction, float radiusWorld, float angleDegrees, Color fillColor, Color outlineColor, string name)
    {
        if (radiusWorld <= 0.001f || angleDegrees <= 0.001f)
        {
            return;
        }

        EnsureVisualRoots();

        GameObject overlay = new GameObject(name + "_" + highlightLayerOrder);
        overlay.transform.SetParent(highlightRoot, false);

        MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = overlay.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = BuildSectorFillMesh(origin, direction, radiusWorld, angleDegrees, overlayY + (highlightLayerOrder * 0.002f));
        meshRenderer.sharedMaterial = new Material(fillMaterialTemplate);
        meshRenderer.sharedMaterial.color = fillColor;
        meshRenderer.sortingOrder = highlightLayerOrder * 10;

        CreateSectorOutline(
            overlay.transform,
            origin,
            direction,
            radiusWorld,
            angleDegrees,
            outlineColor,
            overlayY + 0.001f + (highlightLayerOrder * 0.002f),
            (highlightLayerOrder * 10) + 1);

        highlightLayerOrder++;
    }

    private Mesh BuildClippedRayFillMesh(Vector3 origin, Vector3 direction, float lengthWorld, float halfWidthWorld, float y)
    {
        Mesh mesh = new Mesh
        {
            name = "BattleGridClippedRayOverlay"
        };

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            flatDirection = Vector3.right;
        }

        flatDirection.Normalize();
        Vector3 right = new Vector3(-flatDirection.z, 0f, flatDirection.x);
        float radius = Mathf.Max(0f, lengthWorld);
        float halfWidth = Mathf.Clamp(halfWidthWorld, 0f, radius);
        float forwardLimit = Mathf.Sqrt(Mathf.Max(0f, (radius * radius) - (halfWidth * halfWidth)));
        float seamOverlap = Mathf.Min(cellSize * 0.08f, Mathf.Max(0.01f, radius * 0.02f));
        float rectangleForward = Mathf.Min(radius, forwardLimit + seamOverlap);
        float arcStartForward = Mathf.Max(0f, forwardLimit - seamOverlap);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        Vector3 rearLeft = new Vector3(origin.x - right.x * halfWidth, y, origin.z - right.z * halfWidth);
        Vector3 frontLeft = new Vector3(origin.x + flatDirection.x * rectangleForward - right.x * halfWidth, y, origin.z + flatDirection.z * rectangleForward - right.z * halfWidth);
        Vector3 frontRight = new Vector3(origin.x + flatDirection.x * rectangleForward + right.x * halfWidth, y, origin.z + flatDirection.z * rectangleForward + right.z * halfWidth);
        Vector3 rearRight = new Vector3(origin.x + right.x * halfWidth, y, origin.z + right.z * halfWidth);

        vertices.Add(rearLeft);
        vertices.Add(frontLeft);
        vertices.Add(frontRight);
        vertices.Add(rearRight);
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);
        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);

        const int ArcSegments = 24;
        float startAngle = Mathf.Acos(Mathf.Clamp(arcStartForward / Mathf.Max(0.0001f, radius), -1f, 1f)) * Mathf.Rad2Deg;
        float arcStart = -startAngle;
        float arcEnd = startAngle;
        Vector3 arcFanCenter = new Vector3(
            origin.x + flatDirection.x * ((arcStartForward + radius) * 0.5f),
            y + 0.0002f,
            origin.z + flatDirection.z * ((arcStartForward + radius) * 0.5f));
        int fanCenterIndex = vertices.Count;
        vertices.Add(arcFanCenter);

        int arcStartIndex = vertices.Count;
        for (int i = 0; i <= ArcSegments; i++)
        {
            float t = ArcSegments == 0 ? 0f : (float)i / ArcSegments;
            float angle = Mathf.Lerp(arcStart, arcEnd, t);
            Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * flatDirection;
            vertices.Add(new Vector3(
                origin.x + radial.x * radius,
                y + 0.0002f,
                origin.z + radial.z * radius));
        }

        for (int i = 0; i < ArcSegments; i++)
        {
            triangles.Add(fanCenterIndex);
            triangles.Add(arcStartIndex + i);
            triangles.Add(arcStartIndex + i + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Mesh BuildSectorFillMesh(Vector3 origin, Vector3 direction, float radiusWorld, float angleDegrees, float y)
    {
        List<Vector3> perimeter = BuildSectorPerimeter(origin, direction, radiusWorld, angleDegrees, y);
        return BuildConvexPolygonMesh(perimeter, "BattleGridSectorOverlay");
    }

    private Mesh BuildConvexPolygonMesh(List<Vector3> perimeter, string meshName)
    {
        Mesh mesh = new Mesh
        {
            name = meshName
        };

        if (perimeter == null || perimeter.Count < 3)
        {
            return mesh;
        }

        List<Vector3> vertices = new List<Vector3>(perimeter);
        List<int> triangles = new List<int>((perimeter.Count - 2) * 3);
        for (int i = 1; i < perimeter.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ApplyHoveredFootprint(HashSet<Vector2Int> cells, Color fillColor)
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

    private Mesh BuildCircleFillMesh(Vector3 center, float radiusWorld, float y)
    {
        const int SegmentCount = 72;
        List<Vector3> vertices = new List<Vector3>(SegmentCount + 1);
        List<int> triangles = new List<int>(SegmentCount * 3);

        vertices.Add(new Vector3(center.x, y, center.z));
        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / SegmentCount;
            float x = center.x + Mathf.Cos(angle) * radiusWorld;
            float z = center.z + Mathf.Sin(angle) * radiusWorld;
            vertices.Add(new Vector3(x, y, z));
        }

        for (int i = 1; i <= SegmentCount; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        Mesh mesh = new Mesh
        {
            name = "BattleGridCircleOverlay"
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

    private void CreateOutlineLoop(Transform parent, List<Vector2Int> loop, Color lineColor, float y, int sortingOrder, bool useAbsoluteSortingOrder = false)
    {
        CreateOutlineLoopRenderer(parent, loop, lineColor, y, sortingOrder, useAbsoluteSortingOrder);
    }

    private void CreateCircleOutline(Transform parent, Vector3 center, float radiusWorld, Color lineColor, float y, int sortingOrder)
    {
        const int SegmentCount = 72;
        GameObject lineObject = new GameObject("CircleOutline");
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = CreateGridLineMaterial(lineColor);
        line.loop = true;
        line.useWorldSpace = false;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 12;
        line.numCapVertices = 12;
        line.widthMultiplier = cellSize * 0.12f;
        line.alignment = LineAlignment.View;
        line.sortingOrder = ResolveGridOutlineSortingOrder(sortingOrder);
        line.positionCount = SegmentCount;

        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / SegmentCount;
            float x = center.x + Mathf.Cos(angle) * radiusWorld;
            float z = center.z + Mathf.Sin(angle) * radiusWorld;
            line.SetPosition(i, new Vector3(x, y, z));
        }
    }

    private void CreateClippedRayOutline(Transform parent, Vector3 origin, Vector3 direction, float lengthWorld, float halfWidthWorld, Color lineColor, float y, int sortingOrder)
    {
        const int ArcSegments = 24;
        List<Vector3> perimeter = BuildClippedRayPerimeter(origin, direction, lengthWorld, halfWidthWorld, ArcSegments, y);
        CreatePolylineOutline(parent, perimeter, true, lineColor, y, sortingOrder, "ClippedRayOutline");
    }

    private void CreateSectorOutline(Transform parent, Vector3 origin, Vector3 direction, float radiusWorld, float angleDegrees, Color lineColor, float y, int sortingOrder)
    {
        List<Vector3> points = BuildSectorPerimeter(origin, direction, radiusWorld, angleDegrees, y);
        if (points.Count > 0)
        {
            points.Add(points[0]);
        }
        CreatePolylineOutline(parent, points, false, lineColor, y, sortingOrder, "SectorOutline");
    }

    private void CreatePolylineOutline(Transform parent, List<Vector3> points, bool loop, Color lineColor, float y, int sortingOrder, string name)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = CreateGridLineMaterial(lineColor);
        line.loop = loop;
        line.useWorldSpace = false;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 12;
        line.numCapVertices = 12;
        line.widthMultiplier = cellSize * 0.12f;
        line.alignment = LineAlignment.View;
        line.sortingOrder = ResolveGridOutlineSortingOrder(sortingOrder);
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, new Vector3(points[i].x, y, points[i].z));
        }
    }

    private static List<Vector3> BuildClippedRayPerimeter(Vector3 origin, Vector3 direction, float radiusWorld, float halfWidthWorld, int arcSegments, float y)
    {
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            flatDirection = Vector3.right;
        }

        flatDirection.Normalize();
        Vector3 right = new Vector3(-flatDirection.z, 0f, flatDirection.x);
        float clampedRadius = Mathf.Max(0f, radiusWorld);
        float clampedHalfWidth = Mathf.Clamp(halfWidthWorld, 0f, clampedRadius);
        float forwardLimit = Mathf.Sqrt(Mathf.Max(0f, (clampedRadius * clampedRadius) - (clampedHalfWidth * clampedHalfWidth)));
        List<Vector3> points = new List<Vector3>(arcSegments + 4);

        points.Add(new Vector3(origin.x - right.x * clampedHalfWidth, y, origin.z - right.z * clampedHalfWidth));
        points.Add(new Vector3(
            origin.x + flatDirection.x * forwardLimit - right.x * clampedHalfWidth,
            y,
            origin.z + flatDirection.z * forwardLimit - right.z * clampedHalfWidth));

        float arcStart = -Mathf.Asin(Mathf.Clamp(clampedHalfWidth / Mathf.Max(0.0001f, clampedRadius), -1f, 1f)) * Mathf.Rad2Deg;
        float arcEnd = -arcStart;
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = arcSegments == 0 ? 0f : (float)i / arcSegments;
            float angle = Mathf.Lerp(arcStart, arcEnd, t);
            Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * flatDirection;
            points.Add(new Vector3(
                origin.x + radial.x * clampedRadius,
                y,
                origin.z + radial.z * clampedRadius));
        }

        points.Add(new Vector3(
            origin.x + flatDirection.x * forwardLimit + right.x * clampedHalfWidth,
            y,
            origin.z + flatDirection.z * forwardLimit + right.z * clampedHalfWidth));
        points.Add(new Vector3(origin.x + right.x * clampedHalfWidth, y, origin.z + right.z * clampedHalfWidth));

        return points;
    }

    private static List<Vector3> BuildSectorPerimeter(Vector3 origin, Vector3 direction, float radiusWorld, float angleDegrees, float y)
    {
        int segmentCount = Mathf.Max(12, Mathf.CeilToInt(angleDegrees / 8f));
        List<Vector3> points = new List<Vector3>(segmentCount + 3);

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            flatDirection = Vector3.right;
        }

        flatDirection.Normalize();
        float halfAngle = angleDegrees * 0.5f;
        points.Add(new Vector3(origin.x, y, origin.z));

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = segmentCount == 0 ? 0f : (float)i / segmentCount;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 rotated = Quaternion.Euler(0f, angle, 0f) * flatDirection;
            points.Add(new Vector3(
                origin.x + rotated.x * radiusWorld,
                y,
                origin.z + rotated.z * radiusWorld));
        }

        return points;
    }

    private LineRenderer CreateOutlineLoopRenderer(
        Transform parent,
        List<Vector2Int> loop,
        Color lineColor,
        float y,
        int sortingOrder,
        bool useAbsoluteSortingOrder = false)
    {
        if (loop == null || loop.Count < 2)
        {
            return null;
        }

        GameObject lineObject = new GameObject("Outline");
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = CreateGridLineMaterial(lineColor);
        line.sortingOrder = useAbsoluteSortingOrder ? sortingOrder : ResolveGridOutlineSortingOrder(sortingOrder);
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

    private Material CreateGridLineMaterial(Color color)
    {
        Material template = gridOutlineMaterialTemplate != null
            ? gridOutlineMaterialTemplate
            : lineMaterialTemplate;
        Material material = new Material(template);
        material.color = color;
        return material;
    }

    private static int ResolveGridOutlineSortingOrder(int relativeSortingOrder)
    {
        return GridOutlineSortingOrderBase + Mathf.Clamp(relativeSortingOrder, 0, GridOutlineSortingOrderRelativeLimit);
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
                if (cell == origin || !IsInside(cell))
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

        foreach (Vector2Int cell in doorExitCells)
        {
            if (cell == origin)
            {
                continue;
            }

            List<Vector2Int> path = FindPath(unit, cell);
            if (path != null && path.Count > 1 && path.Count - 1 <= range)
            {
                cells.Add(cell);
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
                if (IsInside(cell) && ManhattanDistance(centerCell, cell) <= clampedRange)
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
        return IsCellWithinCircularRange(unit, cell, range);
    }

    public bool IsUnitWithinRange(BattleUnit source, BattleUnit target, int range)
    {
        return IsUnitWithinCircularRange(source, target, range);
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
        if (occupied && doorExitCells.Contains(centerCell))
        {
            return;
        }

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
                    if (doorExitCells.Contains(cell))
                    {
                        continue;
                    }

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

    private static bool TryResolveDoorExitAxes(
        MapTemplateDatabase.ConnectionDirection direction,
        out Vector2Int forward,
        out Vector2Int side)
    {
        switch (direction)
        {
            case MapTemplateDatabase.ConnectionDirection.East:
                forward = Vector2Int.right;
                side = Vector2Int.up;
                return true;
            case MapTemplateDatabase.ConnectionDirection.South:
                forward = Vector2Int.down;
                side = Vector2Int.right;
                return true;
            case MapTemplateDatabase.ConnectionDirection.West:
                forward = Vector2Int.left;
                side = Vector2Int.up;
                return true;
            case MapTemplateDatabase.ConnectionDirection.North:
                forward = Vector2Int.up;
                side = Vector2Int.right;
                return true;
            default:
                forward = default;
                side = default;
                return false;
        }
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
