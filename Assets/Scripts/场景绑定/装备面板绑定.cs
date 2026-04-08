using UnityEngine;

[DisallowMultipleComponent]
public sealed class 装备面板绑定 : MonoBehaviour
{
    [SerializeField] private RectTransform equipmentContainer;

    public RectTransform EquipmentContainer
    {
        get
        {
            if (equipmentContainer != null)
            {
                return equipmentContainer;
            }

            return transform as RectTransform;
        }
    }

    private void Reset()
    {
        if (equipmentContainer == null)
        {
            equipmentContainer = transform as RectTransform;
        }
    }
}
