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

    private const string TimelineAnchorPath = "Canvas/\u4E0A\u65B9\u680F\u4F4D/\u56DE\u5408\u65F6\u95F4\u8F74";
    private const string RuntimeWeaponModelName = "__RuntimeWeaponModel";

    private const string EndTurnButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u7ED3\u675F\u56DE\u5408\u6309\u94AE";
    private const string MoveSkillButtonPath = "Canvas/\u4E0B\u65B9\u680F\u4F4D/\u79FB\u52A8\u6309\u94AE";
    private const string TargetPanelPath = "Canvas/\u4e0a\u65b9\u680f\u4f4d/\u76ee\u6807";
    private const string TargetHealthPanelPath = "Canvas/\u4e0a\u65b9\u680f\u4f4d/\u76ee\u6807/\u751f\u547d\u503c";
    private const string TargetHealthFillPath = "Canvas/\u4e0a\u65b9\u680f\u4f4d/\u76ee\u6807/\u751f\u547d\u503c/\u751f\u547d\u503c";
    private const string TargetHealthTextPath = "Canvas/\u4e0a\u65b9\u680f\u4f4d/\u76ee\u6807/\u751f\u547d\u503c\u6570\u5b57";
    private const string TargetNameTextPath = "Canvas/\u4e0a\u65b9\u680f\u4f4d/\u76ee\u6807/\u540d\u5b57/\u76ee\u6807\u540d\u5b57text";
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
    private RectTransform targetPanelRect;
    private Image targetHealthFillImage;
    private TMP_Text targetHealthText;
    private TMP_Text targetNameText;
    private BattleInfoWindowPresenter battleInfoWindowPresenter;
    private Transform timelineAnchor;
    private Button endTurnButton;
    private Button moveSkillButton;
    private BattleSceneBindings sceneBindings;
    private BattleSkillDatabase skillDatabase;
    private TurnTimelineButtonDatabase timelineDatabase;
    private Coroutine timelineAnimationRoutine;
    private Coroutine skillExecutionRoutine;
    private Coroutine hitFeelRoutine;
    private BattleUnit timelineLeadUnit;
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
    private BattleUnit hoveredTargetUnit;
    private BattleUnit lockedTargetUnit;
    private bool isResolvingSkillExecution;
    private string lastTargetUiSignature = "<unset>";
    private RectSnapshot targetHealthFillBaseRect;
    private bool cachedTargetBaseRect;
    private bool combatArtAimAnimationActive;
    private bool hasLastCombatArtAimHoverCell;
    private Vector2Int lastCombatArtAimHoverCell;
    private float combatArtAimAnimationActiveUntilTime;
    private const float MinCombatArtAimAnimationDurationSeconds = 60f / 60f;
    private string currentCombatArtAimStateName = string.Empty;
    private BattleAudioUtility.PlaybackHandle currentCombatArtAimAudioHandle;
    private BattleAudioUtility.PlaybackHandle currentExplorationMoveAudioHandle;
    private BattleFlowMode currentMode = BattleFlowMode.Combat;
    private string activeExplorationActionId = ExplorationMoveSkillId;
    private AudioSource modeMusicSource;
    private Coroutine explorationMoveAudioStopRoutine;
    private bool pendingExplorationModeEnter;

    private sealed class EnemySkillChoice
    {
        public string skillId = string.Empty;
        public int weight;
        public int order;
        public BattleSkillDatabase.SkillEntry skill;
    }

    private enum DamageAttributeType
    {
        Physical,
        Fire,
        Corruption,
        Cold
    }

    private struct EnemySkillAction
    {
        public EnemySkillChoice choice;
        public BattleUnit targetUnit;
        public Vector2Int targetCell;
    }

    private struct DamageComponent
    {
        public DamageAttributeType attributeType;
        public float amount;
    }

    private sealed class CombatDamageResult
    {
        public readonly List<DamageComponent> components = new List<DamageComponent>();
        public bool isCritical;
        public float totalDamage;
        public int appliedDamage;
    }

    private struct RectSnapshot
    {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector2 pivot;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector3 localScale;
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

    public bool IsExplorationMode
    {
        get { return currentMode == BattleFlowMode.Exploration; }
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
        currentMode = BattleFlowMode.Exploration;
        activeExplorationActionId = ExplorationMoveSkillId;
        pendingExplorationModeEnter = false;
        timelineLeadUnit = null;
        lastTimelineSlots.Clear();

        foreach (BattleUnit unit in battleUnits)
        {
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        if (HasLivingEnemies())
        {
            EnterCombatMode(playEnterAnimation: true);
            StartNewRound();
            BeginCurrentTurn();
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
            HandlePlayerInput();
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

        RefreshTargetPanelUi();
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

        if (activeUnit == null || !activeUnit.IsAlive || activeUnit.IsMoving)
        {
            return;
        }

        HandleExplorationInput();
    }

    private void HandlePlayerInput()
    {
        if (IsSkillModeActive() && Input.GetMouseButtonDown(1))
        {
            ClearActiveSkillMode();
            RefreshHighlights();
            return;
        }

        if (!IsSkillModeActive() && Input.GetMouseButtonDown(1))
        {
            ClearLockedTargetUnit();
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (IsPointerBlockedByUi())
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

        if (target != null && target.IsAlive)
        {
            SetLockedTargetUnit(target);
        }

    }

    private void HandleExplorationInput()
    {
        if (!string.Equals(activeExplorationActionId, ExplorationMoveSkillId, System.StringComparison.Ordinal))
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerBlockedByUi() || grid == null || battleCamera == null)
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
        if (target != null && target != activeUnit)
        {
            return;
        }

        TryMoveFreely(activeUnit, clickedCell);
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

                RefreshHighlights();

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
            StartCoroutine(PlayTrackedSkillAudioRoutine(unit, moveSkill, moveDuration));
            unit.PlayTimedAnimation(
                unit.GetMoveAnimationStateName(moveSkill.actionStateName),
                moveDuration,
                unit.GetIdleAnimationStateName(ResolveIdleStateName()));
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        ClearActiveSkillMode();
        RefreshHighlights();
    }

    private void TryMoveFreely(BattleUnit unit, Vector2Int destination)
    {
        if (unit == null || grid == null || unit.IsMoving || destination == unit.currentCell)
        {
            return;
        }

        List<Vector2Int> path = grid.FindPath(unit, destination);
        if (path == null || path.Count <= 1)
        {
            return;
        }

        float originalMoveSpeed = unit.moveSpeed;
        unit.moveSpeed = Mathf.Max(0.01f, originalMoveSpeed * 0.5f);
        float moveDuration = grid.MoveUnit(unit, destination);
        unit.moveSpeed = originalMoveSpeed;
        string idleStateName = ResolveExplorationIdleStateName();
        PlayExplorationMoveAudio(unit, moveDuration);
        unit.PlayTimedAnimation(
            unit.GetMoveAnimationStateName(ResolveExplorationMoveStateName()),
            moveDuration,
            idleStateName,
            ResolveExplorationMoveCompensateMotion());
        RefreshHighlights();
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
        hoveredTargetUnit = null;
        activeUnit = FindExplorationPlayerUnit();
        if (playExitAnimation && switchedFromCombat)
        {
            PlayExitBattleAnimations();
        }
        else
        {
            PlayExplorationIdleAnimation();
        }

        if (activeUnit != null)
        {
            FocusCameraOnActiveUnit();
        }

        SetCombatUiVisible(false);
        RefreshModeMusic();
        RefreshSelectionOutlines();
        RefreshHighlights();
        RefreshActiveUnitUi();
        RefreshTimeline();
        lastTargetUiSignature = "<exploration>";
        ApplyTargetPanelUi(null, string.Empty, 0, 0, false);
    }

    private void EnterCombatMode(bool playEnterAnimation)
    {
        bool switchedFromExploration = currentMode == BattleFlowMode.Exploration;
        currentMode = BattleFlowMode.Combat;
        waitingForEnemyAction = false;
        activeExplorationActionId = ExplorationMoveSkillId;
        SetCombatUiVisible(true);
        RefreshModeMusic();
        RefreshSelectionOutlines();
        RefreshHighlights();
        RefreshActiveUnitUi();
        RefreshTimeline();
        if (playEnterAnimation && switchedFromExploration)
        {
            StartCoroutine(PlayEnterBattleAnimations());
        }
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
        if (timelineAnchor != null)
        {
            timelineAnchor.gameObject.SetActive(visible);
        }

        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(visible);
        }

        if (moveSkillButton != null)
        {
            moveSkillButton.gameObject.SetActive(visible);
        }

        if (sceneBindings != null && sceneBindings.actionPointPanel != null)
        {
            sceneBindings.actionPointPanel.gameObject.SetActive(visible);
        }

        if (sceneBindings != null)
        {
            for (int i = 0; i < sceneBindings.skillPageButtons.Count; i++)
            {
                Button button = sceneBindings.skillPageButtons[i];
                if (button != null)
                {
                    button.gameObject.SetActive(visible);
                }
            }

            for (int i = 0; i < sceneBindings.skillPageIcons.Count; i++)
            {
                Image image = sceneBindings.skillPageIcons[i];
                if (image != null)
                {
                    image.gameObject.SetActive(visible);
                }
            }

            if (sceneBindings.spellCurrentPageText != null)
            {
                sceneBindings.spellCurrentPageText.gameObject.SetActive(visible);
            }

            if (sceneBindings.spellTotalPageText != null)
            {
                sceneBindings.spellTotalPageText.gameObject.SetActive(visible);
            }
        }
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
        StopCombatArtAimAnimation(force: true);
        hasLastCombatArtAimHoverCell = false;
        combatArtAimAnimationActiveUntilTime = 0f;
        currentCombatArtAimStateName = string.Empty;
        CleanupDeadUnits();
        while (currentRoundIndex >= 0 && currentRoundIndex < currentRoundOrder.Count)
        {
            BattleUnit candidate = currentRoundOrder[currentRoundIndex];
            if (candidate != null && candidate.IsAlive)
            {
                activeUnit = candidate;
                activeUnit.BeginTurn();
                FocusCameraOnActiveUnit();
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

            BattleUnit persistentTarget = ResolvePersistentTargetUnit();
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

    private void RefreshTargetPanelUi()
    {
        if (IsExplorationMode)
        {
            lastTargetUiSignature = "<exploration>";
            ApplyTargetPanelUi(null, string.Empty, 0, 0, false);
            return;
        }

        CacheTargetPanelReferences();

        BattleUnit directHoveredUnit = ResolveHoveredPanelUnit();
        if (hoveredTargetUnit != directHoveredUnit)
        {
            hoveredTargetUnit = directHoveredUnit;
            RefreshSelectionOutlines();
        }

        BattleUnit targetUnit = directHoveredUnit != null ? directHoveredUnit : ResolvePersistentTargetUnit();
        string targetId = targetUnit != null && targetUnit.IsAlive
            ? (string.IsNullOrWhiteSpace(targetUnit.characterId) ? targetUnit.unitName : targetUnit.characterId)
            : string.Empty;
        int currentHealth = targetUnit != null && targetUnit.IsAlive ? Mathf.Max(0, targetUnit.currentHealth) : 0;
        int maxHealth = targetUnit != null && targetUnit.IsAlive ? Mathf.Max(0, targetUnit.maxHealth) : 0;
        string signature = string.Concat(targetId, "|", currentHealth, "/", maxHealth);
        if (string.Equals(signature, lastTargetUiSignature, System.StringComparison.Ordinal))
        {
            return;
        }

        lastTargetUiSignature = signature;
        ApplyTargetPanelUi(targetUnit, targetId, currentHealth, maxHealth, targetUnit != null && targetUnit.IsAlive);
    }

    private void CacheTargetPanelReferences()
    {
        if (targetPanelRect == null)
        {
            targetPanelRect = FindTransformByPath(TargetPanelPath) as RectTransform;
        }

        if (targetHealthFillImage == null)
        {
            targetHealthFillImage = FindImageByPath(TargetHealthFillPath);
            if (targetHealthFillImage != null)
            {
                targetHealthFillImage.color = targetHealthBarColor;
                CacheTargetFillBaseRect();
            }
        }

        if (targetHealthText == null)
        {
            targetHealthText = FindTextByPath(TargetHealthTextPath) ?? FindTargetHealthTextFallback();
        }

        if (targetNameText == null)
        {
            targetNameText = FindTextByPath(TargetNameTextPath);
        }
    }

    private void CacheTargetFillBaseRect()
    {
        if (cachedTargetBaseRect || targetHealthFillImage == null || targetHealthFillImage.rectTransform == null)
        {
            return;
        }

        targetHealthFillBaseRect = CaptureSnapshot(targetHealthFillImage.rectTransform);
        cachedTargetBaseRect = true;
    }

    private BattleUnit ResolveHoveredPanelUnit()
    {
        if (hoveredSkillTarget != null && hoveredSkillTarget.IsAlive)
        {
            return hoveredSkillTarget;
        }

        if (IsPointerBlockedByUi())
        {
            return null;
        }

        if (grid == null || battleCamera == null)
        {
            return null;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!clickPlane.Raycast(ray, out enter))
        {
            return null;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            return null;
        }

        BattleUnit unit = grid.GetUnitAt(hoveredCell);
        if (unit != null && unit.IsAlive)
        {
            return unit;
        }

        return null;
    }

    private BattleUnit ResolvePersistentTargetUnit()
    {
        if (lockedTargetUnit != null && lockedTargetUnit.IsAlive)
        {
            return lockedTargetUnit;
        }

        if (lockedTargetUnit != null && !lockedTargetUnit.IsAlive)
        {
            lockedTargetUnit = null;
            RefreshSelectionOutlines();
        }

        return null;
    }

    private void SetLockedTargetUnit(BattleUnit unit)
    {
        if (IsSkillModeActive())
        {
            return;
        }

        lockedTargetUnit = unit != null && unit.IsAlive ? unit : null;
        RefreshSelectionOutlines();
        lastTargetUiSignature = "<unset>";
        RefreshHighlights();
    }

    private void ClearLockedTargetUnit()
    {
        if (lockedTargetUnit == null)
        {
            return;
        }

        lockedTargetUnit = null;
        RefreshSelectionOutlines();
        lastTargetUiSignature = "<unset>";
        RefreshHighlights();
    }

    private void RefreshSelectionOutlines()
    {
        ClearSelectionOutlines();

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

    private TMP_Text FindTargetHealthTextFallback()
    {
        Transform panel = FindTransformByPath(TargetHealthPanelPath);
        if (panel == null)
        {
            return null;
        }

        TMP_Text existing = panel.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject textObject = new GameObject("生命值数字", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(240f, 40f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28f;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private void ApplyTargetPanelUi(BattleUnit targetUnit, string targetId, int currentHealth, int maxHealth, bool visible)
    {
        if (targetPanelRect != null)
        {
            targetPanelRect.gameObject.SetActive(visible);
        }

        if (targetNameText != null)
        {
            targetNameText.text = visible ? targetId : string.Empty;
            targetNameText.color = visible ? ResolveTargetNameColor(targetUnit) : targetNameSelfColor;
        }

        if (targetHealthText != null)
        {
            targetHealthText.text = visible ? currentHealth + "/" + maxHealth : string.Empty;
        }

        ApplyTargetHealthBar(currentHealth, maxHealth, visible);
    }

    private Color ResolveTargetNameColor(BattleUnit targetUnit)
    {
        if (targetUnit == null || activeUnit == null)
        {
            return targetNameSelfColor;
        }

        if (targetUnit == activeUnit)
        {
            return targetNameSelfColor;
        }

        return targetUnit.team == activeUnit.team
            ? targetNameAllyColor
            : targetNameEnemyColor;
    }

    private void ApplyTargetHealthBar(int current, int max, bool visible)
    {
        if (targetHealthFillImage == null)
        {
            return;
        }

        CacheTargetFillBaseRect();
        targetHealthFillImage.enabled = visible && max > 0;
        targetHealthFillImage.color = targetHealthBarColor;

        RectTransform rectTransform = targetHealthFillImage.rectTransform;
        if (rectTransform == null)
        {
            return;
        }

        ApplySnapshot(rectTransform, targetHealthFillBaseRect);
        if (!visible || max <= 0)
        {
            rectTransform.sizeDelta = new Vector2(0f, targetHealthFillBaseRect.sizeDelta.y);
            return;
        }

        float ratio = Mathf.Clamp01((float)current / max);
        float fullWidth = Mathf.Max(0f, targetHealthFillBaseRect.sizeDelta.x);
        float targetWidth = fullWidth * ratio;
        float widthDelta = fullWidth - targetWidth;
        rectTransform.sizeDelta = new Vector2(targetWidth, targetHealthFillBaseRect.sizeDelta.y);

        Vector2 anchoredPosition = targetHealthFillBaseRect.anchoredPosition;
        anchoredPosition.x -= widthDelta * 0.5f;
        rectTransform.anchoredPosition = anchoredPosition;
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
        if (IsExplorationMode)
        {
            ClearTimelineInstances();
            timelineLeadUnit = null;
            return;
        }

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

        if (lockedTargetUnit == unit)
        {
            lockedTargetUnit.ClearLockOutline();
            lockedTargetUnit = null;
            lastTargetUiSignature = "<unset>";
        }

        if (hoveredTargetUnit == unit)
        {
            hoveredTargetUnit = null;
            lastTargetUiSignature = "<unset>";
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
            hasLastCombatArtAimHoverCell = false;
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

        if (ShouldUseCombatArtAimPreview(activeSkill))
        {
            UpdateCombatArtAimFacing(hitPoint, hoveredCell);
        }
        else
        {
            StopCombatArtAimAnimation(force: true);
            hasLastCombatArtAimHoverCell = false;
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
        activeSkillId = string.Empty;
        activeSkill = null;
        hasSkillHoverPreview = false;
        skillHoverValid = false;
        skillHoverHasAnyVisibleCells = false;
        skillHoverActionPointCost = 0;
        ClearHoveredSkillTarget();
        HideSkillCostHint();
        StopCombatArtAimAnimation(force: true);
        hasLastCombatArtAimHoverCell = false;
        combatArtAimAnimationActiveUntilTime = 0f;
        currentCombatArtAimStateName = string.Empty;
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
        yield return PlaySkillAnimationWithResolveRoutine(caster, skill, () => ResolveTargetSkillInfoAndDamage(caster, target, skill));
        ClearActiveSkillMode();
        RefreshHighlights();
        RefreshTimeline();

        Debug.Log("Target skill selected: " + caster.unitName + " -> " + target.unitName + " using " + skill.skillId);
        isResolvingSkillExecution = false;
        skillExecutionRoutine = null;
        TryEnterPendingExplorationMode();
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
        yield return PlaySkillAnimationWithResolveRoutine(caster, skill, () => ResolveAreaSkillInfoAndDamage(caster, targetCell, skill));
        ClearActiveSkillMode();
        RefreshHighlights();
        RefreshTimeline();

        Debug.Log("Area skill selected: " + caster.unitName + " -> " + targetCell + " using " + skill.skillId);
        isResolvingSkillExecution = false;
        skillExecutionRoutine = null;
        TryEnterPendingExplorationMode();
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

        if (!RollSkillHit(caster, target))
        {
            PlayDodgeReaction(target);
            BattleDamageNumberPopup.ShowMiss(target, battleCamera);
            return;
        }

        CombatDamageResult damageResult = CalculateCombatArtDamage(caster, target, skill);
        if (damageResult == null || damageResult.appliedDamage <= 0)
        {
            return;
        }

        PlayHitReaction(target);
        target.ApplyDamage(damageResult.appliedDamage);
        ShowDamagePopup(target, damageResult);
        HandleUnitDefeat(target);
    }

    private void ResolveTargetSkillInfoAndDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        string message = ApplyCombatArtDamageWithMessage(caster, target, skill);
        ShowBattleInfoMessage(message);
    }

    private string ApplyCombatArtDamageWithMessage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return string.Empty;
        }

        string casterName = ResolveBattleInfoUnitName(caster, richText: true);
        string targetName = ResolveBattleInfoUnitName(target, richText: true);
        string skillName = ResolveBattleInfoSkillName(skill);

        if (skill.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            return $"{casterName}对{targetName}使用了{skillName}";
        }

        if (!RollSkillHit(caster, target))
        {
            PlayDodgeReaction(target);
            BattleDamageNumberPopup.ShowMiss(target, battleCamera);
            return $"{casterName}对{targetName}使用了{skillName}，被{targetName}闪避了";
        }

        CombatDamageResult damageResult = CalculateCombatArtDamage(caster, target, skill);
        if (damageResult == null || damageResult.appliedDamage <= 0)
        {
            return $"{casterName}对{targetName}使用了{skillName}";
        }

        PlayHitReaction(target);
        target.ApplyDamage(damageResult.appliedDamage);
        ShowDamagePopup(target, damageResult);
        HandleUnitDefeat(target);
        string criticalText = damageResult.isCritical ? $"{WrapBattleInfoColor("触发了暴击，", PhysicalInfoColorHex)}" : string.Empty;
        string damageText = FormatBattleInfoDamageText(damageResult);
        string deathText = BuildUnitDefeatMessage(target);
        return WrapBattleInfoColor($"{casterName}对{targetName}使用了{skillName}，{criticalText}对{targetName}造成{damageText}{deathText}", NeutralInfoColorHex);
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
                if (!IsUnitInsideContinuousArea(unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (IsCircularAxisAreaSkill(skill))
            {
                if (!IsUnitInsideCircularAxisArea(caster, unit, targetCell, skill))
                {
                    continue;
                }
            }
            else
            {
                HashSet<Vector2Int> affectedCells = CollectAreaEffectCells(caster, targetCell, skill);
                if (!IsUnitInsideAreaCells(unit, affectedCells))
                {
                    continue;
                }
            }

            if (!RollSkillHit(caster, unit))
            {
                PlayDodgeReaction(unit);
                BattleDamageNumberPopup.ShowMiss(unit, battleCamera);
                continue;
            }

            CombatDamageResult damageResult = CalculateCombatArtDamage(caster, unit, skill);
            if (damageResult == null || damageResult.appliedDamage <= 0)
            {
                continue;
            }

            PlayHitReaction(unit);
            unit.ApplyDamage(damageResult.appliedDamage);
            ShowDamagePopup(unit, damageResult);
            HandleUnitDefeat(unit);
        }
    }

    private void ResolveAreaSkillInfoAndDamage(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        string message = ApplyCombatArtAreaDamageWithMessage(caster, targetCell, skill);
        ShowBattleInfoMessage(message);
    }

    private string ApplyCombatArtAreaDamageWithMessage(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || skill == null)
        {
            return string.Empty;
        }

        string casterName = ResolveBattleInfoUnitName(caster, richText: true);
        string skillName = ResolveBattleInfoSkillName(skill);
        if (skill.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            return FormatAreaSkillMessage(caster, targetCell, skill);
        }

        List<string> hitTargets = new List<string>();
        List<string> missedTargets = new List<string>();
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
                if (!IsUnitInsideContinuousArea(unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (IsCircularAxisAreaSkill(skill))
            {
                if (!IsUnitInsideCircularAxisArea(caster, unit, targetCell, skill))
                {
                    continue;
                }
            }
            else
            {
                HashSet<Vector2Int> affectedCells = CollectAreaEffectCells(caster, targetCell, skill);
                if (!IsUnitInsideAreaCells(unit, affectedCells))
                {
                    continue;
                }
            }

            string unitName = ResolveBattleInfoUnitName(unit, richText: true);
            if (!RollSkillHit(caster, unit))
            {
                PlayDodgeReaction(unit);
                BattleDamageNumberPopup.ShowMiss(unit, battleCamera);
                missedTargets.Add(unitName);
                continue;
            }

            CombatDamageResult damageResult = CalculateCombatArtDamage(caster, unit, skill);
            if (damageResult == null || damageResult.appliedDamage <= 0)
            {
                continue;
            }

            PlayHitReaction(unit);
            unit.ApplyDamage(damageResult.appliedDamage);
            ShowDamagePopup(unit, damageResult);
            HandleUnitDefeat(unit);
            string criticalText = damageResult.isCritical ? $"{WrapBattleInfoColor("触发暴击，", PhysicalInfoColorHex)}" : string.Empty;
            string damageText = FormatBattleInfoDamageText(damageResult);
            string deathText = BuildUnitDefeatMessage(unit);
            hitTargets.Add($"{unitName}{criticalText}受到{damageText}{deathText}");
        }

        if (hitTargets.Count > 0)
        {
            string message = WrapBattleInfoColor($"{casterName}使用了{skillName}，命中了{string.Join("、", hitTargets)}", NeutralInfoColorHex);
            if (missedTargets.Count > 0)
            {
                message += WrapBattleInfoColor($"，被{string.Join("、", missedTargets)}闪避了", NeutralInfoColorHex);
            }

            return message;
        }

        if (missedTargets.Count > 0)
        {
            return $"{casterName}对{string.Join("、", missedTargets)}使用了{skillName}，被{string.Join("、", missedTargets)}闪避了";
        }

        return $"{casterName}在{targetCell}使用了{skillName}";
    }

    private CombatDamageResult CalculateCombatArtDamage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || target == null || skill == null)
        {
            return null;
        }

        float attackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(caster.characterId);
        if (attackPower <= 0f)
        {
            return null;
        }

        float damage = attackPower * Mathf.Max(0f, skill.damageMultiplier);
        if (damage <= 0f)
        {
            return null;
        }

        CombatDamageResult result = new CombatDamageResult();
        int totalCriticalChance = caster.CriticalChance + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalChanceBonus(caster.characterId);
        int totalCriticalDamage = caster.CriticalDamage + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalDamageBonus(caster.characterId);
        result.isCritical = RollCriticalHit(totalCriticalChance);
        if (result.isCritical)
        {
            damage *= Mathf.Max(0f, totalCriticalDamage) / 100f;
        }

        BuildDamageComponents(result.components, damage, InventoryShortcutRuntimeBinder.GetCharacterWeaponDamageDistribution(caster.characterId), caster, target);
        for (int i = 0; i < result.components.Count; i++)
        {
            result.totalDamage += result.components[i].amount;
        }

        result.appliedDamage = Mathf.Max(0, Mathf.RoundToInt(result.totalDamage));
        return result;
    }

    private bool RollCriticalHit(int criticalChance)
    {
        criticalChance = Mathf.Max(0, criticalChance);
        if (criticalChance >= MaxHitChancePercent)
        {
            return true;
        }

        if (criticalChance <= MinHitChancePercent)
        {
            return false;
        }

        return Random.Range(0, MaxHitChancePercent) < criticalChance;
    }

    private void BuildDamageComponents(
        List<DamageComponent> components,
        float totalDamage,
        ItemDatabase.WeaponDamageDistribution distribution,
        BattleUnit caster,
        BattleUnit target)
    {
        components.Clear();
        if (totalDamage <= 0f)
        {
            return;
        }

        ItemDatabase.WeaponDamageDistribution resolvedDistribution = distribution ?? ItemDatabase.CreateDefaultWeaponDamageDistribution();
        int distributionTotal = Mathf.Max(0, resolvedDistribution.Total);
        if (distributionTotal <= 0)
        {
            resolvedDistribution = ItemDatabase.CreateDefaultWeaponDamageDistribution();
            distributionTotal = resolvedDistribution.Total;
        }

        AddDamageComponent(components, DamageAttributeType.Physical, totalDamage, resolvedDistribution.physical, distributionTotal, caster, target);
        AddDamageComponent(components, DamageAttributeType.Fire, totalDamage, resolvedDistribution.fire, distributionTotal, caster, target);
        AddDamageComponent(components, DamageAttributeType.Corruption, totalDamage, resolvedDistribution.corruption, distributionTotal, caster, target);
        AddDamageComponent(components, DamageAttributeType.Cold, totalDamage, resolvedDistribution.cold, distributionTotal, caster, target);
    }

    private void AddDamageComponent(
        List<DamageComponent> components,
        DamageAttributeType attributeType,
        float totalDamage,
        int distributionValue,
        int distributionTotal,
        BattleUnit caster,
        BattleUnit target)
    {
        if (components == null || totalDamage <= 0f || distributionValue <= 0 || distributionTotal <= 0)
        {
            return;
        }

        float baseAmount = totalDamage * distributionValue / distributionTotal;
        float mitigatedAmount = ApplyResistance(baseAmount, caster, target, attributeType);
        if (mitigatedAmount <= 0f)
        {
            return;
        }

        components.Add(new DamageComponent
        {
            attributeType = attributeType,
            amount = mitigatedAmount
        });
    }

    private static float ApplyResistance(float damage, BattleUnit caster, BattleUnit target, DamageAttributeType attributeType)
    {
        if (damage <= 0f)
        {
            return 0f;
        }

        int resistance = ResolveResistance(target, attributeType);
        int penetration = ResolveResistancePenetration(caster, attributeType);
        int finalResistance = Mathf.Max(0, resistance - penetration);
        float multiplier = 1f - (Mathf.Clamp(finalResistance, 0, 100) / 100f);
        return Mathf.Max(0f, damage * multiplier);
    }

    private static int ResolveResistancePenetration(BattleUnit caster, DamageAttributeType attributeType)
    {
        if (caster == null)
        {
            return 0;
        }

        int basePenetration;
        ItemDatabase.ResistanceModifierType resistanceType;
        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                basePenetration = caster.FireResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Fire;
                break;
            case DamageAttributeType.Corruption:
                basePenetration = caster.CorruptionResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Corruption;
                break;
            case DamageAttributeType.Cold:
                basePenetration = caster.ColdResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Cold;
                break;
            default:
                basePenetration = caster.PhysicalResistancePenetration;
                resistanceType = ItemDatabase.ResistanceModifierType.Physical;
                break;
        }

        return basePenetration + InventoryShortcutRuntimeBinder.GetCharacterWeaponResistancePenetration(caster.characterId, resistanceType);
    }

    private static int ResolveResistance(BattleUnit target, DamageAttributeType attributeType)
    {
        if (target == null)
        {
            return 0;
        }

        switch (attributeType)
        {
            case DamageAttributeType.Fire:
                return target.FireResistance;
            case DamageAttributeType.Corruption:
                return target.CorruptionResistance;
            case DamageAttributeType.Cold:
                return target.ColdResistance;
            default:
                return target.PhysicalResistance;
        }
    }

    private void ShowDamagePopup(BattleUnit target, CombatDamageResult damageResult)
    {
        if (target == null || damageResult == null)
        {
            return;
        }

        List<BattleDamageNumberPopup.DamageSegment> segments = BuildDamageSegments(damageResult);
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

    private List<BattleDamageNumberPopup.DamageSegment> BuildDamageSegments(CombatDamageResult damageResult)
    {
        List<BattleDamageNumberPopup.DamageSegment> segments = new List<BattleDamageNumberPopup.DamageSegment>();
        if (damageResult == null)
        {
            return segments;
        }

        for (int i = 0; i < damageResult.components.Count; i++)
        {
            DamageComponent component = damageResult.components[i];
            if (component.amount <= 0f)
            {
                continue;
            }

            segments.Add(new BattleDamageNumberPopup.DamageSegment
            {
                text = FormatDamageValue(component.amount),
                color = ResolveDamageColor(component.attributeType)
            });
        }

        return segments;
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
        for (int i = 0; i < damageResult.components.Count; i++)
        {
            DamageComponent component = damageResult.components[i];
            if (component.amount <= 0f)
            {
                continue;
            }

            string attributeColorHex = GetDamageAttributeColorHex(component.attributeType);
            string amountText = WrapBattleInfoColor(FormatDamageValue(component.amount), attributeColorHex);
            string attributeText = WrapBattleInfoColor(GetDamageAttributeDisplayName(component.attributeType), attributeColorHex);
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

    private bool RollSkillHit(BattleUnit caster, BattleUnit target)
    {
        if (caster == null || target == null)
        {
            return false;
        }

        int hitChance = Mathf.Clamp(caster.HitRate - target.DodgeRate, MinHitChancePercent, MaxHitChancePercent);
        if (hitChance >= MaxHitChancePercent)
        {
            return true;
        }

        if (hitChance <= MinHitChancePercent)
        {
            return false;
        }

        return Random.Range(0, MaxHitChancePercent) < hitChance;
    }

    private void PlayHitReaction(BattleUnit target)
    {
        BattleAudioUtility.PlayOnce(ResolveHitReactionSound(), ResolveHitReactionSoundPrefab(), target, battleCamera);
        PlayReactionAnimation(target, target != null ? target.GetHitReactionAnimationStateName(ResolveHitReactionStateName()) : ResolveHitReactionStateName());
    }

    private void PlayDodgeReaction(BattleUnit target)
    {
        BattleAudioUtility.PlayOnce(ResolveDodgeSound(), ResolveDodgeSoundPrefab(), target, battleCamera);
        PlayReactionAnimation(target, target != null ? target.GetDodgeAnimationStateName(ResolveDodgeStateName()) : ResolveDodgeStateName());
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
            target.GetIdleAnimationStateName(ResolveIdleStateName()),
            ShouldCompensateGlobalMotionForState(stateName));
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
        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null && eventSystem.IsPointerOverGameObject();
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

    private static Image FindImageByPath(string path)
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static TMP_Text FindTextByPath(string path)
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static RectSnapshot CaptureSnapshot(RectTransform rectTransform)
    {
        return new RectSnapshot
        {
            anchoredPosition = rectTransform.anchoredPosition,
            sizeDelta = rectTransform.sizeDelta,
            offsetMin = rectTransform.offsetMin,
            offsetMax = rectTransform.offsetMax,
            pivot = rectTransform.pivot,
            anchorMin = rectTransform.anchorMin,
            anchorMax = rectTransform.anchorMax,
            localScale = rectTransform.localScale
        };
    }

    private static void ApplySnapshot(RectTransform rectTransform, RectSnapshot snapshot)
    {
        rectTransform.anchorMin = snapshot.anchorMin;
        rectTransform.anchorMax = snapshot.anchorMax;
        rectTransform.pivot = snapshot.pivot;
        rectTransform.offsetMin = snapshot.offsetMin;
        rectTransform.offsetMax = snapshot.offsetMax;
        rectTransform.sizeDelta = snapshot.sizeDelta;
        rectTransform.anchoredPosition = snapshot.anchoredPosition;
        rectTransform.localScale = snapshot.localScale;
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

        grid.ResetHighlights();
        moveDuration = grid.MoveUnit(unit, destination);
        if (moveSkill != null)
        {
            StartCoroutine(PlayTrackedSkillAudioRoutine(unit, moveSkill, moveDuration));
            unit.PlayTimedAnimation(
                unit.GetMoveAnimationStateName(moveSkill.actionStateName),
                moveDuration,
                unit.GetIdleAnimationStateName(ResolveIdleStateName()));
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
        if (caster == null || skill == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(skill.actionStateName))
        {
            yield return PlayTrackedSkillAudioRoutine(caster, skill, 0f);
            yield break;
        }

        Animator animator = caster.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            yield break;
        }

        caster.SetAnimationPositionCompensation(skill.compensateActionMotion);

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        Quaternion previousRotation = caster.transform.rotation;
        if (Mathf.Abs(skill.actionYawOffset) > 0.01f)
        {
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, skill.actionYawOffset, 0f);
        }

        animator.Play(skill.actionStateName, 0, 0f);

        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float clipDuration = currentState.length;
        StartCoroutine(PlayTrackedSkillAudioRoutine(caster, skill, clipDuration));
        if (clipDuration > 0.01f)
        {
            yield return new WaitForSeconds(clipDuration);
        }

        string idleStateName = caster.GetIdleAnimationStateName(ResolveIdleStateName());
        float idleYawOffset = ResolveIdleYawOffset();
        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            animator.Play(idleStateName, 0, 0f);
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, idleYawOffset, 0f);
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            animator.Play(previousStateHash, 0, 0f);
            caster.transform.rotation = previousRotation;
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

        if (string.IsNullOrWhiteSpace(skill.actionStateName))
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

        caster.SetAnimationPositionCompensation(skill.compensateActionMotion);

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        Quaternion previousRotation = caster.transform.rotation;
        if (Mathf.Abs(skill.actionYawOffset) > 0.01f)
        {
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, skill.actionYawOffset, 0f);
        }

        animator.Play(skill.actionStateName, 0, 0f);
        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float clipDuration = currentState.length;
        StartCoroutine(PlayTrackedSkillAudioRoutine(caster, skill, clipDuration));
        int totalFrames = ResolveAnimationStateTotalFrames(animator, skill.actionStateName, clipDuration);
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

        string idleStateName = caster.GetIdleAnimationStateName(ResolveIdleStateName());
        float idleYawOffset = ResolveIdleYawOffset();
        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            animator.Play(idleStateName, 0, 0f);
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, idleYawOffset, 0f);
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            animator.Play(previousStateHash, 0, 0f);
            caster.transform.rotation = previousRotation;
        }

        caster.SetAnimationPositionCompensation(false);
    }

    private IEnumerator PlayTrackedSkillAudioRoutine(BattleUnit unit, BattleSkillDatabase.SkillEntry skill, float totalDuration)
    {
        if (skill == null)
        {
            yield break;
        }

        float delay = ResolveSkillSoundDelaySeconds(skill, totalDuration);
        if (delay > 0.01f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (totalDuration <= 0.01f)
        {
            BattleAudioUtility.PlayOnce(skill.actionSound, skill.actionSoundPrefab, unit, battleCamera);
            yield break;
        }

        BattleAudioUtility.PlaybackHandle handle = BattleAudioUtility.StartTracked(skill.actionSound, skill.actionSoundPrefab, unit, battleCamera);
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

    private static float ResolveSkillSoundDelaySeconds(BattleSkillDatabase.SkillEntry skill, float clipDuration)
    {
        if (skill == null)
        {
            return 0f;
        }

        int soundDelayFrame = Mathf.Max(0, skill.soundDelayFrame);
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
                if (!IsUnitInsideContinuousArea(unit, targetCell, skill))
                {
                    continue;
                }
            }
            else if (IsCircularAxisAreaSkill(skill))
            {
                if (!IsUnitInsideCircularAxisArea(caster, unit, targetCell, skill))
                {
                    continue;
                }
            }
            else
            {
                HashSet<Vector2Int> affectedCells = CollectAreaEffectCells(caster, targetCell, skill);
                if (!IsUnitInsideAreaCells(unit, affectedCells))
                {
                    continue;
                }
            }

            names.Add(ResolveBattleInfoUnitName(unit, richText: true));
        }

        return names;
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

    private static string ResolveIdleStateName()
    {
        return BattleAnimationSettingsResolver.ResolveIdleStateName();
    }

    private static string ResolveEnterBattleStateName()
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleStateName();
    }

    private static AudioClip ResolveEnterBattleSound()
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleSound();
    }

    private static GameObject ResolveEnterBattleSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleSoundPrefab();
    }

    private static bool ResolveEnterBattleCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveEnterBattleCompensateMotion();
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

    private static string ResolveExitBattleStateName()
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleStateName();
    }

    private static AudioClip ResolveExitBattleSound()
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleSound();
    }

    private static GameObject ResolveExitBattleSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleSoundPrefab();
    }

    private static bool ResolveExitBattleCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveExitBattleCompensateMotion();
    }

    private void PlayExitBattleAnimations()
    {
        string stateName = ResolveExitBattleStateName();
        if (string.IsNullOrWhiteSpace(stateName))
        {
            PlayExplorationIdleAnimation();
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlive || unit.team != BattleTeam.Player)
            {
                continue;
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            {
                continue;
            }

            BattleAudioUtility.PlayOnce(ResolveExitBattleSound(), ResolveExitBattleSoundPrefab(), unit, battleCamera);
            unit.PlayAnimationStateForCurrentClipDuration(
                stateName,
                ResolveExplorationIdleStateName(),
                ResolveExitBattleCompensateMotion());
            StartCoroutine(HideRuntimeWeaponsAfterExitAnimation(unit, stateName));
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

    private IEnumerator PlayEnterBattleAnimations()
    {
        if (units == null || units.Count == 0)
        {
            yield break;
        }

        bool playedAny = false;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            string enterBattleStateName = unit.GetEnterBattleAnimationStateName(ResolveEnterBattleStateName());
            if (string.IsNullOrWhiteSpace(enterBattleStateName))
            {
                continue;
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            {
                continue;
            }

            BattleAudioUtility.PlayOnce(ResolveEnterBattleSound(), ResolveEnterBattleSoundPrefab(), unit, battleCamera);
            unit.SetAnimationPositionCompensation(ResolveEnterBattleCompensateMotion());
            animator.Play(enterBattleStateName, 0, 0f);
            playedAny = true;
        }

        if (!playedAny)
        {
            yield break;
        }

        yield return null;

        float longestDuration = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            string enterBattleStateName = unit.GetEnterBattleAnimationStateName(ResolveEnterBattleStateName());
            if (string.IsNullOrWhiteSpace(enterBattleStateName))
            {
                continue;
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            {
                continue;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            longestDuration = Mathf.Max(longestDuration, stateInfo.length);
        }

        if (longestDuration > 0.01f)
        {
            yield return new WaitForSeconds(longestDuration);
        }

        string idleStateName = ResolveIdleStateName();
        if (string.IsNullOrWhiteSpace(idleStateName))
        {
            yield break;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            unit.SetAnimationPositionCompensation(false);
            unit.PlayAnimationState(unit.GetIdleAnimationStateName(idleStateName));
        }
    }

    private IEnumerator HideRuntimeWeaponsAfterExitAnimation(BattleUnit unit, string stateName)
    {
        if (unit == null || string.IsNullOrWhiteSpace(stateName))
        {
            yield break;
        }

        Animator animator = unit.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            yield break;
        }

        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float duration = currentState.length;
        if (duration > 0.01f)
        {
            yield return new WaitForSeconds(duration);
        }

        SetRuntimeWeaponModelsVisible(unit.transform, false);
    }

    private static void SetRuntimeWeaponModelsVisible(Transform root, bool visible)
    {
        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, RuntimeWeaponModelName, System.StringComparison.Ordinal))
            {
                child.gameObject.SetActive(visible);
            }

            SetRuntimeWeaponModelsVisible(child, visible);
        }
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

    private static string ResolveCombatArtLeftAimStateName()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtLeftAimStateName();
    }

    private static bool ResolveCombatArtLeftAimCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtLeftAimCompensateMotion();
    }

    private static AudioClip ResolveCombatArtLeftAimSound()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtLeftAimSound();
    }

    private static GameObject ResolveCombatArtLeftAimSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtLeftAimSoundPrefab();
    }

    private static string ResolveCombatArtRightAimStateName()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtRightAimStateName();
    }

    private static bool ResolveCombatArtRightAimCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtRightAimCompensateMotion();
    }

    private static AudioClip ResolveCombatArtRightAimSound()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtRightAimSound();
    }

    private static GameObject ResolveCombatArtRightAimSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveCombatArtRightAimSoundPrefab();
    }

    private static float ResolveIdleYawOffset()
    {
        return BattleAnimationSettingsResolver.ResolveIdleYawOffset();
    }

    private static string ResolveHitReactionStateName()
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionStateName();
    }

    private static bool ResolveHitReactionCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionCompensateMotion();
    }

    private static AudioClip ResolveHitReactionSound()
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionSound();
    }

    private static GameObject ResolveHitReactionSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveHitReactionSoundPrefab();
    }

    private static string ResolveDodgeStateName()
    {
        return BattleAnimationSettingsResolver.ResolveDodgeStateName();
    }

    private static bool ResolveDodgeCompensateMotion()
    {
        return BattleAnimationSettingsResolver.ResolveDodgeCompensateMotion();
    }

    private static AudioClip ResolveDodgeSound()
    {
        return BattleAnimationSettingsResolver.ResolveDodgeSound();
    }

    private static GameObject ResolveDodgeSoundPrefab()
    {
        return BattleAnimationSettingsResolver.ResolveDodgeSoundPrefab();
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

    private void StartCombatArtAimAnimation(string stateName)
    {
        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(stateName))
        {
            combatArtAimAnimationActive = false;
            currentCombatArtAimStateName = string.Empty;
            return;
        }

        StopCombatArtAimAudio();
        ResolveCombatArtAimAudioForState(stateName, out AudioClip aimSound, out GameObject aimSoundPrefab);
        currentCombatArtAimAudioHandle = BattleAudioUtility.StartTracked(aimSound, aimSoundPrefab, activeUnit, battleCamera);
        activeUnit.PlayAnimationState(stateName, ShouldCompensateGlobalMotionForState(stateName));
        combatArtAimAnimationActive = true;
        combatArtAimAnimationActiveUntilTime = Time.time + MinCombatArtAimAnimationDurationSeconds;
        currentCombatArtAimStateName = stateName;
    }

    private void StopCombatArtAimAnimation(bool force = false)
    {
        if (!combatArtAimAnimationActive)
        {
            return;
        }

        if (activeUnit == null || !activeUnit.IsAlive || activeUnit.IsMoving || isResolvingSkillExecution)
        {
            if (force)
            {
                StopCombatArtAimAudio();
                combatArtAimAnimationActive = false;
                currentCombatArtAimStateName = string.Empty;
            }
            return;
        }

        if (!force && Time.time < combatArtAimAnimationActiveUntilTime)
        {
            return;
        }

        string idleStateName = activeUnit.GetIdleAnimationStateName(ResolveIdleStateName());
        if (!string.IsNullOrWhiteSpace(idleStateName))
        {
            activeUnit.PlayAnimationState(idleStateName);
        }

        activeUnit.SetAnimationPositionCompensation(false);
        StopCombatArtAimAudio();
        combatArtAimAnimationActive = false;
        currentCombatArtAimStateName = string.Empty;
    }

    private void UpdateCombatArtAimFacing(Vector3 worldPosition, Vector2Int hoveredCell)
    {
        if (!IsSkillModeActive() || activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled)
        {
            return;
        }

        // Root 朝向始终由代码接管，瞄准动画只负责脚步表现。
        activeUnit.FaceToward(worldPosition);

        bool cellChanged = !hasLastCombatArtAimHoverCell || hoveredCell != lastCombatArtAimHoverCell;
        lastCombatArtAimHoverCell = hoveredCell;
        hasLastCombatArtAimHoverCell = true;

        if (!cellChanged)
        {
            StopCombatArtAimAnimation();
            return;
        }

        string stateName = ResolveCombatArtAimStateNameForTarget(worldPosition);
        if (string.IsNullOrWhiteSpace(stateName))
        {
            StopCombatArtAimAnimation(force: true);
            activeUnit.FaceToward(worldPosition);
            return;
        }

        if (!combatArtAimAnimationActive || !string.Equals(currentCombatArtAimStateName, stateName, System.StringComparison.Ordinal))
        {
            StartCombatArtAimAnimation(stateName);
        }
    }

    private static bool ShouldUseCombatArtAimPreview(BattleSkillDatabase.SkillEntry skill)
    {
        return skill != null && skill.group == BattleSkillDatabase.SkillGroup.CombatArt;
    }

    private static void ResolveCombatArtAimAudioForState(string stateName, out AudioClip clip, out GameObject soundPrefab)
    {
        BattleAnimationSettingsResolver.ResolveCombatArtAimAudioForState(stateName, out clip, out soundPrefab);
    }

    private static bool ShouldCompensateGlobalMotionForState(string stateName)
    {
        return BattleAnimationSettingsResolver.ShouldCompensateGlobalMotionForState(stateName);
    }

    private void StopCombatArtAimAudio()
    {
        if (currentCombatArtAimAudioHandle == null)
        {
            return;
        }

        currentCombatArtAimAudioHandle.Stop();
        currentCombatArtAimAudioHandle = null;
    }

    private string ResolveCombatArtAimStateNameForTarget(Vector3 worldPosition)
    {
        if (activeUnit == null)
        {
            return string.Empty;
        }

        Vector3 localDirection = activeUnit.transform.InverseTransformPoint(worldPosition);
        string leftStateName = activeUnit.GetCombatArtLeftAimAnimationStateName(ResolveCombatArtLeftAimStateName());
        string rightStateName = activeUnit.GetCombatArtRightAimAnimationStateName(ResolveCombatArtRightAimStateName());

        if (localDirection.x < -0.001f)
        {
            if (!string.IsNullOrWhiteSpace(leftStateName))
            {
                return leftStateName;
            }

            return rightStateName;
        }

        if (localDirection.x > 0.001f)
        {
            if (!string.IsNullOrWhiteSpace(rightStateName))
            {
                return rightStateName;
            }

            return leftStateName;
        }

        if (!string.IsNullOrWhiteSpace(currentCombatArtAimStateName))
        {
            return currentCombatArtAimStateName;
        }

        return !string.IsNullOrWhiteSpace(rightStateName) ? rightStateName : leftStateName;
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


