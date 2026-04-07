using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class 界面ID列表
{
    private static BattleTurnSystem cachedBattleTurnSystem;

    public static string 当前ID => 解析当前ID();

    public static List<string> 可选ID => 获取可选ID();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void 初始化()
    {
        SceneManager.sceneLoaded -= 处理场景加载;
        SceneManager.sceneLoaded += 处理场景加载;
        cachedBattleTurnSystem = 查找战斗回合系统();
    }

    private static void 处理场景加载(Scene scene, LoadSceneMode mode)
    {
        cachedBattleTurnSystem = 查找战斗回合系统();
    }

    public static string 解析当前ID()
    {
        string displayedCharacterId = InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId;
        if (!string.IsNullOrWhiteSpace(displayedCharacterId))
        {
            return displayedCharacterId;
        }

        BattleTurnSystem turnSystem = 获取战斗回合系统();
        BattleUnit activeUnit = turnSystem != null ? turnSystem.ActiveUnit : null;
        if (activeUnit != null &&
            activeUnit.IsAlive &&
            activeUnit.isPlayerControlled &&
            !string.IsNullOrWhiteSpace(activeUnit.characterId))
        {
            return activeUnit.characterId;
        }

        string activeCharacterId = CharacterSelectionState.ActiveCharacterId;
        if (!string.IsNullOrWhiteSpace(activeCharacterId))
        {
            return activeCharacterId;
        }

        List<string> selectableIds = 获取可选ID();
        return selectableIds.Count > 0 ? selectableIds[0] : string.Empty;
    }

    public static List<string> 获取可选ID()
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<CharacterSelectionState.SlotSelection> slotSelections = CharacterSelectionState.SlotSelections;
        for (int i = 0; i < slotSelections.Count; i++)
        {
            string characterId = slotSelections[i].characterId;
            if (string.IsNullOrWhiteSpace(characterId) || !seen.Add(characterId))
            {
                continue;
            }

            result.Add(characterId);
        }

        return result;
    }

    private static BattleTurnSystem 获取战斗回合系统()
    {
        if (cachedBattleTurnSystem == null)
        {
            cachedBattleTurnSystem = 查找战斗回合系统();
        }

        return cachedBattleTurnSystem;
    }

    private static BattleTurnSystem 查找战斗回合系统()
    {
        return UnityEngine.Object.FindObjectOfType<BattleTurnSystem>(true);
    }
}
