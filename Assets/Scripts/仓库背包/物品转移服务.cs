using System;
using System.Collections.Generic;
using UnityEngine;

using ItemSlotData = InventoryShortcutRuntimeBinder.ItemSlotData;
using SlotKind = InventoryShortcutRuntimeBinder.SlotKind;
using SlotRef = InventoryShortcutRuntimeBinder.SlotRef;
using SlotSurface = InventoryShortcutRuntimeBinder.SlotSurface;
using StorageRightClickTarget = InventoryShortcutRuntimeBinder.StorageRightClickTarget;

internal sealed class 物品转移服务
{
    internal delegate bool TryGetSlotDataDelegate(SlotRef slot, out ItemSlotData data);

    internal sealed class Context
    {
        public Func<SlotRef, SlotRef> ResolvePrimarySlotRef;
        public Func<SlotRef, ItemSlotData> GetResolvedSlotData;
        public TryGetSlotDataDelegate TryGetSlotData;
        public Func<SlotKind, List<ItemSlotData>> GetDataList;
        public Func<string, ItemDatabase.ItemEntry> ResolveItemEntry;
        public Func<SlotKind, int, bool> IsSlotUsable;
        public Func<ItemSlotData, bool> IsFootprintItem;
        public Func<ItemDatabase.ItemEntry, bool> IsOneByTwoItem;
        public Func<List<ItemSlotData>, List<ItemSlotData>> CloneItemSlotDataList;
        public Func<SlotKind, int, bool, int> GetOneByTwoExtensionIndex;
        public Func<ItemSlotData, string, ItemSlotData> PrepareItemSlotDataForStorage;
        public Func<string> ResolveEquipmentCharacterId;
        public Func<int> GetEquipmentSlotCount;
        public Func<int, ItemDatabase.EquipmentSlotType> GetEquipmentSlotTypeAt;
        public Action<string> RequestEquipmentChanged;
        public Action RequestStorageRefresh;
        public Action<string> PlayItemSound;
        public Action<List<ItemSlotData>> RebuildEquipmentFootprintOccupancy;
        public Func<int> GetOffHandEquipmentSlotIndex;
    }

    public bool TryHandleRightClickMove(
        Context context,
        SlotRef source,
        SlotSurface surface,
        StorageRightClickTarget target,
        ItemSlotData sourceData)
    {
        switch (surface)
        {
            case SlotSurface.Warehouse:
            case SlotSurface.WarehouseBackpack:
                switch (target)
                {
                    case StorageRightClickTarget.Backpack:
                        return TryAutoMoveToFirstEmpty(context, source, SlotKind.Backpack, sourceData);
                    case StorageRightClickTarget.TargetIdEquipment:
                        return TryAutoEquipToTargetEquipment(context, source, sourceData);
                    case StorageRightClickTarget.Chest:
                        return TryAutoMoveToFirstEmpty(context, source, SlotKind.Chest, sourceData);
                    case StorageRightClickTarget.Warehouse:
                    default:
                        return TryAutoMoveToFirstEmpty(context, source, SlotKind.Warehouse, sourceData);
                }

            case SlotSurface.Equipment:
                return target == StorageRightClickTarget.Warehouse
                    ? TryAutoMoveToFirstEmpty(context, source, SlotKind.Warehouse, sourceData)
                    : TryAutoMoveToFirstEmpty(context, source, SlotKind.Backpack, sourceData);

            default:
                return false;
        }
    }

