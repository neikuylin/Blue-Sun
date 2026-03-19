using System.Collections.Generic;
using UnityEngine;

public sealed class BattleUnitFactory
{
    private readonly string idleStateName;
    private readonly float idleYawOffset;
    private readonly BattleCharacterBindingDatabase characterBindingDatabase;
    private readonly CharacterStatDatabase characterStatDatabase;
    private readonly Transform runtimeRoot;
    private readonly BattleGrid grid;
    private readonly Vector3 placeholderScale;
    private readonly Color playerPlaceholderColor;
    private readonly Color enemyPlaceholderColor;

    public BattleUnitFactory(
        string idleStateName,
        float idleYawOffset,
        BattleCharacterBindingDatabase characterBindingDatabase,
        CharacterStatDatabase characterStatDatabase,
        Transform runtimeRoot,
        BattleGrid grid,
        Vector3 placeholderScale,
        Color playerPlaceholderColor,
        Color enemyPlaceholderColor)
    {
        this.idleStateName = idleStateName;
        this.idleYawOffset = idleYawOffset;
        this.characterBindingDatabase = characterBindingDatabase;
        this.characterStatDatabase = characterStatDatabase;
        this.runtimeRoot = runtimeRoot;
        this.grid = grid;
        this.placeholderScale = placeholderScale;
        this.playerPlaceholderColor = playerPlaceholderColor;
        this.enemyPlaceholderColor = enemyPlaceholderColor;
    }

    public List<BattleUnit> CreatePlayers(
        IReadOnlyList<CharacterSelectionState.SlotSelection> playerSelections,
        Vector2Int playerSpawnOrigin,
        Vector2Int playerSpawnSpacing)
    {
        List<BattleUnit> units = new List<BattleUnit>();
        if (playerSelections == null)
        {
            return units;
        }

        for (int i = 0; i < playerSelections.Count; i++)
        {
            CharacterSelectionState.SlotSelection selection = playerSelections[i];
            if (string.IsNullOrWhiteSpace(selection.characterId))
            {
                continue;
            }

            Vector2Int startCell = GetPlayerSpawnCell(i, playerSpawnOrigin, playerSpawnSpacing);
            BattleCharacterBindingDatabase.BindingEntry binding = FindBinding(selection.characterId);
            CharacterStatDatabase.StatEntry statEntry = FindStats(selection.characterId);
            if (statEntry == null)
            {
                Debug.LogError($"BattleUnitFactory: missing CharacterStatDatabase entry for player '{selection.characterId}'.");
                continue;
            }

            GameObject unitObject = CreateUnitObject(
                selection.characterId,
                binding,
                grid.GetWorldPosition(startCell),
                playerPlaceholderColor);
            if (unitObject == null)
            {
                continue;
            }

            BattleUnit unit = EnsureBattleUnit(unitObject);
            unit.moveRange = 4;
            unit.attackRange = 1;
            unit.attackDamage = 5;
            unit.footprintSize = 3;
            unit.yawOffset = 0f;
            unit.useAutoVisualAnchor = binding != null && binding.useAutoVisualAnchor;
            unit.ApplyStats(statEntry);
            unit.Setup(selection.characterId, BattleTeam.Player, ResolveDisplayName(selection.characterId, binding), startCell);
            unit.isPlayerControlled = true;
            unit.SetCell(startCell, grid.GetWorldPosition(startCell));
            unit.FaceToward(grid.GetWorldPosition(startCell + Vector2Int.right));
            unit.PlayAnimationState(idleStateName);
            unit.ApplyYawOffset(idleYawOffset);
            grid.RegisterUnit(unit);
            units.Add(unit);
        }

        return units;
    }

    public List<BattleUnit> CreateEnemies(IReadOnlyList<BattleBootstrap.EnemySpawnEntry> enemyEntries)
    {
        List<BattleUnit> units = new List<BattleUnit>();
        if (enemyEntries == null)
        {
            return units;
        }

        for (int i = 0; i < enemyEntries.Count; i++)
        {
            BattleBootstrap.EnemySpawnEntry enemyEntry = enemyEntries[i];
            if (enemyEntry == null || string.IsNullOrWhiteSpace(enemyEntry.enemyId))
            {
                continue;
            }

            Vector2Int spawnCell = enemyEntry.spawnCell;
            BattleCharacterBindingDatabase.BindingEntry binding = FindBinding(enemyEntry.enemyId);
            CharacterStatDatabase.StatEntry statEntry = FindStats(enemyEntry.enemyId);
            if (statEntry == null)
            {
                Debug.LogError($"BattleUnitFactory: missing CharacterStatDatabase entry for enemy '{enemyEntry.enemyId}'.");
                continue;
            }

            Color placeholderColor = enemyEntry.team == BattleTeam.Enemy ? enemyPlaceholderColor : playerPlaceholderColor;
            GameObject enemyObject = CreateUnitObject(
                enemyEntry.enemyId,
                binding,
                grid.GetWorldPosition(spawnCell),
                placeholderColor);
            if (enemyObject == null)
            {
                continue;
            }

            enemyObject.name = enemyEntry.enemyId + "_" + i;

            BattleUnit unit = EnsureBattleUnit(enemyObject);
            unit.attackRange = 1;
            unit.attackDamage = 2;
            unit.footprintSize = 3;
            unit.yawOffset = 0f;
            unit.useAutoVisualAnchor = binding != null && binding.useAutoVisualAnchor;
            unit.ApplyStats(statEntry);
            unit.Setup(enemyEntry.enemyId, enemyEntry.team, ResolveDisplayName(enemyEntry.enemyId, binding), spawnCell);
            unit.isPlayerControlled = enemyEntry.isPlayerControlled;
            unit.SetCell(spawnCell, grid.GetWorldPosition(spawnCell));
            Vector2Int facingCell = enemyEntry.team == BattleTeam.Enemy ? spawnCell + Vector2Int.left : spawnCell + Vector2Int.right;
            unit.FaceToward(grid.GetWorldPosition(facingCell));
            unit.PlayAnimationState(idleStateName);
            unit.ApplyYawOffset(idleYawOffset);
            grid.RegisterUnit(unit);
            units.Add(unit);
        }

        return units;
    }

