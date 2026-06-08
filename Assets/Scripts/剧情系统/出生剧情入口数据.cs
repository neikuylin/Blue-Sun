using System;
using UnityEngine;

public static class 出生剧情入口数据
{
    public const string 剧情ID = "出生剧情";

    private const string 玩家ID = "玩家";
    private const string 库鲁斯ID = "库鲁斯";

    public static bool 尝试应用(剧情数据库.剧情条目 剧情, 剧情数据库.剧情蓝图节点 节点)
    {
        if (剧情 == null || 节点 == null)
        {
            return false;
        }

        if (节点.目标类型 != 剧情数据库.场景目标类型.战斗副本)
        {
            return false;
        }

        登记战斗副本入口(节点);

        if (!string.Equals(剧情.剧情ID, 剧情ID, StringComparison.Ordinal))
        {
            return true;
        }

        应用角色选择();
        return true;
    }

    public static void 登记战斗副本入口(剧情数据库.剧情蓝图节点 节点)
    {
        if (节点 == null)
        {
            return;
        }

        副本选择状态.选择地图模板(节点.地图模板ID);
        BattleBootstrap.SetCurrentRoom(节点.地图模板ID, 节点.房间节点ID);
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
}
