using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class 界面ID列表
{
    private const string CampSceneName = "营地";
    private const string PlayerCharacterId = "玩家";
    private const string OptionalTeammateEventPrefix = "可选队友：";

    private static BattleTurnSystem cachedBattleTurnSystem;
    private static string currentCharacterIdOverride = string.Empty;

    public static string 当前ID => 解析当前ID();

    public static List<string> 可选ID => 获取可选ID();

    public static string 构建调试文本()
    {
        if (IsCampScene())
        {
            return 构建营地调试文本();
        }

        List<string> selectableIds = 获取可选ID();
        string currentId = string.IsNullOrWhiteSpace(当前ID) ? "（空）" : 当前ID;
        string selectableText = selectableIds.Count == 0 ? "（空）" : string.Join(", ", selectableIds);
        return $"场景: {SceneManager.GetActiveScene().name}\n当前ID: {currentId}\n可选ID: {selectableText}";
    }

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
        currentCharacterIdOverride = string.Empty;
    }

    public static string 解析当前ID()
    {
        if (!string.IsNullOrWhiteSpace(currentCharacterIdOverride))
        {
            return currentCharacterIdOverride;
        }

        if (IsCampScene())
        {
            return 解析营地当前ID();
        }

        BattleTurnSystem turnSystem = 获取战斗回合系统();
        string displayedEquipmentCharacterId = InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId;
        if (turnSystem != null && !string.IsNullOrWhiteSpace(displayedEquipmentCharacterId))
        {
            return displayedEquipmentCharacterId;
        }

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

    public static void 设置当前ID(string characterId)
    {
        currentCharacterIdOverride = string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId.Trim();
    }

    public static void 清空当前ID()
    {
        currentCharacterIdOverride = string.Empty;
    }

    public static List<string> 获取可选ID()
    {
        if (IsCampScene())
        {
            return 获取营地可选ID();
        }

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

    private static string 解析营地当前ID()
    {
        List<string> selectableIds = 获取营地可选ID();
        if (!string.IsNullOrWhiteSpace(currentCharacterIdOverride) &&
            selectableIds.Contains(currentCharacterIdOverride))
        {
            return currentCharacterIdOverride;
        }

        return string.Empty;
    }

    public static void 设置营地当前ID(string characterId)
    {
        设置当前ID(characterId);
    }

    public static void 清空营地当前ID()
    {
        清空当前ID();
    }

    private static List<string> 获取营地可选ID()
    {
        List<string> result = new List<string> { PlayerCharacterId };
        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        if (eventDatabase == null || eventDatabase.Entries == null)
        {
            return result;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal) { PlayerCharacterId };
        for (int i = 0; i < eventDatabase.Entries.Count; i++)
        {
            EventDatabase.EventEntry entry = eventDatabase.Entries[i];
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.eventId) ||
                !entry.enabled ||
                !entry.eventId.StartsWith(OptionalTeammateEventPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string characterId = entry.eventId.Substring(OptionalTeammateEventPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(characterId) || !seen.Add(characterId))
            {
                continue;
            }

            result.Add(characterId);
        }

        return result;
    }

    private static bool IsCampScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && string.Equals(activeScene.name, CampSceneName, StringComparison.Ordinal);
    }

    private static string 构建营地调试文本()
    {
        List<string> selectableIds = 获取营地可选ID();
        List<string> enabledEventIds = new List<string>();
        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        if (eventDatabase != null && eventDatabase.Entries != null)
        {
            for (int i = 0; i < eventDatabase.Entries.Count; i++)
            {
                EventDatabase.EventEntry entry = eventDatabase.Entries[i];
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.eventId) ||
                    !entry.enabled ||
                    !entry.eventId.StartsWith(OptionalTeammateEventPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                enabledEventIds.Add(entry.eventId);
            }
        }

        string currentId = string.IsNullOrWhiteSpace(解析营地当前ID()) ? "（空）" : 解析营地当前ID();
        string eventText = enabledEventIds.Count == 0 ? "（无）" : string.Join(", ", enabledEventIds);
        string selectableText = selectableIds.Count == 0 ? "（空）" : string.Join(", ", selectableIds);
        return $"场景: {CampSceneName}\n当前ID: {currentId}\n已启用可选队友事件: {eventText}\n可选ID: {selectableText}";
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
