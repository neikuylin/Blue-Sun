using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class HoverTargetActive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject target;
    [SerializeField] private bool hideTargetOnEnable = true;

    private bool isPointerInside;
    private bool isSelected;

    private void OnEnable()
    {
        isPointerInside = false;
        isSelected = false;

        if (hideTargetOnEnable)
        {
            ApplyState(false);
        }
    }

    private void OnDisable()
    {
        isPointerInside = false;
        isSelected = false;
        ApplyState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        RefreshState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        RefreshState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        RefreshState();
    }

    private void RefreshState()
    {
        ApplyState(isPointerInside || isSelected);
    }

    private void ApplyState(bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }
}
