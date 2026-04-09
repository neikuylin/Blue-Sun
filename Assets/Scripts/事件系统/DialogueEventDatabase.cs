using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueEventDatabase", menuName = "事件/对话事件数据库")]
public sealed class DialogueEventDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueEventDatabase";

    public enum TriggerType
    {
        Manual,
        SceneEnter,
        ClickObject,
        CampCharacter,
        BattleRoom,
        Interaction
    }

    [Serializable]
    public sealed class DialogueEventEntry
    {
        public string eventId = string.Empty;
        public string displayName = string.Empty;
        public bool enabled = true;
        public TriggerType triggerType = TriggerType.Manual;
        public string dialogueId = string.Empty;
        public string sceneName = string.Empty;
        public string targetId = string.Empty;
        public string speakerId = string.Empty;
        public bool playOnce;
        [TextArea(2, 5)] public string description = string.Empty;
        public List<string> tags = new List<string>();
    }

    [SerializeField] private List<DialogueEventEntry> entries = new List<DialogueEventEntry>();

    public List<DialogueEventEntry> Entries => entries;

    public DialogueEventEntry FindEntry(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            DialogueEventEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.eventId, eventId.Trim(), StringComparison.Ordinal))
            {
                EnsureEntry(entry);
                return entry;
            }
        }

        return null;
    }

    public DialogueEventEntry GetOrCreateEntry(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        string resolvedId = eventId.Trim();
        DialogueEventEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        DialogueEventEntry created = new DialogueEventEntry
        {
            eventId = resolvedId,
            displayName = resolvedId
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

        if (entry.tags == null)
        {
            entry.tags = new List<string>();
        }
    }

    public static DialogueEventDatabase LoadDefault()
    {
        return Resources.Load<DialogueEventDatabase>(DefaultResourcePath);
    }
}
