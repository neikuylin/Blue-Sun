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
    public int moveRange = 4;
    public int moveDistance = 4;
    public int attackRange = 1;
    public int attackDamage = 3;
    public int footprintSize = 1;
    public int strength;
    [SerializeField] private int agility;
    public int intelligence;
    public int maxActionPoints = 4;

    [Header("Presentation")]
    public float yawOffset = 0f;
    public Vector2Int cellOffset = Vector2Int.zero;
    public Vector3 worldOffset = Vector3.zero;
    public bool useAutoVisualAnchor = true;
    public float moveSpeed = 8f;

    [Header("Runtime")]
    public int currentHealth;
    public int currentActionPoints;
    public Vector2Int currentCell;

    private Vector3 anchorOffset;
    private bool initialized;
    private Coroutine moveRoutine;

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    public int Agility
    {
        get { return agility; }
    }

    public bool IsMoving { get; private set; }

    public void Setup(string assignedCharacterId, BattleTeam assignedTeam, string assignedName, Vector2Int startCell)
    {
        characterId = assignedCharacterId;
        team = assignedTeam;
        unitName = assignedName;
        currentCell = startCell;

        if (!initialized)
        {
            currentHealth = maxHealth;
            anchorOffset = useAutoVisualAnchor ? transform.position - GetVisualAnchorWorldPosition() : Vector3.zero;
            initialized = true;
        }
    }

    public void ApplyStats(CharacterStatDatabase.StatEntry statEntry)
    {
        if (statEntry == null)
        {
            maxHealth = 50;
            strength = 0;
            SetAgilityInternal(0, false);
            intelligence = 0;
            maxActionPoints = 4;
            moveDistance = 3;
            moveRange = moveDistance;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            currentActionPoints = Mathf.Min(currentActionPoints, maxActionPoints);
            return;
        }

        maxHealth = statEntry.ResolveMaxHealth();
        strength = statEntry.strength;
        SetAgilityInternal(statEntry.agility, false);
        intelligence = statEntry.intelligence;
        maxActionPoints = statEntry.ResolveActionPoints();
        moveDistance = statEntry.ResolveMoveDistance();
        moveRange = moveDistance;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
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

    public int FootprintRadius
    {
        get { return Mathf.Max(0, footprintSize / 2); }
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
        Vector3 adjustedWorldPosition = worldPosition + new Vector3(cellOffset.x, 0f, cellOffset.y) + worldOffset;
        return adjustedWorldPosition + anchorOffset;
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
}
