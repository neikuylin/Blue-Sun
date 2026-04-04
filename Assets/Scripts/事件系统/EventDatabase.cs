using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "事件/事件数据库")]
public sealed class EventDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "EventDatabase";

    [Serializable]
    public sealed class EventEntry
    {
        public string eventId = string.Empty;
        public string displayName = string.Empty;
        public bool enabled = true;
        [TextArea(2, 5)] public string description = string.Empty;
    }

    [SerializeField] private List<EventEntry> entries = new List<EventEntry>();

    public List<EventEntry> Entries => entries;

    public EventEntry FindEntry(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            EventEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.eventId, eventId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public EventEntry GetOrCreateEntry(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        string resolvedId = eventId.Trim();
        EventEntry entry = FindEntry(resolvedId);
        if (entry != null)
        {
            return entry;
        }

        entry = new EventEntry
        {
            eventId = resolvedId,
            displayName = resolvedId,
            enabled = true
        };
        entries.Add(entry);
        return entry;
    }

    public bool IsEventEnabled(string eventId)
    {
        EventEntry entry = FindEntry(eventId);
        return entry != null && entry.enabled;
    }

    public static EventDatabase LoadDefault()
    {
        return Resources.Load<EventDatabase>(DefaultResourcePath);
    }
}