    public bool TryTransferItem(Context context, SlotRef source, SlotRef target, ItemSlotData sourceData)
    {
        source = context.ResolvePrimarySlotRef(source);
        sourceData = context.GetResolvedSlotData(source);
        if (sourceData.IsEmpty)
        {
            return false;
        }

        if (source.kind == target.kind && source.index == target.index)
        {
            return false;
        }

        if (!context.TryGetSlotData(target, out ItemSlotData targetRawData))
        {
            return false;
        }

        SlotRef placementTarget = ShouldUseRawTargetSlotForDrop(context, sourceData, targetRawData)
            ? target
            : context.ResolvePrimarySlotRef(target);
        List<SlotRef> displacedTargets = CollectDisplacedTargetsForPlacement(context, source, placementTarget, sourceData);
        List<ItemSlotData> displacedItems = GetResolvedSlotDataList(context, displacedTargets);
        List<SlotRef> displacedPlacements = new List<SlotRef>();

        if (placementTarget.kind == source.kind && placementTarget.index == source.index)
        {
            return false;
        }

        if (displacedTargets.Count == 1 && TryMergeStorageStack(context, source, placementTarget, sourceData, displacedItems[0]))
        {
            return true;
        }

        if (!CanSwapPlacements(context, source, sourceData, placementTarget, displacedTargets, displacedItems, displacedPlacements))
        {
            return false;
        }

        ClearPlacement(context, source, sourceData);
        for (int i = 0; i < displacedTargets.Count; i++)
        {
            ClearPlacement(context, displacedTargets[i], displacedItems[i]);
        }

        PlaceDataAt(context, placementTarget, sourceData);
        for (int i = 0; i < displacedItems.Count; i++)
        {
            PlaceDataAt(context, displacedPlacements[i], displacedItems[i]);
        }

        if (source.kind == SlotKind.Equipment || placementTarget.kind == SlotKind.Equipment)
        {
            context.RequestEquipmentChanged?.Invoke(context.ResolveEquipmentCharacterId?.Invoke());
        }
        else
        {
            context.RequestStorageRefresh?.Invoke();
        }

        context.PlayItemSound?.Invoke(sourceData.itemId);
        return true;
    }

    private bool TryAutoMoveToFirstEmpty(Context context, SlotRef source, SlotKind targetKind, ItemSlotData sourceData)
    {
        source = context.ResolvePrimarySlotRef(source);
        sourceData = context.GetResolvedSlotData(source);
        int targetIndex = FindFirstAvailableSlotIndex(context, targetKind, sourceData);
        if (targetIndex < 0)
        {
            return false;
        }

        return TryTransferItem(context, source, new SlotRef { kind = targetKind, index = targetIndex }, sourceData);
    }

