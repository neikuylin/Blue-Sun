using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTurnSystem : MonoBehaviour
{
    public static event System.Action<MapTemplateDatabase.ConnectionDirection> 换房移动开始;

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
    private const string NoSkillSourceText = BattleSkillDatabase.NoSkillSourceText;
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
    private readonly Color activePlayerFootprintColor = new Color(1.00f, 1.00f, 1.00f, 0.15f);
    private readonly Color activeEnemyFootprintColor = new Color(0.95f, 0.28f, 0.20f, 0.15f);
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
    private bool cameraSpaceLockActive;
    private BattleUnit cameraSpaceLockUnit;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;
    private Button endTurnButton;
    private Button moveSkillButton;
    private BattleSceneBindings sceneBindings;
    private BattleSkillDatabase skillDatabase;
    private Coroutine skillExecutionRoutine;
    private int absoluteRoundIndex = -1;
    private int currentRoundIndex = -1;
    private string activeSkillId = string.Empty;
    private string activeSkillSource = string.Empty;
    private BattleSkillDatabase.SkillEntry activeSkill;
    private int activeSkillRemainingCastCount;
    private readonly List<BattleUnit> queuedActiveSkillTargets = new List<BattleUnit>();
    private readonly List<Vector2Int> queuedActiveSkillTargetCells = new List<Vector2Int>();
    private bool hasSkillHoverPreview;
    private Vector2Int skillHoverCell;
    private bool skillHoverValid;
    private bool skillHoverHasAnyVisibleCells;
    private int skillHoverActionPointCost;
    private bool isResolvingSkillExecution;
    private BattleFlowMode currentMode = BattleFlowMode.Combat;
    private string activeExplorationActionId = ExplorationMoveSkillId;
    private Coroutine pendingDoorNavigationRoutine;
    private bool hasPendingDoorNavigationCell;
    private Vector2Int pendingDoorNavigationCell;
    private bool doorExitNavigationLocked;
    private bool forceNavigationFollowerFollow;
    private 可交互状态对象切换器 pendingDoorNavigationStateSwitcher;
    private bool enterBattleAnimationInProgress;
    private bool beginTurnAfterEnterBattle;
    private BattleUnit pendingEnterBattleLeadUnit;
    private bool pendingExplorationModeEnter;
    private BattleInputService inputService;
    private 格子交互点击接入器 gridInteractionClickAdapter;
    private BattleTargetPanelService targetPanelService;
    private BattleTurnTimelineService timelineService;
    private 战斗模式服务 modeService;
    private 战斗技能执行服务 skillExecutionService;
    private 战斗伤害结算服务 damageResolutionService;
    private 战斗技能基础结算服务 skillCoreResolutionService;
    private 战斗效果回合结算服务 effectTurnResolutionService;
    private 战斗敌方回合服务 enemyTurnService;
    private 战斗敌方决策服务 enemyDecisionService;
    private 战斗敌方执行服务 enemyExecutionService;
    private 战斗技能预览服务 skillPreviewService;
    private 战斗技能预览判定服务 skillPreviewJudgeService;
    private 战斗技能区域预览服务 skillPreviewAreaService;
    private 战斗技能区域规则服务 skillAreaRuleService;
    private 战斗技能指向表现服务 skillTargetingPresentationService;
    private 战斗信息文本服务 battleInfoTextService;
    private 战斗技能表现服务 skillPresentationService;
    private 战斗技能动作解析服务 skillActionResolverService;
    private 战斗探索移动服务 explorationMoveService;
    private 格子触发导航服务 gridTriggerNavigationService;
    private 战斗伤害弹字服务 damagePopupService;

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
        sceneBindings = BattleSceneBindings.FindInActiveScene();
        BindEndTurnButton();
        BindSkillButton();
        skillDatabase = BattleSkillDatabase.LoadDefault();
        if (inputService == null)
        {
            inputService = new BattleInputService();
        }

        if (gridInteractionClickAdapter == null)
        {
            gridInteractionClickAdapter = new 格子交互点击接入器();
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

        if (enemyExecutionService == null)
        {
            enemyExecutionService = new 战斗敌方执行服务();
        }

        if (skillPreviewService == null)
        {
            skillPreviewService = new 战斗技能预览服务();
        }

        if (skillPreviewJudgeService == null)
        {
            skillPreviewJudgeService = new 战斗技能预览判定服务();
        }

        if (skillPreviewAreaService == null)
        {
            skillPreviewAreaService = new 战斗技能区域预览服务();
        }

        if (skillAreaRuleService == null)
        {
            skillAreaRuleService = new 战斗技能区域规则服务();
        }

        if (skillTargetingPresentationService == null)
        {
            skillTargetingPresentationService = new 战斗技能指向表现服务();
        }

        if (battleInfoTextService == null)
        {
            battleInfoTextService = new 战斗信息文本服务();
        }

        if (skillPresentationService == null)
        {
            skillPresentationService = new 战斗技能表现服务();
        }

        if (skillActionResolverService == null)
        {
            skillActionResolverService = new 战斗技能动作解析服务();
        }

        if (explorationMoveService == null)
        {
            explorationMoveService = new 战斗探索移动服务();
        }

        if (gridTriggerNavigationService == null)
        {
            gridTriggerNavigationService = new 格子触发导航服务();
        }

        if (damagePopupService == null)
        {
            damagePopupService = new 战斗伤害弹字服务();
        }

        gridTriggerNavigationService.初始化(this, grid, TryMoveToGridTriggerCell);
        battleInfoTextService.绑定显示器(BattleInfoWindowPresenter.FindInActiveScene());

        skillPreviewService.重置状态();
        skillAreaRuleService.初始化(
            grid,
            units,
            GetDisplayedSkillRange,
            IsValidSkillTarget);
        skillPreviewJudgeService.初始化(
            grid,
            units,
            IsValidSkillTarget,
            GetDisplayedSkillRange,
            skillAreaRuleService.使用连续圆形区域,
            skillAreaRuleService.是圆轴区域技能,
            skillAreaRuleService.是否位于连续圆形区域内,
            skillAreaRuleService.是否位于圆轴区域内,
            skillAreaRuleService.收集区域效果格);
        skillPreviewAreaService.初始化(
            grid,
            skillAreaRuleService.使用连续圆形区域,
            skillAreaRuleService.是圆轴区域技能,
            skillAreaRuleService.获取连续区域半径世界,
            skillAreaRuleService.解析轴向方向世界,
            skillAreaRuleService.获取轴向范围世界,
            skillAreaRuleService.获取轴向宽度世界,
            skillAreaRuleService.收集区域效果格);

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
        activeSkillRemainingCastCount = 0;
        queuedActiveSkillTargets.Clear();
        queuedActiveSkillTargetCells.Clear();
        currentMode = BattleFlowMode.Exploration;
        activeExplorationActionId = ExplorationMoveSkillId;
        pendingExplorationModeEnter = false;
        ClearPendingDoorNavigation();

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
        skillPreviewService?.设置行动点提示文本(hintText);
    }

    private void OnDestroy()
    {
        ClearPendingDoorNavigation();
        gridTriggerNavigationService?.清除();
        timelineService?.Dispose();
        explorationMoveService?.停止全部(this);
        skillPresentationService?.恢复全局时间缩放(this, HitFeelTimeScale);
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
        HandleCameraReturnInput();

        if (IsExplorationMode)
        {
            UpdateExplorationMode();
            return;
        }

        gridInteractionClickAdapter?.更新悬浮(battleCamera);
        if (gridInteractionClickAdapter != null &&
            gridInteractionClickAdapter.处理点击(battleCamera, TryNavigateToDoor, TryTriggerGridInteraction))
        {
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
            skillPreviewService?.更新悬停目标闪烁(
                grid,
                Time.time,
                hoveredEnemyFlashColor,
                hoveredAllyFlashColor);
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
            skillPreviewService != null ? skillPreviewService.HoveredSkillTarget : null,
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

        if (doorExitNavigationLocked)
        {
            return;
        }

        gridInteractionClickAdapter?.更新悬浮(battleCamera);

        if (gridInteractionClickAdapter != null &&
            gridInteractionClickAdapter.处理点击(battleCamera, TryNavigateToDoor, TryTriggerGridInteraction))
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
        if (enemyTurnService == null || enemyDecisionService == null || enemyExecutionService == null)
        {
            EndTurn();
            yield break;
        }

        bool IsEnemySkillTarget(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
        {
            return target != null &&
                target.IsAlive &&
                caster != null &&
                target.team != caster.team &&
                IsValidSkillTarget(caster, target, skill);
        }

        float? MoveEnemyToCell(BattleUnit unit, Vector2Int destination)
        {
            return enemyExecutionService.尝试移动到格子(
                this,
                unit,
                destination,
                grid,
                ResolveSkill,
                GetSkillManaCost,
                GetMoveActionPointCost,
                GetMoveMaxRange,
                (skill, battleUnit) => skillActionResolverService != null ? skillActionResolverService.解析动作状态名(skill, battleUnit) : string.Empty,
                ResolveIdleStateName,
                (skill, battleUnit) => skillActionResolverService != null && skillActionResolverService.解析动作位移补偿(skill, battleUnit),
                PlayTrackedSkillAudioRoutine);
        }

        IEnumerator ExecuteEnemyAction(BattleUnit caster, 战斗敌方回合服务.技能动作 action)
        {
            yield return enemyExecutionService.执行动作(
                caster,
                action,
                ExecuteTargetSkillRoutine,
                ExecuteAreaSkillRoutine);
        }

        yield return enemyTurnService.执行敌方回合(
            activeUnit,
            caster => enemyDecisionService.构建技能选项(caster, NormalAttackSkillId, ResolveSkill),
            (caster, skillChoices) => enemyDecisionService.尝试查找技能动作(
                caster,
                units,
                skillChoices,
                grid,
                (unit, choice) => enemyExecutionService.可以使用技能(unit, choice, GetSkillActionPointCost, GetSkillManaCost),
                IsEnemySkillTarget,
                (unit, targetCell, target, skill) => enemyExecutionService.可以在目标处施放(unit, targetCell, target, skill, CanCastSkillAt)),
            (caster, skillChoices) => enemyDecisionService.尝试向技能范围移动(
                caster,
                units,
                skillChoices,
                grid,
                ResolveSkill,
                GetMoveMaxRange,
                GetSkillManaCost,
                GetMoveActionPointCost,
                GetSkillActionPointCost,
                (unit, choice) => enemyExecutionService.可以使用技能(unit, choice, GetSkillActionPointCost, GetSkillManaCost),
                IsEnemySkillTarget,
                (unit, castCell, target, skill) => enemyExecutionService.可以从格子施放(
                    unit,
                    castCell,
                    target,
                    skill,
                    grid,
                    GetSkillRange,
                    IsValidSkillTarget),
                FindClosestLivingOpponent,
                FindBestStepToward,
                GetSkillRange,
                MoveEnemyToCell,
                FaceTowardTargetUnit),
            ExecuteEnemyAction,
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
        宝箱内容绑定.关闭已打开宝箱内容();
        if (moveSkill != null)
        {
            StartCoroutine(PlayTrackedSkillAudioRoutine(unit, moveSkill, moveDuration));
            unit.PlayTimedAnimation(
                unit.GetMoveAnimationStateName(skillActionResolverService != null ? skillActionResolverService.解析动作状态名(moveSkill, unit) : string.Empty),
                moveDuration,
                unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)),
                skillActionResolverService != null && skillActionResolverService.解析动作位移补偿(moveSkill, unit));
        }

        unit.SpendActionPoints(moveActionPointCost);
        unit.SpendMana(moveManaCost);
        ClearActiveSkillMode();
        RefreshHighlights();
    }

    private bool TryMoveFreely(BattleUnit unit, Vector2Int destination)
    {
        return TryMoveFreely(unit, destination, true);
    }

    private bool TryMoveFreely(
        BattleUnit unit,
        Vector2Int destination,
        bool clearDoorNavigationOnDifferentDestination)
    {
        if (doorExitNavigationLocked && destination != pendingDoorNavigationCell)
        {
            return false;
        }

        if (clearDoorNavigationOnDifferentDestination &&
            !doorExitNavigationLocked &&
            pendingDoorNavigationStateSwitcher != null)
        {
            ClearPendingDoorNavigation();
        }

        if (clearDoorNavigationOnDifferentDestination &&
            !doorExitNavigationLocked &&
            hasPendingDoorNavigationCell &&
            destination != pendingDoorNavigationCell)
        {
            ClearPendingDoorNavigation();
        }

        if (clearDoorNavigationOnDifferentDestination &&
            !doorExitNavigationLocked &&
            forceNavigationFollowerFollow &&
            pendingDoorNavigationStateSwitcher == null &&
            !hasPendingDoorNavigationCell)
        {
            ClearPendingDoorNavigation();
        }

        bool moved = explorationMoveService != null && explorationMoveService.尝试自由移动(
            this,
            unit,
            destination,
            IsExplorationMode,
            grid,
            units,
            FindUnitByCharacterId,
            ResolveExplorationIdleStateName,
            ResolveExplorationMoveStateName,
            ResolveExplorationMoveCompensateMotion,
            ResolveExplorationMoveSound,
            ResolveExplorationMoveSoundPrefab,
            battleCamera,
            RefreshHighlights,
            forceNavigationFollowerFollow);
        if (moved)
        {
            宝箱内容绑定.关闭已打开宝箱内容();
        }

        return moved;
    }

    private bool TryMoveToGridTriggerCell(BattleUnit unit, Vector2Int destination)
    {
        return TryMoveFreely(unit, destination, true);
    }

    public bool TryNavigateToDoor(
        MapTemplateDatabase.ConnectionDirection direction,
        可交互状态对象切换器 doorStateSwitcher)
    {
        if (!IsExplorationMode)
        {
            提示战斗中不能触发格子交互("门");
            return false;
        }

        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled || grid == null)
        {
            return false;
        }

        if (!BattleBootstrap.IsCurrentRoomEncounterCleared())
        {
            return false;
        }

        if (!BattleBootstrap.TryResolveCurrentRoomTarget(direction, out _))
        {
            return false;
        }

        List<Vector2Int> triggerCells = new List<Vector2Int>();
        grid.CollectDoorExitTriggerCells(direction, triggerCells);
        if (triggerCells.Count == 0)
        {
            return false;
        }

        Vector2Int autoDestination;
        if (!grid.TryGetDoorExitDefaultTargetCell(direction, out autoDestination))
        {
            return false;
        }

        ClearPendingDoorNavigation();
        if (gridTriggerNavigationService == null ||
            !gridTriggerNavigationService.尝试移动到触发格并执行(
            activeUnit,
            triggerCells,
            () => LockDoorExitNavigation(activeUnit, direction, autoDestination)))
        {
            ClearPendingDoorNavigation();
            return false;
        }

        forceNavigationFollowerFollow = true;
        pendingDoorNavigationStateSwitcher = doorStateSwitcher;
        StartNavigationFollowerFollow();
        return true;
    }

    private void StartNavigationFollowerFollow()
    {
        explorationMoveService?.开始跟随移动(
            this,
            activeUnit,
            IsExplorationMode,
            grid,
            units,
            FindUnitByCharacterId,
            ResolveExplorationIdleStateName,
            ResolveExplorationMoveStateName,
            ResolveExplorationMoveCompensateMotion,
            RefreshHighlights,
            forceNavigationFollowerFollow);
    }

    public bool TryTriggerGridInteraction(格子物件触发器 trigger)
    {
        if (trigger == null)
        {
            return false;
        }

        if (!IsExplorationMode)
        {
            提示战斗中不能触发格子交互(trigger.名称);
            return true;
        }

        if (activeUnit == null || !activeUnit.IsAlive || !activeUnit.isPlayerControlled || grid == null)
        {
            return true;
        }

        List<Vector2Int> triggerCells = new List<Vector2Int>();
        IReadOnlyList<格子模板数据库.CellPosition> cells = trigger.触发格;
        if (cells != null)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                triggerCells.Add(cells[i].ToVector2Int());
            }
        }

        ClearPendingDoorNavigation();
        bool alreadyAtTriggerCell = ContainsCell(triggerCells, activeUnit.IsMoving ? grid.WorldToCell(activeUnit.transform.position) : activeUnit.currentCell);
        if (gridTriggerNavigationService != null &&
            gridTriggerNavigationService.尝试移动到触发格并执行(
                activeUnit,
                triggerCells,
                () =>
                {
                    trigger.执行到达触发();
                }))
        {
            if (!alreadyAtTriggerCell)
            {
                forceNavigationFollowerFollow = true;
                StartNavigationFollowerFollow();
            }
        }

        return true;
    }

    private static bool ContainsCell(IReadOnlyList<Vector2Int> cells, Vector2Int target)
    {
        if (cells == null)
        {
            return false;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private void 提示战斗中不能触发格子交互(string triggerName)
    {
    }

    private void LockDoorExitNavigation(
        BattleUnit unit,
        MapTemplateDatabase.ConnectionDirection direction,
        Vector2Int autoDestination)
    {
        if (unit == null)
        {
            return;
        }

        if (pendingDoorNavigationRoutine != null)
        {
            StopCoroutine(pendingDoorNavigationRoutine);
            pendingDoorNavigationRoutine = null;
        }

        pendingDoorNavigationCell = autoDestination;
        doorExitNavigationLocked = true;
        hasPendingDoorNavigationCell = true;
        换房移动开始?.Invoke(direction);

        pendingDoorNavigationRoutine = StartCoroutine(RunLockedDoorExitNavigation(unit, direction, autoDestination));
    }

    private IEnumerator RunLockedDoorExitNavigation(
        BattleUnit unit,
        MapTemplateDatabase.ConnectionDirection direction,
        Vector2Int autoDestination)
    {
        yield return PlayRoomEnterForwardAnimations();

        if (unit == null)
        {
            pendingDoorNavigationRoutine = null;
            ClearPendingDoorNavigation();
            yield break;
        }

        Vector2Int currentCell = grid != null
            ? (unit.IsMoving ? grid.WorldToCell(unit.transform.position) : unit.currentCell)
            : unit.currentCell;
        if (currentCell != autoDestination && !TryMoveFreely(unit, autoDestination, false))
        {
            pendingDoorNavigationRoutine = null;
            ClearPendingDoorNavigation();
            yield break;
        }

        yield return WaitForLockedDoorExitNavigation(unit, direction, autoDestination);
    }

    private IEnumerator WaitForLockedDoorExitNavigation(
        BattleUnit unit,
        MapTemplateDatabase.ConnectionDirection direction,
        Vector2Int autoDestination)
    {
        while (unit != null && unit.IsMoving)
        {
            yield return null;
        }

        if (unit == null || !hasPendingDoorNavigationCell || pendingDoorNavigationCell != autoDestination)
        {
            pendingDoorNavigationRoutine = null;
            yield break;
        }

        if (grid == null || grid.WorldToCell(unit.transform.position) != autoDestination)
        {
            pendingDoorNavigationRoutine = null;
            ClearPendingDoorNavigation();
            yield break;
        }

        pendingDoorNavigationRoutine = null;
        ClearPendingDoorNavigation();
        墨血换房转场控制器.尝试播放换房转场(() =>
        {
            BattleBootstrap.NavigateToDirection(direction);
        });
    }

    private IEnumerator PlayRoomEnterForwardAnimations()
    {
        清空房间墙体动画控制器[] controllers = FindObjectsOfType<清空房间墙体动画控制器>(false);
        float duration = 0f;
        for (int i = 0; i < controllers.Length; i++)
        {
            清空房间墙体动画控制器 controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            duration = Mathf.Max(duration, controller.播放切房间时正向动画());
        }

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
    }

    private void ClearPendingDoorNavigation()
    {
        if (pendingDoorNavigationRoutine != null)
        {
            StopCoroutine(pendingDoorNavigationRoutine);
            pendingDoorNavigationRoutine = null;
        }

        hasPendingDoorNavigationCell = false;
        pendingDoorNavigationCell = default;
        doorExitNavigationLocked = false;
        ClearForcedNavigationFollowerFollow();

        if (pendingDoorNavigationStateSwitcher != null)
        {
            pendingDoorNavigationStateSwitcher.取消选中();
            pendingDoorNavigationStateSwitcher = null;
        }

        gridTriggerNavigationService?.清除();
    }

    private void ClearForcedNavigationFollowerFollow()
    {
        bool wasForceNavigationFollowerFollow = forceNavigationFollowerFollow;
        forceNavigationFollowerFollow = false;
        if (wasForceNavigationFollowerFollow)
        {
            explorationMoveService?.停止跟随(this);
        }
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
        if (BattleBootstrap.IsCurrentRoomEncounterBattleRoom() && !HasLivingEnemies())
        {
            BattleBootstrap.MarkCurrentRoomEncounterCleared();
            BattleBootstrap.ConfigureCurrentRoomDoorExitCells(grid);
        }

        bool switchedFromCombat = currentMode == BattleFlowMode.Combat;
        currentMode = BattleFlowMode.Exploration;
        activeExplorationActionId = ExplorationMoveSkillId;
        waitingForEnemyAction = false;
        currentRoundOrder.Clear();
        upcomingRoundOrders.Clear();
        currentRoundIndex = -1;
        absoluteRoundIndex = -1;
        activeUnit = FindExplorationPlayerUnit();
        ClearActiveSkillMode();
        ClearLockedTargetUnit();
        skillPreviewService?.清空悬停目标(grid);
        if (modeService != null)
        {
            activeUnit = modeService.进入探索模式(
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
                RefreshTimeline);
        }
    }

    private void EnterCombatMode(bool playEnterAnimation)
    {
        ClearPendingDoorNavigation();
        explorationMoveService?.停止全部(this);
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
            int turnIndexBeforeEffects = currentRoundIndex;
            BattleUnit candidate = currentRoundOrder[currentRoundIndex];
            if (candidate != null && candidate.IsAlive)
            {
                activeUnit = candidate;
                FocusCameraOnActiveUnit();
                List<BattleUnit> roundOrderBeforeEffects = new List<BattleUnit>(currentRoundOrder);
                effectTurnResolutionService?.处理回合持有效果(
                    activeUnit,
                    units,
                    battleCamera,
                    FindUnitByInstanceId,
                    ResolveEffectDamagePopupColor);
                CleanupDeadUnits();
                if (activeUnit == null || !activeUnit.IsAlive)
                {
                    currentRoundIndex = ResolveNextRoundIndexAfterTurnOwnerRemoved(roundOrderBeforeEffects, turnIndexBeforeEffects);
                    activeUnit = null;
                    continue;
                }

                int refreshedActiveIndex = currentRoundOrder.IndexOf(activeUnit);
                if (refreshedActiveIndex < 0)
                {
                    currentRoundIndex = ResolveNextRoundIndexAfterTurnOwnerRemoved(roundOrderBeforeEffects, turnIndexBeforeEffects);
                    activeUnit = null;
                    continue;
                }

                currentRoundIndex = refreshedActiveIndex;
                activeUnit.BeginTurn();
                ClearActiveSkillMode();
                RefreshSelectionOutlines();
                RefreshHighlights();
                RefreshActiveUnitUi();
                RefreshTimeline();
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

    private int ResolveNextRoundIndexAfterTurnOwnerRemoved(List<BattleUnit> roundOrderBeforeCleanup, int removedTurnIndex)
    {
        if (roundOrderBeforeCleanup == null)
        {
            return currentRoundOrder.Count;
        }

        for (int i = removedTurnIndex + 1; i < roundOrderBeforeCleanup.Count; i++)
        {
            BattleUnit nextUnit = roundOrderBeforeCleanup[i];
            if (nextUnit == null || !nextUnit.IsAlive)
            {
                continue;
            }

            int currentIndex = currentRoundOrder.IndexOf(nextUnit);
            if (currentIndex >= 0)
            {
                return currentIndex;
            }
        }

        return currentRoundOrder.Count;
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
        RefreshSkillHoverHighlights();
    }

    private void RefreshSkillHoverHighlights()
    {
        grid.ResetSkillPreviewHighlights();
        skillPreviewAreaService?.应用技能悬停预览(
            activeUnit,
            activeSkill,
            skillHoverCell,
            hasSkillHoverPreview,
            skillHoverHasAnyVisibleCells,
            skillHoverValid,
            IsMovementSkillActive(),
            skillPreviewValidColor,
            skillPreviewInvalidColor);
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

    private void HandleCameraReturnInput()
    {
        if (battleCameraController == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            BattleUnit targetUnit = ResolveCurrentIdCameraTarget();
            if (targetUnit == null)
            {
                return;
            }

            battleCameraController.SnapToTarget(targetUnit.transform);
            battleCameraController.StartFollowing(targetUnit.transform, snapImmediately: false);
            cameraSpaceLockActive = true;
            cameraSpaceLockUnit = targetUnit;
            return;
        }

        if (cameraSpaceLockActive && Input.GetKey(KeyCode.Space))
        {
            BattleUnit targetUnit = ResolveCurrentIdCameraTarget();
            if (targetUnit == null)
            {
                battleCameraController.StopFollowing();
                cameraSpaceLockActive = false;
                cameraSpaceLockUnit = null;
                return;
            }

            if (targetUnit != cameraSpaceLockUnit)
            {
                battleCameraController.StartFollowing(targetUnit.transform, snapImmediately: true);
                cameraSpaceLockUnit = targetUnit;
            }

            return;
        }

        if (cameraSpaceLockActive && Input.GetKeyUp(KeyCode.Space))
        {
            battleCameraController.StopFollowing();
            cameraSpaceLockActive = false;
            cameraSpaceLockUnit = null;
        }
    }

    private BattleUnit ResolveCurrentIdCameraTarget()
    {
        string currentId = 界面ID列表.当前ID;
        BattleUnit targetUnit = FindUnitByCharacterId(currentId);
        if (targetUnit != null && targetUnit.IsAlive)
        {
            return targetUnit;
        }

        return null;
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
        ToggleSkillMode(skillId, string.Empty);
    }

    public void ToggleSkillMode(string skillId, string skillSource)
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
            skillTargetingPresentationService?.缓存技能模式旋转锚点(activeUnit, wasSkillModeActive);
            activeSkillId = skillId;
            activeSkillSource = 解析播放技能来源(skillSource, nextSkill);
            activeSkill = nextSkill;
            activeSkillRemainingCastCount = nextSkill.ResolveCastCount();
            queuedActiveSkillTargets.Clear();
            queuedActiveSkillTargetCells.Clear();
            hasSkillHoverPreview = false;
            skillHoverHasAnyVisibleCells = false;
            skillTargetingPresentationService?.开始技能指向引导(
                this,
                activeUnit,
                activeSkill,
                (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析抬手状态名(skill, unit) : string.Empty,
                (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析抬手偏航(skill, unit) : 0f,
                (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析目标选择状态名(skill, unit) : string.Empty,
                (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析目标选择偏航(skill, unit) : 0f,
                unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : string.Empty,
                TryGetMouseWorldPointNullable,
                (unit, skill) =>
                    activeUnit == unit &&
                    activeSkill == skill &&
                    !string.IsNullOrWhiteSpace(activeSkillId) &&
                    string.Equals(activeSkillId, skill.skillId, System.StringComparison.Ordinal));
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
            explorationMoveService?.停止移动音效(this);
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
                explorationMoveService?.停止移动音效(this);
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

    private static string 解析播放技能来源(string skillSource)
    {
        return 解析播放技能来源(skillSource, null);
    }

    private static string 解析播放技能来源(string skillSource, BattleSkillDatabase.SkillEntry skill)
    {
        return BattleSkillDatabase.ResolveSkillSource(skillSource, skill);
    }

    private void UpdateSkillHoverPreview()
    {
        if (!IsSkillModeActive() || activeUnit == null || !activeUnit.IsAlive)
        {
            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells)
            {
                hasSkillHoverPreview = false;
                skillHoverHasAnyVisibleCells = false;
                RefreshSkillHoverHighlights();
            }

            skillPreviewService?.隐藏行动点提示();
            skillPreviewService?.清空悬停目标(grid);
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
                RefreshSkillHoverHighlights();
            }

            skillPreviewService?.隐藏行动点提示();
            skillPreviewService?.清空悬停目标(grid);
            return;
        }

        if (skillTargetingPresentationService != null && !skillTargetingPresentationService.技能目标选择已就绪)
        {
            if (TryGetMouseWorldPoint(out Vector3 introHitPoint))
            {
                skillTargetingPresentationService?.更新技能指向朝向(IsSkillModeActive(), activeUnit, introHitPoint);
            }

            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells || skillHoverValid || skillHoverActionPointCost > 0)
            {
                hasSkillHoverPreview = false;
                skillHoverValid = false;
                skillHoverHasAnyVisibleCells = false;
                skillHoverActionPointCost = 0;
                RefreshSkillHoverHighlights();
            }

            skillPreviewService?.隐藏行动点提示();
            skillPreviewService?.清空悬停目标(grid);
            return;
        }

        bool shouldShowAreaPreview = ShouldShowSkillAreaPreview(activeSkill);
        if (!shouldShowAreaPreview)
        {
            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells)
            {
                hasSkillHoverPreview = false;
                skillHoverHasAnyVisibleCells = false;
                RefreshSkillHoverHighlights();
            }

            skillPreviewService?.隐藏行动点提示();
        }

        skillPreviewService?.更新悬停目标(
            grid,
            battleCamera,
            activeUnit,
            activeSkill,
            IsSkillModeActive(),
            IsPointerBlockedByUi,
            skillPreviewJudgeService != null ? skillPreviewJudgeService.收集悬停技能目标 : null);

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
                RefreshSkillHoverHighlights();
            }

            skillPreviewService?.隐藏行动点提示();
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        skillTargetingPresentationService?.更新技能指向朝向(IsSkillModeActive(), activeUnit, hitPoint);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            if (hasSkillHoverPreview || skillHoverHasAnyVisibleCells || skillHoverValid || skillHoverActionPointCost > 0)
            {
                hasSkillHoverPreview = false;
                skillHoverValid = false;
                skillHoverHasAnyVisibleCells = false;
                skillHoverActionPointCost = 0;
                RefreshSkillHoverHighlights();
            }

            skillPreviewService?.隐藏行动点提示();
            return;
        }

        BattleUnit hoveredUnit = grid.GetUnitAt(hoveredCell);
        bool footprintInside = grid.IsFootprintInside(activeUnit, hoveredCell);
        List<Vector2Int> path = IsMovementSkillActive() && footprintInside ? grid.FindPath(activeUnit, hoveredCell) : null;
        bool canCastAtHover = CanCastSkillAt(activeUnit, hoveredCell, hoveredUnit, activeSkill, path);
        int actionPointCost = canCastAtHover ? GetHoveredSkillActionPointCost(activeUnit, path, activeSkill) : 0;
        bool hasAnyVisibleCells = shouldShowAreaPreview &&
            skillPreviewAreaService != null &&
            skillPreviewAreaService.是否存在可见技能预览格(
                activeUnit,
                activeSkill,
                hoveredCell,
                ShouldShowSkillAreaPreview);

        if (hasSkillHoverPreview &&
            skillHoverCell == hoveredCell &&
            skillHoverValid == canCastAtHover &&
            skillHoverHasAnyVisibleCells == hasAnyVisibleCells &&
            skillHoverActionPointCost == actionPointCost)
        {
            skillPreviewService?.更新行动点提示(
                IsSkillModeActive(),
                skillHoverValid,
                skillHoverActionPointCost,
                GetRemainingTargetSelectionCount(),
                activeUnit,
                skillCostInsufficientColor,
                skillCostNormalColor,
                ResolveOverlayCanvasTransform,
                FindChildByName);
            return;
        }

        hasSkillHoverPreview = hasAnyVisibleCells;
        skillHoverCell = hoveredCell;
        skillHoverValid = canCastAtHover;
        skillHoverHasAnyVisibleCells = hasAnyVisibleCells;
        skillHoverActionPointCost = actionPointCost;
        RefreshSkillHoverHighlights();
        skillPreviewService?.更新行动点提示(
            IsSkillModeActive(),
            skillHoverValid,
            skillHoverActionPointCost,
            GetRemainingTargetSelectionCount(),
            activeUnit,
            skillCostInsufficientColor,
            skillCostNormalColor,
            ResolveOverlayCanvasTransform,
            FindChildByName);
    }

    private bool ShouldShowSkillAreaPreview(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null)
        {
            return false;
        }

        if (skill.skillType != BattleSkillDatabase.SkillType.Area)
        {
            return false;
        }

        if (skillAreaRuleService != null && skillAreaRuleService.是圆轴区域技能(skill))
        {
            return true;
        }

        int width = Mathf.Max(1, skill.effectSize.x);
        int height = Mathf.Max(1, skill.effectSize.y);
        return width > 1 || height > 1;
    }

    private bool IsSkillModeActive()
    {
        return activeSkill != null && !string.IsNullOrWhiteSpace(activeSkillId);
    }

    private void ClearActiveSkillMode()
    {
        BattleUnit unit = activeUnit;
        bool shouldRestoreRotation = !IsExplorationMode && !isResolvingSkillExecution;

        activeSkillId = string.Empty;
        activeSkillSource = string.Empty;
        activeSkill = null;
        activeSkillRemainingCastCount = 0;
        queuedActiveSkillTargets.Clear();
        queuedActiveSkillTargetCells.Clear();
        hasSkillHoverPreview = false;
        skillHoverValid = false;
        skillHoverHasAnyVisibleCells = false;
        skillHoverActionPointCost = 0;
        skillPreviewService?.清空悬停目标(grid);
        skillPreviewService?.隐藏行动点提示();
        skillTargetingPresentationService?.清空技能模式状态(this, shouldRestoreRotation);

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

    private void TryUseActiveSkill(BattleUnit unit, Vector2Int clickedCell, BattleUnit target)
    {
        if (ShouldQueueActiveSkillTargets())
        {
            QueueActiveSkillTarget(unit, clickedCell, target);
            return;
        }

        skillExecutionRoutine = skillExecutionService != null
            ? skillExecutionService.尝试使用当前技能(
                this,
                skillExecutionRoutine,
                unit,
                clickedCell,
                target,
                IsSkillModeActive(),
                isResolvingSkillExecution,
                skillTargetingPresentationService != null && skillTargetingPresentationService.技能目标选择已就绪,
                activeSkillId,
                activeSkillSource,
                activeSkill,
                (caster, cell, clickedTarget, skill) => CanCastSkillAt(caster, cell, clickedTarget, skill, null),
                TryMove,
                GetActiveSkillActionPointCostForExecution,
                GetActiveSkillManaCostForExecution,
                SetSkillExecutionResolvingState,
                FaceTowardTargetUnit,
                FaceTowardTargetCell,
                (caster, skill, resolveAction) => skillPresentationService != null
                    ? skillPresentationService.播放技能动画并在结算点执行(
                        this,
                        caster,
                        skill,
                        resolveAction,
                        (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作状态名(skill, unit) : string.Empty,
                        (skill, unit) => skillActionResolverService != null && skillActionResolverService.解析动作位移补偿(skill, unit),
                        (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作偏航(skill, unit) : 0f,
                        (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析收招偏航(skill, unit) : 0f,
                        unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : string.Empty,
                        PlayTrackedSkillAudioRoutine,
                        ResolveAnimationStateTotalFrames,
                        ResolveSkillResolveDelaySeconds,
                        HitFeelDurationSeconds,
                        HitFeelTimeScale,
                        DefaultFixedDeltaTime)
                    : null,
                (caster, targetUnit, skill) => damageResolutionService?.结算单体技能并显示信息(
                    caster,
                    targetUnit,
                    skill,
                    battleCamera,
                    战斗技能基础结算服务.格式化单位效果调试文本,
                    (source, destination, skillEntry) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能命中率(source, destination, skillEntry)
                        : MinHitChancePercent,
                    (source, destination, skillEntry) => skillCoreResolutionService != null &&
                        skillCoreResolutionService.判定技能命中(source, destination, skillEntry),
                    (source, destination, skillEntry) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能伤害(source, destination, skillEntry, activeSkillSource)
                        : null,
                    (source, destination, skillEntry) => skillCoreResolutionService?.应用附加效果到单位(
                        source,
                        destination,
                        skillEntry,
                        battleCamera,
                        physicalDamageColor),
                    ShowZeroDamagePopup,
                    ShowDamagePopup,
                    target => skillPresentationService?.播放受击反应(
                        target,
                        battleCamera,
                        ResolveDodgeSound,
                        ResolveDodgeSoundPrefab,
                        unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                        unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                        ShouldCompensateGlobalMotionForState),
                    target => PlaySkillHitReaction(target, skill),
                    HandleUnitDefeat,
                    uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                    skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                    damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                    unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                    isCritical => battleInfoTextService != null ? battleInfoTextService.构建暴击信息(isCritical) : string.Empty,
                    (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                    战斗信息文本服务.中性信息颜色,
                    message => battleInfoTextService?.显示消息(message)),
                (caster, targetCellValue, skill) => damageResolutionService?.结算范围技能并显示信息(
                    caster,
                    targetCellValue,
                    skill,
                    battleCamera,
                    skillAreaRuleService != null ? skillAreaRuleService.收集区域技能目标 : null,
                    战斗技能基础结算服务.格式化单位效果调试文本,
                    (source, destination, skillEntry) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能命中率(source, destination, skillEntry)
                        : MinHitChancePercent,
                    (source, destination, skillEntry) => skillCoreResolutionService != null &&
                        skillCoreResolutionService.判定技能命中(source, destination, skillEntry),
                    (source, destination, skillEntry) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能伤害(source, destination, skillEntry, activeSkillSource)
                        : null,
                    (source, destination, skillEntry) => skillCoreResolutionService?.应用附加效果到单位(
                        source,
                        destination,
                        skillEntry,
                        battleCamera,
                        physicalDamageColor),
                    ShowZeroDamagePopup,
                    ShowDamagePopup,
                    target => skillPresentationService?.播放受击反应(
                        target,
                        battleCamera,
                        ResolveDodgeSound,
                        ResolveDodgeSoundPrefab,
                        unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                        unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                        ShouldCompensateGlobalMotionForState),
                    target => PlaySkillHitReaction(target, skill),
                    HandleUnitDefeat,
                    uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                    skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                    damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                    unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                    isCritical => battleInfoTextService != null ? battleInfoTextService.构建范围暴击信息(isCritical) : string.Empty,
                    (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                    战斗信息文本服务.中性信息颜色,
                    message => battleInfoTextService?.显示消息(message)),
                CompleteActiveSkillCastClick,
                RefreshHighlights,
                RefreshTimeline,
                TryEnterPendingExplorationMode)
            : skillExecutionRoutine;
    }

    private bool ShouldQueueActiveSkillTargets()
    {
        return activeSkill != null &&
            activeSkill.ResolveCastCount() > 1 &&
            !IsMovementSkillId(activeSkillId);
    }

    private int GetRemainingTargetSelectionCount()
    {
        if (!ShouldQueueActiveSkillTargets())
        {
            return 1;
        }

        return Mathf.Max(1, activeSkill.ResolveCastCount() - queuedActiveSkillTargetCells.Count);
    }

    private void QueueActiveSkillTarget(BattleUnit unit, Vector2Int clickedCell, BattleUnit target)
    {
        if (unit == null ||
            activeSkill == null ||
            skillExecutionService == null ||
            isResolvingSkillExecution ||
            skillTargetingPresentationService == null ||
            !skillTargetingPresentationService.技能目标选择已就绪 ||
            !CanCastSkillAt(unit, clickedCell, target, activeSkill, null))
        {
            return;
        }

        queuedActiveSkillTargets.Add(target);
        queuedActiveSkillTargetCells.Add(clickedCell);
        hasSkillHoverPreview = false;
        skillHoverValid = false;
        skillHoverHasAnyVisibleCells = false;
        skillHoverActionPointCost = 0;
        skillPreviewService?.清空悬停目标(grid);
        skillPreviewService?.隐藏行动点提示();
        RefreshHighlights();

        if (queuedActiveSkillTargetCells.Count < activeSkill.ResolveCastCount())
        {
            return;
        }

        List<BattleUnit> selectedTargets = new List<BattleUnit>(queuedActiveSkillTargets);
        List<Vector2Int> selectedCells = new List<Vector2Int>(queuedActiveSkillTargetCells);
        BattleSkillDatabase.SkillEntry selectedSkill = activeSkill;
        string selectedSkillId = activeSkillId;
        string selectedSkillSource = activeSkillSource;
        queuedActiveSkillTargets.Clear();
        queuedActiveSkillTargetCells.Clear();

        if (skillExecutionRoutine != null)
        {
            StopCoroutine(skillExecutionRoutine);
        }

        skillExecutionRoutine = StartCoroutine(ExecuteQueuedActiveSkillCasts(
            unit,
            selectedTargets,
            selectedCells,
            selectedSkillId,
            selectedSkillSource,
            selectedSkill));
    }

    private IEnumerator ExecuteQueuedActiveSkillCasts(
        BattleUnit caster,
        List<BattleUnit> targets,
        List<Vector2Int> targetCells,
        string skillId,
        string skillSource,
        BattleSkillDatabase.SkillEntry skill)
    {
        if (caster == null || skill == null || targets == null || targetCells == null)
        {
            yield break;
        }

        int castCount = Mathf.Min(targetCells.Count, targets.Count);
        for (int i = 0; i < castCount; i++)
        {
            bool consumeResource = i == 0;
            bool clearSkillMode = i == castCount - 1;
            Coroutine castRoutine = skillExecutionService.尝试使用当前技能(
                this,
                null,
                caster,
                targetCells[i],
                targets[i],
                true,
                isResolvingSkillExecution,
                true,
                skillId,
                skillSource,
                skill,
                (source, cell, clickedTarget, skillEntry) => CanCastSkillAt(source, cell, clickedTarget, skillEntry, null),
                TryMove,
                GetSkillActionPointCost,
                GetSkillManaCostForExecution,
                SetSkillExecutionResolvingState,
                FaceTowardTargetUnit,
                FaceTowardTargetCell,
                (source, skillEntry, resolveAction) => skillPresentationService != null
                    ? skillPresentationService.播放技能动画并在结算点执行(
                        this,
                        source,
                        skillEntry,
                        resolveAction,
                        (entry, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作状态名(entry, unit) : string.Empty,
                        (entry, unit) => skillActionResolverService != null && skillActionResolverService.解析动作位移补偿(entry, unit),
                        (entry, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作偏航(entry, unit) : 0f,
                        (entry, unit) => skillActionResolverService != null ? skillActionResolverService.解析收招偏航(entry, unit) : 0f,
                        unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : string.Empty,
                        PlayTrackedSkillAudioRoutine,
                        ResolveAnimationStateTotalFrames,
                        ResolveSkillResolveDelaySeconds,
                        HitFeelDurationSeconds,
                        HitFeelTimeScale,
                        DefaultFixedDeltaTime)
                    : null,
                (source, targetUnit, skillEntry) => damageResolutionService?.结算单体技能并显示信息(
                    source,
                    targetUnit,
                    skillEntry,
                    battleCamera,
                    战斗技能基础结算服务.格式化单位效果调试文本,
                    (attacker, defender, currentSkill) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能命中率(attacker, defender, currentSkill)
                        : MinHitChancePercent,
                    (attacker, defender, currentSkill) => skillCoreResolutionService != null &&
                        skillCoreResolutionService.判定技能命中(attacker, defender, currentSkill),
                    (attacker, defender, currentSkill) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能伤害(attacker, defender, currentSkill, skillSource)
                        : null,
                    (attacker, defender, currentSkill) => skillCoreResolutionService?.应用附加效果到单位(
                        attacker,
                        defender,
                        currentSkill,
                        battleCamera,
                        physicalDamageColor),
                    ShowZeroDamagePopup,
                    ShowDamagePopup,
                    target => skillPresentationService?.播放受击反应(
                        target,
                        battleCamera,
                        ResolveDodgeSound,
                        ResolveDodgeSoundPrefab,
                        unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                        unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                        ShouldCompensateGlobalMotionForState),
                    target => PlaySkillHitReaction(target, skillEntry),
                    HandleUnitDefeat,
                    uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                    skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                    damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                    unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                    isCritical => battleInfoTextService != null ? battleInfoTextService.构建暴击信息(isCritical) : string.Empty,
                    (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                    战斗信息文本服务.中性信息颜色,
                    message => battleInfoTextService?.显示消息(message)),
                (source, targetCellValue, skillEntry) => damageResolutionService?.结算范围技能并显示信息(
                    source,
                    targetCellValue,
                    skillEntry,
                    battleCamera,
                    skillAreaRuleService != null ? skillAreaRuleService.收集区域技能目标 : null,
                    战斗技能基础结算服务.格式化单位效果调试文本,
                    (attacker, defender, currentSkill) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能命中率(attacker, defender, currentSkill)
                        : MinHitChancePercent,
                    (attacker, defender, currentSkill) => skillCoreResolutionService != null &&
                        skillCoreResolutionService.判定技能命中(attacker, defender, currentSkill),
                    (attacker, defender, currentSkill) => skillCoreResolutionService != null
                        ? skillCoreResolutionService.计算技能伤害(attacker, defender, currentSkill, skillSource)
                        : null,
                    (attacker, defender, currentSkill) => skillCoreResolutionService?.应用附加效果到单位(
                        attacker,
                        defender,
                        currentSkill,
                        battleCamera,
                        physicalDamageColor),
                    ShowZeroDamagePopup,
                    ShowDamagePopup,
                    target => skillPresentationService?.播放受击反应(
                        target,
                        battleCamera,
                        ResolveDodgeSound,
                        ResolveDodgeSoundPrefab,
                        unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                        unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                        ShouldCompensateGlobalMotionForState),
                    target => PlaySkillHitReaction(target, skillEntry),
                    HandleUnitDefeat,
                    uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                    skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                    damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                    unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                    isCritical => battleInfoTextService != null ? battleInfoTextService.构建范围暴击信息(isCritical) : string.Empty,
                    (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                    战斗信息文本服务.中性信息颜色,
                    message => battleInfoTextService?.显示消息(message)),
                clearSkillMode ? (System.Action)ClearActiveSkillMode : null,
                RefreshHighlights,
                RefreshTimeline,
                clearSkillMode ? (System.Action)TryEnterPendingExplorationMode : null,
                consumeResource,
                clearSkillMode);

            if (castRoutine != null)
            {
                yield return castRoutine;
            }
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

    private int GetSkillActionPointCost(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        return GetSkillActionPointCost(skill);
    }

    private int GetActiveSkillActionPointCostForExecution(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        return IsActiveSkillContinuation(skill) ? 0 : GetSkillActionPointCost(skill);
    }

    private int GetSkillManaCostForExecution(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        return GetSkillManaCost(skill);
    }

    private int GetActiveSkillManaCostForExecution(BattleUnit unit, BattleSkillDatabase.SkillEntry skill)
    {
        return IsActiveSkillContinuation(skill) ? 0 : GetSkillManaCostForExecution(unit, skill);
    }

    private bool IsActiveSkillContinuation(BattleSkillDatabase.SkillEntry skill)
    {
        return skill != null &&
            activeSkill == skill &&
            activeSkillRemainingCastCount > 0 &&
            activeSkillRemainingCastCount < skill.ResolveCastCount();
    }

    private void CompleteActiveSkillCastClick()
    {
        if (activeSkill == null)
        {
            ClearActiveSkillMode();
            return;
        }

        activeSkillRemainingCastCount = Mathf.Max(0, activeSkillRemainingCastCount - 1);
        if (activeSkillRemainingCastCount <= 0)
        {
            ClearActiveSkillMode();
            return;
        }

        hasSkillHoverPreview = false;
        skillHoverValid = false;
        skillHoverHasAnyVisibleCells = false;
        skillHoverActionPointCost = 0;
        skillPreviewService?.清空悬停目标(grid);
        skillPreviewService?.隐藏行动点提示();
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
        yield return ExecuteTargetSkillRoutine(caster, target, skill, string.Empty);
    }

    private IEnumerator ExecuteTargetSkillRoutine(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill, string skillSource)
    {
        if (skillExecutionService == null)
        {
            yield break;
        }

        string resolvedSkillSource = 解析播放技能来源(skillSource, skill);
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
            resolvedSkillSource,
            skill,
            (unit, cell, clickedTarget, activeSkill) => CanCastSkillAt(unit, cell, clickedTarget, activeSkill, null),
            TryMove,
            GetSkillActionPointCost,
            GetSkillManaCostForExecution,
            SetSkillExecutionResolvingState,
            FaceTowardTargetUnit,
            FaceTowardTargetCell,
            (caster, skill, resolveAction) => skillPresentationService != null
                ? skillPresentationService.播放技能动画并在结算点执行(
                    this,
                    caster,
                    skill,
                    resolveAction,
                    (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作状态名(skill, unit) : string.Empty,
                    (skill, unit) => skillActionResolverService != null && skillActionResolverService.解析动作位移补偿(skill, unit),
                    (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作偏航(skill, unit) : 0f,
                    (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析收招偏航(skill, unit) : 0f,
                    unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : string.Empty,
                    PlayTrackedSkillAudioRoutine,
                    ResolveAnimationStateTotalFrames,
                    ResolveSkillResolveDelaySeconds,
                    HitFeelDurationSeconds,
                    HitFeelTimeScale,
                    DefaultFixedDeltaTime)
                : null,
            (source, targetUnit, skillEntry) => damageResolutionService?.结算单体技能并显示信息(
                source,
                targetUnit,
                skillEntry,
                battleCamera,
                战斗技能基础结算服务.格式化单位效果调试文本,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能命中率(attacker, defender, currentSkill)
                    : MinHitChancePercent,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null &&
                    skillCoreResolutionService.判定技能命中(attacker, defender, currentSkill),
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能伤害(attacker, defender, currentSkill, resolvedSkillSource)
                    : null,
                (attacker, defender, currentSkill) => skillCoreResolutionService?.应用附加效果到单位(
                    attacker,
                    defender,
                    currentSkill,
                    battleCamera,
                    physicalDamageColor),
                ShowZeroDamagePopup,
                ShowDamagePopup,
                target => skillPresentationService?.播放受击反应(
                    target,
                    battleCamera,
                    ResolveDodgeSound,
                    ResolveDodgeSoundPrefab,
                    unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                    unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                    ShouldCompensateGlobalMotionForState),
                target => PlaySkillHitReaction(target, skillEntry),
                HandleUnitDefeat,
                uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                isCritical => battleInfoTextService != null ? battleInfoTextService.构建暴击信息(isCritical) : string.Empty,
                (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                战斗信息文本服务.中性信息颜色,
                message => battleInfoTextService?.显示消息(message)),
            (source, targetCellValue, skillEntry) => damageResolutionService?.结算范围技能并显示信息(
                source,
                targetCellValue,
                skillEntry,
                battleCamera,
                skillAreaRuleService != null ? skillAreaRuleService.收集区域技能目标 : null,
                战斗技能基础结算服务.格式化单位效果调试文本,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能命中率(attacker, defender, currentSkill)
                    : MinHitChancePercent,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null &&
                    skillCoreResolutionService.判定技能命中(attacker, defender, currentSkill),
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能伤害(attacker, defender, currentSkill, resolvedSkillSource)
                    : null,
                (attacker, defender, currentSkill) => skillCoreResolutionService?.应用附加效果到单位(
                    attacker,
                    defender,
                    currentSkill,
                    battleCamera,
                    physicalDamageColor),
                ShowZeroDamagePopup,
                ShowDamagePopup,
                target => skillPresentationService?.播放受击反应(
                    target,
                    battleCamera,
                    ResolveDodgeSound,
                    ResolveDodgeSoundPrefab,
                    unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                    unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                    ShouldCompensateGlobalMotionForState),
                target => PlaySkillHitReaction(target, skillEntry),
                HandleUnitDefeat,
                uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                isCritical => battleInfoTextService != null ? battleInfoTextService.构建范围暴击信息(isCritical) : string.Empty,
                (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                战斗信息文本服务.中性信息颜色,
                message => battleInfoTextService?.显示消息(message)),
            ClearActiveSkillMode,
            RefreshHighlights,
            RefreshTimeline,
            TryEnterPendingExplorationMode);
    }

    private IEnumerator ExecuteAreaSkillRoutine(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill)
    {
        yield return ExecuteAreaSkillRoutine(caster, targetCell, skill, string.Empty);
    }

    private IEnumerator ExecuteAreaSkillRoutine(BattleUnit caster, Vector2Int targetCell, BattleSkillDatabase.SkillEntry skill, string skillSource)
    {
        if (skillExecutionService == null)
        {
            yield break;
        }

        string resolvedSkillSource = 解析播放技能来源(skillSource, skill);
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
            resolvedSkillSource,
            skill,
            (unit, cell, clickedTarget, activeSkill) => CanCastSkillAt(unit, cell, clickedTarget, activeSkill, null),
            TryMove,
            GetSkillActionPointCost,
            GetSkillManaCostForExecution,
            SetSkillExecutionResolvingState,
            FaceTowardTargetUnit,
            FaceTowardTargetCell,
            (caster, skill, resolveAction) => skillPresentationService != null
                ? skillPresentationService.播放技能动画并在结算点执行(
                    this,
                    caster,
                    skill,
                    resolveAction,
                    (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作状态名(skill, unit) : string.Empty,
                    (skill, unit) => skillActionResolverService != null && skillActionResolverService.解析动作位移补偿(skill, unit),
                    (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析动作偏航(skill, unit) : 0f,
                    (skill, unit) => skillActionResolverService != null ? skillActionResolverService.解析收招偏航(skill, unit) : 0f,
                    unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : string.Empty,
                    PlayTrackedSkillAudioRoutine,
                    ResolveAnimationStateTotalFrames,
                    ResolveSkillResolveDelaySeconds,
                    HitFeelDurationSeconds,
                    HitFeelTimeScale,
                    DefaultFixedDeltaTime)
                : null,
            (source, targetUnit, skillEntry) => damageResolutionService?.结算单体技能并显示信息(
                source,
                targetUnit,
                skillEntry,
                battleCamera,
                战斗技能基础结算服务.格式化单位效果调试文本,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能命中率(attacker, defender, currentSkill)
                    : MinHitChancePercent,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null &&
                    skillCoreResolutionService.判定技能命中(attacker, defender, currentSkill),
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能伤害(attacker, defender, currentSkill, resolvedSkillSource)
                    : null,
                (attacker, defender, currentSkill) => skillCoreResolutionService?.应用附加效果到单位(
                    attacker,
                    defender,
                    currentSkill,
                    battleCamera,
                    physicalDamageColor),
                ShowZeroDamagePopup,
                ShowDamagePopup,
                target => skillPresentationService?.播放受击反应(
                    target,
                    battleCamera,
                    ResolveDodgeSound,
                    ResolveDodgeSoundPrefab,
                    unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                    unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                    ShouldCompensateGlobalMotionForState),
                target => PlaySkillHitReaction(target, skillEntry),
                HandleUnitDefeat,
                uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                isCritical => battleInfoTextService != null ? battleInfoTextService.构建暴击信息(isCritical) : string.Empty,
                (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                战斗信息文本服务.中性信息颜色,
                message => battleInfoTextService?.显示消息(message)),
            (source, targetCellValue, skillEntry) => damageResolutionService?.结算范围技能并显示信息(
                source,
                targetCellValue,
                skillEntry,
                battleCamera,
                skillAreaRuleService != null ? skillAreaRuleService.收集区域技能目标 : null,
                战斗技能基础结算服务.格式化单位效果调试文本,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能命中率(attacker, defender, currentSkill)
                    : MinHitChancePercent,
                (attacker, defender, currentSkill) => skillCoreResolutionService != null &&
                    skillCoreResolutionService.判定技能命中(attacker, defender, currentSkill),
                (attacker, defender, currentSkill) => skillCoreResolutionService != null
                    ? skillCoreResolutionService.计算技能伤害(attacker, defender, currentSkill, resolvedSkillSource)
                    : null,
                (attacker, defender, currentSkill) => skillCoreResolutionService?.应用附加效果到单位(
                    attacker,
                    defender,
                    currentSkill,
                    battleCamera,
                    physicalDamageColor),
                ShowZeroDamagePopup,
                ShowDamagePopup,
                target => skillPresentationService?.播放受击反应(
                    target,
                    battleCamera,
                    ResolveDodgeSound,
                    ResolveDodgeSoundPrefab,
                    unit => unit != null ? unit.GetDodgeAnimationStateName(ResolveDodgeStateName(unit)) : ResolveDodgeStateName(unit),
                    unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
                    ShouldCompensateGlobalMotionForState),
                target => PlaySkillHitReaction(target, skillEntry),
                HandleUnitDefeat,
                uiUnit => battleInfoTextService != null ? battleInfoTextService.解析单位名(uiUnit, richText: true) : string.Empty,
                skillEntry => battleInfoTextService != null ? battleInfoTextService.解析技能名(skillEntry) : string.Empty,
                damageResult => battleInfoTextService != null ? battleInfoTextService.构建伤害信息文本(damageResult) : string.Empty,
                unitToShow => battleInfoTextService != null ? battleInfoTextService.构建单位死亡信息(unitToShow) : string.Empty,
                isCritical => battleInfoTextService != null ? battleInfoTextService.构建范围暴击信息(isCritical) : string.Empty,
                (content, colorHex) => battleInfoTextService != null ? battleInfoTextService.包装颜色(content, colorHex) : content,
                战斗信息文本服务.中性信息颜色,
                message => battleInfoTextService?.显示消息(message)),
            ClearActiveSkillMode,
            RefreshHighlights,
            RefreshTimeline,
            TryEnterPendingExplorationMode);
    }

    private void ShowDamagePopup(BattleUnit target, CombatDamageResult damageResult)
    {
        damagePopupService?.显示伤害弹字(
            target,
            damageResult,
            battleCamera,
            physicalDamageColor,
            fireDamageColor,
            corruptionDamageColor,
            coldDamageColor);
    }

    private void ShowZeroDamagePopup(BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        damagePopupService?.显示零伤害弹字(
            target,
            skill,
            battleCamera,
            physicalDamageColor);
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

        return GetActiveSkillActionPointCostForExecution(unit, skill);
    }

    private bool IsMovementSkillActive()
    {
        return IsMovementSkillId(activeSkillId);
    }

    private static bool IsMovementSkillId(string skillId)
    {
        return string.Equals(skillId, BattleSkillDatabase.MoveSkillId, System.StringComparison.Ordinal);
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
            BattleAudioUtility.PlayOnce(
                skillActionResolverService != null ? skillActionResolverService.解析动作音效(skill, unit) : null,
                skillActionResolverService != null ? skillActionResolverService.解析动作音效预制体(skill, unit) : null,
                unit,
                battleCamera);
            yield break;
        }

        BattleAudioUtility.PlaybackHandle handle = BattleAudioUtility.StartTracked(
            skillActionResolverService != null ? skillActionResolverService.解析动作音效(skill, unit) : null,
            skillActionResolverService != null ? skillActionResolverService.解析动作音效预制体(skill, unit) : null,
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

    private float ResolveSkillSoundDelaySeconds(BattleSkillDatabase.SkillEntry skill, BattleUnit unit, float clipDuration)
    {
        if (skill == null)
        {
            return 0f;
        }

        int soundDelayFrame = skillActionResolverService != null ? skillActionResolverService.解析音效延迟帧(skill, unit) : 0;
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

    private static Quaternion ResolvePostSkillIdleRotation(
        Quaternion currentRotation,
        float actionYawOffset,
        float postUseYawOffset)
    {
        float idleYawOffset = ResolveIdleYawOffset();
        return currentRotation * Quaternion.Euler(0f, idleYawOffset - actionYawOffset + postUseYawOffset, 0f);
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

    private Vector3? TryGetMouseWorldPointNullable()
    {
        return TryGetMouseWorldPoint(out Vector3 hitPoint)
            ? hitPoint
            : (Vector3?)null;
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

        explorationMoveService?.停止移动音效(this);
        activeUnit.PlayAnimationState(idleStateName, ResolveExplorationIdleCompensateMotion());
    }

    private void PlayExplorationIdleAnimations()
    {
        string idleStateName = ResolveExplorationIdleStateName();
        if (string.IsNullOrWhiteSpace(idleStateName))
        {
            return;
        }

        explorationMoveService?.停止移动音效(this);

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

    private void PlaySkillHitReaction(BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        skillPresentationService?.播放受击反应(
            target,
            battleCamera,
            ResolveHitReactionSound,
            ResolveHitReactionSoundPrefab,
            unit => unit != null ? unit.GetHitReactionAnimationStateName(ResolveHitReactionStateName(unit)) : ResolveHitReactionStateName(unit),
            unit => unit != null ? unit.GetIdleAnimationStateName(ResolveIdleStateName(unit)) : ResolveIdleStateName(unit),
            ShouldCompensateGlobalMotionForState);
        BattleHitEffectUtility.TryPlaySkillHitEffect(target, skill, battleCamera);
    }

    private void RefreshModeMusic()
    {
        BattleMusicRuntime.RefreshForMode(IsExplorationMode);
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


