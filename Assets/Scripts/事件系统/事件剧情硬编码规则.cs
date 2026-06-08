using System;
using UnityEngine;

public static class 事件剧情硬编码规则
{
    public const string 出生剧情事件ID = "出生剧情";

    public static event Action<string> 请求播放剧情;

    public static bool 尝试从开始按钮播放出生剧情()
    {
        EventDatabase 数据库 = EventDatabase.LoadDefault();
        if (数据库 == null)
        {
            Debug.LogError("事件剧情硬编码规则：缺少 EventDatabase，无法从开始按钮播放出生剧情。");
            return false;
        }

        if (!EventRuntimeState.IsEnabled(出生剧情事件ID))
        {
            return false;
        }

        EventDatabase.EventEntry 事件条目 = 数据库.FindEntry(出生剧情事件ID);
        if (!尝试播放事件绑定剧情(事件条目))
        {
            return false;
        }

        return true;
    }

    private static bool 尝试播放事件绑定剧情(EventDatabase.EventEntry 事件条目)
    {
        string 剧情ID = 事件条目 != null ? 事件条目.boundStoryId : string.Empty;
        if (string.IsNullOrWhiteSpace(剧情ID))
        {
            Debug.LogWarning($"事件剧情硬编码规则：事件“{出生剧情事件ID}”已勾选，但这个事件没有绑定剧情。");
            return false;
        }

        if (请求播放剧情 == null)
        {
            Debug.LogWarning($"事件剧情硬编码规则：事件“{出生剧情事件ID}”请求播放剧情“{剧情ID}”，但当前没有剧情播放器接收这个请求。");
            return false;
        }

        请求播放剧情?.Invoke(剧情ID);
        return true;
    }
}
