using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class HoverTooltipController : MonoBehaviour
{
    public enum HoverCategory
    {
        Item,
        Skill,
        Buff
    }

    private static HoverTooltipController instance;

    private HoverCategory pendingCategory;
    private Transform pendingRoot;
    private float pendingShownAt;
    private Action pendingShow;
    private Action pendingHide;

    private HoverCategory hoveredCategory;
    private Transform hoveredRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(HoverTooltipController));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HoverTooltipController>();
    }

    public static void BeginHover(
        HoverCategory category,
        Transform root,
        float delaySeconds,
        Action showAction,
        Action hideAction)
    {
        if (instance == null || root == null || showAction == null)
        {
            return;
        }

        if (instance.hoveredRoot != null &&
            (instance.hoveredRoot != root || instance.hoveredCategory != category))
        {
            instance.pendingHide?.Invoke();
            instance.hoveredRoot = null;
        }

        instance.pendingCategory = category;
        instance.pendingRoot = root;
        instance.pendingShownAt = Time.unscaledTime + Mathf.Max(0f, delaySeconds);
        instance.pendingShow = showAction;
        instance.pendingHide = hideAction;
    }

    public static void EndHover(HoverCategory category, Transform root, PointerEventData eventData)
    {
        if (instance == null || root == null)
        {
            return;
        }

        Transform pointerTransform = eventData != null
            ? (eventData.pointerEnter != null
                ? eventData.pointerEnter.transform
                : eventData.pointerCurrentRaycast.gameObject != null ? eventData.pointerCurrentRaycast.gameObject.transform : null)
            : null;

        if (pointerTransform != null && (pointerTransform == root || pointerTransform.IsChildOf(root)))
        {
            return;
        }

        if (instance.pendingRoot == root && instance.pendingCategory == category)
        {
            instance.pendingRoot = null;
            instance.pendingShow = null;
            instance.pendingShownAt = 0f;
        }

        if (instance.hoveredRoot == root && instance.hoveredCategory == category)
        {
            instance.hoveredRoot = null;
            instance.pendingHide?.Invoke();
        }
    }

    public static void Cancel(HoverCategory category, Action hideAction = null)
    {
        if (instance == null)
        {
            return;
        }

        if (instance.pendingCategory == category)
        {
            instance.pendingRoot = null;
            instance.pendingShow = null;
            instance.pendingShownAt = 0f;
        }

        if (instance.hoveredCategory == category)
        {
            instance.hoveredRoot = null;
        }

        if (hideAction != null)
        {
            hideAction.Invoke();
        }
    }

    private void Update()
    {
        if (pendingRoot == null || pendingShow == null)
        {
            return;
        }

        if (Time.unscaledTime < pendingShownAt)
        {
            return;
        }

        hoveredCategory = pendingCategory;
        hoveredRoot = pendingRoot;
        pendingRoot = null;
        pendingShownAt = 0f;
        Action showAction = pendingShow;
        pendingShow = null;
        showAction.Invoke();
    }
}
