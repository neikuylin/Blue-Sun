using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 仓储状态服务
{
    private readonly List<InventoryShortcutRuntimeBinder.ItemSlotData> warehouseData = new List<InventoryShortcutRuntimeBinder.ItemSlotData>();
    private readonly List<InventoryShortcutRuntimeBinder.ItemSlotData> backpackData = new List<InventoryShortcutRuntimeBinder.ItemSlotData>();
    private readonly Dictionary<string, List<InventoryShortcutRuntimeBinder.ItemSlotData>> equipmentDataByCharacter =
        new Dictionary<string, List<InventoryShortcutRuntimeBinder.ItemSlotData>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<InventoryShortcutRuntimeBinder.ItemSlotData>> boundEnemyEquipmentDataCache =
        new Dictionary<string, List<InventoryShortcutRuntimeBinder.ItemSlotData>>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> equipmentUsableSlotCounts =
        new Dictionary<string, int>(StringComparer.Ordinal);

    private int warehouseUsableSlotCount = -1;
    private int backpackUsableSlotCount = -1;

    public List<InventoryShortcutRuntimeBinder.ItemSlotData> 仓库数据 => warehouseData;
    public List<InventoryShortcutRuntimeBinder.ItemSlotData> 背包数据 => backpackData;
    public IReadOnlyDictionary<string, List<InventoryShortcutRuntimeBinder.ItemSlotData>> 角色装备数据 => equipmentDataByCharacter;
    public IReadOnlyDictionary<string, int> 角色装备可用槽位数量 => equipmentUsableSlotCounts;

    public int 仓库可用槽位数量 => warehouseUsableSlotCount;
    public int 背包可用槽位数量 => backpackUsableSlotCount;

    public void 清空运行时状态()
    {
        warehouseData.Clear();
        backpackData.Clear();
        equipmentDataByCharacter.Clear();
        boundEnemyEquipmentDataCache.Clear();
        equipmentUsableSlotCounts.Clear();
        warehouseUsableSlotCount = -1;
        backpackUsableSlotCount = -1;
    }

    public static void 确保容量(List<InventoryShortcutRuntimeBinder.ItemSlotData> data, int size)
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

    public static List<InventoryShortcutRuntimeBinder.ItemSlotData> 克隆数据(List<InventoryShortcutRuntimeBinder.ItemSlotData> source)
    {
        return source != null ? new List<InventoryShortcutRuntimeBinder.ItemSlotData>(source) : new List<InventoryShortcutRuntimeBinder.ItemSlotData>();
    }

    public void 确保背包数据容量(int size)
    {
        while (backpackData.Count < size)
        {
            backpackData.Add(default);
        }
    }

    public void 确保仓库数据容量(int fixedStorageSlotCount)
    {
        while (warehouseData.Count < fixedStorageSlotCount)
        {
            warehouseData.Add(default);
        }
    }

    public List<string> 获取角色装备数据键列表()
    {
        List<string> result = new List<string>(equipmentDataByCharacter.Keys);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public List<InventoryShortcutRuntimeBinder.ItemSlotData> 获取角色装备数据(
        string characterId,
        bool createIfMissing,
        int expectedEquipmentSlotCount,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        Func<GameObject, Sprite> resolveDisplaySpriteFromPrefab,
        Func<InventoryShortcutRuntimeBinder.ItemSlotData, string, InventoryShortcutRuntimeBinder.ItemSlotData> prepareItemSlotDataForStorage)
    {
        string resolvedCharacterId = 规范化角色ID(characterId);
        if (equipmentDataByCharacter.TryGetValue(resolvedCharacterId, out List<InventoryShortcutRuntimeBinder.ItemSlotData> data))
        {
            return data;
        }

        if (!createIfMissing)
        {
            return 获取敌人绑定装备数据(resolvedCharacterId, expectedEquipmentSlotCount, resolveItemEntry, resolveDisplaySpriteFromPrefab, prepareItemSlotDataForStorage);
        }

        List<InventoryShortcutRuntimeBinder.ItemSlotData> boundEnemyEquipment = 获取敌人绑定装备数据(
            resolvedCharacterId,
            expectedEquipmentSlotCount,
            resolveItemEntry,
            resolveDisplaySpriteFromPrefab,
            prepareItemSlotDataForStorage);
        if (boundEnemyEquipment != null)
        {
            data = 克隆数据(boundEnemyEquipment);
            确保容量(data, Mathf.Max(expectedEquipmentSlotCount, data.Count));
            equipmentDataByCharacter[resolvedCharacterId] = data;
            return data;
        }

        data = new List<InventoryShortcutRuntimeBinder.ItemSlotData>();
        确保容量(data, expectedEquipmentSlotCount);
        equipmentDataByCharacter[resolvedCharacterId] = data;
        return data;
    }

    public List<InventoryShortcutRuntimeBinder.ItemSlotData> 获取当前角色装备数据(
        string currentCharacterId,
        bool createIfMissing,
        int expectedEquipmentSlotCount,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        Func<GameObject, Sprite> resolveDisplaySpriteFromPrefab,
        Func<InventoryShortcutRuntimeBinder.ItemSlotData, string, InventoryShortcutRuntimeBinder.ItemSlotData> prepareItemSlotDataForStorage)
    {
        if (string.IsNullOrWhiteSpace(currentCharacterId))
        {
            return null;
        }

        return 获取角色装备数据(
            currentCharacterId,
            createIfMissing,
            expectedEquipmentSlotCount,
            resolveItemEntry,
            resolveDisplaySpriteFromPrefab,
            prepareItemSlotDataForStorage);
    }

    public void 设置可用槽位数量(InventoryShortcutRuntimeBinder.SlotKind kind, string characterId, int count)
    {
        int normalized = Mathf.Max(0, count);
        switch (kind)
        {
            case InventoryShortcutRuntimeBinder.SlotKind.Warehouse:
                warehouseUsableSlotCount = normalized;
                break;
            case InventoryShortcutRuntimeBinder.SlotKind.Backpack:
                backpackUsableSlotCount = normalized;
                break;
            case InventoryShortcutRuntimeBinder.SlotKind.Equipment:
                equipmentUsableSlotCounts[规范化角色ID(characterId)] = normalized;
                break;
        }
    }

    public int 获取可用槽位数量(
        InventoryShortcutRuntimeBinder.SlotKind kind,
        string characterId,
        int totalCount,
        string[] backpackLevelEventIds,
        int[] backpackLevelSlotCounts)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        int configuredCount;
        switch (kind)
        {
            case InventoryShortcutRuntimeBinder.SlotKind.Warehouse:
                configuredCount = warehouseUsableSlotCount;
                break;
            case InventoryShortcutRuntimeBinder.SlotKind.Backpack:
                configuredCount = 解析背包可用槽位数量(totalCount, backpackLevelEventIds, backpackLevelSlotCounts);
                break;
            case InventoryShortcutRuntimeBinder.SlotKind.Equipment:
                if (!equipmentUsableSlotCounts.TryGetValue(规范化角色ID(characterId), out configuredCount))
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

    private List<InventoryShortcutRuntimeBinder.ItemSlotData> 获取敌人绑定装备数据(
        string characterId,
        int expectedEquipmentSlotCount,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        Func<GameObject, Sprite> resolveDisplaySpriteFromPrefab,
        Func<InventoryShortcutRuntimeBinder.ItemSlotData, string, InventoryShortcutRuntimeBinder.ItemSlotData> prepareItemSlotDataForStorage)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        if (boundEnemyEquipmentDataCache.TryGetValue(characterId, out List<InventoryShortcutRuntimeBinder.ItemSlotData> cachedData))
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

        int slotCount = Mathf.Max(expectedEquipmentSlotCount, EnemyEquipmentDatabase.SlotCount);
        List<InventoryShortcutRuntimeBinder.ItemSlotData> result = new List<InventoryShortcutRuntimeBinder.ItemSlotData>(slotCount);
        确保容量(result, slotCount);
        for (int i = 0; i < entry.itemIds.Count && i < result.Count; i++)
        {
            string itemId = entry.itemIds[i];
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            ItemDatabase.ItemEntry itemEntry = resolveItemEntry != null ? resolveItemEntry(itemId) : null;
            if (itemEntry == null || itemEntry.category != ItemDatabase.ItemCategory.Equipment)
            {
                continue;
            }

            result[i] = prepareItemSlotDataForStorage(new InventoryShortcutRuntimeBinder.ItemSlotData
            {
                itemId = itemId,
                icon = resolveDisplaySpriteFromPrefab != null ? resolveDisplaySpriteFromPrefab(itemEntry.prefab) : null,
                count = 1,
                maxStack = 1
            }, $"敌人装备栏 {characterId}:{i}");
        }

        boundEnemyEquipmentDataCache[characterId] = result;
        return result;
    }

    private int 解析背包可用槽位数量(int totalCount, string[] backpackLevelEventIds, int[] backpackLevelSlotCounts)
    {
        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        if (eventDatabase == null)
        {
            return backpackUsableSlotCount >= 0 ? backpackUsableSlotCount : totalCount;
        }

        bool foundAnyMatchingEvent = false;
        int resolvedCount = -1;
        for (int i = 0; i < backpackLevelEventIds.Length && i < backpackLevelSlotCounts.Length; i++)
        {
            EventDatabase.EventEntry entry = eventDatabase.FindEntry(backpackLevelEventIds[i]);
            if (entry == null)
            {
                continue;
            }

            foundAnyMatchingEvent = true;
            if (EventRuntimeState.IsEnabled(entry))
            {
                resolvedCount = backpackLevelSlotCounts[i];
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

    private static string 规范化角色ID(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? "玩家" : characterId.Trim();
    }
}
