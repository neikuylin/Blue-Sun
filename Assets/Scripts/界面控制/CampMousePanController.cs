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

    public enum AxisUnlockMode
    {
        LockedToBackground,
        FreeXOnly,
        FreeYOnly,
        FreeBoth
    }

    [Serializable]
    public sealed class ForegroundBinding
    {
        public string name = "Binding";
        public RectTransform target;
        public AxisUnlockMode axisUnlockMode = AxisUnlockMode.LockedToBackground;
        public Vector2 freeAxisMultiplier = Vector2.one;
        public float followSpeed = 6f;
        public bool allowExceedCanvas = false;

        [NonSerialized] public Vector2 initialAnchoredPosition;
        [NonSerialized] public Vector2 backgroundRelativeOffset;
    }

    [Header("References")]
    [SerializeField] private RectTransform viewportRoot;
    [SerializeField] private RectTransform background;

    [Header("Background Motion")]
    [SerializeField] private Vector2 backgroundMoveMultiplier = new Vector2(0.35f, 0.35f);
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private bool backgroundAllowExceedCanvas = false;

    [Header("Foreground Bindings")]
    [SerializeField] private List<ForegroundBinding> foregroundBindings = new List<ForegroundBinding>();

    private Vector2 backgroundInitialAnchoredPosition;

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
        controller.background = SceneHierarchyPathUtility.Find(activeScene, BackgroundPath) as RectTransform;
        controller.RefreshBindings();
    }

    private void Awake()
    {
        RefreshBindings();
    }

    private void OnEnable()
    {
        RefreshBindings();
    }

    private void OnValidate()
    {
        followSpeed = Mathf.Max(0.01f, followSpeed);
        RefreshBindings();
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
        Vector2 backgroundMaxOffset = ResolveBackgroundMaxOffset();
        Vector2 backgroundVirtualOffset = new Vector2(
            Mathf.Lerp(-backgroundMaxOffset.x, backgroundMaxOffset.x, mouseNormalized.x),
            Mathf.Lerp(-backgroundMaxOffset.y, backgroundMaxOffset.y, mouseNormalized.y));
        Vector2 backgroundTarget = backgroundInitialAnchoredPosition - backgroundVirtualOffset;

        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        background.anchoredPosition = Vector2.Lerp(background.anchoredPosition, backgroundTarget, t);

        Vector2 backgroundDelta = background.anchoredPosition - backgroundInitialAnchoredPosition;
        for (int i = 0; i < foregroundBindings.Count; i++)
        {
            ForegroundBinding binding = foregroundBindings[i];
            if (binding == null || binding.target == null)
            {
                continue;
            }

            Vector2 freeAxisTarget = ResolveFreeAxisTarget(binding, mouseNormalized);
            Vector2 targetPosition = ResolveForegroundTarget(binding, backgroundDelta, freeAxisTarget);
            float bindingT = 1f - Mathf.Exp(-Mathf.Max(0.01f, binding.followSpeed) * Time.unscaledDeltaTime);
            binding.target.anchoredPosition = ResolveSmoothedForegroundPosition(
                binding,
                binding.target.anchoredPosition,
                targetPosition,
                bindingT);
        }
    }

    [ContextMenu("Refresh Bindings")]
    public void RefreshBindings()
    {
        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        if (background == null)
        {
            background = SceneHierarchyPathUtility.FindInActiveScene(BackgroundPath) as RectTransform;
        }

        if (background != null)
        {
            backgroundInitialAnchoredPosition = background.anchoredPosition;
        }

        for (int i = foregroundBindings.Count - 1; i >= 0; i--)
        {
            ForegroundBinding binding = foregroundBindings[i];
            if (binding == null)
            {
                foregroundBindings.RemoveAt(i);
                continue;
            }

            if (binding.target == null)
            {
                continue;
            }

            binding.followSpeed = Mathf.Max(0.01f, binding.followSpeed);

            binding.initialAnchoredPosition = binding.target.anchoredPosition;
            binding.backgroundRelativeOffset = background != null
                ? binding.initialAnchoredPosition - backgroundInitialAnchoredPosition
                : binding.initialAnchoredPosition;

            if (string.IsNullOrWhiteSpace(binding.name))
            {
                binding.name = binding.target.name;
            }
        }
    }

    private bool EnsureReferences()
    {
        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        if (background == null)
        {
            background = SceneHierarchyPathUtility.FindInActiveScene(BackgroundPath) as RectTransform;
        }

        if (viewportRoot == null || background == null)
        {
            return false;
        }

        return true;
    }

    private Vector2 ResolveBackgroundMaxOffset()
    {
        if (viewportRoot == null || background == null)
        {
            return Vector2.zero;
        }

        if (backgroundAllowExceedCanvas)
        {
            return new Vector2(
                Mathf.Abs(backgroundMoveMultiplier.x) * 10000f,
                Mathf.Abs(backgroundMoveMultiplier.y) * 10000f);
        }

        Vector2 viewportSize = viewportRoot.rect.size;
        Vector2 backgroundSize = background.rect.size;
        Vector2 baseMaxOffset = new Vector2(
            Mathf.Max(0f, (backgroundSize.x - viewportSize.x) * 0.5f),
            Mathf.Max(0f, (backgroundSize.y - viewportSize.y) * 0.5f));

        return new Vector2(
            Mathf.Abs(backgroundMoveMultiplier.x) <= 0.0001f ? 0f : baseMaxOffset.x / Mathf.Abs(backgroundMoveMultiplier.x),
            Mathf.Abs(backgroundMoveMultiplier.y) <= 0.0001f ? 0f : baseMaxOffset.y / Mathf.Abs(backgroundMoveMultiplier.y));
    }

    private Vector2 ResolveFreeAxisTarget(ForegroundBinding binding, Vector2 mouseNormalized)
    {
        if (binding == null || binding.target == null)
        {
            return Vector2.zero;
        }

        if (binding.allowExceedCanvas)
        {
            float freeUnlimitedX = Mathf.Abs(binding.freeAxisMultiplier.x) <= 0.0001f
                ? 0f
                : Mathf.Lerp(-10000f, 10000f, mouseNormalized.x) * binding.freeAxisMultiplier.x;
            float freeUnlimitedY = Mathf.Abs(binding.freeAxisMultiplier.y) <= 0.0001f
                ? 0f
                : Mathf.Lerp(-10000f, 10000f, mouseNormalized.y) * binding.freeAxisMultiplier.y;

            return new Vector2(freeUnlimitedX, freeUnlimitedY);
        }

        Vector2 viewportSize = viewportRoot != null ? viewportRoot.rect.size : Vector2.zero;
        Vector2 targetSize = binding.target.rect.size;
        Vector2 baseMaxOffset = new Vector2(
            Mathf.Max(0f, (targetSize.x - viewportSize.x) * 0.5f),
            Mathf.Max(0f, (targetSize.y - viewportSize.y) * 0.5f));

        float freeX = Mathf.Abs(binding.freeAxisMultiplier.x) <= 0.0001f
            ? 0f
            : Mathf.Lerp(-baseMaxOffset.x, baseMaxOffset.x, mouseNormalized.x) * binding.freeAxisMultiplier.x;
        float freeY = Mathf.Abs(binding.freeAxisMultiplier.y) <= 0.0001f
            ? 0f
            : Mathf.Lerp(-baseMaxOffset.y, baseMaxOffset.y, mouseNormalized.y) * binding.freeAxisMultiplier.y;

        return new Vector2(freeX, freeY);
    }

    private Vector2 ResolveForegroundTarget(ForegroundBinding binding, Vector2 backgroundDelta, Vector2 freeAxisTarget)
    {
        Vector2 lockedBase = backgroundInitialAnchoredPosition + binding.backgroundRelativeOffset + backgroundDelta;
        switch (binding.axisUnlockMode)
        {
            case AxisUnlockMode.FreeXOnly:
                return new Vector2(binding.initialAnchoredPosition.x - freeAxisTarget.x, lockedBase.y);

            case AxisUnlockMode.FreeYOnly:
                return new Vector2(lockedBase.x, binding.initialAnchoredPosition.y - freeAxisTarget.y);

            case AxisUnlockMode.FreeBoth:
                return new Vector2(
                    binding.initialAnchoredPosition.x - freeAxisTarget.x,
                    binding.initialAnchoredPosition.y - freeAxisTarget.y);

            case AxisUnlockMode.LockedToBackground:
            default:
                return lockedBase;
        }
    }

    private static Vector2 ResolveSmoothedForegroundPosition(
        ForegroundBinding binding,
        Vector2 currentPosition,
        Vector2 targetPosition,
        float smoothing)
    {
        if (binding == null)
        {
            return targetPosition;
        }

        switch (binding.axisUnlockMode)
        {
            case AxisUnlockMode.FreeXOnly:
                return new Vector2(
                    Mathf.Lerp(currentPosition.x, targetPosition.x, smoothing),
                    targetPosition.y);

            case AxisUnlockMode.FreeYOnly:
                return new Vector2(
                    targetPosition.x,
                    Mathf.Lerp(currentPosition.y, targetPosition.y, smoothing));

            case AxisUnlockMode.FreeBoth:
                return Vector2.Lerp(currentPosition, targetPosition, smoothing);

            case AxisUnlockMode.LockedToBackground:
            default:
                return targetPosition;
        }
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