    private GameObject CreateUnitObject(
        string characterId,
        BattleCharacterBindingDatabase.BindingEntry binding,
        Vector3 worldPosition,
        Color placeholderColor)
    {
        if (binding != null && binding.modelPrefab != null)
        {
            GameObject instance = Object.Instantiate(binding.modelPrefab, worldPosition, Quaternion.identity, runtimeRoot);
            instance.name = characterId + "_Unit";
            ApplyBindingScale(instance, binding);
            ApplyAnimatorBinding(instance, binding);
            return instance;
        }

        return CreatePlaceholderUnitRoot(characterId + "_Placeholder", worldPosition, placeholderColor);
    }

    private static void ApplyAnimatorBinding(GameObject instance, BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (instance == null || binding == null || binding.animatorController == null)
        {
            return;
        }

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        Animator sourceAnimator = binding.modelPrefab != null
            ? binding.modelPrefab.GetComponentInChildren<Animator>(true)
            : null;

        if (animator == null)
        {
            animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }
        }

        if (sourceAnimator != null && animator.avatar == null && sourceAnimator.avatar != null)
        {
            animator.avatar = sourceAnimator.avatar;
        }

        animator.runtimeAnimatorController = binding.animatorController;
        animator.enabled = true;

        if (sourceAnimator == null)
        {
            Debug.LogWarning($"BattleUnitFactory: model '{binding.modelPrefab?.name ?? instance.name}' has no Animator component. Added Animator to '{instance.name}', but you may still need to assign a valid Avatar on the source model import settings.");
        }
        else if (animator.avatar == null)
        {
            Debug.LogWarning($"BattleUnitFactory: Animator bound for '{instance.name}', but no Avatar was found. Humanoid animations may not play correctly.");
        }
    }

    private static void ApplyBindingScale(GameObject instance, BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (instance == null || binding == null)
        {
            return;
        }

        Vector3 configuredScale = binding.modelScale;
        if (configuredScale == Vector3.zero)
        {
            configuredScale = Vector3.one;
        }

        Vector3 prefabScale = instance.transform.localScale;
        instance.transform.localScale = new Vector3(
            prefabScale.x * configuredScale.x,
            prefabScale.y * configuredScale.y,
            prefabScale.z * configuredScale.z);
    }

    private GameObject CreatePlaceholderUnitRoot(string rootName, Vector3 worldPosition, Color color)
    {
        GameObject root = new GameObject(rootName);
        root.transform.SetParent(runtimeRoot, false);
        root.transform.position = worldPosition;
        root.transform.localScale = Vector3.one;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "UnitVisual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = placeholderScale;

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        return root;
    }

    private static BattleUnit EnsureBattleUnit(GameObject target)
    {
        BattleUnit unit = target.GetComponent<BattleUnit>();
        if (unit == null)
        {
            unit = target.AddComponent<BattleUnit>();
        }

        return unit;
    }

    private static Vector2Int GetPlayerSpawnCell(int index, Vector2Int playerSpawnOrigin, Vector2Int playerSpawnSpacing)
    {
        int column = index % 2;
        int row = index / 2;
        return playerSpawnOrigin + new Vector2Int(column * playerSpawnSpacing.x, row * playerSpawnSpacing.y);
    }

    private BattleCharacterBindingDatabase.BindingEntry FindBinding(string characterId)
    {
        return characterBindingDatabase != null ? characterBindingDatabase.FindBinding(characterId) : null;
    }

    private CharacterStatDatabase.StatEntry FindStats(string characterId)
    {
        return characterStatDatabase != null ? characterStatDatabase.FindEntry(characterId) : null;
    }

    private static string ResolveDisplayName(string characterId, BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (binding != null && !string.IsNullOrWhiteSpace(binding.displayName))
        {
            return binding.displayName;
        }

        return string.IsNullOrWhiteSpace(characterId) ? "Player" : characterId;
    }
}
