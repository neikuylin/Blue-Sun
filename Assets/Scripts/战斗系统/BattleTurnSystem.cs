using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleTurnSystem : MonoBehaviour
{
    private const string TimelineAnchorPath = "Canvas/上方栏位/回合时间轴";

    private readonly List<BattleUnit> units = new List<BattleUnit>();
    private readonly Dictionary<BattleUnit, int> initiativeTieBreakers = new Dictionary<BattleUnit, int>();
    private readonly List<GameObject> timelineInstances = new List<GameObject>();

    private BattleGrid grid;
    private Camera battleCamera;
    private BattleUnit activeUnit;
    private int activeIndex = -1;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;
    private Transform timelineAnchor;
    private TurnTimelineButtonDatabase timelineDatabase;

    public void Initialize(BattleGrid battleGrid, Camera mainCamera, IEnumerable<BattleUnit> battleUnits)
    {
        grid = battleGrid;
        battleCamera = mainCamera;
        activeUnitIdText = FindActiveUnitIdText();
        timelineAnchor = FindTransformByPath(TimelineAnchorPath);
        timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
        units.Clear();
        initiativeTieBreakers.Clear();

        int index = 0;
        foreach (BattleUnit unit in battleUnits)
        {
            if (unit == null)
            {
                continue;
            }

            units.Add(unit);
            initiativeTieBreakers[unit] = index;
            index++;
        }

        RandomizeTieBreakers();
        SortUnitsByInitiative();
        RefreshTimeline();
        BeginNextTurn();
    }

    public void NotifyUnitInitiativeChanged(BattleUnit changedUnit)
    {
        if (changedUnit == null || !units.Contains(changedUnit))
        {
            return;
        }

        RebuildTurnOrderPreserveCurrent();
        RefreshTimeline();
    }

    private void Update()
    {
        if (activeUnit == null || !activeUnit.IsAlive)
        {
            return;
        }

        if (activeUnit.team == BattleTeam.Player)
        {
            HandlePlayerInput();
            return;
        }

        if (!waitingForEnemyAction)
        {
            StartCoroutine(RunEnemyTurn());
        }
    }

    private void HandlePlayerInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;

        if (!clickPlane.Raycast(ray, out enter))
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int clickedCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(clickedCell))
        {
            return;
        }

        BattleUnit target = grid.GetUnitAt(clickedCell);
        if (target != null && target.team != activeUnit.team)
        {
            TryAttack(activeUnit, target);
            return;
        }

        TryMove(activeUnit, clickedCell);
    }

    private IEnumerator RunEnemyTurn()
    {
        waitingForEnemyAction = true;
        yield return new WaitForSeconds(0.5f);

        BattleUnit target = FindClosestLivingOpponent(activeUnit);
        if (target == null)
        {
            waitingForEnemyAction = false;
            yield break;
        }

        if (grid.ManhattanDistance(activeUnit.currentCell, target.currentCell) <= activeUnit.attackRange)
        {
            ExecuteAttack(activeUnit, target);
            yield return new WaitForSeconds(0.35f);
            EndTurn();
            yield break;
        }

        Vector2Int destination = FindBestStepToward(activeUnit, target);
        if (destination != activeUnit.currentCell)
        {
            grid.MoveUnit(activeUnit, destination);
            activeUnit.FaceToward(target.transform.position);
        }

        yield return new WaitForSeconds(0.35f);

        if (target.IsAlive && grid.ManhattanDistance(activeUnit.currentCell, target.currentCell) <= activeUnit.attackRange)
        {
            ExecuteAttack(activeUnit, target);
            yield return new WaitForSeconds(0.35f);
        }

        EndTurn();
    }

    private void TryMove(BattleUnit unit, Vector2Int destination)
    {
        if (grid.ManhattanDistance(unit.currentCell, destination) > unit.moveRange)
        {
            return;
        }

        if (!grid.IsWalkable(unit, destination))
        {
            return;
        }

        grid.MoveUnit(unit, destination);
        EndTurn();
    }

    private void TryAttack(BattleUnit attacker, BattleUnit defender)
    {
        if (grid.ManhattanDistance(attacker.currentCell, defender.currentCell) > attacker.attackRange)
        {
            return;
        }

        ExecuteAttack(attacker, defender);
        EndTurn();
    }

    private void ExecuteAttack(BattleUnit attacker, BattleUnit defender)
    {
        attacker.FaceToward(defender.transform.position);
        defender.ApplyDamage(attacker.attackDamage);
        Debug.Log(attacker.unitName + " attacks " + defender.unitName + " for " + attacker.attackDamage + " damage.");

        if (!defender.IsAlive)
        {
            grid.RemoveUnit(defender);
            Debug.Log(defender.unitName + " is defeated.");
        }
    }

    private void EndTurn()
    {
        waitingForEnemyAction = false;
        RebuildTurnOrderPreserveCurrent();
        RefreshTimeline();
        BeginNextTurn();
    }

    private void BeginNextTurn()
    {
        CleanupDeadUnits();
        if (units.Count == 0)
        {
            activeUnit = null;
            RefreshActiveUnitUi();
            RefreshTimeline();
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            activeIndex = (activeIndex + 1) % units.Count;
            BattleUnit candidate = units[activeIndex];
            if (candidate != null && candidate.IsAlive)
            {
                activeUnit = candidate;
                RefreshHighlights();
                RefreshActiveUnitUi();
                RefreshTimeline();
                Debug.Log("Turn: " + activeUnit.unitName + " (AGI=" + activeUnit.Agility + ")");
                return;
            }
        }

        activeUnit = null;
        RefreshActiveUnitUi();
        RefreshTimeline();
    }

    private void RefreshHighlights()
    {
        grid.ResetHighlights();
        if (activeUnit == null)
        {
            return;
        }

        grid.HighlightReachable(activeUnit);
        grid.HighlightAttackTargets(activeUnit);
        grid.HighlightFootprint(activeUnit, new Color(1.00f, 0.90f, 0.20f, 0.60f));
    }

    private void CleanupDeadUnits()
    {
        units.RemoveAll(unit => unit == null || !unit.IsAlive);
    }

    private void RandomizeTieBreakers()
    {
        for (int i = units.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            BattleUnit current = units[i];
            BattleUnit swapped = units[swapIndex];
            int currentTieBreaker = initiativeTieBreakers[current];
            initiativeTieBreakers[current] = initiativeTieBreakers[swapped];
            initiativeTieBreakers[swapped] = currentTieBreaker;
        }
    }

    private void RebuildTurnOrderPreserveCurrent()
    {
        CleanupDeadUnits();

        if (units.Count == 0)
        {
            activeIndex = -1;
            return;
        }

        BattleUnit current = activeUnit;
        SortUnitsByInitiative();

        if (current == null)
        {
            activeIndex = -1;
            return;
        }

        int index = units.IndexOf(current);
        activeIndex = index >= 0 ? index : -1;
    }

    private void SortUnitsByInitiative()
    {
        units.Sort(CompareInitiative);
    }

    private int CompareInitiative(BattleUnit left, BattleUnit right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int agilityCompare = right.Agility.CompareTo(left.Agility);
        if (agilityCompare != 0)
        {
            return agilityCompare;
        }

        int leftIndex;
        int rightIndex;
        if (!initiativeTieBreakers.TryGetValue(left, out leftIndex))
        {
            leftIndex = int.MaxValue;
        }

        if (!initiativeTieBreakers.TryGetValue(right, out rightIndex))
        {
            rightIndex = int.MaxValue;
        }

        return leftIndex.CompareTo(rightIndex);
    }

    private void RefreshActiveUnitUi()
    {
        if (activeUnitIdText == null)
        {
            activeUnitIdText = FindActiveUnitIdText();
        }

        if (activeUnitIdText == null)
        {
            return;
        }

        if (activeUnit == null)
        {
            activeUnitIdText.text = string.Empty;
            return;
        }

        activeUnitIdText.text = string.IsNullOrWhiteSpace(activeUnit.characterId) ? activeUnit.unitName : activeUnit.characterId;
    }

    private void RefreshTimeline()
    {
        if (timelineAnchor == null)
        {
            timelineAnchor = FindTransformByPath(TimelineAnchorPath);
        }

        if (timelineDatabase == null)
        {
            timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
        }

        ClearTimelineInstances();
        if (timelineAnchor == null || timelineDatabase == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            GameObject prefab = timelineDatabase.FindButtonPrefab(unit.characterId);
            if (prefab == null)
            {
                continue;
            }

            GameObject instance = Instantiate(prefab, timelineAnchor, false);
            instance.name = string.IsNullOrWhiteSpace(unit.characterId)
                ? prefab.name
                : unit.characterId + "_时间轴";

            if (unit == activeUnit)
            {
                instance.transform.localScale = Vector3.one * 1.1f;
            }

            timelineInstances.Add(instance);
        }
    }

    private void ClearTimelineInstances()
    {
        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance == null)
            {
                continue;
            }

            Destroy(instance);
        }

        timelineInstances.Clear();
    }

    private static TMP_Text FindActiveUnitIdText()
    {
        TMP_Text[] texts = Object.FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.name == "ID")
            {
                return text;
            }
        }

        return null;
    }

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, segments[0], System.StringComparison.Ordinal))
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, System.StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private BattleUnit FindClosestLivingOpponent(BattleUnit source)
    {
        BattleUnit best = null;
        int bestDistance = int.MaxValue;

        foreach (BattleUnit unit in units)
        {
            if (unit == null || !unit.IsAlive || unit.team == source.team)
            {
                continue;
            }

            int distance = grid.ManhattanDistance(source.currentCell, unit.currentCell);
            if (distance < bestDistance)
            {
                best = unit;
                bestDistance = distance;
            }
        }

        return best;
    }

    private Vector2Int FindBestStepToward(BattleUnit mover, BattleUnit target)
    {
        Vector2Int bestCell = mover.currentCell;
        int bestDistance = grid.ManhattanDistance(mover.currentCell, target.currentCell);

        for (int dx = -mover.moveRange; dx <= mover.moveRange; dx++)
        {
            for (int dy = -mover.moveRange; dy <= mover.moveRange; dy++)
            {
                Vector2Int candidate = mover.currentCell + new Vector2Int(dx, dy);
                if (grid.ManhattanDistance(mover.currentCell, candidate) > mover.moveRange)
                {
                    continue;
                }

                if (!grid.IsWalkable(mover, candidate))
                {
                    continue;
                }

                int candidateDistance = grid.ManhattanDistance(candidate, target.currentCell);
                if (candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    bestCell = candidate;
                }
            }
        }

        return bestCell;
    }
}
