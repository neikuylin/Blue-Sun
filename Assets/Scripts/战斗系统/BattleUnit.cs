using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleTeam
{
    Player,
    Enemy
}

public class BattleUnit : MonoBehaviour
{
    private const int GridOcclusionTransparentQueue = 3000;
    public sealed class ActiveEffectState
    {
        public string effectId = string.Empty;
        public string sourceCharacterId = string.Empty;
        public int sourceUnitInstanceId;
        public int stackCount = 1;
        public int remainingTurns;
        public EffectDatabase.TurnOwner durationTurnOwner = EffectDatabase.TurnOwner.Target;
    }

    private const string OutlineObjectPrefix = "__Outline_";
    private const string OutlineMaskObjectPrefix = "__OutlineMask_";
    private static readonly Color DefaultOutlineColor = Color.black;
    private const float DefaultOutlineWidth = 0.025f;

    [Header("Identity")]
    public string characterId = string.Empty;
    public string unitName = "Unit";
    public BattleTeam team = BattleTeam.Player;
    public bool isPlayerControlled = true;

    [Header("Stats")]
    public int maxHealth = 10;
    public int maxMana = 5;
    public int moveRange = 4;
    public int moveDistance = 4;
    public int attackRange = 1;
    public int attackDamage = 3;
    public int footprintSize = 1;
    public int strength;
    [SerializeField] private int agility;
    public int intelligence;
    public int maxActionPoints = 4;
    [SerializeField] private int hitRate = 100;
    [SerializeField] private int physicalResistance;
    [SerializeField] private int fireResistance;
    [SerializeField] private int corruptionResistance;
    [SerializeField] private int coldResistance;
    [SerializeField] private int physicalResistancePenetration;
    [SerializeField] private int fireResistancePenetration;
    [SerializeField] private int corruptionResistancePenetration;
    [SerializeField] private int coldResistancePenetration;
    [SerializeField] private int criticalChance = 20;
    [SerializeField] private int criticalDamage = 150;

    [Header("Presentation")]
    public float yawOffset = 0f;
    public bool useAutoVisualAnchor = true;
    public float moveSpeed = 8f;

    [Header("Runtime")]
    public int currentHealth;
    public int currentMana;
    public int currentActionPoints;
    public Vector2Int currentCell;

    private Vector3 anchorOffset;
    private bool initialized;
    private Coroutine moveRoutine;
    private Coroutine timedAnimationRoutine;
    private Transform animationYawCorrectionTarget;
    private Quaternion animationYawCorrectionBaseLocalRotation;
    private Renderer[] cachedRenderers;
    private Color[] originalRendererColors;
    private Renderer[] gridOcclusionRenderers;
    private readonly Dictionary<Renderer, int> gridOcclusionSortingOffsets = new Dictionary<Renderer, int>();
    private Renderer[] outlineRenderers;
    private Material[] outlineMaterials;
    private bool hasLockOutlineState;
    private Color lockOutlineColor = DefaultOutlineColor;
    private float lockOutlineWidth = DefaultOutlineWidth;
    private bool hasPreviewOutlineState;
    private Color previewOutlineColor = DefaultOutlineColor;
    private float previewOutlineWidth = DefaultOutlineWidth;
    private bool animationPositionCompensationEnabled;
    private Transform animationCompensationTarget;
    private Vector3 animationCompensationLocalPosition;
    private Vector3 animationCompensationWorldPosition;
    private BattleGrid owningGrid;
    private readonly List<ActiveEffectState> activeEffects = new List<ActiveEffectState>();

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    public IReadOnlyList<ActiveEffectState> ActiveEffects
    {
        get { return activeEffects; }
    }

    public int Agility
    {
        get { return GetEffectiveAgility(); }
    }

    public int DodgeRate
    {
        get { return GetEffectiveDodgeRate(); }
    }

    public int HitRate
    {
        get { return GetEffectiveHitRate(); }
    }

