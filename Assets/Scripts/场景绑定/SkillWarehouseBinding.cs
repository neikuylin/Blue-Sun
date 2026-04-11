using UnityEngine;

[DisallowMultipleComponent]
public class SkillWarehouseBinding : MonoBehaviour
{
    [SerializeField]
    private RectTransform warehousePanel;

    [SerializeField]
    private RectTransform warehouseSlotArea;

    [SerializeField]
    private RectTransform slotTemplate;

    public RectTransform ResolveWarehousePanel()
    {
        return warehousePanel;
    }

    public RectTransform ResolveWarehouseContainer()
    {
        return warehouseSlotArea;
    }

    public RectTransform ResolveWarehouseSlotTemplate()
    {
        return slotTemplate;
    }

    public void SetAutoBindReferences(RectTransform panel, RectTransform slotArea)
    {
        warehousePanel = panel;
        warehouseSlotArea = slotArea;
    }

    public static SkillWarehouseBinding FindBindingInActiveScene()
    {
        return FindObjectOfType<SkillWarehouseBinding>(true);
    }
}
