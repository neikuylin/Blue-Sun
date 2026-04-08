using UnityEngine;

[DisallowMultipleComponent]
public class 技能仓库绑定 : MonoBehaviour
{
    [Header("技能仓库格子模板")]
    public RectTransform 技能仓库格子;

    public RectTransform ResolveWarehouseContainer()
    {
        return transform as RectTransform;
    }

    public RectTransform ResolveWarehouseSlotTemplate()
    {
        return 技能仓库格子;
    }

    public static 技能仓库绑定 FindBindingInActiveScene()
    {
        return FindObjectOfType<技能仓库绑定>(true);
    }
}
