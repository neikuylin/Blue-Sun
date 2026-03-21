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
    [SerializeField] private string combatArtLeftAimStateName = string.Empty;
    [SerializeField] private string combatArtRightAimStateName = string.Empty;

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

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    public int Agility
    {
        get { return agility; }
    }

    public int DodgeRate
    {
        get { return CharacterStatDatabase.ResolveDodgeRateFromAgility(agility); }
    }

    public int HitRate
    {
        get { return CharacterStatDatabase.ResolveHitRateValue(hitRate); }
    }

    public bool IsMoving { get; private set; }

    public void ConfigureAnimationBindings(BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (binding == null)
        {
            idleStateName = string.Empty;
            enterBattleStateName = string.Empty;
            moveStateName = string.Empty;
            hitReactionStateName = string.Empty;
            dodgeStateName = string.Empty;
            combatArtLeftAimStateName = string.Empty;
            combatArtRightAimStateName = string.Empty;
            return;
        }

        idleStateName = binding.idleStateName ?? string.Empty;
        enterBattleStateName = binding.enterBattleStateName ?? string.Empty;
        moveStateName = binding.moveStateName ?? string.Empty;
        hitReactionStateName = binding.hitReactionStateName ?? string.Empty;
        dodgeStateName = binding.dodgeStateName ?? string.Empty;
        combatArtLeftAimStateName = binding.combatArtLeftAimStateName ?? string.Empty;
        combatArtRightAimStateName = binding.combatArtRightAimStateName ?? string.Empty;
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

    public string GetCombatArtLeftAimAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(combatArtLeftAimStateName) ? fallback : combatArtLeftAimStateName;
    }

    public string GetCombatArtRightAimAnimationStateName(string fallback = "")
    {
        return string.IsNullOrWhiteSpace(combatArtRightAimStateName) ? fallback : combatArtRightAimStateName;
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
        moveDistance = statEntry.ResolveMoveDistance();
        moveRange = moveDistance;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        currentMana = Mathf.Min(currentMana, maxMana);
        currentActionPoints = Mathf.Min(currentActionPoints, maxActionPoints);
    }

    public void BeginTurn()
    {
        currentActionPoints = maxActionPoints;
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

    public void PlayTimedAnimation(string stateName, float duration, string idleStateName = "")
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

        timedAnimationRoutine = StartCoroutine(PlayTimedAnimationRoutine(animator, stateName, duration, idleStateName));
    }

    public void PlayAnimationState(string stateName)
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

        animator.Play(stateName, 0, 0f);
    }

    public void PlayAnimationStateForCurrentClipDuration(string stateName, string idleStateName = "")
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

        timedAnimationRoutine = StartCoroutine(PlayAnimationStateForCurrentClipDurationRoutine(animator, stateName, idleStateName));
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

    public int FootprintRadius
    {
        get { return Mathf.Max(0, footprintSize / 2); }
    }

    private IEnumerator PlayTimedAnimationRoutine(Animator animator, string stateName, float duration, string idleStateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            timedAnimationRoutine = null;
            yield break;
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

        timedAnimationRoutine = null;
    }

    private IEnumerator PlayAnimationStateForCurrentClipDurationRoutine(Animator animator, string stateName, string idleStateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            timedAnimationRoutine = null;
            yield break;
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
        if (renderers.Length == 0)
        {
            return transform.position;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
    }

    private void CacheRenderers()
    {
        if (cachedRenderers != null && originalRendererColors != null && cachedRenderers.Length == originalRendererColors.Length)
        {
            return;
        }

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
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
}
