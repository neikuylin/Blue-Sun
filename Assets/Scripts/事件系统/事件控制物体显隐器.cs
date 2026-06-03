using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("事件/事件控制物体显隐器")]
public sealed class 事件控制物体显隐器 : MonoBehaviour
{
    [SerializeField] private string 事件ID = string.Empty;
    [SerializeField] private GameObject 目标物体;

    private bool hasReportedMissingEventId;
    private bool hasReportedMissingTarget;
    private bool hasReportedMissingEventEntry;

    private void OnEnable()
    {
        EventRuntimeState.StateChanged += 处理事件状态变化;
        应用当前事件状态();
    }

    private void OnDisable()
    {
        EventRuntimeState.StateChanged -= 处理事件状态变化;
    }

    private void 处理事件状态变化(string eventId, bool enabled)
    {
        string resolvedId = 解析事件ID();
        if (string.IsNullOrEmpty(resolvedId) || !string.Equals(eventId, resolvedId, System.StringComparison.Ordinal))
        {
            return;
        }

        应用目标显隐(enabled);
    }

    private void 应用当前事件状态()
    {
        string resolvedId = 解析事件ID();
        if (string.IsNullOrEmpty(resolvedId))
        {
            if (!hasReportedMissingEventId)
            {
                hasReportedMissingEventId = true;
                Debug.LogWarning($"{name}：事件控制物体显隐器没有填写事件ID。", this);
            }

            return;
        }

        EventDatabase database = EventDatabase.LoadDefault();
        if (database == null || database.FindEntry(resolvedId) == null)
        {
            if (!hasReportedMissingEventEntry)
            {
                hasReportedMissingEventEntry = true;
                Debug.LogWarning($"{name}：事件控制物体显隐器找不到事件ID：{resolvedId}", this);
            }

            return;
        }

        应用目标显隐(EventRuntimeState.IsEnabled(resolvedId));
    }

    private void 应用目标显隐(bool visible)
    {
        if (目标物体 == null)
        {
            if (!hasReportedMissingTarget)
            {
                hasReportedMissingTarget = true;
                Debug.LogWarning($"{name}：事件控制物体显隐器没有绑定目标物体。", this);
            }

            return;
        }

        目标物体.SetActive(visible);
    }

    private string 解析事件ID()
    {
        return string.IsNullOrWhiteSpace(事件ID) ? string.Empty : 事件ID.Trim();
    }
}
