using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryShortcutRuntimeBinder : MonoBehaviour
{
    [Serializable]
    public struct ItemSlotData
    {
        public string itemId;
        public Sprite icon;
        public int count;
        public int maxStack;

        public bool IsEmpty => icon == null && string.IsNullOrEmpty(itemId) && count <= 0;
    }

    private enum SlotKind
    {
        Warehouse,
        Backpack,
        Equipment
    }

    private struct SlotRef
    {
        public SlotKind kind;
        public int index;
    }

    private sealed class SlotWidget
    {
        public RectTransform root;
        public Button button;
        public Image icon;
        public bool iconIsRoot;
        public Color iconOriginalColor;
        public Sprite iconOriginalSprite;
    }

    private sealed class SlotDragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private InventoryShortcutRuntimeBinder owner;
        private SlotKind kind;
        private int index;

        public void Configure(InventoryShortcutRuntimeBinder binder, SlotKind slotKind, int slotIndex)
        {
            owner = binder;
            kind = slotKind;
            index = slotIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.HandleBeginDrag(kind, index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.HandleDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.HandleEndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.HandleDrop(kind, index);
        }
    }

    private const string WarehouseContainerPath = "UI控制器/目录/仓库页面/仓库面板/格子区域/格子容器";
    private const string BackpackContainerPath = "UI控制器/目录/仓库页面/背包面板/格子区域/格子容器";
    private const string EquipmentContainerPath = "UI控制器/目录/角色页面/左边栏位/装备栏位";
    private const string QuickAnchorPath = "UI控制器/目录/角色页面/右边栏位/格子区域";
    private const string SlotNameKeyword = "格子";

    private static InventoryShortcutRuntimeBinder instance;

    private readonly List<ItemSlotData> warehouseData = new List<ItemSlotData>();
    private readonly List<ItemSlotData> backpackData = new List<ItemSlotData>();
    private readonly List<ItemSlotData> equipmentData = new List<ItemSlotData>();

    private readonly List<SlotWidget> warehouseSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> backpackSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> equipmentSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> quickSlots = new List<SlotWidget>();

    private bool isDragging;
    private SlotRef draggingSource;
    private Canvas dragCanvas;
    private RectTransform dragIconRoot;
    private Image dragIconImage;

    public static int WarehouseSlotCount => instance != null ? instance.backpackData.Count : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("InventoryShortcutRuntimeBinder");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<InventoryShortcutRuntimeBinder>();
    }

    public static bool TryGetWarehouseSlotData(int index, out ItemSlotData data)
    {
        data = default;
        if (instance == null || index < 0 || index >= instance.backpackData.Count)
        {
            return false;
        }

        data = instance.backpackData[index];
        return true;
    }

    public static bool TrySetWarehouseSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.backpackData.Count)
        {
            return false;
        }

        instance.backpackData[index] = data;
        instance.RefreshBackpackSlot(index);
        instance.RefreshQuickSlot(index);
        return true;
    }

    public static int AddItem(string itemId, Sprite icon, int count, int maxStack = 99)
    {
        if (instance == null || string.IsNullOrEmpty(itemId) || icon == null || count <= 0)
        {
            return count;
        }

        int remain = count;

        for (int i = 0; i < instance.backpackData.Count && remain > 0; i++)
        {
            ItemSlotData slot = instance.backpackData[i];
            if (slot.IsEmpty || slot.itemId != itemId)
            {
                continue;
            }

            int cap = Mathf.Max(1, slot.maxStack > 0 ? slot.maxStack : maxStack);
            if (slot.count >= cap)
            {
                continue;
            }

            int add = Mathf.Min(remain, cap - slot.count);
            slot.count += add;
            slot.icon = icon;
            slot.maxStack = cap;
            instance.backpackData[i] = slot;
            remain -= add;
            instance.RefreshBackpackSlot(i);
            instance.RefreshQuickSlot(i);
        }

        for (int i = 0; i < instance.backpackData.Count && remain > 0; i++)
        {
            if (!instance.backpackData[i].IsEmpty)
            {
                continue;
            }

            int cap = Mathf.Max(1, maxStack);
            int add = Mathf.Min(remain, cap);
            instance.backpackData[i] = new ItemSlotData
            {
                itemId = itemId,
                icon = icon,
                count = add,
                maxStack = cap
            };
            remain -= add;
            instance.RefreshBackpackSlot(i);
            instance.RefreshQuickSlot(i);
        }

        return remain;
    }
    public static bool RemoveItemAt(int slotIndex, int count)
    {
        if (instance == null || slotIndex < 0 || slotIndex >= instance.backpackData.Count || count <= 0)
        {
            return false;
        }

        ItemSlotData slot = instance.backpackData[slotIndex];
        if (slot.IsEmpty || slot.count <= 0)
        {
            return false;
        }

        slot.count -= count;
        if (slot.count <= 0)
        {
            slot = default;
        }

        instance.backpackData[slotIndex] = slot;
        instance.RefreshBackpackSlot(slotIndex);
        instance.RefreshQuickSlot(slotIndex);
        return true;
    }

    public static bool MoveItem(int fromSlot, int toSlot)
    {
        if (instance == null ||
            fromSlot < 0 || toSlot < 0 ||
            fromSlot >= instance.backpackData.Count || toSlot >= instance.backpackData.Count ||
            fromSlot == toSlot)
        {
            return false;
        }

        ItemSlotData from = instance.backpackData[fromSlot];
        ItemSlotData to = instance.backpackData[toSlot];
        if (from.IsEmpty)
        {
            return false;
        }

        if (!to.IsEmpty && to.itemId == from.itemId)
        {
            int cap = Mathf.Max(1, to.maxStack > 0 ? to.maxStack : from.maxStack);
            int canMove = Mathf.Min(from.count, Mathf.Max(0, cap - to.count));
            if (canMove > 0)
            {
                to.count += canMove;
                from.count -= canMove;
                if (from.count <= 0)
                {
                    from = default;
                }

                instance.backpackData[fromSlot] = from;
                instance.backpackData[toSlot] = to;
                instance.RefreshBackpackSlot(fromSlot);
                instance.RefreshBackpackSlot(toSlot);
                instance.RefreshQuickSlot(fromSlot);
                instance.RefreshQuickSlot(toSlot);
                return true;
            }
        }

        instance.backpackData[fromSlot] = to;
        instance.backpackData[toSlot] = from;
        instance.RefreshBackpackSlot(fromSlot);
        instance.RefreshBackpackSlot(toSlot);
        instance.RefreshQuickSlot(fromSlot);
        instance.RefreshQuickSlot(toSlot);
        return true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        UnbindAll();

        CollectWarehouseSlots();
        CollectBackpackSlots();
        CollectEquipmentSlots();

        EnsureDataSize(warehouseData, warehouseSlots.Count);
        EnsureDataSize(backpackData, backpackSlots.Count);
        EnsureDataSize(equipmentData, equipmentSlots.Count);

        RectTransform quickAnchor = FindQuickAnchor();
        if (quickAnchor != null)
        {
            ApplyBackpackLayoutToQuickAnchor(quickAnchor);
            EnsureQuickSlots(quickAnchor);
            CollectSlotsFromContainer(quickAnchor, quickSlots);
        }

        BindDragRelays();
        RefreshAll();
    }

    private void CollectWarehouseSlots()
    {
        warehouseSlots.Clear();
        GameObject container = GameObject.Find(WarehouseContainerPath);
        if (container != null)
        {
            CollectSlotsFromContainer(container.transform, warehouseSlots);
        }
    }

    private void CollectBackpackSlots()
    {
        backpackSlots.Clear();
        GameObject container = GameObject.Find(BackpackContainerPath);
        if (container != null)
        {
            CollectSlotsFromContainer(container.transform, backpackSlots);
        }
    }

    private void CollectEquipmentSlots()
    {
        equipmentSlots.Clear();
        GameObject container = GameObject.Find(EquipmentContainerPath);
        if (container != null)
        {
            CollectSlotsFromContainer(container.transform, equipmentSlots);
        }
    }

    private RectTransform FindQuickAnchor()
    {
        GameObject anchor = GameObject.Find(QuickAnchorPath);
        return anchor != null ? anchor.transform as RectTransform : null;
    }

    private void ApplyBackpackLayoutToQuickAnchor(RectTransform quickAnchor)
    {
        if (quickAnchor == null || backpackSlots.Count == 0 || backpackSlots[0].root == null)
        {
            return;
        }

        RectTransform sourceContainer = backpackSlots[0].root.parent as RectTransform;
        if (sourceContainer == null)
        {
            return;
        }

        quickAnchor.sizeDelta = sourceContainer.sizeDelta;

        GridLayoutGroup source = sourceContainer.GetComponent<GridLayoutGroup>();
        if (source == null)
        {
            return;
        }

        GridLayoutGroup target = quickAnchor.GetComponent<GridLayoutGroup>();
        if (target == null)
        {
            target = quickAnchor.gameObject.AddComponent<GridLayoutGroup>();
        }

        target.padding = source.padding;
        target.cellSize = source.cellSize;
        target.spacing = source.spacing;
        target.startCorner = source.startCorner;
        target.startAxis = source.startAxis;
        target.childAlignment = source.childAlignment;
        target.constraint = source.constraint;
        target.constraintCount = source.constraintCount;
    }

    private void EnsureQuickSlots(RectTransform quickAnchor)
    {
        quickSlots.Clear();
        CollectSlotsFromContainer(quickAnchor, quickSlots);
        if (quickSlots.Count == backpackSlots.Count)
        {
            return;
        }

        for (int i = quickAnchor.childCount - 1; i >= 0; i--)
        {
            Destroy(quickAnchor.GetChild(i).gameObject);
        }

        if (backpackSlots.Count == 0)
        {
            return;
        }

        GameObject template = backpackSlots[0].root != null ? backpackSlots[0].root.gameObject : null;
        if (template == null)
        {
            return;
        }

        for (int i = 0; i < backpackSlots.Count; i++)
        {
            GameObject go = Instantiate(template, quickAnchor, false);
            go.name = "快捷格子 (" + (i + 1) + ")";
            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }
        }
    }

    private static void CollectSlotsFromContainer(Transform container, List<SlotWidget> target)
    {
        target.Clear();
        if (container == null)
        {
            return;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child = container.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            bool looksLikeSlot = child.name.Contains(SlotNameKeyword);
            Button button = child.GetComponent<Button>();
            if (!looksLikeSlot && button == null)
            {
                continue;
            }

            Image icon = FindBestIconImage(child);
            if (icon == null)
            {
                continue;
            }

            target.Add(new SlotWidget
            {
                root = child,
                button = button,
                icon = icon,
                iconIsRoot = icon.transform == child,
                iconOriginalColor = icon.color,
                iconOriginalSprite = icon.sprite
            });
        }
    }

    private static Image FindBestIconImage(RectTransform slotRoot)
    {
        Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
        Image rootImage = slotRoot.GetComponent<Image>();

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null)
            {
                continue;
            }

            string n = img.gameObject.name;
            if (n.Contains("图标") || n.Contains("Icon") || n.Contains("icon"))
            {
                return img;
            }
        }

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img != null && img != rootImage)
            {
                return img;
            }
        }

        return rootImage;
    }

    private static void EnsureDataSize(List<ItemSlotData> data, int size)
    {
        while (data.Count < size)
        {
            data.Add(default);
        }

        while (data.Count > size)
        {
            data.RemoveAt(data.Count - 1);
        }
    }

    private void BindDragRelays()
    {
        BindDragRelaysForList(warehouseSlots, SlotKind.Warehouse);
        BindDragRelaysForList(backpackSlots, SlotKind.Backpack);
        BindDragRelaysForList(quickSlots, SlotKind.Backpack);
        BindDragRelaysForList(equipmentSlots, SlotKind.Equipment);
    }

    private void BindDragRelaysForList(List<SlotWidget> slots, SlotKind kind)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            RectTransform root = slots[i].root;
            if (root == null)
            {
                continue;
            }

            SlotDragRelay relay = root.GetComponent<SlotDragRelay>();
            if (relay == null)
            {
                relay = root.gameObject.AddComponent<SlotDragRelay>();
            }

            relay.Configure(this, kind, i);
        }
    }

    private void HandleBeginDrag(SlotKind kind, int index, PointerEventData eventData)
    {
        if (isDragging)
        {
            return;
        }

        SlotRef source = new SlotRef { kind = kind, index = index };
        if (!TryGetSlotData(source, out ItemSlotData data) || data.IsEmpty)
        {
            return;
        }

        SlotWidget widget = GetWidget(source);
        if (widget == null || widget.root == null)
        {
            return;
        }

        EnsureDragVisual(widget.root);
        if (dragIconImage == null || dragIconRoot == null)
        {
            return;
        }

        dragIconImage.sprite = data.icon;
        dragIconImage.color = Color.white;
        dragIconRoot.sizeDelta = widget.root.rect.size;
        dragIconRoot.gameObject.SetActive(true);
        UpdateDragVisualPosition(eventData);

        draggingSource = source;
        isDragging = true;
    }

    private void HandleDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            UpdateDragVisualPosition(eventData);
        }
    }

    private void HandleDrop(SlotKind kind, int index)
    {
        if (!isDragging)
        {
            return;
        }

        SlotRef target = new SlotRef { kind = kind, index = index };
        if (draggingSource.kind == target.kind && draggingSource.index == target.index)
        {
            return;
        }

        if (!TryGetSlotData(target, out _))
        {
            return;
        }

        SwapSlotData(draggingSource, target);
        RefreshByRef(draggingSource);
        RefreshByRef(target);
    }

    private void HandleEndDrag()
    {
        isDragging = false;
        if (dragIconRoot != null)
        {
            dragIconRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureDragVisual(RectTransform fromRoot)
    {
        if (dragIconRoot != null && dragIconImage != null)
        {
            return;
        }

        if (dragCanvas == null)
        {
            dragCanvas = fromRoot.GetComponentInParent<Canvas>();
        }

        if (dragCanvas == null)
        {
            return;
        }

        GameObject go = new GameObject("InventoryDragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(dragCanvas.transform, false);

        dragIconRoot = go.GetComponent<RectTransform>();
        dragIconRoot.anchorMin = new Vector2(0.5f, 0.5f);
        dragIconRoot.anchorMax = new Vector2(0.5f, 0.5f);
        dragIconRoot.pivot = new Vector2(0.5f, 0.5f);

        dragIconImage = go.GetComponent<Image>();
        dragIconImage.raycastTarget = false;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        go.SetActive(false);
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragCanvas == null || dragIconRoot == null)
        {
            return;
        }

        Camera uiCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : dragCanvas.worldCamera;
        RectTransform canvasRect = dragCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out Vector2 localPos))
        {
            dragIconRoot.anchoredPosition = localPos;
        }
    }

    private bool TryGetSlotData(SlotRef slot, out ItemSlotData data)
    {
        data = default;
        List<ItemSlotData> list = GetDataList(slot.kind);
        if (slot.index < 0 || slot.index >= list.Count)
        {
            return false;
        }

        data = list[slot.index];
        return true;
    }

    private void SetSlotData(SlotRef slot, ItemSlotData data)
    {
        List<ItemSlotData> list = GetDataList(slot.kind);
        if (slot.index < 0 || slot.index >= list.Count)
        {
            return;
        }

        list[slot.index] = data;
    }

    private void SwapSlotData(SlotRef a, SlotRef b)
    {
        if (!TryGetSlotData(a, out ItemSlotData aData) || !TryGetSlotData(b, out ItemSlotData bData))
        {
            return;
        }

        SetSlotData(a, bData);
        SetSlotData(b, aData);
    }

    private List<ItemSlotData> GetDataList(SlotKind kind)
    {
        if (kind == SlotKind.Warehouse)
        {
            return warehouseData;
        }

        if (kind == SlotKind.Backpack)
        {
            return backpackData;
        }

        return equipmentData;
    }

    private SlotWidget GetWidget(SlotRef slot)
    {
        List<SlotWidget> list = GetWidgetList(slot.kind);
        if (slot.index < 0 || slot.index >= list.Count)
        {
            return null;
        }

        return list[slot.index];
    }

    private List<SlotWidget> GetWidgetList(SlotKind kind)
    {
        if (kind == SlotKind.Warehouse)
        {
            return warehouseSlots;
        }

        if (kind == SlotKind.Backpack)
        {
            return backpackSlots;
        }

        return equipmentSlots;
    }

    private void RefreshByRef(SlotRef slot)
    {
        if (slot.kind == SlotKind.Warehouse)
        {
            RefreshWarehouseSlot(slot.index);
            return;
        }

        if (slot.kind == SlotKind.Backpack)
        {
            RefreshBackpackSlot(slot.index);
            RefreshQuickSlot(slot.index);
            return;
        }

        RefreshEquipmentSlot(slot.index);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < warehouseSlots.Count; i++)
        {
            RefreshWarehouseSlot(i);
        }

        for (int i = 0; i < backpackSlots.Count; i++)
        {
            RefreshBackpackSlot(i);
        }

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            RefreshEquipmentSlot(i);
        }

        int mirrorCount = Mathf.Min(quickSlots.Count, backpackData.Count);
        for (int i = 0; i < mirrorCount; i++)
        {
            RefreshQuickSlot(i);
        }
    }

    private void RefreshWarehouseSlot(int index)
    {
        if (index < 0 || index >= warehouseSlots.Count || index >= warehouseData.Count)
        {
            return;
        }

        ApplyItemToWidget(warehouseSlots[index], warehouseData[index]);
    }

    private void RefreshBackpackSlot(int index)
    {
        if (index < 0 || index >= backpackSlots.Count || index >= backpackData.Count)
        {
            return;
        }

        ApplyItemToWidget(backpackSlots[index], backpackData[index]);
    }

    private void RefreshEquipmentSlot(int index)
    {
        if (index < 0 || index >= equipmentSlots.Count || index >= equipmentData.Count)
        {
            return;
        }

        ApplyItemToWidget(equipmentSlots[index], equipmentData[index]);
    }

    private void RefreshQuickSlot(int index)
    {
        if (index < 0 || index >= quickSlots.Count || index >= backpackData.Count)
        {
            return;
        }

        ApplyItemToWidget(quickSlots[index], backpackData[index]);
    }

    private static void ApplyItemToWidget(SlotWidget widget, ItemSlotData data)
    {
        if (widget == null || widget.icon == null)
        {
            return;
        }

        bool hasItem = data.icon != null;
        if (widget.button != null)
        {
            ColorBlock colors = widget.button.colors;
            colors.disabledColor = colors.normalColor;
            widget.button.colors = colors;
            widget.button.interactable = hasItem;
        }

        if (widget.iconIsRoot)
        {
            widget.icon.sprite = hasItem ? data.icon : widget.iconOriginalSprite;
            Color c = widget.iconOriginalColor;
            c.a = widget.iconOriginalColor.a;
            widget.icon.color = c;
            return;
        }

        widget.icon.sprite = data.icon;
        widget.icon.gameObject.SetActive(hasItem);
    }

    private void UnbindAll()
    {
        HandleEndDrag();
        warehouseSlots.Clear();
        backpackSlots.Clear();
        equipmentSlots.Clear();
        quickSlots.Clear();
    }
}




