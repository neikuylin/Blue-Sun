using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryShortcutRuntimeBinder : MonoBehaviour
{
    public struct ItemSlotSnapshot
    {
        public int index;
        public string itemId;
        public int count;
        public int maxStack;
        public bool isEmpty;
    }

    [Serializable]
    public struct ItemSlotData
    {
        public string itemId;
        public Sprite icon;
        public int count;
        public int maxStack;
        public bool isRotated;
        public bool isFootprintExtension;
        public int primarySlotIndex;

        public bool IsEmpty => !isFootprintExtension && icon == null && string.IsNullOrEmpty(itemId) && count <= 0;
    }

    internal enum SlotKind
    {
        Warehouse,
        Backpack,
        Equipment
    }

    internal enum SlotSurface
    {
        Warehouse,
        WarehouseBackpack,
        Equipment
    }

    internal enum StorageRightClickTarget
    {
        Backpack,
        Warehouse,
        TargetIdEquipment
    }

    internal struct SlotRef
    {
        public SlotKind kind;
        public int index;
    }

    internal sealed class CategoryFilterBinding
    {
        public Transform panelRoot;
        public readonly List<Toggle> toggles = new List<Toggle>();
        public readonly HashSet<ItemDatabase.ItemCategory> selectedCategories = new HashSet<ItemDatabase.ItemCategory>();
    }

    internal sealed class SlotWidget
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
        public StorageRightClickTarget rightClickTarget;
    }

    internal sealed class SlotDragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private InventoryShortcutRuntimeBinder owner;
        private SlotKind kind;
        private SlotSurface surface;
        private int index;

        public void Configure(InventoryShortcutRuntimeBinder binder, SlotKind slotKind, SlotSurface slotSurface, int slotIndex)
        {
            owner = binder;
            kind = slotKind;
            surface = slotSurface;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandlePointerEnter(kind, index, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandlePointerExit(kind, index, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.HandlePointerClick(kind, surface, index, eventData);
        }
    }

    private const int FixedStorageSlotCount = 42;
    private const string SlotNameKeyword = "格子";
    private const string SlotContainerName = "格子容器";
    private const string ItemBackgroundName = "物品底背景";
    private const string ItemIconName = "物品图标";
    private const string ItemTooltipIconFadeShaderName = "UI/BottomFadeImage";
    private static readonly Vector3 ItemTooltipScale = Vector3.one;
    private static readonly Vector3 ItemTooltipIconScale = new Vector3(1.5f, 1.5f, 1f);
    private static readonly Color DisabledSlotColor = new Color32(100, 100, 100, 255);
    private static readonly string[] BackpackLevelEventIds =
    {
        "背包lv1",
        "背包lv2",
        "背包lv3",
        "背包lv4",
        "背包lv5"
    };
    private static readonly int[] BackpackLevelSlotCounts = { 14, 21, 28, 35, 42 };
    private static InventoryShortcutRuntimeBinder instance;
    private static Material itemTooltipIconFadeMaterial;
    private readonly 物品转移服务 物品转移规则 = new 物品转移服务();
    private readonly 物品提示框服务 物品提示框规则 = new 物品提示框服务();
    private readonly 物品提示框服务.State 物品提示框状态 = new 物品提示框服务.State();
    private readonly 武器模型挂载服务 武器模型挂载规则 = new 武器模型挂载服务();
    private readonly 仓储界面刷新服务 仓储界面刷新规则 = new 仓储界面刷新服务();
    private readonly 仓储界面绑定服务 仓储界面绑定规则 = new 仓储界面绑定服务();
    private readonly 仓储交互服务 仓储交互规则 = new 仓储交互服务();
    private readonly 仓储交互服务.State 仓储交互状态 = new 仓储交互服务.State();
    private readonly 物品占格服务 物品占格规则 = new 物品占格服务();

    private readonly List<ItemSlotData> warehouseData = new List<ItemSlotData>();
    private readonly List<ItemSlotData> backpackData = new List<ItemSlotData>();
    private readonly Dictionary<string, List<ItemSlotData>> equipmentDataByCharacter = new Dictionary<string, List<ItemSlotData>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ItemSlotData>> boundEnemyEquipmentDataCache = new Dictionary<string, List<ItemSlotData>>(StringComparer.Ordinal);
    private readonly Dictionary<string, GameObject> qualityBackgroundPrefabCache = new Dictionary<string, GameObject>(StringComparer.Ordinal);

    private readonly List<SlotWidget> warehouseSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> backpackSlots = new List<SlotWidget>();
    private readonly List<List<SlotWidget>> extraBackpackSlots = new List<List<SlotWidget>>();
    private readonly List<SlotWidget> equipmentSlots = new List<SlotWidget>();
    private readonly List<List<SlotWidget>> extraEquipmentSlots = new List<List<SlotWidget>>();
    private readonly List<Action> categoryFilterUnbindActions = new List<Action>();

    private bool hasCachedBackpackLayout;
    private GridLayoutGroup.Corner cachedBackpackStartCorner;
    private GridLayoutGroup.Constraint cachedBackpackConstraint;
    private int cachedBackpackConstraintCount;
    private int warehouseUsableSlotCount = -1;
    private int backpackUsableSlotCount = -1;
    private readonly Dictionary<string, int> equipmentUsableSlotCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly CategoryFilterBinding warehouseFilter = new CategoryFilterBinding();
    private readonly CategoryFilterBinding backpackFilter = new CategoryFilterBinding();
    private RectTransform itemTooltipRoot;
    private RectTransform itemTooltipDetailRoot;
    private RectTransform itemTooltipLowerBackgroundRoot;
    private RectTransform itemTooltipTextContentRoot;
    private RectTransform itemTooltipExpandHintRoot;
    private TMP_Text itemTooltipLowerContentText;
    private Image itemTooltipDetailBackgroundImage;
    private Image itemTooltipItemIconImage;
    private TMP_Text itemTooltipItemNameText;
    private TMP_Text itemTooltipQualityText;
    private TMP_Text itemTooltipWeaponCategoryText;
    private TMP_Text itemTooltipOwnerText;
    private TMP_Text itemTooltipAttackPowerText;
    private TMP_Text itemTooltipFixedDamageText;
    private TMP_Text itemTooltipAttributeMultiplierText;
    private TMP_Text itemTooltipDescriptionText;
    private TMP_Text itemTooltipGrantedSkillsText;
    private RectTransform itemTooltipGrantedSkillsIconRoot;
    private GameObject runtimeTooltipRootInstance;
    private GameObject runtimeTooltipSourcePrefab;

    public static int BackpackSlotCount => instance != null ? instance.backpackData.Count : 0;
    public static int WarehouseSlotCount => instance != null ? instance.warehouseData.Count : 0;
    public static int EquipmentSlotCount => instance != null ? instance.equipmentSlots.Count : 0;
    public static int GetWarehouseUsableSlotCount()
    {
        return instance != null ? instance.GetResolvedUsableSlotCount(SlotKind.Warehouse, null) : 0;
    }

    public static void SetWarehouseUsableSlotCount(int count)
    {
        instance?.SetUsableSlotCount(SlotKind.Warehouse, null, count);
    }

    public static int GetBackpackUsableSlotCount()
    {
        return instance != null ? instance.GetResolvedUsableSlotCount(SlotKind.Backpack, null) : 0;
    }

    public static void SetBackpackUsableSlotCount(int count)
    {
        instance?.SetUsableSlotCount(SlotKind.Backpack, null, count);
    }

    public static int GetEquipmentUsableSlotCount(string characterId)
    {
        return instance != null ? instance.GetResolvedUsableSlotCount(SlotKind.Equipment, characterId) : 0;
    }

    public static void SetEquipmentUsableSlotCount(string characterId, int count)
    {
        instance?.SetUsableSlotCount(SlotKind.Equipment, characterId, count);
    }

    public static void RefreshRuntimeWeaponModels()
    {
        instance?.RefreshAllRuntimeWeaponModelsInternal();
    }

    public static List<ItemSlotSnapshot> GetBackpackSnapshots()
    {
        return instance != null ? BuildSnapshots(instance.backpackData) : new List<ItemSlotSnapshot>();
    }

    public static List<ItemSlotSnapshot> GetWarehouseSnapshots()
    {
        return instance != null ? BuildSnapshots(instance.warehouseData) : new List<ItemSlotSnapshot>();
    }

    public static List<string> GetEquipmentCharacterIds()
    {
        if (instance == null)
        {
            return new List<string>();
        }

        List<string> result = new List<string>(instance.equipmentDataByCharacter.Keys);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public static List<ItemSlotSnapshot> GetEquipmentSnapshots(string characterId)
    {
        if (instance == null)
        {
            return new List<ItemSlotSnapshot>();
        }

        List<ItemSlotData> data = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return data != null ? BuildSnapshots(data) : new List<ItemSlotSnapshot>();
    }

    public static string GetAttackPowerDisplayTextForCharacter(string itemId, string characterId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return string.Empty;
        }

        ItemDatabase.ItemEntry entry = ResolveItemEntry(itemId);
        return BuildAttackPowerDisplayText(entry, characterId, null, out _);
    }

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
        data = default;
        if (instance == null || index < 0 || index >= instance.warehouseData.Count)
        {
            return false;
        }

        data = instance.warehouseData[index];
        return true;
    }

    public static bool TrySetBackpackSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.backpackData.Count)
        {
            return false;
        }

        instance.backpackData[index] = PrepareItemSlotDataForStorage(data, $"背包格 {index}");
        instance.RefreshBackpackSlot(index);
        instance.RefreshExtraBackpackSlots(index);
        return true;
    }

    public static bool TrySetWarehouseSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.warehouseData.Count)
        {
            return false;
        }

        instance.warehouseData[index] = PrepareItemSlotDataForStorage(data, $"仓库格 {index}");
        instance.RefreshWarehouseSlot(index);
        return true;
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
        EnsureDataSize(equipment, Mathf.Max(instance.GetExpectedEquipmentSlotCount(), index + 1));
        if (index >= equipment.Count)
        {
            return false;
        }

        equipment[index] = PrepareItemSlotDataForStorage(data, $"装备栏 {characterId}:{index}");
        instance.RebuildEquipmentFootprintOccupancy(equipment);
        界面刷新中心.请求装备变更(characterId);

        return true;
    }

    public static List<string> GetGrantedSkillIdsForCharacter(string characterId)
    {
        if (instance == null)
        {
            return new List<string>();
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.构建授予技能列表(equipment, ResolveItemEntry);
    }

    public static string GetGrantedSkillSourceItemIdForCharacter(string characterId, string skillId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(skillId))
        {
            return string.Empty;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.查找授予技能来源物品(equipment, ResolveItemEntry, skillId);
    }

    public static int AddItem(string itemId, Sprite icon, int count, int maxStack = 99)
    {
        if (instance == null || string.IsNullOrEmpty(itemId) || icon == null || count <= 0)
        {
            return count;
        }

        ItemDatabase.ItemEntry itemEntry = ResolveItemEntry(itemId);
        bool useOneByTwo = instance.IsOneByTwoItem(itemEntry);
        maxStack = ResolveMaxStack(itemId, maxStack);
        int remain = count;

        for (int i = instance.backpackData.Count - 1; i >= 0 && remain > 0 && !useOneByTwo; i--)
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
            instance.RefreshExtraBackpackSlots(i);
        }

        while (remain > 0)
        {
            int targetIndex = instance.FindFirstAvailableSlotIndex(SlotKind.Backpack, itemEntry);
            if (targetIndex < 0)
            {
                break;
            }

            int cap = Mathf.Max(1, maxStack);
            int add = Mathf.Min(remain, cap);
            ItemSlotData data = new ItemSlotData
            {
                itemId = itemId,
                icon = icon,
                count = add,
                maxStack = cap
            };
            instance.SetFootprintDataAt(SlotKind.Backpack, targetIndex, data);
            remain -= add;
            instance.RefreshFootprintSlots(SlotKind.Backpack, targetIndex, data);
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
        return AddItem(itemEntry.itemId, icon, count, ResolveMaxStack(itemEntry, maxStack));
    }

    private static List<ItemSlotSnapshot> BuildSnapshots(List<ItemSlotData> source)
    {
        List<ItemSlotSnapshot> result = new List<ItemSlotSnapshot>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            ItemSlotData slot = source[i];
            result.Add(new ItemSlotSnapshot
            {
                index = i,
                itemId = slot.itemId,
                count = slot.count,
                maxStack = slot.maxStack,
                isEmpty = slot.IsEmpty
            });
        }

        return result;
    }

    private static int ResolveMaxStack(string itemId, int fallback)
    {
        ItemDatabase.ItemEntry entry = ResolveItemEntry(itemId);
        if (entry == null)
        {
            Debug.LogWarning($"[物品数据警告] 未找到物品定义，无法确认堆叠上限：{itemId}");
        }

        return ResolveMaxStack(entry, fallback);
    }

    private static int ResolveMaxStack(ItemDatabase.ItemEntry entry, int fallback)
    {
        if (entry == null)
        {
            return Mathf.Max(1, fallback);
        }

        return entry.category == ItemDatabase.ItemCategory.Equipment ? 1 : 5;
    }

    private static ItemSlotData PrepareItemSlotDataForStorage(ItemSlotData data, string context)
    {
        if (data.isFootprintExtension)
        {
            return new ItemSlotData
            {
                isFootprintExtension = true,
                primarySlotIndex = Mathf.Max(0, data.primarySlotIndex)
            };
        }

        if (data.IsEmpty)
        {
            return default;
        }

        ValidateItemSlotData(data, context);
        data.primarySlotIndex = -1;
        return data;
    }

    private static void ValidateItemSlotData(ItemSlotData data, string context)
    {
        string area = string.IsNullOrWhiteSpace(context) ? "未知位置" : context;
        if (string.IsNullOrWhiteSpace(data.itemId))
        {
            Debug.LogWarning($"[物品数据警告] {area} 的格子数据缺少物品ID。");
        }

        ItemDatabase.ItemEntry entry = ResolveItemEntry(data.itemId);
        if (entry == null)
        {
            Debug.LogWarning($"[物品数据警告] {area} 引用了不存在的物品：{data.itemId}");
            return;
        }

        int expectedMaxStack = ResolveMaxStack(entry, data.maxStack);
        if (data.count <= 0)
        {
            Debug.LogWarning($"[物品数据警告] {area} 的物品数量不合法：{data.itemId}，当前数量 {data.count}");
        }

        if (data.maxStack != expectedMaxStack)
        {
            Debug.LogWarning($"[物品数据警告] {area} 的堆叠上限不匹配：{data.itemId}，当前 {data.maxStack}，应为 {expectedMaxStack}");
        }

        if (data.count > expectedMaxStack)
        {
            Debug.LogWarning($"[物品数据警告] {area} 的物品数量超过上限：{data.itemId}，数量 {data.count} / 上限 {expectedMaxStack}");
        }
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

        slotIndex = instance.ResolvePrimarySlotIndex(SlotKind.Backpack, slotIndex);
        ItemSlotData slot = instance.backpackData[slotIndex];
        if (slot.IsEmpty || slot.count <= 0)
        {
            return false;
        }

        slot.count -= count;
        if (slot.count <= 0)
        {
            instance.ClearFootprintAt(SlotKind.Backpack, slotIndex, slot);
            instance.RefreshFootprintSlots(SlotKind.Backpack, slotIndex, slot);
            return true;
        }

        instance.backpackData[slotIndex] = slot;
        instance.RefreshBackpackSlot(slotIndex);
        instance.RefreshExtraBackpackSlots(slotIndex);
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

        fromSlot = instance.ResolvePrimarySlotIndex(SlotKind.Backpack, fromSlot);
        toSlot = instance.ResolvePrimarySlotIndex(SlotKind.Backpack, toSlot);
        ItemSlotData from = instance.backpackData[fromSlot];
        ItemSlotData to = instance.backpackData[toSlot];
        if (from.IsEmpty || from.isFootprintExtension || to.isFootprintExtension)
        {
            return false;
        }

        return instance.TryTransferItem(
            new SlotRef { kind = SlotKind.Backpack, index = fromSlot },
            new SlotRef { kind = SlotKind.Backpack, index = toSlot },
            from);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        界面刷新中心.全部界面刷新 += OnGlobalRefreshRequested;
        界面刷新中心.当前角色切换刷新 += OnCurrentCharacterRefreshRequested;
        界面刷新中心.仓储界面刷新 += OnStorageRefreshRequested;
        界面刷新中心.装备变更 += OnEquipmentChanged;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        界面刷新中心.全部界面刷新 -= OnGlobalRefreshRequested;
        界面刷新中心.当前角色切换刷新 -= OnCurrentCharacterRefreshRequested;
        界面刷新中心.仓储界面刷新 -= OnStorageRefreshRequested;
        界面刷新中心.装备变更 -= OnEquipmentChanged;
        UnbindAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void Update()
    {
        仓储交互规则.HandleHoveredItemRotation(仓储交互状态, 创建仓储交互上下文());
        仓储交互规则.UpdatePendingTooltip(仓储交互状态, 创建仓储交互上下文());
        UpdateTooltipLowerBackgroundState();
    }

    private void BindScene()
    {
        CacheQualityBackgroundPrefabs();
        CacheItemTooltip(ItemDatabase.WeaponCategory.OneHanded, true);
        UnbindAll();
        仓储界面绑定规则.CollectStorageSlots(创建仓储界面绑定上下文());
        仓储界面绑定规则.CollectEquipmentSlots(创建仓储界面绑定上下文());

        EnsureWarehouseDataSize();
        int backpackWidgetCount = backpackSlots.Count;
        for (int i = 0; i < extraBackpackSlots.Count; i++)
        {
            backpackWidgetCount = Mathf.Max(backpackWidgetCount, extraBackpackSlots[i].Count);
        }
        EnsureBackpackDataSize(backpackWidgetCount);
        仓储界面绑定规则.BindCategoryFilters(创建仓储界面绑定上下文());
        仓储界面绑定规则.BindDragRelays(创建仓储界面绑定上下文());
    }

    private void OnGlobalRefreshRequested()
    {
        RefreshAll();
        RefreshAllRuntimeWeaponModelsInternal();
    }

    private void OnCurrentCharacterRefreshRequested(string characterId)
    {
        RefreshEquipmentSlots();
    }

    private void OnStorageRefreshRequested()
    {
        RefreshAll();
    }

    private void OnEquipmentChanged(string characterId)
    {
        RefreshAll();
        RefreshRuntimeWeaponModelForCharacter(characterId);
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

        Transform resolved = SceneHierarchyPathUtility.FindInActiveScene(path);
        return resolved != null ? resolved : FindTransformByAncestorChain(segments);
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

    private void CacheBackpackLayout(RectTransform sourceContainer)
    {
        if (sourceContainer == null)
        {
            return;
        }

        GridLayoutGroup source = sourceContainer.GetComponent<GridLayoutGroup>();
        if (source == null)
        {
            return;
        }

        hasCachedBackpackLayout = true;
        cachedBackpackStartCorner = source.startCorner;
        cachedBackpackConstraint = source.constraint;
        cachedBackpackConstraintCount = source.constraintCount;
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
        string currentId = 界面ID列表.当前ID;
        if (!string.IsNullOrWhiteSpace(currentId))
        {
            return currentId;
        }

        return string.Empty;
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
            return GetBoundEnemyEquipmentDataForCharacter(resolvedCharacterId);
        }

        List<ItemSlotData> boundEnemyEquipment = GetBoundEnemyEquipmentDataForCharacter(resolvedCharacterId);
        if (boundEnemyEquipment != null)
        {
            data = CloneItemSlotDataList(boundEnemyEquipment);
            EnsureDataSize(data, Mathf.Max(GetExpectedEquipmentSlotCount(), data.Count));
            equipmentDataByCharacter[resolvedCharacterId] = data;
            return data;
        }

        data = new List<ItemSlotData>();
        EnsureDataSize(data, GetExpectedEquipmentSlotCount());
        equipmentDataByCharacter[resolvedCharacterId] = data;
        return data;
    }

    private List<ItemSlotData> GetBoundEnemyEquipmentDataForCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        List<ItemSlotData> cachedData;
        if (boundEnemyEquipmentDataCache.TryGetValue(characterId, out cachedData))
        {
            return cachedData;
        }

        EnemyEquipmentDatabase equipmentDatabase = EnemyEquipmentDatabase.LoadDefault();
        if (equipmentDatabase == null)
        {
            return null;
        }

        EnemyEquipmentDatabase.EnemyEquipmentEntry entry = equipmentDatabase.FindEntry(characterId);
        if (entry == null || entry.itemIds == null)
        {
            return null;
        }

        int slotCount = Mathf.Max(GetExpectedEquipmentSlotCount(), EnemyEquipmentDatabase.SlotCount);
        List<ItemSlotData> result = new List<ItemSlotData>(slotCount);
        EnsureDataSize(result, slotCount);
        for (int i = 0; i < entry.itemIds.Count && i < result.Count; i++)
        {
            string itemId = entry.itemIds[i];
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            ItemDatabase.ItemEntry itemEntry = ResolveItemEntry(itemId);
            if (itemEntry == null || itemEntry.category != ItemDatabase.ItemCategory.Equipment)
            {
                continue;
            }

            result[i] = PrepareItemSlotDataForStorage(new ItemSlotData
            {
                itemId = itemId,
                icon = ResolveDisplaySpriteFromPrefab(itemEntry.prefab),
                count = 1,
                maxStack = 1
            }, $"敌人装备栏 {characterId}:{i}");
        }

        boundEnemyEquipmentDataCache[characterId] = result;
        return result;
    }

    private int GetExpectedEquipmentSlotCount()
    {
        if (equipmentSlots.Count > 0)
        {
            return equipmentSlots.Count;
        }

        for (int i = 0; i < extraEquipmentSlots.Count; i++)
        {
            List<SlotWidget> slotGroup = extraEquipmentSlots[i];
            if (slotGroup != null && slotGroup.Count > 0)
            {
                return slotGroup.Count;
            }
        }

        return 8;
    }

    private static List<ItemSlotData> CloneItemSlotDataList(List<ItemSlotData> source)
    {
        return source != null ? new List<ItemSlotData>(source) : new List<ItemSlotData>();
    }

    private List<ItemSlotData> GetCurrentEquipmentData(bool createIfMissing)
    {
        string equipmentCharacterId = ResolveEquipmentCharacterId();
        if (string.IsNullOrWhiteSpace(equipmentCharacterId))
        {
            return null;
        }

        return GetEquipmentDataForCharacter(equipmentCharacterId, createIfMissing);
    }

    private void EnsureBackpackDataSize(int size)
    {
        while (backpackData.Count < size)
        {
            backpackData.Add(default);
        }
    }

    private void EnsureWarehouseDataSize()
    {
        while (warehouseData.Count < FixedStorageSlotCount)
        {
            warehouseData.Add(default);
        }
    }

    private void HandleBeginDrag(SlotKind kind, int index, PointerEventData eventData)
    {
        仓储交互规则.HandleBeginDrag(仓储交互状态, 创建仓储交互上下文(), kind, index, eventData);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        仓储交互规则.HandleDrag(仓储交互状态, 创建仓储交互上下文(), eventData);
    }

    private void HandleDrop(SlotKind kind, int index)
    {
        仓储交互规则.HandleDrop(仓储交互状态, 创建仓储交互上下文(), kind, index);
    }

    private void HandlePointerEnter(SlotKind kind, int index, PointerEventData eventData)
    {
        仓储交互规则.HandlePointerEnter(仓储交互状态, 创建仓储交互上下文(), kind, index, eventData);
    }

    private void HandlePointerExit(SlotKind kind, int index, PointerEventData eventData)
    {
        仓储交互规则.HandlePointerExit(仓储交互状态, 创建仓储交互上下文(), kind, index, eventData);
    }

    private void HandlePointerClick(SlotKind kind, SlotSurface surface, int index, PointerEventData eventData)
    {
        仓储交互规则.HandlePointerClick(仓储交互状态, 创建仓储交互上下文(), kind, surface, index, eventData);
    }

    private bool TryHandleRightClickMove(SlotRef source, SlotSurface surface, StorageRightClickTarget target, ItemSlotData sourceData)
    {
        return 物品转移规则.TryHandleRightClickMove(创建物品转移上下文(), source, surface, target, sourceData);
    }

    private bool TryExecuteSlotTransfer(SlotRef source, SlotRef target, ItemSlotData sourceData)
    {
        return TryTransferItem(source, target, sourceData);
    }

    private int FindFirstEmptySlotIndex(SlotKind kind)
    {
        List<ItemSlotData> dataList = GetDataList(kind);
        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (IsSlotUsable(kind, i) && dataList[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstAvailableSlotIndex(SlotKind kind, ItemDatabase.ItemEntry entry)
    {
        List<ItemSlotData> dataList = GetDataList(kind);
        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (CanPlaceItemAtIndex(kind, i, entry, dataList))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstAvailableSlotIndex(SlotKind kind, ItemSlotData data)
    {
        List<ItemSlotData> dataList = GetDataList(kind);
        for (int i = dataList.Count - 1; i >= 0; i--)
        {
            if (CanPlaceDataAt(kind, i, data, dataList))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindRightClickEquipmentTargetIndex(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || equipmentSlots.Count == 0)
        {
            return -1;
        }

        ItemDatabase.EquipmentSlotType desiredSlotType = entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand
            ? ItemDatabase.EquipmentSlotType.MainHand
            : entry.equipmentSlot;

        int emptySlotIndex = FindEquipmentSlotIndex(desiredSlotType, requireEmpty: true, entry);
        return emptySlotIndex >= 0
            ? emptySlotIndex
            : FindEquipmentSlotIndex(desiredSlotType, requireEmpty: false, entry);
    }

    private int FindEquipmentSlotIndex(ItemDatabase.EquipmentSlotType slotType, bool requireEmpty, ItemDatabase.ItemEntry entry = null)
    {
        List<ItemSlotData> equipmentData = GetCurrentEquipmentData(true);
        if (equipmentData == null)
        {
            return -1;
        }

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            SlotWidget widget = equipmentSlots[i];
            if (widget == null || widget.equipmentSlotType != slotType)
            {
                continue;
            }

            if (!CanUseEquipmentSlotIndex(i, slotType, requireEmpty, entry, equipmentData))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private void HandleEndDrag()
    {
        仓储交互规则.HandleEndDrag(仓储交互状态, 创建仓储交互上下文());
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

        list[slot.index] = PrepareItemSlotDataForStorage(data, $"{slot.kind} {slot.index}");
        if (slot.kind == SlotKind.Equipment)
        {
            RebuildEquipmentFootprintOccupancy(list);
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
        source = ResolvePrimarySlotRef(source);
        target = ResolvePrimarySlotRef(target);
        if (!TryGetSlotData(source, out ItemSlotData sourceData) || !TryGetSlotData(target, out ItemSlotData targetData))
        {
            return false;
        }

        if (IsFootprintItem(sourceData) || IsFootprintItem(targetData) || sourceData.isFootprintExtension || targetData.isFootprintExtension)
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

        if (!IsEquipmentSlotCompatible(entry.equipmentSlot, widget.equipmentSlotType))
        {
            return false;
        }

        int targetIndex = ResolvePrimarySlotIndex(SlotKind.Equipment, target.index);
        return CanUseEquipmentSlotIndex(targetIndex, widget.equipmentSlotType, requireEmpty: false, entry, GetCurrentEquipmentData(true));
    }

    internal static bool IsEquipmentSlotCompatible(
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

    private ItemSlotData GetResolvedSlotData(SlotRef slot)
    {
        SlotRef resolved = ResolvePrimarySlotRef(slot);
        return TryGetSlotData(resolved, out ItemSlotData data) ? data : default;
    }

    private 物品转移服务.Context 创建物品转移上下文()
    {
        return new 物品转移服务.Context
        {
            ResolvePrimarySlotRef = ResolvePrimarySlotRef,
            GetResolvedSlotData = GetResolvedSlotData,
            TryGetSlotData = TryGetSlotData,
            GetDataList = GetDataList,
            ResolveItemEntry = ResolveItemEntry,
            IsSlotUsable = IsSlotUsable,
            IsFootprintItem = IsFootprintItem,
            IsOneByTwoItem = IsOneByTwoItem,
            CloneItemSlotDataList = CloneItemSlotDataList,
            GetOneByTwoExtensionIndex = GetOneByTwoExtensionIndex,
            PrepareItemSlotDataForStorage = PrepareItemSlotDataForStorage,
            ResolveEquipmentCharacterId = ResolveEquipmentCharacterId,
            GetEquipmentSlotCount = () => equipmentSlots.Count,
            GetEquipmentSlotTypeAt = index =>
            {
                if (index < 0 || index >= equipmentSlots.Count || equipmentSlots[index] == null)
                {
                    return ItemDatabase.EquipmentSlotType.None;
                }

                return equipmentSlots[index].equipmentSlotType;
            },
            RequestEquipmentChanged = 界面刷新中心.请求装备变更,
            RequestStorageRefresh = 界面刷新中心.请求刷新仓储界面,
            PlayItemSound = ItemSoundUtility.PlayForItem,
            RebuildEquipmentFootprintOccupancy = RebuildEquipmentFootprintOccupancy,
            GetOffHandEquipmentSlotIndex = GetOffHandEquipmentSlotIndex
        };
    }

    private 物品提示框服务.Context 创建物品提示框上下文()
    {
        return new 物品提示框服务.Context
        {
            GetWeaponTooltipPrefab = weaponCategory =>
            {
                ItemTooltipPrefabDatabase database = ItemTooltipPrefabDatabase.LoadDefault();
                return database != null ? database.GetWeaponTooltipPrefab(weaponCategory) : null;
            },
            GetQualityBackgroundPrefab = quality =>
            {
                ItemTooltipPrefabDatabase database = ItemTooltipPrefabDatabase.LoadDefault();
                return database != null ? database.GetQualityBackgroundPrefab(quality) : null;
            },
            FindTooltipParent = FindTooltipParent,
            FindChildByName = FindChildByName,
            FindDescendantByName = FindDescendantByName,
            FindTooltipTextByName = null,
            FindTransformByPath = FindTransformByPath,
            FindSkillEntry = skillId =>
            {
                BattleSkillDatabase skillDatabase = BattleSkillDatabase.LoadDefault();
                return skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
            },
            ResolveItemDisplayName = ResolveItemDisplayName,
            GetItemQualityDisplayName = GetItemQualityDisplayName,
            GetWeaponCategoryDisplayName = GetWeaponCategoryDisplayName,
            GetTooltipOwnerDisplayText = GetTooltipOwnerDisplayText,
            SetTooltipAttackPowerText = (entry, slot, text) =>
            {
                itemTooltipAttackPowerText = text;
                SetTooltipAttackPowerText(entry, slot);
            },
            GetFixedDamageDisplayText = GetFixedDamageDisplayText,
            GetAttributeMultiplierDisplayText = GetAttributeMultiplierDisplayText,
            BuildTooltipLowerContentText = BuildTooltipLowerContentText,
            ResolveTooltipItemIconSprite = ResolveTooltipItemIconSprite,
            ResolveTooltipItemIconSize = ResolveTooltipItemIconSize,
            EnsureItemTooltipIconFadeMaterial = EnsureItemTooltipIconFadeMaterial,
            CancelHover = () => HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Item),
            ShouldShowLowerBackground = () => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            FindAnyCanvas = () => FindObjectOfType<Canvas>(true),
            ItemTooltipScale = ItemTooltipScale,
            ItemTooltipIconScale = ItemTooltipIconScale
        };
    }

    private 武器模型挂载服务.Context 创建武器模型挂载上下文()
    {
        return new 武器模型挂载服务.Context
        {
            FindBattleUnits = () => FindObjectsOfType<BattleUnit>(true),
            GetEquipmentDataForCharacter = characterId => GetEquipmentDataForCharacter(characterId, createIfMissing: false),
            ResolveItemEntry = ResolveItemEntry,
            FindChildByName = FindChildByName,
            FindDescendantByName = FindDescendantByName
        };
    }

    private 仓储界面刷新服务.Context 创建仓储界面刷新上下文()
    {
        return new 仓储界面刷新服务.Context
        {
            WarehouseSlots = warehouseSlots,
            BackpackSlots = backpackSlots,
            ExtraBackpackSlots = extraBackpackSlots,
            EquipmentSlots = equipmentSlots,
            ExtraEquipmentSlots = extraEquipmentSlots,
            WarehouseData = warehouseData,
            BackpackData = backpackData,
            GetCurrentEquipmentData = () => GetCurrentEquipmentData(true),
            GetExpectedEquipmentSlotCount = GetExpectedEquipmentSlotCount,
            ResolveEquipmentCharacterId = ResolveEquipmentCharacterId,
            IsSlotUsable = IsSlotUsable,
            ShouldDisplayWarehouseItem = data => ShouldDisplayItem(warehouseFilter, data),
            ShouldDisplayBackpackItem = data => ShouldDisplayItem(backpackFilter, data),
            RefreshRuntimeWeaponModelForCharacter = RefreshRuntimeWeaponModelForCharacter,
            ResolveItemEntry = ResolveItemEntry,
            ResolveQualityBackgroundPrefab = ResolveQualityBackgroundPrefab,
            IsOneByTwoItem = IsOneByTwoItem,
            FindChildByName = FindChildByName,
            FindDescendantByName = FindDescendantByName,
            DisabledSlotColor = DisabledSlotColor
        };
    }

    private 仓储界面绑定服务.Context 创建仓储界面绑定上下文()
    {
        return new 仓储界面绑定服务.Context
        {
            Owner = this,
            FixedStorageSlotCount = FixedStorageSlotCount,
            WarehouseData = warehouseData,
            BackpackData = backpackData,
            WarehouseSlots = warehouseSlots,
            BackpackSlots = backpackSlots,
            ExtraBackpackSlots = extraBackpackSlots,
            EquipmentSlots = equipmentSlots,
            ExtraEquipmentSlots = extraEquipmentSlots,
            WarehouseFilter = warehouseFilter,
            BackpackFilter = backpackFilter,
            CategoryFilterUnbindActions = categoryFilterUnbindActions,
            CacheBackpackLayout = CacheBackpackLayout,
            RefreshWarehouseFilteredView = RefreshWarehouseFilteredView,
            RefreshBackpackFilteredView = RefreshBackpackFilteredView
        };
    }

    private 仓储交互服务.Context 创建仓储交互上下文()
    {
        return new 仓储交互服务.Context
        {
            WarehouseSlots = warehouseSlots,
            BackpackSlots = backpackSlots,
            ExtraBackpackSlots = extraBackpackSlots,
            EquipmentSlots = equipmentSlots,
            ExtraEquipmentSlots = extraEquipmentSlots,
            IsSlotUsable = IsSlotUsable,
            ResolvePrimarySlotRef = ResolvePrimarySlotRef,
            GetResolvedSlotData = GetResolvedSlotData,
            TryGetSlotData = TryGetSlotData,
            GetWidget = GetWidget,
            ResolveItemEntry = ResolveItemEntry,
            ShouldShowWeaponTooltip = ShouldShowWeaponTooltip,
            TryTransferToSlot = (target, sourceData) => TryTransferItem(仓储交互状态.draggingSource, target, sourceData),
            TryHandleRightClickMove = TryHandleRightClickMove,
            ShowItemTooltip = ShowItemTooltip,
            HideItemTooltip = HideItemTooltip,
            ResolveRuntimeIconSprite = ResolveRuntimeIconSprite,
            ResolveDisplaySprite = ResolveDisplaySprite,
            SetWidgetDraggingVisible = SetWidgetDraggingVisible,
            RefreshByRef = RefreshByRef,
            IsOneByTwoItem = IsOneByTwoItem,
            GetDataList = GetDataList,
            CloneItemSlotDataList = CloneItemSlotDataList,
            ClearPlacement = ClearPlacement,
            CanPlaceDataAt = CanPlaceDataAt,
            GetExtensionIndexForData = GetExtensionIndexForData,
            PrepareItemSlotDataForStorage = PrepareItemSlotDataForStorage,
            RefreshFootprintSlots = RefreshFootprintSlots,
            PlayItemSound = ItemSoundUtility.PlayForItem
        };
    }

    private 物品占格服务.Context 创建物品占格上下文()
    {
        return new 物品占格服务.Context
        {
            GetDataList = GetDataList,
            GetWidgetList = GetWidgetList,
            ResolveItemEntry = ResolveItemEntry,
            IsOneByTwoItem = IsOneByTwoItem,
            PrepareItemSlotDataForStorage = PrepareItemSlotDataForStorage,
            IsSlotUsable = IsSlotUsable,
            GetOffHandEquipmentSlotIndex = GetOffHandEquipmentSlotIndex,
            RebuildEquipmentFootprintOccupancy = RebuildEquipmentFootprintOccupancy,
            RefreshByRef = RefreshByRef,
            HasCachedBackpackLayout = () => hasCachedBackpackLayout,
            GetCachedBackpackStartCorner = () => cachedBackpackStartCorner,
            GetCachedBackpackConstraint = () => cachedBackpackConstraint,
            GetCachedBackpackConstraintCount = () => cachedBackpackConstraintCount
        };
    }

    private void 同步物品提示框状态()
    {
        itemTooltipRoot = 物品提示框状态.itemTooltipRoot;
        itemTooltipDetailRoot = 物品提示框状态.itemTooltipDetailRoot;
        itemTooltipLowerBackgroundRoot = 物品提示框状态.itemTooltipLowerBackgroundRoot;
        itemTooltipTextContentRoot = 物品提示框状态.itemTooltipTextContentRoot;
        itemTooltipExpandHintRoot = 物品提示框状态.itemTooltipExpandHintRoot;
        itemTooltipLowerContentText = 物品提示框状态.itemTooltipLowerContentText;
        itemTooltipDetailBackgroundImage = 物品提示框状态.itemTooltipDetailBackgroundImage;
        itemTooltipItemIconImage = 物品提示框状态.itemTooltipItemIconImage;
        itemTooltipItemNameText = 物品提示框状态.itemTooltipItemNameText;
        itemTooltipQualityText = 物品提示框状态.itemTooltipQualityText;
        itemTooltipWeaponCategoryText = 物品提示框状态.itemTooltipWeaponCategoryText;
        itemTooltipOwnerText = 物品提示框状态.itemTooltipOwnerText;
        itemTooltipAttackPowerText = 物品提示框状态.itemTooltipAttackPowerText;
        itemTooltipFixedDamageText = 物品提示框状态.itemTooltipFixedDamageText;
        itemTooltipAttributeMultiplierText = 物品提示框状态.itemTooltipAttributeMultiplierText;
        itemTooltipDescriptionText = 物品提示框状态.itemTooltipDescriptionText;
        itemTooltipGrantedSkillsText = 物品提示框状态.itemTooltipGrantedSkillsText;
        itemTooltipGrantedSkillsIconRoot = 物品提示框状态.itemTooltipGrantedSkillsIconRoot;
        runtimeTooltipRootInstance = 物品提示框状态.runtimeTooltipRootInstance;
        runtimeTooltipSourcePrefab = 物品提示框状态.runtimeTooltipSourcePrefab;
    }

    private Sprite ResolveTooltipItemIconSprite(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.prefab == null)
        {
            return null;
        }

        Transform iconRoot = FindChildByName(entry.prefab.transform, ItemIconName) ?? FindDescendantByName(entry.prefab.transform, ItemIconName);
        Image iconImage = iconRoot != null ? iconRoot.GetComponent<Image>() : null;
        return iconImage != null ? iconImage.sprite : null;
    }

    private Vector2 ResolveTooltipItemIconSize(ItemDatabase.ItemEntry entry)
    {
        if (itemTooltipItemIconImage == null)
        {
            return Vector2.zero;
        }

        Vector2 iconSize = itemTooltipItemIconImage.rectTransform.sizeDelta;
        if (entry == null || entry.prefab == null)
        {
            return iconSize;
        }

        Transform iconRoot = FindChildByName(entry.prefab.transform, ItemIconName) ?? FindDescendantByName(entry.prefab.transform, ItemIconName);
        Image iconImage = iconRoot != null ? iconRoot.GetComponent<Image>() : null;
        if (iconImage == null || iconImage.rectTransform == null)
        {
            return iconSize;
        }

        return iconImage.rectTransform.sizeDelta;
    }

    private bool TryTransferItem(SlotRef source, SlotRef target, ItemSlotData sourceData)
    {
        return 物品转移规则.TryTransferItem(创建物品转移上下文(), source, target, sourceData);
    }

    private bool CanPlaceDataAt(SlotKind kind, int index, ItemSlotData data, List<ItemSlotData> list)
    {
        return 物品占格规则.CanPlaceDataAt(创建物品占格上下文(), kind, index, data, list);
    }

    private void PlaceDataAt(SlotRef target, ItemSlotData data)
    {
        物品占格规则.PlaceDataAt(创建物品占格上下文(), target, data);
    }

    private void PlaceDataAt(SlotKind kind, int index, ItemSlotData data, List<ItemSlotData> list)
    {
        物品占格规则.PlaceDataAt(创建物品占格上下文(), kind, index, data, list);
    }

    private void ClearPlacement(SlotRef slot, ItemSlotData data)
    {
        物品占格规则.ClearPlacement(创建物品占格上下文(), slot, data);
    }

    private void ClearPlacement(SlotKind kind, int primaryIndex, ItemSlotData data, List<ItemSlotData> list)
    {
        物品占格规则.ClearPlacement(创建物品占格上下文(), kind, primaryIndex, data, list);
    }

    private int GetExtensionIndexForData(SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        return 物品占格规则.GetExtensionIndexForData(创建物品占格上下文(), kind, primaryIndex, data);
    }

    private List<int> GetFootprintCellIndices(SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        return 物品占格规则.GetFootprintCellIndices(创建物品占格上下文(), kind, primaryIndex, data);
    }

    private SlotRef ResolvePrimarySlotRef(SlotRef slot)
    {
        return 物品占格规则.ResolvePrimarySlotRef(创建物品占格上下文(), slot);
    }

    private int ResolvePrimarySlotIndex(SlotKind kind, int index)
    {
        return 物品占格规则.ResolvePrimarySlotIndex(创建物品占格上下文(), kind, index);
    }

    private bool TryMoveItemToEmptyStorageSlot(SlotRef source, SlotRef target, ItemSlotData sourceData)
    {
        List<ItemSlotData> sourceList = GetDataList(source.kind);
        List<ItemSlotData> targetList = GetDataList(target.kind);
        if (source.index < 0 || source.index >= sourceList.Count || target.index < 0 || target.index >= targetList.Count)
        {
            return false;
        }

        if (!CanPlaceDataAt(target.kind, target.index, sourceData, targetList))
        {
            return false;
        }

        ClearFootprintAt(source.kind, source.index, sourceData);
        SetFootprintDataAt(target.kind, target.index, sourceData);
        RefreshFootprintSlots(source.kind, source.index, sourceData);
        RefreshFootprintSlots(target.kind, target.index, sourceData);
        ItemSoundUtility.PlayForItem(sourceData.itemId);
        return true;
    }

    private bool TryMoveStorageItemToEquipment(SlotRef source, SlotRef target, ItemSlotData sourceData)
    {
        if (!TryGetSlotData(target, out ItemSlotData targetData))
        {
            return false;
        }

        if (!CanPlaceIntoTarget(sourceData, target))
        {
            return false;
        }

        ClearFootprintAt(source.kind, source.index, sourceData);
        if (targetData.IsEmpty)
        {
            SetSlotData(source, default);
        }
        else if (IsFootprintItem(targetData))
        {
            SetFootprintDataAt(source.kind, source.index, targetData);
        }
        else
        {
            SetSlotData(source, targetData);
        }

        sourceData.isRotated = false;
        SetSlotData(target, sourceData);
        RefreshFootprintSlots(source.kind, source.index, sourceData);
        RefreshByRef(target);
        ItemSoundUtility.PlayForItem(sourceData.itemId);
        return true;
    }

    private bool CanUseEquipmentSlotIndex(int index, ItemDatabase.EquipmentSlotType slotType, bool requireEmpty, ItemDatabase.ItemEntry entry, List<ItemSlotData> equipmentData)
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

        if (!IsOneByTwoItem(entry))
        {
            return true;
        }

        if (slotType != ItemDatabase.EquipmentSlotType.MainHand &&
            slotType != ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return false;
        }

        int offHandIndex = GetOffHandEquipmentSlotIndex();
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

    private void RebuildEquipmentFootprintOccupancy(List<ItemSlotData> equipmentData)
    {
        物品占格规则.RebuildEquipmentFootprintOccupancy(创建物品占格上下文(), equipmentData);
    }

    private bool ShouldOccupyOffHandSlot(int primaryIndex, ItemSlotData data)
    {
        return 物品占格规则.ShouldOccupyOffHandSlot(创建物品占格上下文(), primaryIndex, data);
    }

    private int GetOffHandEquipmentSlotIndex()
    {
        return 物品占格规则.GetOffHandEquipmentSlotIndex(创建物品占格上下文());
    }

    private bool CanPlaceItemAtIndex(SlotKind kind, int primaryIndex, ItemDatabase.ItemEntry entry, List<ItemSlotData> dataList = null)
    {
        dataList = dataList ?? GetDataList(kind);
        if (primaryIndex < 0 || primaryIndex >= dataList.Count || !IsSlotUsable(kind, primaryIndex) || !dataList[primaryIndex].IsEmpty)
        {
            return false;
        }

        if (!IsOneByTwoItem(entry))
        {
            return true;
        }

        int extensionIndex = GetOneByTwoExtensionIndex(kind, primaryIndex, false);
        return extensionIndex >= 0 &&
            IsSlotUsable(kind, extensionIndex) &&
            extensionIndex < dataList.Count &&
            dataList[extensionIndex].IsEmpty;
    }

    private void SetFootprintDataAt(SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        物品占格规则.SetFootprintDataAt(创建物品占格上下文(), kind, primaryIndex, data);
    }

    private void ClearFootprintAt(SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        物品占格规则.ClearPlacement(创建物品占格上下文(), kind, primaryIndex, data, GetDataList(kind));
    }

    private void RefreshFootprintSlots(SlotKind kind, int primaryIndex, ItemSlotData data)
    {
        物品占格规则.RefreshFootprintSlots(创建物品占格上下文(), kind, primaryIndex, data);
    }

    private int GetOneByTwoExtensionIndex(SlotKind kind, int primaryIndex, bool isRotated)
    {
        return 物品占格规则.GetOneByTwoExtensionIndex(创建物品占格上下文(), kind, primaryIndex, isRotated);
    }

    private int GetGridColumnCount(SlotKind kind)
    {
        return 物品占格规则.GetGridColumnCount(创建物品占格上下文(), kind);
    }

    private bool UsesLowerStartCorner(SlotKind kind)
    {
        return 物品占格规则.UsesLowerStartCorner(创建物品占格上下文(), kind);
    }

    private GridLayoutGroup GetGridLayout(SlotKind kind)
    {
        return 物品占格规则.GetGridLayout(创建物品占格上下文(), kind);
    }

    private bool IsOneByTwoItem(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        return entry.weaponCategory == ItemDatabase.WeaponCategory.Bow ||
            entry.weaponCategory == ItemDatabase.WeaponCategory.TwoHanded ||
            entry.weaponCategory == ItemDatabase.WeaponCategory.Staff;
    }

    private bool IsFootprintItem(ItemSlotData data)
    {
        return 物品占格规则.IsFootprintItem(创建物品占格上下文(), data);
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

            if (extraBackpackSlots.Count > 0)
            {
                return extraBackpackSlots[0];
            }

            return backpackSlots;
        }

        return equipmentSlots;
    }

    private static bool ShouldShowWeaponTooltip(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        return entry.weaponCategory != ItemDatabase.WeaponCategory.None &&
            (entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainHand ||
             entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand);
    }

    public static float GetCharacterWeaponAttackPower(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return CharacterSelectionState.GetCapturedWeaponAttackPower(characterId);
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        if (equipment == null || equipment.Count == 0)
        {
            return CharacterSelectionState.GetCapturedWeaponAttackPower(characterId);
        }

        float attackPower = 装备数值服务.获取角色武器攻击力(characterId, equipment, ResolveItemEntry);
        return attackPower > 0f ? attackPower : CharacterSelectionState.GetCapturedWeaponAttackPower(characterId);
    }

    public static ItemDatabase.WeaponDamageDistribution GetCharacterWeaponDamageDistribution(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.获取角色武器伤害分布(equipment, ResolveItemEntry);
    }

    public static int GetCharacterWeaponCriticalChanceBonus(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return 0;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.获取角色武器暴击率加成(equipment, ResolveItemEntry);
    }

    public static int GetCharacterWeaponCriticalDamageBonus(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return 0;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.获取角色武器暴击伤害加成(equipment, ResolveItemEntry);
    }

    public static int GetCharacterWeaponResistancePenetration(string characterId, ItemDatabase.ResistanceModifierType resistanceType)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return 0;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.获取角色武器抗性穿透(equipment, ResolveItemEntry, resistanceType);
    }

    public static ItemDatabase.WeaponCategory GetCharacterEquippedWeaponCategory(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return ItemDatabase.WeaponCategory.None;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.获取角色已装备武器类型(equipment, ResolveItemEntry);
    }

    public static float GetCharacterStaffDamageMultiplier(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return 1f;
        }

        List<ItemSlotData> equipment = instance.GetEquipmentDataForCharacter(characterId, createIfMissing: false);
        return 装备数值服务.获取角色法杖伤害倍率(equipment, ResolveItemEntry);
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
            RefreshExtraBackpackSlots(slot.index);
            return;
        }

        RefreshEquipmentSlots();
    }

    private void RefreshAll()
    {
        仓储界面刷新规则.RefreshWarehouseFilteredView(创建仓储界面刷新上下文());
        仓储界面刷新规则.RefreshBackpackFilteredView(创建仓储界面刷新上下文());
        RefreshEquipmentSlots();
    }

    private void SetUsableSlotCount(SlotKind kind, string characterId, int count)
    {
        int normalized = Mathf.Max(0, count);
        switch (kind)
        {
            case SlotKind.Warehouse:
                warehouseUsableSlotCount = normalized;
                break;
            case SlotKind.Backpack:
                backpackUsableSlotCount = normalized;
                break;
            case SlotKind.Equipment:
                equipmentUsableSlotCounts[ResolveEquipmentCountCharacterKey(characterId)] = normalized;
                break;
        }

        RefreshAll();
    }

    private int GetResolvedUsableSlotCount(SlotKind kind, string characterId)
    {
        int totalCount = GetSlotCountForKind(kind);
        if (totalCount <= 0)
        {
            return 0;
        }

        int configuredCount;
        switch (kind)
        {
            case SlotKind.Warehouse:
                configuredCount = warehouseUsableSlotCount;
                break;
            case SlotKind.Backpack:
                configuredCount = ResolveBackpackUsableSlotCountFromEvents(totalCount);
                break;
            case SlotKind.Equipment:
                if (!equipmentUsableSlotCounts.TryGetValue(ResolveEquipmentCountCharacterKey(characterId), out configuredCount))
                {
                    configuredCount = totalCount;
                }
                break;
            default:
                configuredCount = totalCount;
                break;
        }

        if (configuredCount < 0)
        {
            configuredCount = totalCount;
        }

        return Mathf.Clamp(configuredCount, 0, totalCount);
    }

    private int ResolveBackpackUsableSlotCountFromEvents(int totalCount)
    {
        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        if (eventDatabase == null)
        {
            return backpackUsableSlotCount >= 0 ? backpackUsableSlotCount : totalCount;
        }

        bool foundAnyMatchingEvent = false;
        int resolvedCount = -1;
        for (int i = 0; i < BackpackLevelEventIds.Length && i < BackpackLevelSlotCounts.Length; i++)
        {
            EventDatabase.EventEntry entry = eventDatabase.FindEntry(BackpackLevelEventIds[i]);
            if (entry == null)
            {
                continue;
            }

            foundAnyMatchingEvent = true;
            if (entry.enabled)
            {
                resolvedCount = BackpackLevelSlotCounts[i];
            }
        }

        if (resolvedCount >= 0)
        {
            return resolvedCount;
        }

        if (foundAnyMatchingEvent)
        {
            return 0;
        }

        return backpackUsableSlotCount >= 0 ? backpackUsableSlotCount : totalCount;
    }

    private int GetSlotCountForKind(SlotKind kind)
    {
        switch (kind)
        {
            case SlotKind.Warehouse:
                return Mathf.Max(warehouseSlots.Count, warehouseData.Count);
            case SlotKind.Backpack:
                return Mathf.Max(backpackSlots.Count, backpackData.Count);
            case SlotKind.Equipment:
                return GetExpectedEquipmentSlotCount();
            default:
                return 0;
        }
    }

    private bool IsSlotUsable(SlotKind kind, int index)
    {
        int totalCount = GetSlotCountForKind(kind);
        if (index < 0 || index >= totalCount)
        {
            return false;
        }

        int usableCount = GetResolvedUsableSlotCount(kind, kind == SlotKind.Equipment ? ResolveEquipmentCharacterId() : null);
        if (usableCount <= 0)
        {
            return false;
        }

        if (kind == SlotKind.Equipment)
        {
            return index < usableCount;
        }

        return index >= totalCount - usableCount;
    }

    private static string ResolveEquipmentCountCharacterKey(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? "玩家" : characterId.Trim();
    }

    private void RefreshWarehouseSlot(int index)
    {
        仓储界面刷新规则.RefreshWarehouseSlot(创建仓储界面刷新上下文(), index);
    }

    private void RefreshBackpackSlot(int index)
    {
        仓储界面刷新规则.RefreshBackpackSlot(创建仓储界面刷新上下文(), index);
    }

    private void RefreshEquipmentSlot(int index)
    {
        仓储界面刷新规则.RefreshEquipmentSlot(创建仓储界面刷新上下文(), index);
    }

    private void RefreshEquipmentSlots()
    {
        仓储界面刷新规则.RefreshEquipmentSlots(创建仓储界面刷新上下文());
    }

    private void RefreshAllRuntimeWeaponModelsInternal()
    {
        武器模型挂载规则.RefreshAllRuntimeWeaponModels(创建武器模型挂载上下文());
    }

    private void RefreshRuntimeWeaponModelForCharacter(string characterId)
    {
        武器模型挂载规则.RefreshRuntimeWeaponModelForCharacter(创建武器模型挂载上下文(), characterId);
    }

    private void RefreshExtraBackpackSlots(int index)
    {
        仓储界面刷新规则.RefreshExtraBackpackSlots(创建仓储界面刷新上下文(), index);
    }

    private void CacheItemTooltip(ItemDatabase.WeaponCategory weaponCategory, bool resetTooltipState)
    {
        物品提示框规则.CacheTooltip(物品提示框状态, 创建物品提示框上下文(), weaponCategory, resetTooltipState);
        同步物品提示框状态();
    }

    private Transform FindTooltipParent()
    {
        Transform popupRoot = FindTransformByPath("Canvas/UI控制器/弹窗");
        if (popupRoot != null)
        {
            return popupRoot;
        }

        popupRoot = FindTransformByPath("Canvas/弹窗");
        if (popupRoot != null)
        {
            return popupRoot;
        }

        Canvas canvas = FindObjectOfType<Canvas>(true);
        return canvas != null ? canvas.transform : null;
    }

    private void ShowItemTooltip(SlotWidget widget, ItemDatabase.ItemEntry entry, SlotRef slot)
    {
        物品提示框规则.ShowTooltip(物品提示框状态, 创建物品提示框上下文(), widget, slot, entry);
        同步物品提示框状态();
    }

    private void HideItemTooltip()
    {
        物品提示框规则.HideTooltip(物品提示框状态, 创建物品提示框上下文());
        同步物品提示框状态();
    }

    private void UpdateTooltipLowerBackgroundState()
    {
        物品提示框规则.UpdateTooltipLowerBackgroundState(物品提示框状态, 创建物品提示框上下文());
        同步物品提示框状态();
    }

    private void SetTooltipAttackPowerText(ItemDatabase.ItemEntry entry, SlotRef slot)
    {
        TMP_Text attackPowerText = EnsureTooltipAttackPowerText();
        if (attackPowerText == null)
        {
            return;
        }

        string ownerCharacterId = ResolveTooltipEquipmentOwnerCharacterId();
        string value = GetAttackPowerDisplayText(entry, slot, ownerCharacterId, attackPowerText, out List<AttackPowerSegment> segments);
        bool hasValue = !string.IsNullOrEmpty(value);
        attackPowerText.gameObject.SetActive(hasValue);
        attackPowerText.text = value ?? string.Empty;
    }

    private TMP_Text EnsureTooltipAttackPowerText()
    {
        if (itemTooltipAttackPowerText != null)
        {
            return itemTooltipAttackPowerText;
        }

        if (itemTooltipTextContentRoot == null)
        {
            return null;
        }

        TMP_Text template = itemTooltipFixedDamageText ?? itemTooltipAttributeMultiplierText ?? itemTooltipDescriptionText;
        if (template == null)
        {
            return null;
        }

        GameObject attackPowerObject = Instantiate(template.gameObject, itemTooltipTextContentRoot, false);
        attackPowerObject.name = "攻击力";

        itemTooltipAttackPowerText = attackPowerObject.GetComponent<TMP_Text>();
        RectTransform attackPowerRect = attackPowerObject.transform as RectTransform;
        RectTransform templateRect = template.rectTransform;
        if (itemTooltipAttackPowerText == null || attackPowerRect == null || templateRect == null)
        {
            return itemTooltipAttackPowerText;
        }

        attackPowerRect.anchorMin = templateRect.anchorMin;
        attackPowerRect.anchorMax = templateRect.anchorMax;
        attackPowerRect.pivot = templateRect.pivot;
        attackPowerRect.sizeDelta = templateRect.sizeDelta;
        attackPowerRect.localScale = templateRect.localScale;
        attackPowerRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -36f);
        itemTooltipAttackPowerText.text = string.Empty;
        itemTooltipAttackPowerText.gameObject.SetActive(false);
        return itemTooltipAttackPowerText;
    }

    private string GetAttackPowerDisplayText(ItemDatabase.ItemEntry entry, SlotRef slot, string ownerCharacterId, TMP_Text attackPowerText, out List<AttackPowerSegment> segments)
    {
        segments = null;
        if (!装备数值服务.是攻击力武器条目(entry) || slot.kind != SlotKind.Equipment)
        {
            return string.Empty;
        }

        return BuildAttackPowerDisplayText(entry, ownerCharacterId, attackPowerText, out segments);
    }

    private static string BuildAttackPowerDisplayText(ItemDatabase.ItemEntry entry, string ownerCharacterId, TMP_Text attackPowerText, out List<AttackPowerSegment> segments)
    {
        segments = null;
        if (!装备数值服务.是攻击力武器条目(entry))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(ownerCharacterId))
        {
            return "攻击力：无";
        }

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(ownerCharacterId) : null;
        if (statEntry == null)
        {
            return "攻击力：无";
        }

        segments = BuildAttackPowerSegments(entry, statEntry);
        if (segments == null)
        {
            return "<color=#E6C229>攻击力：伤害分布未配置</color>";
        }

        if (segments.Count == 0)
        {
            float attackPower = 装备数值服务.计算武器攻击力(entry, statEntry);
            return $"攻击力：{attackPower:0.##}";
        }

        TMP_SpriteAsset activeSpriteAsset = ResolveAttackPowerSpriteAsset();
        List<string> parts = new List<string>();
        for (int i = 0; i < segments.Count; i++)
        {
            AttackPowerSegment segment = segments[i];
            string segmentText = $"<color={segment.colorHex}>{FormatTooltipAttackPowerValue(segment.amount)}</color>";

            string spriteName = GetAttackPowerSpriteName(segment.attributeId);
            if (activeSpriteAsset != null && !string.IsNullOrWhiteSpace(spriteName))
            {
                segmentText += $"<sprite name=\"{spriteName}\">";
            }

            parts.Add(segmentText);
        }

        if (attackPowerText != null)
        {
            attackPowerText.spriteAsset = activeSpriteAsset;
        }

        return $"攻击力：{string.Join("<color=#808080>+</color>", parts)}";
    }

    private static string BuildTooltipLowerContentText(ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        WeaponDetailLowerTextDatabase database = WeaponDetailLowerTextDatabase.LoadDefault();
        List<string> lines = new List<string>();

        if (entry.criticalChanceBonus > 0)
        {
            string line = FormatWeaponDetailLowerText(
                database != null ? database.criticalChanceFormat : string.Empty,
                entry.criticalChanceBonus.ToString());
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        if (entry.criticalDamageBonus > 0)
        {
            string line = FormatWeaponDetailLowerText(
                database != null ? database.criticalDamageFormat : string.Empty,
                entry.criticalDamageBonus.ToString());
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
    }

    private static string FormatWeaponDetailLowerText(string format, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(format))
        {
            return value;
        }

        return format.Contains("x")
            ? format.Replace("x", value)
            : format + value;
    }

    private sealed class AttackPowerSegment
    {
        public string attributeId = string.Empty;
        public float amount;
        public string colorHex = "#FFFFFF";
    }

    private static List<AttackPowerSegment> BuildAttackPowerSegments(ItemDatabase.ItemEntry entry, CharacterStatDatabase.StatEntry statEntry)
    {
        List<AttackPowerSegment> segments = new List<AttackPowerSegment>();
        if (!装备数值服务.是攻击力武器条目(entry) || statEntry == null)
        {
            return segments;
        }

        float attackPower = 装备数值服务.计算武器攻击力(entry, statEntry);
        if (attackPower <= 0f)
        {
            return segments;
        }

        ItemDatabase.WeaponDamageDistribution distribution = entry.weaponDamageDistribution;
        if (distribution == null || distribution.physical < 0 || distribution.fire < 0 ||
            distribution.corruption < 0 || distribution.cold < 0 || distribution.Total != 100)
        {
            Debug.LogWarning($"[物品数据警告] 武器提示读取到未配置伤害分布：{entry.itemId}");
            return null;
        }

        int total = Mathf.Max(1, distribution.Total);
        TryAddAttackPowerSegment(segments, "物理", distribution.physical, total, attackPower, "#FFFFFF");
        TryAddAttackPowerSegment(segments, "火焰", distribution.fire, total, attackPower, "#FF8A00");
        TryAddAttackPowerSegment(segments, "腐败", distribution.corruption, total, attackPower, "#33CC66");
        TryAddAttackPowerSegment(segments, "寒冷", distribution.cold, total, attackPower, "#4DA6FF");
        return segments;
    }

    private static void TryAddAttackPowerSegment(List<AttackPowerSegment> segments, string attributeId, int distributionValue, int distributionTotal, float attackPower, string colorHex)
    {
        if (segments == null || distributionValue <= 0 || distributionTotal <= 0 || attackPower <= 0f)
        {
            return;
        }

        float amount = attackPower * distributionValue / distributionTotal;
        if (amount <= 0f)
        {
            return;
        }

        segments.Add(new AttackPowerSegment
        {
            attributeId = attributeId,
            amount = amount,
            colorHex = colorHex
        });
    }

    private static string FormatTooltipAttackPowerValue(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }

        return value.ToString("0.#");
    }

    private static string GetAttackPowerSpriteName(string attributeId)
    {
        switch (attributeId)
        {
            case "物理":
                return "物理伤害";
            case "火焰":
                return "火焰伤害";
            case "腐败":
                return "腐败伤害";
            case "寒冷":
                return "寒冷伤害";
            default:
                return string.Empty;
        }
    }

    private static TMP_SpriteAsset ResolveAttackPowerSpriteAsset()
    {
        AttackPowerTextSpriteDatabase database = AttackPowerTextSpriteDatabase.LoadDefault();
        return database != null ? database.spriteAsset : null;
    }

    private string ResolveTooltipEquipmentOwnerCharacterId()
    {
        string activeCharacterId = ResolveEquipmentCharacterId();
        return string.IsNullOrWhiteSpace(activeCharacterId) ? string.Empty : activeCharacterId;
    }

    private string GetTooltipOwnerDisplayText(ItemDatabase.ItemEntry entry, SlotRef slot)
    {
        if (!装备数值服务.是攻击力武器条目(entry) || slot.kind != SlotKind.Equipment)
        {
            return string.Empty;
        }

        string ownerCharacterId = ResolveTooltipEquipmentOwnerCharacterId();
        if (string.IsNullOrWhiteSpace(ownerCharacterId))
        {
            return "装备者：\n无";
        }

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(ownerCharacterId) : null;
        return statEntry != null ? $"装备者：\n{ownerCharacterId}" : "装备者：\n无";
    }

    private static Material EnsureItemTooltipIconFadeMaterial()
    {
        if (itemTooltipIconFadeMaterial != null)
        {
            return itemTooltipIconFadeMaterial;
        }

        Shader shader = Shader.Find(ItemTooltipIconFadeShaderName);
        if (shader == null)
        {
            return null;
        }

        itemTooltipIconFadeMaterial = new Material(shader)
        {
            name = "ItemTooltipIconBottomFade"
        };
        itemTooltipIconFadeMaterial.hideFlags = HideFlags.HideAndDontSave;
        itemTooltipIconFadeMaterial.SetFloat("_FadeHeight", 0.2f);
        itemTooltipIconFadeMaterial.SetFloat("_FadePower", 3f);
        return itemTooltipIconFadeMaterial;
    }

    private static string ResolveItemDisplayName(ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(entry.displayName))
        {
            return entry.displayName;
        }

        if (!string.IsNullOrWhiteSpace(entry.itemId) && entry.itemId.StartsWith("itm_", StringComparison.Ordinal))
        {
            return entry.itemId.Substring(4);
        }

        return entry.itemId ?? string.Empty;
    }

    public static string GetItemDisplayName(string itemId)
    {
        ItemDatabase.ItemEntry entry = ResolveItemEntry(itemId);
        return ResolveItemDisplayName(entry);
    }

    private static string GetFixedDamageDisplayText(ItemDatabase.ItemEntry entry)
    {
        float value = entry != null ? entry.fixedDamage : 0f;
        return $"固定伤害：{value:0.##}";
    }

    private static string GetAttributeMultiplierDisplayText(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.weaponAttributeMultipliers == null || entry.weaponAttributeMultipliers.Count == 0)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        for (int i = 0; i < entry.weaponAttributeMultipliers.Count; i++)
        {
            ItemDatabase.WeaponAttributeMultiplierEntry multiplier = entry.weaponAttributeMultipliers[i];
            if (multiplier == null || multiplier.attributeType == ItemDatabase.WeaponAttributeType.None)
            {
                continue;
            }

            parts.Add($"{GetWeaponAttributeTypeDisplayName(multiplier.attributeType)}{GetAttributeMultiplierRank(multiplier.multiplier)}");
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" ", parts);
    }

    private static string GetItemQualityDisplayName(ItemDatabase.ItemQuality quality)
    {
        switch (quality)
        {
            case ItemDatabase.ItemQuality.Excellent:
                return "优秀";
            case ItemDatabase.ItemQuality.Epic:
                return "史诗";
            case ItemDatabase.ItemQuality.Blessed:
                return "赐福";
            default:
                return "普通";
        }
    }

    private static string GetWeaponCategoryDisplayName(ItemDatabase.WeaponCategory weaponCategory)
    {
        switch (weaponCategory)
        {
            case ItemDatabase.WeaponCategory.OneHanded:
                return "单手武器";
            case ItemDatabase.WeaponCategory.TwoHanded:
                return "双手武器";
            case ItemDatabase.WeaponCategory.Bow:
                return "弓箭";
            case ItemDatabase.WeaponCategory.Staff:
                return "法杖";
            default:
                return weaponCategory == ItemDatabase.WeaponCategory.None ? string.Empty : weaponCategory.ToString();
        }
    }

    private static string GetWeaponAttributeTypeDisplayName(ItemDatabase.WeaponAttributeType attributeType)
    {
        switch (attributeType)
        {
            case ItemDatabase.WeaponAttributeType.Strength:
                return "力量";
            case ItemDatabase.WeaponAttributeType.Agility:
                return "敏捷";
            case ItemDatabase.WeaponAttributeType.Intelligence:
                return "智力";
            default:
                return string.Empty;
        }
    }

    private static string GetAttributeMultiplierRank(float multiplier)
    {
        if (multiplier < 0.5f)
        {
            return "E";
        }

        if (multiplier < 1f)
        {
            return "D";
        }

        if (multiplier < 1.5f)
        {
            return "C";
        }

        if (multiplier < 2f)
        {
            return "B";
        }

        if (multiplier < 2.5f)
        {
            return "A";
        }

        return "S";
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

    private GameObject ResolveQualityBackgroundPrefab(ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        string cacheKey = BuildQualityBackgroundCacheKey(entry.quality, ShouldUseOneByTwoQualityBackground(entry));
        if (qualityBackgroundPrefabCache.TryGetValue(cacheKey, out GameObject prefab))
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

    private void CacheQualityBackgroundPrefabs()
    {
        qualityBackgroundPrefabCache.Clear();

        ItemQualityBackgroundDatabase database = ItemQualityBackgroundDatabase.LoadDefault();
        if (database == null)
        {
            return;
        }

        CacheQualityBackgroundPrefab(database, ItemDatabase.ItemQuality.Common);
        CacheQualityBackgroundPrefab(database, ItemDatabase.ItemQuality.Excellent);
        CacheQualityBackgroundPrefab(database, ItemDatabase.ItemQuality.Epic);
        CacheQualityBackgroundPrefab(database, ItemDatabase.ItemQuality.Blessed);
    }

    private void CacheQualityBackgroundPrefab(ItemQualityBackgroundDatabase database, ItemDatabase.ItemQuality quality)
    {
        if (database == null)
        {
            return;
        }

        CacheQualityBackgroundPrefabVariant(database, quality, useOneByTwo: false);
        CacheQualityBackgroundPrefabVariant(database, quality, useOneByTwo: true);
    }

    private void CacheQualityBackgroundPrefabVariant(
        ItemQualityBackgroundDatabase database,
        ItemDatabase.ItemQuality quality,
        bool useOneByTwo)
    {
        if (database == null)
        {
            return;
        }

        GameObject prefab = database.GetPrefab(quality, useOneByTwo);
        if (prefab == null)
        {
            return;
        }

        qualityBackgroundPrefabCache[BuildQualityBackgroundCacheKey(quality, useOneByTwo)] = prefab;
    }

    private static bool ShouldUseOneByTwoQualityBackground(ItemDatabase.ItemEntry entry)
    {
        return entry != null &&
            entry.category == ItemDatabase.ItemCategory.Equipment &&
            (entry.weaponCategory == ItemDatabase.WeaponCategory.Bow ||
             entry.weaponCategory == ItemDatabase.WeaponCategory.TwoHanded ||
             entry.weaponCategory == ItemDatabase.WeaponCategory.Staff);
    }

    private static string BuildQualityBackgroundCacheKey(ItemDatabase.ItemQuality quality, bool useOneByTwo)
    {
        return quality.ToString() + (useOneByTwo ? "_1x2" : "_1x1");
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

    private static bool ShouldDisplayItem(CategoryFilterBinding binding, ItemSlotData data)
    {
        if (data.IsEmpty || binding == null || binding.selectedCategories.Count == 0)
        {
            return true;
        }

        ItemDatabase.ItemEntry entry = ResolveItemEntry(data.itemId);
        return entry != null && binding.selectedCategories.Contains(entry.category);
    }

    private void RefreshWarehouseFilteredView()
    {
        仓储界面刷新规则.RefreshWarehouseFilteredView(创建仓储界面刷新上下文());
    }

    private void RefreshBackpackFilteredView()
    {
        仓储界面刷新规则.RefreshBackpackFilteredView(创建仓储界面刷新上下文());
    }

    private void UnbindAll()
    {
        for (int i = 0; i < categoryFilterUnbindActions.Count; i++)
        {
            categoryFilterUnbindActions[i]?.Invoke();
        }

        categoryFilterUnbindActions.Clear();
        warehouseFilter.panelRoot = null;
        warehouseFilter.toggles.Clear();
        warehouseFilter.selectedCategories.Clear();
        backpackFilter.panelRoot = null;
        backpackFilter.toggles.Clear();
        backpackFilter.selectedCategories.Clear();

        HandleEndDrag();
        HideItemTooltip();
        仓储界面刷新规则.ClearRuntimeVisuals(warehouseSlots);
        仓储界面刷新规则.ClearRuntimeVisuals(backpackSlots);
        for (int i = 0; i < extraBackpackSlots.Count; i++)
        {
            仓储界面刷新规则.ClearRuntimeVisuals(extraBackpackSlots[i]);
        }
        仓储界面刷新规则.ClearRuntimeVisuals(equipmentSlots);
        for (int i = 0; i < extraEquipmentSlots.Count; i++)
        {
            仓储界面刷新规则.ClearRuntimeVisuals(extraEquipmentSlots[i]);
        }
        warehouseSlots.Clear();
        backpackSlots.Clear();
        extraBackpackSlots.Clear();
        equipmentSlots.Clear();
        extraEquipmentSlots.Clear();
    }

}









