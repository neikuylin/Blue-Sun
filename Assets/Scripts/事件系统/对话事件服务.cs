using System;
using System.Collections.Generic;
using UnityEngine;

internal static class 对话事件服务
{
    internal sealed class 触发监听项
    {
        public string 对话事件ID = string.Empty;
        public string 事件ID = string.Empty;
        public bool 目标值;
    }

    public static void 绑定事件触发(DialogueEventDatabase.DialogueEventEntry entry, List<触发监听项> 事件触发监听项, Dictionary<string, bool> 上次事件状态)
    {
        if (entry == null || entry.trigger == null || entry.trigger.eventIds == null)
        {
            return;
        }

        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        for (int i = 0; i < entry.trigger.eventIds.Count; i++)
        {
            DialogueEventDatabase.TriggerEventEntry triggerEntry = entry.trigger.eventIds[i];
            if (triggerEntry == null || string.IsNullOrWhiteSpace(triggerEntry.eventId))
            {
                continue;
            }

            string eventId = triggerEntry.eventId.Trim();
            事件触发监听项.Add(new 触发监听项
            {
                对话事件ID = entry.id,
                事件ID = eventId,
                目标值 = triggerEntry.expectedValue
            });

            上次事件状态[eventId] = 解析事件状态(eventDatabase, eventId);
        }
    }

    public static void 更新事件触发(
        List<触发监听项> 事件触发监听项,
        Dictionary<string, bool> 上次事件状态,
        Action<string> triggerDialogueEvent)
    {
        if (事件触发监听项 == null || 事件触发监听项.Count == 0)
        {
            return;
        }

        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        for (int i = 0; i < 事件触发监听项.Count; i++)
        {
            触发监听项 item = 事件触发监听项[i];
            if (item == null || string.IsNullOrWhiteSpace(item.事件ID))
            {
                continue;
            }

            bool currentValue = 解析事件状态(eventDatabase, item.事件ID);
            if (!上次事件状态.TryGetValue(item.事件ID, out bool previousValue))
            {
                上次事件状态[item.事件ID] = currentValue;
                continue;
            }

            if (previousValue == currentValue)
            {
                continue;
            }

            上次事件状态[item.事件ID] = currentValue;
            if (currentValue == item.目标值)
            {
                triggerDialogueEvent?.Invoke(item.对话事件ID);
            }
        }
    }

    public static DialogueEventDatabase.DialogueEventEntry 获取对话事件(string dialogueEventId)
    {
        if (string.IsNullOrWhiteSpace(dialogueEventId))
        {
            Debug.LogError("对话运行时: 对话事件ID为空。");
            return null;
        }

        DialogueEventDatabase eventDatabase = DialogueEventDatabase.LoadDefault();
        if (eventDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueEventDatabase。");
            return null;
        }

        DialogueEventDatabase.DialogueEventEntry eventEntry = eventDatabase.FindEntry(dialogueEventId);
        if (eventEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到对话事件 '{dialogueEventId}'。");
            return null;
        }

        return eventEntry;
    }

    public static bool 满足条件(DialogueEventDatabase.DialogueEventEntry eventEntry)
    {
        if (eventEntry == null || eventEntry.condition == null || eventEntry.condition.eventIds == null)
        {
            return true;
        }

        DialogueConditionDatabase conditionDatabase = DialogueConditionDatabase.LoadDefault();
        if (conditionDatabase == null && eventEntry.condition.eventIds.Count > 0)
        {
            Debug.LogError($"对话运行时: 对话事件 '{eventEntry.id}' 需要 DialogueConditionDatabase，但资源缺失。");
            return false;
        }

        for (int i = 0; i < eventEntry.condition.eventIds.Count; i++)
        {
            DialogueEventDatabase.ConditionEntry conditionEntry = eventEntry.condition.eventIds[i];
            if (conditionEntry == null || string.IsNullOrWhiteSpace(conditionEntry.eventId))
            {
                continue;
            }

            DialogueConditionDatabase.ConditionDefinitionEntry definition = conditionDatabase.FindEntry(conditionEntry.eventId);
            if (definition == null)
            {
                Debug.LogError($"对话运行时: 找不到条件定义 '{conditionEntry.eventId}'。");
                return false;
            }

            if (definition.number != conditionEntry.number)
            {
                return false;
            }
        }

        return true;
    }

    public static DialogueGroupDatabase.DialogueGroupEntry 获取对话组(DialogueEventDatabase.DialogueEventEntry eventEntry)
    {
        if (eventEntry == null || eventEntry.presentation == null || string.IsNullOrWhiteSpace(eventEntry.presentation.dialogueGroupId))
        {
            Debug.LogError($"对话运行时: 对话事件 '{eventEntry?.id ?? "<null>"}' 缺少表现配置。");
            return null;
        }

        DialogueGroupDatabase groupDatabase = DialogueGroupDatabase.LoadDefault();
        if (groupDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueGroupDatabase。");
            return null;
        }

        DialogueGroupDatabase.DialogueGroupEntry groupEntry = groupDatabase.FindEntry(eventEntry.presentation.dialogueGroupId);
        if (groupEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到对话组 '{eventEntry.presentation.dialogueGroupId}'。");
            return null;
        }

        if (groupEntry.contentIds == null || groupEntry.contentIds.Count == 0)
        {
            Debug.LogError($"对话运行时: 对话组 '{groupEntry.id}' 没有内容ID。");
            return null;
        }

        return groupEntry;
    }

    public static bool 解析事件状态(EventDatabase database, string eventId)
    {
        if (database == null || string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        EventDatabase.EventEntry entry = database.FindEntry(eventId);
        return entry != null && EventRuntimeState.IsEnabled(entry);
    }
}
