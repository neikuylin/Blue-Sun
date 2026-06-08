using System;
using System.Collections.Generic;
using UnityEngine;

public static class 出生剧情入口数据
{
    public const string 剧情ID = "出生剧情";

    private const string 玩家ID = "玩家";
    private const string 库鲁斯ID = "库鲁斯";
    private const string 库鲁斯主手武器ID = "itm_直剑";
    private const int 装备槽数量 = 8;
    private const int 主手槽索引 = 6;

    public static bool 尝试应用(剧情数据库.剧情条目 剧情, 剧情数据库.剧情步骤 步骤)
    {
        if (剧情 == null || 步骤 == null)
        {
            return false;
        }

        if (步骤.目标类型 != 剧情数据库.场景目标类型.战斗副本)
        {
            return false;
        }

        登记战斗副本入口(步骤);

        if (!string.Equals(剧情.剧情ID, 剧情ID, StringComparison.Ordinal))
        {
            return true;
        }

        应用角色选择();
        应用库鲁斯主手直剑();
        return true;
    }

    public static void 登记战斗副本入口(剧情数据库.剧情步骤 步骤)
    {
        if (步骤 == null)
        {
            return;
        }

        副本选择状态.选择地图模板(步骤.地图模板ID);
        BattleBootstrap.SetCurrentRoom(步骤.地图模板ID, 步骤.房间节点ID);
    }

    private static void 应用角色选择()
    {
        SaveGameData.CharacterSelectionSave selection = new SaveGameData.CharacterSelectionSave
        {
            activeCharacterId = 玩家ID
        };

        selection.slots.Add(new SaveGameData.CharacterSlotSave
        {
            slotName = "剧情玩家栏位1",
            characterId = 玩家ID,
            isMainSlot = true,
            isActiveSlot = true
        });

        selection.slots.Add(new SaveGameData.CharacterSlotSave
        {
            slotName = "剧情玩家栏位2",
            characterId = 库鲁斯ID,
            isMainSlot = false,
            isActiveSlot = false
        });

        CharacterSelectionState.ApplySaveData(selection);
        界面ID列表.设置当前ID(玩家ID);
    }

    private static void 应用库鲁斯主手直剑()
    {
        if (!物品存在(库鲁斯主手武器ID))
        {
            Debug.LogWarning($"出生剧情入口数据：找不到物品“{库鲁斯主手武器ID}”，无法给库鲁斯主手装备直剑。");
            return;
        }

        SaveGameData.InventorySave inventory = new SaveGameData.InventorySave
        {
            warehouseUsableSlotCount = -1,
            backpackUsableSlotCount = -1
        };

        SaveGameData.CharacterEquipmentSave equipment = new SaveGameData.CharacterEquipmentSave
        {
            characterId = 库鲁斯ID,
            slots = 创建空装备槽()
        };

        equipment.slots[主手槽索引] = new SaveGameData.ItemSlotSave
        {
            itemId = 库鲁斯主手武器ID,
            count = 1,
            maxStack = 1,
            primarySlotIndex = -1
        };

        inventory.equipmentByCharacter.Add(equipment);
        inventory.equipmentUsableSlotCounts.Add(new SaveGameData.CharacterSlotCountSave
        {
            characterId = 库鲁斯ID,
            usableSlotCount = 装备槽数量
        });

        InventoryShortcutRuntimeBinder.ApplySaveData(inventory);
    }

    private static List<SaveGameData.ItemSlotSave> 创建空装备槽()
    {
        List<SaveGameData.ItemSlotSave> slots = new List<SaveGameData.ItemSlotSave>(装备槽数量);
        for (int i = 0; i < 装备槽数量; i++)
        {
            slots.Add(new SaveGameData.ItemSlotSave
            {
                primarySlotIndex = -1
            });
        }

        return slots;
    }

    private static bool 物品存在(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        ItemDatabase database = ItemDatabase.LoadDefault();
        return database != null && database.FindEntry(itemId) != null;
    }
}
