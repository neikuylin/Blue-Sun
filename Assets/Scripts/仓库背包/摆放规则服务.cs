using System;
using System.Collections.Generic;

internal sealed class 摆放规则服务
{
    internal sealed class Context
    {
        public Func<InventoryShortcutRuntimeBinder.SlotKind, List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetDataList;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, bool> IsSlotUsable;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, bool, int> GetOneByTwoExtensionIndex;
        public Func<int> GetOffHandEquipmentSlotIndex;
    }

    public static bool 是一乘二物品(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        return entry.weaponCategory == ItemDatabase.WeaponCategory.Bow ||
            entry.weaponCategory == ItemDatabase.WeaponCategory.TwoHanded ||
            entry.weaponCategory == ItemDatabase.WeaponCategory.Staff;
    }

    public bool 可以使用装备槽位(
        Context context,
        int index,
        ItemDatabase.EquipmentSlotType slotType,
        bool requireEmpty,
        ItemDatabase.ItemEntry entry,
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipmentData)
    {
        if (equipmentData == null || index < 0 || index >= equipmentData.Count)
        {
            return false;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData targetData = equipmentData[index];
        if (targetData.isFootprintExtension)
        {
            return false;
        }

        if (requireEmpty && !targetData.IsEmpty)
        {
            return false;
        }

        if (!是一乘二物品(entry))
        {
            return true;
        }

        if (slotType != ItemDatabase.EquipmentSlotType.MainHand &&
            slotType != ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return false;
        }

        int offHandIndex = context.GetOffHandEquipmentSlotIndex != null ? context.GetOffHandEquipmentSlotIndex() : -1;
        if (offHandIndex < 0 || offHandIndex >= equipmentData.Count)
        {
            return false;
        }

        if (offHandIndex == index)
        {
            return false;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData offHandData = equipmentData[offHandIndex];
        return offHandData.IsEmpty || (offHandData.isFootprintExtension && offHandData.primarySlotIndex == index);
    }

    public bool 可以放置物品到索引(
        Context context,
        InventoryShortcutRuntimeBinder.SlotKind kind,
        int primaryIndex,
        ItemDatabase.ItemEntry entry,
        List<InventoryShortcutRuntimeBinder.ItemSlotData> dataList = null)
    {
        dataList ??= context.GetDataList(kind);
        if (dataList == null ||
            primaryIndex < 0 ||
            primaryIndex >= dataList.Count ||
            !context.IsSlotUsable(kind, primaryIndex) ||
            !dataList[primaryIndex].IsEmpty)
        {
            return false;
        }

        if (!是一乘二物品(entry))
        {
            return true;
        }

        int extensionIndex = context.GetOneByTwoExtensionIndex != null
            ? context.GetOneByTwoExtensionIndex(kind, primaryIndex, false)
            : -1;
        return extensionIndex >= 0 &&
            context.IsSlotUsable(kind, extensionIndex) &&
            extensionIndex < dataList.Count &&
            dataList[extensionIndex].IsEmpty;
    }
}
