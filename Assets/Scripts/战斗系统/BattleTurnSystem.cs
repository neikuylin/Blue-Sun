using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTurnSystem : MonoBehaviour
{
    private const string TimelineAnchorPath = "Canvas/\u4E0A\u65B9\u680F\u4F4D/\u56DE\u5408\u65F6\u95F4\u8F74";

    private const string EndTurnButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u7ED3\u675F\u56DE\u5408\u6309\u94AE";
    private const string MoveButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u79FB\u52A8\u6309\u94AE";
    private readonly List<BattleUnit> units = new List<BattleUnit>();
    private readonly List<BattleUnit> currentRoundOrder = new List<BattleUnit>();
    private readonly List<List<BattleUnit>> upcomingRoundOrders = new List<List<BattleUnit>>();
    private readonly Dictionary<BattleUnit, int> initiativeTieBreakers = new Dictionary<BattleUnit, int>();
    private readonly List<GameObject> timelineInstances = new List<GameObject>();
    private readonly Dictionary<GameObject, BattleUnit> timelineInstanceUnits = new Dictionary<GameObject, BattleUnit>();
    private readonly Dictionary<GameObject, TimelineSlotKey> timelineInstanceKeys = new Dictionary<GameObject, TimelineSlotKey>();
    private readonly List<TimelineSlot> lastTimelineSlots = new List<TimelineSlot>();

    [HideInInspector] public float timelineSpacing = 0f;
    [HideInInspector] public float activeTimelineExtraSpacing = 0f;
    [HideInInspector] public float activeTimelineScale = 1.1f;
    [HideInInspector] public int previewRoundCount = 3;
    [HideInInspector] public float roundSeparatorSpacing = 32f;
    [HideInInspector] public Sprite roundSeparatorSprite;
    [HideInInspector] public Vector2 roundSeparatorSize = new Vector2(32f, 125f);
    [HideInInspector] public float timelineShiftDuration = 0.18f;

    [HideInInspector] public Color playerTimelineColor = new Color(0.20f, 0.75f, 0.35f, 1f);
    [HideInInspector] public Color enemyTimelineColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    [HideInInspector] public Color activePlayerTimelineColor = Color.white;

    private readonly Color movementPreviewValidColor = new Color(1.00f, 0.90f, 0.20f, 0.70f);
    private readonly Color movementPreviewInvalidColor = new Color(1.00f, 0.25f, 0.20f, 0.60f);
    private readonly Color movementOccupiedColor = new Color(0.22f, 0.22f, 0.22f, 0.65f);

    private BattleGrid grid;
    private Camera battleCamera;
    private BattleUnit activeUnit;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;
    private Transform timelineAnchor;
    private Button endTurnButton;
    private Button moveButton;
    private BattleSkillDatabase skillDatabase;
    private TurnTimelineButtonDatabase timelineDatabase;
    private Coroutine timelineAnimationRoutine;
    private BattleUnit timelineLeadUnit;
    private int absoluteRoundIndex = -1;
    private int currentRoundIndex = -1;
    private bool movementModeActive;
    private bool hasMovementHoverPreview;
    private Vector2Int movementHoverCell;
    private bool movementHoverValid;
    private bool movementHoverHasAnyVisibleCells;

    public void Initialize(BattleGrid battleGrid, Camera mainCamera, IEnumerable<BattleUnit> battleUnits)
    {
        grid = battleGrid;
        battleCamera = mainCamera;
        activeUnitIdText = FindActiveUnitIdText();
        timelineAnchor = FindTransformByPath(TimelineAnchorPath);
        EnsureTimelineMask();
        BindEndTurnButton();
        BindMoveButton();
        skillDatabase = BattleSkillDatabase.LoadDefault();
        timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
        units.Clear();
        currentRoundOrder.Clear();
        upcomingRoundOrders.Clear();
        initiativeTieBreakers.Clear();
        timelineInstances.Clear();
        timelineInstanceUnits.Clear();
        activeUnit = null;
        waitingForEnemyAction = false;
        currentRoundIndex = -1;
        absoluteRoundIndex = -1;
        movementModeActive = false;
        timelineLeadUnit = null;
        lastTimelineSlots.Clear();

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

    private void OnDestroy()
    {
        UnbindEndTurnButton();
        UnbindMoveButton();
    }

    public void NotifyUnitInitiativeChanged(BattleUnit changedUnit)
    {
        if (changedUnit == null || !units.Contains(changedUnit))
        {
            return;
        }

        ReorderRemainingRound();
        InvalidateFutureRounds();
        EnsureUpcomingRounds(Mathf.Max(0, previewRoundCount - 1));
        RefreshTimeline();
    }

    private void Update()
    {
        if (activeUnit == null || !activeUnit.IsAlive)
        {
            return;
        }

        if (activeUnit.IsMoving)
        {
            return;
        }

        if (activeUnit.isPlayerControlled)
        {
            UpdateMovementHoverPreview();
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

        if (movementModeActive)
        {
            TryMove(activeUnit, clickedCell);
        }
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
            float moveDuration = grid.MoveUnit(activeUnit, destination);
            activeUnit.FaceToward(target.transform.position);
            if (moveDuration > 0f)
            {
                yield return new WaitForSeconds(moveDuration);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (target.IsAlive && grid.ManhattanDistance(activeUnit.currentCell, target.currentCell) <= activeUnit.attackRange)
        {
            ExecuteAttack(activeUnit, target);
            yield return new WaitForSeconds(0.35f);
        }

        EndTurn();
    }

    private void TryMove(BattleUnit unit, Vector2Int destination)
    {
        int moveActionPointCost = GetMoveSkillActionPointCost();
        if (unit == null || !unit.CanSpendActionPoints(moveActionPointCost))
        {
            return;
        }

        if (unit.IsMoving)
        {
            return;
        }

        if (destination == unit.currentCell)
        {
            return;
        }

        List<Vector2Int> path = grid.FindPath(unit, destination);
        if (path == null || path.Count <= 1)
        {
            return;
        }

        if (path.Count - 1 > GetMoveSkillRange(unit))
        {
            return;
        }

        grid.MoveUnit(unit, destination);
        unit.SpendActionPoints(moveActionPointCost);
        movementModeActive = false;
        hasMovementHoverPreview = false;
        movementHoverHasAnyVisibleCells = false;
        RefreshHighlights();
    }

    private void TryAttack(BattleUnit attacker, BattleUnit defender)
    {
        if (attacker == null)
        {
            return;
        }

        if (grid.ManhattanDistance(attacker.currentCell, defender.currentCell) > attacker.attackRange)
        {
            return;
        }

        ExecuteAttack(attacker, defender);
        RefreshHighlights();
        RefreshTimeline();
    }

    private void ExecuteAttack(BattleUnit attacker, BattleUnit defender)
    {
        attacker.FaceToward(defender.transform.position);
        defender.ApplyDamage(attacker.attackDamage);
        Debug.Log(attacker.unitName + " attacks " + defender.unitName + " for " + attacker.attackDamage + " damage.");

        if (!defender.IsAlive)
        {
            grid.RemoveUnit(defender);
            InvalidateFutureRounds();
            Debug.Log(defender.unitName + " is defeated.");
        }
    }

    private void EndTurn()
    {
        waitingForEnemyAction = false;
        movementModeActive = false;
        hasMovementHoverPreview = false;
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

        EnsureUpcomingRounds(1);
        if (upcomingRoundOrders.Count == 0)
        {
            currentRoundIndex = -1;
            activeUnit = null;
            return;
        }

        absoluteRoundIndex++;
        currentRoundOrder.AddRange(upcomingRoundOrders[0]);
        upcomingRoundOrders.RemoveAt(0);
        CacheCurrentRoundTieBreakers();
        currentRoundIndex = 0;
        EnsureUpcomingRounds(Mathf.Max(0, previewRoundCount - 1));
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
                activeUnit.BeginTurn();
                movementModeActive = false;
                hasMovementHoverPreview = false;
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
        CacheCurrentRoundTieBreakers();
        currentRoundIndex = Mathf.Min(prefix.Count - 1, currentRoundOrder.Count - 1);
    }

    private void RefreshHighlights()
    {
        grid.ResetHighlights();
        if (activeUnit == null)
        {
            return;
        }

        if (activeUnit.isPlayerControlled && movementModeActive)
        {
            int moveRange = GetMoveSkillRange(activeUnit);
            grid.HighlightReachable(activeUnit, moveRange);
            grid.HighlightOccupiedCellsWithinRange(activeUnit, moveRange, movementOccupiedColor);
        }

        grid.HighlightAttackTargets(activeUnit);
        grid.HighlightFootprint(activeUnit, new Color(1.00f, 0.90f, 0.20f, 0.60f));
        ApplyMovementHoverPreview();
    }

    private void CleanupDeadUnits()
    {
        units.RemoveAll(unit => unit == null || !unit.IsAlive);
        currentRoundOrder.RemoveAll(unit => unit == null || !unit.IsAlive);

        for (int i = upcomingRoundOrders.Count - 1; i >= 0; i--)
        {
            List<BattleUnit> round = upcomingRoundOrders[i];
            if (round == null)
            {
                upcomingRoundOrders.RemoveAt(i);
                continue;
            }

            round.RemoveAll(unit => unit == null || !unit.IsAlive);
            if (round.Count == 0)
            {
                upcomingRoundOrders.RemoveAt(i);
            }
        }

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

    private void InvalidateFutureRounds()
    {
        upcomingRoundOrders.Clear();
    }

    private void EnsureUpcomingRounds(int minimumCount)
    {
        CleanupDeadUnits();
        while (upcomingRoundOrders.Count < minimumCount)
        {
            List<BattleUnit> roundOrder = CreateRoundOrderSnapshot();
            if (roundOrder.Count == 0)
            {
                break;
            }

            upcomingRoundOrders.Add(roundOrder);
        }
    }

    private List<BattleUnit> CreateRoundOrderSnapshot()
    {
        List<BattleUnit> livingUnits = CollectLivingUnits(units);
        Dictionary<BattleUnit, int> tieBreakers = CreateTieBreakers(livingUnits);
        livingUnits.Sort((left, right) => CompareInitiative(left, right, tieBreakers));
        return livingUnits;
    }

    private void CacheCurrentRoundTieBreakers()
    {
        initiativeTieBreakers.Clear();
        for (int i = 0; i < currentRoundOrder.Count; i++)
        {
            BattleUnit unit = currentRoundOrder[i];
            if (unit != null && !initiativeTieBreakers.ContainsKey(unit))
            {
                initiativeTieBreakers[unit] = i;
            }
        }
    }

    private void RandomizeTieBreakers(List<BattleUnit> targetUnits)
    {
        initiativeTieBreakers.Clear();
        Dictionary<BattleUnit, int> generated = CreateTieBreakers(targetUnits);
        foreach (KeyValuePair<BattleUnit, int> pair in generated)
        {
            initiativeTieBreakers[pair.Key] = pair.Value;
        }
    }

    private static Dictionary<BattleUnit, int> CreateTieBreakers(List<BattleUnit> targetUnits)
    {
        Dictionary<BattleUnit, int> tieBreakers = new Dictionary<BattleUnit, int>();
        for (int i = 0; i < targetUnits.Count; i++)
        {
            tieBreakers[targetUnits[i]] = i;
        }

        for (int i = targetUnits.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            BattleUnit current = targetUnits[i];
            BattleUnit swapped = targetUnits[swapIndex];
            int currentTieBreaker = tieBreakers[current];
            tieBreakers[current] = tieBreakers[swapped];
            tieBreakers[swapped] = currentTieBreaker;
        }

        return tieBreakers;
    }

    private int CompareInitiative(BattleUnit left, BattleUnit right)
    {
        return CompareInitiative(left, right, initiativeTieBreakers);
    }

    private static int CompareInitiative(BattleUnit left, BattleUnit right, Dictionary<BattleUnit, int> tieBreakerLookup)
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
        if (tieBreakerLookup == null || !tieBreakerLookup.TryGetValue(left, out leftIndex))
        {
            leftIndex = int.MaxValue;
        }

        if (tieBreakerLookup == null || !tieBreakerLookup.TryGetValue(right, out rightIndex))
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
            EnsureTimelineMask();
        }

        if (timelineDatabase == null)
        {
            timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
        }

        List<List<BattleUnit>> timelineRounds = BuildTimelineRounds();
        BattleUnit newLeadUnit = FindTimelineLeadUnit(timelineRounds);
        if (timelineAnchor == null || timelineDatabase == null || currentRoundIndex < 0)
        {
            ClearTimelineInstances();
            timelineLeadUnit = null;
            return;
        }

        if (timelineInstances.Count > 0 &&
            Application.isPlaying &&
            TimelineNeedsAnimation(timelineRounds, newLeadUnit))
        {
            if (timelineAnimationRoutine != null)
            {
                StopCoroutine(timelineAnimationRoutine);
            }

            timelineAnimationRoutine = StartCoroutine(AnimateTimelineReorderAndRebuild(timelineRounds, newLeadUnit));
            return;
        }

        BuildTimelineImmediate(timelineRounds);
        timelineLeadUnit = newLeadUnit;
        return;
    }

    private void BuildTimelineImmediate(List<List<BattleUnit>> timelineRounds)
    {
        ClearTimelineInstances();
        List<TimelineSlot> slots = BuildTimelineSlots(timelineRounds);
        lastTimelineSlots.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            TimelineSlot slot = slots[i];
            if (slot.isSeparator)
            {
                CreateRoundSeparator(slot);
                lastTimelineSlots.Add(slot);
                continue;
            }

            CreateTimelineUnitInstance(slot);
            lastTimelineSlots.Add(slot);
        }
    }

    private GameObject CreateTimelineUnitInstance(TimelineSlot slot)
    {
        BattleUnit unit = slot.unit;
        GameObject prefab = timelineDatabase.FindButtonPrefab(unit.characterId);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, timelineAnchor, false);
        instance.name = string.IsNullOrWhiteSpace(unit.characterId) ? prefab.name : unit.characterId + "_时间轴";

        TurnTimelineTeamTint teamTint = instance.GetComponent<TurnTimelineTeamTint>();
        if (teamTint == null)
        {
            teamTint = instance.AddComponent<TurnTimelineTeamTint>();
        }

        teamTint.Apply(ResolveTimelineColor(unit, slot.isActive));

        RectTransform rect = instance.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(slot.x, 0f);
        }

        instance.transform.localScale = slot.isActive ? Vector3.one * activeTimelineScale : Vector3.one;
        timelineInstances.Add(instance);
        timelineInstanceUnits[instance] = unit;
        timelineInstanceKeys[instance] = slot.key;
        return instance;
    }

    private IEnumerator AnimateTimelineReorderAndRebuild(List<List<BattleUnit>> timelineRounds, BattleUnit newLeadUnit)
    {
        List<TimelineSlot> desiredSlots = BuildTimelineSlots(timelineRounds);
        List<GameObject> currentInstances = GetCurrentTimelineInstances();
        if (currentInstances.Count != lastTimelineSlots.Count)
        {
            BuildTimelineImmediate(timelineRounds);
            timelineLeadUnit = newLeadUnit;
            timelineAnimationRoutine = null;
            yield break;
        }

        int[] matchedDesiredIndices = MatchTimelineSlotsByKey(lastTimelineSlots, desiredSlots);
        int earliestMatchedCurrentIndex = int.MaxValue;
        for (int i = 0; i < matchedDesiredIndices.Length; i++)
        {
            if (matchedDesiredIndices[i] >= 0)
            {
                earliestMatchedCurrentIndex = Mathf.Min(earliestMatchedCurrentIndex, i);
            }
        }

        List<RectTransform> animatedRects = new List<RectTransform>();
        List<Vector2> startPositions = new List<Vector2>();
        List<Vector2> targetPositions = new List<Vector2>();
        List<Vector3> startScales = new List<Vector3>();
        List<Vector3> targetScales = new List<Vector3>();

        for (int i = 0; i < currentInstances.Count; i++)
        {
            GameObject instance = currentInstances[i];
            RectTransform rect = instance.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }

            TimelineSlot currentSlot = lastTimelineSlots[i];
            int matchedIndex = matchedDesiredIndices[i];
            bool stillInQueue = matchedIndex >= 0 && matchedIndex < desiredSlots.Count;
            animatedRects.Add(rect);
            startPositions.Add(rect.anchoredPosition);
            if (stillInQueue)
            {
                targetPositions.Add(new Vector2(desiredSlots[matchedIndex].x, 0f));
            }
            else
            {
                if (i < earliestMatchedCurrentIndex)
                {
                    float exitX = -(Mathf.Max(rect.rect.width, rect.sizeDelta.x, 100f) + 40f);
                    targetPositions.Add(new Vector2(exitX, rect.anchoredPosition.y));
                }
                else
                {
                    float exitY = -(Mathf.Max(rect.rect.height, rect.sizeDelta.y, 100f) + 40f);
                    targetPositions.Add(new Vector2(rect.anchoredPosition.x, exitY));
                }
            }

            startScales.Add(rect.localScale);
            if (currentSlot.isSeparator)
            {
                targetScales.Add(Vector3.one);
            }
            else
            {
                targetScales.Add(stillInQueue && desiredSlots[matchedIndex].isActive ? Vector3.one * activeTimelineScale : Vector3.one);
            }
        }

        float duration = Mathf.Max(0.01f, timelineShiftDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < animatedRects.Count; i++)
            {
                RectTransform rect = animatedRects[i];
                if (rect == null)
                {
                    continue;
                }

                rect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);
                rect.localScale = Vector3.Lerp(startScales[i], targetScales[i], t);
            }

            yield return null;
        }

        timelineAnimationRoutine = null;
        BuildTimelineImmediate(timelineRounds);
        timelineLeadUnit = newLeadUnit;
    }

    private List<TimelineSlot> BuildTimelineSlots(List<List<BattleUnit>> timelineRounds)
    {
        List<TimelineSlot> slots = new List<TimelineSlot>();
        float cursorX = 0f;
        for (int roundIndex = 0; roundIndex < timelineRounds.Count; roundIndex++)
        {
            List<BattleUnit> round = timelineRounds[roundIndex];
            int unitIndexInRound = roundIndex == 0 ? Mathf.Max(0, currentRoundIndex) : 0;
            for (int i = 0; i < round.Count; i++)
            {
                BattleUnit unit = round[i];
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                GameObject prefab = timelineDatabase.FindButtonPrefab(unit.characterId);
                if (prefab == null)
                {
                    continue;
                }

                float width = ResolveTimelinePrefabWidth(prefab);
                bool isActive = roundIndex == 0 && i == 0;
                slots.Add(TimelineSlot.CreateUnit(
                    unit,
                    cursorX,
                    isActive,
                    absoluteRoundIndex + roundIndex,
                    unitIndexInRound));
                unitIndexInRound++;
                cursorX += width + timelineSpacing + (isActive ? activeTimelineExtraSpacing : 0f);
            }

            if (roundIndex < timelineRounds.Count - 1)
            {
                float separatorWidth = GetRoundSeparatorWidth();
                if (roundSeparatorSprite != null)
                {
                    slots.Add(TimelineSlot.CreateSeparator(cursorX + roundSeparatorSpacing, absoluteRoundIndex + roundIndex + 1));
                }

                cursorX += separatorWidth;
            }
        }

        return slots;
    }

    private List<GameObject> GetCurrentTimelineInstances()
    {
        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance == null)
            {
                continue;
            }

            result.Add(instance);
        }

        return result;
    }

    private void CreateRoundSeparator(TimelineSlot slot)
    {
        if (roundSeparatorSprite == null || timelineAnchor == null)
        {
            return;
        }

        GameObject separatorObject = new GameObject("鍥炲悎鍒嗛殧", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        separatorObject.transform.SetParent(timelineAnchor, false);

        RectTransform rect = separatorObject.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = roundSeparatorSize;
            rect.anchoredPosition = new Vector2(slot.x, 0f);
        }

        Image image = separatorObject.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = roundSeparatorSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        timelineInstances.Add(separatorObject);
        timelineInstanceKeys[separatorObject] = slot.key;
    }

    private bool TimelineNeedsAnimation(List<List<BattleUnit>> timelineRounds, BattleUnit newLeadUnit)
    {
        List<TimelineSlot> desiredSlots = BuildTimelineSlots(timelineRounds);
        if (lastTimelineSlots.Count != desiredSlots.Count)
        {
            return true;
        }

        for (int i = 0; i < lastTimelineSlots.Count; i++)
        {
            if (!lastTimelineSlots[i].key.Equals(desiredSlots[i].key))
            {
                return true;
            }
        }

        return timelineLeadUnit != newLeadUnit;
    }

    private List<BattleUnit> GetCurrentTimelineUnits()
    {
        List<BattleUnit> result = new List<BattleUnit>();
        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance == null)
            {
                continue;
            }

            BattleUnit unit;
            if (timelineInstanceUnits.TryGetValue(instance, out unit) && unit != null)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private static int[] MatchTimelineSlotsByKey(List<TimelineSlot> previousSlots, List<TimelineSlot> desiredSlots)
    {
        int[] matches = new int[previousSlots.Count];
        for (int i = 0; i < matches.Length; i++)
        {
            matches[i] = -1;
        }

        Dictionary<TimelineSlotKey, int> desiredIndices = new Dictionary<TimelineSlotKey, int>();
        for (int i = 0; i < desiredSlots.Count; i++)
        {
            desiredIndices[desiredSlots[i].key] = i;
        }

        for (int i = 0; i < previousSlots.Count; i++)
        {
            int desiredIndex;
            if (desiredIndices.TryGetValue(previousSlots[i].key, out desiredIndex))
            {
                matches[i] = desiredIndex;
            }
        }

        return matches;
    }

    private Dictionary<BattleUnit, float> BuildTimelineUnitPositions(List<List<BattleUnit>> timelineRounds)
    {
        Dictionary<BattleUnit, float> positions = new Dictionary<BattleUnit, float>();
        float cursorX = 0f;
        for (int roundIndex = 0; roundIndex < timelineRounds.Count; roundIndex++)
        {
            List<BattleUnit> round = timelineRounds[roundIndex];
            for (int i = 0; i < round.Count; i++)
            {
                BattleUnit unit = round[i];
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                GameObject prefab = timelineDatabase.FindButtonPrefab(unit.characterId);
                if (prefab == null)
                {
                    continue;
                }

                float width = ResolveTimelinePrefabWidth(prefab);
                positions[unit] = cursorX;
                bool isActive = roundIndex == 0 && i == 0;
                cursorX += width + timelineSpacing + (isActive ? activeTimelineExtraSpacing : 0f);
            }

            if (roundIndex < timelineRounds.Count - 1)
            {
                cursorX += GetRoundSeparatorWidth();
            }
        }

        return positions;
    }

    private List<BattleUnit> FlattenTimelineUnits(List<List<BattleUnit>> timelineRounds)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        for (int roundIndex = 0; roundIndex < timelineRounds.Count; roundIndex++)
        {
            List<BattleUnit> round = timelineRounds[roundIndex];
            for (int i = 0; i < round.Count; i++)
            {
                BattleUnit unit = round[i];
                if (unit != null && unit.IsAlive)
                {
                    result.Add(unit);
                }
            }
        }

        return result;
    }

    private GameObject FindTimelineInstance(BattleUnit unit)
    {
        if (unit == null)
        {
            return null;
        }

        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance == null)
            {
                continue;
            }

            BattleUnit mappedUnit;
            if (timelineInstanceUnits.TryGetValue(instance, out mappedUnit) && mappedUnit == unit)
            {
                return instance;
            }
        }

        return null;
    }

    private float GetRoundSeparatorWidth()
    {
        if (roundSeparatorSprite == null)
        {
            return roundSeparatorSpacing;
        }

        return roundSeparatorSpacing + roundSeparatorSize.x;
    }

    private static float ResolveTimelinePrefabWidth(GameObject prefab)
    {
        if (prefab == null)
        {
            return 100f;
        }

        RectTransform rect = prefab.transform as RectTransform;
        return ResolveTimelineItemWidth(rect);
    }

    private List<List<BattleUnit>> BuildTimelineRounds()
    {
        List<List<BattleUnit>> rounds = new List<List<BattleUnit>>();

        if (currentRoundIndex >= 0 && currentRoundIndex < currentRoundOrder.Count)
        {
            List<BattleUnit> currentRemaining = new List<BattleUnit>();
            for (int i = currentRoundIndex; i < currentRoundOrder.Count; i++)
            {
                BattleUnit unit = currentRoundOrder[i];
                if (unit != null && unit.IsAlive)
                {
                    currentRemaining.Add(unit);
                }
            }

            if (currentRemaining.Count > 0)
            {
                rounds.Add(currentRemaining);
            }
        }

        int desiredRoundCount = Mathf.Max(1, previewRoundCount);
        EnsureUpcomingRounds(Mathf.Max(0, desiredRoundCount - rounds.Count));
        for (int i = 0; i < upcomingRoundOrders.Count && rounds.Count < desiredRoundCount; i++)
        {
            List<BattleUnit> previewRound = new List<BattleUnit>();
            List<BattleUnit> sourceRound = upcomingRoundOrders[i];
            for (int j = 0; j < sourceRound.Count; j++)
            {
                BattleUnit unit = sourceRound[j];
                if (unit != null && unit.IsAlive)
                {
                    previewRound.Add(unit);
                }
            }

            if (previewRound.Count > 0)
            {
                rounds.Add(previewRound);
            }
        }

        return rounds;
    }

    private void EnsureTimelineMask()
    {
        if (timelineAnchor == null)
        {
            return;
        }

        if (timelineAnchor.GetComponent<RectMask2D>() == null)
        {
            timelineAnchor.gameObject.AddComponent<RectMask2D>();
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

    public void RemoveUnitFromBattle(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        bool wasActiveUnit = unit == activeUnit;
        int removedRoundIndex = currentRoundOrder.IndexOf(unit);

        if (grid != null)
        {
            grid.RemoveUnit(unit);
        }

        units.Remove(unit);
        currentRoundOrder.RemoveAll(candidate => candidate == unit);
        for (int i = 0; i < upcomingRoundOrders.Count; i++)
        {
            upcomingRoundOrders[i].RemoveAll(candidate => candidate == unit);
        }

        if (removedRoundIndex >= 0 && removedRoundIndex < currentRoundIndex)
        {
            currentRoundIndex = Mathf.Max(0, currentRoundIndex - 1);
        }

        unit.gameObject.SetActive(false);
        waitingForEnemyAction = false;
        hasMovementHoverPreview = false;
        movementHoverHasAnyVisibleCells = false;

        if (wasActiveUnit)
        {
            activeUnit = null;
            if (currentRoundOrder.Count == 0)
            {
                StartNewRound();
            }

            if (currentRoundOrder.Count > 0)
            {
                currentRoundIndex = Mathf.Clamp(currentRoundIndex, 0, currentRoundOrder.Count - 1);
                BeginCurrentTurn();
                return;
            }
        }

        CleanupDeadUnits();
        RefreshHighlights();
        RefreshActiveUnitUi();
        RefreshTimeline();
    }

    private void ClearTimelineInstances()
    {
        if (timelineAnimationRoutine != null)
        {
            StopCoroutine(timelineAnimationRoutine);
            timelineAnimationRoutine = null;
        }

        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        timelineInstances.Clear();
        timelineInstanceUnits.Clear();
        timelineInstanceKeys.Clear();
        lastTimelineSlots.Clear();
    }

    private static BattleUnit FindTimelineLeadUnit(List<List<BattleUnit>> timelineRounds)
    {
        for (int roundIndex = 0; roundIndex < timelineRounds.Count; roundIndex++)
        {
            List<BattleUnit> round = timelineRounds[roundIndex];
            for (int i = 0; i < round.Count; i++)
            {
                BattleUnit unit = round[i];
                if (unit != null && unit.IsAlive)
                {
                    return unit;
                }
            }
        }

        return null;
    }

    private struct TimelineSlot
    {
        public readonly BattleUnit unit;
        public readonly float x;
        public readonly bool isActive;
        public readonly bool isSeparator;
        public readonly TimelineSlotKey key;

        private TimelineSlot(BattleUnit unit, float x, bool isActive, bool isSeparator, TimelineSlotKey key)
        {
            this.unit = unit;
            this.x = x;
            this.isActive = isActive;
            this.isSeparator = isSeparator;
            this.key = key;
        }

        public static TimelineSlot CreateUnit(BattleUnit unit, float x, bool isActive, int absoluteRound, int indexInRound)
        {
            return new TimelineSlot(unit, x, isActive, false, TimelineSlotKey.CreateUnit(absoluteRound, unit));
        }

        public static TimelineSlot CreateSeparator(float x, int absoluteRound)
        {
            return new TimelineSlot(null, x, false, true, TimelineSlotKey.CreateSeparator(absoluteRound));
        }
    }

    private struct TimelineSlotKey
    {
        public readonly int absoluteRound;
        public readonly BattleUnit unit;
        public readonly bool isSeparator;

        private TimelineSlotKey(int absoluteRound, BattleUnit unit, bool isSeparator)
        {
            this.absoluteRound = absoluteRound;
            this.unit = unit;
            this.isSeparator = isSeparator;
        }

        public static TimelineSlotKey CreateUnit(int absoluteRound, BattleUnit unit)
        {
            return new TimelineSlotKey(absoluteRound, unit, false);
        }

        public static TimelineSlotKey CreateSeparator(int absoluteRound)
        {
            return new TimelineSlotKey(absoluteRound, null, true);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TimelineSlotKey))
            {
                return false;
            }

            TimelineSlotKey other = (TimelineSlotKey)obj;
            return absoluteRound == other.absoluteRound &&
                   unit == other.unit &&
                   isSeparator == other.isSeparator;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = absoluteRound * 397;
                hash ^= isSeparator ? 1 : 0;
                if (unit != null)
                {
                    hash ^= unit.GetHashCode();
                }

                return hash;
            }
        }
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

    public void RequestEndTurn()
    {
        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        if (activeUnit.IsMoving)
        {
            return;
        }

        EndTurn();
    }

    public void ToggleMovementMode()
    {
        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        if (activeUnit.IsMoving)
        {
            return;
        }

        if (!activeUnit.CanSpendActionPoints(GetMoveSkillActionPointCost()))
        {
            movementModeActive = false;
            hasMovementHoverPreview = false;
            movementHoverHasAnyVisibleCells = false;
            RefreshHighlights();
            return;
        }

        movementModeActive = !movementModeActive;
        if (!movementModeActive)
        {
            hasMovementHoverPreview = false;
            movementHoverHasAnyVisibleCells = false;
        }

        RefreshHighlights();
    }

    private void UpdateMovementHoverPreview()
    {
        if (!movementModeActive || activeUnit == null || !activeUnit.IsAlive)
        {
            if (hasMovementHoverPreview || movementHoverHasAnyVisibleCells)
            {
                hasMovementHoverPreview = false;
                movementHoverHasAnyVisibleCells = false;
                RefreshHighlights();
            }

            return;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;

        if (!clickPlane.Raycast(ray, out enter))
        {
            if (hasMovementHoverPreview)
            {
                hasMovementHoverPreview = false;
                RefreshHighlights();
            }

            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            if (hasMovementHoverPreview || movementHoverHasAnyVisibleCells)
            {
                hasMovementHoverPreview = false;
                movementHoverHasAnyVisibleCells = false;
                RefreshHighlights();
            }

            return;
        }

        bool footprintInside = grid.IsFootprintInside(activeUnit, hoveredCell);
        List<Vector2Int> path = footprintInside ? grid.FindPath(activeUnit, hoveredCell) : null;
        bool withinMoveRange = path != null && path.Count > 1 && path.Count - 1 <= GetMoveSkillRange(activeUnit);
        bool previewValid = footprintInside && withinMoveRange;
        bool hasAnyVisibleCells = HasAnyVisibleMovementPreviewCells(hoveredCell);

        if (hasMovementHoverPreview &&
            movementHoverCell == hoveredCell &&
            movementHoverValid == previewValid &&
            movementHoverHasAnyVisibleCells == hasAnyVisibleCells)
        {
            return;
        }

        hasMovementHoverPreview = hasAnyVisibleCells;
        movementHoverCell = hoveredCell;
        movementHoverValid = previewValid;
        movementHoverHasAnyVisibleCells = hasAnyVisibleCells;
        RefreshHighlights();
    }

    private void ApplyMovementHoverPreview()
    {
        if (!movementModeActive || !hasMovementHoverPreview || !movementHoverHasAnyVisibleCells || activeUnit == null)
        {
            return;
        }

        if (movementHoverValid)
        {
            grid.HighlightFootprintAt(
                movementHoverCell,
                activeUnit.footprintSize,
                movementPreviewValidColor);
            return;
        }

        grid.HighlightPartialFootprint(activeUnit, movementHoverCell, movementPreviewInvalidColor);
    }

    private bool HasAnyVisibleMovementPreviewCells(Vector2Int centerCell)
    {
        if (activeUnit == null)
        {
            return false;
        }

        int radius = activeUnit.FootprintRadius;
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!grid.IsInside(cell))
                {
                    continue;
                }

                BattleUnit occupant = grid.GetUnitAt(cell);
                if (occupant != null && occupant != activeUnit)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private int GetMoveSkillRange(BattleUnit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry moveSkill = skillDatabase != null
            ? skillDatabase.FindEntry(BattleSkillDatabase.MoveSkillId)
            : null;
        if (moveSkill == null)
        {
            return unit.moveDistance;
        }

        return moveSkill.ResolveRange(unit.moveDistance);
    }

    private int GetMoveSkillActionPointCost()
    {
        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry moveSkill = skillDatabase != null
            ? skillDatabase.FindEntry(BattleSkillDatabase.MoveSkillId)
            : null;
        if (moveSkill == null)
        {
            return 0;
        }

        return moveSkill.ResolveActionPointCost();
    }

    public IReadOnlyList<BattleUnit> GetTimelineUnitsForUi()
    {
        List<List<BattleUnit>> rounds = BuildTimelineRounds();
        return FlattenTimelineUnits(rounds);
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

    private void BindEndTurnButton()
    {
        UnbindEndTurnButton();

        Transform buttonTransform = FindTransformByPath(EndTurnButtonPath);
        if (buttonTransform == null)
        {
            return;
        }

        endTurnButton = buttonTransform.GetComponent<Button>();
        if (endTurnButton == null)
        {
            return;
        }

        endTurnButton.onClick.AddListener(RequestEndTurn);
    }

    private void BindMoveButton()
    {
        UnbindMoveButton();

        Transform buttonTransform = FindTransformByPath(MoveButtonPath);
        if (buttonTransform == null)
        {
            return;
        }

        moveButton = buttonTransform.GetComponent<Button>();
        if (moveButton == null)
        {
            return;
        }

        moveButton.onClick.AddListener(ToggleMovementMode);
    }

    private void UnbindEndTurnButton()
    {
        if (endTurnButton == null)
        {
            return;
        }

        endTurnButton.onClick.RemoveListener(RequestEndTurn);
        endTurnButton = null;
    }

    private void UnbindMoveButton()
    {
        if (moveButton == null)
        {
            return;
        }

        moveButton.onClick.RemoveListener(ToggleMovementMode);
        moveButton = null;
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

