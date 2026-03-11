using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class BattleBootstrap : MonoBehaviour
{
    [System.Serializable]
    public sealed class EnemySpawnEntry
    {
        public string enemyId = EnemyId;
        public Vector2Int spawnCell = new Vector2Int(13, 12);
        public BattleTeam team = BattleTeam.Enemy;
        public bool isPlayerControlled;
        public int maxHealth = 12;
        public int moveRange = 3;
        public int attackRange = 1;
        public int attackDamage = 2;
        public int footprintSize = 3;
    }

    private const string SceneName = "20x20";
    private const string RuntimeRootName = "BattleRuntime";
    private const string GridObjectName = "BattleGrid";
    private const string LegacyAliceRootName = "爱丽丝root";
    private const string EnemyId = "假人";

    [Header("Binding Database")]
    public BattleCharacterBindingDatabase characterBindingDatabase;

    [Header("Stat Database")]
    public CharacterStatDatabase characterStatDatabase;

    [Header("Scene References")]
    public Transform dungeonBoard;

    [Header("Placeholder")]
    public Vector3 placeholderScale = new Vector3(0.8f, 1.2f, 0.8f);
    public Color playerPlaceholderColor = new Color(0.20f, 0.75f, 0.35f, 0.45f);
    public Color enemyPlaceholderColor = new Color(0.85f, 0.25f, 0.20f, 1f);

    [Header("Grid")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float gridCellSize = 1f;

    [Header("Player Spawn")]
    public Vector2Int playerSpawnOrigin = new Vector2Int(4, 4);
    public Vector2Int playerSpawnSpacing = new Vector2Int(4, 4);

    [Header("Enemy Spawn")]
    public Vector2Int enemySpawnCell = new Vector2Int(13, 12);
    public List<EnemySpawnEntry> enemySpawns = new List<EnemySpawnEntry>();

    [Header("Board")]
    public float boardDistance = 18f;
    public Vector3 boardOffset = new Vector3(0f, -2f, 0f);

    [Header("Camera")]
    public float cameraSize = 8f;
    public Vector3 cameraPosition = new Vector3(10f, 14f, -4f);
    public Vector3 cameraEulerAngles = new Vector3(45f, 45f, 0f);

    [Header("Timeline")]
    public float timelineSpacing = 0f;
    public float activeTimelineExtraSpacing = 0f;
    public float activeTimelineScale = 1.1f;
    [Header("时间轴预览")]
    public int timelinePreviewRoundCount = 3;
    public float timelineRoundSeparatorSpacing = 32f;

    [Header("Timeline Colors")]
    public Color playerTimelineColor = new Color(0.20f, 0.75f, 0.35f, 1f);
    public Color enemyTimelineColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    public Color activePlayerTimelineColor = Color.white;

    [Header("Editor Preview")]
    public bool showEditorGrid = true;
    public Color editorGridColor = new Color(0.15f, 0.7f, 1f, 0.8f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneName)
        {
            return;
        }

        if (FindObjectOfType<BattleBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("BattleBootstrap");
        bootstrapObject.AddComponent<BattleBootstrap>();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("BattleBootstrap: no Main Camera found.");
            return;
        }

        ResolveReferences();
        CleanupRuntimeObjects();
        HideLegacySceneCharacters();
        SetupBattleCamera(mainCamera);
        AlignDungeonBoardToCamera(mainCamera);

        Transform runtimeRoot = CreateRuntimeRoot();
        BattleGrid grid = CreateGrid(runtimeRoot);
        List<BattleUnit> units = CreateUnits(grid, runtimeRoot);
        if (units.Count < 2)
        {
            Debug.LogWarning("BattleBootstrap: not enough units for turn-based combat.");
            return;
        }

        BattleTurnSystem turnSystem = ResetTurnSystems();
        turnSystem.timelineSpacing = timelineSpacing;
        turnSystem.activeTimelineExtraSpacing = activeTimelineExtraSpacing;
        turnSystem.activeTimelineScale = activeTimelineScale;
        turnSystem.previewRoundCount = Mathf.Max(1, timelinePreviewRoundCount);
        turnSystem.roundSeparatorSpacing = Mathf.Max(0f, timelineRoundSeparatorSpacing);
        turnSystem.playerTimelineColor = playerTimelineColor;
        turnSystem.enemyTimelineColor = enemyTimelineColor;
        turnSystem.activePlayerTimelineColor = activePlayerTimelineColor;
        turnSystem.Initialize(grid, mainCamera, units);
    }

    private void ResolveReferences()
    {
        if (characterBindingDatabase == null)
        {
            characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        }

        if (characterStatDatabase == null)
        {
            characterStatDatabase = CharacterStatDatabase.LoadDefault();
        }
    }

    private Transform CreateRuntimeRoot()
    {
        GameObject runtimeRoot = new GameObject(RuntimeRootName);
        runtimeRoot.transform.SetParent(transform, false);
        return runtimeRoot.transform;
    }

    private BattleGrid CreateGrid(Transform runtimeRoot)
    {
        GameObject gridObject = new GameObject(GridObjectName);
        gridObject.transform.SetParent(runtimeRoot, false);

        BattleGrid grid = gridObject.AddComponent<BattleGrid>();
        grid.width = gridWidth;
        grid.height = gridHeight;
        grid.cellSize = gridCellSize;
        grid.BuildVisuals();
        return grid;
    }

    private List<BattleUnit> CreateUnits(BattleGrid grid, Transform runtimeRoot)
    {
        List<BattleUnit> units = new List<BattleUnit>();
        List<CharacterSelectionState.SlotSelection> playerSelections = GetSelectedPlayers();

        for (int i = 0; i < playerSelections.Count; i++)
        {
            BattleUnit player = SetupPlayer(grid, runtimeRoot, playerSelections[i], i);
            if (player != null)
            {
                units.Add(player);
            }
        }

        List<EnemySpawnEntry> enemyEntries = GetEnemySpawnEntries();
        for (int i = 0; i < enemyEntries.Count; i++)
        {
            BattleUnit enemy = SetupEnemy(grid, runtimeRoot, enemyEntries[i], i);
            if (enemy != null)
            {
                units.Add(enemy);
            }
        }

        return units;
    }

    private List<CharacterSelectionState.SlotSelection> GetSelectedPlayers()
    {
        List<CharacterSelectionState.SlotSelection> result = new List<CharacterSelectionState.SlotSelection>();
        IReadOnlyList<CharacterSelectionState.SlotSelection> selections = CharacterSelectionState.SlotSelections;
        for (int i = 0; i < selections.Count; i++)
        {
            CharacterSelectionState.SlotSelection selection = selections[i];
            if (string.IsNullOrWhiteSpace(selection.characterId))
            {
                continue;
            }

            result.Add(selection);
        }

        return result;
    }

    private BattleUnit SetupPlayer(BattleGrid grid, Transform runtimeRoot, CharacterSelectionState.SlotSelection selection, int index)
    {
        BattleCharacterBindingDatabase.BindingEntry binding = FindBinding(selection.characterId);
        CharacterStatDatabase.StatEntry statEntry = FindStats(selection.characterId);
        Vector2Int startCell = GetPlayerSpawnCell(index);
        GameObject unitObject = CreateUnitObject(selection.characterId, binding, runtimeRoot, grid.GetWorldPosition(startCell), playerPlaceholderColor);
        if (unitObject == null)
        {
            return null;
        }

        BattleUnit unit = EnsureBattleUnit(unitObject);
        unit.maxHealth = 18;
        unit.moveRange = 4;
        unit.attackRange = 1;
        unit.attackDamage = 5;
        unit.footprintSize = 3;
        unit.yawOffset = 0f;
        unit.cellOffset = binding != null ? binding.cellOffset : Vector2Int.zero;
        unit.useAutoVisualAnchor = binding != null ? binding.useAutoVisualAnchor : false;
        unit.worldOffset = binding != null ? binding.worldOffset : Vector3.zero;
        unit.ApplyStats(statEntry);
        unit.Setup(selection.characterId, BattleTeam.Player, ResolvePlayerDisplayName(selection.characterId, binding), startCell);
        unit.isPlayerControlled = true;
        unit.SetCell(startCell, grid.GetWorldPosition(startCell));
        unit.FaceToward(grid.GetWorldPosition(startCell + Vector2Int.right));
        grid.RegisterUnit(unit);
        return unit;
    }

    private List<EnemySpawnEntry> GetEnemySpawnEntries()
    {
        List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
        if (enemySpawns != null)
        {
            for (int i = 0; i < enemySpawns.Count; i++)
            {
                EnemySpawnEntry entry = enemySpawns[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.enemyId))
                {
                    continue;
                }

                entries.Add(entry);
            }
        }

        if (entries.Count == 0)
        {
            entries.Add(CreateDefaultEnemyEntry());
        }

        return entries;
    }

    private EnemySpawnEntry CreateDefaultEnemyEntry()
    {
        return new EnemySpawnEntry
        {
            enemyId = EnemyId,
            spawnCell = enemySpawnCell,
            team = BattleTeam.Enemy,
            isPlayerControlled = false,
            maxHealth = 12,
            moveRange = 3,
            attackRange = 1,
            attackDamage = 2,
            footprintSize = 3
        };
    }

    private BattleUnit SetupEnemy(BattleGrid grid, Transform runtimeRoot, EnemySpawnEntry enemyEntry, int index)
    {
        if (enemyEntry == null || string.IsNullOrWhiteSpace(enemyEntry.enemyId))
        {
            return null;
        }

        Vector2Int spawnCell = enemyEntry.spawnCell;
        BattleCharacterBindingDatabase.BindingEntry binding = FindBinding(enemyEntry.enemyId);
        Color placeholderColor = enemyEntry.team == BattleTeam.Enemy ? enemyPlaceholderColor : playerPlaceholderColor;
        GameObject enemyObject = CreateUnitObject(
            enemyEntry.enemyId,
            binding,
            runtimeRoot,
            grid.GetWorldPosition(spawnCell),
            placeholderColor);
        enemyObject.name = enemyEntry.enemyId + "_" + index;

        BattleUnit unit = EnsureBattleUnit(enemyObject);
        unit.maxHealth = enemyEntry.maxHealth;
        unit.moveRange = enemyEntry.moveRange;
        unit.attackRange = enemyEntry.attackRange;
        unit.attackDamage = enemyEntry.attackDamage;
        unit.footprintSize = enemyEntry.footprintSize;
        unit.yawOffset = 0f;
        unit.cellOffset = binding != null ? binding.cellOffset : Vector2Int.zero;
        unit.useAutoVisualAnchor = binding != null ? binding.useAutoVisualAnchor : false;
        unit.worldOffset = binding != null ? binding.worldOffset : Vector3.zero;
        unit.ApplyStats(FindStats(enemyEntry.enemyId));
        unit.Setup(enemyEntry.enemyId, enemyEntry.team, ResolvePlayerDisplayName(enemyEntry.enemyId, binding), spawnCell);
        unit.isPlayerControlled = enemyEntry.isPlayerControlled;
        unit.SetCell(spawnCell, grid.GetWorldPosition(spawnCell));
        Vector2Int facingCell = enemyEntry.team == BattleTeam.Enemy ? spawnCell + Vector2Int.left : spawnCell + Vector2Int.right;
        unit.FaceToward(grid.GetWorldPosition(facingCell));
        grid.RegisterUnit(unit);
        return unit;
    }

    private GameObject CreateUnitObject(string characterId, BattleCharacterBindingDatabase.BindingEntry binding, Transform runtimeRoot, Vector3 worldPosition, Color placeholderColor)
    {
        if (binding != null && binding.modelPrefab != null)
        {
            GameObject instance = Instantiate(binding.modelPrefab, worldPosition, Quaternion.identity, runtimeRoot);
            instance.name = characterId + "_Unit";
            ApplyAnimatorBinding(instance, binding);
            return instance;
        }

        return CreatePlaceholderUnitRoot(characterId + "_Placeholder", runtimeRoot, worldPosition, placeholderColor);
    }

    private static void ApplyAnimatorBinding(GameObject instance, BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (instance == null || binding == null || binding.animatorController == null)
        {
            return;
        }

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            return;
        }

        animator.runtimeAnimatorController = binding.animatorController;
        animator.enabled = true;
    }

    private GameObject CreatePlaceholderUnitRoot(string rootName, Transform parent, Vector3 worldPosition, Color color)
    {
        GameObject root = new GameObject(rootName);
        root.transform.SetParent(parent, false);
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

    private Vector2Int GetPlayerSpawnCell(int index)
    {
        int column = index % 2;
        int row = index / 2;
        return playerSpawnOrigin + new Vector2Int(column * playerSpawnSpacing.x, row * playerSpawnSpacing.y);
    }

    private BattleCharacterBindingDatabase.BindingEntry FindBinding(string characterId)
    {
        if (characterBindingDatabase == null)
        {
            return null;
        }

        return characterBindingDatabase.FindBinding(characterId);
    }

    private CharacterStatDatabase.StatEntry FindStats(string characterId)
    {
        if (characterStatDatabase == null)
        {
            return null;
        }

        return characterStatDatabase.FindEntry(characterId);
    }

    private static string ResolvePlayerDisplayName(string characterId, BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (binding != null && !string.IsNullOrWhiteSpace(binding.displayName))
        {
            return binding.displayName;
        }

        return string.IsNullOrWhiteSpace(characterId) ? "Player" : characterId;
    }

    private void SetupBattleCamera(Camera mainCamera)
    {
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = cameraSize;
        mainCamera.transform.position = cameraPosition;
        mainCamera.transform.rotation = Quaternion.Euler(cameraEulerAngles);

        if (mainCamera.GetComponent<BattleCameraController>() == null)
        {
            mainCamera.gameObject.AddComponent<BattleCameraController>();
        }
    }

    private void AlignDungeonBoardToCamera(Camera mainCamera)
    {
        if (dungeonBoard == null)
        {
            return;
        }

        dungeonBoard.rotation = mainCamera.transform.rotation;
        dungeonBoard.position = mainCamera.transform.position + mainCamera.transform.forward * boardDistance + boardOffset;
    }

    private void CleanupRuntimeObjects()
    {
        Transform runtimeRoot = transform.Find(RuntimeRootName);
        if (runtimeRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeRoot.gameObject);
        }
        else
        {
            DestroyImmediate(runtimeRoot.gameObject);
        }
    }

    private static void HideLegacySceneCharacters()
    {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.name != LegacyAliceRootName)
            {
                continue;
            }

            target.gameObject.SetActive(false);
        }
    }

    private BattleTurnSystem ResetTurnSystems()
    {
        BattleTurnSystem[] existing = GetComponents<BattleTurnSystem>();
        for (int i = 0; i < existing.Length; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(existing[i]);
            }
            else
            {
                DestroyImmediate(existing[i]);
            }
        }

        return gameObject.AddComponent<BattleTurnSystem>();
    }

    private void OnDrawGizmos()
    {
        if (!showEditorGrid || Application.isPlaying)
        {
            return;
        }

        Gizmos.color = editorGridColor;

        float width = gridWidth * gridCellSize;
        float height = gridHeight * gridCellSize;
        Vector3 origin = Vector3.zero;
        float y = -0.05f;

        for (int x = 0; x <= gridWidth; x++)
        {
            float xPos = x * gridCellSize;
            Vector3 from = origin + new Vector3(xPos, y, 0f);
            Vector3 to = origin + new Vector3(xPos, y, height);
            Gizmos.DrawLine(from, to);
        }

        for (int yIndex = 0; yIndex <= gridHeight; yIndex++)
        {
            float zPos = yIndex * gridCellSize;
            Vector3 from = origin + new Vector3(0f, y, zPos);
            Vector3 to = origin + new Vector3(width, y, zPos);
            Gizmos.DrawLine(from, to);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Align Scene View To Battle Camera")]
    private void AlignSceneViewToBattleCamera()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            return;
        }

        sceneView.orthographic = true;
        sceneView.LookAt(cameraPosition, Quaternion.Euler(cameraEulerAngles), cameraSize * 2f);
        sceneView.Repaint();
    }
#endif
}
