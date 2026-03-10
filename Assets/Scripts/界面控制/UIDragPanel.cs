using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIDragPanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform dragTarget;

    private Canvas rootCanvas;
    private Vector2 startAnchoredPosition;
    private Vector2 pointerStartLocalPosition;

    public void SetDragTarget(RectTransform target)
    {
        dragTarget = target;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryPrepareDrag(eventData))
        {
            return;
        }

        startAnchoredPosition = dragTarget.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragTarget == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (canvasRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out Vector2 currentLocalPosition))
        {
            return;
        }

        Vector2 delta = currentLocalPosition - pointerStartLocalPosition;
        dragTarget.anchoredPosition = startAnchoredPosition + delta;
    }

    private bool TryPrepareDrag(PointerEventData eventData)
    {
        if (dragTarget == null)
        {
            dragTarget = transform as RectTransform;
        }

        if (dragTarget == null)
        {
            return false;
        }

        rootCanvas = dragTarget.GetComponentInParent<Canvas>();
        RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        return canvasRect != null &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out pointerStartLocalPosition);
    }
}
