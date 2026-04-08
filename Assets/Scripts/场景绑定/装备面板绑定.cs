using UnityEngine;

[DisallowMultipleComponent]
public sealed class 装备面板绑定 : MonoBehaviour
{
    public enum 回流目标类型
    {
        背包,
        仓库
    }

    [SerializeField] private RectTransform equipmentContainer;
    [SerializeField] private 回流目标类型 returnTarget = 回流目标类型.背包;

    public RectTransform EquipmentContainer
    {
        get
        {
            return equipmentContainer;
        }
    }

    public 回流目标类型 ReturnTarget => returnTarget;
}