    public int PhysicalResistance => GetEffectivePhysicalResistance();
    public int FireResistance => GetEffectiveFireResistance();
    public int CorruptionResistance => GetEffectiveCorruptionResistance();
    public int ColdResistance => GetEffectiveColdResistance();
    public int PhysicalResistancePenetration => GetEffectivePhysicalResistancePenetration();
    public int FireResistancePenetration => GetEffectiveFireResistancePenetration();
    public int CorruptionResistancePenetration => GetEffectiveCorruptionResistancePenetration();
    public int ColdResistancePenetration => GetEffectiveColdResistancePenetration();
    public int CriticalChance => GetEffectiveCriticalChance();
    public int CriticalDamage => GetEffectiveCriticalDamage();

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        确保效果特效状态启用脚本();
    }

    private void 确保效果特效状态启用脚本()
    {
        if (GetComponent<效果特效状态启用脚本>() == null)
        {
            gameObject.AddComponent<效果特效状态启用脚本>();
        }
    }

    public Vector3 GetOcclusionRevealAnchorWorldPosition()
    {
        return owningGrid != null ? owningGrid.GetWorldPosition(GetOcclusionAnchorCell()) : GetOcclusionRevealFallbackAnchorWorldPosition();
    }

    public float GetOcclusionDepthKey(Camera cameraToUse)
    {
        return owningGrid != null
            ? owningGrid.GetOcclusionDepthKey(GetOcclusionAnchorCell(), cameraToUse)
            : (cameraToUse != null ? cameraToUse.WorldToScreenPoint(GetOcclusionRevealFallbackAnchorWorldPosition()).y : 0f);
    }

    private Vector2Int GetOcclusionAnchorCell()
    {
        if (owningGrid != null && IsMoving)
        {
            Vector2Int movingCell = owningGrid.WorldToCell(transform.position);
            if (owningGrid.IsInside(movingCell))
            {
                return movingCell;
            }
        }

        return currentCell;
    }

    public Vector3 GetOcclusionRevealCenterWorldPosition()
    {
        return useAutoVisualAnchor ? GetVisualBoundsCenterWorldPosition() : transform.position;
    }

    public string GetIdleAnimationStateName(string fallback = "")
    {
        return fallback;
    }

    public string GetEnterBattleAnimationStateName(string fallback = "")
    {
        return fallback;
    }

    public string GetMoveAnimationStateName(string fallback = "")
    {
        return fallback;
    }

    public string GetHitReactionAnimationStateName(string fallback = "")
    {
        return fallback;
    }

    public string GetDodgeAnimationStateName(string fallback = "")
    {
        return fallback;
    }

    public void Setup(string assignedCharacterId, BattleTeam assignedTeam, string assignedName, Vector2Int startCell)
    {
        characterId = assignedCharacterId;
        team = assignedTeam;
        unitName = assignedName;
        currentCell = startCell;

        if (!initialized)
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            anchorOffset = useAutoVisualAnchor ? transform.position - GetVisualAnchorWorldPosition() : Vector3.zero;
            initialized = true;
        }
    }

    public void SetOwningGrid(BattleGrid grid)
    {
        owningGrid = grid;
    }

    public void ApplyStats(CharacterStatDatabase.StatEntry statEntry)
    {
        if (statEntry == null)
        {
            Debug.LogError($"BattleUnit.ApplyStats missing CharacterStatDatabase entry for '{characterId}' on '{name}'.");
            return;
        }

        maxHealth = statEntry.ResolveMaxHealth();
        maxMana = statEntry.ResolveMaxMana();
        strength = statEntry.strength;
        SetAgilityInternal(statEntry.agility, false);
        intelligence = statEntry.intelligence;
        maxActionPoints = statEntry.ResolveActionPoints();
        hitRate = statEntry.ResolveHitRate();
        physicalResistance = statEntry.ResolvePhysicalResistance();
        fireResistance = statEntry.ResolveFireResistance();
        corruptionResistance = statEntry.ResolveCorruptionResistance();
        coldResistance = statEntry.ResolveColdResistance();
        physicalResistancePenetration = statEntry.ResolvePhysicalResistancePenetration();
        fireResistancePenetration = statEntry.ResolveFireResistancePenetration();
        corruptionResistancePenetration = statEntry.ResolveCorruptionResistancePenetration();
        coldResistancePenetration = statEntry.ResolveColdResistancePenetration();
        criticalChance = statEntry.ResolveCriticalChance();
        criticalDamage = statEntry.ResolveCriticalDamage();
        moveDistance = statEntry.ResolveMoveDistance();
        moveRange = moveDistance;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        currentMana = Mathf.Min(currentMana, maxMana);
        currentActionPoints = Mathf.Min(currentActionPoints, maxActionPoints);
    }

    public void BeginTurn()
    {
        currentActionPoints = GetEffectiveMaxActionPoints();
        NormalizeRuntimeState();
    }

    public bool CanSpendActionPoints(int amount)
    {
        return amount <= 0 || currentActionPoints >= amount;
    }

    public bool SpendActionPoints(int amount)
    {
        if (!CanSpendActionPoints(amount))
        {
            return false;
        }

        currentActionPoints = Mathf.Max(0, currentActionPoints - Mathf.Max(0, amount));
        return true;
    }

    public bool CanSpendMana(int amount)
    {
        return amount <= 0 || currentMana >= amount;
    }

    public bool SpendMana(int amount)
    {
        if (!CanSpendMana(amount))
        {
            return false;
        }

        currentMana = Mathf.Max(0, currentMana - Mathf.Max(0, amount));
        return true;
    }

    public void SetAgility(int value)
    {
        SetAgilityInternal(value, true);
    }

    public void AddAgility(int delta)
    {
        SetAgilityInternal(agility + delta, true);
    }

    public void SetCell(Vector2Int cell, Vector3 worldPosition)
    {
        currentCell = cell;
        transform.position = ResolveAdjustedWorldPosition(worldPosition);
    }

    public float MoveAlongPath(IReadOnlyList<Vector3> worldPositions, Vector2Int destinationCell)
    {
        currentCell = destinationCell;
        if (worldPositions == null || worldPositions.Count == 0)
        {
            IsMoving = false;
            return 0f;
        }

        Vector3[] targets = new Vector3[worldPositions.Count];
        float totalDistance = 0f;
        Vector3 previous = transform.position;
        for (int i = 0; i < worldPositions.Count; i++)
        {
            targets[i] = ResolveAdjustedWorldPosition(worldPositions[i]);
            totalDistance += Vector3.Distance(previous, targets[i]);
            previous = targets[i];
        }

        float speed = Mathf.Max(0.01f, moveSpeed);
        float duration = totalDistance / speed;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        if (duration <= 0.001f)
        {
            transform.position = targets[targets.Length - 1];
            IsMoving = false;
            moveRoutine = null;
            return 0f;
        }

        moveRoutine = StartCoroutine(MoveAlongPathRoutine(targets, speed));
        return duration;
    }

    public void CancelMovement()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        IsMoving = false;
    }

    public void FaceToward(Vector3 worldPosition)
    {
        Vector3 delta = worldPosition - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.001f)
        {
            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg + yawOffset;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    public void ApplyDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (!IsAlive)
        {
            gameObject.SetActive(false);
        }
    }

    public int GetEffectiveStrength()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            strength,
            EffectDatabase.CharacterStatField.Strength,
            usePercentScaling: true,
            treatPercentAsPoints: false));
    }

    public int GetEffectiveAgility()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            agility,
            EffectDatabase.CharacterStatField.Agility,
            usePercentScaling: true,
            treatPercentAsPoints: false));
    }

    public int GetEffectiveIntelligence()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            intelligence,
            EffectDatabase.CharacterStatField.Intelligence,
            usePercentScaling: true,
            treatPercentAsPoints: false));
    }

    public int GetEffectiveMaxActionPoints()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            maxActionPoints,
            EffectDatabase.CharacterStatField.ActionPoints,
            usePercentScaling: true,
            treatPercentAsPoints: false));
    }

    public int GetEffectiveMaxHealth()
    {
        return Mathf.Max(0, maxHealth);
    }

    public int GetEffectiveHitRate()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveHitRateValue(hitRate),
            EffectDatabase.CharacterStatField.HitRate,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveDodgeRate()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveDodgeRateFromAgility(agility),
            EffectDatabase.CharacterStatField.DodgeRate,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectivePhysicalResistance()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistanceValue(physicalResistance),
            EffectDatabase.CharacterStatField.PhysicalResistance,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveFireResistance()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistanceValue(fireResistance),
            EffectDatabase.CharacterStatField.FireResistance,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveCorruptionResistance()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistanceValue(corruptionResistance),
            EffectDatabase.CharacterStatField.CorruptionResistance,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveColdResistance()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistanceValue(coldResistance),
            EffectDatabase.CharacterStatField.ColdResistance,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectivePhysicalResistancePenetration()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistancePenetrationValue(physicalResistancePenetration),
            EffectDatabase.CharacterStatField.PhysicalResistancePenetration,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveFireResistancePenetration()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistancePenetrationValue(fireResistancePenetration),
            EffectDatabase.CharacterStatField.FireResistancePenetration,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveCorruptionResistancePenetration()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistancePenetrationValue(corruptionResistancePenetration),
            EffectDatabase.CharacterStatField.CorruptionResistancePenetration,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveColdResistancePenetration()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveResistancePenetrationValue(coldResistancePenetration),
            EffectDatabase.CharacterStatField.ColdResistancePenetration,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveCriticalChance()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveCriticalChanceValue(criticalChance),
            EffectDatabase.CharacterStatField.CriticalChance,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    public int GetEffectiveCriticalDamage()
    {
        return Mathf.Max(0, ResolveModifiedAttributeValue(
            CharacterStatDatabase.ResolveCriticalDamageValue(criticalDamage),
            EffectDatabase.CharacterStatField.CriticalDamage,
            usePercentScaling: false,
            treatPercentAsPoints: true));
    }

    internal WeaponEnchantmentAttackPower GetWeaponEnchantmentAttackPower(ItemDatabase.WeaponCategory weaponCategory)
    {
        WeaponEnchantmentAttackPower result = new WeaponEnchantmentAttackPower();
        if (!CanApplyWeaponEnchantment(weaponCategory))
        {
            return result;
        }

        EffectDatabase database = EffectDatabase.LoadDefault();
        if (database == null || activeEffects.Count == 0)
        {
            return result;
        }

        for (int effectIndex = 0; effectIndex < activeEffects.Count; effectIndex++)
        {
            ActiveEffectState activeEffect = activeEffects[effectIndex];
            if (activeEffect == null || activeEffect.remainingTurns <= 0 || string.IsNullOrWhiteSpace(activeEffect.effectId))
            {
                continue;
            }

            EffectDatabase.EffectEntry effectEntry = database.FindEntry(activeEffect.effectId);
            if (effectEntry == null || effectEntry.statModifiers == null || effectEntry.statModifiers.Count == 0)
            {
                continue;
            }

            int stackCount = Mathf.Max(1, activeEffect.stackCount);
            for (int modifierIndex = 0; modifierIndex < effectEntry.statModifiers.Count; modifierIndex++)
            {
                EffectDatabase.StatModifier modifier = effectEntry.statModifiers[modifierIndex];
                if (modifier == null || modifier.statField != EffectDatabase.CharacterStatField.WeaponEnchantment)
                {
                    continue;
                }

                float amount = modifier.amount * stackCount;
                switch (modifier.healthDamageType)
                {
                    case EffectDatabase.StatModifier.HealthDamageType.Fire:
                        result.fire += amount;
                        break;
                    case EffectDatabase.StatModifier.HealthDamageType.Corruption:
                        result.corruption += amount;
                        break;
                    case EffectDatabase.StatModifier.HealthDamageType.Cold:
                        result.cold += amount;
                        break;
                    default:
                        result.physical += amount;
                        break;
                }
            }
        }

        return result;
    }

    private static bool CanApplyWeaponEnchantment(ItemDatabase.WeaponCategory weaponCategory)
    {
        return weaponCategory == ItemDatabase.WeaponCategory.OneHanded ||
            weaponCategory == ItemDatabase.WeaponCategory.TwoHanded ||
            weaponCategory == ItemDatabase.WeaponCategory.Bow;
    }

    public void NormalizeRuntimeState()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, GetEffectiveMaxHealth());
        currentActionPoints = Mathf.Clamp(currentActionPoints, 0, GetEffectiveMaxActionPoints());
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        if (currentHealth <= 0 && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public void ApplyCurrentHealthDelta(int delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0, GetEffectiveMaxHealth());
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    public void RestoreCurrentVitals(int health, int mana)
    {
        currentHealth = Mathf.Clamp(health, 0, GetEffectiveMaxHealth());
        currentMana = Mathf.Clamp(mana, 0, maxMana);
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
        else if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public bool ShouldAdvanceEffectOnTurn(BattleUnit actingUnit, ActiveEffectState effectState)
    {
        if (actingUnit == null || effectState == null)
        {
            return false;
        }

        if (effectState.durationTurnOwner == EffectDatabase.TurnOwner.Target)
        {
            return ReferenceEquals(this, actingUnit);
        }

        return effectState.sourceUnitInstanceId != 0 &&
            effectState.sourceUnitInstanceId == actingUnit.GetInstanceID();
    }

    public void ConsumeEffectTurn(ActiveEffectState effectState)
    {
        if (effectState == null || effectState.remainingTurns <= 0)
        {
            return;
        }

        effectState.remainingTurns = Mathf.Max(0, effectState.remainingTurns - 1);
    }

    public void RemoveActiveEffect(ActiveEffectState effectState)
    {
        if (effectState == null)
        {
            return;
        }

        activeEffects.Remove(effectState);
        NormalizeRuntimeState();
    }

    public bool ApplyAttachedEffect(string effectId, int durationTurns, BattleUnit sourceUnit, out EffectDatabase.EffectEntry appliedEffectEntry)
    {
        appliedEffectEntry = null;
        if (string.IsNullOrWhiteSpace(effectId) || durationTurns <= 0)
        {
            return false;
        }

        EffectDatabase database = EffectDatabase.LoadDefault();
        EffectDatabase.EffectEntry effectEntry = database != null ? database.FindEntry(effectId) : null;
        if (effectEntry == null)
        {
            return false;
        }

        ActiveEffectState existing = FindActiveEffect(effectId);
        if (existing == null)
        {
            activeEffects.Add(new ActiveEffectState
            {
                effectId = effectId,
                sourceCharacterId = sourceUnit != null && !string.IsNullOrWhiteSpace(sourceUnit.characterId) ? sourceUnit.characterId : string.Empty,
                sourceUnitInstanceId = sourceUnit != null ? sourceUnit.GetInstanceID() : 0,
                stackCount = 1,
                remainingTurns = durationTurns,
                durationTurnOwner = effectEntry.durationTurnOwner
            });
        }
        else
        {
            if (effectEntry.valueStackRule == EffectDatabase.ValueStackRule.Stackable)
            {
                existing.stackCount = Mathf.Max(1, existing.stackCount + 1);
            }

            if (sourceUnit != null)
            {
                existing.sourceCharacterId = string.IsNullOrWhiteSpace(sourceUnit.characterId) ? string.Empty : sourceUnit.characterId;
                existing.sourceUnitInstanceId = sourceUnit.GetInstanceID();
            }

            existing.durationTurnOwner = effectEntry.durationTurnOwner;
            switch (effectEntry.durationStackRule)
            {
                case EffectDatabase.DurationStackRule.Stackable:
                    existing.remainingTurns = Mathf.Max(0, existing.remainingTurns) + durationTurns;
                    break;
                case EffectDatabase.DurationStackRule.KeepHigher:
                    existing.remainingTurns = Mathf.Max(existing.remainingTurns, durationTurns);
                    break;
                case EffectDatabase.DurationStackRule.NotStackable:
                default:
                    break;
            }
        }

        appliedEffectEntry = effectEntry;
        NormalizeRuntimeState();
        return true;
    }

    private ActiveEffectState FindActiveEffect(string effectId)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveEffectState activeEffect = activeEffects[i];
            if (activeEffect != null && string.Equals(activeEffect.effectId, effectId, System.StringComparison.Ordinal))
            {
                return activeEffect;
            }
        }

        return null;
    }

    private int ResolveModifiedAttributeValue(
        int baseValue,
        EffectDatabase.CharacterStatField targetField,
        bool usePercentScaling,
        bool treatPercentAsPoints)
    {
        float value = baseValue;
        EffectDatabase database = EffectDatabase.LoadDefault();
        if (database == null || activeEffects.Count == 0)
        {
            return Mathf.RoundToInt(value);
        }

        for (int effectIndex = 0; effectIndex < activeEffects.Count; effectIndex++)
        {
            ActiveEffectState activeEffect = activeEffects[effectIndex];
            if (activeEffect == null || activeEffect.remainingTurns <= 0 || string.IsNullOrWhiteSpace(activeEffect.effectId))
            {
                continue;
            }

            EffectDatabase.EffectEntry effectEntry = database.FindEntry(activeEffect.effectId);
            if (effectEntry == null || effectEntry.statModifiers == null || effectEntry.statModifiers.Count == 0)
            {
                continue;
            }

            int stackCount = Mathf.Max(1, activeEffect.stackCount);
            for (int modifierIndex = 0; modifierIndex < effectEntry.statModifiers.Count; modifierIndex++)
            {
                EffectDatabase.StatModifier modifier = effectEntry.statModifiers[modifierIndex];
                if (modifier == null || modifier.statField != targetField || targetField == EffectDatabase.CharacterStatField.TargetHealth)
                {
                    continue;
                }

                for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                {
                    value = ApplyModifierToValue(value, modifier, usePercentScaling, treatPercentAsPoints);
                }
            }
        }

        return Mathf.RoundToInt(value);
    }

    private static float ApplyModifierToValue(
        float currentValue,
        EffectDatabase.StatModifier modifier,
        bool usePercentScaling,
        bool treatPercentAsPoints)
    {
        if (modifier == null)
        {
            return currentValue;
        }

        if (modifier.amountMode == EffectDatabase.StatModifier.AmountMode.Flat)
        {
            return currentValue + modifier.amount;
        }

        if (treatPercentAsPoints || !usePercentScaling)
        {
            return currentValue + modifier.amount;
        }

        return currentValue + (currentValue * modifier.amount / 100f);
    }

    public void PlayTimedAnimation(string stateName, float duration, string idleStateName = "", bool compensateMotion = false)
    {
        if (string.IsNullOrWhiteSpace(stateName) || duration <= 0.01f)
        {
            return;
        }

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (timedAnimationRoutine != null)
        {
            StopCoroutine(timedAnimationRoutine);
        }

        timedAnimationRoutine = StartCoroutine(PlayTimedAnimationRoutine(animator, stateName, duration, idleStateName, compensateMotion));
    }

    public void PlayAnimationState(string stateName, bool compensateMotion = false)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (compensateMotion)
        {
            SetAnimationPositionCompensation(true);
        }
        else
        {
            SetAnimationPositionCompensation(false);
        }

        ApplyAnimationYawCorrection(animator, ResolveGlobalAnimationYawCorrection(stateName));
        animator.Play(stateName, 0, 0f);
    }

    public void PlayAnimationStateForCurrentClipDuration(string stateName, string idleStateName = "", bool compensateMotion = false)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"<color=#FFA500>[战斗受击反应] 单位“{unitName}”已经失活，跳过动画“{stateName}”。通常是多段命中里前一段已经击杀目标，后一段不再播放受击反应。</color>", this);
            return;
        }

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (timedAnimationRoutine != null)
        {
            StopCoroutine(timedAnimationRoutine);
        }

        timedAnimationRoutine = StartCoroutine(PlayAnimationStateForCurrentClipDurationRoutine(animator, stateName, idleStateName, compensateMotion));
    }

    public void SetAnimationPositionCompensation(bool enabled)
    {
        if (!enabled)
        {
            animationPositionCompensationEnabled = false;
            animationCompensationTarget = null;
            return;
        }

        Animator animator = GetComponentInChildren<Animator>(true);
        animationCompensationTarget = animator != null ? animator.transform : transform;
        if (animationCompensationTarget == transform)
        {
            animationCompensationWorldPosition = transform.position;
        }
        else
        {
            animationCompensationLocalPosition = animationCompensationTarget.localPosition;
        }

        animationPositionCompensationEnabled = true;
        ApplyAnimationPositionCompensation();
    }

    public void ApplyYawOffset(float yawOffsetDegrees)
    {
        if (Mathf.Abs(yawOffsetDegrees) <= 0.01f)
        {
            return;
        }

        transform.rotation = transform.rotation * Quaternion.Euler(0f, yawOffsetDegrees, 0f);
    }

    private float ResolveGlobalAnimationYawCorrection(string stateName)
    {
        return BattleAnimationSettingsResolver.ResolveGlobalYawCorrectionForState(stateName, characterId);
    }

    private void ApplyAnimationYawCorrection(Animator animator, float yawCorrection)
    {
        ClearAnimationYawCorrection();
        if (animator == null || Mathf.Abs(yawCorrection) <= 0.01f)
        {
            return;
        }

        animationYawCorrectionTarget = animator.transform;
        animationYawCorrectionBaseLocalRotation = animationYawCorrectionTarget.localRotation;
        animationYawCorrectionTarget.localRotation =
            animationYawCorrectionBaseLocalRotation * Quaternion.Euler(0f, yawCorrection, 0f);
    }

    private void ClearAnimationYawCorrection()
    {
        if (animationYawCorrectionTarget != null)
        {
            animationYawCorrectionTarget.localRotation = animationYawCorrectionBaseLocalRotation;
        }

        animationYawCorrectionTarget = null;
        animationYawCorrectionBaseLocalRotation = Quaternion.identity;
    }

    public void ApplyTint(Color tintColor, float strength)
    {
        CacheRenderers();
        if (cachedRenderers == null || originalRendererColors == null)
        {
            return;
        }

        float clampedStrength = Mathf.Clamp01(strength);
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null || !renderer.material.HasProperty("_Color"))
            {
                continue;
            }

            renderer.material.color = Color.Lerp(originalRendererColors[i], tintColor, clampedStrength);
        }
    }

    public void ClearTint()
    {
        CacheRenderers();
        if (cachedRenderers == null || originalRendererColors == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null || !renderer.material.HasProperty("_Color"))
            {
                continue;
            }

            renderer.material.color = originalRendererColors[i];
        }
    }

    public void SetLockOutline(Color outlineColor, float outlineWidth, bool visible)
    {
        hasLockOutlineState = visible;
        lockOutlineColor = outlineColor;
        lockOutlineWidth = Mathf.Max(0f, outlineWidth);
        ApplyCombinedOutlineState();
    }

    public void ClearLockOutline()
    {
        hasLockOutlineState = false;
        ApplyCombinedOutlineState();
    }

    public void SetPreviewOutline(Color outlineColor, float outlineWidth, bool visible)
    {
        hasPreviewOutlineState = visible;
        previewOutlineColor = outlineColor;
        previewOutlineWidth = Mathf.Max(0f, outlineWidth);
        ApplyCombinedOutlineState();
    }

    public void ClearPreviewOutline()
    {
        hasPreviewOutlineState = false;
        ApplyCombinedOutlineState();
    }

    public void RefreshOutlineBindings()
    {
        outlineRenderers = null;
        outlineMaterials = null;
        EnsureOutlineMaterials();
        ApplyCombinedOutlineState();
    }

    public int FootprintRadius
    {
        get { return Mathf.Max(0, footprintSize / 2); }
    }

    private IEnumerator PlayTimedAnimationRoutine(Animator animator, string stateName, float duration, string idleStateName, bool compensateMotion)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            if (compensateMotion)
            {
                SetAnimationPositionCompensation(false);
            }
            timedAnimationRoutine = null;
            yield break;
        }

        if (compensateMotion)
        {
            SetAnimationPositionCompensation(true);
        }

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        ApplyAnimationYawCorrection(animator, ResolveGlobalAnimationYawCorrection(stateName));
        animator.Play(stateName, 0, 0f);

        yield return new WaitForSeconds(duration);

        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            ApplyAnimationYawCorrection(animator, ResolveGlobalAnimationYawCorrection(idleStateName));
            animator.Play(idleStateName, 0, 0f);
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            ClearAnimationYawCorrection();
            animator.Play(previousStateHash, 0, 0f);
        }

        if (compensateMotion)
        {
            SetAnimationPositionCompensation(false);
        }

        timedAnimationRoutine = null;
    }

    private IEnumerator PlayAnimationStateForCurrentClipDurationRoutine(Animator animator, string stateName, string idleStateName, bool compensateMotion)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            if (compensateMotion)
            {
                SetAnimationPositionCompensation(false);
            }
            timedAnimationRoutine = null;
            yield break;
        }

        if (compensateMotion)
        {
            SetAnimationPositionCompensation(true);
        }

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        ApplyAnimationYawCorrection(animator, ResolveGlobalAnimationYawCorrection(stateName));
        animator.Play(stateName, 0, 0f);

        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float duration = currentState.length;
        if (duration > 0.01f)
        {
            yield return new WaitForSeconds(duration);
        }

        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            ApplyAnimationYawCorrection(animator, ResolveGlobalAnimationYawCorrection(idleStateName));
            animator.Play(idleStateName, 0, 0f);
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            ClearAnimationYawCorrection();
            animator.Play(previousStateHash, 0, 0f);
        }

        if (compensateMotion)
        {
            SetAnimationPositionCompensation(false);
        }

        timedAnimationRoutine = null;
    }

    private void SetAgilityInternal(int value, bool notifyTurnSystem)
    {
        agility = value;

        if (!notifyTurnSystem)
        {
            return;
        }

        BattleTurnSystem turnSystem = FindObjectOfType<BattleTurnSystem>();
        if (turnSystem != null)
        {
            turnSystem.NotifyUnitInitiativeChanged(this);
        }
    }

    private void LateUpdate()
    {
        ApplyAnimationPositionCompensation();
        ApplyGridOcclusionRendererSorting();
    }

    private void ApplyAnimationPositionCompensation()
    {
        if (!animationPositionCompensationEnabled || animationCompensationTarget == null)
        {
            return;
        }

        if (animationCompensationTarget == transform)
        {
            transform.position = animationCompensationWorldPosition;
            return;
        }

        animationCompensationTarget.localPosition = animationCompensationLocalPosition;
    }

    private Vector3 ResolveAdjustedWorldPosition(Vector3 worldPosition)
    {
        return worldPosition + anchorOffset;
    }

    private IEnumerator MoveAlongPathRoutine(IReadOnlyList<Vector3> targets, float speed)
    {
        IsMoving = true;
        for (int pathIndex = 0; pathIndex < targets.Count; pathIndex++)
        {
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = targets[pathIndex];
            float segmentDistance = Vector3.Distance(startPosition, targetPosition);
            if (segmentDistance <= 0.001f)
            {
                transform.position = targetPosition;
                continue;
            }

            FaceToward(targetPosition);

            float segmentDuration = segmentDistance / speed;
            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
        }

        IsMoving = false;
        moveRoutine = null;
    }

    private Vector3 GetVisualAnchorWorldPosition()
    {
        if (!TryGetVisualBounds(out Bounds combinedBounds))
        {
            return transform.position;
        }

        return new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
    }

    private Vector3 GetOcclusionRevealFallbackAnchorWorldPosition()
    {
        return useAutoVisualAnchor ? GetVisualAnchorWorldPosition() : transform.position;
    }

    private Vector3 GetVisualBoundsCenterWorldPosition()
    {
        return TryGetVisualBounds(out Bounds combinedBounds) ? combinedBounds.center : transform.position;
    }

    private bool TryGetVisualBounds(out Bounds combinedBounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Renderer> validRenderers = new List<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            validRenderers.Add(renderer);
        }

        if (validRenderers.Count == 0)
        {
            combinedBounds = default;
            return false;
        }

        combinedBounds = validRenderers[0].bounds;
        for (int i = 1; i < validRenderers.Count; i++)
        {
            combinedBounds.Encapsulate(validRenderers[i].bounds);
        }

        return true;
    }

    private void ApplyGridOcclusionRendererSorting()
    {
        if (owningGrid == null)
        {
            return;
        }

        Camera cameraToUse = Camera.main;
        float depthKey = GetOcclusionDepthKey(cameraToUse);
        int baseSortingOrder = BattleGrid.ResolveOcclusionSortingOrder(depthKey);

        if (gridOcclusionRenderers == null || gridOcclusionRenderers.Length == 0)
        {
            CacheGridOcclusionRenderers();
        }

        for (int i = 0; i < gridOcclusionRenderers.Length; i++)
        {
            Renderer renderer = gridOcclusionRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            int relativeOffset = gridOcclusionSortingOffsets.TryGetValue(renderer, out int offset) ? offset : 0;
            renderer.sortingOrder = baseSortingOrder + relativeOffset;
            EnsureRendererUsesGridOcclusionQueue(renderer);
        }
    }

    private void CacheGridOcclusionRenderers()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> validRenderers = new List<Renderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (!ShouldUseRendererForGridOcclusion(renderer))
            {
                continue;
            }

            validRenderers.Add(renderer);
        }

        gridOcclusionRenderers = validRenderers.ToArray();
        gridOcclusionSortingOffsets.Clear();
        if (gridOcclusionRenderers.Length == 0)
        {
            return;
        }

        int baseSortingOrder = gridOcclusionRenderers[0] != null ? gridOcclusionRenderers[0].sortingOrder : 0;
        for (int i = 0; i < gridOcclusionRenderers.Length; i++)
        {
            Renderer renderer = gridOcclusionRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            gridOcclusionSortingOffsets[renderer] = renderer.sortingOrder - baseSortingOrder;
            EnsureRendererUsesGridOcclusionQueue(renderer);
        }
    }

    private static bool ShouldUseRendererForGridOcclusion(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
    }

    private static void EnsureRendererUsesGridOcclusionQueue(Renderer renderer)
    {
        if (!Application.isPlaying || renderer == null)
        {
            return;
        }

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            int targetQueue = ResolveGridOcclusionRenderQueue(renderer, material.renderQueue);
            if (material.renderQueue < targetQueue)
            {
                material.renderQueue = targetQueue;
            }
        }

        renderer.materials = materials;
    }

    private static int ResolveGridOcclusionRenderQueue(Renderer renderer, int currentRenderQueue)
    {
        if (renderer == null)
        {
            return GridOcclusionTransparentQueue;
        }

        string rendererName = renderer.name;
        if (!string.IsNullOrEmpty(rendererName) &&
            rendererName.StartsWith(OutlineObjectPrefix, System.StringComparison.Ordinal))
        {
            return GridOcclusionTransparentQueue + 2;
        }

        if (!string.IsNullOrEmpty(rendererName) &&
            rendererName.StartsWith(OutlineMaskObjectPrefix, System.StringComparison.Ordinal))
        {
            return GridOcclusionTransparentQueue + 1;
        }

        return Mathf.Max(GridOcclusionTransparentQueue, currentRenderQueue);
    }

    private void CacheRenderers()
    {
        if (cachedRenderers != null && originalRendererColors != null && cachedRenderers.Length == originalRendererColors.Length)
        {
            return;
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> filteredRenderers = new List<Renderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null || renderer.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            filteredRenderers.Add(renderer);
        }

        cachedRenderers = filteredRenderers.ToArray();
        originalRendererColors = new Color[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer != null && renderer.material.HasProperty("_Color"))
            {
                originalRendererColors[i] = renderer.material.color;
            }
            else
            {
                originalRendererColors[i] = Color.white;
            }
        }
    }

    private void EnsureOutlineMaterials()
    {
        if (outlineRenderers != null && outlineMaterials != null && outlineRenderers.Length == outlineMaterials.Length && outlineRenderers.Length > 0)
        {
            return;
        }

        List<Renderer> renderers = new List<Renderer>();
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null || renderer.gameObject == gameObject)
            {
                continue;
            }

            if (!renderer.name.StartsWith(OutlineObjectPrefix, System.StringComparison.Ordinal))
            {
                continue;
            }

            renderers.Add(renderer);
        }

        outlineRenderers = renderers.ToArray();
        outlineMaterials = new Material[outlineRenderers.Length];
        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            Renderer renderer = outlineRenderers[i];
            if (renderer != null)
            {
                outlineMaterials[i] = renderer.material;
                outlineMaterials[i].SetColor("_OutlineColor", DefaultOutlineColor);
                outlineMaterials[i].SetFloat("_OutlineWidth", DefaultOutlineWidth);
            }
        }
    }

    private void ApplyCombinedOutlineState()
    {
        EnsureOutlineMaterials();
        if (outlineRenderers == null || outlineMaterials == null)
        {
            return;
        }

        Color resolvedColor = DefaultOutlineColor;
        float resolvedWidth = DefaultOutlineWidth;

        if (hasLockOutlineState)
        {
            resolvedColor = lockOutlineColor;
            resolvedWidth = lockOutlineWidth;
        }

        if (hasPreviewOutlineState)
        {
            resolvedColor = previewOutlineColor;
            resolvedWidth = previewOutlineWidth;
        }

        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            Material material = outlineMaterials[i];
            if (material == null)
            {
                continue;
            }

            material.SetColor("_OutlineColor", resolvedColor);
            material.SetFloat("_OutlineWidth", resolvedWidth);
        }
    }
}
