using System;
using System.Collections.Generic;

internal sealed class 槽位访问服务
{
    internal sealed class Context
    {
        public Func<InventoryShortcutRuntimeBinder.SlotKind, List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetDataList;
        public Func<InventoryShortcutRuntimeBinder.SlotRef, InventoryShortcutRuntimeBinder.SlotWidget> GetWidget;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, bool> IsSlotUsable;
        public Func<InventoryShortcutRuntimeBinder.ItemSlotData, string, InventoryShortcutRuntimeBinder.ItemSlotData> PrepareItemSlotDataForStorage;
        public Func<string, ItemDatabase.ItemEntry> ResolveItemEntry;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, int> ResolvePrimarySlotIndex;
        public Func<int, ItemDatabase.EquipmentSlotType, bool, ItemDatabase.ItemEntry, List<InventoryShortcutRuntimeBinder.ItemSlotData>, bool> CanUseEquipmentSlotIndex;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, ItemDatabase.ItemEntry, List<InventoryShortcutRuntimeBinder.ItemSlotData>, bool> CanPlaceItemAtIndex;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, InventoryShortcutRuntimeBinder.ItemSlotData, List<InventoryShortcutRuntimeBinder.ItemSlotData>, bool> CanPlaceDataAt;
        public Action<List<InventoryShortcutRuntimeBinder.ItemSlotData>> RebuildEquipmentFootprintOccupancy;
        public Func<List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetCurrentEquipmentData;
    }

    public bool TryGetSlotData(Context context, InventoryShortcutRuntimeBinder.SlotRef slot, out InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        data = default;
        List<InventoryShortcutRuntimeBinder.ItemSlotData> list = context.GetDataList(slot.kind);
        if (list == null || slot.index < 0 || slot.index >= list.Count)
        {
            return false;
        }

        data = list[slot.index];
        return true;
    }

    public void SetSlotData(Context context, InventoryShortcutRuntimeBinder.SlotRef slot, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> list = context.GetDataList(slot.kind);
        if (list == null || slot.index < 0 || slot.index >= list.Count)
        {
            return;
        }

        list[slot.index] = context.PrepareItemSlotDataForStorage(data, $"{slot.kind} {slot.index}");
        if (slot.kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }
    }

    public int FindFirstEmptySlotIndex(Context context, InventoryShortcutRuntimeBinder.SlotKind kind)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> dataList = context.GetDataList(kind);
        if (dataList == null)
        {
            return -1;
        }

        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (context.IsSlotUsable(kind, i) && dataList[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    public int FindFirstAvailableSlotIndex(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, ItemDatabase.ItemEntry entry)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> dataList = context.GetDataList(kind);
        if (dataList == null)
        {
            return -1;
        }

        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (context.CanPlaceItemAtIndex != null && context.CanPlaceItemAtIndex(kind, i, entry, dataList))
            {
                return i;
            }
        }

        return -1;
    }

    public int FindFirstAvailableSlotIndex(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> dataList = context.GetDataList(kind);
        if (dataList == null)
        {
            return -1;
        }

        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (context.CanPlaceDataAt != null && context.CanPlaceDataAt(kind, i, data, dataList))
            {
                return i;
            }
        }

        return -1;
    }

    public int FindRightClickEquipmentTargetIndex(Context context, ItemDatabase.ItemEntry entry, List<InventoryShortcutRuntimeBinder.SlotWidget> equipmentSlots)
    {
        if (entry == null || equipmentSlots == null || equipmentSlots.Count == 0)
        {
            return -1;
        }

        ItemDatabase.EquipmentSlotType desiredSlotType = entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand
            ? ItemDatabase.EquipmentSlotType.MainHand
            : entry.equipmentSlot;

        int emptySlotIndex = FindEquipmentSlotIndex(context, desiredSlotType, requireEmpty: true, entry, equipmentSlots);
        return emptySlotIndex >= 0
            ? emptySlotIndex
            : FindEquipmentSlotIndex(context, desiredSlotType, requireEmpty: false, entry, equipmentSlots);
    }

    public int FindEquipmentSlotIndex(
        Context context,
        ItemDatabase.EquipmentSlotType slotType,
        bool requireEmpty,
        ItemDatabase.ItemEntry entry,
        List<InventoryShortcutRuntimeBinder.SlotWidget> equipmentSlots)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipmentData = context.GetCurrentEquipmentData != null ? context.GetCurrentEquipmentData() : null;
        if (equipmentData == null || equipmentSlots == null)
        {
            return -1;
        }

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            InventoryShortcutRuntimeBinder.SlotWidget widget = equipmentSlots[i];
            if (widget == null || widget.equipmentSlotType != slotType)
            {
                continue;
            }

            if (context.CanUseEquipmentSlotIndex == null ||
                !context.CanUseEquipmentSlotIndex(i, slotType, requireEmpty, entry, equipmentData))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    public bool CanPlaceIntoTarget(Context context, InventoryShortcutRuntimeBinder.ItemSlotData data, InventoryShortcutRuntimeBinder.SlotRef target)
    {
        if (target.kind != InventoryShortcutRuntimeBinder.SlotKind.Equipment || data.IsEmpty)
        {
            return true;
        }

        InventoryShortcutRuntimeBinder.SlotWidget widget = context.GetWidget != null ? context.GetWidget(target) : null;
        if (widget == null)
        {
            return false;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry != null ? context.ResolveItemEntry(data.itemId) : null;
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        if (!InventoryShortcutRuntimeBinder.IsEquipmentSlotCompatible(entry.equipmentSlot, widget.equipmentSlotType))
        {
            return false;
        }

        int targetIndex = context.ResolvePrimarySlotIndex != null
            ? context.ResolvePrimarySlotIndex(InventoryShortcutRuntimeBinder.SlotKind.Equipment, target.index)
            : target.index;
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipmentData = context.GetCurrentEquipmentData != null ? context.GetCurrentEquipmentData() : null;
        return context.CanUseEquipmentSlotIndex != null &&
            context.CanUseEquipmentSlotIndex(targetIndex, widget.equipmentSlotType, false, entry, equipmentData);
    }
}
