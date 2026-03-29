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

    [Header("Enemy Spawn")]
    public Vector2Int enemySpawnCell = new Vector2Int(13, 12);
    public List<EnemySpawnEntry> enemySpawns = new List<EnemySpawnEntry>();

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
        CreateRoomContent(runtimeRoot);
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
        turnSystem.roundSeparatorSprite = timelineRoundSeparatorSprite;
        turnSystem.roundSeparatorSize = timelineRoundSeparatorSize;
        turnSystem.playerTimelineColor = playerTimelineColor;
        turnSystem.enemyTimelineColor = enemyTimelineColor;
        turnSystem.activePlayerTimelineColor = activePlayerTimelineColor;
        turnSystem.Initialize(grid, mainCamera, units);
        InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacterForBattle();
        InventoryShortcutRuntimeBinder.RefreshRuntimeWeaponModels();
        turnSystem.SetSkillCostHintText(skillCostHintText);
        RefreshPartyPortraits(GetSelectedPlayers());
        RefreshSkillPagination(turnSystem);
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

    private Transform CreateRuntimeRoot()
    {
        GameObject runtimeRoot = new GameObject(RuntimeRootName);
        runtimeRoot.transform.SetParent(transform, false);
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
        grid.width = gridWidth;
        grid.height = gridHeight;
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
        instanceButton.interactable = !string.IsNullOrWhiteSpace(targetNodeId);
        if (!string.IsNullOrWhiteSpace(targetNodeId))
        {
            instanceButton.onClick.AddListener(() => OnDirectionButtonClicked(targetNodeId));
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

    private static void OnDirectionButtonClicked(string targetNodeId)
    {
        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            return;
        }

        SetCurrentRoom(currentDungeonTemplateId, targetNodeId);
        SceneManager.LoadScene(SceneName);
    }

    private List<BattleUnit> CreateUnits(BattleGrid grid, Transform runtimeRoot)
    {
        BattleAnimationSettings animationSettings = BattleAnimationSettings.LoadDefault();
        BattleUnitFactory factory = new BattleUnitFactory(
            animationSettings != null ? animationSettings.idleStateName : string.Empty,
            animationSettings != null ? animationSettings.idleYawOffset : 0f,
            characterBindingDatabase,
            characterStatDatabase,
            runtimeRoot,
            grid,
            placeholderScale,
            playerPlaceholderColor,
            enemyPlaceholderColor);

        List<BattleUnit> units = factory.CreatePlayers(GetSelectedPlayers(), playerSpawnOrigin, playerSpawnSpacing);
        units.AddRange(factory.CreateEnemies(GetEnemySpawnEntries()));
        return units;
    }

    private static string ResolveIdleStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.idleStateName : string.Empty;
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

    private List<EnemySpawnEntry> GetEnemySpawnEntries()
    {
        List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
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

    private void RefreshSkillPagination(BattleTurnSystem turnSystem)
    {
        if (turnSystem == null)
        {
            return;
        }

        BattleSkillPaginationBinder binder = GetComponent<BattleSkillPaginationBinder>();
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattleSkillPaginationBinder>();
        }

        binder.Initialize(turnSystem);
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