    private bool TryAutoEquipToTargetEquipment(Context context, SlotRef source, ItemSlotData sourceData)
    {
        string targetCharacterId = context.ResolveEquipmentCharacterId?.Invoke();
        if (string.IsNullOrWhiteSpace(targetCharacterId))
        {
            return false;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(sourceData.itemId);
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        List<ItemSlotData> equipmentData = context.GetDataList(SlotKind.Equipment);
        if (equipmentData == null)
        {
            return false;
        }

        int equipmentSlotCount = context.GetEquipmentSlotCount != null ? context.GetEquipmentSlotCount() : 0;
        for (int pass = 0; pass < 2; pass++)
        {
            bool requireEmpty = pass == 0;
            for (int i = 0; i < equipmentSlotCount; i++)
            {
                ItemDatabase.EquipmentSlotType slotType = context.GetEquipmentSlotTypeAt(i);
                if (!IsEquipmentSlotCompatible(entry.equipmentSlot, slotType))
                {
                    continue;
                }

                ItemSlotData slotData = i < equipmentData.Count ? equipmentData[i] : default;
                if (slotData.isFootprintExtension)
                {
                    continue;
                }

                if (requireEmpty != slotData.IsEmpty)
                {
                    continue;
                }

                if (TryTransferItem(context, source, new SlotRef { kind = SlotKind.Equipment, index = i }, sourceData))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int FindFirstAvailableSlotIndex(Context context, SlotKind kind, ItemSlotData data)
    {
        List<ItemSlotData> dataList = context.GetDataList(kind);
        if (dataList == null)
        {
            return -1;
        }

        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (CanPlaceDataAt(context, kind, i, data, dataList))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryMergeStorageStack(Context context, SlotRef source, SlotRef target, ItemSlotData sourceData, ItemSlotData targetData)
    {
        if (source.kind == SlotKind.Equipment || target.kind == SlotKind.Equipment)
        {
            return false;
        }

        if (sourceData.IsEmpty || targetData.IsEmpty || sourceData.itemId != targetData.itemId)
        {
            return false;
        }

        if (context.IsFootprintItem(sourceData) || context.IsFootprintItem(targetData))
        {
            return false;
        }

        int cap = Mathf.Max(1, targetData.maxStack > 0 ? targetData.maxStack : sourceData.maxStack);
        int canMove = Mathf.Min(sourceData.count, Mathf.Max(0, cap - targetData.count));
        if (canMove <= 0)
        {
            return false;
        }

        targetData.count += canMove;
        sourceData.count -= canMove;
        SetSlotData(context, target, targetData);
        SetSlotData(context, source, sourceData.count > 0 ? sourceData : default);
        context.RequestStorageRefresh?.Invoke();
        context.PlayItemSound?.Invoke(targetData.itemId);
        return true;
    }

    private bool CanSwapPlacements(
        Context context,
        SlotRef source,
        ItemSlotData sourceData,
        SlotRef placementTarget,
        List<SlotRef> displacedTargets,
        List<ItemSlotData> displacedItems,
        List<SlotRef> displacedPlacements)
    {
        if (ShouldSplitDisplacedItemsByTargetCell(context, source, sourceData, placementTarget))
        {
            return CanSwapPlacementsWithStorageOverflow(
                context,
                source,
                sourceData,
                placementTarget,
                displacedTargets,
                displacedItems,
                displacedPlacements);
        }

        List<ItemSlotData> sourceActual = context.GetDataList(source.kind);
        List<ItemSlotData> sourceSim = context.CloneItemSlotDataList(sourceActual);
        List<ItemSlotData> targetSim = source.kind == placementTarget.kind
            ? sourceSim
            : context.CloneItemSlotDataList(context.GetDataList(placementTarget.kind));

        ClearPlacement(context, source.kind, source.index, sourceData, sourceSim);
        for (int i = 0; i < displacedTargets.Count; i++)
        {
            ClearPlacement(context, displacedTargets[i].kind, displacedTargets[i].index, displacedItems[i], targetSim);
        }

        if (!CanPlaceDataAt(context, placementTarget.kind, placementTarget.index, sourceData, targetSim))
        {
            return false;
        }

        PlaceDataAt(context, placementTarget.kind, placementTarget.index, sourceData, targetSim);

        displacedPlacements.Clear();
        if (displacedItems == null || displacedItems.Count == 0)
        {
            return true;
        }

        List<int> sourceCells = GetFootprintCellIndices(context, source.kind, source.index, sourceData);
        if (sourceCells.Count == 0)
        {
            return false;
        }

        List<SlotRef> workingPlacements = new List<SlotRef>(displacedItems.Count);
        if (TryResolveDisplacedPlacementsRecursive(context, source.kind, displacedItems, 0, sourceCells, sourceSim, workingPlacements))
        {
            displacedPlacements.AddRange(workingPlacements);
            return true;
        }

        return false;
    }

    private bool CanSwapPlacementsWithStorageOverflow(
        Context context,
        SlotRef source,
        ItemSlotData sourceData,
        SlotRef placementTarget,
        List<SlotRef> displacedTargets,
        List<ItemSlotData> displacedItems,
        List<SlotRef> displacedPlacements)
    {
        List<ItemSlotData> sourceActual = context.GetDataList(source.kind);
        List<ItemSlotData> sourceSim = context.CloneItemSlotDataList(sourceActual);
        List<ItemSlotData> targetActual = context.GetDataList(placementTarget.kind);
        List<ItemSlotData> targetSim = context.CloneItemSlotDataList(targetActual);

        ClearPlacement(context, source.kind, source.index, sourceData, sourceSim);
        for (int i = 0; i < displacedTargets.Count; i++)
        {
            ClearPlacement(context, displacedTargets[i].kind, displacedTargets[i].index, displacedItems[i], targetSim);
        }

        if (!CanPlaceDataAt(context, placementTarget.kind, placementTarget.index, sourceData, targetSim))
        {
            return false;
        }

        PlaceDataAt(context, placementTarget.kind, placementTarget.index, sourceData, targetSim);

        displacedPlacements.Clear();
        for (int i = 0; i < displacedItems.Count; i++)
        {
            displacedPlacements.Add(default);
        }

        bool hasPrimaryDisplacedTarget = TryResolvePrimaryDisplacedTarget(context, source, placementTarget, out SlotRef primaryDisplacedTarget);
        List<int> primaryIndices = new List<int>();
        List<ItemSlotData> primaryItems = new List<ItemSlotData>();
        List<int> overflowIndices = new List<int>();
        List<ItemSlotData> overflowItems = new List<ItemSlotData>();

        for (int i = 0; i < displacedTargets.Count; i++)
        {
            if (hasPrimaryDisplacedTarget &&
                primaryDisplacedTarget.kind == displacedTargets[i].kind &&
                primaryDisplacedTarget.index == displacedTargets[i].index)
            {
                primaryIndices.Add(i);
                primaryItems.Add(displacedItems[i]);
                continue;
            }

            overflowIndices.Add(i);
            overflowItems.Add(displacedItems[i]);
        }

        if (primaryItems.Count > 0)
        {
            List<int> sourceCells = GetFootprintCellIndices(context, source.kind, source.index, sourceData);
            if (sourceCells.Count == 0)
            {
                return false;
            }

            List<SlotRef> primaryPlacements = new List<SlotRef>(primaryItems.Count);
            if (!TryResolveDisplacedPlacementsRecursive(
                context,
                source.kind,
                primaryItems,
                0,
                sourceCells,
                sourceSim,
                primaryPlacements))
            {
                return false;
            }

            for (int i = 0; i < primaryIndices.Count; i++)
            {
                displacedPlacements[primaryIndices[i]] = primaryPlacements[i];
            }
        }

        if (overflowItems.Count > 0)
        {
            List<int> storageCandidates = BuildPlacementCandidateCells(context, placementTarget.kind, targetSim);
            if (storageCandidates.Count == 0)
            {
                return false;
            }

            List<SlotRef> overflowPlacements = new List<SlotRef>(overflowItems.Count);
            if (!TryResolveDisplacedPlacementsRecursive(
                context,
                placementTarget.kind,
                overflowItems,
                0,
                storageCandidates,
                targetSim,
                overflowPlacements))
            {
                return false;
            }

            for (int i = 0; i < overflowIndices.Count; i++)
            {
                displacedPlacements[overflowIndices[i]] = overflowPlacements[i];
            }
        }

        return true;
    }

    private bool TryResolveDisplacedPlacementsRecursive(
        Context context,
        SlotKind kind,
        List<ItemSlotData> displacedItems,
        int itemIndex,
        List<int> candidateCells,
        List<ItemSlotData> workingData,
        List<SlotRef> resolvedPlacements)
    {
        if (itemIndex >= displacedItems.Count)
        {
            return true;
        }

        ItemSlotData item = displacedItems[itemIndex];
        for (int i = 0; i < candidateCells.Count; i++)
        {
            int candidateIndex = candidateCells[i];
            if (!CanPlaceDataAt(context, kind, candidateIndex, item, workingData))
            {
                continue;
            }

            List<ItemSlotData> nextData = context.CloneItemSlotDataList(workingData);
            PlaceDataAt(context, kind, candidateIndex, item, nextData);
            resolvedPlacements.Add(new SlotRef { kind = kind, index = candidateIndex });
            if (TryResolveDisplacedPlacementsRecursive(context, kind, displacedItems, itemIndex + 1, candidateCells, nextData, resolvedPlacements))
            {
                return true;
            }

            resolvedPlacements.RemoveAt(resolvedPlacements.Count - 1);
        }

        return false;
    }

    private bool ShouldUseRawTargetSlotForDrop(Context context, ItemSlotData sourceData, ItemSlotData targetRawData)
    {
        return !context.IsFootprintItem(sourceData) && targetRawData.isFootprintExtension;
    }

    private bool ShouldSplitDisplacedItemsByTargetCell(Context context, SlotRef source, ItemSlotData sourceData, SlotRef placementTarget)
    {
        return source.kind == SlotKind.Equipment &&
            placementTarget.kind != SlotKind.Equipment &&
            context.IsFootprintItem(sourceData);
    }

    private List<ItemSlotData> GetResolvedSlotDataList(Context context, List<SlotRef> slots)
    {
        List<ItemSlotData> result = new List<ItemSlotData>(slots != null ? slots.Count : 0);
        if (slots == null)
        {
            return result;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            result.Add(context.GetResolvedSlotData(slots[i]));
        }

        return result;
    }

    private bool TryResolvePrimaryDisplacedTarget(Context context, SlotRef source, SlotRef placementTarget, out SlotRef resolvedTarget)
    {
        resolvedTarget = default;
        List<ItemSlotData> list = context.GetDataList(placementTarget.kind);
        if (list == null || placementTarget.index < 0 || placementTarget.index >= list.Count)
        {
            return false;
        }

        ItemSlotData data = list[placementTarget.index];
        if (data.IsEmpty)
        {
            return false;
        }

        resolvedTarget = context.ResolvePrimarySlotRef(placementTarget);
        if (resolvedTarget.kind == source.kind && resolvedTarget.index == source.index)
        {
            resolvedTarget = default;
            return false;
        }

        return true;
    }

    private List<int> BuildPlacementCandidateCells(Context context, SlotKind kind, List<ItemSlotData> list)
    {
        List<int> result = new List<int>();
        if (list == null)
        {
            return result;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (!context.IsSlotUsable(kind, i))
            {
                continue;
            }

            result.Add(i);
        }

        return result;
    }

    private List<SlotRef> CollectDisplacedTargetsForPlacement(Context context, SlotRef source, SlotRef placementTarget, ItemSlotData sourceData)
    {
        List<SlotRef> result = new List<SlotRef>();
        List<ItemSlotData> list = context.GetDataList(placementTarget.kind);
        TryAddDisplacedTarget(context, result, source, placementTarget.kind, placementTarget.index, list);

        int extensionIndex = GetExtensionIndexForData(context, placementTarget.kind, placementTarget.index, sourceData);
        if (extensionIndex >= 0)
        {
            TryAddDisplacedTarget(context, result, source, placementTarget.kind, extensionIndex, list);
        }

        return result;
    }

    private void TryAddDisplacedTarget(Context context, List<SlotRef> targets, SlotRef source, SlotKind kind, int index, List<ItemSlotData> list)
    {
        if (targets == null || list == null || index < 0 || index >= list.Count)
        {
            return;
        }

        ItemSlotData data = list[index];
        if (data.IsEmpty)
        {
            return;
        }

        SlotRef resolved = context.ResolvePrimarySlotRef(new SlotRef { kind = kind, index = index });
        if (resolved.kind == source.kind && resolved.index == source.index)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].kind == resolved.kind && targets[i].index == resolved.index)
            {
                return;
            }
        }

        targets.Add(resolved);
    }

    private bool CanPlaceDataAt(Context context, SlotKind kind, int index, ItemSlotData data, List<ItemSlotData> list)
    {
        if (data.IsEmpty)
        {
            return true;
        }

        if (kind == SlotKind.Equipment)
        {
            if (index < 0 || index >= (context.GetEquipmentSlotCount != null ? context.GetEquipmentSlotCount() : 0))
            {
                return false;
            }

            ItemDatabase.EquipmentSlotType slotType = context.GetEquipmentSlotTypeAt(index);
            ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
            if (entry == null || !IsEquipmentSlotCompatible(entry.equipmentSlot, slotType))
            {
                return false;
            }

            return CanUseEquipmentSlotIndex(context, index, slotType, requireEmpty: false, entry, list);
        }

        if (index < 0 || index >= list.Count || !context.IsSlotUsable(kind, index) || !list[index].IsEmpty)
        {
            return false;
        }

        ItemDatabase.ItemEntry storageEntry = context.ResolveItemEntry(data.itemId);
        if (!context.IsOneByTwoItem(storageEntry))
        {
            return true;
        }

        int extensionIndex = GetExtensionIndexForData(context, kind, index, data);
        return extensionIndex >= 0 &&
            extensionIndex < list.Count &&
            context.IsSlotUsable(kind, extensionIndex) &&
            list[extensionIndex].IsEmpty;
    }

    private void PlaceDataAt(Context context, SlotRef target, ItemSlotData data)
    {
        if (data.IsEmpty)
        {
            SetSlotData(context, target, default);
            return;
        }

        if (target.kind == SlotKind.Equipment)
        {
            data.isRotated = false;
            SetFootprintDataAt(context, target.kind, target.index, data);
            return;
        }

        SetFootprintDataAt(context, target.kind, target.index, data);
    }

    private void PlaceDataAt(Context context, SlotKind kind, int index, ItemSlotData data, List<ItemSlotData> list)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
            return;
        }

        if (data.IsEmpty)
        {
            list[index] = default;
            if (kind == SlotKind.Equipment)
            {
                context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
            }

            return;
        }

        ItemSlotData normalizedPrimary = context.PrepareItemSlotDataForStorage(data, $"{kind} {index}");
        list[index] = normalizedPrimary;
        int extensionIndex = GetExtensionIndexForData(context, kind, index, normalizedPrimary);
        if (extensionIndex >= 0 && extensionIndex < list.Count)
        {
            list[extensionIndex] = context.PrepareItemSlotDataForStorage(new ItemSlotData
            {
                isFootprintExtension = true,
                primarySlotIndex = index
            }, $"{kind} 扩展格 {extensionIndex}");
        }

        if (kind == SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }
    }

