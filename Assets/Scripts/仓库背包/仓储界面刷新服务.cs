using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 仓储界面刷新服务
{
    internal sealed class Context
    {
        public List<InventoryShortcutRuntimeBinder.SlotWidget> WarehouseSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> BackpackSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> ChestSlots;
        public List<List<InventoryShortcutRuntimeBinder.SlotWidget>> ExtraBackpackSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> EquipmentSlots;
        public List<List<InventoryShortcutRuntimeBinder.SlotWidget>> ExtraEquipmentSlots;
        public List<InventoryShortcutRuntimeBinder.ItemSlotData> WarehouseData;
        public List<InventoryShortcutRuntimeBinder.ItemSlotData> BackpackData;
        public List<InventoryShortcutRuntimeBinder.ItemSlotData> ChestData;
        public Func<List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetCurrentEquipmentData;
        public Func<int> GetExpectedEquipmentSlotCount;
        public Func<string> ResolveEquipmentCharacterId;
        public Func<InventoryShortcutRuntimeBinder.SlotKind, int, bool> IsSlotUsable;
        public Func<InventoryShortcutRuntimeBinder.ItemSlotData, bool> ShouldDisplayWarehouseItem;
        public Func<InventoryShortcutRuntimeBinder.ItemSlotData, bool> ShouldDisplayBackpackItem;
        public Action<string> RefreshRuntimeWeaponModelForCharacter;
        public Func<string, ItemDatabase.ItemEntry> ResolveItemEntry;
        public Func<ItemDatabase.ItemEntry, GameObject> ResolveQualityBackgroundPrefab;
        public Func<ItemDatabase.ItemEntry, bool> IsOneByTwoItem;
        public Func<Transform, string, Transform> FindChildByName;
        public Func<Transform, string, Transform> FindDescendantByName;
        public Color DisabledSlotColor;
    }

    public void RefreshWarehouseFilteredView(Context context)
    {
        int count = context.WarehouseSlots != null ? context.WarehouseSlots.Count : 0;
        for (int i = 0; i < count; i++)
        {
            RefreshWarehouseSlot(context, i);
        }
    }

    public void RefreshBackpackFilteredView(Context context)
    {
        int count = context.BackpackSlots != null ? context.BackpackSlots.Count : 0;
        for (int i = 0; i < count; i++)
        {
            RefreshBackpackSlot(context, i);
        }

        if (context.ExtraBackpackSlots == null || context.BackpackData == null)
        {
            return;
        }

        for (int groupIndex = 0; groupIndex < context.ExtraBackpackSlots.Count; groupIndex++)
        {
            List<InventoryShortcutRuntimeBinder.SlotWidget> group = context.ExtraBackpackSlots[groupIndex];
            int groupCount = group != null ? group.Count : 0;
            int maxCount = Mathf.Min(groupCount, context.BackpackData.Count);
            for (int i = 0; i < maxCount; i++)
            {
                RefreshExtraBackpackSlot(context, group, i);
            }
        }
    }

    public void RefreshChestFilteredView(Context context)
    {
        int count = context.ChestSlots != null ? context.ChestSlots.Count : 0;
        for (int i = 0; i < count; i++)
        {
            RefreshChestSlot(context, i);
        }
    }

    public void RefreshWarehouseSlot(Context context, int index)
    {
        if (context.WarehouseSlots == null || context.WarehouseData == null ||
            index < 0 || index >= context.WarehouseSlots.Count || index >= context.WarehouseData.Count)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData data = context.WarehouseData[index];
        bool shouldDisplay = context.ShouldDisplayWarehouseItem == null || context.ShouldDisplayWarehouseItem(data);
        ApplyItemToWidget(context, context.WarehouseSlots[index], shouldDisplay ? data : default);
        ApplyWidgetAvailability(context, context.WarehouseSlots[index], IsSlotUsable(context, InventoryShortcutRuntimeBinder.SlotKind.Warehouse, index));
    }

    public void RefreshBackpackSlot(Context context, int index)
    {
        if (context.BackpackSlots == null || context.BackpackData == null ||
            index < 0 || index >= context.BackpackSlots.Count || index >= context.BackpackData.Count)
        {
            return;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData data = context.BackpackData[index];
        bool shouldDisplay = context.ShouldDisplayBackpackItem == null || context.ShouldDisplayBackpackItem(data);
        ApplyItemToWidget(context, context.BackpackSlots[index], shouldDisplay ? data : default);
        ApplyWidgetAvailability(context, context.BackpackSlots[index], IsSlotUsable(context, InventoryShortcutRuntimeBinder.SlotKind.Backpack, index));
    }

    public void RefreshChestSlot(Context context, int index)
    {
        if (context.ChestSlots == null || context.ChestData == null ||
            index < 0 || index >= context.ChestSlots.Count || index >= context.ChestData.Count)
        {
            return;
        }

        ApplyItemToWidget(context, context.ChestSlots[index], context.ChestData[index]);
        ApplyWidgetAvailability(context, context.ChestSlots[index], IsSlotUsable(context, InventoryShortcutRuntimeBinder.SlotKind.Chest, index));
    }

    public void RefreshEquipmentSlot(Context context, int index)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipmentData = context.GetCurrentEquipmentData != null
            ? context.GetCurrentEquipmentData()
            : null;
        int slotCount = context.GetExpectedEquipmentSlotCount != null ? context.GetExpectedEquipmentSlotCount() : 0;
        if (index < 0 || index >= slotCount)
        {
            return;
        }

        bool isUsable = IsSlotUsable(context, InventoryShortcutRuntimeBinder.SlotKind.Equipment, index);
        InventoryShortcutRuntimeBinder.ItemSlotData data = equipmentData != null && index < equipmentData.Count ? equipmentData[index] : default;
        ApplyEquipmentSlotData(context, index, data, isUsable);
        if (context.ResolveEquipmentCharacterId != null && context.RefreshRuntimeWeaponModelForCharacter != null)
        {
            context.RefreshRuntimeWeaponModelForCharacter(context.ResolveEquipmentCharacterId());
        }
    }

    public void RefreshEquipmentSlots(Context context)
    {
        string equipmentCharacterId = context.ResolveEquipmentCharacterId != null ? context.ResolveEquipmentCharacterId() : null;
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipmentData = context.GetCurrentEquipmentData != null
            ? context.GetCurrentEquipmentData()
            : null;
        int slotCount = context.GetExpectedEquipmentSlotCount != null ? context.GetExpectedEquipmentSlotCount() : 0;
        for (int i = 0; i < slotCount; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData data = equipmentData != null && i < equipmentData.Count ? equipmentData[i] : default;
            ApplyEquipmentSlotData(context, i, data, IsSlotUsable(context, InventoryShortcutRuntimeBinder.SlotKind.Equipment, i));
        }

        context.RefreshRuntimeWeaponModelForCharacter?.Invoke(equipmentCharacterId);
    }

    public void RefreshExtraBackpackSlots(Context context, int index)
    {
        if (context.ExtraBackpackSlots == null)
        {
            return;
        }

        for (int i = 0; i < context.ExtraBackpackSlots.Count; i++)
        {
            RefreshExtraBackpackSlot(context, context.ExtraBackpackSlots[i], index);
        }
    }

    public void ClearRuntimeVisuals(List<InventoryShortcutRuntimeBinder.SlotWidget> widgets)
    {
        if (widgets == null)
        {
            return;
        }

        for (int i = 0; i < widgets.Count; i++)
        {
            InventoryShortcutRuntimeBinder.SlotWidget widget = widgets[i];
            if (widget == null)
            {
                continue;
            }

            ClearRuntimeVisual(ref widget.runtimeBackgroundVisual);
            ClearRuntimeVisual(ref widget.runtimeIconVisual);
        }
    }

    private static bool IsSlotUsable(Context context, InventoryShortcutRuntimeBinder.SlotKind kind, int index)
    {
        return context.IsSlotUsable != null && context.IsSlotUsable(kind, index);
    }

    private void ApplyEquipmentSlotData(Context context, int index, InventoryShortcutRuntimeBinder.ItemSlotData data, bool isUsable)
    {
        ApplyEquipmentSlotDataToList(context, context.EquipmentSlots, index, data, isUsable);
        if (context.ExtraEquipmentSlots == null)
        {
            return;
        }

        for (int i = 0; i < context.ExtraEquipmentSlots.Count; i++)
        {
            ApplyEquipmentSlotDataToList(context, context.ExtraEquipmentSlots[i], index, data, isUsable);
        }
    }

    private void ApplyEquipmentSlotDataToList(
        Context context,
        List<InventoryShortcutRuntimeBinder.SlotWidget> slots,
        int index,
        InventoryShortcutRuntimeBinder.ItemSlotData data,
        bool isUsable)
    {
        if (slots == null || index < 0 || index >= slots.Count)
        {
            return;
        }

        ApplyItemToWidget(context, slots[index], data);
        ApplyWidgetAvailability(context, slots[index], isUsable);
    }

    private void RefreshExtraBackpackSlot(Context context, List<InventoryShortcutRuntimeBinder.SlotWidget> slots, int index)
    {
        if (slots == null || context.BackpackData == null ||
            index < 0 || index >= slots.Count || index >= context.BackpackData.Count)
        {
            return;
        }

        ApplyItemToWidget(context, slots[index], context.BackpackData[index]);
        ApplyWidgetAvailability(context, slots[index], IsSlotUsable(context, InventoryShortcutRuntimeBinder.SlotKind.Backpack, index));
    }

    private void ApplyItemToWidget(Context context, InventoryShortcutRuntimeBinder.SlotWidget widget, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (widget == null || widget.icon == null)
        {
            return;
        }

        RebuildItemVisual(context, widget, data);
    }

    private static void ApplyWidgetAvailability(Context context, InventoryShortcutRuntimeBinder.SlotWidget widget, bool isUsable)
    {
        if (widget == null)
        {
            return;
        }

        if (widget.button != null)
        {
            ColorBlock colors = widget.button.colors;
            colors.disabledColor = context.DisabledSlotColor;
            widget.button.colors = colors;
            widget.button.interactable = isUsable;
        }
    }

    private void RebuildItemVisual(Context context, InventoryShortcutRuntimeBinder.SlotWidget widget, InventoryShortcutRuntimeBinder.ItemSlotData data)
    {
        if (widget == null || widget.icon == null)
        {
            return;
        }

        ClearRuntimeVisual(ref widget.runtimeBackgroundVisual);
        ClearRuntimeVisual(ref widget.runtimeIconVisual);

        if (string.IsNullOrWhiteSpace(data.itemId))
        {
            return;
        }

        ItemDatabase.ItemEntry entry = context.ResolveItemEntry != null ? context.ResolveItemEntry(data.itemId) : null;
        if (entry == null || entry.prefab == null)
        {
            return;
        }

        GameObject qualityBackgroundPrefab = context.ResolveQualityBackgroundPrefab != null
            ? context.ResolveQualityBackgroundPrefab(entry)
            : null;
        widget.runtimeBackgroundVisual = TryCreateRuntimePrefabVisual(qualityBackgroundPrefab, widget.backgroundAnchor ?? widget.root);
        widget.runtimeIconVisual = TryCreateRuntimeVisual(context, entry.prefab.transform, "物品图标", widget.iconAnchor ?? widget.root);
        bool shouldRotate = data.isRotated && context.IsOneByTwoItem != null && context.IsOneByTwoItem(entry);
        ApplyRuntimeVisualRotation(widget.runtimeBackgroundVisual, shouldRotate);
        ApplyRuntimeVisualRotation(widget.runtimeIconVisual, shouldRotate);
    }

    private static void ApplyRuntimeVisualRotation(GameObject target, bool rotated)
    {
        RectTransform rect = target != null ? target.transform as RectTransform : null;
        if (rect == null)
        {
            return;
        }

        if (rotated)
        {
            RectTransform parentRect = rect.parent as RectTransform;
            float halfCellWidth = parentRect != null ? parentRect.rect.width * 0.5f : 0f;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(halfCellWidth, 0f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 90f);
            return;
        }

        rect.localRotation = Quaternion.identity;
    }

    private static GameObject TryCreateRuntimeVisual(Context context, Transform prefabRoot, string childName, RectTransform anchor)
    {
        if (prefabRoot == null || anchor == null)
        {
            return null;
        }

        Transform source = context.FindChildByName != null ? context.FindChildByName(prefabRoot, childName) : null;
        source ??= context.FindDescendantByName != null ? context.FindDescendantByName(prefabRoot, childName) : null;
        if (source == null)
        {
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(source.gameObject, anchor, false);
        instance.name = source.gameObject.name;

        RectTransform sourceRect = source as RectTransform;
        RectTransform instanceRect = instance.transform as RectTransform;
        if (instanceRect != null && sourceRect != null)
        {
            instanceRect.anchorMin = sourceRect.anchorMin;
            instanceRect.anchorMax = sourceRect.anchorMax;
            instanceRect.pivot = sourceRect.pivot;
            instanceRect.anchoredPosition = sourceRect.anchoredPosition;
            instanceRect.sizeDelta = sourceRect.sizeDelta;
            instanceRect.localRotation = sourceRect.localRotation;
            instanceRect.localScale = sourceRect.localScale;
            instanceRect.offsetMin = sourceRect.offsetMin;
            instanceRect.offsetMax = sourceRect.offsetMax;
        }
        else if (instanceRect != null)
        {
            instanceRect.anchorMin = new Vector2(0.5f, 0.5f);
            instanceRect.anchorMax = new Vector2(0.5f, 0.5f);
            instanceRect.pivot = new Vector2(0.5f, 0.5f);
            instanceRect.anchoredPosition3D = Vector3.zero;
            instanceRect.localRotation = Quaternion.identity;
            instanceRect.localScale = Vector3.one;
        }

        DisableRaycasts(instance);
        instance.transform.SetAsLastSibling();
        return instance;
    }

    private static GameObject TryCreateRuntimePrefabVisual(GameObject prefab, RectTransform anchor)
    {
        if (prefab == null || anchor == null)
        {
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab, anchor, false);
        RectTransform instanceRect = instance.transform as RectTransform;
        RectTransform prefabRect = prefab.transform as RectTransform;
        if (instanceRect != null)
        {
            if (prefabRect != null)
            {
                instanceRect.anchorMin = prefabRect.anchorMin;
                instanceRect.anchorMax = prefabRect.anchorMax;
                instanceRect.pivot = prefabRect.pivot;
                instanceRect.anchoredPosition = prefabRect.anchoredPosition;
                instanceRect.sizeDelta = prefabRect.sizeDelta;
                instanceRect.localRotation = prefabRect.localRotation;
                instanceRect.localScale = prefabRect.localScale;
                instanceRect.offsetMin = prefabRect.offsetMin;
                instanceRect.offsetMax = prefabRect.offsetMax;
            }
            else
            {
                instanceRect.anchorMin = new Vector2(0.5f, 0.5f);
                instanceRect.anchorMax = new Vector2(0.5f, 0.5f);
                instanceRect.pivot = new Vector2(0.5f, 0.5f);
                instanceRect.anchoredPosition3D = Vector3.zero;
                instanceRect.localRotation = Quaternion.identity;
                instanceRect.localScale = Vector3.one;
            }
        }

        DisableRaycasts(instance);
        instance.transform.SetAsLastSibling();
        return instance;
    }

    private static void DisableRaycasts(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private static void ClearRuntimeVisual(ref GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(visual);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(visual);
        }

        visual = null;
    }
}
