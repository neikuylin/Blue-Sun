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
    private const string SceneName = "20x20";
    private const string AliceRootName = "爱丽丝root";
    private const string BoardName = "20x20右上 开门动画";
    private const string PlayerPlaceholderName = "PlayerPlaceholder";

    [Header("Scene References")]
    public Transform playerRoot;
    public Transform dungeonBoard;

    [Header("Player Source")]
    public bool usePlayerPlaceholder = false;

    [Header("Placeholder Visual")]
    public Vector3 placeholderScale = new Vector3(0.8f, 1.2f, 0.8f);
    public Vector3 aliceVisualLocalOffset = Vector3.zero;

    [Header("Grid")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float gridCellSize = 1f;

    [Header("Player Placement")]
    public Vector2Int playerCellOffset = Vector2Int.zero;
    public Vector3 playerWorldOffset = Vector3.zero;
    public bool playerUseAutoVisualAnchor = false;

    [Header("Board Placement")]
    public float boardDistance = 18f;
    public Vector3 boardOffset = new Vector3(0f, -2f, 0f);

    [Header("Camera")]
    public float cameraSize = 8f;
    public Vector3 cameraPosition = new Vector3(10f, 14f, -4f);
    public Vector3 cameraEulerAngles = new Vector3(45f, 45f, 0f);

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
        ResolveReferences();
        SetupBattleCamera(mainCamera);
        AlignDungeonBoardToCamera(mainCamera);

        BattleGrid grid = CreateGrid();
        List<BattleUnit> units = CreateUnits(grid);
        if (units.Count < 2)
        {
            Debug.LogWarning("BattleBootstrap: not enough units for turn-based combat.");
            return;
        }

        BattleTurnSystem turnSystem = gameObject.AddComponent<BattleTurnSystem>();
        turnSystem.Initialize(grid, mainCamera, units);
    }

    private void ResolveReferences()
    {
        if (dungeonBoard == null)
        {
            dungeonBoard = FindTransformByName(BoardName);
        }

        if (usePlayerPlaceholder)
        {
            playerRoot = FindTransformByName(PlayerPlaceholderName);
            return;
        }

        if (playerRoot == null)
        {
            playerRoot = FindTransformByName(AliceRootName);
        }
    }

    private BattleGrid CreateGrid()
    {
        GameObject gridObject = new GameObject("BattleGrid");
        BattleGrid grid = gridObject.AddComponent<BattleGrid>();
        grid.width = gridWidth;
        grid.height = gridHeight;
        grid.cellSize = gridCellSize;
        grid.BuildVisuals();
        return grid;
    }

    private List<BattleUnit> CreateUnits(BattleGrid grid)
    {
        List<BattleUnit> units = new List<BattleUnit>();

        BattleUnit player = SetupPlayer(grid);
        if (player != null)
        {
            units.Add(player);
        }

        BattleUnit enemy = SetupEnemy(grid);
        if (enemy != null)
        {
            units.Add(enemy);
        }

        return units;
    }

    private BattleUnit SetupPlayer(BattleGrid grid)
    {
        Transform aliceVisual = FindTransformByName(AliceRootName);

        if (usePlayerPlaceholder)
        {
            if (playerRoot == null || playerRoot.name != PlayerPlaceholderName)
            {
                playerRoot = CreateUnitRoot(
                    PlayerPlaceholderName,
                    grid.GetWorldPosition(new Vector2Int(8, 7)),
                    new Color(0.20f, 0.75f, 0.35f, 0.45f)).transform;
            }

            AttachAliceVisual(playerRoot, aliceVisual);
        }
        else if (playerRoot == null)
        {
            playerRoot = aliceVisual;
        }

        if (playerRoot == null)
        {
            Debug.LogWarning("BattleBootstrap: player root not found.");
            return null;
        }

        BattleUnit unit = playerRoot.GetComponent<BattleUnit>();
        if (unit == null)
        {
            unit = playerRoot.gameObject.AddComponent<BattleUnit>();
        }

        Vector2Int startCell = grid.WorldToCell(playerRoot.position);
        unit.maxHealth = 18;
        unit.moveRange = 4;
        unit.attackRange = 1;
        unit.attackDamage = 5;
        unit.footprintSize = 3;
        unit.yawOffset = 0f;
        unit.cellOffset = usePlayerPlaceholder ? Vector2Int.zero : playerCellOffset;
        unit.useAutoVisualAnchor = usePlayerPlaceholder ? false : playerUseAutoVisualAnchor;
        unit.worldOffset = usePlayerPlaceholder ? Vector3.zero : playerWorldOffset;
        unit.Setup(BattleTeam.Player, "Alice", startCell);
        unit.SetCell(startCell, grid.GetWorldPosition(startCell));
        unit.FaceToward(grid.GetWorldPosition(startCell + Vector2Int.right));
        grid.RegisterUnit(unit);
        return unit;
    }

    private BattleUnit SetupEnemy(BattleGrid grid)
    {
        GameObject enemyObject = CreateUnitRoot(
            "TrainingDummy",
            grid.GetWorldPosition(new Vector2Int(13, 12)),
            new Color(0.85f, 0.25f, 0.20f, 1f));

        BattleUnit unit = enemyObject.AddComponent<BattleUnit>();
        unit.maxHealth = 12;
        unit.moveRange = 3;
        unit.attackRange = 1;
        unit.attackDamage = 2;
        unit.footprintSize = 3;
        unit.yawOffset = 0f;
        unit.Setup(BattleTeam.Enemy, "TrainingDummy", new Vector2Int(13, 12));
        unit.FaceToward(grid.GetWorldPosition(new Vector2Int(12, 12)));
        grid.RegisterUnit(unit);
        return unit;
    }

    private GameObject CreateUnitRoot(string rootName, Vector3 worldPosition, Color color)
    {
        GameObject root = new GameObject(rootName);
        root.transform.position = worldPosition;
        root.transform.localScale = Vector3.one;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "UnitVisual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = placeholderScale;

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.material.color = color;

        return root;
    }

    private void AttachAliceVisual(Transform placeholderRoot, Transform aliceVisual)
    {
        if (placeholderRoot == null || aliceVisual == null || aliceVisual == placeholderRoot)
        {
            return;
        }

        aliceVisual.SetParent(placeholderRoot, false);
        aliceVisual.localPosition = aliceVisualLocalOffset;
        aliceVisual.localRotation = Quaternion.identity;
        aliceVisual.localScale = Vector3.one;
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
            Debug.LogWarning("BattleBootstrap: dungeon board not found.");
            return;
        }

        dungeonBoard.rotation = mainCamera.transform.rotation;
        dungeonBoard.position = mainCamera.transform.position + mainCamera.transform.forward * boardDistance + boardOffset;
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsOfType<Transform>();
        foreach (Transform current in transforms)
        {
            if (current.name == objectName)
            {
                return current;
            }
        }

        return null;
    }

    private void CleanupRuntimeObjects()
    {
        DestroyRuntimeObjects("BattleGrid");
        DestroyRuntimeObjects("TrainingDummy");
        DestroyRuntimeObjects("UnitVisual");

        DestroyRuntimeObjects(PlayerPlaceholderName);
    }

    private void DestroyRuntimeObjects(string objectName)
    {
        Transform[] transforms = FindObjectsOfType<Transform>();
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform target = transforms[i];
            if (target.name != objectName)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(target.gameObject);
            }
            else
            {
                DestroyImmediate(target.gameObject);
            }
        }
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
