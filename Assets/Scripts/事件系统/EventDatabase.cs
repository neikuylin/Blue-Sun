using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "事件/事件数据库")]
public sealed class EventDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "EventDatabase";
    public const string CampCharacterCategoryId = "营地角色";
    public const string OptionalTeammateCategoryId = "可选队友";
    public const string BackpackLevelCategoryId = "背包等级";
    public const string StoryCategoryId = "剧情";

    [Serializable]
    public sealed class EventEntry
    {
        public string eventId = string.Empty;
        public string displayName = string.Empty;
        public string categoryId = string.Empty;
        public string boundStoryId = string.Empty;
        public bool enabled = true;
        [TextArea(2, 5)] public string description = string.Empty;
    }

    [Serializable]
    public sealed class EventCategory
    {
        public string categoryId = string.Empty;
        public string displayName = string.Empty;
    }

    [SerializeField] private List<EventCategory> categories = new List<EventCategory>();
    [SerializeField] private List<EventEntry> entries = new List<EventEntry>();

    public List<EventCategory> Categories => categories;
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
            categoryId = ResolveDefaultCategoryId(resolvedId),
            enabled = true
        };
        entries.Add(entry);
        return entry;
    }

    public bool IsEventEnabled(string eventId)
    {
        EventEntry entry = FindEntry(eventId);
        return entry != null && EventRuntimeState.IsEnabled(entry);
    }

    public static EventDatabase LoadDefault()
    {
        return Resources.Load<EventDatabase>(DefaultResourcePath);
    }

    public bool EnsureCategoryList()
    {
        bool changed = false;
        if (categories == null)
        {
            categories = new List<EventCategory>();
            changed = true;
        }

        changed |= EnsureCategory(CampCharacterCategoryId, CampCharacterCategoryId);
        changed |= EnsureCategory(OptionalTeammateCategoryId, OptionalTeammateCategoryId);
        changed |= EnsureCategory(BackpackLevelCategoryId, BackpackLevelCategoryId);
        changed |= EnsureCategory(StoryCategoryId, StoryCategoryId);

        if (entries == null)
        {
            entries = new List<EventEntry>();
            return true;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            EventEntry entry = entries[i];
            if (entry == null || !string.IsNullOrWhiteSpace(entry.categoryId))
            {
                continue;
            }

            entry.categoryId = ResolveDefaultCategoryId(entry.eventId);
            changed = true;
        }

        return changed;
    }

    public static string ResolveDefaultCategoryId(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return BackpackLevelCategoryId;
        }

        if (eventId.StartsWith("营地角色：", StringComparison.Ordinal))
        {
            return CampCharacterCategoryId;
        }

        if (eventId.StartsWith("可选队友：", StringComparison.Ordinal))
        {
            return OptionalTeammateCategoryId;
        }

        return BackpackLevelCategoryId;
    }

    private bool EnsureCategory(string categoryId, string displayName)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            EventCategory category = categories[i];
            if (category != null && string.Equals(category.categoryId, categoryId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        categories.Add(new EventCategory
        {
            categoryId = categoryId,
            displayName = displayName
        });
        return true;
    }
}
