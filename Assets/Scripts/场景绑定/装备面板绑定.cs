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
    [SerializeField] private RectTransform 主手栏位;
    [SerializeField] private RectTransform 副手栏位;
    [SerializeField] private RectTransform 头盔栏位;
    [SerializeField] private RectTransform 胸甲栏位;
    [SerializeField] private RectTransform 手套栏位;
    [SerializeField] private RectTransform 鞋子栏位;
    [SerializeField] private RectTransform 腿甲栏位;
    [SerializeField] private RectTransform 饰品栏位;
    [SerializeField] private 回流目标类型 returnTarget = 回流目标类型.背包;

    public RectTransform EquipmentContainer => equipmentContainer;
    public RectTransform MainHandSlot => 主手栏位;
    public RectTransform OffHandSlot => 副手栏位;
    public RectTransform HelmetSlot => 头盔栏位;
    public RectTransform ArmorSlot => 胸甲栏位;
    public RectTransform GlovesSlot => 手套栏位;
    public RectTransform ShoesSlot => 鞋子栏位;
    public RectTransform LegArmorSlot => 腿甲栏位;
    public RectTransform AccessorySlot => 饰品栏位;
    public 回流目标类型 ReturnTarget => returnTarget;
}
