using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTurnSystem : MonoBehaviour
{
    private const string TimelineAnchorPath = "Canvas/上方栏位/回合时间轴";

    private readonly List<BattleUnit> units = new List<BattleUnit>();
    private readonly List<BattleUnit> currentRoundOrder = new List<BattleUnit>();
    private readonly Dictionary<BattleUnit, int> initiativeTieBreakers = new Dictionary<BattleUnit, int>();
    private readonly List<GameObject> timelineInstances = new List<GameObject>();

    [HideInInspector] public float timelineSpacing = 0f;
    [HideInInspector] public float activeTimelineExtraSpacing = 0f;
    [HideInInspector] public float activeTimelineScale = 1.1f;

    [HideInInspector] public Color playerTimelineColor = new Color(0.20f, 0.75f, 0.35f, 1f);
    [HideInInspector] public Color enemyTimelineColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    [HideInInspector] public Color activePlayerTimelineColor = Color.white;

    private BattleGrid grid;
    private Camera battleCamera;
    private BattleUnit activeUnit;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;
    private Transform timelineAnchor;
    private TurnTimelineButtonDatabase timelineDatabase;
    private int currentRoundIndex = -1;

    public void Initialize(BattleGrid battleGrid, Camera mainCamera, IEnumerable<BattleUnit> battleUnits)
    {
        grid = battleGrid;
        battleCamera = mainCamera;
        activeUnitIdText = FindActiveUnitIdText();
        timelineAnchor = FindTransformByPath(TimelineAnchorPath);
        timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
        units.Clear();
        currentRoundOrder.Clear();
        initiativeTieBreakers.Clear();
        timelineInstances.Clear();
        activeUnit = null;
        waitingForEnemyAction = false;
        currentRoundIndex = -1;

        foreach (BattleUnit unit in battleUnits)
        {
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        StartNewRound();
        BeginCurrentTurn();
    }

    public void NotifyUnitInitiativeChanged(BattleUnit changedUnit)
    {
        if (changedUnit == null || !units.Contains(changedUnit))
        {
            return;
        }

        ReorderRemainingRound();
        RefreshTimeline();
    }

    private void Update()
    {
        if (activeUnit == null || !activeUnit.IsAlive)
        {
            return;
        }

        if (activeUnit.isPlayerControlled)
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
        AdvanceTurn();
    }

    private void AdvanceTurn()
    {
        CleanupDeadUnits();
        if (currentRoundOrder.Count == 0)
        {
            activeUnit = null;
            RefreshActiveUnitUi();
            RefreshTimeline();
            return;
        }

        currentRoundIndex++;
        if (currentRoundIndex >= currentRoundOrder.Count)
        {
            StartNewRound();
        }

        BeginCurrentTurn();
    }

    private void StartNewRound()
    {
        CleanupDeadUnits();
        currentRoundOrder.Clear();

        if (units.Count == 0)
        {
            currentRoundIndex = -1;
            activeUnit = null;
            return;
        }

        List<BattleUnit> livingUnits = CollectLivingUnits(units);
        RandomizeTieBreakers(livingUnits);
        livingUnits.Sort(CompareInitiative);
        currentRoundOrder.AddRange(livingUnits);
        currentRoundIndex = 0;
    }

    private void BeginCurrentTurn()
    {
        CleanupDeadUnits();
        while (currentRoundIndex >= 0 && currentRoundIndex < currentRoundOrder.Count)
        {
            BattleUnit candidate = currentRoundOrder[currentRoundIndex];
            if (candidate != null && candidate.IsAlive)
            {
                activeUnit = candidate;
                RefreshHighlights();
                RefreshActiveUnitUi();
                RefreshTimeline();
                Debug.Log("Turn: " + activeUnit.unitName + " (AGI=" + activeUnit.Agility + ")");
                return;
            }

            currentRoundIndex++;
        }

        if (units.Count > 0)
        {
            StartNewRound();
            if (currentRoundOrder.Count > 0)
            {
                BeginCurrentTurn();
                return;
            }
        }

        activeUnit = null;
        RefreshActiveUnitUi();
        RefreshTimeline();
    }

    private void ReorderRemainingRound()
    {
        CleanupDeadUnits();
        if (currentRoundOrder.Count == 0 || currentRoundIndex < 0 || currentRoundIndex >= currentRoundOrder.Count)
        {
            return;
        }

        List<BattleUnit> prefix = new List<BattleUnit>();
        for (int i = 0; i <= currentRoundIndex && i < currentRoundOrder.Count; i++)
        {
            BattleUnit unit = currentRoundOrder[i];
            if (unit != null && unit.IsAlive)
            {
                prefix.Add(unit);
            }
        }

        List<BattleUnit> remaining = new List<BattleUnit>();
        for (int i = currentRoundIndex + 1; i < currentRoundOrder.Count; i++)
        {
            BattleUnit unit = currentRoundOrder[i];
            if (unit != null && unit.IsAlive)
            {
                remaining.Add(unit);
            }
        }

        RandomizeTieBreakers(remaining);
        remaining.Sort(CompareInitiative);

        currentRoundOrder.Clear();
        currentRoundOrder.AddRange(prefix);
        currentRoundOrder.AddRange(remaining);
        currentRoundIndex = Mathf.Min(prefix.Count - 1, currentRoundOrder.Count - 1);
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
        currentRoundOrder.RemoveAll(unit => unit == null || !unit.IsAlive);
        if (currentRoundOrder.Count == 0)
        {
            currentRoundIndex = -1;
            return;
        }

        currentRoundIndex = Mathf.Clamp(currentRoundIndex, 0, currentRoundOrder.Count - 1);
    }

    private static List<BattleUnit> CollectLivingUnits(List<BattleUnit> source)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        for (int i = 0; i < source.Count; i++)
        {
            BattleUnit unit = source[i];
            if (unit != null && unit.IsAlive)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private void RandomizeTieBreakers(List<BattleUnit> targetUnits)
    {
        initiativeTieBreakers.Clear();
        for (int i = 0; i < targetUnits.Count; i++)
        {
            initiativeTieBreakers[targetUnits[i]] = i;
        }

        for (int i = targetUnits.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            BattleUnit current = targetUnits[i];
            BattleUnit swapped = targetUnits[swapIndex];
            int currentTieBreaker = initiativeTieBreakers[current];
            initiativeTieBreakers[current] = initiativeTieBreakers[swapped];
            initiativeTieBreakers[swapped] = currentTieBreaker;
        }
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
        if (timelineAnchor == null || timelineDatabase == null || currentRoundIndex < 0)
        {
            return;
        }

        float cursorX = 0f;
        for (int i = currentRoundIndex; i < currentRoundOrder.Count; i++)
        {
            BattleUnit unit = currentRoundOrder[i];
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
            instance.name = string.IsNullOrWhiteSpace(unit.characterId) ? prefab.name : unit.characterId + "_时间轴";

            TurnTimelineTeamTint teamTint = instance.GetComponent<TurnTimelineTeamTint>();
            if (teamTint == null)
            {
                teamTint = instance.AddComponent<TurnTimelineTeamTint>();
            }

            bool isActive = i == currentRoundIndex;
            Color timelineColor = ResolveTimelineColor(unit, isActive);
            teamTint.Apply(timelineColor);

            RectTransform rect = instance.transform as RectTransform;
            if (rect != null)
            {
                float width = ResolveTimelineItemWidth(rect);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(cursorX, 0f);
                cursorX += width + timelineSpacing + (isActive ? activeTimelineExtraSpacing : 0f);
            }

            if (isActive)
            {
                instance.transform.localScale = Vector3.one * activeTimelineScale;
            }

            timelineInstances.Add(instance);
        }
    }

    private Color ResolveTimelineColor(BattleUnit unit, bool isActive)
    {
        if (unit == null)
        {
            return playerTimelineColor;
        }

        if (unit.team == BattleTeam.Player)
        {
            return isActive ? activePlayerTimelineColor : playerTimelineColor;
        }

        return enemyTimelineColor;
    }

    private static float ResolveTimelineItemWidth(RectTransform rect)
    {
        if (rect == null)
        {
            return 100f;
        }

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.preferredWidth > 0f)
        {
            return layoutElement.preferredWidth;
        }

        if (rect.sizeDelta.x > 0f)
        {
            return rect.sizeDelta.x;
        }

        return 100f;
    }

    private void ClearTimelineInstances()
    {
        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance != null)
            {
                Destroy(instance);
            }
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

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
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