    private void ClearPlacement(Context context, SlotRef slot, ItemSlotData data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        ClearPlacement(context, slot.kind, slot.index, data, context.GetDataList(slot.kind));
        if (slot.kind == SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(context.GetDataList(slot.kind));
        }
    }

    private void ClearPlacement(Context context, SlotKind kind, int primaryIndex, ItemSlotData data, List<ItemSlotData> list)
    {
        if (data.IsEmpty || list == null || primaryIndex < 0 || primaryIndex >= list.Count)
        {
            return;
        }

        list[primaryIndex] = default;
        int extensionIndex = GetExtensionIndexForData(context, kind, primaryIndex, data);
        if (extensionIndex >= 0 && extensionIndex < list.Count)
        {
            list[extensionIndex] = default;
        }

        if (kind == SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }
    }

    private void SetSlotData(Context context, SlotRef slot, ItemSlotData data)
    {
        List<ItemSlotData> list = context.GetDataList(slot.kind);
        if (list == null || slot.index < 0 || slot.index >= list.Count)
        {
            return;
        }

        list[slot.index] = context.PrepareItemSlotDataForStorage(data, $"{slot.kind} {slot.index}");
        if (slot.kind == SlotKind.Equipment)
        {
            context.RebuildEquipmentFootprintOccupancy?.Invoke(list);
        }
    }

