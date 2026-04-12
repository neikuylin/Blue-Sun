using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTurnSystem : MonoBehaviour
{
    public const string ExplorationIdleSkillId = "探索待机";
    public const string ExplorationMoveSkillId = "探索移动";

    private enum BattleFlowMode
    {
        Combat,
        Exploration
    }

    private const string EndTurnButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u7ED3\u675F\u56DE\u5408\u6309\u94AE";
    private const string MoveSkillButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u79FB\u52A8\u6309\u94AE";
    private const string NormalAttackSkillId = "\u666E\u901A\u653B\u51FB";
    private readonly List<BattleUnit> units = new List<BattleUnit>();
    private readonly List<BattleUnit> currentRoundOrder = new List<BattleUnit>();
    private readonly List<List<BattleUnit>> upcomingRoundOrders = new List<List<BattleUnit>>();
    private readonly Dictionary<BattleUnit, int> initiativeTieBreakers = new Dictionary<BattleUnit, int>();
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
    private readonly Color activePlayerFootprintColor = new Color(1.00f, 1.00f, 1.00f, 0.32f);
    private readonly Color activeEnemyFootprintColor = new Color(0.95f, 0.28f, 0.20f, 0.32f);
    private readonly Color targetHealthBarColor = new Color(0.90f, 0.18f, 0.22f, 1f);
    private readonly Color physicalDamageColor = Color.white;
    private readonly Color fireDamageColor = new Color(1.00f, 0.55f, 0.12f, 1f);
    private readonly Color corruptionDamageColor = new Color(0.25f, 0.90f, 0.35f, 1f);
    private readonly Color coldDamageColor = new Color(0.25f, 0.65f, 1.00f, 1f);
    private readonly Color targetNameSelfColor = Color.white;
    private readonly Color targetNameAllyColor = new Color(0.20f, 0.85f, 0.42f, 1f);
    private readonly Color targetNameEnemyColor = new Color(0.95f, 0.28f, 0.20f, 1f);
    private readonly Color hoveredEnemyFlashColor = new Color(1.00f, 0.20f, 0.20f, 0.72f);
    private readonly Color hoveredAllyFlashColor = new Color(0.20f, 0.85f, 0.42f, 0.72f);
    private readonly Color activePlayerOutlineColor = Color.white;
    private readonly Color lockedEnemyOutlineColor = new Color(1.00f, 0.22f, 0.22f, 1f);
    private readonly Color lockedAllyOutlineColor = new Color(0.20f, 0.95f, 0.42f, 1f);
    private readonly Color skillCostNormalColor = Color.white;
    private readonly Color skillCostInsufficientColor = new Color(0.95f, 0.25f, 0.25f, 1f);
    private const string PlayerInfoColorHex = BattleInfoTextUtility.PlayerInfoColorHex;
    private const string EnemyInfoColorHex = BattleInfoTextUtility.EnemyInfoColorHex;
    private const string NeutralInfoColorHex = BattleInfoTextUtility.NeutralInfoColorHex;
    private const string PhysicalInfoColorHex = BattleInfoTextUtility.PhysicalInfoColorHex;
    private const string FireInfoColorHex = BattleInfoTextUtility.FireInfoColorHex;
    private const string CorruptionInfoColorHex = BattleInfoTextUtility.CorruptionInfoColorHex;
    private const string ColdInfoColorHex = BattleInfoTextUtility.ColdInfoColorHex;
    private const int MinHitChancePercent = 0;
    private const int MaxHitChancePercent = 100;
    private const float HitFeelDurationSeconds = 0.3f;
    private const float HitFeelTimeScale = 0.1f;
    private const float DefaultFixedDeltaTime = 0.02f;
    private const float LockedTargetOutlineWidth = 0.065f;
    private const float ActiveUnitOutlineWidth = 0.075f;

    private BattleGrid grid;
    private Camera battleCamera;
    private BattleCameraController battleCameraController;
    private BattleUnit activeUnit;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;
    private RectTransform overlayCanvasRect;
    private RectTransform skillCostHintRect;
    private TMP_Text skillCostHintText;
    private BattleInfoWindowPresenter battleInfoWindowPresenter;
    private Button endTurnButton;
    private Button moveSkillButton;
    private BattleSceneBindings sceneBindings;
    private BattleSkillDatabase skillDatabase;
    private Coroutine skillExecutionRoutine;
    private Coroutine hitFeelRoutine;
    private float hitFeelRestoreTimeScale = 1f;
    private float hitFeelRestoreFixedDeltaTime = DefaultFixedDeltaTime;
    private bool hitFeelActive;
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
    private readonly List<BattleUnit> hoveredSkillTargets = new List<BattleUnit>();
    private bool isResolvingSkillExecution;
    private BattleAudioUtility.PlaybackHandle currentExplorationMoveAudioHandle;
    private BattleFlowMode currentMode = BattleFlowMode.Combat;
    private string activeExplorationActionId = ExplorationMoveSkillId;
    private string currentSkillTargetingStateName = string.Empty;
    private float currentSkillTargetingYawOffset;
    private bool skillTargetSelectionReady;
    private Coroutine skillTargetingIntroRoutine;
    private BattleUnit skillModeRotationAnchorUnit;
    private Quaternion skillModeRotationAnchorRotation = Quaternion.identity;
    private bool hasSkillModeRotationAnchor;
    private bool enterBattleAnimationInProgress;
    private bool beginTurnAfterEnterBattle;
    private BattleUnit pendingEnterBattleLeadUnit;
    private Coroutine explorationFollowerRoutine;
    private bool explorationFollowerInProgress;
    private AudioSource modeMusicSource;
    private Coroutine explorationMoveAudioStopRoutine;
    private bool pendingExplorationModeEnter;
    private BattleInputService inputService;
    private BattleTargetPanelService targetPanelService;
    private BattleTurnTimelineService timelineService;
    private 战斗模式服务 modeService;
    private 战斗技能执行服务 skillExecutionService;
    private 战斗伤害结算服务 damageResolutionService;
    private 战斗技能基础结算服务 skillCoreResolutionService;
    private 战斗效果回合结算服务 effectTurnResolutionService;
    private 战斗敌方回合服务 enemyTurnService;
    private 战斗敌方决策服务 enemyDecisionService;

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

    public bool IsExplorationMode
    {
        get { return currentMode == BattleFlowMode.Exploration; }
    }

    public BattleUnit FindUnitByCharacterId(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !string.Equals(unit.characterId, characterId, System.StringComparison.Ordinal))
            {
                continue;
            }

            return unit;
        }

        return null;
    }

    public BattleUnit FindUnitByInstanceId(int instanceId)
    {
        if (instanceId == 0)
        {
            return null;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || unit.GetInstanceID() != instanceId)
            {
                continue;
            }

            return unit;
        }

        return null;
    }

    public void Initialize(BattleGrid battleGrid, Camera mainCamera, IEnumerable<BattleUnit> battleUnits)
    {
        grid = battleGrid;
        battleCamera = mainCamera;
        battleCameraController = battleCamera != null ? battleCamera.GetComponent<BattleCameraController>() : null;
        activeUnitIdText = FindActiveUnitIdText();
        overlayCanvasRect = null;
        skillCostHintRect = null;
        skillCostHintText = null;
        battleInfoWindowPresenter = BattleInfoWindowPresenter.FindInActiveScene();
        sceneBindings = BattleSceneBindings.FindInActiveScene();
        BindEndTurnButton();
        BindSkillButton();
        skillDatabase = BattleSkillDatabase.LoadDefault();
        if (inputService == null)
        {
            inputService = new BattleInputService();
        }

        if (targetPanelService == null)
        {
            targetPanelService = new BattleTargetPanelService(
                targetHealthBarColor,
                targetNameSelfColor,
                targetNameAllyColor,
                targetNameEnemyColor);
        }

        if (timelineService == null)
        {
            timelineService = new BattleTurnTimelineService(this);
        }

        if (modeService == null)
        {
            modeService = new 战斗模式服务();
        }

        if (skillExecutionService == null)
        {
            skillExecutionService = new 战斗技能执行服务();
        }

        if (damageResolutionService == null)
        {
            damageResolutionService = new 战斗伤害结算服务();
        }

        if (skillCoreResolutionService == null)
        {
            skillCoreResolutionService = new 战斗技能基础结算服务(MinHitChancePercent, MaxHitChancePercent);
        }

        if (effectTurnResolutionService == null)
        {
            effectTurnResolutionService = new 战斗效果回合结算服务();
        }

        if (enemyTurnService == null)
        {
            enemyTurnService = new 战斗敌方回合服务();
        }

        if (enemyDecisionService == null)
        {
            enemyDecisionService = new 战斗敌方决策服务();
        }

        timelineService.Initialize(sceneBindings);
        units.Clear();
        currentRoundOrder.Clear();
        upcomingRoundOrders.Clear();
        initiativeTieBreakers.Clear();
        activeUnit = null;
        waitingForEnemyAction = false;
        isResolvingSkillExecution = false;
        currentRoundIndex = -1;
        absoluteRoundIndex = -1;
        activeSkillId = string.Empty;
        activeSkill = null;
        currentMode = BattleFlowMode.Exploration;
        activeExplorationActionId = ExplorationMoveSkillId;
        pendingExplorationModeEnter = false;

        foreach (BattleUnit unit in battleUnits)
        {
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        if (HasLivingEnemies())
        {
            StartNewRound();
            EnterCombatMode(playEnterAnimation: true);
            if (!enterBattleAnimationInProgress)
            {
                BeginCurrentTurn();
            }
        }
        else
        {
            EnterExplorationMode(playExitAnimation: false);
        }
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
        timelineService?.Dispose();
        StopExplorationFollowerRoutine();
        RestoreGlobalTimeScale();
        UnbindEndTurnButton();
        UnbindSkillButton();
        StopTrackedAudio(currentExplorationMoveAudioHandle);
        StopModeMusic();
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
        if (IsExplorationMode)
        {
            UpdateExplorationMode();
            return;
        }

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
            inputService?.HandleCombatInput(
                grid,
                battleCamera,
                activeUnit,
                IsSkillModeActive(),
                ClearActiveSkillMode,
                RefreshHighlights,
                ClearLockedTargetUnit,
                SetLockedTargetUnit,
                TryUseActiveSkill);
            return;
        }

        if (!waitingForEnemyAction)
        {
            StartCoroutine(RunEnemyTurn());
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        targetPanelService?.Refresh(
            grid,
            battleCamera,
            activeUnit,
            hoveredSkillTarget,
            IsExplorationMode,
            RefreshSelectionOutlines);
    }

    private void UpdateExplorationMode()
    {
        if (activeUnit == null || !activeUnit.IsAlive)
        {
            activeUnit = FindExplorationPlayerUnit();
            RefreshHighlights();
            RefreshSelectionOutlines();
            RefreshActiveUnitUi();
        }

        if (activeUnit == null || !activeUnit.IsAlive)
        {
            return;
        }

        inputService?.HandleExplorationInput(
            activeExplorationActionId,
            grid,
            battleCamera,
            activeUnit,
            TryMoveFreely);
    }

    private IEnumerator RunEnemyTurn()
    {
        waitingForEnemyAction = true;
        if (enemyTurnService == null)
        {
            EndTurn();
            yield break;
        }

        yield return enemyTurnService.执行敌方回合(
            activeUnit,
            BuildEnemySkillChoices,
            TryFindEnemySkillActionNullable,
            TryMoveEnemyTowardSkillRangeNullable,
            ExecuteEnemySkillAction,
            RefreshHighlights,
            EndTurn);
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

        List<Vector2Int> path = grid.FindPathIgnoringAllies(unit, destination);
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

        float moveDuration = grid.MoveUnitIgnoringAllies(unit, destination);
        if (moveSkill != null)
        {
            StartCoroutine(PlayTrackedSkillAudioRoutine(unit, moveSkill, moveDuration));
            unit.PlayTimedAnimation(
                unit.GetMoveAnimationStateName(ResolveSkillActionStateName(moveSkill, unit)),
                moveDuration,
                unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)),
                ResolveSkillCompensateActionMotion(moveSkill, unit));
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        ClearActiveSkillMode();
        RefreshHighlights();
    }

    private void TryMoveFreely(BattleUnit unit, Vector2Int destination)
    {
        if (unit == null || grid == null)
        {
            return;
        }

        Vector2Int currentCell = unit.IsMoving ? grid.WorldToCell(unit.transform.position) : unit.currentCell;
        if (destination == currentCell)
        {
            return;
        }

        if (!unit.IsMoving)
        {
            List<Vector2Int> path = grid.FindPathIgnoringAllies(unit, destination);
            if (path == null || path.Count <= 1)
            {
                return;
            }
        }

        float originalMoveSpeed = unit.moveSpeed;
        unit.moveSpeed = Mathf.Max(0.01f, originalMoveSpeed * 0.5f);
        bool redirected = unit.IsMoving;
        float moveDuration = redirected
            ? grid.RedirectMovingUnitIgnoringAllies(unit, destination)
            : grid.MoveUnitIgnoringAllies(unit, destination);
        unit.moveSpeed = originalMoveSpeed;
        if (moveDuration <= 0f)
        {
            return;
        }

        string idleStateName = ResolveExplorationIdleStateName();
        PlayExplorationMoveAudio(unit, moveDuration);
        unit.PlayTimedAnimation(
            unit.GetMoveAnimationStateName(ResolveExplorationMoveStateName()),
            moveDuration,
            idleStateName,
            ResolveExplorationMoveCompensateMotion());
        QueueExplorationFollowerMovement(unit);
        RefreshHighlights();
    }

    private void QueueExplorationFollowerMovement(BattleUnit leaderUnit)
    {
        if (!IsExplorationMode || leaderUnit == null || !leaderUnit.IsAlive || !leaderUnit.isPlayerControlled)
        {
            return;
        }

        if (!string.Equals(leaderUnit.characterId, "玩家", System.StringComparison.Ordinal))
        {
            return;
        }

        if (explorationFollowerRoutine != null)
        {
            StopCoroutine(explorationFollowerRoutine);
            explorationFollowerRoutine = null;
        }

        explorationFollowerRoutine = StartCoroutine(RunExplorationFollowerMovementRoutine(leaderUnit));
    }

    private IEnumerator RunExplorationFollowerMovementRoutine(BattleUnit leaderUnit)
    {
        explorationFollowerInProgress = true;
        WaitForSeconds idleDelay = new WaitForSeconds(2f);

        while (IsExplorationMode && leaderUnit != null && leaderUnit.IsAlive)
        {
            bool issuedFollowerMove = false;
            bool hasPendingFollowerGap = false;
            float maxMoveDuration = 0f;
            Vector2Int leaderCell = leaderUnit.IsMoving ? grid.WorldToCell(leaderUnit.transform.position) : leaderUnit.currentCell;
            List<BattleUnit> followers = GetExplorationFollowersInSlotOrder(leaderUnit);
            HashSet<Vector2Int> reservedDestinations = new HashSet<Vector2Int>();
            for (int i = 0; i < followers.Count; i++)
            {
                BattleUnit follower = followers[i];
                if (follower == null || !follower.IsAlive)
                {
                    continue;
                }

                if (follower.IsMoving)
                {
                    hasPendingFollowerGap = true;
                    continue;
                }

                if (grid.ManhattanDistance(follower.currentCell, leaderCell) <= 10)
                {
                    continue;
                }

                hasPendingFollowerGap = true;

                Vector2Int destination;
                if (!TryFindExplorationFollowerDestination(follower, leaderCell, reservedDestinations, out destination))
                {
                    continue;
                }

                reservedDestinations.Add(destination);
                float moveDuration = PlayExplorationFollowerMove(follower, destination);
                if (moveDuration > 0f)
                {
                    issuedFollowerMove = true;
                    maxMoveDuration = Mathf.Max(maxMoveDuration, moveDuration);
                }
            }

            if (issuedFollowerMove)
            {
                RefreshHighlights();
                yield return new WaitForSeconds(maxMoveDuration);
                continue;
            }

            if (!leaderUnit.IsMoving && !hasPendingFollowerGap && !issuedFollowerMove)
            {
                break;
            }

            yield return idleDelay;
        }

        explorationFollowerInProgress = false;
        explorationFollowerRoutine = null;
        RefreshHighlights();
    }

    private List<BattleUnit> GetExplorationFollowersInSlotOrder(BattleUnit leaderUnit)
    {
        List<BattleUnit> orderedFollowers = new List<BattleUnit>();
        HashSet<BattleUnit> added = new HashSet<BattleUnit>();
        IReadOnlyList<CharacterSelectionState.SlotSelection> slotSelections = CharacterSelectionState.SlotSelections;
        for (int i = 0; i < slotSelections.Count; i++)
        {
            CharacterSelectionState.SlotSelection slot = slotSelections[i];
            if (string.IsNullOrWhiteSpace(slot.characterId))
            {
                continue;
            }

            BattleUnit unit = FindUnitByCharacterId(slot.characterId);
            if (unit == null || unit == leaderUnit || !unit.IsAlive || !unit.isPlayerControlled || unit.team != BattleTeam.Player)
            {
                continue;
            }

            if (added.Add(unit))
            {
                orderedFollowers.Add(unit);
            }
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || unit == leaderUnit || !unit.IsAlive || !unit.isPlayerControlled || unit.team != BattleTeam.Player)
            {
                continue;
            }

            if (added.Add(unit))
            {
                orderedFollowers.Add(unit);
            }
        }

        return orderedFollowers;
    }

    private bool TryFindExplorationFollowerDestination(
        BattleUnit follower,
        Vector2Int leaderCell,
        HashSet<Vector2Int> reservedDestinations,
        out Vector2Int destination)
    {
        destination = follower != null ? follower.currentCell : Vector2Int.zero;
        if (follower == null || grid == null)
        {
            return false;
        }

        int bestDistanceDelta = int.MaxValue;
        int bestPathLength = int.MaxValue;
        bool found = false;

        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                int leaderDistance = grid.ManhattanDistance(candidate, leaderCell);
                if (leaderDistance > 5)
                {
                    continue;
                }

                if (reservedDestinations != null && reservedDestinations.Contains(candidate))
                {
                    continue;
                }

                if (!grid.IsWalkableIgnoringAllies(follower, candidate))
                {
                    continue;
                }

                List<Vector2Int> path = grid.FindPathIgnoringAllies(follower, candidate);
                if (path == null || path.Count <= 1)
                {
                    continue;
                }

                int distanceDelta = Mathf.Abs(5 - leaderDistance);
                int pathLength = path.Count - 1;
                if (distanceDelta > bestDistanceDelta)
                {
                    continue;
                }

                if (distanceDelta == bestDistanceDelta && pathLength >= bestPathLength)
                {
                    continue;
                }

                bestDistanceDelta = distanceDelta;
                bestPathLength = pathLength;
                destination = candidate;
                found = true;
            }
        }

        return found;
    }

    private float PlayExplorationFollowerMove(BattleUnit unit, Vector2Int destination)
    {
        if (unit == null || grid == null || destination == unit.currentCell)
        {
            return 0f;
        }

        float originalMoveSpeed = unit.moveSpeed;
        unit.moveSpeed = Mathf.Max(0.01f, originalMoveSpeed * 0.5f);
        float moveDuration = grid.MoveUnitIgnoringAllies(unit, destination);
        unit.moveSpeed = originalMoveSpeed;
        if (moveDuration <= 0f)
        {
            return 0f;
        }

        string idleStateName = ResolveExplorationIdleStateName();
        unit.PlayTimedAnimation(
            unit.GetMoveAnimationStateName(ResolveExplorationMoveStateName()),
            moveDuration,
            idleStateName,
            ResolveExplorationMoveCompensateMotion());
        return moveDuration;
    }

    private void EndTurn()
    {
        waitingForEnemyAction = false;
        ClearLockedTargetUnit();
        ClearActiveSkillMode();
        AdvanceTurn();
    }

    private void EvaluateExplorationMode()
    {
        if (IsExplorationMode || HasLivingEnemies())
        {
            return;
        }

        if (isResolvingSkillExecution || skillExecutionRoutine != null)
        {
            pendingExplorationModeEnter = true;
            return;
        }

        pendingExplorationModeEnter = false;
        EnterExplorationMode();
    }

    private void EnterExplorationMode(bool playExitAnimation = true)
    {
        bool switchedFromCombat = currentMode == BattleFlowMode.Combat;
        currentMode = BattleFlowMode.Exploration;
        activeExplorationActionId = ExplorationMoveSkillId;
        waitingForEnemyAction = false;
        currentRoundOrder.Clear();
        upcomingRoundOrders.Clear();
        currentRoundIndex = -1;
        absoluteRoundIndex = -1;
        ClearActiveSkillMode();
        ClearLockedTargetUnit();
        ClearHoveredSkillTarget();
        activeUnit = modeService != null
            ? modeService.进入探索模式(
                switchedFromCombat && playExitAnimation,
                FindExplorationPlayerUnit,
                PlayExitBattleAnimations,
                PlayExplorationIdleAnimations,
                FocusCameraOnUnit,
                SetCombatUiVisible,
                RefreshModeMusic,
                RefreshSelectionOutlines,
                RefreshHighlights,
                RefreshActiveUnitUi,
                RefreshTimeline)
            : FindExplorationPlayerUnit();
    }

    private void EnterCombatMode(bool playEnterAnimation)
    {
        StopExplorationFollowerRoutine();
        bool switchedFromExploration = currentMode == BattleFlowMode.Exploration;
        currentMode = BattleFlowMode.Combat;
        waitingForEnemyAction = false;
        activeExplorationActionId = ExplorationMoveSkillId;
        战斗模式服务.进入战斗结果 result = modeService.进入战斗模式(
            switchedFromExploration,
            playEnterAnimation,
            GetNextLivingRoundUnit,
            FocusCameraOnUnit,
            StopCameraFollow,
            SetCombatUiVisible,
            RefreshModeMusic,
            RefreshSelectionOutlines,
            RefreshHighlights,
            RefreshActiveUnitUi,
            RefreshTimeline);
        pendingEnterBattleLeadUnit = result.待进入战斗单位;
        beginTurnAfterEnterBattle = result.进入战斗后开始回合;
        enterBattleAnimationInProgress = result.进入战斗动画进行中;
        if (pendingEnterBattleLeadUnit != null)
        {
            activeUnit = pendingEnterBattleLeadUnit;
        }

        if (enterBattleAnimationInProgress)
        {
            StartCoroutine(PlayEnterBattleAnimations());
        }
    }

    private void StopExplorationFollowerRoutine()
    {
        explorationFollowerInProgress = false;
        if (explorationFollowerRoutine == null)
        {
            return;
        }

        StopCoroutine(explorationFollowerRoutine);
        explorationFollowerRoutine = null;
    }

    private bool HasLivingEnemies()
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null && unit.IsAlive && unit.team == BattleTeam.Enemy)
            {
                return true;
            }
        }

        return false;
    }

    private BattleUnit FindExplorationPlayerUnit()
    {
        if (activeUnit != null &&
            activeUnit.IsAlive &&
            activeUnit.team == BattleTeam.Player &&
            activeUnit.isPlayerControlled &&
            string.Equals(activeUnit.characterId, "玩家", System.StringComparison.Ordinal))
        {
            return activeUnit;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null &&
                unit.IsAlive &&
                unit.team == BattleTeam.Player &&
                unit.isPlayerControlled &&
                string.Equals(unit.characterId, "玩家", System.StringComparison.Ordinal))
            {
                return unit;
            }
        }

        return null;
    }

    private void SetCombatUiVisible(bool visible)
    {
        modeService?.设置战斗界面可见(timelineService, sceneBindings, endTurnButton, moveSkillButton, visible);
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
        EvaluateExplorationMode();
        if (IsExplorationMode)
        {
            return;
        }

        ClearSelectionOutlines();
        CleanupDeadUnits();
        while (currentRoundIndex >= 0 && currentRoundIndex < currentRoundOrder.Count)
        {
            BattleUnit candidate = currentRoundOrder[currentRoundIndex];
            if (candidate != null && candidate.IsAlive)
            {
                activeUnit = candidate;
                FocusCameraOnActiveUnit();
                ProcessEffectTurnsForTurnOwner(activeUnit);
                CleanupDeadUnits();
                if (activeUnit == null || !activeUnit.IsAlive)
                {
                    currentRoundIndex++;
                    continue;
                }
                activeUnit.BeginTurn();
                ClearActiveSkillMode();
                RefreshSelectionOutlines();
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
        RefreshSelectionOutlines();
        StopCameraFollow();
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

        if (IsExplorationMode)
        {
            grid.HighlightFootprint(activeUnit, activePlayerFootprintColor);
            return;
        }

        if (!IsSkillModeActive())
        {
            Color activeFootprintColor = activeUnit.team == BattleTeam.Enemy
                ? activeEnemyFootprintColor
                : activePlayerFootprintColor;
            grid.HighlightFootprint(activeUnit, activeFootprintColor);

            BattleUnit persistentTarget = targetPanelService != null ? targetPanelService.LockedTargetUnit : null;
            if (persistentTarget != null && persistentTarget != activeUnit)
            {
                Color targetFootprintColor = persistentTarget.team == BattleTeam.Enemy
                    ? activeEnemyFootprintColor
                    : skillAllyOccupiedColor;
                grid.HighlightFootprint(persistentTarget, targetFootprintColor);
            }
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
                grid.HighlightCircularRange(activeUnit, skillRange);
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

    private void SetLockedTargetUnit(BattleUnit unit)
    {
        targetPanelService?.SetLockedTargetUnit(unit, IsSkillModeActive(), RefreshSelectionOutlines, RefreshHighlights);
    }

    private void ClearLockedTargetUnit()
    {
        targetPanelService?.ClearLockedTargetUnit(RefreshSelectionOutlines, RefreshHighlights);
    }

    private void RefreshSelectionOutlines()
    {
        ClearSelectionOutlines();
        BattleUnit hoveredTargetUnit = targetPanelService != null ? targetPanelService.HoveredTargetUnit : null;
        BattleUnit lockedTargetUnit = targetPanelService != null ? targetPanelService.LockedTargetUnit : null;

        if (IsExplorationMode)
        {
            if (activeUnit != null && activeUnit.IsAlive)
            {
                activeUnit.SetLockOutline(activePlayerOutlineColor, ActiveUnitOutlineWidth, true);
            }

            return;
        }

        if (activeUnit != null && activeUnit.IsAlive)
        {
            Color activeOutlineColor = activeUnit.team == BattleTeam.Enemy
                ? lockedEnemyOutlineColor
                : activePlayerOutlineColor;
            activeUnit.SetLockOutline(activeOutlineColor, ActiveUnitOutlineWidth, true);
        }

        if (hoveredTargetUnit != null &&
            hoveredTargetUnit.IsAlive &&
            hoveredTargetUnit != activeUnit &&
            hoveredTargetUnit != lockedTargetUnit)
        {
            Color hoverOutlineColor = hoveredTargetUnit.team == BattleTeam.Enemy
                ? lockedEnemyOutlineColor
                : lockedAllyOutlineColor;
            hoveredTargetUnit.SetLockOutline(hoverOutlineColor, LockedTargetOutlineWidth, true);
        }

        if (lockedTargetUnit == null || !lockedTargetUnit.IsAlive || lockedTargetUnit == activeUnit)
        {
            return;
        }

        Color outlineColor = lockedTargetUnit.team == BattleTeam.Enemy
            ? lockedEnemyOutlineColor
            : lockedAllyOutlineColor;
        lockedTargetUnit.SetLockOutline(outlineColor, LockedTargetOutlineWidth, true);
    }

    private void ClearSelectionOutlines()
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null)
            {
                unit.ClearLockOutline();
            }
        }
    }

    private void FocusCameraOnActiveUnit()
    {
        if (battleCameraController == null || activeUnit == null)
        {
            return;
        }

        battleCameraController.SnapToTarget(activeUnit.transform);
        if (activeUnit.team == BattleTeam.Enemy)
        {
            battleCameraController.StartFollowing(activeUnit.transform, snapImmediately: false);
            return;
        }

        battleCameraController.StopFollowing();
    }

    private void FocusCameraOnUnit(BattleUnit unit)
    {
        if (battleCameraController == null || unit == null)
        {
            return;
        }

        battleCameraController.SnapToTarget(unit.transform);
        if (unit.team == BattleTeam.Enemy)
        {
            battleCameraController.StartFollowing(unit.transform, snapImmediately: false);
            return;
        }

        battleCameraController.StopFollowing();
    }

    private void StopCameraFollow()
    {
        if (battleCameraController == null)
        {
            return;
        }

        battleCameraController.StopFollowing();
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
        EvaluateExplorationMode();
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

        bool shouldShow = activeUnit != null &&
            activeUnit.IsAlive &&
            activeUnit.isPlayerControlled &&
            activeUnit.team == BattleTeam.Player;

        activeUnitIdText.enabled = shouldShow;

        if (!shouldShow)
        {
            activeUnitIdText.text = string.Empty;
            return;
        }

        activeUnitIdText.text = string.IsNullOrWhiteSpace(activeUnit.characterId) ? activeUnit.unitName : activeUnit.characterId;
    }

    private void RefreshTimeline()
    {
        List<List<BattleUnit>> timelineRounds = BuildTimelineRounds();
        timelineService?.Refresh(sceneBindings, timelineRounds, currentRoundIndex, absoluteRoundIndex, this);
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

        targetPanelService?.NotifyUnitRemoved(unit, RefreshSelectionOutlines);

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
        if (IsExplorationMode)
        {
            RefreshHighlights();
            RefreshActiveUnitUi();
            RefreshTimeline();
            return;
        }

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
        if (IsExplorationMode)
        {
            ToggleExplorationAction(skillId);
            return;
        }

        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        if (activeUnit.IsMoving)
        {
            return;
        }

        ClearLockedTargetUnit();

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

        bool wasSkillModeActive = IsSkillModeActive();
        if (string.Equals(activeSkillId, skillId, System.StringComparison.Ordinal))
        {
            ClearActiveSkillMode();
        }
        else
        {
            CacheSkillModeRotationAnchor(activeUnit, wasSkillModeActive);
            activeSkillId = skillId;
            activeSkill = nextSkill;
            hasSkillHoverPreview = false;
            skillHoverHasAnyVisibleCells = false;
            StartSkillTargetingIntro(activeUnit, activeSkill);
        }

        RefreshHighlights();
    }

    public void ToggleExplorationAction(string actionId)
    {
        if (!IsExplorationMode || activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        if (!string.Equals(actionId, ExplorationIdleSkillId, System.StringComparison.Ordinal) &&
            !string.Equals(actionId, ExplorationMoveSkillId, System.StringComparison.Ordinal))
        {
            return;
        }

        activeExplorationActionId = actionId;
        ClearLockedTargetUnit();
        ClearActiveSkillMode();

        if (string.Equals(actionId, ExplorationIdleSkillId, System.StringComparison.Ordinal))
        {
            StopExplorationMoveAudio();
            string idleStateName = ResolveExplorationIdleStateName();
            if (!string.IsNullOrWhiteSpace(idleStateName))
            {
                BattleAudioUtility.PlayOnce(ResolveExplorationIdleSound(), ResolveExplorationIdleSoundPrefab(), activeUnit, battleCamera);
                activeUnit.PlayAnimationState(idleStateName, ResolveExplorationIdleCompensateMotion());
            }
        }
        else
        {
            string moveStateName = activeUnit.GetMoveAnimationStateName(ResolveExplorationMoveStateName());
            string idleStateName = ResolveExplorationIdleStateName();
            if (!string.IsNullOrWhiteSpace(moveStateName))
            {
                StopExplorationMoveAudio();
                activeUnit.PlayTimedAnimation(moveStateName, 0.05f, idleStateName, ResolveExplorationMoveCompensateMotion());
            }
        }

        RefreshHighlights();
    }

    public bool IsExplorationActionActive(string actionId)
    {
        return IsExplorationMode &&
            !string.IsNullOrWhiteSpace(actionId) &&
            string.Equals(activeExplorationActionId, actionId, System.StringComparison.Ordinal);
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

        if (IsPointerBlockedByUi())
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
            ClearHoveredSkillTarget();
            return;
        }

        if (!skillTargetSelectionReady)
        {
            if (TryGetMouseWorldPoint(out Vector3 introHitPoint))
            {
                UpdateSkillTargetingFacing(introHitPoint);
            }

            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells || skillHoverValid || skillHoverActionPointCost > 0)
            {
                hasSkillHoverPreview = false;
                skillHoverValid = false;
                skillHoverHasAnyVisibleCells = false;
                skillHoverActionPointCost = 0;
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
        UpdateSkillTargetingFacing(hitPoint);
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

        if (UsesContinuousCircularArea(activeSkill))
        {
            if (IsMovementSkillActive())
            {
                Color movePreviewColor = skillHoverValid ? skillPreviewValidColor : skillPreviewInvalidColor;
                movePreviewColor.a = skillHoverValid ? 0.24f : 0.20f;
                grid.HighlightFootprintAt(skillHoverCell, activeUnit.footprintSize, movePreviewColor);
                return;
            }

            Color previewColor = skillHoverValid ? skillPreviewValidColor : skillPreviewInvalidColor;
            previewColor.a = skillHoverValid ? 0.18f : 0.16f;
            grid.HighlightCircleAt(skillHoverCell, GetContinuousAreaRadiusWorld(activeSkill), previewColor);
            grid.HighlightFootprintAt(skillHoverCell, activeUnit.footprintSize, previewColor);
            return;
        }

        if (IsCircularAxisAreaSkill(activeSkill))
        {
            Color previewColor = skillHoverValid ? skillPreviewValidColor : skillPreviewInvalidColor;
            previewColor.a = skillHoverValid ? 0.18f : 0.16f;

            Vector3 origin = grid.GetWorldPosition(activeUnit.currentCell);
            Vector3 direction = ResolveAxisDirectionWorld(activeUnit, skillHoverCell);
            float rangeWorld = GetAxisRangeWorld(activeUnit, activeSkill);
            if (activeSkill.circularAxisAreaType == BattleSkillDatabase.CircularAxisAreaType.Fan)
            {
                grid.HighlightAxisFan(origin, direction, rangeWorld, activeSkill.axisAngle, previewColor);
            }
            else
            {
                grid.HighlightAxisRay(origin, direction, rangeWorld, GetAxisWidthWorld(activeSkill), previewColor);
            }

            return;
        }

        HashSet<Vector2Int> previewCells = CollectVisibleAreaEffectCells(activeUnit, skillHoverCell, activeSkill);
        if (previewCells == null || previewCells.Count == 0)
        {
            return;
        }

        if (skillHoverValid)
        {
            Color previewColor = skillPreviewValidColor;
            previewColor.a = 0.18f;
            grid.HighlightCells(previewCells, previewColor);
            return;
        }

        Color invalidColor = skillPreviewInvalidColor;
        invalidColor.a = 0.16f;
        grid.HighlightPartialCells(previewCells, activeUnit, invalidColor);
    }

    private void ApplyHoveredTargetPreview()
    {
        if (hoveredSkillTargets.Count == 0)
        {
            grid.ClearHoveredFootprint();
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
        HashSet<Vector2Int> hoveredCells = new HashSet<Vector2Int>();
        Color overlayColor = ResolveHoveredTargetFlashColor(hoveredSkillTarget);
        overlayColor.a = Mathf.Lerp(0.18f, overlayColor.a, pulse);

        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit target = hoveredSkillTargets[i];
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            int radius = target.FootprintRadius;
            for (int y = target.currentCell.y - radius; y <= target.currentCell.y + radius; y++)
            {
                for (int x = target.currentCell.x - radius; x <= target.currentCell.x + radius; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (grid.IsInside(cell))
                    {
                        hoveredCells.Add(cell);
                    }
                }
            }
        }

        grid.SetHoveredFootprint(hoveredCells, overlayColor);
    }

    private bool HasAnyVisibleSkillPreviewCells(Vector2Int centerCell)
    {
        if (activeUnit == null || !ShouldShowSkillAreaPreview(activeSkill))
        {
            return false;
        }

        if (UsesContinuousCircularArea(activeSkill) || IsCircularAxisAreaSkill(activeSkill))
        {
            return grid != null && grid.IsInside(centerCell);
        }

        HashSet<Vector2Int> visibleCells = CollectVisibleAreaEffectCells(activeUnit, centerCell, activeSkill);
        return visibleCells != null && visibleCells.Count > 0;
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

        if (IsCircularAxisAreaSkill(skill))
        {
            return true;
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

    private static bool IsCircularAxisAreaSkill(BattleSkillDatabase.SkillEntry skill)
    {
        return skill != null &&
            skill.skillType == BattleSkillDatabase.SkillType.Area &&
            skill.areaCastType == BattleSkillDatabase.AreaCastType.CircularAxis;
    }

    private static bool UsesContinuousCircularArea(BattleSkillDatabase.SkillEntry skill)
    {
        return skill != null &&
            skill.skillType == BattleSkillDatabase.SkillType.Area &&
            !string.Equals(skill.skillId, BattleSkillDatabase.MoveSkillId, System.StringComparison.Ordinal) &&
            !IsCircularAxisAreaSkill(skill);
    }

    private float GetContinuousAreaRadiusWorld(BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || skill == null)
        {
            return 0f;
        }

        return grid.GetAreaRadiusWorld(GetSkillPreviewFootprintSize(skill));
    }

    private bool IsUnitInsideContinuousArea(BattleUnit target, Vector2Int centerCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || target == null || skill == null || !grid.IsInside(centerCell))
        {
            return false;
        }

        Vector3 areaCenter = grid.GetWorldPosition(centerCell);
        Vector3 targetCenter = grid.GetWorldPosition(target.currentCell);
        float maxDistance = GetContinuousAreaRadiusWorld(skill) + grid.GetUnitRadiusWorld(target);
        return Vector3.Distance(areaCenter, targetCenter) <= maxDistance + 0.001f;
    }

    private Vector3 ResolveAxisDirectionWorld(BattleUnit caster, Vector2Int targetCell)
    {
        if (caster == null || grid == null)
        {
            return Vector3.right;
        }

        Vector3 origin = grid.GetWorldPosition(caster.currentCell);
        Vector3 target = grid.GetWorldPosition(targetCell);
        Vector3 direction = target - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction.normalized;
        }

        Vector3 forward = caster.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            return forward.normalized;
        }

        return Vector3.right;
    }

    private float GetAxisRangeWorld(BattleUnit caster, BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || caster == null || skill == null)
        {
            return 0f;
        }

        return grid.GetCastRadiusWorld(caster, GetDisplayedSkillRange(caster, skill));
    }

    private float GetAxisWidthWorld(BattleSkillDatabase.SkillEntry skill)
    {
        return grid == null || skill == null
            ? 0f
            : Mathf.Max(1, skill.axisWidth) * grid.cellSize;
    }

    private bool IsUnitInsideCircularAxisArea(BattleUnit caster, BattleUnit target, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (grid == null || caster == null || target == null || skill == null)
        {
            return false;
        }

        Vector3 origin = grid.GetWorldPosition(caster.currentCell);
        Vector3 direction = ResolveAxisDirectionWorld(caster, targetCell);
        Vector3 unitCenter = grid.GetWorldPosition(target.currentCell);
        float targetRadius = grid.GetUnitRadiusWorld(target);
        float rangeWorld = GetAxisRangeWorld(caster, skill);

        if (skill.circularAxisAreaType == BattleSkillDatabase.CircularAxisAreaType.Fan)
        {
            Vector3 toTarget = unitCenter - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            if (distance > rangeWorld + targetRadius + 0.001f)
            {
                return false;
            }

            float angleToTarget = Vector3.Angle(direction, toTarget);
            float extraAngle = distance <= targetRadius
                ? 180f
                : Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(targetRadius / distance, 0f, 1f));
            return angleToTarget <= (Mathf.Clamp(skill.axisAngle, 1f, 360f) * 0.5f) + extraAngle + 0.001f;
        }

        float halfWidth = GetAxisWidthWorld(skill) * 0.5f;
        Vector3 toTargetOnPlane = unitCenter - origin;
        toTargetOnPlane.y = 0f;
        float forwardDistance = Vector3.Dot(toTargetOnPlane, direction);
        Vector3 right = new Vector3(-direction.z, 0f, direction.x);
        float lateralDistance = Mathf.Abs(Vector3.Dot(toTargetOnPlane, right));
        float centerDistance = toTargetOnPlane.magnitude;

        if (forwardDistance < -targetRadius)
        {
            return false;
        }

        if (centerDistance > rangeWorld + targetRadius + 0.001f)
        {
            return false;
        }

        if (lateralDistance > halfWidth + targetRadius + 0.001f)
        {
            return false;
        }

        if (forwardDistance < 0f)
        {
            return targetRadius >= -forwardDistance;
        }

        float arcStartForward = Mathf.Sqrt(Mathf.Max(0f, (rangeWorld * rangeWorld) - (halfWidth * halfWidth)));
        if (forwardDistance <= arcStartForward)
        {
            return lateralDistance <= halfWidth + targetRadius + 0.001f;
        }

        return centerDistance <= rangeWorld + targetRadius + 0.001f;
    }

    private HashSet<Vector2Int> CollectAreaEffectCells(BattleUnit caster, Vector2Int centerCell, BattleSkillDatabase.SkillEntry skill)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        if (grid == null || skill == null || !grid.IsInside(centerCell))
        {
            return result;
        }

        if (IsCircularAxisAreaSkill(skill))
        {
            return result;
        }

        int footprintSize = GetSkillPreviewFootprintSize(skill);
        if (footprintSize <= 0)
        {
            return result;
        }

        int footprintRadius = Mathf.Max(0, footprintSize / 2);
        for (int y = centerCell.y - footprintRadius; y <= centerCell.y + footprintRadius; y++)
        {
            for (int x = centerCell.x - footprintRadius; x <= centerCell.x + footprintRadius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (grid.IsInside(cell))
                {
                    result.Add(cell);
                }
            }
        }

        return result;
    }

    private HashSet<Vector2Int> CollectVisibleAreaEffectCells(BattleUnit caster, Vector2Int centerCell, BattleSkillDatabase.SkillEntry skill)
    {
        HashSet<Vector2Int> cells = CollectAreaEffectCells(caster, centerCell, skill);
        if (cells.Count == 0)
        {
            return cells;
        }

        HashSet<Vector2Int> visibleCells = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            BattleUnit occupant = grid.GetUnitAt(cell);
            if (occupant != null && occupant != caster)
            {
                continue;
            }

            visibleCells.Add(cell);
        }

        return visibleCells;
    }

    private bool IsSkillModeActive()
    {
        return activeSkill != null && !string.IsNullOrWhiteSpace(activeSkillId);
    }

    private void ClearActiveSkillMode()
    {
        BattleUnit unit = activeUnit;
        bool shouldRestoreRotation =
            hasSkillModeRotationAnchor &&
            skillModeRotationAnchorUnit != null &&
            !IsExplorationMode &&
            !isResolvingSkillExecution;

        activeSkillId = string.Empty;
        activeSkill = null;
        currentSkillTargetingStateName = string.Empty;
        currentSkillTargetingYawOffset = 0f;
        skillTargetSelectionReady = false;
        hasSkillHoverPreview = false;
        skillHoverValid = false;
        skillHoverHasAnyVisibleCells = false;
        skillHoverActionPointCost = 0;
        ClearHoveredSkillTarget();
        HideSkillCostHint();
        if (skillTargetingIntroRoutine != null)
        {
            StopCoroutine(skillTargetingIntroRoutine);
            skillTargetingIntroRoutine = null;
        }

        if (shouldRestoreRotation &&
            skillModeRotationAnchorUnit.IsAlive &&
            !skillModeRotationAnchorUnit.IsMoving)
        {
            skillModeRotationAnchorUnit.transform.rotation = skillModeRotationAnchorRotation;
        }

        ClearSkillModeRotationAnchor();

        if (IsExplorationMode || unit == null || !unit.IsAlive || unit.IsMoving || isResolvingSkillExecution)
        {
            return;
        }

        string idleStateName = unit.GetIdleAnimationStateName(ResolveIdleStateName(unit));
        if (!string.IsNullOrWhiteSpace(idleStateName))
        {
            unit.PlayAnimationState(idleStateName);
        }
    }

    private void CacheSkillModeRotationAnchor(BattleUnit unit, bool wasSkillModeActive)
    {
        if (wasSkillModeActive || unit == null)
        {
            return;
        }

        skillModeRotationAnchorUnit = unit;
        skillModeRotationAnchorRotation = unit.transform.rotation;
        hasSkillModeRotationAnchor = true;
    }

    private void ClearSkillModeRotationAnchor()
    {
        skillModeRotationAnchorUnit = null;
        skillModeRotationAnchorRotation = Quaternion.identity;
        hasSkillModeRotationAnchor = false;
    }

    private void TryUseActiveSkill(BattleUnit unit, Vector2Int clickedCell, BattleUnit target)
    {
        skillExecutionRoutine = skillExecutionService != null
            ? skillExecutionService.尝试使用当前技能(
                this,
                skillExecutionRoutine,
                unit,
                clickedCell,
                target,
                IsSkillModeActive(),
                isResolvingSkillExecution,
                skillTargetSelectionReady,
                activeSkillId,
                activeSkill,
                (caster, cell, clickedTarget, skill) => CanCastSkillAt(caster, cell, clickedTarget, skill, null),
                TryMove,
                GetSkillActionPointCost,
                GetSkillManaCostForExecution,
                SetSkillExecutionResolvingState,
                FaceTowardTargetUnit,
                FaceTowardTargetCell,
                PlaySkillAnimationWithResolveRoutine,
                ResolveTargetSkillInfoAndDamage,
                ResolveAreaSkillInfoAndDamage,
                ClearActiveSkillMode,
                RefreshHighlights,
                RefreshTimeline,
                TryEnterPendingExplorationMode)
            : skillExecutionRoutine;
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

    private int GetSkillActionPointCost(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        return GetSkillActionPointCost(skill);
    }

    private int GetSkillManaCostForExecution(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        return GetSkillManaCost(skill);
    }

    private void SetSkillExecutionResolvingState(bool value)
    {
        isResolvingSkillExecution = value;
        if (!value)
        {
            skillExecutionRoutine = null;
        }
    }

    private void FaceTowardTargetUnit(BattleUnit caster, BattleUnit target)
    {
        if (caster == null || target == null)
        {
            return;
        }

        caster.FaceToward(target.transform.position);
    }

    private void FaceTowardTargetCell(BattleUnit caster, Vector2Int targetCell)
    {
        if (caster == null || grid == null)
        {
            return;
        }

        caster.FaceToward(grid.GetWorldPosition(targetCell));
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

    private void TryEnterPendingExplorationMode()
    {
        if (!pendingExplorationModeEnter || IsExplorationMode || HasLivingEnemies())
        {
            return;
        }

        if (isResolvingSkillExecution || skillExecutionRoutine != null)
        {
            return;
        }

        pendingExplorationModeEnter = false;
        EnterExplorationMode();
    }

    private IEnumerator ExecuteTargetSkillRoutine(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (skillExecutionService == null)
        {
            yield break;
        }

        yield return skillExecutionService.尝试使用当前技能(
            this,
            null,
            caster,
            target != null ? target.currentCell : default,
            target,
            true,
            isResolvingSkillExecution,
            true,
            skill != null ? skill.skillId : string.Empty,
            skill,
            (unit, cell, clickedTarget, activeSkill) => CanCastSkillAt(unit, cell, clickedTarget, activeSkill, null),
            TryMove,
            GetSkillActionPointCost,
            GetSkillManaCostForExecution,
            SetSkillExecutionResolvingState,
            FaceTowardTargetUnit,
            FaceTowardTargetCell,
            PlaySkillAnimationWithResolveRoutine,
            ResolveTargetSkillInfoAndDamage,
            ResolveAreaSkillInfoAndDamage,
            ClearActiveSkillMode,
            RefreshHighlights,
            RefreshTimeline,
            TryEnterPendingExplorationMode);
    }

    private IEnumerator ExecuteAreaSkillRoutine(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (skillExecutionService == null)
        {
            yield break;
        }

        yield return skillExecutionService.尝试使用当前技能(
            this,
            null,
            caster,
            targetCell,
            null,
            true,
            isResolvingSkillExecution,
            true,
            skill != null ? skill.skillId : string.Empty,
            skill,
            (unit, cell, clickedTarget, activeSkill) => CanCastSkillAt(unit, cell, clickedTarget, activeSkill, null),
            TryMove,
            GetSkillActionPointCost,
            GetSkillManaCostForExecution,
            SetSkillExecutionResolvingState,
            FaceTowardTargetUnit,
            FaceTowardTargetCell,
            PlaySkillAnimationWithResolveRoutine,
            ResolveTargetSkillInfoAndDamage,
            ResolveAreaSkillInfoAndDamage,
            ClearActiveSkillMode,
            RefreshHighlights,
            RefreshTimeline,
            TryEnterPendingExplorationMode);
    }

    private void ApplyCombatArtDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        damageResolutionService?.应用战技单体伤害(
            caster,
            target,
            skill,
            battleCamera,
            RollSkillHit,
            CalculateCombatArtDamage,
            ApplyAttachedEffectsToUnit,
            ShowZeroDamagePopup,
            ShowDamagePopup,
            PlayDodgeReaction,
            PlayHitReaction,
            HandleUnitDefeat);
    }

    private void ResolveTargetSkillInfoAndDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        damageResolutionService?.结算单体技能并显示信息(
            caster,
            target,
            skill,
            battleCamera,
            FormatUnitEffectDebugText,
            CalculateSkillHitChance,
            RollSkillHit,
            CalculateSkillDamage,
            ApplyAttachedEffectsToUnit,
            ShowZeroDamagePopup,
            ShowDamagePopup,
            PlayDodgeReaction,
            PlayHitReaction,
            HandleUnitDefeat,
            unit => ResolveBattleInfoUnitName(unit, richText: true),
            ResolveBattleInfoSkillName,
            FormatBattleInfoDamageText,
            BuildUnitDefeatMessage,
            BuildCriticalBattleInfoText,
            WrapBattleInfoColor,
            NeutralInfoColorHex,
            ShowBattleInfoMessage);
    }

    private void ApplyCombatArtAreaDamage(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        damageResolutionService?.应用战技范围伤害(
            caster,
            targetCell,
            skill,
            battleCamera,
            CollectAreaSkillTargets,
            RollSkillHit,
            CalculateCombatArtDamage,
            ApplyAttachedEffectsToUnit,
            ShowZeroDamagePopup,
            ShowDamagePopup,
            PlayDodgeReaction,
            PlayHitReaction,
            HandleUnitDefeat);
    }

    private void ResolveAreaSkillInfoAndDamage(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        damageResolutionService?.结算范围技能并显示信息(
            caster,
            targetCell,
            skill,
            battleCamera,
            CollectAreaSkillTargets,
            FormatUnitEffectDebugText,
            CalculateSkillHitChance,
            RollSkillHit,
            CalculateSkillDamage,
            ApplyAttachedEffectsToUnit,
            ShowZeroDamagePopup,
            ShowDamagePopup,
            PlayDodgeReaction,
            PlayHitReaction,
            HandleUnitDefeat,
            unit => ResolveBattleInfoUnitName(unit, richText: true),
            ResolveBattleInfoSkillName,
            FormatBattleInfoDamageText,
            BuildUnitDefeatMessage,
            BuildAreaCriticalBattleInfoText,
            WrapBattleInfoColor,
            NeutralInfoColorHex,
            ShowBattleInfoMessage);
    }

    private CombatDamageResult CalculateSkillDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        return skillCoreResolutionService != null
            ? skillCoreResolutionService.计算技能伤害(caster, target, skill)
            : null;
    }

    private CombatDamageResult CalculateCombatArtDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        return skillCoreResolutionService != null
            ? skillCoreResolutionService.计算战技伤害(caster, target, skill)
            : null;
    }

    private CombatDamageResult CalculateSpellDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        return skillCoreResolutionService != null
            ? skillCoreResolutionService.计算法术伤害(caster, target, skill)
            : null;
    }

    private static float ApplyResistance(float damage, BattleUnit caster, BattleUnit target, DamageAttributeType attributeType)
    {
        return 战斗技能基础结算服务.应用抗性(damage, caster, target, attributeType);
    }

    private void ShowDamagePopup(BattleUnit target, CombatDamageResult damageResult)
    {
        if (target == null || damageResult == null)
        {
            return;
        }

        List<BattleDamageNumberPopup.DamageSegment> segments = BuildDamageSegments(damageResult);
        if (damageResult.isCritical)
        {
            string criticalDamageText = BuildPopupDamageText(segments, damageResult.appliedDamage);
            if (!string.IsNullOrWhiteSpace(criticalDamageText))
            {
                BattleDamageNumberPopup.ShowConfiguredText(
                    target,
                    "<color=#FFD700>暴击</color>\n" + criticalDamageText,
                    BattleDamageNumberPopup.ConfiguredPopupKind.Damage,
                    Color.white,
                    battleCamera);
                return;
            }
        }

        if (segments.Count > 0)
        {
            BattleDamageNumberPopup.ShowSegments(target, segments, battleCamera);
            return;
        }

        if (damageResult.appliedDamage > 0)
        {
            BattleDamageNumberPopup.Show(target, damageResult.appliedDamage, battleCamera);
        }
    }

    private void ShowZeroDamagePopup(BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (target == null || skill == null || skill.noDamage)
        {
            return;
        }

        BattleDamageNumberPopup.ShowConfiguredText(
            target,
            "0",
            BattleDamageNumberPopup.ConfiguredPopupKind.Damage,
            physicalDamageColor,
            battleCamera);
    }

    private List<BattleDamageNumberPopup.DamageSegment> BuildDamageSegments(CombatDamageResult damageResult)
    {
        List<BattleDamageNumberPopup.DamageSegment> segments = new List<BattleDamageNumberPopup.DamageSegment>();
        if (damageResult == null)
        {
            return segments;
        }

        List<DamageDisplayAllocation> allocations = BuildDamageDisplayAllocations(damageResult);
        for (int i = 0; i < allocations.Count; i++)
        {
            DamageDisplayAllocation allocation = allocations[i];
            if (allocation.displayAmount <= 0)
            {
                continue;
            }

            segments.Add(new BattleDamageNumberPopup.DamageSegment
            {
                text = allocation.displayAmount.ToString(),
                color = ResolveDamageColor(allocation.attributeType)
            });
        }

        return segments;
    }

    private string BuildPopupDamageText(IList<BattleDamageNumberPopup.DamageSegment> segments, int appliedDamage)
    {
        if (segments != null && segments.Count > 0)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < segments.Count; i++)
            {
                BattleDamageNumberPopup.DamageSegment segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment.text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("<color=#FFFFFF>+</color>");
                }

                builder.Append("<color=#");
                builder.Append(ColorUtility.ToHtmlStringRGB(segment.color));
                builder.Append(">");
                builder.Append(segment.text);
                builder.Append("</color>");
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }
        }

        return appliedDamage > 0 ? appliedDamage.ToString() : string.Empty;
    }

    private static List<DamageDisplayAllocation> BuildDamageDisplayAllocations(CombatDamageResult damageResult)
    {
        List<DamageDisplayAllocation> allocations = new List<DamageDisplayAllocation>();
        if (damageResult == null)
        {
            return allocations;
        }

        int totalAssigned = 0;
        for (int i = 0; i < damageResult.components.Count; i++)
        {
            DamageComponent component = damageResult.components[i];
            if (component.amount <= 0f)
            {
                continue;
            }

            int baseAmount = Mathf.FloorToInt(component.amount);
            allocations.Add(new DamageDisplayAllocation
            {
                attributeType = component.attributeType,
                amount = component.amount,
                displayAmount = baseAmount,
                fractionalPart = component.amount - baseAmount
            });
            totalAssigned += baseAmount;
        }

        int delta = Mathf.Max(0, damageResult.appliedDamage) - totalAssigned;
        if (delta <= 0 || allocations.Count == 0)
        {
            return allocations;
        }

        allocations.Sort(CompareDamageDisplayAllocationForIncrement);
        for (int i = 0; i < delta; i++)
        {
            int index = i % allocations.Count;
            DamageDisplayAllocation allocation = allocations[index];
            allocation.displayAmount += 1;
            allocations[index] = allocation;
        }

        allocations.Sort(CompareDamageDisplayAllocationForOutput);
        return allocations;
    }

    private Color ResolveDamageColor(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return fireDamageColor;
            case DamageAttributeType.Corruption:
                return corruptionDamageColor;
            case DamageAttributeType.Cold:
                return coldDamageColor;
            default:
                return physicalDamageColor;
        }
    }

    private static int CompareDamageDisplayAllocationForIncrement(DamageDisplayAllocation left, DamageDisplayAllocation right)
    {
        int fractionalComparison = right.fractionalPart.CompareTo(left.fractionalPart);
        if (fractionalComparison != 0)
        {
            return fractionalComparison;
        }

        int priorityComparison = GetDamageAttributeDisplayPriority(left.attributeType).CompareTo(GetDamageAttributeDisplayPriority(right.attributeType));
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return right.amount.CompareTo(left.amount);
    }

    private static int CompareDamageDisplayAllocationForOutput(DamageDisplayAllocation left, DamageDisplayAllocation right)
    {
        return GetDamageAttributeDisplayPriority(left.attributeType).CompareTo(GetDamageAttributeDisplayPriority(right.attributeType));
    }

    private static int GetDamageAttributeDisplayPriority(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Physical:
                return 0;
            case DamageAttributeType.Fire:
                return 1;
            case DamageAttributeType.Corruption:
                return 2;
            case DamageAttributeType.Cold:
                return 3;
            default:
                return int.MaxValue;
        }
    }

    private static string FormatDamageValue(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }

        return value.ToString("0.#");
    }

    private static string FormatBattleInfoDamageText(CombatDamageResult damageResult)
    {
        if (damageResult == null || damageResult.components.Count == 0)
        {
            return $"{WrapBattleInfoColor("0", PhysicalInfoColorHex)}{WrapBattleInfoColor("点伤害", NeutralInfoColorHex)}";
        }

        List<string> parts = new List<string>();
        List<DamageDisplayAllocation> allocations = BuildDamageDisplayAllocations(damageResult);
        for (int i = 0; i < allocations.Count; i++)
        {
            DamageDisplayAllocation allocation = allocations[i];
            if (allocation.displayAmount <= 0)
            {
                continue;
            }

            string attributeColorHex = GetDamageAttributeColorHex(allocation.attributeType);
            string amountText = WrapBattleInfoColor(allocation.displayAmount.ToString(), attributeColorHex);
            string attributeText = WrapBattleInfoColor(GetDamageAttributeDisplayName(allocation.attributeType), attributeColorHex);
            string suffixText = WrapBattleInfoColor("伤害", attributeColorHex);
            parts.Add($"{amountText}{attributeText}{suffixText}");
        }

        if (parts.Count == 0)
        {
            return $"{WrapBattleInfoColor("0", PhysicalInfoColorHex)}{WrapBattleInfoColor("点伤害", NeutralInfoColorHex)}";
        }

        return string.Join(WrapBattleInfoColor("和", NeutralInfoColorHex), parts);
    }

    private static string GetDamageAttributeDisplayName(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return "火焰";
            case DamageAttributeType.Corruption:
                return "腐蚀";
            case DamageAttributeType.Cold:
                return "寒冷";
            default:
                return "物理";
        }
    }

    private static string GetDamageAttributeColorHex(DamageAttributeType attributeType)
    {
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return FireInfoColorHex;
            case DamageAttributeType.Corruption:
                return CorruptionInfoColorHex;
            case DamageAttributeType.Cold:
                return ColdInfoColorHex;
            default:
                return PhysicalInfoColorHex;
        }
    }

    private static string BuildUnitDefeatMessage(BattleUnit unit)
    {
        if (unit == null || unit.IsAlive)
        {
            return string.Empty;
        }

        return WrapBattleInfoColor($"，{ResolveBattleInfoUnitName(unit, richText: true)}死亡", NeutralInfoColorHex);
    }

    private static string BuildCriticalBattleInfoText(bool isCritical)
    {
        return isCritical
            ? WrapBattleInfoColor("触发了暴击，", PhysicalInfoColorHex)
            : string.Empty;
    }

    private static string BuildAreaCriticalBattleInfoText(bool isCritical)
    {
        return isCritical
            ? WrapBattleInfoColor("触发暴击，", PhysicalInfoColorHex)
            : string.Empty;
    }

    private bool RollSkillHit(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        return skillCoreResolutionService != null &&
            skillCoreResolutionService.判定技能命中(caster, target, skill);
    }

    private int CalculateSkillHitChance(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        return skillCoreResolutionService != null
            ? skillCoreResolutionService.计算技能命中率(caster, target, skill)
            : MinHitChancePercent;
    }

    private void ApplyAttachedEffectsToUnit(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (skillCoreResolutionService == null)
        {
            return;
        }

        skillCoreResolutionService.应用附加效果到单位(
            caster,
            target,
            skill,
            battleCamera,
            physicalDamageColor);
    }

    private static string FormatUnitEffectDebugText(BattleUnit unit)
    {
        return 战斗技能基础结算服务.格式化单位效果调试文本(unit);
    }

    private static string ResolveEffectDebugName(EffectDatabase.EffectEntry effectEntry)
    {
        return 战斗技能基础结算服务.解析效果调试名称(effectEntry);
    }

    private void ApplyAttachedEffectsToUnits(BattleUnit caster, List<BattleUnit> targets, BattleSkillDatabase.SkillEntry skill)
    {
        if (skillCoreResolutionService == null)
        {
            return;
        }

        skillCoreResolutionService.应用附加效果到单位列表(
            caster,
            targets,
            skill,
            battleCamera,
            physicalDamageColor);
    }

    private void ProcessEffectTurnsForTurnOwner(BattleUnit turnOwner)
    {
        if (effectTurnResolutionService == null)
        {
            return;
        }

        effectTurnResolutionService.处理回合持有效果(
            turnOwner,
            units,
            battleCamera,
            FindUnitByInstanceId,
            ResolveEffectDamagePopupColor);
    }

    private Color ResolveEffectDamagePopupColor(EffectDatabase.StatModifier.HealthDamageType damageType)
    {
        switch (damageType)
        {
            case EffectDatabase.StatModifier.HealthDamageType.Fire:
                return fireDamageColor;
            case EffectDatabase.StatModifier.HealthDamageType.Corruption:
                return corruptionDamageColor;
            case EffectDatabase.StatModifier.HealthDamageType.Cold:
                return coldDamageColor;
            default:
                return physicalDamageColor;
        }
    }

    private void PlayHitReaction(BattleUnit target)
    {
        BattleAudioUtility.PlayOnce(ResolveHitReactionSound(target), ResolveHitReactionSoundPrefab(target), target, battleCamera);
        PlayReactionAnimation(target, target != null ? target.GetHitReactionAnimationStateName(ResolveHitReactionStateName(target)) : ResolveHitReactionStateName(target));
    }

    private void PlayDodgeReaction(BattleUnit target)
    {
        BattleAudioUtility.PlayOnce(ResolveDodgeSound(target), ResolveDodgeSoundPrefab(target), target, battleCamera);
        PlayReactionAnimation(target, target != null ? target.GetDodgeAnimationStateName(ResolveDodgeStateName(target)) : ResolveDodgeStateName(target));
    }

    private void PlayReactionAnimation(BattleUnit target, string stateName)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        target.PlayAnimationStateForCurrentClipDuration(
            stateName,
            target.GetIdleAnimationStateName(ResolveIdleStateName(target)),
            ShouldCompensateGlobalMotionForState(target, stateName));
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
                (target == null || target == caster) &&
                movementPath.Count - 1 <= GetDisplayedSkillRange(caster, skill);
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return target != null &&
                IsValidSkillTarget(caster, target, skill) &&
                grid.IsUnitWithinCircularRange(caster, target, GetDisplayedSkillRange(caster, skill));
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Area)
        {
            return grid.IsCellWithinCircularRange(caster, targetCell, GetDisplayedSkillRange(caster, skill));
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

        if (IsPointerBlockedByUi())
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
        List<BattleUnit> validTargets = CollectHoveredSkillTargets(activeUnit, hoveredCell, target, activeSkill);
        if (validTargets.Count == 0)
        {
            ClearHoveredSkillTarget();
            return;
        }

        ApplyHoveredSkillTargets(validTargets, target);
    }

    private List<BattleUnit> CollectHoveredSkillTargets(
        BattleUnit caster,
        Vector2Int hoveredCell,
        BattleUnit directTarget,
        BattleSkillDatabase.SkillEntry skill)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        if (caster == null || skill == null)
        {
            return result;
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Target)
        {
            if (IsHoveredSkillTargetValid(caster, directTarget, skill, hoveredCell))
            {
                result.Add(directTarget);
            }

            return result;
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return result;
        }

        if (!grid.IsCellWithinCircularRange(caster, hoveredCell, GetDisplayedSkillRange(caster, skill)))
        {
            return result;
        }
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

            if (UsesContinuousCircularArea(skill))
            {
                if (!IsUnitInsideContinuousArea(unit, hoveredCell, skill))
                {
                    continue;
                }
            }
            else if (IsCircularAxisAreaSkill(skill))
            {
                if (!IsUnitInsideCircularAxisArea(caster, unit, hoveredCell, skill))
                {
                    continue;
                }
            }
            else
            {
                HashSet<Vector2Int> affectedCells = CollectAreaEffectCells(caster, hoveredCell, skill);
                if (!IsUnitInsideAreaCells(unit, affectedCells))
                {
                    continue;
                }
            }

            result.Add(unit);
        }

        return result;
    }

    private void ApplyHoveredSkillTargets(List<BattleUnit> nextTargets, BattleUnit directTarget)
    {
        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit previous = hoveredSkillTargets[i];
            if (previous != null && !nextTargets.Contains(previous))
            {
                previous.ClearTint();
            }
        }

        hoveredSkillTargets.Clear();
        hoveredSkillTargets.AddRange(nextTargets);
        hoveredSkillTarget = directTarget != null && nextTargets.Contains(directTarget)
            ? directTarget
            : nextTargets[0];
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
            return grid.IsUnitWithinCircularRange(caster, target, GetDisplayedSkillRange(caster, skill));
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return false;
        }

        if (!grid.IsCellWithinCircularRange(caster, hoveredCell, GetDisplayedSkillRange(caster, skill)))
        {
            return false;
        }

        if (UsesContinuousCircularArea(skill))
        {
            return IsUnitInsideContinuousArea(target, hoveredCell, skill);
        }

        if (IsCircularAxisAreaSkill(skill))
        {
            return IsUnitInsideCircularAxisArea(caster, target, hoveredCell, skill);
        }

        HashSet<Vector2Int> affectedCells = CollectAreaEffectCells(caster, hoveredCell, skill);
        return IsUnitInsideAreaCells(target, affectedCells);
    }

    private static bool IsUnitInsideAreaCells(BattleUnit target, HashSet<Vector2Int> areaCells)
    {
        if (target == null || areaCells == null || areaCells.Count == 0)
        {
            return false;
        }

        int unitRadius = target.FootprintRadius;
        for (int y = target.currentCell.y - unitRadius; y <= target.currentCell.y + unitRadius; y++)
        {
            for (int x = target.currentCell.x - unitRadius; x <= target.currentCell.x + unitRadius; x++)
            {
                if (areaCells.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateHoveredTargetFlash()
    {
        if (hoveredSkillTargets.Count == 0)
        {
            grid.ClearHoveredFootprint();
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
        ApplyHoveredTargetPreview();
        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit target = hoveredSkillTargets[i];
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            Color targetFlashColor = ResolveHoveredTargetFlashColor(target);
            target.ApplyTint(targetFlashColor, Mathf.Lerp(0.2f, 0.75f, pulse));
        }
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
        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit target = hoveredSkillTargets[i];
            if (target != null)
            {
                target.ClearTint();
            }
        }

        hoveredSkillTargets.Clear();
        grid.ClearHoveredFootprint();
        hoveredSkillTarget = null;
    }

    private static bool IsPointerBlockedByUi()
    {
        return BattleInputService.IsPointerBlockedByUi();
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
        List<BattleUnit> result = new List<BattleUnit>();
        for (int roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
        {
            List<BattleUnit> round = rounds[roundIndex];
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

    private static Transform FindDescendantByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, childName, System.StringComparison.Ordinal))
            {
                return child;
            }

            Transform nested = FindDescendantByName(child, childName);
            if (nested != null)
            {
                return nested;
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

    private List<战斗敌方回合服务.技能选项> BuildEnemySkillChoices(BattleUnit caster)
    {
        return enemyDecisionService != null
            ? enemyDecisionService.构建技能选项(caster, NormalAttackSkillId, ResolveSkill)
            : new List<战斗敌方回合服务.技能选项>();
    }

    private bool TryFindEnemySkillAction(BattleUnit caster, List<战斗敌方回合服务.技能选项> skillChoices, out 战斗敌方回合服务.技能动作 action)
    {
        战斗敌方回合服务.技能动作? resolvedAction = enemyDecisionService != null
            ? enemyDecisionService.尝试查找技能动作(
                caster,
                units,
                skillChoices,
                grid,
                CanEnemyUseSkill,
                IsValidEnemySkillTarget,
                CanEnemyCastSkillAt)
            : null;
        action = resolvedAction ?? default;
        return resolvedAction.HasValue;
    }

    private 战斗敌方回合服务.技能动作? TryFindEnemySkillActionNullable(BattleUnit caster, List<战斗敌方回合服务.技能选项> skillChoices)
    {
        战斗敌方回合服务.技能动作 action;
        return TryFindEnemySkillAction(caster, skillChoices, out action)
            ? action
            : (战斗敌方回合服务.技能动作?)null;
    }

    private bool TryMoveEnemyTowardSkillRange(BattleUnit caster, List<战斗敌方回合服务.技能选项> skillChoices, out float moveDuration)
    {
        float? resolvedDuration = enemyDecisionService != null
            ? enemyDecisionService.尝试向技能范围移动(
                caster,
                units,
                skillChoices,
                grid,
                ResolveSkill,
                GetMoveMaxRange,
                GetSkillManaCost,
                GetMoveActionPointCost,
                GetSkillActionPointCost,
                CanEnemyUseSkill,
                IsValidEnemySkillTarget,
                CanEnemyCastSkillFromCell,
                FindClosestLivingOpponent,
                FindBestStepToward,
                GetSkillRange,
                TryMoveEnemyToCellNullable,
                FaceTowardTargetUnit)
            : null;
        moveDuration = resolvedDuration ?? 0f;
        return resolvedDuration.HasValue;
    }

    private float? TryMoveEnemyTowardSkillRangeNullable(BattleUnit caster, List<战斗敌方回合服务.技能选项> skillChoices)
    {
        float moveDuration;
        return TryMoveEnemyTowardSkillRange(caster, skillChoices, out moveDuration)
            ? moveDuration
            : (float?)null;
    }

    private float? TryMoveEnemyToCellNullable(BattleUnit unit, Vector2Int destination)
    {
        float moveDuration;
        return TryMoveEnemyToCell(unit, destination, out moveDuration)
            ? moveDuration
            : (float?)null;
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

        grid.ResetHighlights();
        moveDuration = grid.MoveUnit(unit, destination);
        if (moveSkill != null)
        {
            StartCoroutine(PlayTrackedSkillAudioRoutine(unit, moveSkill, moveDuration));
            unit.PlayTimedAnimation(
                unit.GetMoveAnimationStateName(ResolveSkillActionStateName(moveSkill, unit)),
                moveDuration,
                unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)),
                ResolveSkillCompensateActionMotion(moveSkill, unit));
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        return true;
    }

    private bool CanEnemyUseSkill(BattleUnit caster, 战斗敌方回合服务.技能选项 choice)
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
            return IsUnitWithinRangeFromCell(caster, castCell, target, skillRange) &&
                IsValidSkillTarget(caster, target, skill);
        }

        if (skill.skillType == BattleSkillDatabase.SkillType.Area)
        {
            Vector3 castPosition = grid.GetWorldPosition(castCell);
            Vector3 targetPosition = grid.GetWorldPosition(target.currentCell);
            float maxDistance = grid.GetCastRadiusWorld(caster, skillRange) + grid.GetUnitRadiusWorld(target);
            return Vector3.Distance(castPosition, targetPosition) <= maxDistance + 0.001f;
        }

        return false;
    }

    private bool IsUnitWithinRangeFromCell(BattleUnit caster, Vector2Int castCell, BattleUnit target, int range)
    {
        if (caster == null || target == null || grid == null)
        {
            return false;
        }

        int casterRadius = caster.FootprintRadius;
        int targetRadius = target.FootprintRadius;
        int clampedRange = Mathf.Max(0, range);

        for (int casterY = castCell.y - casterRadius; casterY <= castCell.y + casterRadius; casterY++)
        {
            for (int casterX = castCell.x - casterRadius; casterX <= castCell.x + casterRadius; casterX++)
            {
                Vector2Int casterFootprintCell = new Vector2Int(casterX, casterY);
                if (!grid.IsInside(casterFootprintCell))
                {
                    continue;
                }

                for (int targetY = target.currentCell.y - targetRadius; targetY <= target.currentCell.y + targetRadius; targetY++)
                {
                    for (int targetX = target.currentCell.x - targetRadius; targetX <= target.currentCell.x + targetRadius; targetX++)
                    {
                        Vector2Int targetFootprintCell = new Vector2Int(targetX, targetY);
                        if (!grid.IsInside(targetFootprintCell))
                        {
                            continue;
                        }

                        if (grid.ManhattanDistance(casterFootprintCell, targetFootprintCell) <= clampedRange)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private IEnumerator ExecuteEnemySkillAction(BattleUnit caster, 战斗敌方回合服务.技能动作 action)
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
        if (caster == null || skill == null)
        {
            yield break;
        }

        string actionStateName = ResolveSkillActionStateName(skill, caster);
        if (string.IsNullOrWhiteSpace(actionStateName))
        {
            yield return PlayTrackedSkillAudioRoutine(caster, skill, 0f);
            yield break;
        }

        Animator animator = caster.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            yield break;
        }

        caster.SetAnimationPositionCompensation(ResolveSkillCompensateActionMotion(skill, caster));

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        Quaternion previousRotation = caster.transform.rotation;
        float actionYawOffset = ResolveSkillActionYawOffset(skill, caster);
        if (Mathf.Abs(actionYawOffset) > 0.01f)
        {
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, actionYawOffset, 0f);
        }

        animator.Play(actionStateName, 0, 0f);

        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float clipDuration = currentState.length;
        StartCoroutine(PlayTrackedSkillAudioRoutine(caster, skill, clipDuration));
        if (clipDuration > 0.01f)
        {
            yield return new WaitForSeconds(clipDuration);
        }

        string idleStateName = caster.GetIdleAnimationStateName(ResolveIdleStateName(caster));
        Quaternion postSkillIdleRotation = ResolvePostSkillIdleRotation(
            caster.transform.rotation,
            actionYawOffset,
            ResolveSkillPostUseYawOffset(skill, caster));
        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            animator.Play(idleStateName, 0, 0f);
            caster.transform.rotation = postSkillIdleRotation;
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            animator.Play(previousStateHash, 0, 0f);
            caster.transform.rotation = postSkillIdleRotation;
        }

        caster.SetAnimationPositionCompensation(false);
    }

    private IEnumerator PlaySkillAnimationWithResolveRoutine(BattleUnit caster, BattleSkillDatabase.SkillEntry skill, System.Action resolveAction)
    {
        if (caster == null)
        {
            yield break;
        }

        if (skill == null)
        {
            resolveAction?.Invoke();
            yield break;
        }

        string actionStateName = ResolveSkillActionStateName(skill, caster);
        if (string.IsNullOrWhiteSpace(actionStateName))
        {
            yield return PlayTrackedSkillAudioRoutine(caster, skill, 0f);
            resolveAction?.Invoke();
            yield break;
        }

        Animator animator = caster.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            yield return PlayTrackedSkillAudioRoutine(caster, skill, 0f);
            resolveAction?.Invoke();
            yield break;
        }

        caster.SetAnimationPositionCompensation(ResolveSkillCompensateActionMotion(skill, caster));

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        Quaternion previousRotation = caster.transform.rotation;
        float actionYawOffset = ResolveSkillActionYawOffset(skill, caster);
        if (Mathf.Abs(actionYawOffset) > 0.01f)
        {
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, actionYawOffset, 0f);
        }

        animator.Play(actionStateName, 0, 0f);
        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float clipDuration = currentState.length;
        StartCoroutine(PlayTrackedSkillAudioRoutine(caster, skill, clipDuration));
        int totalFrames = ResolveAnimationStateTotalFrames(animator, actionStateName, clipDuration);
        float resolveDelay = ResolveSkillResolveDelaySeconds(skill, totalFrames, clipDuration);

        if (resolveDelay > 0.01f)
        {
            yield return new WaitForSeconds(resolveDelay);
        }

        TriggerSkillHitFeel(skill);
        resolveAction?.Invoke();

        float remainingDuration = Mathf.Max(0f, clipDuration - Mathf.Max(0f, resolveDelay));
        if (remainingDuration > 0.01f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        string idleStateName = caster.GetIdleAnimationStateName(ResolveIdleStateName(caster));
        Quaternion postSkillIdleRotation = ResolvePostSkillIdleRotation(
            caster.transform.rotation,
            actionYawOffset,
            ResolveSkillPostUseYawOffset(skill, caster));
        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            animator.Play(idleStateName, 0, 0f);
            caster.transform.rotation = postSkillIdleRotation;
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            animator.Play(previousStateHash, 0, 0f);
            caster.transform.rotation = postSkillIdleRotation;
        }

        caster.SetAnimationPositionCompensation(false);
    }

    private IEnumerator PlayTrackedSkillAudioRoutine(BattleUnit unit, BattleSkillDatabase.SkillEntry skill, float totalDuration)
    {
        if (skill == null)
        {
            yield break;
        }

        float delay = ResolveSkillSoundDelaySeconds(skill, unit, totalDuration);
        if (delay > 0.01f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (totalDuration <= 0.01f)
        {
            BattleAudioUtility.PlayOnce(ResolveSkillActionSound(skill, unit), ResolveSkillActionSoundPrefab(skill, unit), unit, battleCamera);
            yield break;
        }

        BattleAudioUtility.PlaybackHandle handle = BattleAudioUtility.StartTracked(
            ResolveSkillActionSound(skill, unit),
            ResolveSkillActionSoundPrefab(skill, unit),
            unit,
            battleCamera);
        if (handle == null)
        {
            yield break;
        }

        float remainingDuration = Mathf.Max(0f, totalDuration - delay);
        if (remainingDuration > 0.01f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }
        else
        {
            yield return null;
        }

        StopTrackedAudio(handle);
    }

    private static void StopTrackedAudio(BattleAudioUtility.PlaybackHandle handle)
    {
        if (handle == null)
        {
            return;
        }

        handle.Stop();
    }

    private static int ResolveAnimationStateTotalFrames(Animator animator, string stateName, float clipDuration)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(stateName) && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                {
                    continue;
                }

                if (string.Equals(clip.name, stateName, System.StringComparison.Ordinal))
                {
                    return Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                }
            }
        }

        if (clipDuration > 0.01f)
        {
            return Mathf.Max(1, Mathf.RoundToInt(clipDuration * 60f));
        }

        return 0;
    }

    private static float ResolveSkillResolveDelaySeconds(BattleSkillDatabase.SkillEntry skill, int totalFrames, float clipDuration)
    {
        if (clipDuration <= 0.01f)
        {
            return 0f;
        }

        if (skill == null || skill.resolveFrame <= 0 || totalFrames <= 0)
        {
            return clipDuration;
        }

        int clampedFrame = Mathf.Clamp(skill.resolveFrame, 1, totalFrames);
        return clipDuration * ((float)clampedFrame / totalFrames);
    }

    private static float ResolveSkillSoundDelaySeconds(BattleSkillDatabase.SkillEntry skill, BattleUnit unit, float clipDuration)
    {
        if (skill == null)
        {
            return 0f;
        }

        int soundDelayFrame = ResolveSkillSoundDelayFrame(skill, unit);
        if (soundDelayFrame <= 0)
        {
            return 0f;
        }

        if (clipDuration <= 0.01f)
        {
            return soundDelayFrame / 60f;
        }

        int totalFrames = Mathf.Max(1, Mathf.RoundToInt(clipDuration * 60f));
        int clampedFrame = Mathf.Clamp(soundDelayFrame, 0, totalFrames);
        return clipDuration * ((float)clampedFrame / totalFrames);
    }

    private static string ResolveSkillActionStateName(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.actionStateName;
        }

        return string.Empty;
    }

    private static Quaternion ResolvePostSkillIdleRotation(
        Quaternion currentRotation,
        float actionYawOffset,
        float postUseYawOffset)
    {
        float idleYawOffset = ResolveIdleYawOffset();
        return currentRotation * Quaternion.Euler(0f, idleYawOffset - actionYawOffset + postUseYawOffset, 0f);
    }

    private static string ResolveSkillTargetSelectionStateName(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.targetSelectionStateName;
        }

        return string.Empty;
    }

    private static string ResolveSkillRaiseHandStateName(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.raiseHandStateName;
        }

        return string.Empty;
    }

    private static float ResolveSkillTargetSelectionYawOffset(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.targetSelectionYawOffset;
        }

        return 0f;
    }

    private static float ResolveSkillRaiseHandYawOffset(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.raiseHandYawOffset;
        }

        return 0f;
    }

    private static float ResolveSkillActionYawOffset(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.actionYawOffset;
        }

        return 0f;
    }

    private static float ResolveSkillPostUseYawOffset(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.postUseYawOffset;
        }

        return 0f;
    }

    private static AudioClip ResolveSkillActionSound(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.actionSound;
        }

        return null;
    }

    private static GameObject ResolveSkillActionSoundPrefab(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.actionSoundPrefab;
        }

        return null;
    }

    private static int ResolveSkillSoundDelayFrame(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return Mathf.Max(0, overrideEntry.soundDelayFrame);
        }

        return 0;
    }

    private static bool ResolveSkillCompensateActionMotion(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride overrideEntry = ResolveSkillActionOverride(skill, unit);
        if (overrideEntry != null)
        {
            return overrideEntry.compensateActionMotion;
        }

        return false;
    }

    private static BattleSkillDatabase.SkillEntry.WeaponScopedActionOverride ResolveSkillActionOverride(BattleSkillDatabase.SkillEntry skill, BattleUnit unit)
    {
        if (skill == null || unit == null)
        {
            return null;
        }

        ItemDatabase.WeaponCategory weaponCategory = InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(unit.characterId);
        bool isMoveSkill = string.Equals(skill.skillId, BattleSkillDatabase.MoveSkillId, System.StringComparison.Ordinal);
        if (!isMoveSkill && !skill.HasRequiredWeaponCategory(weaponCategory))
        {
            return null;
        }

        return skill.FindEnabledWeaponActionOverride(weaponCategory);
    }

    private void StartSkillTargetingIntro(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        if (skillTargetingIntroRoutine != null)
        {
            StopCoroutine(skillTargetingIntroRoutine);
            skillTargetingIntroRoutine = null;
        }

        skillTargetingIntroRoutine = StartCoroutine(PlaySkillTargetingIntroRoutine(unit, skill));
    }

    private IEnumerator PlaySkillTargetingIntroRoutine(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        currentSkillTargetingStateName = string.Empty;
        currentSkillTargetingYawOffset = 0f;
        skillTargetSelectionReady = false;
        if (unit == null || skill == null || !unit.IsAlive)
        {
            skillTargetingIntroRoutine = null;
            yield break;
        }

        string raiseHandStateName = ResolveSkillRaiseHandStateName(skill, unit);
        if (!string.IsNullOrWhiteSpace(raiseHandStateName))
        {
            unit.SetAnimationPositionCompensation(false);
            unit.PlayAnimationState(raiseHandStateName);
            currentSkillTargetingStateName = raiseHandStateName;
            currentSkillTargetingYawOffset = ResolveSkillRaiseHandYawOffset(skill, unit);

            if (TryGetMouseWorldPoint(out Vector3 raiseHandHitPoint))
            {
                UpdateSkillTargetingFacing(raiseHandHitPoint);
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled)
            {
                yield return null;
                float duration = animator.GetCurrentAnimatorStateInfo(0).length;
                if (duration > 0.01f)
                {
                    yield return new WaitForSeconds(duration);
                }
            }
        }

        if (activeUnit != unit || activeSkill != skill || !string.Equals(activeSkillId, skill.skillId, System.StringComparison.Ordinal))
        {
            skillTargetingIntroRoutine = null;
            yield break;
        }

        string targetSelectionStateName = ResolveSkillTargetSelectionStateName(skill, unit);
        if (string.IsNullOrWhiteSpace(targetSelectionStateName))
        {
            string idleStateName = unit.GetIdleAnimationStateName(ResolveIdleStateName(unit));
            if (!string.IsNullOrWhiteSpace(idleStateName))
            {
                unit.PlayAnimationState(idleStateName);
            }

            skillTargetSelectionReady = true;
            skillTargetingIntroRoutine = null;
            yield break;
        }

        unit.SetAnimationPositionCompensation(false);
        unit.PlayAnimationState(targetSelectionStateName);
        currentSkillTargetingStateName = targetSelectionStateName;
        currentSkillTargetingYawOffset = ResolveSkillTargetSelectionYawOffset(skill, unit);
        skillTargetSelectionReady = true;
        skillTargetingIntroRoutine = null;
    }

    private void UpdateSkillTargetingFacing(Vector3 worldPosition)
    {
        if (!IsSkillModeActive() || activeUnit == null || !activeUnit.IsAlive || activeUnit.IsMoving)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(currentSkillTargetingStateName))
        {
            return;
        }

        activeUnit.FaceToward(worldPosition);
        if (Mathf.Abs(currentSkillTargetingYawOffset) > 0.01f)
        {
            activeUnit.transform.rotation = activeUnit.transform.rotation * Quaternion.Euler(0f, currentSkillTargetingYawOffset, 0f);
        }
    }

    private bool TryGetMouseWorldPoint(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (grid == null || battleCamera == null)
        {
            return false;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        if (!clickPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        hitPoint = ray.GetPoint(enter);
        return true;
    }

    private void TriggerSkillHitFeel(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null || !skill.enableHitFeel)
        {
            return;
        }

        if (!hitFeelActive)
        {
            hitFeelRestoreTimeScale = Time.timeScale;
            hitFeelRestoreFixedDeltaTime = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : DefaultFixedDeltaTime;
        }

        if (hitFeelRoutine != null)
        {
            StopCoroutine(hitFeelRoutine);
        }

        hitFeelRoutine = StartCoroutine(PlayHitFeelRoutine());
    }

    private IEnumerator PlayHitFeelRoutine()
    {
        hitFeelActive = true;
        Time.timeScale = HitFeelTimeScale;
        Time.fixedDeltaTime = hitFeelRestoreFixedDeltaTime * HitFeelTimeScale;

        yield return new WaitForSecondsRealtime(HitFeelDurationSeconds);

        if (Mathf.Approximately(Time.timeScale, HitFeelTimeScale))
        {
            Time.timeScale = hitFeelRestoreTimeScale;
        }

        if (Mathf.Approximately(Time.fixedDeltaTime, hitFeelRestoreFixedDeltaTime * HitFeelTimeScale))
        {
            Time.fixedDeltaTime = hitFeelRestoreFixedDeltaTime;
        }

        hitFeelActive = false;
        hitFeelRoutine = null;
    }

    private void RestoreGlobalTimeScale()
    {
        if (hitFeelRoutine != null)
        {
            StopCoroutine(hitFeelRoutine);
            hitFeelRoutine = null;
        }

        if (Mathf.Approximately(Time.timeScale, HitFeelTimeScale))
        {
            Time.timeScale = hitFeelRestoreTimeScale;
        }

        if (Mathf.Approximately(Time.fixedDeltaTime, hitFeelRestoreFixedDeltaTime * HitFeelTimeScale))
        {
            Time.fixedDeltaTime = hitFeelRestoreFixedDeltaTime;
        }

        hitFeelActive = false;
    }

    private void ShowBattleInfoMessage(string message)
    {
        BattleInfoWindowPresenter presenter = battleInfoWindowPresenter != null
            ? battleInfoWindowPresenter
            : BattleInfoWindowPresenter.FindInActiveScene();
        battleInfoWindowPresenter = presenter;
        if (presenter == null)
        {
            return;
        }

        presenter.ShowMessage(message);
    }

    private static string FormatTargetSkillMessage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        string casterName = ResolveBattleInfoUnitName(caster, richText: true);
        string targetName = ResolveBattleInfoUnitName(target, richText: true);
        string skillName = ResolveBattleInfoSkillName(skill);
        return $"{casterName}对{targetName}使用了{skillName}";
    }

    private string FormatAreaSkillMessage(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        string casterName = ResolveBattleInfoUnitName(caster, richText: true);
        string skillName = ResolveBattleInfoSkillName(skill);
        List<string> targetNames = CollectAreaSkillTargetNames(caster, targetCell, skill);
        if (targetNames.Count == 1)
        {
            return $"{casterName}对{targetNames[0]}使用了{skillName}";
        }

        if (targetNames.Count > 1)
        {
            return $"{casterName}对{string.Join("、", targetNames)}使用了{skillName}";
        }

        return $"{casterName}在{targetCell}使用了{skillName}";
    }

    private List<string> CollectAreaSkillTargetNames(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        List<string> names = new List<string>();
        if (caster == null || skill == null)
        {
            return names;
        }

        List<BattleUnit> targets = CollectAreaSkillTargets(caster, targetCell, skill);
        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            names.Add(ResolveBattleInfoUnitName(unit, richText: true));
        }

        return names;
    }

    private List<BattleUnit> CollectAreaSkillTargets(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        List<BattleUnit> targets = new List<BattleUnit>();
        if (caster == null || skill == null)
        {
            return targets;
        }

        bool usesContinuousCircularArea = UsesContinuousCircularArea(skill);
        bool usesCircularAxisArea = IsCircularAxisAreaSkill(skill);
        HashSet<Vector2Int> affectedCells = null;
        if (!usesContinuousCircularArea && !usesCircularAxisArea)
        {
            affectedCells = CollectAreaEffectCells(caster, targetCell, skill);
        }

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

            if (usesContinuousCircularArea)
            {
                if (!IsUnitInsideContinuousArea(unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (usesCircularAxisArea)
            {
                if (!IsUnitInsideCircularAxisArea(caster, unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (!IsUnitInsideAreaCells(unit, affectedCells))
            {
                continue;
            }

            targets.Add(unit);
        }

        return targets;
    }

    private static string ResolveBattleInfoUnitName(BattleUnit unit, bool richText = false)
    {
        return BattleInfoTextUtility.ResolveBattleInfoUnitName(unit, richText);
    }

    private static string ResolveBattleInfoSkillName(BattleSkillDatabase.SkillEntry skill)
    {
        return BattleInfoTextUtility.ResolveBattleInfoSkillName(skill);
    }

    private static string WrapBattleInfoColor(string content, string colorHex)
    {
        return BattleInfoTextUtility.WrapBattleInfoColor(content, colorHex);
    }

    private static string ResolveIdleStateName(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveIdleStateName(ResolveAnimationCharacterId(unit));
    }

    private static string ResolveEnterBattleStateName(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleStateName(ResolveAnimationCharacterId(unit));
    }

    private static AudioClip ResolveEnterBattleSound(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleSound(ResolveAnimationCharacterId(unit));
    }

    private static GameObject ResolveEnterBattleSoundPrefab(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleSoundPrefab(ResolveAnimationCharacterId(unit));
    }

    private static bool ResolveEnterBattleCompensateMotion(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleCompensateMotion(ResolveAnimationCharacterId(unit));
    }

    private static string ResolveExplorationIdleStateName()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationIdleStateName();
    }

    private static AudioClip ResolveExplorationIdleSound()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationIdleSound();
    }

    private static GameObject ResolveExplorationIdleSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationIdleSoundPrefab();
    }

    private static bool ResolveExplorationIdleCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationIdleCompensateMotion();
    }

    private static string ResolveExplorationMoveStateName()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationMoveStateName();
    }

    private static AudioClip ResolveExplorationMoveSound()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationMoveSound();
    }

    private static GameObject ResolveExplorationMoveSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationMoveSoundPrefab();
    }

    private static bool ResolveExplorationMoveCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveExplorationMoveCompensateMotion();
    }

    private static string ResolveExitBattleStateName(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleStateName(ResolveAnimationCharacterId(unit));
    }

    private static AudioClip ResolveExitBattleSound(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleSound(ResolveAnimationCharacterId(unit));
    }

    private static GameObject ResolveExitBattleSoundPrefab(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleSoundPrefab(ResolveAnimationCharacterId(unit));
    }

    private static bool ResolveExitBattleCompensateMotion(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleCompensateMotion(ResolveAnimationCharacterId(unit));
    }

    private void PlayExitBattleAnimations()
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlive || unit.team != BattleTeam.Player)
            {
                continue;
            }

            string stateName = ResolveExitBattleStateName(unit);
            if (string.IsNullOrWhiteSpace(stateName))
            {
                continue;
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            {
                continue;
            }

            BattleAudioUtility.PlayOnce(ResolveExitBattleSound(unit), ResolveExitBattleSoundPrefab(unit), unit, battleCamera);
            unit.PlayAnimationStateForCurrentClipDuration(
                stateName,
                ResolveExplorationIdleStateName(),
                ResolveExitBattleCompensateMotion(unit));
        }
    }

    private void PlayExplorationIdleAnimation()
    {
        if (activeUnit == null || !activeUnit.IsAlive)
        {
            return;
        }

        string idleStateName = ResolveExplorationIdleStateName();
        if (string.IsNullOrWhiteSpace(idleStateName))
        {
            return;
        }

        StopExplorationMoveAudio();
        activeUnit.PlayAnimationState(idleStateName, ResolveExplorationIdleCompensateMotion());
    }

    private void PlayExplorationIdleAnimations()
    {
        string idleStateName = ResolveExplorationIdleStateName();
        if (string.IsNullOrWhiteSpace(idleStateName))
        {
            return;
        }

        StopExplorationMoveAudio();

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlive || unit.team != BattleTeam.Player)
            {
                continue;
            }

            unit.PlayAnimationState(idleStateName, ResolveExplorationIdleCompensateMotion());
        }
    }

    private IEnumerator PlayEnterBattleAnimations()
    {
        if (units == null || units.Count == 0)
        {
            enterBattleAnimationInProgress = false;
            yield break;
        }

        bool playedAny = false;
        BattleUnit leadUnit = pendingEnterBattleLeadUnit;
        Animator leadAnimator = null;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            string enterBattleStateName = unit.GetEnterBattleAnimationStateName(ResolveEnterBattleStateName(unit));
            if (string.IsNullOrWhiteSpace(enterBattleStateName))
            {
                continue;
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            {
                continue;
            }

            BattleAudioUtility.PlayOnce(ResolveEnterBattleSound(unit), ResolveEnterBattleSoundPrefab(unit), unit, battleCamera);
            unit.PlayAnimationStateForCurrentClipDuration(
                enterBattleStateName,
                unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)),
                ResolveEnterBattleCompensateMotion(unit));
            if (unit == leadUnit)
            {
                leadAnimator = animator;
            }

            playedAny = true;
        }

        if (!playedAny)
        {
            enterBattleAnimationInProgress = false;
            BattleUnit currentLeadUnit = pendingEnterBattleLeadUnit;
            pendingEnterBattleLeadUnit = null;
            if (beginTurnAfterEnterBattle && currentLeadUnit != null)
            {
                beginTurnAfterEnterBattle = false;
                BeginCurrentTurn();
            }
            yield break;
        }

        yield return null;

        float leadDuration = 0f;
        if (leadAnimator != null && leadAnimator.runtimeAnimatorController != null && leadAnimator.isActiveAndEnabled)
        {
            leadDuration = leadAnimator.GetCurrentAnimatorStateInfo(0).length;
        }

        if (leadDuration > 0.01f)
        {
            yield return new WaitForSeconds(leadDuration);
        }

        enterBattleAnimationInProgress = false;
        BattleUnit pendingLeadUnit = pendingEnterBattleLeadUnit;
        pendingEnterBattleLeadUnit = null;
        if (beginTurnAfterEnterBattle && pendingLeadUnit != null)
        {
            beginTurnAfterEnterBattle = false;
            BeginCurrentTurn();
        }
    }

    private BattleUnit GetNextLivingRoundUnit()
    {
        if (currentRoundOrder == null || currentRoundOrder.Count == 0)
        {
            return null;
        }

        for (int i = Mathf.Max(0, currentRoundIndex); i < currentRoundOrder.Count; i++)
        {
            BattleUnit unit = currentRoundOrder[i];
            if (unit != null && unit.IsAlive)
            {
                return unit;
            }
        }

        return null;
    }


    private void PlayExplorationMoveAudio(BattleUnit unit, float duration)
    {
        StopExplorationMoveAudio();
        currentExplorationMoveAudioHandle = BattleAudioUtility.StartTracked(
            ResolveExplorationMoveSound(),
            ResolveExplorationMoveSoundPrefab(),
            unit,
            battleCamera);

        if (currentExplorationMoveAudioHandle == null || !currentExplorationMoveAudioHandle.IsValid)
        {
            return;
        }

        if (explorationMoveAudioStopRoutine != null)
        {
            StopCoroutine(explorationMoveAudioStopRoutine);
        }

        explorationMoveAudioStopRoutine = StartCoroutine(StopExplorationMoveAudioAfterDelay(duration));
    }

    private IEnumerator StopExplorationMoveAudioAfterDelay(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
        StopExplorationMoveAudio();
        explorationMoveAudioStopRoutine = null;
    }

    private void StopExplorationMoveAudio()
    {
        if (explorationMoveAudioStopRoutine != null)
        {
            StopCoroutine(explorationMoveAudioStopRoutine);
            explorationMoveAudioStopRoutine = null;
        }

        StopTrackedAudio(currentExplorationMoveAudioHandle);
        currentExplorationMoveAudioHandle = null;
    }

    private static float ResolveIdleYawOffset()
    {
        return BattleAnimationSettingsResolver.ResolveIdleYawOffset();
    }

    private static string ResolveHitReactionStateName(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionStateName(ResolveAnimationCharacterId(unit));
    }

    private static bool ResolveHitReactionCompensateMotion(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionCompensateMotion(ResolveAnimationCharacterId(unit));
    }

    private static AudioClip ResolveHitReactionSound(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionSound(ResolveAnimationCharacterId(unit));
    }

    private static GameObject ResolveHitReactionSoundPrefab(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionSoundPrefab(ResolveAnimationCharacterId(unit));
    }

    private static string ResolveDodgeStateName(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveDodgeStateName(ResolveAnimationCharacterId(unit));
    }

    private static bool ResolveDodgeCompensateMotion(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveDodgeCompensateMotion(ResolveAnimationCharacterId(unit));
    }

    private static AudioClip ResolveDodgeSound(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveDodgeSound(ResolveAnimationCharacterId(unit));
    }

    private static GameObject ResolveDodgeSoundPrefab(BattleUnit unit = null)
    {
        return BattleAnimationSettingsResolver.ResolveDodgeSoundPrefab(ResolveAnimationCharacterId(unit));
    }

    private void RefreshModeMusic()
    {
        BattleMusicSettings settings = BattleMusicSettings.LoadDefault();
        if (settings == null)
        {
            StopModeMusic();
            return;
        }

        AudioClip nextClip = IsExplorationMode ? settings.explorationMusic : settings.combatMusic;
        if (nextClip == null)
        {
            StopModeMusic();
            return;
        }

        EnsureModeMusicSource();
        modeMusicSource.volume = Mathf.Clamp01(settings.volume);
        modeMusicSource.loop = true;
        if (modeMusicSource.clip == nextClip && modeMusicSource.isPlaying)
        {
            return;
        }

        modeMusicSource.clip = nextClip;
        modeMusicSource.Play();
    }

    private void EnsureModeMusicSource()
    {
        if (modeMusicSource != null)
        {
            return;
        }

        modeMusicSource = GetComponent<AudioSource>();
        if (modeMusicSource == null)
        {
            modeMusicSource = gameObject.AddComponent<AudioSource>();
        }

        modeMusicSource.playOnAwake = false;
        modeMusicSource.spatialBlend = 0f;
        modeMusicSource.loop = true;
    }

    private void StopModeMusic()
    {
        if (modeMusicSource == null)
        {
            return;
        }

        modeMusicSource.Stop();
        modeMusicSource.clip = null;
    }

    private static bool ShouldCompensateGlobalMotionForState(BattleUnit unit, string stateName)
    {
        return BattleAnimationSettingsResolver.ShouldCompensateGlobalMotionForState(stateName, ResolveAnimationCharacterId(unit));
    }

    private static string ResolveAnimationCharacterId(BattleUnit unit)
    {
        return unit != null ? unit.characterId : string.Empty;
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


