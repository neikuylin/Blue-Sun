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
        Chest,
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
        Chest,
        TargetIdEquipment
    }

    internal struct SlotRef
    {
        public SlotKind kind;
        public int index;
    }

    private struct 宝箱候选格
    {
        public int index;
        public bool isRotated;
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
    private readonly 物品转移服务 物品转移规则 = new 物品转移服务();
    private readonly 物品提示框服务 物品提示框规则 = new 物品提示框服务();
    private readonly 物品提示框服务.State 物品提示框状态 = new 物品提示框服务.State();
    private readonly 武器模型挂载服务 武器模型挂载规则 = new 武器模型挂载服务();
    private readonly 仓储界面刷新服务 仓储界面刷新规则 = new 仓储界面刷新服务();
    private readonly 仓储界面绑定服务 仓储界面绑定规则 = new 仓储界面绑定服务();
    private readonly 仓储交互服务 仓储交互规则 = new 仓储交互服务();
    private readonly 仓储交互服务.State 仓储交互状态 = new 仓储交互服务.State();
    private readonly 物品占格服务 物品占格规则 = new 物品占格服务();
    private readonly 槽位访问服务 槽位访问规则 = new 槽位访问服务();
    private readonly 摆放规则服务 摆放规则 = new 摆放规则服务();
    private readonly 仓储状态服务 仓储状态 = new 仓储状态服务();
    private readonly Dictionary<string, GameObject> qualityBackgroundPrefabCache = new Dictionary<string, GameObject>(StringComparer.Ordinal);

    private readonly List<SlotWidget> warehouseSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> backpackSlots = new List<SlotWidget>();
    private readonly List<SlotWidget> chestSlots = new List<SlotWidget>();
    private readonly List<List<SlotWidget>> extraBackpackSlots = new List<List<SlotWidget>>();
    private readonly List<SlotWidget> equipmentSlots = new List<SlotWidget>();
    private readonly List<List<SlotWidget>> extraEquipmentSlots = new List<List<SlotWidget>>();
    private readonly List<Action> categoryFilterUnbindActions = new List<Action>();

    private bool hasCachedBackpackLayout;
    private GridLayoutGroup.Corner cachedBackpackStartCorner;
    private GridLayoutGroup.Constraint cachedBackpackConstraint;
    private int cachedBackpackConstraintCount;
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

    public static int BackpackSlotCount => instance != null ? instance.仓储状态.背包数据.Count : 0;
    public static int ChestSlotCount => instance != null ? instance.GetCurrentChestSlotCount() : 0;
    public static int CurrentChestSerial => instance != null ? instance.仓储状态.当前宝箱序列号 : 0;
    public static int WarehouseSlotCount => instance != null ? instance.仓储状态.仓库数据.Count : 0;
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

    public static void CaptureSaveData(SaveGameData.InventorySave target)
    {
        if (target == null)
        {
            return;
        }

        if (instance == null)
        {
            Bootstrap();
        }

        target.warehouseSlots.Clear();
        target.backpackSlots.Clear();
        target.equipmentByCharacter.Clear();
        target.equipmentUsableSlotCounts.Clear();

        if (instance == null)
        {
            return;
        }

        target.warehouseUsableSlotCount = instance.仓储状态.仓库可用槽位数量;
        target.backpackUsableSlotCount = instance.仓储状态.背包可用槽位数量;
        CopySlotsToSave(instance.仓储状态.仓库数据, target.warehouseSlots);
        CopySlotsToSave(instance.仓储状态.背包数据, target.backpackSlots);

        foreach (KeyValuePair<string, List<ItemSlotData>> pair in instance.仓储状态.角色装备数据)
        {
            SaveGameData.CharacterEquipmentSave equipmentSave = new SaveGameData.CharacterEquipmentSave
            {
                characterId = pair.Key
            };
            CopySlotsToSave(pair.Value, equipmentSave.slots);
            target.equipmentByCharacter.Add(equipmentSave);
        }

        foreach (KeyValuePair<string, int> pair in instance.仓储状态.角色装备可用槽位数量)
        {
            target.equipmentUsableSlotCounts.Add(new SaveGameData.CharacterSlotCountSave
            {
                characterId = pair.Key,
                usableSlotCount = pair.Value
            });
        }
    }

    public static void ApplySaveData(SaveGameData.InventorySave source)
    {
        if (instance == null)
        {
            Bootstrap();
        }

        if (instance == null)
        {
            return;
        }

        instance.仓储状态.清空运行时状态();

        if (source != null)
        {
            CopySlotsFromSave(source.warehouseSlots, instance.仓储状态.仓库数据);
            CopySlotsFromSave(source.backpackSlots, instance.仓储状态.背包数据);

            if (source.equipmentByCharacter != null)
            {
                for (int i = 0; i < source.equipmentByCharacter.Count; i++)
                {
                    SaveGameData.CharacterEquipmentSave savedEquipment = source.equipmentByCharacter[i];
                    if (savedEquipment == null || string.IsNullOrWhiteSpace(savedEquipment.characterId))
                    {
                        continue;
                    }

                    List<ItemSlotData> equipmentData = instance.GetEquipmentDataForCharacter(savedEquipment.characterId, createIfMissing: true);
                    equipmentData.Clear();
                    CopySlotsFromSave(savedEquipment.slots, equipmentData);
                    instance.RebuildEquipmentFootprintOccupancy(equipmentData);
                }
            }

            if (source.warehouseUsableSlotCount >= 0)
            {
                instance.仓储状态.设置可用槽位数量(SlotKind.Warehouse, null, source.warehouseUsableSlotCount);
            }

            if (source.backpackUsableSlotCount >= 0)
            {
                instance.仓储状态.设置可用槽位数量(SlotKind.Backpack, null, source.backpackUsableSlotCount);
            }

            if (source.equipmentUsableSlotCounts != null)
            {
                for (int i = 0; i < source.equipmentUsableSlotCounts.Count; i++)
                {
                    SaveGameData.CharacterSlotCountSave countSave = source.equipmentUsableSlotCounts[i];
                    if (countSave == null || string.IsNullOrWhiteSpace(countSave.characterId) || countSave.usableSlotCount < 0)
                    {
                        continue;
                    }

                    instance.仓储状态.设置可用槽位数量(SlotKind.Equipment, countSave.characterId, countSave.usableSlotCount);
                }
            }
        }

        instance.RefreshAll();
        instance.RefreshAllRuntimeWeaponModelsInternal();
    }

    public static void ResetSaveData()
    {
        if (instance == null)
        {
            Bootstrap();
        }

        if (instance == null)
        {
            return;
        }

        instance.仓储状态.清空运行时状态();
        instance.RefreshAll();
        instance.RefreshAllRuntimeWeaponModelsInternal();
    }

    public static List<ItemSlotSnapshot> GetBackpackSnapshots()
    {
        return instance != null ? BuildSnapshots(instance.仓储状态.背包数据) : new List<ItemSlotSnapshot>();
    }

    public static List<ItemSlotSnapshot> GetChestSnapshots()
    {
        return instance != null ? BuildSnapshots(instance.GetCurrentChestData(false)) : new List<ItemSlotSnapshot>();
    }

    public static List<ItemSlotSnapshot> GetChestSnapshots(int chestSerial)
    {
        if (instance == null)
        {
            return new List<ItemSlotSnapshot>();
        }

        return BuildSnapshots(instance.仓储状态.获取宝箱数据(chestSerial, false));
    }

    public static List<int> GetChestSerialNumbers()
    {
        return instance != null ? instance.仓储状态.获取宝箱序列号列表() : new List<int>();
    }

    public static int RegisterChestInstance()
    {
        if (instance == null)
        {
            Bootstrap();
        }

        return instance != null ? instance.仓储状态.注册宝箱序列号() : 0;
    }

    public static void OpenChest(int chestSerial)
    {
        OpenChest(chestSerial, string.Empty);
    }

    public static void OpenChest(int chestSerial, string chestContentGroupId)
    {
        if (instance == null)
        {
            Bootstrap();
        }

        instance?.ActivateChest(chestSerial, chestContentGroupId);
    }

    public static List<ItemSlotSnapshot> GetWarehouseSnapshots()
    {
        return instance != null ? BuildSnapshots(instance.仓储状态.仓库数据) : new List<ItemSlotSnapshot>();
    }

    public static List<string> GetEquipmentCharacterIds()
    {
        if (instance == null)
        {
            return new List<string>();
        }

        return instance.仓储状态.获取角色装备数据键列表();
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
        return 物品显示辅助服务.获取角色攻击力显示文本(entry, characterId);
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
        if (instance == null || index < 0 || index >= instance.仓储状态.背包数据.Count)
        {
            return false;
        }

        data = instance.仓储状态.背包数据[index];
        return true;
    }

    public static bool TryGetWarehouseSlotData(int index, out ItemSlotData data)
    {
        data = default;
        if (instance == null || index < 0 || index >= instance.仓储状态.仓库数据.Count)
        {
            return false;
        }

        data = instance.仓储状态.仓库数据[index];
        return true;
    }

    public static bool TryGetChestSlotData(int index, out ItemSlotData data)
    {
        return TryGetChestSlotData(CurrentChestSerial, index, out data);
    }

    public static bool TryGetChestSlotData(int chestSerial, int index, out ItemSlotData data)
    {
        data = default;
        List<ItemSlotData> chestData = instance != null ? instance.仓储状态.获取宝箱数据(chestSerial, false) : null;
        if (chestData == null || index < 0 || index >= chestData.Count)
        {
            return false;
        }

        data = chestData[index];
        return true;
    }

    public static bool TrySetBackpackSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.仓储状态.背包数据.Count)
        {
            return false;
        }

        instance.仓储状态.背包数据[index] = PrepareItemSlotDataForStorage(data, $"背包格 {index}");
        instance.RefreshBackpackSlot(index);
        instance.RefreshExtraBackpackSlots(index);
        return true;
    }

    public static bool TrySetWarehouseSlotData(int index, ItemSlotData data)
    {
        if (instance == null || index < 0 || index >= instance.仓储状态.仓库数据.Count)
        {
            return false;
        }

        instance.仓储状态.仓库数据[index] = PrepareItemSlotDataForStorage(data, $"仓库格 {index}");
        instance.RefreshWarehouseSlot(index);
        return true;
    }

    public static bool TrySetChestSlotData(int index, ItemSlotData data)
    {
        return TrySetChestSlotData(CurrentChestSerial, index, data);
    }

    public static bool TrySetChestSlotData(int chestSerial, int index, ItemSlotData data)
    {
        List<ItemSlotData> chestData = instance != null ? instance.仓储状态.获取宝箱数据(chestSerial, false) : null;
        if (chestData == null || index < 0 || index >= chestData.Count)
        {
            return false;
        }

        chestData[index] = PrepareItemSlotDataForStorage(data, $"宝箱{chestSerial}格 {index}");
        if (chestSerial == CurrentChestSerial)
        {
            instance.RefreshChestSlot(index);
        }

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
        仓储状态服务.确保容量(equipment, Mathf.Max(instance.GetExpectedEquipmentSlotCount(), index + 1));
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
        return AddItemToStorage(SlotKind.Backpack, itemId, icon, count, maxStack);
    }

    public static int AddItemToWarehouse(ItemDatabase.ItemEntry itemEntry, int count, int maxStack = 99)
    {
        return AddItemToStorage(SlotKind.Warehouse, itemEntry, count, maxStack);
    }

    public static int AddItemToBackpack(ItemDatabase.ItemEntry itemEntry, int count, int maxStack = 99)
    {
        return AddItemToStorage(SlotKind.Backpack, itemEntry, count, maxStack);
    }

    public static int AddItemToChest(ItemDatabase.ItemEntry itemEntry, int count, int maxStack = 99)
    {
        return AddItemToChest(CurrentChestSerial, itemEntry, count, maxStack);
    }

    public static int AddItemToChest(int chestSerial, ItemDatabase.ItemEntry itemEntry, int count, int maxStack = 99)
    {
        if (instance == null || chestSerial <= 0)
        {
            return count;
        }

        int previousChestSerial = instance.仓储状态.当前宝箱序列号;
        instance.仓储状态.设置当前宝箱序列号(chestSerial);
        instance.仓储状态.确保当前宝箱数据容量(instance.chestSlots.Count);
        int remain = AddItemToStorage(SlotKind.Chest, itemEntry, count, maxStack);
        if (previousChestSerial != chestSerial)
        {
            instance.仓储状态.设置当前宝箱序列号(previousChestSerial);
        }

        instance.RefreshAll();
        return remain;
    }

    private static int AddItemToStorage(SlotKind targetKind, ItemDatabase.ItemEntry itemEntry, int count, int maxStack = 99)
    {
        if (itemEntry == null)
        {
            return count;
        }

        Sprite icon = ResolveDisplaySpriteFromPrefab(itemEntry.prefab);
        return AddItemToStorage(targetKind, itemEntry.itemId, icon, count, ResolveMaxStack(itemEntry, maxStack));
    }

    private static int AddItemToStorage(SlotKind targetKind, string itemId, Sprite icon, int count, int maxStack = 99)
    {
        if (instance == null || string.IsNullOrEmpty(itemId) || icon == null || count <= 0)
        {
            return count;
        }

        ItemDatabase.ItemEntry itemEntry = ResolveItemEntry(itemId);
        bool useOneByTwo = 摆放规则服务.是一乘二物品(itemEntry);
        maxStack = ResolveMaxStack(itemId, maxStack);
        int remain = count;
        List<ItemSlotData> targetData = instance.GetDataList(targetKind);
        if (targetData == null)
        {
            return count;
        }

        for (int i = targetData.Count - 1; i >= 0 && remain > 0 && !useOneByTwo; i--)
        {
            ItemSlotData slot = targetData[i];
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
            targetData[i] = slot;
            remain -= add;
            instance.RefreshByRef(new SlotRef { kind = targetKind, index = i });
        }

        while (remain > 0)
        {
            int targetIndex = instance.FindFirstAvailableSlotIndex(targetKind, itemEntry);
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
            instance.SetFootprintDataAt(targetKind, targetIndex, data);
            remain -= add;
            instance.RefreshFootprintSlots(targetKind, targetIndex, data);
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

    private static void CopySlotsToSave(List<ItemSlotData> source, List<SaveGameData.ItemSlotSave> target)
    {
        if (source == null || target == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            ItemSlotData slot = source[i];
            target.Add(new SaveGameData.ItemSlotSave
            {
                itemId = slot.itemId,
                count = slot.count,
                maxStack = slot.maxStack,
                isRotated = slot.isRotated,
                isFootprintExtension = slot.isFootprintExtension,
                primarySlotIndex = slot.primarySlotIndex
            });
        }
    }

    private static void CopySlotsFromSave(List<SaveGameData.ItemSlotSave> source, List<ItemSlotData> target)
    {
        if (source == null || target == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            SaveGameData.ItemSlotSave slot = source[i];
            target.Add(ConvertSlotFromSave(slot));
        }
    }

    private static ItemSlotData ConvertSlotFromSave(SaveGameData.ItemSlotSave source)
    {
        if (source == null)
        {
            return default;
        }

        if (source.isFootprintExtension)
        {
            return new ItemSlotData
            {
                isFootprintExtension = true,
                primarySlotIndex = Mathf.Max(0, source.primarySlotIndex)
            };
        }

        if (string.IsNullOrWhiteSpace(source.itemId) && source.count <= 0)
        {
            return default;
        }

        ItemDatabase.ItemEntry entry = ResolveItemEntry(source.itemId);
        if (entry == null)
        {
            Debug.LogError($"存档：物品 ID 不存在，无法读档：{source.itemId}");
            return default;
        }

        return PrepareItemSlotDataForStorage(new ItemSlotData
        {
            itemId = source.itemId,
            icon = ResolveDisplaySpriteFromPrefab(entry.prefab),
            count = source.count,
            maxStack = source.maxStack,
            isRotated = source.isRotated,
            primarySlotIndex = source.primarySlotIndex
        }, $"存档物品:{source.itemId}");
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
        return 物品显示辅助服务.解析预制体显示图标(prefab, FindChildByName, FindDescendantByName);
    }
    public static bool RemoveItemAt(int slotIndex, int count)
    {
        if (instance == null || slotIndex < 0 || slotIndex >= instance.仓储状态.背包数据.Count || count <= 0)
        {
            return false;
        }

        slotIndex = instance.ResolvePrimarySlotIndex(SlotKind.Backpack, slotIndex);
        ItemSlotData slot = instance.仓储状态.背包数据[slotIndex];
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

        instance.仓储状态.背包数据[slotIndex] = slot;
        instance.RefreshBackpackSlot(slotIndex);
        instance.RefreshExtraBackpackSlots(slotIndex);
        return true;
    }

    public static bool MoveItem(int fromSlot, int toSlot)
    {
        if (instance == null ||
            fromSlot < 0 || toSlot < 0 ||
            fromSlot >= instance.仓储状态.背包数据.Count || toSlot >= instance.仓储状态.背包数据.Count ||
            fromSlot == toSlot)
        {
            return false;
        }

        fromSlot = instance.ResolvePrimarySlotIndex(SlotKind.Backpack, fromSlot);
        toSlot = instance.ResolvePrimarySlotIndex(SlotKind.Backpack, toSlot);
        ItemSlotData from = instance.仓储状态.背包数据[fromSlot];
        ItemSlotData to = instance.仓储状态.背包数据[toSlot];
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

        仓储状态.确保仓库数据容量(FixedStorageSlotCount);
        int backpackWidgetCount = backpackSlots.Count;
        for (int i = 0; i < extraBackpackSlots.Count; i++)
        {
            backpackWidgetCount = Mathf.Max(backpackWidgetCount, extraBackpackSlots[i].Count);
        }
        仓储状态.确保背包数据容量(backpackWidgetCount);
        仓储状态.确保当前宝箱数据容量(chestSlots.Count);
        仓储界面绑定规则.BindCategoryFilters(创建仓储界面绑定上下文());
        仓储界面绑定规则.BindDragRelays(创建仓储界面绑定上下文());
        RefreshAll();
    }

    private void ActivateChest(int chestSerial, string chestContentGroupId)
    {
        if (chestSerial <= 0)
        {
            return;
        }

        仓储状态.设置当前宝箱序列号(chestSerial);
        if (chestSlots.Count == 0)
        {
            BindScene();
            TryGenerateCurrentChestContent(chestContentGroupId);
            RefreshAll();
            return;
        }

        仓储状态.确保当前宝箱数据容量(chestSlots.Count);
        TryGenerateCurrentChestContent(chestContentGroupId);
        RefreshAll();
    }

    private void TryGenerateCurrentChestContent(string chestContentGroupId)
    {
        int chestSerial = 仓储状态.当前宝箱序列号;
        if (chestSerial <= 0 ||
            string.IsNullOrWhiteSpace(chestContentGroupId) ||
            仓储状态.宝箱内容已生成(chestSerial))
        {
            return;
        }

        宝箱内容数据库 chestContentDatabase = 宝箱内容数据库.LoadDefault();
        if (chestContentDatabase == null)
        {
            return;
        }

        宝箱内容数据库.宝箱内容组 group = chestContentDatabase.FindGroup(chestContentGroupId);
        if (group == null)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成失败：找不到内容组ID：{chestContentGroupId}");
            return;
        }

        if (group.物品列表 == null || group.物品列表.Count == 0)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成失败：内容组没有物品：{chestContentGroupId}");
            return;
        }

        ItemDatabase itemDatabase = ItemDatabase.LoadDefault();
        if (itemDatabase == null)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成失败：物品数据库未加载。");
            return;
        }

        List<ItemSlotData> chestData = GetCurrentChestData(true);
        if (chestData == null)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成失败：宝箱数据不存在。");
            return;
        }

        仓储状态.标记宝箱内容已生成(chestSerial);

        if (group.生成类型 == 宝箱内容数据库.宝箱物品生成类型.随机物品)
        {
            for (int i = 0; i < group.物品列表.Count; i++)
            {
                宝箱内容数据库.宝箱物品条目 itemRule = group.物品列表[i];
                if (itemRule == null || UnityEngine.Random.value > Mathf.Clamp01(itemRule.出现概率))
                {
                    continue;
                }

                if (!TryResolveChestGroupItem(chestSerial, itemDatabase, itemRule, i, out ItemDatabase.ItemEntry itemEntry))
                {
                    return;
                }

                if (!TryPlaceChestRuleItem(chestSerial, itemEntry, itemRule.数量, chestData))
                {
                    return;
                }
            }

            return;
        }

        for (int i = 0; i < group.物品列表.Count; i++)
        {
            宝箱内容数据库.宝箱物品条目 itemRule = group.物品列表[i];
            if (!TryResolveChestGroupItem(chestSerial, itemDatabase, itemRule, i, out ItemDatabase.ItemEntry itemEntry))
            {
                return;
            }

            if (!TryPlaceChestRuleItem(chestSerial, itemEntry, itemRule.数量, chestData))
            {
                return;
            }
        }
    }

    private bool TryResolveChestGroupItem(
        int chestSerial,
        ItemDatabase itemDatabase,
        宝箱内容数据库.宝箱物品条目 itemRule,
        int ruleIndex,
        out ItemDatabase.ItemEntry itemEntry)
    {
        itemEntry = null;
        if (itemRule == null)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成中断：第 {ruleIndex + 1} 个物品为空。");
            return false;
        }

        itemEntry = itemDatabase.FindEntry(itemRule.物品ID);
        if (itemEntry == null)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成中断：物品不存在：{itemRule.物品ID}");
            return false;
        }

        return true;
    }

    private bool TryPlaceChestRuleItem(
        int chestSerial,
        ItemDatabase.ItemEntry itemEntry,
        int count,
        List<ItemSlotData> chestData)
    {
        if (itemEntry == null)
        {
            return false;
        }

        Sprite icon = ResolveDisplaySpriteFromPrefab(itemEntry.prefab);
        if (icon == null)
        {
            Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成中断：物品缺少可显示图标：{itemEntry.itemId}");
            return false;
        }

        int maxStack = ResolveMaxStack(itemEntry, 1);
        int remain = Mathf.Max(1, count);
        bool useOneByTwo = 摆放规则服务.是一乘二物品(itemEntry);

        while (remain > 0)
        {
            int addCount = Mathf.Min(remain, maxStack);
            bool isRotated = useOneByTwo && UnityEngine.Random.value < 0.3f;
            if (!useOneByTwo && TryAddToExistingChestStackRandom(itemEntry.itemId, icon, maxStack, ref addCount, chestData))
            {
                remain -= addCount;
                continue;
            }

            ItemSlotData data = new ItemSlotData
            {
                itemId = itemEntry.itemId,
                icon = icon,
                count = addCount,
                maxStack = maxStack
            };

            data.isRotated = isRotated;
            if (!TryFindRandomChestPlacement(data, chestData, out 宝箱候选格 placement))
            {
                Debug.LogWarning($"[宝箱内容] 宝箱 {chestSerial} 生成中断：没有位置放入物品 {itemEntry.itemId}。");
                return false;
            }

            data.isRotated = placement.isRotated;
            SetFootprintDataAt(SlotKind.Chest, placement.index, data);
            RefreshFootprintSlots(SlotKind.Chest, placement.index, data);
            remain -= addCount;
        }

        return true;
    }

    private bool TryAddToExistingChestStackRandom(
        string itemId,
        Sprite icon,
        int maxStack,
        ref int addCount,
        List<ItemSlotData> chestData)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < chestData.Count; i++)
        {
            ItemSlotData slot = chestData[i];
            if (slot.isFootprintExtension || slot.IsEmpty || slot.itemId != itemId)
            {
                continue;
            }

            int cap = Mathf.Max(1, slot.maxStack > 0 ? slot.maxStack : maxStack);
            if (slot.count < cap)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        int index = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        ItemSlotData target = chestData[index];
        int targetCap = Mathf.Max(1, target.maxStack > 0 ? target.maxStack : maxStack);
        int actualAdd = Mathf.Min(addCount, targetCap - target.count);
        target.count += actualAdd;
        target.icon = icon;
        target.maxStack = targetCap;
        chestData[index] = PrepareItemSlotDataForStorage(target, $"宝箱{仓储状态.当前宝箱序列号}格 {index}");
        RefreshChestSlot(index);
        addCount = actualAdd;
        return actualAdd > 0;
    }

    private bool TryFindRandomChestPlacement(
        ItemSlotData data,
        List<ItemSlotData> chestData,
        out 宝箱候选格 placement)
    {
        List<宝箱候选格> candidates = new List<宝箱候选格>();
        for (int i = 0; i < chestData.Count; i++)
        {
            if (CanPlaceDataAt(SlotKind.Chest, i, data, chestData))
            {
                candidates.Add(new 宝箱候选格 { index = i, isRotated = data.isRotated });
            }
        }

        if (candidates.Count == 0)
        {
            placement = default;
            return false;
        }

        placement = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
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
        return 仓储状态.获取角色装备数据(
            resolvedCharacterId,
            createIfMissing,
            GetExpectedEquipmentSlotCount(),
            ResolveItemEntry,
            ResolveDisplaySpriteFromPrefab,
            PrepareItemSlotDataForStorage);
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

    private List<ItemSlotData> GetCurrentEquipmentData(bool createIfMissing)
    {
        return 仓储状态.获取当前角色装备数据(
            ResolveEquipmentCharacterId(),
            createIfMissing,
            GetExpectedEquipmentSlotCount(),
            ResolveItemEntry,
            ResolveDisplaySpriteFromPrefab,
            PrepareItemSlotDataForStorage);
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
        return 槽位访问规则.FindFirstEmptySlotIndex(创建槽位访问上下文(), kind);
    }

    private int FindFirstAvailableSlotIndex(SlotKind kind, ItemDatabase.ItemEntry entry)
    {
        return 槽位访问规则.FindFirstAvailableSlotIndex(创建槽位访问上下文(), kind, entry);
    }

    private int FindFirstAvailableSlotIndex(SlotKind kind, ItemSlotData data)
    {
        return 槽位访问规则.FindFirstAvailableSlotIndex(创建槽位访问上下文(), kind, data);
    }

    private int FindRightClickEquipmentTargetIndex(ItemDatabase.ItemEntry entry)
    {
        return 槽位访问规则.FindRightClickEquipmentTargetIndex(创建槽位访问上下文(), entry, equipmentSlots);
    }

    private int FindEquipmentSlotIndex(ItemDatabase.EquipmentSlotType slotType, bool requireEmpty, ItemDatabase.ItemEntry entry = null)
    {
        return 槽位访问规则.FindEquipmentSlotIndex(创建槽位访问上下文(), slotType, requireEmpty, entry, equipmentSlots);
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
        return 槽位访问规则.TryGetSlotData(创建槽位访问上下文(), slot, out data);
    }

    private void SetSlotData(SlotRef slot, ItemSlotData data)
    {
        槽位访问规则.SetSlotData(创建槽位访问上下文(), slot, data);
    }

    private bool CanPlaceIntoTarget(ItemSlotData data, SlotRef target)
    {
        return 槽位访问规则.CanPlaceIntoTarget(创建槽位访问上下文(), data, target);
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
        switch (kind)
        {
            case SlotKind.Warehouse:
                return 仓储状态.仓库数据;
            case SlotKind.Backpack:
                return 仓储状态.背包数据;
            case SlotKind.Chest:
                return GetCurrentChestData(true);
            case SlotKind.Equipment:
                return GetCurrentEquipmentData(true);
            default:
                return null;
        }
    }

    private List<ItemSlotData> GetCurrentChestData(bool createIfMissing)
    {
        return 仓储状态.获取当前宝箱数据(createIfMissing);
    }

    private int GetCurrentChestSlotCount()
    {
        List<ItemSlotData> data = GetCurrentChestData(false);
        return data != null ? data.Count : 0;
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
            IsOneByTwoItem = 摆放规则服务.是一乘二物品,
            CloneItemSlotDataList = 仓储状态服务.克隆数据,
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

    private 槽位访问服务.Context 创建槽位访问上下文()
    {
        return new 槽位访问服务.Context
        {
            GetDataList = GetDataList,
            GetWidget = GetWidget,
            IsSlotUsable = IsSlotUsable,
            PrepareItemSlotDataForStorage = PrepareItemSlotDataForStorage,
            ResolveItemEntry = ResolveItemEntry,
            ResolvePrimarySlotIndex = ResolvePrimarySlotIndex,
            CanUseEquipmentSlotIndex = (index, slotType, requireEmpty, entry, equipmentData) =>
                摆放规则.可以使用装备槽位(创建摆放规则上下文(), index, slotType, requireEmpty, entry, equipmentData),
            CanPlaceItemAtIndex = (kind, primaryIndex, entry, dataList) =>
                摆放规则.可以放置物品到索引(创建摆放规则上下文(), kind, primaryIndex, entry, dataList),
            CanPlaceDataAt = CanPlaceDataAt,
            RebuildEquipmentFootprintOccupancy = RebuildEquipmentFootprintOccupancy,
            GetCurrentEquipmentData = () => GetCurrentEquipmentData(true)
        };
    }

    private 摆放规则服务.Context 创建摆放规则上下文()
    {
        return new 摆放规则服务.Context
        {
            GetDataList = GetDataList,
            IsSlotUsable = IsSlotUsable,
            GetOneByTwoExtensionIndex = GetOneByTwoExtensionIndex,
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
            ChestSlots = chestSlots,
            ExtraBackpackSlots = extraBackpackSlots,
            EquipmentSlots = equipmentSlots,
            ExtraEquipmentSlots = extraEquipmentSlots,
            WarehouseData = 仓储状态.仓库数据,
            BackpackData = 仓储状态.背包数据,
            ChestData = GetCurrentChestData(false),
            GetCurrentEquipmentData = () => GetCurrentEquipmentData(true),
            GetExpectedEquipmentSlotCount = GetExpectedEquipmentSlotCount,
            ResolveEquipmentCharacterId = ResolveEquipmentCharacterId,
            IsSlotUsable = IsSlotUsable,
            ShouldDisplayWarehouseItem = data => ShouldDisplayItem(warehouseFilter, data),
            ShouldDisplayBackpackItem = data => ShouldDisplayItem(backpackFilter, data),
            RefreshRuntimeWeaponModelForCharacter = RefreshRuntimeWeaponModelForCharacter,
            ResolveItemEntry = ResolveItemEntry,
            ResolveQualityBackgroundPrefab = ResolveQualityBackgroundPrefab,
            IsOneByTwoItem = 摆放规则服务.是一乘二物品,
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
            WarehouseData = 仓储状态.仓库数据,
            BackpackData = 仓储状态.背包数据,
            ChestData = GetCurrentChestData(false),
            WarehouseSlots = warehouseSlots,
            BackpackSlots = backpackSlots,
            ChestSlots = chestSlots,
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
            ChestSlots = chestSlots,
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
            IsOneByTwoItem = 摆放规则服务.是一乘二物品,
            GetDataList = GetDataList,
            CloneItemSlotDataList = 仓储状态服务.克隆数据,
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
            IsOneByTwoItem = 摆放规则服务.是一乘二物品,
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
        return 物品显示辅助服务.解析提示框物品图标(entry, FindChildByName, FindDescendantByName, ItemIconName);
    }

    private Vector2 ResolveTooltipItemIconSize(ItemDatabase.ItemEntry entry)
    {
        return 物品显示辅助服务.解析提示框物品图标尺寸(entry, itemTooltipItemIconImage, FindChildByName, FindDescendantByName, ItemIconName);
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

        if (kind == SlotKind.Chest)
        {
            return chestSlots;
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

        if (slot.kind == SlotKind.Chest)
        {
            RefreshChestSlot(slot.index);
            return;
        }

        RefreshEquipmentSlots();
    }

    private void RefreshAll()
    {
        仓储界面刷新规则.RefreshWarehouseFilteredView(创建仓储界面刷新上下文());
        仓储界面刷新规则.RefreshBackpackFilteredView(创建仓储界面刷新上下文());
        仓储界面刷新规则.RefreshChestFilteredView(创建仓储界面刷新上下文());
        RefreshEquipmentSlots();
    }

    private void SetUsableSlotCount(SlotKind kind, string characterId, int count)
    {
        仓储状态.设置可用槽位数量(kind, characterId, count);
        RefreshAll();
    }

    private int GetResolvedUsableSlotCount(SlotKind kind, string characterId)
    {
        int totalCount = GetSlotCountForKind(kind);
        return 仓储状态.获取可用槽位数量(kind, characterId, totalCount, BackpackLevelEventIds, BackpackLevelSlotCounts);
    }

    private int GetSlotCountForKind(SlotKind kind)
    {
        switch (kind)
        {
            case SlotKind.Warehouse:
                return Mathf.Max(warehouseSlots.Count, 仓储状态.仓库数据.Count);
            case SlotKind.Backpack:
                return Mathf.Max(backpackSlots.Count, 仓储状态.背包数据.Count);
            case SlotKind.Chest:
                return Mathf.Max(chestSlots.Count, GetCurrentChestSlotCount());
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

    private void RefreshWarehouseSlot(int index)
    {
        仓储界面刷新规则.RefreshWarehouseSlot(创建仓储界面刷新上下文(), index);
    }

    private void RefreshBackpackSlot(int index)
    {
        仓储界面刷新规则.RefreshBackpackSlot(创建仓储界面刷新上下文(), index);
    }

    private void RefreshChestSlot(int index)
    {
        仓储界面刷新规则.RefreshChestSlot(创建仓储界面刷新上下文(), index);
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

        物品显示辅助服务.设置提示框攻击力文本(entry, slot, attackPowerText, ResolveTooltipEquipmentOwnerCharacterId);
    }

    private TMP_Text EnsureTooltipAttackPowerText()
    {
        itemTooltipAttackPowerText = 物品显示辅助服务.确保提示框攻击力文本(
            itemTooltipAttackPowerText,
            itemTooltipTextContentRoot,
            itemTooltipFixedDamageText,
            itemTooltipAttributeMultiplierText,
            itemTooltipDescriptionText);
        return itemTooltipAttackPowerText;
    }

    private static string BuildTooltipLowerContentText(ItemDatabase.ItemEntry entry)
    {
        return 物品显示辅助服务.构建提示框下方面板文本(entry);
    }

    private string ResolveTooltipEquipmentOwnerCharacterId()
    {
        string activeCharacterId = ResolveEquipmentCharacterId();
        return string.IsNullOrWhiteSpace(activeCharacterId) ? string.Empty : activeCharacterId;
    }

    private string GetTooltipOwnerDisplayText(ItemDatabase.ItemEntry entry, SlotRef slot)
    {
        return 物品显示辅助服务.获取提示框装备者显示文本(entry, slot, ResolveTooltipEquipmentOwnerCharacterId);
    }

    private static Material EnsureItemTooltipIconFadeMaterial()
    {
        return 物品显示辅助服务.确保提示框图标渐隐材质();
    }

    private static string ResolveItemDisplayName(ItemDatabase.ItemEntry entry)
    {
        return 物品显示辅助服务.解析物品显示名(entry);
    }

    public static string GetItemDisplayName(string itemId)
    {
        return 物品显示辅助服务.解析物品显示名(ResolveItemEntry(itemId));
    }

    private static string GetFixedDamageDisplayText(ItemDatabase.ItemEntry entry)
    {
        return 物品显示辅助服务.获取固定伤害显示文本(entry);
    }

    private static string GetAttributeMultiplierDisplayText(ItemDatabase.ItemEntry entry)
    {
        return 物品显示辅助服务.获取属性倍率显示文本(entry);
    }

    private static string GetItemQualityDisplayName(ItemDatabase.ItemQuality quality)
    {
        return 物品显示辅助服务.获取物品品质显示名(quality);
    }

    private static string GetWeaponCategoryDisplayName(ItemDatabase.WeaponCategory weaponCategory)
    {
        return 物品显示辅助服务.获取武器类别显示名(weaponCategory);
    }

    private static Sprite ResolveDisplaySprite(ItemSlotData data)
    {
        return 物品显示辅助服务.解析显示图标(data, ResolveItemEntry, FindChildByName, FindDescendantByName);
    }

    private GameObject ResolveQualityBackgroundPrefab(ItemDatabase.ItemEntry entry)
    {
        return 物品显示辅助服务.解析品质背景预制体(qualityBackgroundPrefabCache, entry);
    }

    private static Sprite ResolveRuntimeIconSprite(SlotWidget widget)
    {
        return 物品显示辅助服务.解析运行时图标(widget);
    }

    private void CacheQualityBackgroundPrefabs()
    {
        物品显示辅助服务.缓存品质背景预制体(qualityBackgroundPrefabCache);
    }

    private static ItemDatabase.ItemEntry ResolveItemEntry(string itemId)
    {
        return 物品显示辅助服务.解析物品条目(itemId);
    }

    private static bool ShouldDisplayItem(CategoryFilterBinding binding, ItemSlotData data)
    {
        return 物品显示辅助服务.应显示物品(binding, data, ResolveItemEntry);
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
        仓储界面刷新规则.ClearRuntimeVisuals(chestSlots);
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
        chestSlots.Clear();
        extraBackpackSlots.Clear();
        equipmentSlots.Clear();
        extraEquipmentSlots.Clear();
    }

}