    private void SetFootprintDataAt(Context context, SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        List<ItemSlotData> list = context.GetDataList(kind);
        if (list == null || primaryIndex < 0 || primaryIndex >= list.Count)
        {
            return;
        }

        ItemSlotData normalizedPrimary = context.PrepareItemSlotDataForStorage(data, $"{kind} {primaryIndex}");
        list[primaryIndex] = normalizedPrimary;

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(normalizedPrimary.itemId);
        if (!context.IsOneByTwoItem(entry))
        {
            return;
        }

        int extensionIndex = context.GetOneByTwoExtensionIndex(kind, primaryIndex, normalizedPrimary.isRotated);
        if (extensionIndex < 0 || extensionIndex >= list.Count)
        {
            return;
        }

        list[extensionIndex] = context.PrepareItemSlotDataForStorage(new ItemSlotData
        {
            isFootprintExtension = true,
            primarySlotIndex = primaryIndex
        }, $"{kind} 扩展格 {extensionIndex}");
    }

    private int GetExtensionIndexForData(Context context, SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        if (data.IsEmpty)
        {
            return -1;
        }

        if (kind == SlotKind.Equipment)
        {
            return context.IsFootprintItem(data) ? context.GetOffHandEquipmentSlotIndex() : -1;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
        return context.IsOneByTwoItem(entry) ? context.GetOneByTwoExtensionIndex(kind, primaryIndex, data.isRotated) : -1;
    }

    private List<int> GetFootprintCellIndices(Context context, SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        List<int> result = new List<int>();
        if (data.IsEmpty)
        {
            return result;
        }

        result.Add(primaryIndex);
        int extensionIndex = GetExtensionIndexForData(context, kind, primaryIndex, data);
        if (extensionIndex >= 0)
        {
            result.Add(extensionIndex);
        }

        return result;
    }

    private bool CanUseEquipmentSlotIndex(
        Context context,
        int index,
        ItemDatabase.EquipmentSlotType slotType,
        bool requireEmpty,
        ItemDatabase.ItemEntry entry,
        List<ItemSlotData> equipmentData)
    {
        if (equipmentData == null || index < 0 || index >= equipmentData.Count)
        {
            return false;
        }

        ItemSlotData targetData = equipmentData[index];
        if (targetData.isFootprintExtension)
        {
            return false;
        }

        if (requireEmpty && !targetData.IsEmpty)
        {
            return false;
        }

        if (!context.IsOneByTwoItem(entry))
        {
            return true;
        }

        if (slotType != ItemDatabase.EquipmentSlotType.MainHand &&
            slotType != ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return false;
        }

        int offHandIndex = context.GetOffHandEquipmentSlotIndex();
        if (offHandIndex < 0 || offHandIndex >= equipmentData.Count)
        {
            return false;
        }

        if (offHandIndex == index)
        {
            return false;
        }

        ItemSlotData offHandData = equipmentData[offHandIndex];
        return offHandData.IsEmpty || (offHandData.isFootprintExtension && offHandData.primarySlotIndex == index);
    }

    private static bool IsEquipmentSlotCompatible(
        ItemDatabase.EquipmentSlotType itemSlot,
        ItemDatabase.EquipmentSlotType targetSlot)
    {
        if (itemSlot == ItemDatabase.EquipmentSlotType.None || targetSlot == ItemDatabase.EquipmentSlotType.None)
        {
            return false;
        }

        if (itemSlot == targetSlot)
        {
            return true;
        }

        if (itemSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return targetSlot == ItemDatabase.EquipmentSlotType.MainHand ||
                targetSlot == ItemDatabase.EquipmentSlotType.OffHand ||
                targetSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand;
        }

        if (targetSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return itemSlot == ItemDatabase.EquipmentSlotType.MainHand ||
                itemSlot == ItemDatabase.EquipmentSlotType.OffHand ||
                itemSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand;
        }

        return false;
    }
}
