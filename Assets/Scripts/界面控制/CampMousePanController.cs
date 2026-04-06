using System;
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

    [Serializable]
    private sealed class ParallaxLayer
    {
        public string name = "Layer";
        public RectTransform target;
        public Vector2 moveMultiplier = Vector2.one;
        public bool clampToBackground = true;

        [NonSerialized] public Vector2 initialAnchoredPosition;
    }

    [Header("References")]
    [SerializeField] private RectTransform viewportRoot;
    [SerializeField] private RectTransform boundsBackground;

    [Header("Motion")]
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private bool autoPopulateCanvasChildren = true;
    [SerializeField] private bool includeBackgroundInAutoPopulate = true;

    [Header("Parallax Layers")]
    [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

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

        CampMousePanController controller = canvasTransform.gameObject.AddComponent<CampMousePanController>();
        controller.viewportRoot = canvasTransform as RectTransform;
        controller.boundsBackground = SceneHierarchyPathUtility.Find(activeScene, BackgroundPath) as RectTransform;
        controller.RefreshConfiguredLayers();
    }

    private void Awake()
    {
        RefreshConfiguredLayers();
    }

    private void OnEnable()
    {
        RefreshConfiguredLayers();
    }

    private void OnValidate()
    {
        followSpeed = Mathf.Max(0.01f, followSpeed);
        RefreshConfiguredLayers();
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

        Vector2 mouseNormalized = ResolveMouseNormalizedPosition();

        for (int i = 0; i < layers.Count; i++)
        {
            ParallaxLayer layer = layers[i];
            if (layer == null || layer.target == null)
            {
                continue;
            }

            Vector2 maxOffset = ResolveLayerMaxOffset(layer);
            Vector2 virtualCameraOffset = new Vector2(
                Mathf.Lerp(-maxOffset.x, maxOffset.x, mouseNormalized.x),
                Mathf.Lerp(-maxOffset.y, maxOffset.y, mouseNormalized.y));

            Vector2 targetPosition = layer.initialAnchoredPosition - virtualCameraOffset;
            float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            layer.target.anchoredPosition = Vector2.Lerp(layer.target.anchoredPosition, targetPosition, t);
        }
    }

    [ContextMenu("Refresh Configured Layers")]
    public void RefreshConfiguredLayers()
    {
        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        if (boundsBackground == null)
        {
            boundsBackground = SceneHierarchyPathUtility.FindInActiveScene(BackgroundPath) as RectTransform;
        }

        if (autoPopulateCanvasChildren)
        {
            AutoPopulateLayers();
        }

        for (int i = layers.Count - 1; i >= 0; i--)
        {
            ParallaxLayer layer = layers[i];
            if (layer == null || layer.target == null)
            {
                layers.RemoveAt(i);
                continue;
            }

            layer.initialAnchoredPosition = layer.target.anchoredPosition;
            if (string.IsNullOrWhiteSpace(layer.name))
            {
                layer.name = layer.target.name;
            }
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

        if (layers.Count == 0)
        {
            RefreshConfiguredLayers();
        }

        return layers.Count > 0;
    }

    private void AutoPopulateLayers()
    {
        if (viewportRoot == null)
        {
            return;
        }

        List<ParallaxLayer> rebuiltLayers = new List<ParallaxLayer>();
        for (int i = 0; i < viewportRoot.childCount; i++)
        {
            RectTransform child = viewportRoot.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            bool isBackground = child == boundsBackground;
            if (!includeBackgroundInAutoPopulate && isBackground)
            {
                continue;
            }

            ParallaxLayer existing = FindLayerByTarget(child);
            if (existing == null)
            {
                existing = new ParallaxLayer
                {
                    name = child.name,
                    target = child,
                    moveMultiplier = isBackground ? new Vector2(0.35f, 0.35f) : Vector2.one,
                    clampToBackground = true
                };
            }

            rebuiltLayers.Add(existing);
        }

        layers = rebuiltLayers;
    }

    private ParallaxLayer FindLayerByTarget(RectTransform target)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            ParallaxLayer layer = layers[i];
            if (layer != null && layer.target == target)
            {
                return layer;
            }
        }

        return null;
    }

    private Vector2 ResolveLayerMaxOffset(ParallaxLayer layer)
    {
        if (layer == null || layer.target == null || viewportRoot == null)
        {
            return Vector2.zero;
        }

        if (!layer.clampToBackground)
        {
            return new Vector2(
                Mathf.Abs(layer.moveMultiplier.x) * 10000f,
                Mathf.Abs(layer.moveMultiplier.y) * 10000f);
        }

        Vector2 viewportSize = viewportRoot.rect.size;
        Vector2 layerSize = layer.target.rect.size;
        Vector2 baseMaxOffset = new Vector2(
            Mathf.Max(0f, (layerSize.x - viewportSize.x) * 0.5f),
            Mathf.Max(0f, (layerSize.y - viewportSize.y) * 0.5f));

        return new Vector2(
            Mathf.Abs(layer.moveMultiplier.x) <= 0.0001f ? 0f : baseMaxOffset.x / Mathf.Abs(layer.moveMultiplier.x),
            Mathf.Abs(layer.moveMultiplier.y) <= 0.0001f ? 0f : baseMaxOffset.y / Mathf.Abs(layer.moveMultiplier.y));
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
