using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillTooltipRuntime : MonoBehaviour
{
    public struct Snapshot
    {
        public string skillId;
        public string displayName;
        public string description;
        public string ownerCharacterId;
        public string skillSource;
        public int hitRate;
        public int damage;
        public Sprite icon;
        public bool isEmpty;
    }

    private static SkillTooltipRuntime instance;

    private RectTransform tooltipRoot;
    private 战技内容视图 tooltipView;
    private GameObject runtimeTooltipInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(SkillTooltipRuntime));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillTooltipRuntime>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void Show(Snapshot snapshot)
    {
        if (instance == null || snapshot.isEmpty)
        {
            return;
        }

        instance.ShowInternal(snapshot);
    }

    public static void Hide()
    {
        instance?.HideInternal();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        tooltipRoot = null;
        tooltipView = null;
        EnsureTooltipInstance();
        CacheTooltipBindings();
        HideInternal();
    }

    private void EnsureTooltipInstance()
    {
        if (runtimeTooltipInstance != null)
        {
            tooltipRoot = runtimeTooltipInstance.transform as RectTransform;
            return;
        }

        SkillTooltipPrefabDatabase database = SkillTooltipPrefabDatabase.LoadDefault();
        if (database == null || database.combatArtTooltipPrefab == null)
        {
            return;
        }

        Transform parent = FindTooltipParent();
        if (parent == null)
        {
            return;
        }

        runtimeTooltipInstance = Instantiate(database.combatArtTooltipPrefab, parent, false);
        runtimeTooltipInstance.name = "战技内容";
        DisableRaycasts(runtimeTooltipInstance);
        runtimeTooltipInstance.SetActive(false);
        tooltipRoot = runtimeTooltipInstance.transform as RectTransform;
        tooltipView = runtimeTooltipInstance.GetComponent<战技内容视图>();
    }

    private void CacheTooltipBindings()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        tooltipView = tooltipRoot.GetComponent<战技内容视图>();
        if (tooltipView == null)
        {
            Debug.LogWarning("战技内容预制体缺少组件：战技内容视图。");
        }
    }

    private void ShowInternal(Snapshot snapshot)
    {
        EnsureTooltipInstance();
        CacheTooltipBindings();
        if (tooltipRoot == null)
        {
            return;
        }

        if (tooltipView == null)
        {
            Debug.LogWarning("战技内容无法显示：缺少战技内容视图。");
            return;
        }

        tooltipView.刷新(战技内容视图.构建显示数据(snapshot));

        PositionTooltip();
        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.SetAsLastSibling();
    }

    private void HideInternal()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    private void PositionTooltip()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        RectTransform parentRect = tooltipRoot.parent as RectTransform;
        Canvas canvas = tooltipRoot.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
        {
            Vector2 pivotOffset = new Vector2(
                tooltipRoot.rect.width * tooltipRoot.pivot.x,
                tooltipRoot.rect.height * tooltipRoot.pivot.y);
            tooltipRoot.anchoredPosition = localPoint + pivotOffset;
        }
    }

    private Transform FindTooltipParent()
    {
        Transform popupRoot = SceneHierarchyPathUtility.FindInActiveScene("Canvas/弹窗");
        if (popupRoot != null)
        {
            return popupRoot;
        }

        Transform canvasRoot = SceneHierarchyPathUtility.FindInActiveScene("Canvas");
        if (canvasRoot != null)
        {
            return canvasRoot;
        }

        Canvas canvas = FindObjectOfType<Canvas>(true);
        return canvas != null ? canvas.transform : null;
    }

    private static void DisableRaycasts(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }

        CanvasGroup[] canvasGroups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            CanvasGroup canvasGroup = canvasGroups[i];
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
