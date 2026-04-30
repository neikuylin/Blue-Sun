using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventRuntimeState
{
    private static readonly Dictionary<string, bool> statesByEventId =
        new Dictionary<string, bool>(StringComparer.Ordinal);

    public static event Action<string, bool> StateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
        statesByEventId.Clear();
        StateChanged = null;
    }

    public static bool IsEnabled(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        string resolvedId = eventId.Trim();
        if (statesByEventId.TryGetValue(resolvedId, out bool runtimeValue))
        {
            return runtimeValue;
        }

        EventDatabase database = EventDatabase.LoadDefault();
        EventDatabase.EventEntry entry = database != null ? database.FindEntry(resolvedId) : null;
        return entry != null && entry.enabled;
    }

    public static bool IsEnabled(EventDatabase.EventEntry entry)
    {
        return entry != null && IsEnabled(entry.eventId);
    }

    public static void SetState(string eventId, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        string resolvedId = eventId.Trim();
        EventDatabase database = EventDatabase.LoadDefault();
        if (database == null || database.FindEntry(resolvedId) == null)
        {
            Debug.LogError($"事件运行时状态：事件 ID 不存在：{resolvedId}");
            return;
        }

        bool previousEnabled = IsEnabled(resolvedId);
        statesByEventId[resolvedId] = enabled;
        if (previousEnabled != enabled)
        {
            StateChanged?.Invoke(resolvedId, enabled);
        }
    }

    public static void CaptureSaveData(SaveGameData.EventSave target)
    {
        if (target == null)
        {
            return;
        }

        target.entries.Clear();
        EventDatabase database = EventDatabase.LoadDefault();
        if (database == null || database.Entries == null)
        {
            return;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            EventDatabase.EventEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.eventId))
            {
                continue;
            }

            target.entries.Add(new SaveGameData.EventStateSave
            {
                eventId = entry.eventId.Trim(),
                enabled = IsEnabled(entry.eventId)
            });
        }
    }

    public static void ApplySaveData(SaveGameData.EventSave source)
    {
        statesByEventId.Clear();
        if (source == null || source.entries == null)
        {
            return;
        }

        EventDatabase database = EventDatabase.LoadDefault();
        if (database == null)
        {
            Debug.LogError("事件运行时状态：缺少 EventDatabase，无法读档。");
            return;
        }

        for (int i = 0; i < source.entries.Count; i++)
        {
            SaveGameData.EventStateSave state = source.entries[i];
            if (state == null || string.IsNullOrWhiteSpace(state.eventId))
            {
                continue;
            }

            string resolvedId = state.eventId.Trim();
            if (database.FindEntry(resolvedId) == null)
            {
                Debug.LogError($"事件运行时状态：存档引用了不存在的事件 ID：{resolvedId}");
                continue;
            }

            statesByEventId[resolvedId] = state.enabled;
        }
    }

    public static void ResetSaveData()
    {
        statesByEventId.Clear();
    }
}
