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
        public RectTransform backgroundAnchor;
        public RectTransform iconAnchor;
        public Image icon;
        public bool iconIsRoot;
        public Color iconOriginalColor;
        public Sprite iconOriginalSprite;
        public GameObject runtimeBackgroundVisual;
        public GameObject runtimeIconVisual;
        public ItemDatabase.EquipmentSlotType equipmentSlotType;
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

    private const string WarehouseContainerPath = "Canvas/UI控制器/目录/仓库页面/仓库面板/格子区域/格子容器";
    private const string BackpackContainerPath = "Canvas/UI控制器/目录/仓库页面/背包面板/格子区域/格子容器";
    private const string EquipmentContainerPath = "Canvas/UI控制器/目录/角色页面/装备栏位";
    private const string QuickAnchorPath = "Canvas/UI控制器/目录/角色页面/右边栏位/格子区域";
    private const string QuickContainerPath = "Canvas/UI控制器/目录/角色页面/右边栏位/格子区域/格子容器";
    private const string BattleBackpackContainerPath = "Canvas/下方栏位/背包/背包内容/格子区域";
    private const string BattleBackpackMirrorContainerPath = "Canvas/下方栏位/背包/背包内容/格子区域/格子容器";
    private const string BattleBackpackContentPath = "Canvas/下方栏位/背包/背包内容";
    private const string BattleBackpackDragHandlePath = "Canvas/下方栏位/背包/背包内容/背包背景板";
    private const string SlotNameKeyword = "格子";
    private const string SlotContainerName = "格子容器";
    private const string ItemBackgroundName = "物品底背景";
    private const string ItemIconName = "物品图标";
    private const string QualityBackgroundRootPath = "Canvas/UI控制器/物品底";

    private static readonly string[] EquipmentSlotNames =
    {
        "主手",
        "副手",
        "主副手",
        "头盔",
        "胸甲",
        "手套",
        "鞋子",
        "腿甲",
        "饰品"
    };

    private static InventoryShortcutRuntimeBinder instance;

    private readonly List<ItemSlotData> warehouseData = new List<ItemSlotData>();
    private readonly List<ItemSlotData> backpackData = new List<ItemSlotData>();
    private readonly Dictionary<string, List<ItemSlotData>> equipmentDataByCharacter = new Dictionary<string, List<ItemSlotData>>(StringComparer.Ordinal);
    private readonly Dictionary<ItemDatabase.ItemQuality, GameObject> qualityBackgroundPrefabCache = new Dictionary<ItemDatabase.ItemQuality, GameObject>();

    private readonly List<SlotWidget> warehouseSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> backpackSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> equipmentSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> quickSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> battleBackpackSlots = new List<SlotWidget>();

    private bool isDragging;
    private SlotRef draggingSource;
    private SlotWidget draggingSourceWidget;
    private Canvas dragCanvas;
    private RectTransform dragIconRoot;
    private Image dragIconImage;
    private GameObject backpackSlotTemplate;
    private bool hasCachedBackpackLayout;
    private Vector2 cachedBackpackContainerSize;
    private RectOffset cachedBackpackPadding = new RectOffset();
    private Vector2 cachedBackpackCellSize;
    private Vector2 cachedBackpackSpacing;
    private GridLayoutGroup.Corner cachedBackpackStartCorner;
    private GridLayoutGroup.Axis cachedBackpackStartAxis;
    private TextAnchor cachedBackpackChildAlignment;
    private GridLayoutGroup.Constraint cachedBackpackConstraint;
    private int cachedBackpackConstraintCount;
    private string currentEquipmentCharacterId = string.Empty;
    private int equipmentSkillRevision;
    private JourneySceneBindings journeyBindings;
    private BattleSceneBindings battleBindings;

    public static int BackpackSlotCount => instance != null ? instance.backpackData.Count : 0;
    public static int WarehouseSlotCount => BackpackSlotCount;
    public static int EquipmentSlotCount => instance != null ? instance.equipmentSlots.Count : 0;
    public static int EquipmentSkillRevision => instance != null ? instance.equipmentSkillRevision : 0;

    public static string CurrentEquipmentCharacterId => instance != null ? instance.currentEquipmentCharacterId : string.Empty;

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

    public static bool TryGetBackpackSlotData(int index, out ItemSlotData data)
    {
        data = default;
        if (instance == null || index < 0 || index >= instance.backpackData.Count)
        {
            return false;
        }

        data = instance.backpackData[index];
        return true;
    }

    public static bool TryGetWarehouseSlotData(int index, out ItemSlotData data)
    {
        return TryGetBackpackSlotData(index, out data);
    }

    public static bool TrySetBackpackSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.backpackData.Count)
        {
            return false;
        }

        instance.backpackData[index] = data;
        instance.RefreshBackpackSlot(index);
        instance.RefreshQuickSlot(index);
        instance.RefreshBattleBackpackSlot(index);
        return true;
    }

    public static bool TrySetWarehouseSlotData(int index, ItemSlotData data)
    {
        return TrySetBackpackSlotData(index, data);
    }

    public static bool TryGetEquipmentSlotData(string characterId, int index, out ItemSlotData data)
    {
        data = default;
        if (instance == null)
        {
            return false;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        if (equipment == null || index < 0 || index >= equipment.Count)
        {
            return false;
        }

        data = equipment[index];
        return true;
    }

    public static bool TrySetEquipmentSlotData(string characterId, int index, ItemSlotData data)
    {
        if (instance == null || index < 0)
        {
            return false;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: true);
        EnsureDataSize(equipment, Mathf.Max(instance.equipmentSlots.Count, index + 1));
        if (index >= equipment.Count)
        {
            return false;
        }

        equipment[index] = data;
        instance.MarkEquipmentSkillsDirty();
        if (string.Equals(instance.currentEquipmentCharacterId, characterId, StringComparison.Ordinal))
        {
            instance.RefreshEquipmentSlot(index);
        }

        return true;
    }

    public static List<string> GetGrantedSkillIdsForCharacter(string characterId)
    {
        if (instance == null)
        {
            return new List<string>();
        }

        return instance.BuildGrantedSkillList(characterId);
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
            instance.RefreshBattleBackpackSlot(i);
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
            instance.RefreshBattleBackpackSlot(i);
        }

        return remain;
    }

    public static int AddItem(ItemDatabase.ItemEntry itemEntry, int count, int maxStack = 99)
    {
        if (itemEntry == null)
        {
            return count;
        }

        Sprite icon = ResolveDisplaySpriteFromPrefab(itemEntry.prefab);
        return AddItem(itemEntry.itemId, icon, count, maxStack);
    }

    private static Sprite ResolveDisplaySpriteFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        Image image = ResolveDisplayImage(prefab.transform);
        return image != null ? image.sprite : null;
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
        instance.RefreshBattleBackpackSlot(slotIndex);
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
                instance.RefreshBattleBackpackSlot(fromSlot);
                instance.RefreshBattleBackpackSlot(toSlot);
                return true;
            }
        }

        instance.backpackData[fromSlot] = to;
        instance.backpackData[toSlot] = from;
        instance.RefreshBackpackSlot(fromSlot);
        instance.RefreshBackpackSlot(toSlot);
        instance.RefreshQuickSlot(fromSlot);
        instance.RefreshQuickSlot(toSlot);
        instance.RefreshBattleBackpackSlot(fromSlot);
        instance.RefreshBattleBackpackSlot(toSlot);
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

    private void Update()
    {
        string targetCharacterId = ResolveEquipmentCharacterId();
        if (string.Equals(currentEquipmentCharacterId, targetCharacterId, StringComparison.Ordinal))
        {
            return;
        }

        SetCurrentEquipmentCharacter(targetCharacterId);
    }

    private void BindScene()
    {
        journeyBindings = JourneySceneBindings.FindInActiveScene();
        battleBindings = BattleSceneBindings.FindInActiveScene();
        CacheQualityBackgroundPrefabs();
        CacheBackpackSlotTemplate();
        UnbindAll();

        CollectWarehouseSlots();
        CollectBackpackSlots();
        CollectEquipmentSlots();
        CollectBattleBackpackSlots();

        EnsureDataSize(warehouseData, warehouseSlots.Count);
        SetCurrentEquipmentCharacter(ResolveEquipmentCharacterId());

        int backpackWidgetCount = Mathf.Max(backpackSlots.Count, quickSlots.Count);
        backpackWidgetCount = Mathf.Max(backpackWidgetCount, battleBackpackSlots.Count);
        EnsureBackpackDataSize(backpackWidgetCount);

        RectTransform quickAnchor = FindQuickAnchor();
        RectTransform quickContainer = ResolveMirrorContainer(quickAnchor, QuickContainerPath);
        if (quickContainer != null)
        {
            ApplyBackpackLayoutToMirrorAnchor(quickContainer);
            EnsureMirrorSlots(quickContainer, quickSlots, "快捷格子");
            CollectSlotsFromContainer(quickContainer, quickSlots);
            backpackWidgetCount = Mathf.Max(backpackWidgetCount, quickSlots.Count);
            EnsureBackpackDataSize(backpackWidgetCount);
        }

        RectTransform battleBackpackAnchor = FindBattleBackpackAnchor();
        RectTransform battleContainer = ResolveMirrorContainer(battleBackpackAnchor, BattleBackpackMirrorContainerPath);
        if (battleContainer != null)
        {
            ApplyBackpackLayoutToMirrorAnchor(battleContainer);
            EnsureMirrorSlots(battleContainer, battleBackpackSlots, "战斗背包格子");
            CollectSlotsFromContainer(battleContainer, battleBackpackSlots);
            backpackWidgetCount = Mathf.Max(backpackWidgetCount, battleBackpackSlots.Count);
            EnsureBackpackDataSize(backpackWidgetCount);
        }

        EnsureBattleBackpackDrag();

        BindDragRelays();
        RefreshAll();
    }

    private void CollectWarehouseSlots()
    {
        warehouseSlots.Clear();
        Transform container = journeyBindings != null && journeyBindings.warehouseContainer != null
            ? journeyBindings.warehouseContainer
            : FindTransformByPath(WarehouseContainerPath);
        if (container != null)
        {
            CollectSlotsFromContainer(container, warehouseSlots);
        }
    }

    private void CollectBackpackSlots()
    {
        backpackSlots.Clear();
        Transform container = journeyBindings != null && journeyBindings.backpackContainer != null
            ? journeyBindings.backpackContainer
            : FindTransformByPath(BackpackContainerPath);
        if (container != null)
        {
            CollectSlotsFromContainer(container, backpackSlots);
        }
    }

    private void CacheBackpackSlotTemplate()
    {
        if (backpackSlots.Count > 0 && backpackSlots[0].root != null)
        {
            StoreBackpackTemplate(backpackSlots[0].root.gameObject);
            return;
        }

        Transform container = journeyBindings != null && journeyBindings.backpackContainer != null
            ? journeyBindings.backpackContainer
            : FindTransformByPath(BackpackContainerPath);
        if (container == null || container.childCount == 0)
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

            if (!child.name.Contains(SlotNameKeyword) && child.GetComponent<Button>() == null)
            {
                continue;
            }

            StoreBackpackTemplate(child.gameObject);
            return;
        }
    }

    private void StoreBackpackTemplate(GameObject source)
    {
        if (source == null)
        {
            return;
        }

        if (backpackSlotTemplate != null)
        {
            Destroy(backpackSlotTemplate);
        }

        backpackSlotTemplate = Instantiate(source);
        backpackSlotTemplate.name = "BackpackSlotTemplate";
        backpackSlotTemplate.SetActive(false);
        DontDestroyOnLoad(backpackSlotTemplate);
        CacheBackpackLayout(source.transform.parent as RectTransform);
    }

    private void CacheBackpackLayout(RectTransform sourceContainer)
    {
        if (sourceContainer == null)
        {
            return;
        }

        cachedBackpackContainerSize = sourceContainer.sizeDelta;
        GridLayoutGroup source = sourceContainer.GetComponent<GridLayoutGroup>();
        if (source == null)
        {
            return;
        }

        hasCachedBackpackLayout = true;
        cachedBackpackPadding = new RectOffset(source.padding.left, source.padding.right, source.padding.top, source.padding.bottom);
        cachedBackpackCellSize = source.cellSize;
        cachedBackpackSpacing = source.spacing;
        cachedBackpackStartCorner = source.startCorner;
        cachedBackpackStartAxis = source.startAxis;
        cachedBackpackChildAlignment = source.childAlignment;
        cachedBackpackConstraint = source.constraint;
        cachedBackpackConstraintCount = source.constraintCount;
    }

    private void CollectEquipmentSlots()
    {
        equipmentSlots.Clear();
        Transform container = journeyBindings != null && journeyBindings.equipmentContainer != null
            ? journeyBindings.equipmentContainer
            : FindTransformByPath(EquipmentContainerPath);
        if (container != null)
        {
            CollectEquipmentSlotsFromNamedChildren(container, equipmentSlots);
        }
    }

    private void CollectBattleBackpackSlots()
    {
        battleBackpackSlots.Clear();
        Transform container = battleBindings != null && battleBindings.battleBackpackContainer != null
            ? battleBindings.battleBackpackContainer
            : FindTransformByPath(BattleBackpackContainerPath);
        if (container != null)
        {
            CollectSlotsFromContainer(container, battleBackpackSlots);
        }
    }

    private RectTransform FindQuickAnchor()
    {
        RectTransform resolvedByPath = FindTransformByPath(QuickAnchorPath) as RectTransform;
        if (resolvedByPath != null)
        {
            return resolvedByPath;
        }

        if (journeyBindings != null && journeyBindings.quickSlotAnchor != null)
        {
            return journeyBindings.quickSlotAnchor;
        }

        return null;
    }

    private RectTransform FindBattleBackpackAnchor()
    {
        if (battleBindings != null && battleBindings.battleBackpackContainer != null)
        {
            return battleBindings.battleBackpackContainer;
        }

        return FindTransformByPath(BattleBackpackContainerPath) as RectTransform;
    }

    private static RectTransform EnsureSlotContainer(RectTransform anchor)
    {
        if (anchor == null)
        {
            return null;
        }

        if (string.Equals(anchor.name, SlotContainerName, StringComparison.Ordinal))
        {
            return anchor;
        }

        Transform existing = FindChildByName(anchor, SlotContainerName);
        if (existing is RectTransform existingRect)
        {
            return existingRect;
        }

        GameObject go = new GameObject(SlotContainerName, typeof(RectTransform));
        RectTransform container = go.GetComponent<RectTransform>();
        container.SetParent(anchor, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = Vector2.zero;
        container.localScale = Vector3.one;
        return container;
    }

    private RectTransform ResolveMirrorContainer(RectTransform anchor, string containerPath)
    {
        RectTransform existingByPath = FindTransformByPath(containerPath) as RectTransform;
        if (existingByPath != null)
        {
            return existingByPath;
        }

        return EnsureSlotContainer(anchor);
    }

    private void EnsureBattleBackpackDrag()
    {
        RectTransform dragTarget = battleBindings != null && battleBindings.battleBackpackContent != null
            ? battleBindings.battleBackpackContent
            : FindTransformByPath(BattleBackpackContentPath) as RectTransform;
        RectTransform dragHandle = battleBindings != null && battleBindings.battleBackpackDragHandle != null
            ? battleBindings.battleBackpackDragHandle
            : FindTransformByPath(BattleBackpackDragHandlePath) as RectTransform;
        if (dragTarget == null || dragHandle == null)
        {
            return;
        }

        UIDragPanel dragPanel = dragHandle.GetComponent<UIDragPanel>();
        if (dragPanel == null)
        {
            dragPanel = dragHandle.gameObject.AddComponent<UIDragPanel>();
        }

        dragPanel.SetDragTarget(dragTarget);
    }

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        Transform current = null;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, segments[0], StringComparison.Ordinal))
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            return FindTransformByAncestorChain(segments);
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return FindTransformByAncestorChain(segments);
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindTransformByAncestorChain(string[] segments)
    {
        if (segments == null || segments.Length == 0)
        {
            return null;
        }

        string targetName = segments[segments.Length - 1];
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
            {
                continue;
            }

            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                Transform candidate = transforms[j];
                if (candidate == null || !string.Equals(candidate.name, targetName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (MatchesAncestorChain(candidate, segments))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool MatchesAncestorChain(Transform candidate, string[] segments)
    {
        if (candidate == null || segments == null || segments.Length == 0)
        {
            return false;
        }

        int index = segments.Length - 2;
        Transform current = candidate.parent;

        while (index >= 0)
        {
            bool found = false;
            while (current != null)
            {
                if (string.Equals(current.name, segments[index], StringComparison.Ordinal))
                {
                    found = true;
                    current = current.parent;
                    break;
                }

                current = current.parent;
            }

            if (!found)
            {
                return false;
            }

            index--;
        }

        return true;
    }

    private static Transform FindDescendantByName(Transform parent, string targetName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, targetName, StringComparison.Ordinal))
            {
                return child;
            }

            Transform nested = FindDescendantByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void ApplyBackpackLayoutToMirrorAnchor(RectTransform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        RectTransform sourceContainer = backpackSlots.Count > 0 && backpackSlots[0].root != null
            ? backpackSlots[0].root.parent as RectTransform
            : null;
        GridLayoutGroup source = sourceContainer != null ? sourceContainer.GetComponent<GridLayoutGroup>() : null;

        if (sourceContainer == null && !hasCachedBackpackLayout)
        {
            return;
        }

        anchor.sizeDelta = sourceContainer != null ? sourceContainer.sizeDelta : cachedBackpackContainerSize;

        GridLayoutGroup target = anchor.GetComponent<GridLayoutGroup>();
        if (target == null)
        {
            target = anchor.gameObject.AddComponent<GridLayoutGroup>();
        }

        if (source != null)
        {
            target.padding = source.padding;
            target.cellSize = source.cellSize;
            target.spacing = source.spacing;
            target.startCorner = source.startCorner;
            target.startAxis = source.startAxis;
            target.childAlignment = source.childAlignment;
            target.constraint = source.constraint;
            target.constraintCount = source.constraintCount;
            CacheBackpackLayout(sourceContainer);
            return;
        }

        target.padding = new RectOffset(cachedBackpackPadding.left, cachedBackpackPadding.right, cachedBackpackPadding.top, cachedBackpackPadding.bottom);
        target.cellSize = cachedBackpackCellSize;
        target.spacing = cachedBackpackSpacing;
        target.startCorner = cachedBackpackStartCorner;
        target.startAxis = cachedBackpackStartAxis;
        target.childAlignment = cachedBackpackChildAlignment;
        target.constraint = cachedBackpackConstraint;
        target.constraintCount = cachedBackpackConstraintCount;
    }

    private void EnsureMirrorSlots(RectTransform anchor, List<SlotWidget> cache, string slotNamePrefix)
    {
        cache.Clear();
        int desiredCount = backpackSlots.Count > 0 ? backpackSlots.Count : backpackData.Count;

        for (int i = anchor.childCount - 1; i >= 0; i--)
        {
            Destroy(anchor.GetChild(i).gameObject);
        }

        if (desiredCount <= 0 || (backpackSlots.Count == 0 && backpackSlotTemplate == null))
        {
            return;
        }

        GameObject template = backpackSlots.Count > 0 && backpackSlots[0].root != null
            ? backpackSlots[0].root.gameObject
            : backpackSlotTemplate;
        if (template == null)
        {
            return;
        }

        for (int i = 0; i < desiredCount; i++)
        {
            GameObject go = Instantiate(template, anchor, false);
            go.name = slotNamePrefix + " (" + (i + 1) + ")";
            SetActiveRecursively(go, true);
            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }
        }
    }

    private static void SetActiveRecursively(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(active);
        Transform root = target.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            SetActiveRecursively(root.GetChild(i).gameObject, active);
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

            RectTransform backgroundAnchor = FindNamedRectTransform(child, ItemBackgroundName);
            RectTransform iconAnchor = FindNamedRectTransform(child, ItemIconName);
            Image icon = ResolveSlotDisplayImage(child, iconAnchor);
            if (icon == null)
            {
                continue;
            }

            target.Add(new SlotWidget
            {
                root = child,
                button = button,
                backgroundAnchor = backgroundAnchor,
                iconAnchor = iconAnchor,
                icon = icon,
                iconIsRoot = icon.transform == child,
                iconOriginalColor = icon.color,
                iconOriginalSprite = icon.sprite
            });
        }
    }

    private static void CollectEquipmentSlotsFromNamedChildren(Transform container, List<SlotWidget> target)
    {
        target.Clear();
        if (container == null)
        {
            return;
        }

        for (int i = 0; i < EquipmentSlotNames.Length; i++)
        {
            string slotName = EquipmentSlotNames[i];
            Transform slotTransform = FindEquipmentSlotTransform(container, slotName);
            RectTransform slotRoot = slotTransform as RectTransform;
            if (slotRoot == null)
            {
                continue;
            }

            Button button = slotRoot.GetComponent<Button>();
            RectTransform backgroundAnchor = FindNamedRectTransform(slotRoot, ItemBackgroundName);
            RectTransform iconAnchor = FindNamedRectTransform(slotRoot, ItemIconName);
            Image icon = ResolveSlotDisplayImage(slotRoot, iconAnchor) ?? FindEquipmentIconImage(slotRoot);
            if (icon == null)
            {
                continue;
            }

            target.Add(new SlotWidget
            {
                root = slotRoot,
                button = button,
                backgroundAnchor = backgroundAnchor,
                iconAnchor = iconAnchor,
                icon = icon,
                iconIsRoot = icon.transform == slotRoot,
                iconOriginalColor = icon.color,
                iconOriginalSprite = icon.sprite,
                equipmentSlotType = ResolveEquipmentSlotType(EquipmentSlotNames[i])
            });
        }
    }

    private static Transform FindEquipmentSlotTransform(Transform container, string slotName)
    {
        if (container == null || string.IsNullOrWhiteSpace(slotName))
        {
            return null;
        }

        string slotNameWithSuffix = slotName + "栏位";

        return FindChildByName(container, slotName) ??
            FindChildByName(container, slotNameWithSuffix) ??
            FindDescendantByName(container, slotName) ??
            FindDescendantByName(container, slotNameWithSuffix);
    }

    private static ItemDatabase.EquipmentSlotType ResolveEquipmentSlotType(string slotName)
    {
        switch (slotName)
        {
            case "主手":
                return ItemDatabase.EquipmentSlotType.MainHand;
            case "副手":
                return ItemDatabase.EquipmentSlotType.OffHand;
            case "主副手":
                return ItemDatabase.EquipmentSlotType.MainOrOffHand;
            case "头盔":
                return ItemDatabase.EquipmentSlotType.Helmet;
            case "胸甲":
                return ItemDatabase.EquipmentSlotType.Armor;
            case "手套":
                return ItemDatabase.EquipmentSlotType.Gloves;
            case "鞋子":
                return ItemDatabase.EquipmentSlotType.Shoes;
            case "腿甲":
                return ItemDatabase.EquipmentSlotType.LegArmor;
            case "饰品":
                return ItemDatabase.EquipmentSlotType.Accessory;
            default:
                return ItemDatabase.EquipmentSlotType.None;
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

    private static Image FindEquipmentIconImage(RectTransform slotRoot)
    {
        Transform explicitIcon = FindChildByName(slotRoot, ItemIconName) ?? FindDescendantByName(slotRoot, ItemIconName);
        if (explicitIcon != null)
        {
            Image explicitImage = explicitIcon.GetComponent<Image>();
            if (explicitImage != null)
            {
                return explicitImage;
            }
        }

        return FindBestIconImage(slotRoot);
    }

    private static Image ResolveSlotDisplayImage(RectTransform slotRoot, RectTransform iconAnchor)
    {
        if (iconAnchor != null)
        {
            Image anchorImage = iconAnchor.GetComponent<Image>();
            if (anchorImage != null)
            {
                return anchorImage;
            }
        }

        Image rootImage = slotRoot != null ? slotRoot.GetComponent<Image>() : null;
        if (rootImage != null)
        {
            return rootImage;
        }

        return slotRoot != null ? FindBestIconImage(slotRoot) : null;
    }

    private static RectTransform FindNamedRectTransform(RectTransform root, string childName)
    {
        Transform target = FindChildByName(root, childName) ?? FindDescendantByName(root, childName);
        return target as RectTransform;
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

    private string ResolveEquipmentCharacterId()
    {
        string characterId = CharacterSelectionState.ActiveCharacterId;
        if (!string.IsNullOrWhiteSpace(characterId))
        {
            return characterId;
        }

        if (!string.IsNullOrWhiteSpace(currentEquipmentCharacterId))
        {
            return currentEquipmentCharacterId;
        }

        return "玩家";
    }

    private List<ItemSlotData> GetEquipmentDataForCharacter(string characterId, bool createIfMissing)
    {
        string resolvedCharacterId = string.IsNullOrWhiteSpace(characterId) ? "玩家" : characterId;
        List<ItemSlotData> data;
        if (equipmentDataByCharacter.TryGetValue(resolvedCharacterId, out data))
        {
            return data;
        }

        if (!createIfMissing)
        {
            return null;
        }

        data = new List<ItemSlotData>();
        EnsureDataSize(data, equipmentSlots.Count);
        equipmentDataByCharacter[resolvedCharacterId] = data;
        return data;
    }

    private List<ItemSlotData> GetCurrentEquipmentData(bool createIfMissing)
    {
        return GetEquipmentDataForCharacter(currentEquipmentCharacterId, createIfMissing);
    }

    private void SetCurrentEquipmentCharacter(string characterId)
    {
        currentEquipmentCharacterId = string.IsNullOrWhiteSpace(characterId) ? "玩家" : characterId;
        EnsureDataSize(GetCurrentEquipmentData(true), equipmentSlots.Count);
        RefreshEquipmentSlots();
    }

    private void EnsureBackpackDataSize(int size)
    {
        while (backpackData.Count < size)
        {
            backpackData.Add(default);
        }
    }

    private void BindDragRelays()
    {
        BindDragRelaysForList(warehouseSlots, SlotKind.Warehouse);
        BindDragRelaysForList(backpackSlots, SlotKind.Backpack);
        BindDragRelaysForList(quickSlots, SlotKind.Backpack);
        BindDragRelaysForList(battleBackpackSlots, SlotKind.Backpack);
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

        SlotWidget widget = ResolveDraggedWidget(source, eventData);
        if (widget == null || widget.root == null)
        {
            return;
        }

        EnsureDragVisual(widget.root);
        if (dragIconRoot == null)
        {
            return;
        }

        dragIconRoot.sizeDelta = widget.root.rect.size;
        RebuildDragVisual(widget, data);
        dragIconRoot.gameObject.SetActive(true);
        dragIconRoot.SetAsLastSibling();
        UpdateDragVisualPosition(eventData);

        draggingSource = source;
        draggingSourceWidget = widget;
        SetWidgetDraggingVisible(widget, false);
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

        if (!CanSwapSlots(draggingSource, target))
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

        if (dragIconImage != null)
        {
            dragIconImage.sprite = null;
            dragIconImage.enabled = false;
        }
        SetWidgetDraggingVisible(draggingSourceWidget, true);
        draggingSourceWidget = null;
        RefreshByRef(draggingSource);
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
        dragIconImage.preserveAspect = true;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        go.SetActive(false);
    }

    private void RebuildDragVisual(SlotWidget sourceWidget, ItemSlotData data)
    {
        if (dragIconRoot == null || dragIconImage == null)
        {
            return;
        }

        Sprite dragSprite = ResolveRuntimeIconSprite(sourceWidget) ?? ResolveDisplaySprite(data);

        dragIconImage.sprite = dragSprite;
        dragIconImage.color = new Color(1f, 1f, 1f, 0.9f);
        dragIconImage.enabled = dragIconImage.sprite != null;
        dragIconImage.SetNativeSize();
    }

    private static void SetWidgetDraggingVisible(SlotWidget widget, bool visible)
    {
        if (widget == null)
        {
            return;
        }

        SetAnchorVisualVisible(widget.backgroundAnchor, widget.root, visible);
        SetAnchorVisualVisible(widget.iconAnchor, widget.root, visible);

        if (widget.runtimeIconVisual != null)
        {
            widget.runtimeIconVisual.SetActive(visible);
        }

        if (widget.runtimeBackgroundVisual != null)
        {
            widget.runtimeBackgroundVisual.SetActive(visible);
        }

        if (widget.icon == null)
        {
            return;
        }

        bool hasRuntimeVisual = widget.runtimeBackgroundVisual != null || widget.runtimeIconVisual != null;
        if (widget.iconIsRoot && hasRuntimeVisual)
        {
            Color rootColor = widget.icon.color;
            rootColor.a = widget.iconOriginalColor.a;
            widget.icon.color = rootColor;
            return;
        }

        Color color = widget.icon.color;
        if (visible)
        {
            color.a = widget.iconIsRoot ? widget.iconOriginalColor.a : (widget.icon.sprite != null ? 1f : 0f);
        }
        else
        {
            color.a = 0f;
        }

        widget.icon.color = color;
    }

    private static void SetAnchorVisualVisible(RectTransform anchor, RectTransform root, bool visible)
    {
        if (anchor == null || anchor == root)
        {
            return;
        }

        anchor.gameObject.SetActive(visible);
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
        if (slot.kind == SlotKind.Equipment)
        {
            MarkEquipmentSkillsDirty();
        }
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

    private bool CanSwapSlots(SlotRef source, SlotRef target)
    {
        if (!TryGetSlotData(source, out ItemSlotData sourceData) || !TryGetSlotData(target, out ItemSlotData targetData))
        {
            return false;
        }

        if (!CanPlaceIntoTarget(sourceData, target))
        {
            return false;
        }

        if (!CanPlaceIntoTarget(targetData, source))
        {
            return false;
        }

        return true;
    }

    private bool CanPlaceIntoTarget(ItemSlotData data, SlotRef target)
    {
        if (target.kind != SlotKind.Equipment || data.IsEmpty)
        {
            return true;
        }

        SlotWidget widget = GetWidget(target);
        if (widget == null)
        {
            return false;
        }

        ItemDatabase.ItemEntry entry = ResolveItemEntry(data.itemId);
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        return IsEquipmentSlotCompatible(entry.equipmentSlot, widget.equipmentSlotType);
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

        return GetCurrentEquipmentData(true);
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

    private SlotWidget ResolveDraggedWidget(SlotRef slot, PointerEventData eventData)
    {
        Transform pointerTransform = eventData != null
            ? (eventData.pointerDrag != null ? eventData.pointerDrag.transform : eventData.pointerPressRaycast.gameObject != null ? eventData.pointerPressRaycast.gameObject.transform : null)
            : null;

        if (pointerTransform != null)
        {
            SlotWidget matched = FindWidgetByTransform(slot.kind, pointerTransform);
            if (matched != null)
            {
                return matched;
            }
        }

        return GetWidget(slot);
    }

    private SlotWidget FindWidgetByTransform(SlotKind kind, Transform target)
    {
        if (target == null)
        {
            return null;
        }

        if (kind == SlotKind.Backpack)
        {
            SlotWidget matched = FindWidgetByTransform(backpackSlots, target);
            if (matched != null)
            {
                return matched;
            }

            matched = FindWidgetByTransform(quickSlots, target);
            if (matched != null)
            {
                return matched;
            }

            return FindWidgetByTransform(battleBackpackSlots, target);
        }

        if (kind == SlotKind.Warehouse)
        {
            return FindWidgetByTransform(warehouseSlots, target);
        }

        return FindWidgetByTransform(equipmentSlots, target);
    }

    private static SlotWidget FindWidgetByTransform(List<SlotWidget> widgets, Transform target)
    {
        if (widgets == null || target == null)
        {
            return null;
        }

        for (int i = 0; i < widgets.Count; i++)
        {
            SlotWidget widget = widgets[i];
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

    private List<SlotWidget> GetWidgetList(SlotKind kind)
    {
        if (kind == SlotKind.Warehouse)
        {
            return warehouseSlots;
        }

        if (kind == SlotKind.Backpack)
        {
            if (backpackSlots.Count > 0)
            {
                return backpackSlots;
            }

            if (quickSlots.Count > 0)
            {
                return quickSlots;
            }

            return battleBackpackSlots;
        }

        return equipmentSlots;
    }

    private void MarkEquipmentSkillsDirty()
    {
        equipmentSkillRevision++;
    }

    private List<string> BuildGrantedSkillList(string characterId)
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<ItemSlotData> equipment = GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        if (equipment == null)
        {
            return result;
        }

        for (int i = 0; i < equipment.Count; i++)
        {
            ItemSlotData slot = equipment[i];
            if (string.IsNullOrWhiteSpace(slot.itemId))
            {
                continue;
            }

            ItemDatabase.ItemEntry itemEntry = ResolveItemEntry(slot.itemId);
            if (itemEntry == null || itemEntry.grantedSkillIds == null)
            {
                continue;
            }

            for (int s = 0; s < itemEntry.grantedSkillIds.Count; s++)
            {
                string skillId = itemEntry.grantedSkillIds[s];
                if (string.IsNullOrWhiteSpace(skillId) || !seen.Add(skillId))
                {
                    continue;
                }

                result.Add(skillId);
            }
        }

        return result;
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
            RefreshBattleBackpackSlot(slot.index);
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

        RefreshEquipmentSlots();

        int mirrorCount = Mathf.Min(quickSlots.Count, backpackData.Count);
        for (int i = 0; i < mirrorCount; i++)
        {
            RefreshQuickSlot(i);
        }

        int battleMirrorCount = Mathf.Min(battleBackpackSlots.Count, backpackData.Count);
        for (int i = 0; i < battleMirrorCount; i++)
        {
            RefreshBattleBackpackSlot(i);
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
        List<ItemSlotData> equipmentData = GetCurrentEquipmentData(true);
        if (index < 0 || index >= equipmentSlots.Count || index >= equipmentData.Count)
        {
            return;
        }

        ApplyItemToWidget(equipmentSlots[index], equipmentData[index]);
    }

    private void RefreshEquipmentSlots()
    {
        List<ItemSlotData> equipmentData = GetCurrentEquipmentData(true);
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            if (i >= equipmentData.Count)
            {
                break;
            }

            ApplyItemToWidget(equipmentSlots[i], equipmentData[i]);
        }
    }

    private void RefreshQuickSlot(int index)
    {
        if (index < 0 || index >= quickSlots.Count || index >= backpackData.Count)
        {
            return;
        }

        ApplyItemToWidget(quickSlots[index], backpackData[index]);
    }

    private void RefreshBattleBackpackSlot(int index)
    {
        if (index < 0 || index >= battleBackpackSlots.Count || index >= backpackData.Count)
        {
            return;
        }

        ApplyItemToWidget(battleBackpackSlots[index], backpackData[index]);
    }

    private static void ApplyItemToWidget(SlotWidget widget, ItemSlotData data)
    {
        if (widget == null || widget.icon == null)
        {
            return;
        }

        bool hasItem = !string.IsNullOrWhiteSpace(data.itemId);
        if (widget.button != null)
        {
            ColorBlock colors = widget.button.colors;
            colors.disabledColor = colors.normalColor;
            widget.button.colors = colors;
            widget.button.interactable = true;
        }

        RebuildItemVisual(widget, data);
    }

    private static void RebuildItemVisual(SlotWidget widget, ItemSlotData data)
    {
        if (widget == null || widget.icon == null)
        {
            return;
        }

        ClearRuntimeVisual(widget, ref widget.runtimeBackgroundVisual);
        ClearRuntimeVisual(widget, ref widget.runtimeIconVisual);

        if (string.IsNullOrWhiteSpace(data.itemId))
        {
            return;
        }

        ItemDatabase.ItemEntry entry = ResolveItemEntry(data.itemId);
        if (entry == null || entry.prefab == null)
        {
            return;
        }

        GameObject qualityBackgroundPrefab = instance != null ? instance.ResolveQualityBackgroundPrefab(entry.quality) : null;
        widget.runtimeBackgroundVisual = TryCreateRuntimePrefabVisual(qualityBackgroundPrefab, widget.backgroundAnchor ?? widget.root);
        widget.runtimeIconVisual = TryCreateRuntimeVisual(entry.prefab.transform, ItemIconName, widget.iconAnchor ?? widget.root);
    }

    private static Sprite ResolveDisplaySprite(ItemSlotData data)
    {
        if (data.icon != null)
        {
            return data.icon;
        }

        GameObject prefab = ResolvePrefabFromItemId(data.itemId);
        if (prefab == null)
        {
            return null;
        }

        Image image = ResolveDisplayImage(prefab.transform);
        return image != null ? image.sprite : null;
    }

    private static GameObject ResolvePrefabFromItemId(string itemId)
    {
        ItemDatabase.ItemEntry entry = ResolveItemEntry(itemId);
        return entry != null ? entry.prefab : null;
    }

    private GameObject ResolveQualityBackgroundPrefab(ItemDatabase.ItemQuality quality)
    {
        if (qualityBackgroundPrefabCache.TryGetValue(quality, out GameObject prefab))
        {
            return prefab;
        }

        return null;
    }

    private static Sprite ResolveRuntimeIconSprite(SlotWidget widget)
    {
        if (widget == null)
        {
            return null;
        }

        if (widget.runtimeIconVisual != null)
        {
            Image image = widget.runtimeIconVisual.GetComponent<Image>();
            if (image != null)
            {
                return image.sprite;
            }
        }

        return widget.icon != null ? widget.icon.sprite : null;
    }

    private static GameObject TryCreateRuntimeVisual(Transform prefabRoot, string childName, RectTransform anchor)
    {
        if (prefabRoot == null || anchor == null)
        {
            return null;
        }

        Transform source = FindChildByName(prefabRoot, childName) ?? FindDescendantByName(prefabRoot, childName);
        if (source == null)
        {
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(source.gameObject, anchor, false);
        instance.name = source.gameObject.name;

        RectTransform instanceRect = instance.transform as RectTransform;
        if (instanceRect != null)
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
        if (instanceRect != null)
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

    private void CacheQualityBackgroundPrefabs()
    {
        qualityBackgroundPrefabCache.Clear();
        Transform root = FindTransformByPath(QualityBackgroundRootPath);
        if (root == null)
        {
            return;
        }

        CacheQualityBackgroundPrefab(root, ItemDatabase.ItemQuality.Common, "白", "白色物品底");
        CacheQualityBackgroundPrefab(root, ItemDatabase.ItemQuality.Excellent, "蓝", "蓝色物品底");
        CacheQualityBackgroundPrefab(root, ItemDatabase.ItemQuality.Epic, "紫", "紫色物品底");
        CacheQualityBackgroundPrefab(root, ItemDatabase.ItemQuality.Blessed, "金", "金色物品底");
    }

    private void CacheQualityBackgroundPrefab(Transform root, ItemDatabase.ItemQuality quality, params string[] candidateNames)
    {
        for (int i = 0; i < candidateNames.Length; i++)
        {
            string candidateName = candidateNames[i];
            Transform target = FindChildByName(root, candidateName) ?? FindDescendantByName(root, candidateName);
            if (target == null)
            {
                continue;
            }

            qualityBackgroundPrefabCache[quality] = target.gameObject;
            return;
        }
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

    private static void ClearRuntimeVisual(SlotWidget widget, ref GameObject visual)
    {
        if (widget == null || visual == null)
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

    private static Image ResolveDisplayImage(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform picture = FindChildByName(root, "图片") ?? FindDescendantByName(root, "图片");
        if (picture != null)
        {
            Image pictureImage = picture.GetComponent<Image>();
            if (pictureImage != null && pictureImage.sprite != null)
            {
                return pictureImage;
            }
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].sprite != null)
            {
                return images[i];
            }
        }

        return null;
    }

    private static ItemDatabase.ItemEntry ResolveItemEntry(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        ItemDatabase database = ItemDatabase.LoadDefault();
        return database != null ? database.FindEntry(itemId) : null;
    }

    private void UnbindAll()
    {
        HandleEndDrag();
        ClearRuntimeVisuals(warehouseSlots);
        ClearRuntimeVisuals(backpackSlots);
        ClearRuntimeVisuals(equipmentSlots);
        ClearRuntimeVisuals(quickSlots);
        ClearRuntimeVisuals(battleBackpackSlots);
        warehouseSlots.Clear();
        backpackSlots.Clear();
        equipmentSlots.Clear();
        quickSlots.Clear();
        battleBackpackSlots.Clear();
    }

    private static void ClearRuntimeVisuals(List<SlotWidget> widgets)
    {
        if (widgets == null)
        {
            return;
        }

        for (int i = 0; i < widgets.Count; i++)
        {
            SlotWidget widget = widgets[i];
            if (widget == null)
            {
                continue;
            }

            ClearRuntimeVisual(widget, ref widget.runtimeBackgroundVisual);
            ClearRuntimeVisual(widget, ref widget.runtimeIconVisual);
        }
    }
}









