using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        public string enemyId = string.Empty;
        public Vector2Int spawnCell = new Vector2Int(13, 12);
        public BattleTeam team = BattleTeam.Enemy;
        public bool isPlayerControlled;
    }

    private const string SceneName = "战斗副本";
    private const string DefaultDungeonTemplateId = "地牢1";
    private const string DefaultDungeonNodeId = "入口";
    private const string RuntimeRootName = "BattleRuntime";
    private const string GridObjectName = "BattleGrid";
    private const string RoomContentRootName = "RoomContent";

    [System.Serializable]
    public sealed class RoomStateMemory
    {
        public bool encounterCleared;
        public GameObject preservedRuntimeRoot;
    }

    [Header("Binding Database")]
    public BattleCharacterBindingDatabase characterBindingDatabase;

    [Header("Stat Database")]
    public CharacterStatDatabase characterStatDatabase;

    [Header("Scene References")]
    public Transform dungeonBoard;
    public TMP_Text skillCostHintText;

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

    [Header("Legacy Cleanup")]
    public List<string> legacyRootNamesToDisable = new List<string>();

    [Header("Board")]
    public float boardDistance = 18f;
    public Vector3 boardOffset = new Vector3(0f, -2f, 0f);

    [Header("Camera")]
    public float cameraSize = 8f;
    public Vector3 cameraPosition = new Vector3(10f, 14f, -4f);
    public Vector3 cameraEulerAngles = new Vector3(48.6f, 45f, 0f);

    [Header("Timeline")]
    public float timelineSpacing = 0f;
    public float activeTimelineExtraSpacing = 0f;
    public float activeTimelineScale = 1.1f;
    [Header("时间轴预览")]
    public int timelinePreviewRoundCount = 3;
    public float timelineRoundSeparatorSpacing = 32f;
    public Sprite timelineRoundSeparatorSprite;
    public Vector2 timelineRoundSeparatorSize = new Vector2(32f, 125f);

    [Header("Timeline Colors")]
    public Color playerTimelineColor = new Color(0.20f, 0.75f, 0.35f, 1f);
    public Color enemyTimelineColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    public Color activePlayerTimelineColor = Color.white;

    [Header("Editor Preview")]
    public bool showEditorGrid = true;
    public Color editorGridColor = new Color(0.15f, 0.7f, 1f, 0.8f);

    private static string currentDungeonTemplateId = DefaultDungeonTemplateId;
    private static string currentDungeonNodeId = DefaultDungeonNodeId;
    private static MapTemplateDatabase.ConnectionDirection? pendingEntranceDirection;
    private static readonly Dictionary<string, RoomStateMemory> roomStateMemories = new Dictionary<string, RoomStateMemory>(System.StringComparer.Ordinal);

    public static string CurrentDungeonTemplateId => currentDungeonTemplateId;
    public static string CurrentDungeonNodeId => currentDungeonNodeId;

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

    public static void SetCurrentRoom(string templateId, string nodeId)
    {
        if (!string.IsNullOrWhiteSpace(templateId))
        {
            currentDungeonTemplateId = templateId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(nodeId))
        {
            currentDungeonNodeId = nodeId.Trim();
        }
    }

    public static RoomStateMemory GetCurrentRoomStateMemory()
    {
        return GetRoomStateMemory(currentDungeonTemplateId, currentDungeonNodeId);
    }

    public static RoomStateMemory GetRoomStateMemory(string templateId, string nodeId)
    {
        string roomKey = BuildRoomKey(templateId, nodeId);
        if (string.IsNullOrWhiteSpace(roomKey))
        {
            return null;
        }

        RoomStateMemory memory;
        if (!roomStateMemories.TryGetValue(roomKey, out memory) || memory == null)
        {
            memory = new RoomStateMemory();
            roomStateMemories[roomKey] = memory;
        }

        return memory;
    }

    public static void MarkCurrentRoomEncounterCleared()
    {
        RoomStateMemory memory = GetCurrentRoomStateMemory();
        if (memory == null)
        {
            Debug.LogWarning("BattleBootstrap: MarkCurrentRoomEncounterCleared failed because current room memory is missing.");
            return;
        }

        memory.encounterCleared = true;
        Debug.Log($"BattleBootstrap: marked room '{BuildRoomKey(currentDungeonTemplateId, currentDungeonNodeId)}' as encounter cleared.");
    }

    public static bool IsCurrentRoomEncounterCleared()
    {
        RoomStateMemory memory = GetCurrentRoomStateMemory();
        return memory != null && memory.encounterCleared;
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

        bool restoredFromSnapshot;
        Transform runtimeRoot = CreateRuntimeRoot(out restoredFromSnapshot);
        BattleGrid grid;
        List<BattleUnit> units;
        if (restoredFromSnapshot)
        {
            grid = ResolveRestoredGrid(runtimeRoot);
            units = CollectRuntimeUnits(runtimeRoot);
            ResetPlayerUnitPositionsForSnapshotRestore(grid, units);
        }
        else
        {
            CreateRoomContent(runtimeRoot);
            grid = CreateGrid(runtimeRoot);
            units = CreateUnits(grid, runtimeRoot);
        }

        if (grid == null)
        {
            Debug.LogWarning("BattleBootstrap: failed to resolve BattleGrid for current room.");
            return;
        }

        if (units.Count < 1)
        {
            Debug.LogWarning("BattleBootstrap: not enough units to initialize room runtime.");
            return;
        }

        BattleTurnSystem turnSystem = ResetTurnSystems();
        turnSystem.timelineSpacing = timelineSpacing;
        turnSystem.activeTimelineExtraSpacing = activeTimelineExtraSpacing;
        turnSystem.activeTimelineScale = activeTimelineScale;
        turnSystem.previewRoundCount = Mathf.Max(1, timelinePreviewRoundCount);
        turnSystem.roundSeparatorSpacing = Mathf.Max(0f, timelineRoundSeparatorSpacing);
        turnSystem.roundSeparatorSprite = timelineRoundSeparatorSprite;
        turnSystem.roundSeparatorSize = timelineRoundSeparatorSize;
        turnSystem.playerTimelineColor = playerTimelineColor;
        turnSystem.enemyTimelineColor = enemyTimelineColor;
        turnSystem.activePlayerTimelineColor = activePlayerTimelineColor;
        turnSystem.Initialize(grid, mainCamera, units);
        界面ID列表.清空当前ID();
        InventoryShortcutRuntimeBinder.RefreshRuntimeWeaponModels();
        turnSystem.SetSkillCostHintText(skillCostHintText);
        RefreshPartyPortraits(GetSelectedPlayers());
        RefreshBattleSkillBar(turnSystem);
        RefreshActionPointUi(turnSystem);
        RefreshVitalBars(turnSystem);
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

    private Transform CreateRuntimeRoot(out bool restoredFromSnapshot)
    {
        RoomStateMemory roomMemory = GetCurrentRoomStateMemory();
        if (roomMemory != null && roomMemory.preservedRuntimeRoot != null)
        {
            GameObject preservedRuntimeRoot = roomMemory.preservedRuntimeRoot;
            roomMemory.preservedRuntimeRoot = null;
            restoredFromSnapshot = true;
            if (preservedRuntimeRoot.transform.parent != null)
            {
                preservedRuntimeRoot.transform.SetParent(null, true);
            }
            SceneManager.MoveGameObjectToScene(preservedRuntimeRoot, gameObject.scene);
            preservedRuntimeRoot.name = RuntimeRootName;
            preservedRuntimeRoot.SetActive(true);
            return preservedRuntimeRoot.transform;
        }

        GameObject runtimeRoot = new GameObject(RuntimeRootName);
        restoredFromSnapshot = false;
        return runtimeRoot.transform;
    }

    private void CreateRoomContent(Transform runtimeRoot)
    {
        if (runtimeRoot == null)
        {
            return;
        }

        MapTemplateDatabase.MapNodeEntry roomNode = ResolveBattleRoomNode();
        GameObject roomContentPrefab = ResolveBattleRoomContentPrefab();
        if (roomContentPrefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(roomContentPrefab, runtimeRoot, false);
        instance.name = RoomContentRootName;
        instance.transform.localPosition = roomContentPrefab.transform.localPosition;
        instance.transform.localRotation = roomContentPrefab.transform.localRotation;
        instance.transform.localScale = roomContentPrefab.transform.localScale;
        ConfigureRoomDirectionButtons(roomNode, roomContentPrefab.transform, instance.transform);
    }

    private BattleGrid CreateGrid(Transform runtimeRoot)
    {
        GameObject gridObject = new GameObject(GridObjectName);
        gridObject.transform.SetParent(runtimeRoot, false);

        BattleGrid grid = gridObject.AddComponent<BattleGrid>();
        格子模板数据库.格子模板条目 gridTemplate = ResolveCurrentGridTemplate();
        if (gridTemplate != null)
        {
            grid.width = Mathf.Max(1, gridTemplate.width);
            grid.height = Mathf.Max(1, gridTemplate.height);
            grid.SetValidCells(ConvertCells(gridTemplate.walkableCells));
        }
        else
        {
            grid.width = gridWidth;
            grid.height = gridHeight;
            grid.SetValidCells(null);
        }
        grid.cellSize = gridCellSize;
        grid.BuildVisuals();
        return grid;
    }

    private void ConfigureRoomDirectionButtons(
        MapTemplateDatabase.MapNodeEntry roomNode,
        Transform prefabRoot,
        Transform instanceRoot)
    {
        if (roomNode == null || prefabRoot == null || instanceRoot == null)
        {
            return;
        }

        SetDirectionButtonInteractable(roomNode, instanceRoot, roomNode.eastButtonPath, MapTemplateDatabase.ConnectionDirection.East);
        SetDirectionButtonInteractable(roomNode, instanceRoot, roomNode.southButtonPath, MapTemplateDatabase.ConnectionDirection.South);
        SetDirectionButtonInteractable(roomNode, instanceRoot, roomNode.westButtonPath, MapTemplateDatabase.ConnectionDirection.West);
        SetDirectionButtonInteractable(roomNode, instanceRoot, roomNode.northButtonPath, MapTemplateDatabase.ConnectionDirection.North);
    }

    private static void SetDirectionButtonInteractable(
        MapTemplateDatabase.MapNodeEntry roomNode,
        Transform instanceRoot,
        string buttonPath,
        MapTemplateDatabase.ConnectionDirection direction)
    {
        if (roomNode == null || instanceRoot == null || string.IsNullOrWhiteSpace(buttonPath))
        {
            return;
        }

        Transform instanceButtonTransform = instanceRoot.Find(buttonPath);
        if (instanceButtonTransform == null)
        {
            Debug.LogWarning($"BattleBootstrap: missing instantiated direction button path '{buttonPath}' in room content '{instanceRoot.name}'.");
            return;
        }

        Button instanceButton = instanceButtonTransform.GetComponent<Button>();
        if (instanceButton == null)
        {
            Debug.LogWarning($"BattleBootstrap: instantiated object '{instanceButtonTransform.name}' has no Button component.");
            return;
        }

        string targetNodeId = FindConnectionTargetInDirection(roomNode, direction);
        bool canInteract = !string.IsNullOrWhiteSpace(targetNodeId);
        instanceButton.interactable = canInteract;
        ConfigureDirectionClickRelay(instanceButtonTransform.gameObject, targetNodeId, canInteract);
        if (canInteract)
        {
            instanceButton.onClick.AddListener(() => NavigateToNode(targetNodeId));
        }
        else
        {
        }
    }

    private static bool HasConnectionInDirection(
        MapTemplateDatabase.MapNodeEntry roomNode,
        MapTemplateDatabase.ConnectionDirection direction)
    {
        if (roomNode == null || roomNode.connections == null)
        {
            return false;
        }

        for (int i = 0; i < roomNode.connections.Count; i++)
        {
            MapTemplateDatabase.MapConnectionEntry connection = roomNode.connections[i];
            if (connection == null || string.IsNullOrWhiteSpace(connection.targetNodeId))
            {
                continue;
            }

            if (connection.direction == direction)
            {
                return true;
            }
        }

        return false;
    }

    private static string FindConnectionTargetInDirection(
        MapTemplateDatabase.MapNodeEntry roomNode,
        MapTemplateDatabase.ConnectionDirection direction)
    {
        if (roomNode == null || roomNode.connections == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < roomNode.connections.Count; i++)
        {
            MapTemplateDatabase.MapConnectionEntry connection = roomNode.connections[i];
            if (connection == null || string.IsNullOrWhiteSpace(connection.targetNodeId))
            {
                continue;
            }

            if (connection.direction == direction)
            {
                return connection.targetNodeId.Trim();
            }
        }

        return string.Empty;
    }

    private static void ConfigureDirectionClickRelay(GameObject buttonObject, string targetNodeId, bool canInteract)
    {
        if (buttonObject == null)
        {
            return;
        }

        BattleRoomDirectionClickRelay relay = buttonObject.GetComponent<BattleRoomDirectionClickRelay>();
        if (relay == null)
        {
            relay = buttonObject.AddComponent<BattleRoomDirectionClickRelay>();
        }

        relay.Configure(targetNodeId, canInteract);

        if (!canInteract)
        {
            return;
        }

        Collider existingCollider = buttonObject.GetComponent<Collider>();
        if (existingCollider != null)
        {
            existingCollider.enabled = true;
            return;
        }

        SpriteRenderer spriteRenderer = buttonObject.GetComponent<SpriteRenderer>();
        BoxCollider collider = buttonObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = buttonObject.AddComponent<BoxCollider>();
        }

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            collider.size = new Vector3(
                Mathf.Max(0.01f, spriteSize.x),
                Mathf.Max(0.01f, spriteSize.y),
                0.5f);
            collider.center = Vector3.zero;
        }
        else
        {
            collider.size = new Vector3(1f, 1f, 0.5f);
            collider.center = Vector3.zero;
        }
    }

    public static void NavigateToNode(string targetNodeId)
    {
        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            Debug.LogWarning("BattleBootstrap: NavigateToNode received empty targetNodeId.");
            return;
        }

        pendingEntranceDirection = ResolveEntranceDirectionForTarget(targetNodeId);
        PreserveCurrentRoomSnapshot();
        SetCurrentRoom(currentDungeonTemplateId, targetNodeId);
        SceneManager.LoadScene(SceneName);
    }

    private static string BuildRoomKey(string templateId, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(nodeId))
        {
            return string.Empty;
        }

        return templateId.Trim() + "::" + nodeId.Trim();
    }

    private List<BattleUnit> CreateUnits(BattleGrid grid, Transform runtimeRoot)
    {
        BattleAnimationSettings animationSettings = BattleAnimationSettings.LoadDefault();
        List<CharacterSelectionState.SlotSelection> selectedPlayers = GetSelectedPlayers();
        BattleUnitFactory factory = new BattleUnitFactory(
            animationSettings != null ? animationSettings.idleYawOffset : 0f,
            characterBindingDatabase,
            characterStatDatabase,
            runtimeRoot,
            grid,
            placeholderScale,
            playerPlaceholderColor,
            enemyPlaceholderColor);

        List<Vector2Int> playerSpawnCells = ResolvePlayerSpawnCells(selectedPlayers.Count);
        List<BattleUnit> units = factory.CreatePlayers(selectedPlayers, playerSpawnCells);
        units.AddRange(factory.CreateEnemies(GetEnemySpawnEntries()));
        return units;
    }

    private static void PreserveCurrentRoomSnapshot()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RoomStateMemory memory = GetCurrentRoomStateMemory();
        if (memory == null)
        {
            return;
        }

        BattleBootstrap bootstrap = FindObjectOfType<BattleBootstrap>();
        if (bootstrap == null)
        {
            return;
        }

        Transform runtimeRoot = FindRuntimeRootInScene(bootstrap.gameObject.scene);
        if (runtimeRoot == null)
        {
            return;
        }

        if (memory.preservedRuntimeRoot != null && memory.preservedRuntimeRoot != runtimeRoot.gameObject)
        {
            Object.Destroy(memory.preservedRuntimeRoot);
        }

        if (runtimeRoot.parent != null)
        {
            runtimeRoot.SetParent(null, true);
        }
        runtimeRoot.gameObject.SetActive(false);
        DontDestroyOnLoad(runtimeRoot.gameObject);
        memory.preservedRuntimeRoot = runtimeRoot.gameObject;
    }

    private static BattleGrid ResolveRestoredGrid(Transform runtimeRoot)
    {
        return runtimeRoot != null ? runtimeRoot.GetComponentInChildren<BattleGrid>(true) : null;
    }

    private static List<BattleUnit> CollectRuntimeUnits(Transform runtimeRoot)
    {
        List<BattleUnit> units = new List<BattleUnit>();
        if (runtimeRoot == null)
        {
            return units;
        }

        BattleUnit[] foundUnits = runtimeRoot.GetComponentsInChildren<BattleUnit>(true);
        for (int i = 0; i < foundUnits.Length; i++)
        {
            BattleUnit unit = foundUnits[i];
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        return units;
    }

    private static Transform FindRuntimeRootInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root != null && string.Equals(root.name, RuntimeRootName, System.StringComparison.Ordinal))
            {
                return root.transform;
            }
        }

        return null;
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

    private void ResetPlayerUnitPositionsForSnapshotRestore(BattleGrid grid, List<BattleUnit> units)
    {
        if (grid == null || units == null || units.Count == 0)
        {
            return;
        }

        List<CharacterSelectionState.SlotSelection> selectedPlayers = GetSelectedPlayers();
        if (selectedPlayers.Count == 0)
        {
            return;
        }

        List<Vector2Int> playerSpawnCells = ResolvePlayerSpawnCells(selectedPlayers.Count);
        Dictionary<string, BattleUnit> unitsByCharacterId = new Dictionary<string, BattleUnit>(System.StringComparer.Ordinal);
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || string.IsNullOrWhiteSpace(unit.characterId))
            {
                continue;
            }

            unitsByCharacterId[unit.characterId] = unit;
        }

        for (int i = 0; i < selectedPlayers.Count; i++)
        {
            CharacterSelectionState.SlotSelection selection = selectedPlayers[i];
            if (string.IsNullOrWhiteSpace(selection.characterId))
            {
                continue;
            }

            BattleUnit unit;
            if (!unitsByCharacterId.TryGetValue(selection.characterId, out unit) || unit == null)
            {
                continue;
            }

            if (i < 0 || i >= playerSpawnCells.Count)
            {
                continue;
            }

            Vector2Int spawnCell = playerSpawnCells[i];
            grid.RemoveUnit(unit);
            unit.CancelMovement();
            unit.SetCell(spawnCell, grid.GetWorldPosition(spawnCell));
            unit.FaceToward(grid.GetWorldPosition(spawnCell + Vector2Int.right));
            grid.RegisterUnit(unit);
        }
    }

    private static Vector2Int GetPlayerSpawnCell(int index, Vector2Int spawnOrigin, Vector2Int spawnSpacing)
    {
        int column = index % 2;
        int row = index / 2;
        return spawnOrigin + new Vector2Int(column * spawnSpacing.x, row * spawnSpacing.y);
    }

    private List<EnemySpawnEntry> GetEnemySpawnEntries()
    {
        List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
        if (IsCurrentRoomEncounterCleared())
        {
            return entries;
        }

        string roomEnemyPresetId = ResolveBattleRoomEnemyPresetId();
        if (string.IsNullOrWhiteSpace(roomEnemyPresetId))
        {
            return entries;
        }

        RoomEnemyPresetDatabase presetDatabase = RoomEnemyPresetDatabase.LoadDefault();
        if (presetDatabase == null)
        {
            Debug.LogWarning($"BattleBootstrap: missing RoomEnemyPresetDatabase. Scene '{SceneName}' expected preset '{roomEnemyPresetId}'.");
            return entries;
        }

        RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = presetDatabase.FindEntry(roomEnemyPresetId);
        if (preset == null || preset.enemies == null)
        {
            Debug.LogWarning($"BattleBootstrap: missing room enemy preset '{roomEnemyPresetId}' for scene '{SceneName}'.");
            return entries;
        }

        for (int i = 0; i < preset.enemies.Count; i++)
        {
            EnemySpawnEntry entry = preset.enemies[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.enemyId))
            {
                continue;
            }

            entries.Add(RoomEnemyPresetDatabase.CloneEnemy(entry));
        }

        格子模板数据库.格子模板条目 gridTemplate = ResolveCurrentGridTemplate();
        List<Vector2Int> templateEnemySpawnCells = gridTemplate != null ? ConvertCells(gridTemplate.enemySpawnCells) : null;
        if (templateEnemySpawnCells != null && templateEnemySpawnCells.Count > 0)
        {
            int overrideCount = Mathf.Min(entries.Count, templateEnemySpawnCells.Count);
            for (int i = 0; i < overrideCount; i++)
            {
                entries[i].spawnCell = templateEnemySpawnCells[i];
            }

            if (entries.Count > templateEnemySpawnCells.Count)
            {
                Debug.LogWarning($"BattleBootstrap: grid template '{gridTemplate.templateId}' only provides {templateEnemySpawnCells.Count} enemy spawn cells, but preset '{roomEnemyPresetId}' contains {entries.Count} enemies. Remaining enemies will keep preset spawn cells.");
            }
        }

        return entries;
    }

    private static string ResolveBattleRoomEnemyPresetId()
    {
        MapTemplateDatabase.MapNodeEntry node = ResolveBattleRoomNode();
        if (node != null && !string.IsNullOrWhiteSpace(node.encounterPresetId))
        {
            return node.encounterPresetId.Trim();
        }

        return string.Empty;
    }

    private static GameObject ResolveBattleRoomContentPrefab()
    {
        MapTemplateDatabase.MapNodeEntry node = ResolveBattleRoomNode();
        return node != null ? node.battleSceneContentPrefab : null;
    }

    private static MapTemplateDatabase.MapNodeEntry ResolveBattleRoomNode()
    {
        MapTemplateDatabase mapTemplateDatabase = MapTemplateDatabase.LoadDefault();
        if (mapTemplateDatabase == null)
        {
            return null;
        }

        MapTemplateDatabase.MapTemplateEntry template = mapTemplateDatabase.FindEntry(currentDungeonTemplateId);
        if (template == null || template.nodes == null)
        {
            return null;
        }

        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                continue;
            }

            if (!string.Equals(node.nodeId, currentDungeonNodeId, System.StringComparison.Ordinal))
            {
                continue;
            }

            return node;
        }

        if (!string.Equals(currentDungeonNodeId, DefaultDungeonNodeId, System.StringComparison.Ordinal))
        {
            for (int i = 0; i < template.nodes.Count; i++)
            {
                MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (string.Equals(node.nodeId, DefaultDungeonNodeId, System.StringComparison.Ordinal))
                {
                    currentDungeonNodeId = DefaultDungeonNodeId;
                    return node;
                }
            }
        }

        return null;
    }

    private static 格子模板数据库.格子模板条目 ResolveCurrentGridTemplate()
    {
        MapTemplateDatabase.MapNodeEntry node = ResolveBattleRoomNode();
        if (node == null || string.IsNullOrWhiteSpace(node.battleGridTemplateId))
        {
            return null;
        }

        格子模板数据库 database = 格子模板数据库.LoadDefault();
        if (database == null)
        {
            Debug.LogWarning($"BattleBootstrap: missing 格子模板数据库 while room node '{node.nodeId}' references grid template '{node.battleGridTemplateId}'.");
            return null;
        }

        格子模板数据库.格子模板条目 entry = database.FindEntry(node.battleGridTemplateId.Trim());
        if (entry == null)
        {
            Debug.LogWarning($"BattleBootstrap: missing grid template '{node.battleGridTemplateId}' for room node '{node.nodeId}'.");
        }

        return entry;
    }

    private List<Vector2Int> ResolvePlayerSpawnCells(int count)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        int resolvedCount = Mathf.Max(0, count);
        格子模板数据库.格子模板条目 gridTemplate = ResolveCurrentGridTemplate();

        Vector2Int? anchorSpawn = null;
        if (gridTemplate != null)
        {
            anchorSpawn = ResolveTemplatePlayerSpawn(gridTemplate);
        }

        for (int i = 0; i < resolvedCount; i++)
        {
            if (anchorSpawn.HasValue)
            {
                result.Add(ResolveSpawnCellForIndex(i, anchorSpawn.Value));
            }
            else
            {
                result.Add(GetPlayerSpawnCell(i, playerSpawnOrigin, playerSpawnSpacing));
            }
        }

        return result;
    }

    private Vector2Int? ResolveTemplatePlayerSpawn(格子模板数据库.格子模板条目 gridTemplate)
    {
        if (gridTemplate == null)
        {
            return null;
        }

        if (gridTemplate.playerSpawnMode == 格子模板数据库.PlayerSpawnMode.Door)
        {
            Vector2Int? doorSpawn = ResolveDoorSpawn(gridTemplate, pendingEntranceDirection);
            if (doorSpawn.HasValue)
            {
                return doorSpawn.Value;
            }
        }

        if (gridTemplate.hasDefaultPlayerSpawn)
        {
            return gridTemplate.defaultPlayerSpawnCell.ToVector2Int();
        }

        return null;
    }

    private static Vector2Int? ResolveDoorSpawn(
        格子模板数据库.格子模板条目 gridTemplate,
        MapTemplateDatabase.ConnectionDirection? entranceDirection)
    {
        if (gridTemplate == null || !entranceDirection.HasValue)
        {
            return null;
        }

        switch (entranceDirection.Value)
        {
            case MapTemplateDatabase.ConnectionDirection.East:
                return gridTemplate.hasEastDoorPlayerSpawn ? gridTemplate.eastDoorPlayerSpawnCell.ToVector2Int() : (Vector2Int?)null;
            case MapTemplateDatabase.ConnectionDirection.South:
                return gridTemplate.hasSouthDoorPlayerSpawn ? gridTemplate.southDoorPlayerSpawnCell.ToVector2Int() : (Vector2Int?)null;
            case MapTemplateDatabase.ConnectionDirection.West:
                return gridTemplate.hasWestDoorPlayerSpawn ? gridTemplate.westDoorPlayerSpawnCell.ToVector2Int() : (Vector2Int?)null;
            case MapTemplateDatabase.ConnectionDirection.North:
                return gridTemplate.hasNorthDoorPlayerSpawn ? gridTemplate.northDoorPlayerSpawnCell.ToVector2Int() : (Vector2Int?)null;
            default:
                return null;
        }
    }

    private Vector2Int ResolveSpawnCellForIndex(int index, Vector2Int anchorCell)
    {
        int column = index % 2;
        int row = index / 2;
        return anchorCell + new Vector2Int(column * playerSpawnSpacing.x, row * playerSpawnSpacing.y);
    }

    private static List<Vector2Int> ConvertCells(List<格子模板数据库.CellPosition> cells)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        if (cells == null)
        {
            return result;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            result.Add(cells[i].ToVector2Int());
        }

        return result;
    }

    private static MapTemplateDatabase.ConnectionDirection? ResolveEntranceDirectionForTarget(string targetNodeId)
    {
        MapTemplateDatabase.MapNodeEntry currentNode = ResolveBattleRoomNode();
        if (currentNode == null || currentNode.connections == null || string.IsNullOrWhiteSpace(targetNodeId))
        {
            return null;
        }

        string resolvedTargetNodeId = targetNodeId.Trim();
        for (int i = 0; i < currentNode.connections.Count; i++)
        {
            MapTemplateDatabase.MapConnectionEntry connection = currentNode.connections[i];
            if (connection == null || string.IsNullOrWhiteSpace(connection.targetNodeId))
            {
                continue;
            }

            if (!string.Equals(connection.targetNodeId.Trim(), resolvedTargetNodeId, System.StringComparison.Ordinal))
            {
                continue;
            }

            return ReverseDirection(connection.direction);
        }

        return null;
    }

    private static MapTemplateDatabase.ConnectionDirection ReverseDirection(MapTemplateDatabase.ConnectionDirection direction)
    {
        switch (direction)
        {
            case MapTemplateDatabase.ConnectionDirection.East:
                return MapTemplateDatabase.ConnectionDirection.West;
            case MapTemplateDatabase.ConnectionDirection.West:
                return MapTemplateDatabase.ConnectionDirection.East;
            case MapTemplateDatabase.ConnectionDirection.North:
                return MapTemplateDatabase.ConnectionDirection.South;
            case MapTemplateDatabase.ConnectionDirection.South:
                return MapTemplateDatabase.ConnectionDirection.North;
            default:
                return direction;
        }
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

    private void HideLegacySceneCharacters()
    {
        if (legacyRootNamesToDisable == null || legacyRootNamesToDisable.Count == 0)
        {
            return;
        }

        HashSet<string> namesToDisable = new HashSet<string>(legacyRootNamesToDisable);
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || !namesToDisable.Contains(target.name))
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

    private void RefreshPartyPortraits(IReadOnlyList<CharacterSelectionState.SlotSelection> selectedPlayers)
    {
        BattlePartyPortraitBinder binder = GetComponent<BattlePartyPortraitBinder>();
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattlePartyPortraitBinder>();
        }

        BattleTurnSystem turnSystem = GetComponent<BattleTurnSystem>();
        binder.Initialize(turnSystem, selectedPlayers);
    }

    private void RefreshActionPointUi(BattleTurnSystem turnSystem)
    {
        if (turnSystem == null)
        {
            return;
        }

        BattleActionPointBinder binder = GetComponent<BattleActionPointBinder>();
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattleActionPointBinder>();
        }

        binder.Initialize(turnSystem);
    }

    private void RefreshBattleSkillBar(BattleTurnSystem turnSystem)
    {
        if (turnSystem == null)
        {
            return;
        }

        战斗技能栏绑定 binder = FindObjectOfType<战斗技能栏绑定>(true);
        if (binder == null)
        {
            Debug.LogWarning("BattleBootstrap: 场景中未找到战斗技能栏绑定，战斗下方技能栏不会刷新。");
            return;
        }

        binder.初始化(turnSystem);
    }

    private void RefreshVitalBars(BattleTurnSystem turnSystem)
    {
        if (turnSystem == null)
        {
            return;
        }

        BattleVitalBarBinder binder = GetComponent<BattleVitalBarBinder>();
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattleVitalBarBinder>();
        }

        binder.Initialize(turnSystem);
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
