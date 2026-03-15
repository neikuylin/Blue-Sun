using UnityEngine;

[DisallowMultipleComponent]
public sealed class JourneySceneBindings : MonoBehaviour
{
    [Header("Skill Slots")]
    public RectTransform skillSlotContainer;

    [Header("Inventory")]
    public RectTransform warehouseContainer;
    public RectTransform backpackContainer;
    public RectTransform equipmentContainer;
    public RectTransform quickSlotAnchor;

    public static JourneySceneBindings FindInActiveScene()
    {
        return FindObjectOfType<JourneySceneBindings>(true);
    }
}
