using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 物品占格服务
{
    internal sealed class Context
    {
        public Func<InventoryShortcutRuntimeBinder.SlotKind, List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetDataList;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, List<InventoryShortcutRuntimeBinder.SlotWidget>> GetWidgetList;
        public Func<string, ItemDatabase.ItemEntry> ResolveItemEntry;
        public Func<ItemDatabase.ItemEntry, bool> IsOneByTwoItem;
        public Func<InventoryShortcutRuntimeBinder.ItemSlotData, string, InventoryShortcutRuntimeBinder.ItemSlotData> PrepareItemSlotDataForStorage;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, bool> IsSlotUsable;
        public Func<int> GetOffHandEquipmentSlotIndex;
        public Action<List<InventoryShortcutRuntimeBinder.ItemSlotData>> RebuildEquipmentFootprintOccupancy;
        public Action<InventoryShortcutRuntimeBinder.SlotRef> RefreshByRef;
        public Func<bool> HasCachedBackpackLayout;
        public Func<GridLayoutGroup.Corner> GetCachedBackpackStartCorner;
        public Func<GridLayoutGroup.Constraint> GetCachedBackpackConstraint;
        public Func<int> GetCachedBackpackConstraintCount;
    }

    public bool CanPlaceDataAt(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index, InventoryShortcutRuntimeBinder.ItemSlotData data, List<InventoryShortcutRuntimeBinder.ItemSlotData> list)
    {
        list ??= context.GetDataList(kind);
        if (list == null || index < 0 || index >= list.Count || !context.IsSlotUsable(kind, index))
        {
            return false;
        }

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            if (data.IsEmpty)
            {
                return true;
            }

            InventoryShortcutRuntimeBinder.SlotWidget widget = GetWidget(context, new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index });
            ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
            if (widget == null || entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
            {
                return false;
            }

            if (!InventoryShortcutRuntimeBinder.IsEquipmentSlotCompatible(entry.equipmentSlot, widget.equipmentSlotType))
            {
                return false;
            }
        }

        InventoryShortcutRuntimeBinder.ItemSlotData target = list[index];
        if (!target.IsEmpty && !(target.isFootprintExtension && ResolvePrimarySlotRef(context, new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index }).index != index))
        {
            return false;
        }

        bool requiresExtension = IsFootprintItem(context, data);
        int extensionIndex = GetExtensionIndexForData(context, kind, index, data);
        if (requiresExtension && extensionIndex < 0)
        {
            return false;
        }

        if (extensionIndex < 0)
        {
            return true;
        }

        if (extensionIndex >= list.Count || !context.IsSlotUsable(kind, extensionIndex))
        {
            return false;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData extensionData = list[extensionIndex];
        return extensionData.IsEmpty || (extensionData.isFootprintExtension && extensionData.primarySlotIndex == index);
    }

    public void PlaceDataAt(Context context, InventoryShortcutRuntimeBinder.SlotRef target, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        PlaceDataAt(context, target.kind, target.index, data, context.GetDataList(target.kind));
    }

    public void PlaceDataAt(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index, InventoryShortcutRuntimeBinder.ItemSlotData data, List<InventoryShortcutRuntimeBinder.ItemSlotData> list)
    {
        list ??= context.GetDataList(kind);
        if (list == null || index < 0 || index >= list.Count)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData normalizedPrimary = context.PrepareItemSlotDataForStorage(data, $"{kind} {index}");
        list[index] = normalizedPrimary;

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }

        int extensionIndex = GetExtensionIndexForData(context, kind, index, normalizedPrimary);
        if (extensionIndex < 0 || extensionIndex >= list.Count)
        {
            return;
        }

        list[extensionIndex] = context.PrepareItemSlotDataForStorage(new InventoryShortcutRuntimeBinder.ItemSlotData
        {
            isFootprintExtension = true,
            primarySlotIndex = index
        }, $"{kind} 扩展格 {extensionIndex}");

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }
    }

    public void ClearPlacement(Context context, InventoryShortcutRuntimeBinder.SlotRef slot, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        ClearPlacement(context, slot.kind, slot.index, data, context.GetDataList(slot.kind));
        if (slot.kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(context.GetDataList(slot.kind));
        }
    }

    public void ClearPlacement(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int primaryIndex, InventoryShortcutRuntimeBinder.ItemSlotData data, List<InventoryShortcutRuntimeBinder.ItemSlotData> list)
    {
        list ??= context.GetDataList(kind);
        if (list == null || primaryIndex < 0 || primaryIndex >= list.Count)
        {
            return;
        }

        list[primaryIndex] = default;
        int extensionIndex = GetExtensionIndexForData(context, kind, primaryIndex, data);
        if (extensionIndex >= 0 && extensionIndex < list.Count)
        {
            list[extensionIndex] = default;
        }

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }
    }

    public int GetExtensionIndexForData(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int primaryIndex, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            return IsFootprintItem(context, data) ? context.GetOffHandEquipmentSlotIndex() : -1;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
        return entry != null && context.IsOneByTwoItem(entry)
            ? GetOneByTwoExtensionIndex(context, kind, primaryIndex, data.isRotated)
            : -1;
    }

    public List<int> GetFootprintCellIndices(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int primaryIndex, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        List<int> result = new List<int> { primaryIndex };
        int extensionIndex = GetExtensionIndexForData(context, kind, primaryIndex, data);
        if (extensionIndex >= 0)
        {
            result.Add(extensionIndex);
        }

        return result;
    }

    public InventoryShortcutRuntimeBinder.SlotRef ResolvePrimarySlotRef(Context context, InventoryShortcutRuntimeBinder.SlotRef slot)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> list = context.GetDataList(slot.kind);
        if (list == null || slot.index < 0 || slot.index >= list.Count)
        {
            return slot;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData data = list[slot.index];
        if (!data.isFootprintExtension)
        {
            return slot;
        }

        int primaryIndex = data.primarySlotIndex;
        return primaryIndex >= 0 && primaryIndex < list.Count
            ? new InventoryShortcutRuntimeBinder.SlotRef { kind = slot.kind, index = primaryIndex }
            : slot;
    }

    public int ResolvePrimarySlotIndex(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index)
    {
        return ResolvePrimarySlotRef(context, new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index }).index;
    }

    public void RebuildEquipmentFootprintOccupancy(Context context, List<InventoryShortcutRuntimeBinder.ItemSlotData> equipmentData)
    {
        if (equipmentData == null)
        {
            return;
        }

        for (int i = 0; i < equipmentData.Count; i++)
        {
            if (equipmentData[i].isFootprintExtension)
            {
                equipmentData[i] = default;
            }
        }

        int offHandIndex = context.GetOffHandEquipmentSlotIndex();
        if (offHandIndex < 0 || offHandIndex >= equipmentData.Count)
        {
            return;
        }

        for (int i = 0; i < equipmentData.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData data = equipmentData[i];
            if (data.isFootprintExtension || string.IsNullOrWhiteSpace(data.itemId))
            {
                continue;
            }

            if (!ShouldOccupyOffHandSlot(context, i, data))
            {
                continue;
            }

            InventoryShortcutRuntimeBinder.ItemSlotData offHandData = equipmentData[offHandIndex];
            if (!offHandData.IsEmpty && !(offHandData.isFootprintExtension && offHandData.primarySlotIndex == i))
            {
                return;
            }

            equipmentData[offHandIndex] = context.PrepareItemSlotDataForStorage(new InventoryShortcutRuntimeBinder.ItemSlotData
            {
                isFootprintExtension = true,
                primarySlotIndex = i
            }, $"装备栏扩展格 {offHandIndex}");
            return;
        }
    }

    public bool ShouldOccupyOffHandSlot(Context context, int primaryIndex, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (!IsFootprintItem(context, data))
        {
            return false;
        }

        List<InventoryShortcutRuntimeBinder.SlotWidget> equipmentSlots = context.GetWidgetList(InventoryShortcutRuntimeBinder.SlotKind.Equipment);
        if (primaryIndex < 0 || primaryIndex >= equipmentSlots.Count)
        {
            return false;
        }

        ItemDatabase.EquipmentSlotType slotType = equipmentSlots[primaryIndex] != null
            ? equipmentSlots[primaryIndex].equipmentSlotType
            : ItemDatabase.EquipmentSlotType.None;
        return slotType == ItemDatabase.EquipmentSlotType.MainHand ||
            slotType == ItemDatabase.EquipmentSlotType.MainOrOffHand;
    }

    public int GetOffHandEquipmentSlotIndex(Context context)
    {
        List<InventoryShortcutRuntimeBinder.SlotWidget> equipmentSlots = context.GetWidgetList(InventoryShortcutRuntimeBinder.SlotKind.Equipment);
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            InventoryShortcutRuntimeBinder.SlotWidget widget = equipmentSlots[i];
            if (widget != null && widget.equipmentSlotType == ItemDatabase.EquipmentSlotType.OffHand)
            {
                return i;
            }
        }

        return -1;
    }

    public void SetFootprintDataAt(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int primaryIndex, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> list = context.GetDataList(kind);
        if (list == null || primaryIndex < 0 || primaryIndex >= list.Count)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData normalizedPrimary = context.PrepareItemSlotDataForStorage(data, $"{kind} {primaryIndex}");
        list[primaryIndex] = normalizedPrimary;

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(normalizedPrimary.itemId);
        if (entry == null || !context.IsOneByTwoItem(entry))
        {
            return;
        }

        int extensionIndex = GetOneByTwoExtensionIndex(context, kind, primaryIndex, normalizedPrimary.isRotated);
        if (extensionIndex < 0 || extensionIndex >= list.Count)
        {
            return;
        }

        list[extensionIndex] = context.PrepareItemSlotDataForStorage(new InventoryShortcutRuntimeBinder.ItemSlotData
        {
            isFootprintExtension = true,
            primarySlotIndex = primaryIndex
        }, $"{kind} 扩展格 {extensionIndex}");
    }

    public void RefreshFootprintSlots(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int primaryIndex, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        context.RefreshByRef?.Invoke(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = primaryIndex });
        int extensionIndex = GetExtensionIndexForData(context, kind, primaryIndex, data);
        if (extensionIndex >= 0)
        {
            context.RefreshByRef?.Invoke(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = extensionIndex });
        }
    }

    public int GetOneByTwoExtensionIndex(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int primaryIndex, bool isRotated)
    {
        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            return context.GetOffHandEquipmentSlotIndex();
        }

        int columnCount = GetGridColumnCount(context, kind);
        if (columnCount <= 0)
        {
            return -1;
        }

        if (isRotated)
        {
            int rowStart = primaryIndex - (primaryIndex % columnCount);
            int horizontalExtensionIndex = primaryIndex - 1;
            return horizontalExtensionIndex >= rowStart ? horizontalExtensionIndex : -1;
        }

        int verticalStep = UsesLowerStartCorner(context, kind) ? -columnCount : columnCount;
        int extensionIndex = primaryIndex + verticalStep;
        return extensionIndex >= 0 ? extensionIndex : -1;
    }

    public int GetGridColumnCount(Context context, InventoryShortcutRuntimeBinder.SlotKind kind)
    {
        GridLayoutGroup layout = GetGridLayout(context, kind);
        if (layout == null)
        {
            if (kind == InventoryShortcutRuntimeBinder.SlotKind.Backpack &&
                context.HasCachedBackpackLayout() &&
                context.GetCachedBackpackConstraint() == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                return Mathf.Max(1, context.GetCachedBackpackConstraintCount());
            }

            return 0;
        }

        return layout.constraint == GridLayoutGroup.Constraint.FixedColumnCount
            ? Mathf.Max(1, layout.constraintCount)
            : 0;
    }

    public bool UsesLowerStartCorner(Context context, InventoryShortcutRuntimeBinder.SlotKind kind)
    {
        GridLayoutGroup layout = GetGridLayout(context, kind);
        if (layout == null)
        {
            if (kind == InventoryShortcutRuntimeBinder.SlotKind.Backpack && context.HasCachedBackpackLayout())
            {
                GridLayoutGroup.Corner corner = context.GetCachedBackpackStartCorner();
                return corner == GridLayoutGroup.Corner.LowerLeft || corner == GridLayoutGroup.Corner.LowerRight;
            }

            return true;
        }

        return layout.startCorner == GridLayoutGroup.Corner.LowerLeft ||
            layout.startCorner == GridLayoutGroup.Corner.LowerRight;
    }

    public GridLayoutGroup GetGridLayout(Context context, InventoryShortcutRuntimeBinder.SlotKind kind)
    {
        List<InventoryShortcutRuntimeBinder.SlotWidget> widgets = context.GetWidgetList(kind);
        if (widgets.Count > 0 && widgets[0] != null && widgets[0].root != null && widgets[0].root.parent != null)
        {
            return widgets[0].root.parent.GetComponent<GridLayoutGroup>();
        }

        return null;
    }

    public bool IsFootprintItem(Context context, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (data.isFootprintExtension || string.IsNullOrWhiteSpace(data.itemId))
        {
            return false;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
        return entry != null && context.IsOneByTwoItem(entry);
    }

    private static InventoryShortcutRuntimeBinder.SlotWidget GetWidget(Context context, InventoryShortcutRuntimeBinder.SlotRef slot)
    {
        List<InventoryShortcutRuntimeBinder.SlotWidget> list = context.GetWidgetList(slot.kind);
        if (slot.index < 0 || slot.index >= list.Count)
        {
            return null;
        }

        return list[slot.index];
    }
}
