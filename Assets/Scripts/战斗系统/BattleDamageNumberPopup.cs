using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleDamageNumberPopup : MonoBehaviour
{
    public struct DamageSegment
    {
        public string text;
        public Color color;
    }

    private static BattleDamageNumberPopup instance;

    [Header("Motion")]
    public float speed = 90f;
    public float lifetime = 0.6f;
    public float startOffsetY = -18f;
    public float worldHeightOffset = 0.9f;

    [Header("Style")]
    public Color damageColor = Color.white;
    public Color missColor = Color.white;

    private RectTransform templateRect;
    private TMP_Text templateText;
    private Canvas rootCanvas;
    private CanvasGroup templateCanvasGroup;

    private void Awake()
    {
        templateRect = transform as RectTransform;
        templateText = GetComponent<TMP_Text>();
        rootCanvas = GetComponentInParent<Canvas>();
        templateCanvasGroup = GetComponent<CanvasGroup>();

        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        HideTemplateVisual();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void Show(BattleUnit target, int amount, Camera worldCamera = null)
    {
        if (instance == null || target == null || amount <= 0)
        {
            return;
        }

        instance.ShowInternal(target, amount.ToString(), instance.damageColor, worldCamera);
    }

    public static void ShowText(BattleUnit target, string content, Camera worldCamera = null, GameObject popupPrefab = null, Color? popupColor = null)
    {
        if (instance == null || target == null || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        instance.ShowInternal(target, content, popupColor ?? instance.damageColor, worldCamera, popupPrefab);
    }

    public static void ShowSegments(BattleUnit target, IList<DamageSegment> segments, Camera worldCamera = null)
    {
        if (instance == null || target == null || segments == null || segments.Count == 0)
        {
            return;
        }

        string content = BuildSegmentContent(segments);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        instance.ShowInternal(target, content, Color.white, worldCamera);
    }

    public static void ShowMiss(BattleUnit target, Camera worldCamera = null)
    {
        if (instance == null || target == null)
        {
            return;
        }

        instance.ShowInternal(target, "MISS", instance.missColor, worldCamera);
    }

    private void ShowInternal(BattleUnit target, string content, Color popupColor, Camera worldCamera, GameObject popupPrefab = null)
    {
        if (templateRect == null || templateText == null || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        RectTransform popup;
        TMP_Text text;
        CanvasGroup canvasGroup;
        Vector2 popupOffset;
        if (!TryCreatePopupInstance(popupPrefab, out popup, out text, out canvasGroup, out popupOffset))
        {
            return;
        }

        text.text = content;
        text.color = popupColor;
        text.richText = true;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        text.enabled = true;

        Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
        Vector3 worldPosition = ResolvePopupWorldPosition(target);
        Vector2 anchoredPosition;
        if (!TryResolveAnchoredPosition(worldPosition, popup.parent as RectTransform, cameraToUse, out anchoredPosition))
        {
            Destroy(popup.gameObject);
            return;
        }

        Vector2 startPosition = anchoredPosition + popupOffset + new Vector2(0f, startOffsetY);
        Vector2 targetPosition = anchoredPosition + popupOffset;
        popup.anchoredPosition = startPosition;
        StartCoroutine(AnimatePopup(popup, canvasGroup, targetPosition));
    }

    private bool TryCreatePopupInstance(GameObject popupPrefab, out RectTransform popup, out TMP_Text text, out CanvasGroup canvasGroup, out Vector2 popupOffset)
    {
        popup = null;
        text = null;
        canvasGroup = null;
        popupOffset = Vector2.zero;

        if (popupPrefab != null)
        {
            GameObject popupObject = Instantiate(popupPrefab, templateRect.parent, false);
            popupObject.name = popupPrefab.name + "_Runtime";
            popupObject.SetActive(true);

            popup = popupObject.GetComponent<RectTransform>();
            text = popupObject.GetComponentInChildren<TMP_Text>(true);
            if (popup == null || text == null)
            {
                Destroy(popupObject);
                return false;
            }

            popupOffset = popup.anchoredPosition;
            canvasGroup = popupObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = popupObject.AddComponent<CanvasGroup>();
            }

            return true;
        }

        popup = Instantiate(templateRect, templateRect.parent, false);
        popup.gameObject.name = templateRect.name + "_Runtime";
        BattleDamageNumberPopup popupScript = popup.GetComponent<BattleDamageNumberPopup>();
        if (popupScript != null)
        {
            Destroy(popupScript);
        }

        popup.gameObject.SetActive(true);
        text = popup.GetComponent<TMP_Text>();
        if (text == null)
        {
            Destroy(popup.gameObject);
            return false;
        }

        popupOffset = popup.anchoredPosition;
        canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.gameObject.AddComponent<CanvasGroup>();
        }

        return true;
    }

    private void HideTemplateVisual()
    {
        if (templateText != null)
        {
            templateText.enabled = false;
        }

        if (templateCanvasGroup == null)
        {
            templateCanvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (templateCanvasGroup == null)
            {
                templateCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        templateCanvasGroup.alpha = 0f;
        templateCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator AnimatePopup(RectTransform popup, CanvasGroup canvasGroup, Vector2 targetPosition)
    {
        if (popup == null || canvasGroup == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.05f, lifetime);
        float moveSpeed = Mathf.Max(0f, speed);
        Vector2 position = popup.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration && popup != null)
        {
            elapsed += Time.deltaTime;
            position.y += moveSpeed * Time.deltaTime;
            popup.anchoredPosition = new Vector2(targetPosition.x, position.y);
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (popup != null)
        {
            Destroy(popup.gameObject);
        }
    }

    private Vector3 ResolvePopupWorldPosition(BattleUnit target)
    {
        return target.transform.position + Vector3.up * worldHeightOffset;
    }

    private bool TryResolveAnchoredPosition(Vector3 worldPosition, RectTransform parentRect, Camera worldCamera, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;
        if (parentRect == null)
        {
            return false;
        }

        Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
        Vector3 screenPoint = cameraToUse != null
            ? cameraToUse.WorldToScreenPoint(worldPosition)
            : RectTransformUtility.WorldToScreenPoint(null, worldPosition);

        if (screenPoint.z < 0f)
        {
            return false;
        }

        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPoint,
            uiCamera,
            out anchoredPosition);
    }

    private static string BuildSegmentContent(IList<DamageSegment> segments)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < segments.Count; i++)
        {
            DamageSegment segment = segments[i];
            if (string.IsNullOrWhiteSpace(segment.text))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("<color=#FFFFFF>+</color>");
            }

            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(segment.color));
            builder.Append(">");
            builder.Append(segment.text);
            builder.Append("</color>");
        }

        return builder.ToString();
    }
}
