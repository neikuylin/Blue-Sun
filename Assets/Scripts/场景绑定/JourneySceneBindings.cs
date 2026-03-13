using UnityEngine;

[DisallowMultipleComponent]
public sealed class JourneySceneBindings : MonoBehaviour
{
    [Header("技能栏")]
    public RectTransform skillSlotContainer;

    [Header("仓库与背包")]
    public RectTransform warehouseContainer;
    public RectTransform backpackContainer;
    public RectTransform equipmentContainer;
    public RectTransform quickSlotAnchor;

    public static JourneySceneBindings FindInActiveScene()
    {
        return FindObjectOfType<JourneySceneBindings>(true);
    }
}
