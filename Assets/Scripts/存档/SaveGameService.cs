using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveGameService
{
    private const int CurrentVersion = 1;
    private const string DefaultSaveFileName = "save_1.json";
    private const string NewGameSceneName = "营地";

    public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, DefaultSaveFileName);

    public static bool HasDefaultSaveFile()
    {
        return File.Exists(DefaultSavePath);
    }

    public static SaveGameData CaptureCurrentState()
    {
        SaveGameData data = new SaveGameData
        {
            version = CurrentVersion,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            currentSceneName = SceneManager.GetActiveScene().name
        };

        CharacterSelectionState.CaptureSaveData(data.characterSelection);
        InventoryShortcutRuntimeBinder.CaptureSaveData(data.inventory);
        CharacterSkillRuntimeState.CaptureSaveData(data.skills);
        EventRuntimeState.CaptureSaveData(data.events);
        BattleBootstrap.CaptureSaveData(data.dungeon);
        return data;
    }

    public static void SaveDefaultSlot()
    {
        SaveGameData data = CaptureCurrentState();
        string json = JsonUtility.ToJson(data, true);
        Directory.CreateDirectory(Path.GetDirectoryName(DefaultSavePath));
        File.WriteAllText(DefaultSavePath, json, Encoding.UTF8);
        Debug.Log($"存档：已保存到 {DefaultSavePath}");
    }

    public static bool TryLoadDefaultSlot(out SaveGameData data, out string error)
    {
        data = null;
        error = string.Empty;

        if (!File.Exists(DefaultSavePath))
        {
            error = $"存档文件不存在：{DefaultSavePath}";
            return false;
        }

        string json = File.ReadAllText(DefaultSavePath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(json))
        {
            error = $"存档文件为空：{DefaultSavePath}";
            return false;
        }

        data = JsonUtility.FromJson<SaveGameData>(json);
        if (data == null)
        {
            error = $"存档解析失败：{DefaultSavePath}";
            return false;
        }

        if (data.version != CurrentVersion)
        {
            error = $"存档版本不匹配：存档={data.version}，当前={CurrentVersion}";
            return false;
        }

        return true;
    }

    public static bool LoadDefaultSlot()
    {
        if (!TryLoadDefaultSlot(out SaveGameData data, out string error))
        {
            Debug.LogError(error);
            return false;
        }

        ApplySaveData(data);
        string targetScene = string.IsNullOrWhiteSpace(data.currentSceneName)
            ? NewGameSceneName
            : data.currentSceneName.Trim();
        SceneManager.LoadScene(targetScene);
        return true;
    }

    public static void StartNewGame()
    {
        ResetRuntimeToDefaults();
        SceneManager.LoadScene(NewGameSceneName);
    }

    public static void ResetRuntimeToDefaults()
    {
        CharacterSelectionState.ResetSaveData();
        InventoryShortcutRuntimeBinder.ResetSaveData();
        CharacterSkillRuntimeState.ResetSaveData();
        EventRuntimeState.ResetSaveData();
        BattleBootstrap.ResetSaveData();
    }

    public static void ApplySaveData(SaveGameData data)
    {
        if (data == null)
        {
            Debug.LogError("存档：ApplySaveData 收到空数据。");
            return;
        }

        CharacterSelectionState.ApplySaveData(data.characterSelection);
        InventoryShortcutRuntimeBinder.ApplySaveData(data.inventory);
        CharacterSkillRuntimeState.ApplySaveData(data.skills);
        EventRuntimeState.ApplySaveData(data.events);
        BattleBootstrap.ApplySaveData(data.dungeon);
    }

    public static bool DeleteDefaultSlot()
    {
        if (!File.Exists(DefaultSavePath))
        {
            return false;
        }

        File.Delete(DefaultSavePath);
        return true;
    }

}
