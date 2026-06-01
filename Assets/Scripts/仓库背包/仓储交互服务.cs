using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class 仓储交互服务
{
    internal sealed class State
    {
        public bool isDragging;
        public InventoryShortcutRuntimeBinder.SlotRef draggingSource;
        public InventoryShortcutRuntimeBinder.SlotWidget draggingSourceWidget;
        public Canvas dragCanvas;
        public RectTransform dragIconRoot;
        public Image dragIconImage;
        public InventoryShortcutRuntimeBinder.SlotRef hoveredRotateSlot;
        public bool hasHoveredRotateSlot;
        public bool isDraggingEquipment;
    }

    internal sealed class Context
    {
        public List<InventoryShortcutRuntimeBinder.SlotWidget> WarehouseSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> BackpackSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> ChestSlots;
        public List<List<InventoryShortcutRuntimeBinder.SlotWidget>> ExtraBackpackSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> EquipmentSlots;
        public List<List<InventoryShortcutRuntimeBinder.SlotWidget>> ExtraEquipmentSlots;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, bool> IsSlotUsable;
        public Func<InventoryShortcutRuntimeBinder.SlotRef, InventoryShortcutRuntimeBinder.SlotRef> ResolvePrimarySlotRef;
        public Func<InventoryShortcutRuntimeBinder.SlotRef, InventoryShortcutRuntimeBinder.ItemSlotData> GetResolvedSlotData;
        public TryGetSlotDataDelegate TryGetSlotData;
        public Func<InventoryShortcutRuntimeBinder.SlotRef, InventoryShortcutRuntimeBinder.SlotWidget> GetWidget;
        public Func<string, ItemDatabase.ItemEntry> ResolveItemEntry;
        public Func<ItemDatabase.ItemEntry, bool> ShouldShowWeaponTooltip;
        public Func<InventoryShortcutRuntimeBinder.SlotRef, InventoryShortcutRuntimeBinder.ItemSlotData, bool> TryTransferToSlot;
        public Func<InventoryShortcutRuntimeBinder.SlotRef, InventoryShortcutRuntimeBinder.SlotSurface, InventoryShortcutRuntimeBinder.StorageRightClickTarget, InventoryShortcutRuntimeBinder.ItemSlotData, bool> TryHandleRightClickMove;
        public Action<InventoryShortcutRuntimeBinder.SlotWidget, ItemDatabase.ItemEntry, InventoryShortcutRuntimeBinder.SlotRef> ShowItemTooltip;
        public Action HideItemTooltip;
        public Func<InventoryShortcutRuntimeBinder.SlotWidget, Sprite> ResolveRuntimeIconSprite;
        public Func<InventoryShortcutRuntimeBinder.ItemSlotData, Sprite> ResolveDisplaySprite;
        public Action<InventoryShortcutRuntimeBinder.SlotWidget, bool> SetWidgetDraggingVisible;
        public Action<InventoryShortcutRuntimeBinder.SlotRef> RefreshByRef;
        public Func<ItemDatabase.ItemEntry, bool> IsOneByTwoItem;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetDataList;
        public Func<List<InventoryShortcutRuntimeBinder.ItemSlotData>, List<InventoryShortcutRuntimeBinder.ItemSlotData>> CloneItemSlotDataList;
        public Action<InventoryShortcutRuntimeBinder.SlotKind, int, InventoryShortcutRuntimeBinder.ItemSlotData, List<InventoryShortcutRuntimeBinder.ItemSlotData>> ClearPlacement;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, InventoryShortcutRuntimeBinder.ItemSlotData, List<InventoryShortcutRuntimeBinder.ItemSlotData>, bool> CanPlaceDataAt;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, InventoryShortcutRuntimeBinder.ItemSlotData, int> GetExtensionIndexForData;
        public Func<InventoryShortcutRuntimeBinder.ItemSlotData, string, InventoryShortcutRuntimeBinder.ItemSlotData> PrepareItemSlotDataForStorage;
        public Action<InventoryShortcutRuntimeBinder.SlotKind, int, InventoryShortcutRuntimeBinder.ItemSlotData> RefreshFootprintSlots;
        public Action<string> PlayItemSound;
        public Action<ItemDatabase.ItemEntry> NotifyEquipmentDragStarted;
        public Action NotifyEquipmentDragEnded;
    }

    internal delegate bool TryGetSlotDataDelegate(InventoryShortcutRuntimeBinder.SlotRef slot, out InventoryShortcutRuntimeBinder.ItemSlotData data);

    public void HandleHoveredItemRotation(State state, Context context)
    {
        if (state.isDragging || !Input.GetKeyDown(KeyCode.R) || !state.hasHoveredRotateSlot)
        {
            return;
        }

        TryRotateHoveredOneByTwoItem(state, context);
    }

    public void UpdatePendingTooltip(State state, Context context)
    {
    }

    public void HandleBeginDrag(State state, Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index, PointerEventData eventData)
    {
        if (state.isDragging || !context.IsSlotUsable(kind, index))
        {
            return;
        }

        int rawIndex = index;
        InventoryShortcutRuntimeBinder.SlotRef source = context.ResolvePrimarySlotRef(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index });
        if (!context.TryGetSlotData(source, out InventoryShortcutRuntimeBinder.ItemSlotData data) || data.IsEmpty)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.SlotWidget widget = rawIndex != source.index
            ? context.GetWidget(source)
            : ResolveDraggedWidget(context, source, eventData);
        if (widget == null || widget.root == null)
        {
            return;
        }

        EnsureDragVisual(state, widget.root);
        if (state.dragIconRoot == null)
        {
            return;
        }

        state.dragIconRoot.sizeDelta = widget.root.rect.size;
        RebuildDragVisual(state, context, widget, data);
        state.dragIconRoot.gameObject.SetActive(true);
        state.dragIconRoot.SetAsLastSibling();
        UpdateDragVisualPosition(state, eventData);
        context.HideItemTooltip?.Invoke();

        state.draggingSource = source;
        state.draggingSourceWidget = widget;
        context.SetWidgetDraggingVisible?.Invoke(widget, false);
        state.isDragging = true;

        ItemDatabase.ItemEntry draggedEntry = context.ResolveItemEntry != null ? context.ResolveItemEntry(data.itemId) : null;
        state.isDraggingEquipment = draggedEntry != null && draggedEntry.category == ItemDatabase.ItemCategory.Equipment;
        if (state.isDraggingEquipment)
        {
            context.NotifyEquipmentDragStarted?.Invoke(draggedEntry);
        }
    }

    public void HandleDrag(State state, Context context, PointerEventData eventData)
    {
        if (!state.isDragging)
        {
            return;
        }

        UpdateDragVisualPosition(state, eventData);
    }

    public void HandleDrop(State state, Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index)
    {
        if (!state.isDragging || !context.IsSlotUsable(kind, index))
        {
            return;
        }

        InventoryShortcutRuntimeBinder.SlotRef rawTarget = new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index };
        if (!context.TryGetSlotData(rawTarget, out _))
        {
            return;
        }

        if (!context.TryGetSlotData(state.draggingSource, out InventoryShortcutRuntimeBinder.ItemSlotData sourceData))
        {
            return;
        }

        InventoryShortcutRuntimeBinder.SlotRef effectiveTarget = context.ResolvePrimarySlotRef(rawTarget);
        if (state.draggingSource.kind == effectiveTarget.kind &&
            state.draggingSource.index == effectiveTarget.index &&
            !(rawTarget.kind == effectiveTarget.kind && rawTarget.index != effectiveTarget.index && !IsFootprintItem(context, sourceData)))
        {
            return;
        }

        if (context.TryTransferToSlot != null && context.TryTransferToSlot(rawTarget, sourceData))
        {
            state.hoveredRotateSlot = context.ResolvePrimarySlotRef(rawTarget);
            state.hasHoveredRotateSlot = true;
        }
    }

    public void HandlePointerEnter(State state, Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index, PointerEventData eventData)
    {
        if (state.isDragging)
        {
            return;
        }

        if (!context.IsSlotUsable(kind, index))
        {
            ClearHoveredRotateSlot(state, context);
            context.HideItemTooltip?.Invoke();
            return;
        }

        InventoryShortcutRuntimeBinder.SlotRef slot = context.ResolvePrimarySlotRef(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index });
        if (!context.TryGetSlotData(slot, out InventoryShortcutRuntimeBinder.ItemSlotData data) || string.IsNullOrWhiteSpace(data.itemId))
        {
            ClearHoveredRotateSlot(state, context);
            context.HideItemTooltip?.Invoke();
            return;
        }

        state.hoveredRotateSlot = slot;
        state.hasHoveredRotateSlot = true;

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
        if (entry == null || (context.ShouldShowWeaponTooltip != null && !context.ShouldShowWeaponTooltip(entry)))
        {
            context.HideItemTooltip?.Invoke();
            return;
        }

        InventoryShortcutRuntimeBinder.SlotWidget widget = ResolveHoveredWidget(context, slot, eventData);
        if (widget == null || widget.root == null)
        {
            context.HideItemTooltip?.Invoke();
            return;
        }

        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Item,
            widget.root,
            0.5f,
            () => context.ShowItemTooltip?.Invoke(widget, entry, slot),
            () => context.HideItemTooltip?.Invoke());
    }

    public void HandlePointerExit(State state, Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index, PointerEventData eventData)
    {
        InventoryShortcutRuntimeBinder.SlotRef slot = context.ResolvePrimarySlotRef(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index });
        if (state.hasHoveredRotateSlot &&
            state.hoveredRotateSlot.kind == slot.kind &&
            state.hoveredRotateSlot.index == slot.index)
        {
            ClearHoveredRotateSlot(state, context);
        }

        InventoryShortcutRuntimeBinder.SlotWidget widget =
            ResolveHoveredWidget(context, new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index }, eventData) ??
            context.GetWidget(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index });
        if (widget == null || widget.root == null)
        {
            context.HideItemTooltip?.Invoke();
            return;
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Item, widget.root, eventData);
    }

    public void HandlePointerClick(
        State state,
        Context context,
        InventoryShortcutRuntimeBinder.SlotKind kind,
        InventoryShortcutRuntimeBinder.SlotSurface surface,
        int index,
        PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Right || state.isDragging)
        {
            return;
        }

        if (!context.IsSlotUsable(kind, index))
        {
            return;
        }

        InventoryShortcutRuntimeBinder.SlotRef source = context.ResolvePrimarySlotRef(new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index });
        if (!context.TryGetSlotData(source, out InventoryShortcutRuntimeBinder.ItemSlotData sourceData) || sourceData.IsEmpty)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.SlotWidget widget =
            ResolveHoveredWidget(context, new InventoryShortcutRuntimeBinder.SlotRef { kind = kind, index = index }, eventData) ??
            context.GetWidget(source);
        InventoryShortcutRuntimeBinder.StorageRightClickTarget target =
            widget != null ? widget.rightClickTarget : InventoryShortcutRuntimeBinder.StorageRightClickTarget.Warehouse;

        if (context.TryHandleRightClickMove == null ||
            !context.TryHandleRightClickMove(source, surface, target, sourceData))
        {
            return;
        }

        eventData.Use();
    }

    public void HandleEndDrag(State state, Context context)
    {
        bool wasDraggingEquipment = state.isDraggingEquipment;

        state.isDragging = false;
        state.isDraggingEquipment = false;
        if (state.dragIconRoot != null)
        {
            state.dragIconRoot.gameObject.SetActive(false);
        }

        if (state.dragIconImage != null)
        {
            state.dragIconImage.sprite = null;
            state.dragIconImage.enabled = false;
        }

        context.SetWidgetDraggingVisible?.Invoke(state.draggingSourceWidget, true);
        state.draggingSourceWidget = null;
        context.RefreshByRef?.Invoke(state.draggingSource);

        if (wasDraggingEquipment)
        {
            context.NotifyEquipmentDragEnded?.Invoke();
        }
    }

    private static void ClearHoveredRotateSlot(State state, Context context)
    {
        state.hasHoveredRotateSlot = false;
        state.hoveredRotateSlot = default;
    }

    private void TryRotateHoveredOneByTwoItem(State state, Context context)
    {
        InventoryShortcutRuntimeBinder.SlotRef source = context.ResolvePrimarySlotRef(state.hoveredRotateSlot);
        if (!context.TryGetSlotData(source, out InventoryShortcutRuntimeBinder.ItemSlotData data) || data.IsEmpty || source.kind == InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            return;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
        if (entry == null || context.IsOneByTwoItem == null || !context.IsOneByTwoItem(entry))
        {
            return;
        }

        List<InventoryShortcutRuntimeBinder.ItemSlotData> list = context.GetDataList(source.kind);
        if (list == null)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData rotatedData = data;
        rotatedData.isRotated = !rotatedData.isRotated;

        List<InventoryShortcutRuntimeBinder.ItemSlotData> working = context.CloneItemSlotDataList(list);
        context.ClearPlacement?.Invoke(source.kind, source.index, data, working);
        if (context.CanPlaceDataAt == null || !context.CanPlaceDataAt(source.kind, source.index, rotatedData, working))
        {
            return;
        }

        int oldExtensionIndex = context.GetExtensionIndexForData != null ? context.GetExtensionIndexForData(source.kind, source.index, data) : -1;
        int newExtensionIndex = context.GetExtensionIndexForData != null ? context.GetExtensionIndexForData(source.kind, source.index, rotatedData) : -1;
        List<InventoryShortcutRuntimeBinder.ItemSlotData> liveData = context.GetDataList(source.kind);
        if (liveData == null || source.index < 0 || source.index >= liveData.Count)
        {
            return;
        }

        liveData[source.index] = context.PrepareItemSlotDataForStorage(rotatedData, $"{source.kind} {source.index}");
        if (newExtensionIndex >= 0 && newExtensionIndex < liveData.Count)
        {
            liveData[newExtensionIndex] = context.PrepareItemSlotDataForStorage(new InventoryShortcutRuntimeBinder.ItemSlotData
            {
                isFootprintExtension = true,
                primarySlotIndex = source.index
            }, $"{source.kind} 扩展格 {newExtensionIndex}");
        }

        if (oldExtensionIndex >= 0 && oldExtensionIndex < liveData.Count && oldExtensionIndex != newExtensionIndex)
        {
            liveData[oldExtensionIndex] = default;
        }

        context.RefreshFootprintSlots?.Invoke(source.kind, source.index, rotatedData);
        context.PlayItemSound?.Invoke(rotatedData.itemId);
    }

    private static bool IsFootprintItem(Context context, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (data.isFootprintExtension || string.IsNullOrWhiteSpace(data.itemId))
        {
            return false;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry(data.itemId);
        return entry != null && context.IsOneByTwoItem != null && context.IsOneByTwoItem(entry);
    }

    private void EnsureDragVisual(State state, RectTransform fromRoot)
    {
        if (state.dragIconRoot != null && state.dragIconImage != null)
        {
            return;
        }

        if (state.dragCanvas == null)
        {
            state.dragCanvas = fromRoot.GetComponentInParent<Canvas>();
        }

        if (state.dragCanvas == null)
        {
            return;
        }

        GameObject go = new GameObject("InventoryDragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(state.dragCanvas.transform, false);

        state.dragIconRoot = go.GetComponent<RectTransform>();
        state.dragIconRoot.anchorMin = new Vector2(0.5f, 0.5f);
        state.dragIconRoot.anchorMax = new Vector2(0.5f, 0.5f);
        state.dragIconRoot.pivot = new Vector2(0.5f, 0.5f);

        state.dragIconImage = go.GetComponent<Image>();
        state.dragIconImage.raycastTarget = false;
        state.dragIconImage.preserveAspect = true;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        go.SetActive(false);
    }

    private static void RebuildDragVisual(
        State state,
        Context context,
        InventoryShortcutRuntimeBinder.SlotWidget sourceWidget,
        InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (state.dragIconRoot == null || state.dragIconImage == null)
        {
            return;
        }

        Sprite dragSprite = context.ResolveRuntimeIconSprite != null ? context.ResolveRuntimeIconSprite(sourceWidget) : null;
        dragSprite ??= context.ResolveDisplaySprite != null ? context.ResolveDisplaySprite(data) : null;

        state.dragIconImage.sprite = dragSprite;
        state.dragIconImage.color = new Color(1f, 1f, 1f, 0.9f);
        state.dragIconImage.enabled = state.dragIconImage.sprite != null;
        state.dragIconImage.SetNativeSize();
    }

    private static void UpdateDragVisualPosition(State state, PointerEventData eventData)
    {
        if (state.dragCanvas == null || state.dragIconRoot == null)
        {
            return;
        }

        Camera uiCamera = state.dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : state.dragCanvas.worldCamera;
        RectTransform canvasRect = state.dragCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out Vector2 localPos))
        {
            state.dragIconRoot.anchoredPosition = localPos;
        }
    }

    private InventoryShortcutRuntimeBinder.SlotWidget ResolveDraggedWidget(
        Context context,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        PointerEventData eventData)
    {
        Transform pointerTransform = eventData != null
            ? (eventData.pointerDrag != null
                ? eventData.pointerDrag.transform
                : eventData.pointerPressRaycast.gameObject != null ? eventData.pointerPressRaycast.gameObject.transform : null)
            : null;

        if (pointerTransform != null)
        {
            InventoryShortcutRuntimeBinder.SlotWidget matched = FindWidgetByTransform(context, slot.kind, pointerTransform);
            if (matched != null)
            {
                return matched;
            }
        }

        return context.GetWidget(slot);
    }

    private InventoryShortcutRuntimeBinder.SlotWidget ResolveHoveredWidget(
        Context context,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        PointerEventData eventData)
    {
        Transform pointerTransform = eventData != null
            ? (eventData.pointerEnter != null
                ? eventData.pointerEnter.transform
                : eventData.pointerCurrentRaycast.gameObject != null ? eventData.pointerCurrentRaycast.gameObject.transform : null)
            : null;

        if (pointerTransform != null)
        {
            InventoryShortcutRuntimeBinder.SlotWidget matched = FindWidgetByTransform(context, slot.kind, pointerTransform);
            if (matched != null)
            {
                return matched;
            }
        }

        return context.GetWidget(slot);
    }

    private static InventoryShortcutRuntimeBinder.SlotWidget FindWidgetByTransform(
        Context context,
        InventoryShortcutRuntimeBinder.SlotKind kind,
        Transform target)
    {
        if (target == null)
        {
            return null;
        }

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Backpack)
        {
            InventoryShortcutRuntimeBinder.SlotWidget matched = FindWidgetByTransform(context.BackpackSlots, target);
            if (matched != null)
            {
                return matched;
            }

            for (int i = 0; i < context.ExtraBackpackSlots.Count; i++)
            {
                matched = FindWidgetByTransform(context.ExtraBackpackSlots[i], target);
                if (matched != null)
                {
                    return matched;
                }
            }

            return null;
        }

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Warehouse)
        {
            return FindWidgetByTransform(context.WarehouseSlots, target);
        }

        if (kind == InventoryShortcutRuntimeBinder.SlotKind.Chest)
        {
            return FindWidgetByTransform(context.ChestSlots, target);
        }

        InventoryShortcutRuntimeBinder.SlotWidget equipmentWidget = FindWidgetByTransform(context.EquipmentSlots, target);
        if (equipmentWidget != null)
        {
            return equipmentWidget;
        }

        for (int i = 0; i < context.ExtraEquipmentSlots.Count; i++)
        {
            equipmentWidget = FindWidgetByTransform(context.ExtraEquipmentSlots[i], target);
            if (equipmentWidget != null)
            {
                return equipmentWidget;
            }
        }

        return null;
    }

    private static InventoryShortcutRuntimeBinder.SlotWidget FindWidgetByTransform(List<InventoryShortcutRuntimeBinder.SlotWidget> widgets, Transform target)
    {
        if (widgets == null || target == null)
        {
            return null;
        }

        for (int i = 0; i < widgets.Count; i++)
        {
            InventoryShortcutRuntimeBinder.SlotWidget widget = widgets[i];
            if (widget == null || widget.root == null)
            {
                continue;
            }

            if (target == widget.root || target.IsChildOf(widget.root))
            {
                return widget;
            }
        }

        return null;
    }
}
