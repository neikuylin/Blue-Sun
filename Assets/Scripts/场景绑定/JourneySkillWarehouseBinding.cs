using UnityEngine;

[DisallowMultipleComponent]
public sealed class JourneySkillWarehouseBinding : MonoBehaviour
{
    [Header("技能仓库格子容器")]
    public RectTransform warehouseContainer;

    [Header("技能仓库格子模板，可留空")]
    public RectTransform warehouseSlotTemplate;

    [Header("角色技能栏格子容器，可留空")]
    public RectTransform skillSlotContainer;

    public RectTransform ResolveWarehouseContainer()
    {
        return warehouseContainer != null ? warehouseContainer : transform as RectTransform;
    }

    public RectTransform ResolveWarehouseSlotTemplate()
    {
        if (warehouseSlotTemplate != null)
        {
            return warehouseSlotTemplate;
        }

        RectTransform container = ResolveWarehouseContainer();
        return container != null && container.childCount > 0 ? container.GetChild(0) as RectTransform : null;
    }

    public RectTransform ResolveSkillSlotContainer()
    {
        return skillSlotContainer;
    }

    public static JourneySkillWarehouseBinding FindBindingInActiveScene()
    {
        return FindObjectOfType<JourneySkillWarehouseBinding>(true);
    }
}
