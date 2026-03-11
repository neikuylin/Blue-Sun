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
    public int attackRange = 1;
    public int attackDamage = 3;
    public int footprintSize = 1;
    public int strength;
    [SerializeField] private int agility;
    public int intelligence;
    public int endurance;

    [Header("Presentation")]
    public float yawOffset = 0f;
    public Vector2Int cellOffset = Vector2Int.zero;
    public Vector3 worldOffset = Vector3.zero;
    public bool useAutoVisualAnchor = true;

    [Header("Runtime")]
    public int currentHealth;
    public Vector2Int currentCell;

    private Vector3 anchorOffset;
    private bool initialized;

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    public int Agility
    {
        get { return agility; }
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
            anchorOffset = useAutoVisualAnchor ? transform.position - GetVisualAnchorWorldPosition() : Vector3.zero;
            initialized = true;
        }
    }

    public void ApplyStats(CharacterStatDatabase.StatEntry statEntry)
    {
        if (statEntry == null)
        {
            strength = 0;
            SetAgilityInternal(0, false);
            intelligence = 0;
            endurance = 0;
            return;
        }

        strength = statEntry.strength;
        SetAgilityInternal(statEntry.agility, false);
        intelligence = statEntry.intelligence;
        endurance = statEntry.endurance;
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
        Vector3 adjustedWorldPosition = worldPosition + new Vector3(cellOffset.x, 0f, cellOffset.y) + worldOffset;
        transform.position = adjustedWorldPosition + anchorOffset;
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
