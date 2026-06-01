using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField, InspectorName("不可拖入变暗颜色")] private Color 不可拖入变暗颜色 = new Color32(100, 100, 100, 255);

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

    private readonly Dictionary<Image, Color> 原始图片颜色 = new Dictionary<Image, Color>();

    private void OnEnable()
    {
        InventoryShortcutRuntimeBinder.装备物品拖拽开始 += 处理装备物品拖拽开始;
        InventoryShortcutRuntimeBinder.装备物品拖拽结束 += 恢复全部栏位颜色;
    }

    private void OnDisable()
    {
        InventoryShortcutRuntimeBinder.装备物品拖拽开始 -= 处理装备物品拖拽开始;
        InventoryShortcutRuntimeBinder.装备物品拖拽结束 -= 恢复全部栏位颜色;
        恢复全部栏位颜色();
    }

    private void 处理装备物品拖拽开始(ItemDatabase.ItemEntry itemEntry)
    {
        if (itemEntry == null || itemEntry.category != ItemDatabase.ItemCategory.Equipment)
        {
            恢复全部栏位颜色();
            return;
        }

        原始图片颜色.Clear();
        应用栏位拖拽提示(主手栏位, itemEntry, ItemDatabase.EquipmentSlotType.MainHand);
        应用栏位拖拽提示(副手栏位, itemEntry, ItemDatabase.EquipmentSlotType.OffHand);
        应用栏位拖拽提示(头盔栏位, itemEntry, ItemDatabase.EquipmentSlotType.Helmet);
        应用栏位拖拽提示(胸甲栏位, itemEntry, ItemDatabase.EquipmentSlotType.Armor);
        应用栏位拖拽提示(手套栏位, itemEntry, ItemDatabase.EquipmentSlotType.Gloves);
        应用栏位拖拽提示(鞋子栏位, itemEntry, ItemDatabase.EquipmentSlotType.Shoes);
        应用栏位拖拽提示(腿甲栏位, itemEntry, ItemDatabase.EquipmentSlotType.LegArmor);
        应用栏位拖拽提示(饰品栏位, itemEntry, ItemDatabase.EquipmentSlotType.Accessory);
    }

    private void 应用栏位拖拽提示(RectTransform slotRoot, ItemDatabase.ItemEntry itemEntry, ItemDatabase.EquipmentSlotType slotType)
    {
        if (slotRoot == null)
        {
            return;
        }

        bool canDrop = InventoryShortcutRuntimeBinder.IsEquipmentSlotCompatible(itemEntry.equipmentSlot, slotType);
        Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            if (!原始图片颜色.ContainsKey(image))
            {
                原始图片颜色.Add(image, image.color);
            }

            if (!canDrop)
            {
                Color dimColor = 不可拖入变暗颜色;
                dimColor.a = image.color.a;
                image.color = dimColor;
            }
        }
    }

    private void 恢复全部栏位颜色()
    {
        foreach (KeyValuePair<Image, Color> item in 原始图片颜色)
        {
            if (item.Key != null)
            {
                item.Key.color = item.Value;
            }
        }

        原始图片颜色.Clear();
    }
}
