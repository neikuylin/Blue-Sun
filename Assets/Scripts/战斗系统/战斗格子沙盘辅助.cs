using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class 战斗格子沙盘辅助 : MonoBehaviour
{
    [Header("锚点")]
    [FormerlySerializedAs("anchorWorldOffset")]
    [SerializeField] private Vector3 anchorLocalOffset = Vector3.zero;
    [SerializeField] private Vector2Int anchorCellInSandbox = new Vector2Int(10, 10);

    [Header("沙盘")]
    [SerializeField, Min(0.01f)] private float cellSize = 1f;
    [SerializeField, Min(1)] private int gridWidth = 20;
    [SerializeField, Min(1)] private int gridHeight = 20;

    [Header("2D视图")]
    [SerializeField] private Vector3 battleCameraEuler = new Vector3(48.6f, 45f, 0f);
    [SerializeField] private float localZOffset = -0.02f;

    [Header("显示")]
    [SerializeField] private bool drawSandbox = true;
    [SerializeField] private bool drawAnchorLabel = true;
    [SerializeField] private Color gridColor = new Color(0.1f, 0.85f, 1f, 0.85f);
    [SerializeField] private Color anchorColor = new Color(1f, 0.55f, 0.1f, 0.9f);

    public Vector2Int AnchorCellInSandbox => anchorCellInSandbox;

    private void OnValidate()
    {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(0.01f, cellSize);
        anchorCellInSandbox.x = Mathf.Clamp(anchorCellInSandbox.x, 0, gridWidth - 1);
        anchorCellInSandbox.y = Mathf.Clamp(anchorCellInSandbox.y, 0, gridHeight - 1);
    }

    private void OnDrawGizmos()
    {
        if (!drawSandbox)
        {
            return;
        }

        DrawGrid();
        DrawAnchor();
    }

    private void DrawGrid()
    {
        Color previousColor = Gizmos.color;
        Gizmos.color = gridColor;

        for (int x = 0; x <= gridWidth; x++)
        {
            Gizmos.DrawLine(
                GetSandboxCornerWorld(x, 0),
                GetSandboxCornerWorld(x, gridHeight));
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Gizmos.DrawLine(
                GetSandboxCornerWorld(0, y),
                GetSandboxCornerWorld(gridWidth, y));
        }

        Gizmos.color = previousColor;
    }

    private void DrawAnchor()
    {
        Vector3 anchorPreview = TransformProjectedGridPoint(0f, 0f);
        Vector3[] corners = ResolveCellPreviewCorners(anchorCellInSandbox);

        Color previousColor = Gizmos.color;
        Gizmos.color = anchorColor;
        DrawQuadOutline(corners);
        Gizmos.DrawSphere(anchorPreview, cellSize * 0.08f);
        Gizmos.color = previousColor;

#if UNITY_EDITOR
        if (drawAnchorLabel)
        {
            DrawLabel(anchorPreview, $"锚 {anchorCellInSandbox.x},{anchorCellInSandbox.y}", anchorColor);
        }
#endif
    }

    private Vector3 GetSandboxCornerWorld(int cornerX, int cornerY)
    {
        float deltaX = (cornerX - anchorCellInSandbox.x - 0.5f) * cellSize;
        float deltaZ = (cornerY - anchorCellInSandbox.y - 0.5f) * cellSize;
        return TransformProjectedGridPoint(deltaX, deltaZ);
    }

    private Vector3 TransformProjectedGridPoint(float gridDeltaX, float gridDeltaZ)
    {
        Vector2 localDelta = ProjectBattleGridTo2D(gridDeltaX, gridDeltaZ);
        return transform.TransformPoint(new Vector3(
            anchorLocalOffset.x + localDelta.x,
            anchorLocalOffset.y + localDelta.y,
            anchorLocalOffset.z + localZOffset));
    }

    private Vector2 ProjectBattleGridTo2D(float gridDeltaX, float gridDeltaZ)
    {
        Quaternion cameraRotation = Quaternion.Euler(battleCameraEuler);
        Vector3 gridDelta = new Vector3(gridDeltaX, 0f, gridDeltaZ);
        return new Vector2(
            Vector3.Dot(gridDelta, cameraRotation * Vector3.right),
            Vector3.Dot(gridDelta, cameraRotation * Vector3.up));
    }

    private Vector3[] ResolveCellPreviewCorners(Vector2Int cell)
    {
        return new[]
        {
            GetSandboxCornerWorld(cell.x, cell.y),
            GetSandboxCornerWorld(cell.x + 1, cell.y),
            GetSandboxCornerWorld(cell.x + 1, cell.y + 1),
            GetSandboxCornerWorld(cell.x, cell.y + 1)
        };
    }

    private static void DrawQuadOutline(Vector3[] corners)
    {
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
        Gizmos.DrawLine(corners[0], corners[2]);
        Gizmos.DrawLine(corners[1], corners[3]);
    }

#if UNITY_EDITOR
    private static void DrawLabel(Vector3 worldPosition, string label, Color color)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = color }
        };
        Handles.Label(worldPosition, label, style);
    }
#endif
}
