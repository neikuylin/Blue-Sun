using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SkillWarehouseBinding : MonoBehaviour
{
    [FormerlySerializedAs("warehouseContainer")]
    [SerializeField]
    private RectTransform warehousePanel;

    [SerializeField]
    private RectTransform warehouseSlotArea;

    [FormerlySerializedAs("warehouseSlotTemplate")]
    [SerializeField]
    private RectTransform slotTemplate;

    public RectTransform ResolveWarehousePanel()
    {
        return warehousePanel != null ? warehousePanel : transform as RectTransform;
    }

    public RectTransform ResolveWarehouseContainer()
    {
        return warehouseSlotArea != null ? warehouseSlotArea : ResolveWarehousePanel();
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
