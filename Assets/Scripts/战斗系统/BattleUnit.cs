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

    [Header("Animation Bindings")]
    [SerializeField] private string idleStateName = string.Empty;
    [SerializeField] private string enterBattleStateName = string.Empty;
    [SerializeField] private string moveStateName = string.Empty;
    [SerializeField] private string hitReactionStateName = string.Empty;
    [SerializeField] private string dodgeStateName = string.Empty;

    [Header("Runtime")]
    public int currentHealth;
    public int currentMana;
    public int currentActionPoints;
    public Vector2Int currentCell;

    private Vector3 anchorOffset;
    private bool initialized;
    private Coroutine moveRoutine;
    private Coroutine timedAnimationRoutine;
    private Renderer[] cachedRenderers;
    private Color[] originalRendererColors;
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

    public void ConfigureAnimationBindings(BattleCharacterBindingDatabase.BindingEntry binding)
    {
        idleStateName = string.Empty;
        enterBattleStateName = string.Empty;
        moveStateName = string.Empty;
        hitReactionStateName = string.Empty;
        dodgeStateName = string.Empty;
    }

    public string GetIdleAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(idleStateName) ? fallback : idleStateName;
    }

    public string GetEnterBattleAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(enterBattleStateName) ? fallback : enterBattleStateName;
    }

    public string GetMoveAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(moveStateName) ? fallback : moveStateName;
    }

    public string GetHitReactionAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(hitReactionStateName) ? fallback : hitReactionStateName;
    }

    public string GetDodgeAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(dodgeStateName) ? fallback : dodgeStateName;
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

        animator.Play(stateName, 0, 0f);
    }

    public void PlayAnimationStateForCurrentClipDuration(string stateName, string idleStateName = "", bool compensateMotion = false)
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
        animator.Play(stateName, 0, 0f);

        yield return new WaitForSeconds(duration);

        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            animator.Play(idleStateName, 0, 0f);
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
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
            animator.Play(idleStateName, 0, 0f);
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
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
            return transform.position;
        }

        Bounds combinedBounds = validRenderers[0].bounds;
        for (int i = 1; i < validRenderers.Count; i++)
        {
            combinedBounds.Encapsulate(validRenderers[i].bounds);
        }

        return new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
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
