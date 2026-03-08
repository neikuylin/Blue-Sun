using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

    private sealed class SlotWidget
    {
        public RectTransform root;
        public Button button;
        public Image icon;
        public bool iconIsRoot;
        public Color iconOriginalColor;
    }

    private const string WarehouseContainerPath = "UI控制器/目录/仓库页面/背包面板/格子区域/格子容器";
    private const string QuickAnchorPath = "UI控制器/目录/角色页面/右边栏位/格子区域";
    private const string RightBarPath = "UI控制器/目录/角色页面/右边栏位";
    private const string RightBarName = "右边栏位";
    private const string QuickAnchorName = "格子区域";
    private const string SlotNameKeyword = "格子";

    private static InventoryShortcutRuntimeBinder instance;

    private readonly List<ItemSlotData> warehouseData = new List<ItemSlotData>();
    private readonly List<SlotWidget> warehouseSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> quickSlots = new List<SlotWidget>();
    private readonly List<Action> unbindActions = new List<Action>();

    private int[] quickToWarehouseIndex = Array.Empty<int>();
    private int selectedWarehouseIndex = -1;

    public static int WarehouseSlotCount => instance != null ? instance.warehouseData.Count : 0;

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
        if (instance == null || index < 0 || index >= instance.warehouseData.Count)
        {
            return false;
        }

        data = instance.warehouseData[index];
        return true;
    }

    public static bool TrySetWarehouseSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.warehouseData.Count)
        {
            return false;
        }

        instance.warehouseData[index] = data;
        instance.RefreshWarehouseSlot(index);
        instance.RefreshQuickSlotsBySource(index);
        return true;
    }

    public static bool TrySwapWarehouseSlotData(int a, int b)
    {
        if (instance == null || a < 0 || b < 0 || a >= instance.warehouseData.Count || b >= instance.warehouseData.Count)
        {
            return false;
        }

        ItemSlotData tmp = instance.warehouseData[a];
        instance.warehouseData[a] = instance.warehouseData[b];
        instance.warehouseData[b] = tmp;

        instance.RefreshWarehouseSlot(a);
        instance.RefreshWarehouseSlot(b);
        instance.RefreshQuickSlotsBySource(a);
        instance.RefreshQuickSlotsBySource(b);
        return true;
    }

    public static int AddItem(string itemId, Sprite icon, int count, int maxStack = 99)
    {
        if (instance == null || string.IsNullOrEmpty(itemId) || icon == null || count <= 0)
        {
            return count;
        }

        int remain = count;

        for (int i = 0; i < instance.warehouseData.Count && remain > 0; i++)
        {
            ItemSlotData slot = instance.warehouseData[i];
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
            instance.warehouseData[i] = slot;
            remain -= add;
            instance.RefreshWarehouseSlot(i);
            instance.RefreshQuickSlotsBySource(i);
        }

        for (int i = 0; i < instance.warehouseData.Count && remain > 0; i++)
        {
            if (!instance.warehouseData[i].IsEmpty)
            {
                continue;
            }

            int cap = Mathf.Max(1, maxStack);
            int add = Mathf.Min(remain, cap);
            instance.warehouseData[i] = new ItemSlotData
            {
                itemId = itemId,
                icon = icon,
                count = add,
                maxStack = cap
            };
            remain -= add;
            instance.RefreshWarehouseSlot(i);
            instance.RefreshQuickSlotsBySource(i);
        }

        return remain;
    }

    public static bool RemoveItemAt(int slotIndex, int count)
    {
        if (instance == null || slotIndex < 0 || slotIndex >= instance.warehouseData.Count || count <= 0)
        {
            return false;
        }

        ItemSlotData slot = instance.warehouseData[slotIndex];
        if (slot.IsEmpty || slot.count <= 0)
        {
            return false;
        }

        slot.count -= count;
        if (slot.count <= 0)
        {
            slot = default;
        }

        instance.warehouseData[slotIndex] = slot;
        instance.RefreshWarehouseSlot(slotIndex);
        instance.RefreshQuickSlotsBySource(slotIndex);
        return true;
    }

    public static bool MoveItem(int fromSlot, int toSlot)
    {
        if (instance == null ||
            fromSlot < 0 || toSlot < 0 ||
            fromSlot >= instance.warehouseData.Count || toSlot >= instance.warehouseData.Count ||
            fromSlot == toSlot)
        {
            return false;
        }

        ItemSlotData from = instance.warehouseData[fromSlot];
        ItemSlotData to = instance.warehouseData[toSlot];
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

                instance.warehouseData[fromSlot] = from;
                instance.warehouseData[toSlot] = to;
                instance.RefreshWarehouseSlot(fromSlot);
                instance.RefreshWarehouseSlot(toSlot);
                instance.RefreshQuickSlotsBySource(fromSlot);
                instance.RefreshQuickSlotsBySource(toSlot);
                return true;
            }
        }

        instance.warehouseData[fromSlot] = to;
        instance.warehouseData[toSlot] = from;
        instance.RefreshWarehouseSlot(fromSlot);
        instance.RefreshWarehouseSlot(toSlot);
        instance.RefreshQuickSlotsBySource(fromSlot);
        instance.RefreshQuickSlotsBySource(toSlot);
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

        if (warehouseSlots.Count == 0)
        {
            return;
        }

        EnsureWarehouseDataSize(warehouseSlots.Count);
        SeedWarehouseDataFromCurrentUI();

        RectTransform quickAnchor = FindQuickAnchor();
        if (quickAnchor == null)
        {
            return;
        }

        ApplyWarehouseLayoutToQuickAnchor(quickAnchor);
        EnsureQuickSlots(quickAnchor);
        CollectSlotsFromContainer(quickAnchor, quickSlots);
        if (quickSlots.Count == 0)
        {
            return;
        }

        EnsureQuickMappingSize(quickSlots.Count, warehouseSlots.Count);
        BindSlotButtons();

        selectedWarehouseIndex = 0;
        RefreshAll();
    }

    private void CollectWarehouseSlots()
    {
        warehouseSlots.Clear();

        GameObject container = GameObject.Find(WarehouseContainerPath);
        if (container == null)
        {
            return;
        }

        CollectSlotsFromContainer(container.transform, warehouseSlots);
    }

    private RectTransform FindQuickAnchor()
    {
        GameObject anchorGo = GameObject.Find(QuickAnchorPath);
        if (anchorGo != null)
        {
            return anchorGo.transform as RectTransform;
        }

        GameObject rightBar = GameObject.Find(RightBarPath);
        if (rightBar == null)
        {
            Transform[] all = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == RightBarName)
                {
                    rightBar = all[i].gameObject;
                    break;
                }
            }
        }

        if (rightBar == null)
        {
            return null;
        }

        Transform[] children = rightBar.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == QuickAnchorName)
            {
                return children[i] as RectTransform;
            }
        }

        return null;
    }

    private void EnsureQuickSlots(RectTransform quickAnchor)
    {
        quickSlots.Clear();
        CollectSlotsFromContainer(quickAnchor, quickSlots);
        if (quickSlots.Count > 0)
        {
            return;
        }

        if (warehouseSlots.Count == 0)
        {
            return;
        }

        GameObject template = warehouseSlots[0].root != null ? warehouseSlots[0].root.gameObject : null;
        if (template == null)
        {
            return;
        }

        GridLayoutGroup grid = quickAnchor.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = quickAnchor.gameObject.AddComponent<GridLayoutGroup>();
        }

        int createCount = warehouseSlots.Count;
        for (int i = 0; i < createCount; i++)
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

    private void ApplyWarehouseLayoutToQuickAnchor(RectTransform quickAnchor)
    {
        if (quickAnchor == null || warehouseSlots.Count == 0 || warehouseSlots[0].root == null)
        {
            return;
        }

        RectTransform warehouseContainer = warehouseSlots[0].root.parent as RectTransform;
        if (warehouseContainer == null)
        {
            return;
        }

        quickAnchor.sizeDelta = warehouseContainer.sizeDelta;

        GridLayoutGroup source = warehouseContainer.GetComponent<GridLayoutGroup>();
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
                iconOriginalColor = icon.color
            });
        }
    }

    private static Image FindBestIconImage(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

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

    private void EnsureWarehouseDataSize(int size)
    {
        while (warehouseData.Count < size)
        {
            warehouseData.Add(default);
        }

        while (warehouseData.Count > size)
        {
            warehouseData.RemoveAt(warehouseData.Count - 1);
        }
    }

    private void SeedWarehouseDataFromCurrentUI()
    {
        // 调试背包默认从“空数据”开始，避免把占位图标误识别成真实物品。
        // 如果后续需要从存档恢复，请在外部调用 TrySet/AddItem 等接口写入真实数据。
    }

    private void EnsureQuickMappingSize(int quickCount, int warehouseCount)
    {
        if (quickToWarehouseIndex.Length != quickCount)
        {
            int[] next = new int[quickCount];
            for (int i = 0; i < next.Length; i++)
            {
                next[i] = i < warehouseCount ? i : -1;
            }

            int copy = Mathf.Min(quickToWarehouseIndex.Length, next.Length);
            for (int i = 0; i < copy; i++)
            {
                next[i] = quickToWarehouseIndex[i];
            }

            quickToWarehouseIndex = next;
        }

        for (int i = 0; i < quickToWarehouseIndex.Length; i++)
        {
            int mapped = quickToWarehouseIndex[i];
            if (mapped < -1 || mapped >= warehouseCount)
            {
                quickToWarehouseIndex[i] = -1;
            }
        }
    }

    private void BindSlotButtons()
    {
        for (int i = 0; i < warehouseSlots.Count; i++)
        {
            int idx = i;
            Button btn = warehouseSlots[i].button;
            if (btn == null)
            {
                continue;
            }

            UnityAction onClick = delegate
            {
                selectedWarehouseIndex = idx;
                RefreshWarehouseSelectionVisual();
            };

            btn.onClick.AddListener(onClick);
            unbindActions.Add(delegate
            {
                if (btn != null)
                {
                    btn.onClick.RemoveListener(onClick);
                }
            });
        }

        for (int i = 0; i < quickSlots.Count; i++)
        {
            int idx = i;
            Button btn = quickSlots[i].button;
            if (btn == null)
            {
                continue;
            }

            UnityAction onClick = delegate
            {
                if (selectedWarehouseIndex < 0 || selectedWarehouseIndex >= warehouseSlots.Count)
                {
                    return;
                }

                quickToWarehouseIndex[idx] = selectedWarehouseIndex;
                RefreshQuickSlot(idx);
            };

            btn.onClick.AddListener(onClick);
            unbindActions.Add(delegate
            {
                if (btn != null)
                {
                    btn.onClick.RemoveListener(onClick);
                }
            });
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < warehouseSlots.Count; i++)
        {
            RefreshWarehouseSlot(i);
        }

        for (int i = 0; i < quickSlots.Count; i++)
        {
            RefreshQuickSlot(i);
        }

        RefreshWarehouseSelectionVisual();
    }

    private void RefreshWarehouseSlot(int warehouseIndex)
    {
        if (warehouseIndex < 0 || warehouseIndex >= warehouseSlots.Count || warehouseIndex >= warehouseData.Count)
        {
            return;
        }

        ApplyItemToWidget(warehouseSlots[warehouseIndex], warehouseData[warehouseIndex]);
    }

    private void RefreshQuickSlotsBySource(int sourceWarehouseIndex)
    {
        for (int i = 0; i < quickToWarehouseIndex.Length; i++)
        {
            if (quickToWarehouseIndex[i] == sourceWarehouseIndex)
            {
                RefreshQuickSlot(i);
            }
        }
    }

    private void RefreshQuickSlot(int quickIndex)
    {
        if (quickIndex < 0 || quickIndex >= quickSlots.Count)
        {
            return;
        }

        int source = quickToWarehouseIndex[quickIndex];
        if (source < 0 || source >= warehouseData.Count)
        {
            ApplyItemToWidget(quickSlots[quickIndex], default);
            return;
        }

        ApplyItemToWidget(quickSlots[quickIndex], warehouseData[source]);
    }

    private static void ApplyItemToWidget(SlotWidget widget, ItemSlotData data)
    {
        if (widget == null || widget.icon == null)
        {
            return;
        }

        widget.icon.sprite = data.icon;

        if (widget.iconIsRoot)
        {
            Color c = widget.iconOriginalColor;
            c.a = data.icon != null ? widget.iconOriginalColor.a : 0.35f;
            widget.icon.color = c;
            return;
        }

        widget.icon.gameObject.SetActive(data.icon != null);
    }

    private void RefreshWarehouseSelectionVisual()
    {
        for (int i = 0; i < warehouseSlots.Count; i++)
        {
            SlotWidget widget = warehouseSlots[i];
            if (widget == null || widget.root == null)
            {
                continue;
            }

            Vector3 scale = i == selectedWarehouseIndex ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
            widget.root.localScale = scale;
        }
    }

    private void UnbindAll()
    {
        for (int i = 0; i < unbindActions.Count; i++)
        {
            unbindActions[i]?.Invoke();
        }

        unbindActions.Clear();
        warehouseSlots.Clear();
        quickSlots.Clear();
    }
}

