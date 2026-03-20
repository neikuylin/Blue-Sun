using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleDamageNumberPopup : MonoBehaviour
{
    private static BattleDamageNumberPopup instance;

    [Header("Motion")]
    public float speed = 90f;
    public float lifetime = 0.6f;
    public float startOffsetY = -18f;
    public float worldHeightOffset = 1.6f;

    [Header("Style")]
    public Color damageColor = Color.white;

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

        instance.ShowInternal(target, amount, worldCamera);
    }

    private void ShowInternal(BattleUnit target, int amount, Camera worldCamera)
    {
        if (templateRect == null || templateText == null)
        {
            return;
        }

        RectTransform popup = Instantiate(templateRect, templateRect.parent, false);
        popup.gameObject.name = templateRect.name + "_Runtime";
        BattleDamageNumberPopup popupScript = popup.GetComponent<BattleDamageNumberPopup>();
        if (popupScript != null)
        {
            Destroy(popupScript);
        }

        popup.gameObject.SetActive(true);

        TMP_Text text = popup.GetComponent<TMP_Text>();
        if (text == null)
        {
            Destroy(popup.gameObject);
            return;
        }

        text.text = amount.ToString();
        text.color = damageColor;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.gameObject.AddComponent<CanvasGroup>();
        }

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

        popup.anchoredPosition = anchoredPosition + new Vector2(0f, startOffsetY);
        StartCoroutine(AnimatePopup(popup, canvasGroup, anchoredPosition));
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
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return new Vector3(bounds.center.x, bounds.max.y + worldHeightOffset, bounds.center.z);
        }

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
}
