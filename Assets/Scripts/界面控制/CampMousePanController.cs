using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class CampMousePanController : MonoBehaviour
{
    private const string SceneName = "营地";
    private const string CanvasPath = "Canvas";
    private const string BackgroundPath = "Canvas/营地背景";

    [SerializeField] private RectTransform viewportRoot;
    [SerializeField] private RectTransform boundsBackground;
    [SerializeField] private float followSpeed = 6f;

    private readonly List<RectTransform> movableRects = new List<RectTransform>();
    private readonly List<Vector2> initialAnchoredPositions = new List<Vector2>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneName)
        {
            return;
        }

        if (FindObjectOfType<CampMousePanController>() != null)
        {
            return;
        }

        Transform canvasTransform = SceneHierarchyPathUtility.Find(activeScene, CanvasPath);
        if (canvasTransform == null)
        {
            Debug.LogWarning("CampMousePanController: missing Canvas in camp scene.");
            return;
        }

        RectTransform canvasRect = canvasTransform as RectTransform;
        RectTransform backgroundRect = SceneHierarchyPathUtility.Find(activeScene, BackgroundPath) as RectTransform;
        if (canvasRect == null || backgroundRect == null)
        {
            Debug.LogWarning("CampMousePanController: missing Canvas/营地背景 reference in camp scene.");
            return;
        }

        CampMousePanController controller = canvasTransform.gameObject.AddComponent<CampMousePanController>();
        controller.viewportRoot = canvasRect;
        controller.boundsBackground = backgroundRect;
    }

    private void Awake()
    {
        RefreshTargets();
    }

    private void OnEnable()
    {
        RefreshTargets();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!EnsureReferences())
        {
            return;
        }

        Vector2 maxOffset = ResolveMaxOffset();
        Vector2 mouseNormalized = ResolveMouseNormalizedPosition();
        Vector2 virtualCameraOffset = new Vector2(
            Mathf.Lerp(-maxOffset.x, maxOffset.x, mouseNormalized.x),
            Mathf.Lerp(-maxOffset.y, maxOffset.y, mouseNormalized.y));

        Vector2 targetDelta = -virtualCameraOffset;
        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSpeed) * Time.unscaledDeltaTime);

        for (int i = 0; i < movableRects.Count; i++)
        {
            RectTransform rect = movableRects[i];
            if (rect == null)
            {
                continue;
            }

            Vector2 desiredPosition = initialAnchoredPositions[i] + targetDelta;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, desiredPosition, t);
        }
    }

    private bool EnsureReferences()
    {
        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        if (boundsBackground == null)
        {
            boundsBackground = SceneHierarchyPathUtility.FindInActiveScene(BackgroundPath) as RectTransform;
        }

        if (viewportRoot == null || boundsBackground == null)
        {
            return false;
        }

        if (movableRects.Count == 0)
        {
            RefreshTargets();
        }

        return movableRects.Count > 0;
    }

    private void RefreshTargets()
    {
        movableRects.Clear();
        initialAnchoredPositions.Clear();

        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        if (boundsBackground == null)
        {
            boundsBackground = SceneHierarchyPathUtility.FindInActiveScene(BackgroundPath) as RectTransform;
        }

        if (viewportRoot == null)
        {
            return;
        }

        for (int i = 0; i < viewportRoot.childCount; i++)
        {
            RectTransform child = viewportRoot.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            movableRects.Add(child);
            initialAnchoredPositions.Add(child.anchoredPosition);
        }
    }

    private Vector2 ResolveMaxOffset()
    {
        if (viewportRoot == null || boundsBackground == null)
        {
            return Vector2.zero;
        }

        Vector2 viewportSize = viewportRoot.rect.size;
        Vector2 backgroundSize = boundsBackground.rect.size;

        float maxOffsetX = Mathf.Max(0f, (backgroundSize.x - viewportSize.x) * 0.5f);
        float maxOffsetY = Mathf.Max(0f, (backgroundSize.y - viewportSize.y) * 0.5f);
        return new Vector2(maxOffsetX, maxOffsetY);
    }

    private static Vector2 ResolveMouseNormalizedPosition()
    {
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        Vector3 mousePosition = Input.mousePosition;

        return new Vector2(
            Mathf.Clamp01(mousePosition.x / width),
            Mathf.Clamp01(mousePosition.y / height));
    }
}
