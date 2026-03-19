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
    private const string MoveSkillButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u79FB\u52A8\u6309\u94AE";
    private const string NormalAttackSkillId = "\u666E\u901A\u653B\u51FB";
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

    private readonly Color skillPreviewValidColor = new Color(1.00f, 0.90f, 0.20f, 0.70f);
    private readonly Color skillPreviewInvalidColor = new Color(1.00f, 0.25f, 0.20f, 0.60f);
    private readonly Color skillSelfOccupiedColor = new Color(1.00f, 1.00f, 1.00f, 0.32f);
    private readonly Color skillAllyOccupiedColor = new Color(0.20f, 0.85f, 0.42f, 0.28f);
    private readonly Color skillEnemyOccupiedColor = new Color(0.95f, 0.28f, 0.20f, 0.28f);
    private readonly Color hoveredEnemyFlashColor = new Color(1.00f, 0.20f, 0.20f, 0.72f);
    private readonly Color hoveredAllyFlashColor = new Color(0.20f, 0.85f, 0.42f, 0.72f);
    private readonly Color skillCostNormalColor = Color.white;
    private readonly Color skillCostInsufficientColor = new Color(0.95f, 0.25f, 0.25f, 1f);

    private BattleGrid grid;
    private Camera battleCamera;
    private BattleUnit activeUnit;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;
    private RectTransform overlayCanvasRect;
    private RectTransform skillCostHintRect;
    private TMP_Text skillCostHintText;
    private Transform timelineAnchor;
    private Button endTurnButton;
    private Button moveSkillButton;
    private BattleSceneBindings sceneBindings;
    private BattleSkillDatabase skillDatabase;
    private TurnTimelineButtonDatabase timelineDatabase;
    private Coroutine timelineAnimationRoutine;
    private Coroutine skillExecutionRoutine;
    private BattleUnit timelineLeadUnit;
    private int absoluteRoundIndex = -1;
    private int currentRoundIndex = -1;
    private string activeSkillId = string.Empty;
    private BattleSkillDatabase.SkillEntry activeSkill;
    private bool hasSkillHoverPreview;
    private Vector2Int skillHoverCell;
    private bool skillHoverValid;
    private bool skillHoverHasAnyVisibleCells;
    private int skillHoverActionPointCost;
    private BattleUnit hoveredSkillTarget;
    private bool isResolvingSkillExecution;

    private sealed class EnemySkillChoice
    {
        public string skillId = string.Empty;
        public int weight;
        public int order;
        public BattleSkillDatabase.SkillEntry skill;
    }

    private struct EnemySkillAction
    {
        public EnemySkillChoice choice;
        public BattleUnit targetUnit;
        public Vector2Int targetCell;
    }

    public BattleUnit ActiveUnit
    {
        get { return activeUnit; }
    }

    public int PreviewActionPointCost
    {
        get { return skillHoverActionPointCost; }
    }

    public bool ShouldPreviewActionPointCost
    {
        get { return IsSkillModeActive() && skillHoverValid && skillHoverActionPointCost > 0; }
    }

    public void Initialize(BattleGrid battleGrid, Camera mainCamera, IEnumerable<BattleUnit> battleUnits)
    {
        grid = battleGrid;
        battleCamera = mainCamera;
        activeUnitIdText = FindActiveUnitIdText();
        overlayCanvasRect = null;
        skillCostHintRect = null;
        skillCostHintText = null;
        sceneBindings = BattleSceneBindings.FindInActiveScene();
        timelineAnchor = ResolveTimelineAnchor();
        EnsureTimelineMask();
        BindEndTurnButton();
        BindSkillButton();
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
        isResolvingSkillExecution = false;
        currentRoundIndex = -1;
        absoluteRoundIndex = -1;
        activeSkillId = string.Empty;
        activeSkill = null;
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

    public void SetSkillCostHintText(TMP_Text hintText)
    {
        skillCostHintText = hintText;
        skillCostHintRect = hintText != null ? hintText.rectTransform : null;
        overlayCanvasRect = skillCostHintRect != null ? skillCostHintRect.GetComponentInParent<Canvas>()?.transform as RectTransform : null;

        if (skillCostHintText != null)
        {
            skillCostHintText.raycastTarget = false;
            skillCostHintText.text = string.Empty;
            skillCostHintText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        UnbindEndTurnButton();
        UnbindSkillButton();
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

        if (isResolvingSkillExecution)
        {
            return;
        }

        if (activeUnit.isPlayerControlled)
        {
            UpdateSkillHoverPreview();
            UpdateHoveredTargetFlash();
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
        if (IsSkillModeActive() && Input.GetMouseButtonDown(1))
        {
            ClearActiveSkillMode();
            RefreshHighlights();
            return;
        }

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
        if (IsSkillModeActive())
        {
            TryUseActiveSkill(activeUnit, clickedCell, target);
            return;
        }

    }

    private IEnumerator RunEnemyTurn()
    {
        waitingForEnemyAction = true;
        yield return new WaitForSeconds(0.5f);

        while (activeUnit != null && activeUnit.IsAlive && activeUnit.currentActionPoints > 0)
        {
            List<EnemySkillChoice> skillChoices = BuildEnemySkillChoices(activeUnit);
            if (skillChoices.Count == 0)
            {
                break;
            }

            EnemySkillAction action;
            if (TryFindEnemySkillAction(activeUnit, skillChoices, out action))
            {
                yield return ExecuteEnemySkillAction(activeUnit, action);
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            float moveDuration;
            if (TryMoveEnemyTowardSkillRange(activeUnit, skillChoices, out moveDuration))
            {
                if (moveDuration > 0f)
                {
                    yield return new WaitForSeconds(moveDuration);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }

                continue;
            }

            break;
        }

        EndTurn();
    }

    private void TryMove(BattleUnit unit, Vector2Int destination)
    {
        BattleSkillDatabase.SkillEntry moveSkill = ResolveSkill(BattleSkillDatabase.MoveSkillId);
        int moveManaCost = GetSkillManaCost(moveSkill);
        if (unit == null || !unit.CanSpendMana(moveManaCost))
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

        int moveActionPointCost = GetMoveActionPointCost(unit, path, moveSkill);
        if (!unit.CanSpendActionPoints(moveActionPointCost))
        {
            return;
        }

        if (path.Count - 1 > GetMoveMaxRange(unit, moveSkill))
        {
            return;
        }

        float moveDuration = grid.MoveUnit(unit, destination);
        if (moveSkill != null)
        {
            unit.PlayTimedAnimation(moveSkill.actionStateName, moveDuration);
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        ClearActiveSkillMode();
        RefreshHighlights();
    }

    private void EndTurn()
    {
        waitingForEnemyAction = false;
        ClearActiveSkillMode();
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
                ClearActiveSkillMode();
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

        if (activeUnit.isPlayerControlled && IsSkillModeActive())
        {
            int skillRange = GetDisplayedSkillRange(activeUnit, activeSkill);
            if (IsMovementSkillActive())
            {
                grid.HighlightReachable(activeUnit, skillRange);
            }
            else
            {
                grid.HighlightRange(activeUnit, skillRange);
            }

            grid.HighlightOccupiedUnitsWithinRange(
                activeUnit,
                skillRange,
                skillSelfOccupiedColor,
                skillAllyOccupiedColor,
                skillEnemyOccupiedColor);
        }
        ApplySkillHoverPreview();
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
            timelineAnchor = ResolveTimelineAnchor();
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
        InvalidateFutureRounds();

        if (removedRoundIndex >= 0 && removedRoundIndex < currentRoundIndex)
        {
            currentRoundIndex = Mathf.Max(0, currentRoundIndex - 1);
        }

        unit.gameObject.SetActive(false);
        waitingForEnemyAction = false;
        ClearActiveSkillMode();

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
        EnsureUpcomingRounds(Mathf.Max(0, previewRoundCount - 1));
        RefreshHighlights();
        RefreshActiveUnitUi();
        RefreshTimeline();
    }

    private void HandleUnitDefeat(BattleUnit unit)
    {
        if (unit == null || unit.IsAlive)
        {
            return;
        }

        Debug.Log(unit.unitName + " is defeated.");
        RemoveUnitFromBattle(unit);
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

    public void ToggleAttackMode()
    {
        ToggleSkillMode(NormalAttackSkillId);
    }

    public void ToggleMovementMode()
    {
        ToggleSkillMode(BattleSkillDatabase.MoveSkillId);
    }

    public void ToggleSkillMode(string skillId)
    {
        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        if (activeUnit.IsMoving)
        {
            return;
        }

        BattleSkillDatabase.SkillEntry nextSkill = ResolveSkill(skillId);
        if (nextSkill == null)
        {
            ClearActiveSkillMode();
            RefreshHighlights();
            return;
        }

        if (!activeUnit.CanSpendActionPoints(GetSkillActionPointCost(nextSkill)) ||
            !activeUnit.CanSpendMana(GetSkillManaCost(nextSkill)))
        {
            ClearActiveSkillMode();
            RefreshHighlights();
            return;
        }

        if (string.Equals(activeSkillId, skillId, System.StringComparison.Ordinal))
        {
            ClearActiveSkillMode();
        }
        else
        {
            activeSkillId = skillId;
            activeSkill = nextSkill;
            hasSkillHoverPreview = false;
            skillHoverHasAnyVisibleCells = false;
        }

        RefreshHighlights();
    }

    private void UpdateSkillHoverPreview()
    {
        if (!IsSkillModeActive() || activeUnit == null || !activeUnit.IsAlive)
        {
            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells)
            {
                hasSkillHoverPreview = false;
                skillHoverHasAnyVisibleCells = false;
                RefreshHighlights();
            }

            HideSkillCostHint();
            ClearHoveredSkillTarget();
            return;
        }

        bool shouldShowAreaPreview = ShouldShowSkillAreaPreview(activeSkill);
        if (!shouldShowAreaPreview)
        {
            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells)
            {
                hasSkillHoverPreview = false;
                skillHoverHasAnyVisibleCells = false;
                RefreshHighlights();
            }

            HideSkillCostHint();
        }

        UpdateHoveredSkillTarget();

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;

        if (!clickPlane.Raycast(ray, out enter))
        {
            if (hasSkillHoverPreview || skillHoverValid || skillHoverActionPointCost > 0)
            {
                hasSkillHoverPreview = false;
                skillHoverValid = false;
                skillHoverActionPointCost = 0;
                RefreshHighlights();
            }

            HideSkillCostHint();
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells || skillHoverValid || skillHoverActionPointCost > 0)
            {
                hasSkillHoverPreview = false;
                skillHoverValid = false;
                skillHoverHasAnyVisibleCells = false;
                skillHoverActionPointCost = 0;
                RefreshHighlights();
            }

            HideSkillCostHint();
            return;
        }

        BattleUnit hoveredUnit = grid.GetUnitAt(hoveredCell);
        bool footprintInside = grid.IsFootprintInside(activeUnit, hoveredCell);
        List<Vector2Int> path = IsMovementSkillActive() && footprintInside ? grid.FindPath(activeUnit, hoveredCell) : null;
        bool canCastAtHover = CanCastSkillAt(activeUnit, hoveredCell, hoveredUnit, activeSkill, path);
        int actionPointCost = canCastAtHover ? GetHoveredSkillActionPointCost(activeUnit, path, activeSkill) : 0;
        bool hasAnyVisibleCells = shouldShowAreaPreview && HasAnyVisibleSkillPreviewCells(hoveredCell);

        if (hasSkillHoverPreview &&
            skillHoverCell == hoveredCell &&
            skillHoverValid == canCastAtHover &&
            skillHoverHasAnyVisibleCells == hasAnyVisibleCells &&
            skillHoverActionPointCost == actionPointCost)
        {
            UpdateSkillCostHint();
            return;
        }

        hasSkillHoverPreview = hasAnyVisibleCells;
        skillHoverCell = hoveredCell;
        skillHoverValid = canCastAtHover;
        skillHoverHasAnyVisibleCells = hasAnyVisibleCells;
        skillHoverActionPointCost = actionPointCost;
        RefreshHighlights();
        UpdateSkillCostHint();
    }

    private void ApplySkillHoverPreview()
    {
        if (!IsSkillModeActive() || !hasSkillHoverPreview || !skillHoverHasAnyVisibleCells || activeUnit == null)
        {
            return;
        }

        int previewFootprintSize = GetSkillPreviewFootprintSize(activeSkill);
        if (previewFootprintSize <= 1)
        {
            return;
        }

        if (skillHoverValid)
        {
            grid.HighlightFootprintAt(
                skillHoverCell,
                previewFootprintSize,
                skillPreviewValidColor);
            return;
        }

        grid.HighlightPartialFootprint(previewFootprintSize, skillHoverCell, skillPreviewInvalidColor);
    }

    private void ApplyHoveredTargetPreview()
    {
        if (hoveredSkillTarget == null || !hoveredSkillTarget.IsAlive)
        {
            grid.ClearHoveredFootprint();
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
        Color overlayColor = ResolveHoveredTargetFlashColor(hoveredSkillTarget);
        overlayColor.a = Mathf.Lerp(0.18f, overlayColor.a, pulse);
        grid.SetHoveredFootprint(hoveredSkillTarget, overlayColor);
    }

    private bool HasAnyVisibleSkillPreviewCells(Vector2Int centerCell)
    {
        if (activeUnit == null || !ShouldShowSkillAreaPreview(activeSkill))
        {
            return false;
        }

        int footprintSize = GetSkillPreviewFootprintSize(activeSkill);
        if (footprintSize <= 1)
        {
            return false;
        }

        int radius = Mathf.Max(0, footprintSize / 2);
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

    private static bool ShouldShowSkillAreaPreview(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null)
        {
            return false;
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return false;
        }

        int width = Mathf.Max(1, skill.effectSize.x);
        int height = Mathf.Max(1, skill.effectSize.y);
        return width > 1 || height > 1;
    }

    private static int GetSkillPreviewFootprintSize(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null)
        {
            return 0;
        }

        int width = Mathf.Max(1, skill.effectSize.x);
        int height = Mathf.Max(1, skill.effectSize.y);
        return Mathf.Max(width, height);
    }

    private bool IsSkillModeActive()
    {
        return activeSkill != null && !string.IsNullOrWhiteSpace(activeSkillId);
    }

    private void ClearActiveSkillMode()
    {
        activeSkillId = string.Empty;
        activeSkill = null;
        hasSkillHoverPreview = false;
        skillHoverValid = false;
        skillHoverHasAnyVisibleCells = false;
        skillHoverActionPointCost = 0;
        ClearHoveredSkillTarget();
        HideSkillCostHint();
    }

    private void TryUseActiveSkill(BattleUnit unit, Vector2Int clickedCell, BattleUnit target)
    {
        if (!IsSkillModeActive() || isResolvingSkillExecution)
        {
            return;
        }

        if (string.Equals(activeSkillId, BattleSkillDatabase.MoveSkillId, System.StringComparison.Ordinal))
        {
            if (target != null && target != unit)
            {
                return;
            }

            TryMove(unit, clickedCell);
            return;
        }

        if (activeSkill == null)
        {
            return;
        }

        if (activeSkill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            if (!CanCastSkillAt(unit, clickedCell, target, activeSkill, null))
            {
                return;
            }

            if (skillExecutionRoutine != null)
            {
                StopCoroutine(skillExecutionRoutine);
            }

            skillExecutionRoutine = StartCoroutine(ExecuteTargetSkillRoutine(unit, target, activeSkill));
            return;
        }

        if (activeSkill.skillType == BattleSkillDatabase.SkillType.Area)
        {
            if (!CanCastSkillAt(unit, clickedCell, target, activeSkill, null))
            {
                return;
            }

            if (skillExecutionRoutine != null)
            {
                StopCoroutine(skillExecutionRoutine);
            }

            skillExecutionRoutine = StartCoroutine(ExecuteAreaSkillRoutine(unit, clickedCell, activeSkill));
        }
    }

    private int GetSkillRange(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        if (skill == null)
        {
            return unit.moveDistance;
        }

        return skill.ResolveRange(unit.moveDistance);
    }

    private int GetDisplayedSkillRange(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        if (unit == null)
        {
            return 0;
        }

        if (skill != null && IsMovementSkillId(activeSkillId))
        {
            return GetMoveMaxRange(unit, skill);
        }

        return GetSkillRange(skill, unit);
    }

    private int GetSkillActionPointCost(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null)
        {
            return 0;
        }

        return skill.ResolveActionPointCost();
    }

    private int GetSkillManaCost(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null)
        {
            return 0;
        }

        return skill.ResolveManaCost();
    }

    private BattleSkillDatabase.SkillEntry ResolveSkill(string skillId)
    {
        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        return skillDatabase != null
            ? skillDatabase.FindEntry(skillId)
            : null;
    }

    private static bool IsValidSkillTarget(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return false;
        }

        switch (skill.castTarget)
        {
            case BattleSkillDatabase.CastTarget.Self:
                return target == caster;
            case BattleSkillDatabase.CastTarget.Enemy:
                return target.team != caster.team;
            case BattleSkillDatabase.CastTarget.Ally:
                return target.team == caster.team;
            case BattleSkillDatabase.CastTarget.All:
                return true;
            default:
                return false;
        }
    }

    private IEnumerator ExecuteTargetSkillRoutine(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            yield break;
        }

        int actionPointCost = GetSkillActionPointCost(skill);
        int manaCost = GetSkillManaCost(skill);
        if (!caster.CanSpendActionPoints(actionPointCost) || !caster.CanSpendMana(manaCost))
        {
            yield break;
        }

        isResolvingSkillExecution = true;
        caster.SpendActionPoints(actionPointCost);
        caster.SpendMana(manaCost);
        caster.FaceToward(target.transform.position);
        yield return PlaySkillAnimationRoutine(caster, skill);
        ApplyCombatArtDamage(caster, target, skill);
        ClearActiveSkillMode();
        RefreshHighlights();
        RefreshTimeline();

        Debug.Log("Target skill selected: " + caster.unitName + " -> " + target.unitName + " using " + skill.skillId);
        isResolvingSkillExecution = false;
        skillExecutionRoutine = null;
    }

    private IEnumerator ExecuteAreaSkillRoutine(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || skill == null)
        {
            yield break;
        }

        int actionPointCost = GetSkillActionPointCost(skill);
        int manaCost = GetSkillManaCost(skill);
        if (!caster.CanSpendActionPoints(actionPointCost) || !caster.CanSpendMana(manaCost))
        {
            yield break;
        }

        isResolvingSkillExecution = true;
        caster.SpendActionPoints(actionPointCost);
        caster.SpendMana(manaCost);
        caster.FaceToward(grid.GetWorldPosition(targetCell));
        yield return PlaySkillAnimationRoutine(caster, skill);
        ApplyCombatArtAreaDamage(caster, targetCell, skill);
        ClearActiveSkillMode();
        RefreshHighlights();
        RefreshTimeline();

        Debug.Log("Area skill selected: " + caster.unitName + " -> " + targetCell + " using " + skill.skillId);
        isResolvingSkillExecution = false;
        skillExecutionRoutine = null;
    }

    private void ApplyCombatArtDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return;
        }

        if (skill.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            return;
        }

        int damage = CalculateCombatArtDamage(caster, skill);
        if (damage <= 0)
        {
            return;
        }

        target.ApplyDamage(damage);
        HandleUnitDefeat(target);
    }

    private void ApplyCombatArtAreaDamage(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || skill == null)
        {
            return;
        }

        if (skill.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            return;
        }

        int damage = CalculateCombatArtDamage(caster, skill);
        if (damage <= 0)
        {
            return;
        }

        int footprintSize = GetSkillPreviewFootprintSize(skill);
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            if (!IsValidSkillTarget(caster, unit, skill))
            {
                continue;
            }

            if (!IsUnitInsideAreaFootprint(unit, targetCell, footprintSize))
            {
                continue;
            }

            unit.ApplyDamage(damage);
            HandleUnitDefeat(unit);
        }
    }

    private int CalculateCombatArtDamage(BattleUnit caster, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || skill == null)
        {
            return 0;
        }

        float attackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(caster.characterId);
        if (attackPower <= 0f)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(attackPower * Mathf.Max(0f, skill.damageMultiplier)));
    }

    private bool CanCastSkillAt(
        BattleUnit caster,
        Vector2Int targetCell,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        List<Vector2Int> movementPath)
    {
        if (caster == null || skill == null || !grid.IsInside(targetCell))
        {
            return false;
        }

        if (IsMovementSkillId(activeSkillId))
        {
            return movementPath != null &&
                movementPath.Count > 1 &&
                grid.IsFootprintInside(caster, targetCell) &&
                target == null &&
                movementPath.Count - 1 <= GetDisplayedSkillRange(caster, skill);
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return target != null &&
                IsValidSkillTarget(caster, target, skill) &&
                grid.IsUnitWithinRange(caster, target, GetDisplayedSkillRange(caster, skill));
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Area)
        {
            return grid.IsCellWithinRange(caster, targetCell, GetDisplayedSkillRange(caster, skill));
        }

        return false;
    }

    private void UpdateHoveredSkillTarget()
    {
        if (!IsSkillModeActive() || activeSkill == null || activeUnit == null)
        {
            ClearHoveredSkillTarget();
            return;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!clickPlane.Raycast(ray, out enter))
        {
            ClearHoveredSkillTarget();
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            ClearHoveredSkillTarget();
            return;
        }

        BattleUnit target = grid.GetUnitAt(hoveredCell);
        if (!IsHoveredSkillTargetValid(activeUnit, target, activeSkill, hoveredCell))
        {
            ClearHoveredSkillTarget();
            return;
        }

        if (hoveredSkillTarget != null && hoveredSkillTarget != target)
        {
            hoveredSkillTarget.ClearTint();
            grid.ClearHoveredFootprint();
        }

        hoveredSkillTarget = target;
    }

    private bool IsHoveredSkillTargetValid(
        BattleUnit caster,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill,
        Vector2Int hoveredCell)
    {
        if (caster == null || target == null || skill == null)
        {
            return false;
        }

        if (!IsValidSkillTarget(caster, target, skill))
        {
            return false;
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return grid.IsUnitWithinRange(caster, target, GetDisplayedSkillRange(caster, skill));
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return false;
        }

        if (!grid.IsCellWithinRange(caster, hoveredCell, GetDisplayedSkillRange(caster, skill)))
        {
            return false;
        }

        return IsUnitInsideAreaFootprint(target, hoveredCell, GetSkillPreviewFootprintSize(skill));
    }

    private bool IsUnitInsideAreaFootprint(BattleUnit target, Vector2Int areaCenterCell, int footprintSize)
    {
        if (target == null || footprintSize <= 0)
        {
            return false;
        }

        int areaRadius = Mathf.Max(0, footprintSize / 2);
        int unitRadius = target.FootprintRadius;
        for (int y = target.currentCell.y - unitRadius; y <= target.currentCell.y + unitRadius; y++)
        {
            for (int x = target.currentCell.x - unitRadius; x <= target.currentCell.x + unitRadius; x++)
            {
                if (x < areaCenterCell.x - areaRadius || x > areaCenterCell.x + areaRadius)
                {
                    continue;
                }

                if (y < areaCenterCell.y - areaRadius || y > areaCenterCell.y + areaRadius)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private void UpdateHoveredTargetFlash()
    {
        if (hoveredSkillTarget == null || !hoveredSkillTarget.IsAlive)
        {
            grid.ClearHoveredFootprint();
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
        Color targetFlashColor = ResolveHoveredTargetFlashColor(hoveredSkillTarget);
        ApplyHoveredTargetPreview();
        hoveredSkillTarget.ApplyTint(targetFlashColor, Mathf.Lerp(0.2f, 0.75f, pulse));
    }

    private Color ResolveHoveredTargetFlashColor(BattleUnit target)
    {
        if (target == null)
        {
            return hoveredEnemyFlashColor;
        }

        return target.team == BattleTeam.Player
            ? hoveredAllyFlashColor
            : hoveredEnemyFlashColor;
    }

    private void ClearHoveredSkillTarget()
    {
        if (hoveredSkillTarget != null)
        {
            hoveredSkillTarget.ClearTint();
        }

        grid.ClearHoveredFootprint();
        hoveredSkillTarget = null;
    }

    private int GetMoveMaxRange(BattleUnit unit, BattleSkillDatabase.SkillEntry moveSkill)
    {
        if (unit == null)
        {
            return 0;
        }

        int segmentDistance = Mathf.Max(1, unit.moveDistance);
        int actionPointCostPerSegment = Mathf.Max(1, GetSkillActionPointCost(moveSkill));
        int segmentCount = unit.currentActionPoints / actionPointCostPerSegment;
        return segmentCount * segmentDistance;
    }

    private int GetMoveActionPointCost(BattleUnit unit, List<Vector2Int> path, BattleSkillDatabase.SkillEntry moveSkill)
    {
        if (unit == null || path == null || path.Count <= 1)
        {
            return 0;
        }

        int steps = path.Count - 1;
        int segmentDistance = Mathf.Max(1, unit.moveDistance);
        int segmentCount = Mathf.CeilToInt((float)steps / segmentDistance);
        int actionPointCostPerSegment = Mathf.Max(1, GetSkillActionPointCost(moveSkill));
        return segmentCount * actionPointCostPerSegment;
    }

    private int GetHoveredSkillActionPointCost(BattleUnit unit, List<Vector2Int> path, BattleSkillDatabase.SkillEntry skill)
    {
        if (unit == null || skill == null)
        {
            return 0;
        }

        if (IsMovementSkillId(activeSkillId))
        {
            if (path == null || path.Count <= 1)
            {
                return 0;
            }

            return GetMoveActionPointCost(unit, path, skill);
        }

        return GetSkillActionPointCost(skill);
    }

    private bool IsMovementSkillActive()
    {
        return IsMovementSkillId(activeSkillId);
    }

    private static bool IsMovementSkillId(string skillId)
    {
        return string.Equals(skillId, BattleSkillDatabase.MoveSkillId, System.StringComparison.Ordinal);
    }

    private void UpdateSkillCostHint()
    {
        if (!ShouldShowSkillCostHint())
        {
            HideSkillCostHint();
            return;
        }

        TMP_Text hint = EnsureSkillCostHint();
        RectTransform canvasRect = overlayCanvasRect;
        if (hint == null || canvasRect == null)
        {
            return;
        }

        hint.text = "消耗行动点：" + skillHoverActionPointCost;
        hint.color = activeUnit != null && skillHoverActionPointCost > activeUnit.currentActionPoints
            ? skillCostInsufficientColor
            : skillCostNormalColor;
        hint.gameObject.SetActive(true);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out localPoint);
        skillCostHintRect.anchoredPosition = localPoint + new Vector2(90f, -28f);
    }

    private bool ShouldShowSkillCostHint()
    {
        if (!IsSkillModeActive() || !skillHoverValid || skillHoverActionPointCost <= 0)
        {
            return false;
        }

        return true;
    }

    private TMP_Text EnsureSkillCostHint()
    {
        if (skillCostHintText != null && overlayCanvasRect != null)
        {
            return skillCostHintText;
        }

        Transform canvasTransform = ResolveOverlayCanvasTransform();
        if (canvasTransform == null)
        {
            return null;
        }

        overlayCanvasRect = canvasTransform as RectTransform;
        if (overlayCanvasRect == null)
        {
            return null;
        }

        Transform existing = FindChildByName(canvasTransform, "SkillCostHint");
        if (existing == null)
        {
            GameObject hintObject = new GameObject("SkillCostHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            existing = hintObject.transform;
            existing.SetParent(canvasTransform, false);
        }

        skillCostHintRect = existing as RectTransform;
        skillCostHintText = existing.GetComponent<TMP_Text>();
        if (skillCostHintRect == null || skillCostHintText == null)
        {
            return null;
        }

        skillCostHintRect.anchorMin = new Vector2(0.5f, 0.5f);
        skillCostHintRect.anchorMax = new Vector2(0.5f, 0.5f);
        skillCostHintRect.pivot = new Vector2(0f, 0.5f);
        skillCostHintRect.sizeDelta = new Vector2(260f, 44f);

        skillCostHintText.raycastTarget = false;
        skillCostHintText.fontSize = 28f;
        skillCostHintText.alignment = TextAlignmentOptions.Left;
        skillCostHintText.color = skillCostNormalColor;
        skillCostHintText.text = string.Empty;
        skillCostHintText.gameObject.SetActive(false);
        return skillCostHintText;
    }

    private void HideSkillCostHint()
    {
        if (skillCostHintText != null)
        {
            skillCostHintText.gameObject.SetActive(false);
        }
    }

    public IReadOnlyList<BattleUnit> GetTimelineUnitsForUi()
    {
        List<List<BattleUnit>> rounds = BuildTimelineRounds();
        return FlattenTimelineUnits(rounds);
    }

    private static Transform FindTransformByPath(string path)
    {
        return SceneHierarchyPathUtility.FindInActiveScene(path);
    }

    private void BindEndTurnButton()
    {
        UnbindEndTurnButton();

        Button button = ResolveEndTurnButton();
        if (button == null)
        {
            return;
        }

        endTurnButton = button;
        endTurnButton.onClick.AddListener(RequestEndTurn);
    }

    private void BindSkillButton()
    {
        UnbindSkillButton();

        Button button = ResolveMoveSkillButton();
        if (button == null)
        {
            return;
        }

        moveSkillButton = button;
        moveSkillButton.onClick.AddListener(ToggleMovementMode);
    }

    private Transform ResolveTimelineAnchor()
    {
        if (sceneBindings != null && sceneBindings.timelineAnchor != null)
        {
            return sceneBindings.timelineAnchor;
        }

        return FindTransformByPath(TimelineAnchorPath);
    }

    private Button ResolveEndTurnButton()
    {
        if (sceneBindings != null && sceneBindings.endTurnButton != null)
        {
            return sceneBindings.endTurnButton;
        }

        Transform buttonTransform = FindTransformByPath(EndTurnButtonPath);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private Button ResolveMoveSkillButton()
    {
        if (sceneBindings != null && sceneBindings.moveSkillButton != null)
        {
            return sceneBindings.moveSkillButton;
        }

        Transform buttonTransform = FindTransformByPath(MoveSkillButtonPath);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private Transform ResolveOverlayCanvasTransform()
    {
        if (sceneBindings != null && sceneBindings.overlayCanvas != null)
        {
            return sceneBindings.overlayCanvas;
        }

        return FindTransformByPath("Canvas");
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

    private void UnbindSkillButton()
    {
        if (moveSkillButton == null)
        {
            return;
        }

        moveSkillButton.onClick.RemoveListener(ToggleMovementMode);
        moveSkillButton = null;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        return SceneHierarchyPathUtility.FindDirectChildByName(parent, childName);
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

    private List<EnemySkillChoice> BuildEnemySkillChoices(BattleUnit caster)
    {
        List<EnemySkillChoice> result = new List<EnemySkillChoice>();
        if (caster == null)
        {
            return result;
        }

        HashSet<string> seenSkillIds = new HashSet<string>(System.StringComparer.Ordinal);
        CharacterSkillLoadoutDatabase loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry skillEntry =
            loadoutDatabase != null ? loadoutDatabase.FindEntry(caster.characterId) : null;

        if (skillEntry != null && skillEntry.skillIds != null)
        {
            CharacterSkillLoadoutDatabase.EnsureSlotDataSize(skillEntry, skillEntry.skillIds.Count);
            for (int i = 0; i < skillEntry.skillIds.Count; i++)
            {
                TryAddEnemySkillChoice(
                    result,
                    seenSkillIds,
                    skillEntry.skillIds[i],
                    CharacterSkillLoadoutDatabase.GetSkillWeightAt(skillEntry, i),
                    i);
            }
        }

        List<string> grantedSkills = InventoryShortcutRuntimeBinder.GetGrantedSkillIdsForCharacter(caster.characterId);
        for (int i = 0; i < grantedSkills.Count; i++)
        {
            TryAddEnemySkillChoice(result, seenSkillIds, grantedSkills[i], 0, 1000 + i);
        }

        TryAddEnemySkillChoice(result, seenSkillIds, NormalAttackSkillId, 0, int.MaxValue);
        result.Sort(CompareEnemySkillChoices);
        return result;
    }

    private void TryAddEnemySkillChoice(
        List<EnemySkillChoice> choices,
        HashSet<string> seenSkillIds,
        string skillId,
        int weight,
        int order)
    {
        if (choices == null || seenSkillIds == null || string.IsNullOrWhiteSpace(skillId) || !seenSkillIds.Add(skillId))
        {
            return;
        }

        BattleSkillDatabase.SkillEntry skill = ResolveSkill(skillId);
        if (skill == null)
        {
            return;
        }

        choices.Add(new EnemySkillChoice
        {
            skillId = skillId,
            weight = weight,
            order = order,
            skill = skill
        });
    }

    private static int CompareEnemySkillChoices(EnemySkillChoice left, EnemySkillChoice right)
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

        int weightCompare = right.weight.CompareTo(left.weight);
        if (weightCompare != 0)
        {
            return weightCompare;
        }

        return left.order.CompareTo(right.order);
    }

    private bool TryFindEnemySkillAction(BattleUnit caster, List<EnemySkillChoice> skillChoices, out EnemySkillAction action)
    {
        action = new EnemySkillAction();
        if (caster == null || skillChoices == null)
        {
            return false;
        }

        for (int i = 0; i < skillChoices.Count; i++)
        {
            EnemySkillChoice choice = skillChoices[i];
            if (!CanEnemyUseSkill(caster, choice))
            {
                continue;
            }

            if (TryFindEnemySkillActionForChoice(caster, choice, out action))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindEnemySkillActionForChoice(BattleUnit caster, EnemySkillChoice choice, out EnemySkillAction action)
    {
        action = new EnemySkillAction();
        if (caster == null || choice == null || choice.skill == null)
        {
            return false;
        }

        BattleUnit bestTarget = null;
        int bestDistance = int.MaxValue;
        foreach (BattleUnit unit in units)
        {
            if (!IsValidEnemySkillTarget(caster, unit, choice.skill))
            {
                continue;
            }

            Vector2Int targetCell = unit.currentCell;
            if (!CanEnemyCastSkillAt(caster, targetCell, unit, choice.skill))
            {
                continue;
            }

            int distance = grid.ManhattanDistance(caster.currentCell, unit.currentCell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = unit;
                action = new EnemySkillAction
                {
                    choice = choice,
                    targetUnit = unit,
                    targetCell = targetCell
                };
            }
        }

        return bestTarget != null;
    }

    private bool TryMoveEnemyTowardSkillRange(BattleUnit caster, List<EnemySkillChoice> skillChoices, out float moveDuration)
    {
        moveDuration = 0f;
        if (caster == null || grid == null || skillChoices == null || skillChoices.Count == 0)
        {
            return false;
        }

        BattleSkillDatabase.SkillEntry moveSkill = ResolveSkill(BattleSkillDatabase.MoveSkillId);
        int maxMoveRange = GetMoveMaxRange(caster, moveSkill);
        int moveManaCost = GetSkillManaCost(moveSkill);
        if (maxMoveRange <= 0 || !caster.CanSpendMana(moveManaCost))
        {
            return false;
        }

        Vector2Int bestCell = caster.currentCell;
        BattleUnit bestTarget = null;
        int bestWeight = int.MinValue;
        int bestDistanceAfterMove = int.MaxValue;
        int bestPathLength = int.MaxValue;

        for (int skillIndex = 0; skillIndex < skillChoices.Count; skillIndex++)
        {
            EnemySkillChoice choice = skillChoices[skillIndex];
            if (!CanEnemyUseSkill(caster, choice))
            {
                continue;
            }

            foreach (BattleUnit unit in units)
            {
                if (!IsValidEnemySkillTarget(caster, unit, choice.skill))
                {
                    continue;
                }

                for (int y = 0; y < grid.height; y++)
                {
                    for (int x = 0; x < grid.width; x++)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        if (candidate == caster.currentCell)
                        {
                            continue;
                        }

                        List<Vector2Int> path = grid.FindPath(caster, candidate);
                        if (path == null || path.Count <= 1)
                        {
                            continue;
                        }

                        int stepCount = path.Count - 1;
                        if (stepCount > maxMoveRange)
                        {
                            continue;
                        }

                        int moveActionPointCost = GetMoveActionPointCost(caster, path, moveSkill);
                        if (!caster.CanSpendActionPoints(moveActionPointCost))
                        {
                            continue;
                        }

                        if (caster.currentActionPoints - moveActionPointCost < GetSkillActionPointCost(choice.skill))
                        {
                            continue;
                        }

                        if (caster.currentMana - moveManaCost < GetSkillManaCost(choice.skill))
                        {
                            continue;
                        }

                        if (!CanEnemyCastSkillFromCell(caster, candidate, unit, choice.skill))
                        {
                            continue;
                        }

                        int distanceAfterMove = grid.ManhattanDistance(candidate, unit.currentCell);
                        if (choice.weight > bestWeight ||
                            (choice.weight == bestWeight && distanceAfterMove < bestDistanceAfterMove) ||
                            (choice.weight == bestWeight && distanceAfterMove == bestDistanceAfterMove && stepCount < bestPathLength))
                        {
                            bestWeight = choice.weight;
                            bestDistanceAfterMove = distanceAfterMove;
                            bestPathLength = stepCount;
                            bestCell = candidate;
                            bestTarget = unit;
                        }
                    }
                }
            }
        }

        if (bestTarget == null)
        {
            BattleUnit fallbackTarget = FindClosestLivingOpponent(caster);
            if (fallbackTarget == null)
            {
                return false;
            }

            bestCell = FindBestStepToward(caster, fallbackTarget, GetSkillRange(skillChoices[0].skill, caster));
            bestTarget = fallbackTarget;
            if (bestCell == caster.currentCell)
            {
                return false;
            }
        }

        if (!TryMoveEnemyToCell(caster, bestCell, out moveDuration))
        {
            return false;
        }

        caster.FaceToward(bestTarget.transform.position);
        return true;
    }

    private bool TryMoveEnemyToCell(BattleUnit unit, Vector2Int destination, out float moveDuration)
    {
        moveDuration = 0f;
        if (unit == null || destination == unit.currentCell)
        {
            return false;
        }

        BattleSkillDatabase.SkillEntry moveSkill = ResolveSkill(BattleSkillDatabase.MoveSkillId);
        int moveManaCost = GetSkillManaCost(moveSkill);
        if (!unit.CanSpendMana(moveManaCost))
        {
            return false;
        }

        List<Vector2Int> path = grid.FindPath(unit, destination);
        if (path == null || path.Count <= 1)
        {
            return false;
        }

        int moveActionPointCost = GetMoveActionPointCost(unit, path, moveSkill);
        if (!unit.CanSpendActionPoints(moveActionPointCost))
        {
            return false;
        }

        if (path.Count - 1 > GetMoveMaxRange(unit, moveSkill))
        {
            return false;
        }

        moveDuration = grid.MoveUnit(unit, destination);
        if (moveSkill != null)
        {
            unit.PlayTimedAnimation(moveSkill.actionStateName, moveDuration);
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        return true;
    }

    private bool CanEnemyUseSkill(BattleUnit caster, EnemySkillChoice choice)
    {
        if (caster == null || choice == null || choice.skill == null)
        {
            return false;
        }

        return caster.CanSpendActionPoints(GetSkillActionPointCost(choice.skill)) &&
            caster.CanSpendMana(GetSkillManaCost(choice.skill));
    }

    private bool IsValidEnemySkillTarget(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        return target != null &&
            target.IsAlive &&
            target.team != caster.team &&
            IsValidSkillTarget(caster, target, skill);
    }

    private bool CanEnemyCastSkillAt(
        BattleUnit caster,
        Vector2Int targetCell,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill)
    {
        return CanCastSkillAt(caster, targetCell, target, skill, null);
    }

    private bool CanEnemyCastSkillFromCell(
        BattleUnit caster,
        Vector2Int castCell,
        BattleUnit target,
        BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return false;
        }

        int skillRange = GetSkillRange(skill, caster);
        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return grid.ManhattanDistance(castCell, target.currentCell) <= skillRange &&
                IsValidSkillTarget(caster, target, skill);
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Area)
        {
            return grid.ManhattanDistance(castCell, target.currentCell) <= skillRange;
        }

        return false;
    }

    private IEnumerator ExecuteEnemySkillAction(BattleUnit caster, EnemySkillAction action)
    {
        if (caster == null || action.choice == null || action.choice.skill == null)
        {
            yield break;
        }

        if (action.choice.skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            if (action.targetUnit != null)
            {
                yield return ExecuteTargetSkillRoutine(caster, action.targetUnit, action.choice.skill);
            }

            yield break;
        }

        yield return ExecuteAreaSkillRoutine(caster, action.targetCell, action.choice.skill);
    }

    private IEnumerator PlaySkillAnimationRoutine(BattleUnit caster, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || skill == null || string.IsNullOrWhiteSpace(skill.actionStateName))
        {
            yield break;
        }

        Animator animator = caster.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            yield break;
        }

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        animator.Play(skill.actionStateName, 0, 0f);

        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float clipDuration = currentState.length;
        if (clipDuration > 0.01f)
        {
            yield return new WaitForSeconds(clipDuration);
        }

        if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            animator.Play(previousStateHash, 0, 0f);
        }
    }

    private Vector2Int FindBestStepToward(BattleUnit mover, BattleUnit target, int desiredRange = 0)
    {
        Vector2Int bestCell = mover.currentCell;
        int bestDistance = Mathf.Max(0, grid.ManhattanDistance(mover.currentCell, target.currentCell) - Mathf.Max(0, desiredRange));
        BattleSkillDatabase.SkillEntry moveSkill = ResolveSkill(BattleSkillDatabase.MoveSkillId);
        int maxMoveRange = GetMoveMaxRange(mover, moveSkill);

        for (int dx = -maxMoveRange; dx <= maxMoveRange; dx++)
        {
            for (int dy = -maxMoveRange; dy <= maxMoveRange; dy++)
            {
                Vector2Int candidate = mover.currentCell + new Vector2Int(dx, dy);
                if (grid.ManhattanDistance(mover.currentCell, candidate) > maxMoveRange)
                {
                    continue;
                }

                List<Vector2Int> path = grid.FindPath(mover, candidate);
                if (path == null || path.Count <= 1)
                {
                    continue;
                }

                int moveActionPointCost = GetMoveActionPointCost(mover, path, moveSkill);
                if (!mover.CanSpendActionPoints(moveActionPointCost))
                {
                    continue;
                }

                int candidateDistance = Mathf.Max(0, grid.ManhattanDistance(candidate, target.currentCell) - Mathf.Max(0, desiredRange));
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

