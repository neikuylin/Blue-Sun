using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueEventDatabase", menuName = "事件/对话事件数据库")]
public sealed class DialogueEventDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueEventDatabase";

    [Serializable]
    public sealed class DialogueEventEntry
    {
        public string id = string.Empty;
        public PresentationData presentation = new PresentationData();
        public TriggerData trigger = new TriggerData();
        public ConditionData condition = new ConditionData();
    }

    [Serializable]
    public sealed class PresentationData
    {
        public string dialogueContentId = string.Empty;
    }

    [Serializable]
    public sealed class TriggerData
    {
        public List<TriggerEventEntry> eventIds = new List<TriggerEventEntry>();
    }

    [Serializable]
    public sealed class TriggerEventEntry
    {
        public string eventId = string.Empty;
        public bool expectedValue = true;
    }

    [Serializable]
    public sealed class ConditionData
    {
        public List<ConditionEntry> eventIds = new List<ConditionEntry>();
    }

    [Serializable]
    public sealed class ConditionEntry
    {
        public string eventId = string.Empty;
        public int number;
    }

    [SerializeField] private List<DialogueEventEntry> entries = new List<DialogueEventEntry>();

    public List<DialogueEventEntry> Entries => entries;

    public DialogueEventEntry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            DialogueEventEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.id, resolvedId, StringComparison.Ordinal))
            {
                EnsureEntry(entry);
                return entry;
            }
        }

        return null;
    }

    public DialogueEventEntry GetOrCreateEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        DialogueEventEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        DialogueEventEntry created = new DialogueEventEntry
        {
            id = resolvedId
        };
        EnsureEntry(created);
        entries.Add(created);
        return created;
    }

    public static void EnsureEntry(DialogueEventEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.presentation == null)
        {
            entry.presentation = new PresentationData();
        }

        if (entry.trigger == null)
        {
            entry.trigger = new TriggerData();
        }

        if (entry.condition == null)
        {
            entry.condition = new ConditionData();
        }

        if (entry.trigger.eventIds == null)
        {
            entry.trigger.eventIds = new List<TriggerEventEntry>();
        }

        if (entry.condition.eventIds == null)
        {
            entry.condition.eventIds = new List<ConditionEntry>();
        }
    }

    public static DialogueEventDatabase LoadDefault()
    {
        return Resources.Load<DialogueEventDatabase>(DefaultResourcePath);
    }
}
