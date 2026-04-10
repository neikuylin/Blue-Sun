using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueGroupDatabase", menuName = "事件/对话组数据库")]
public sealed class DialogueGroupDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueGroupDatabase";

    [Serializable]
    public sealed class DialogueGroupEntry
    {
        public string id = string.Empty;
        public List<string> contentIds = new List<string>();
    }

    [SerializeField] private List<DialogueGroupEntry> entries = new List<DialogueGroupEntry>();

    public List<DialogueGroupEntry> Entries => entries;

    public DialogueGroupEntry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            DialogueGroupEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.id, resolvedId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public DialogueGroupEntry GetOrCreateEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        DialogueGroupEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        DialogueGroupEntry created = new DialogueGroupEntry
        {
            id = resolvedId
        };
        EnsureEntry(created);
        entries.Add(created);
        return created;
    }

    public static void EnsureEntry(DialogueGroupEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.contentIds == null)
        {
            entry.contentIds = new List<string>();
        }
    }

    public static DialogueGroupDatabase LoadDefault()
    {
        return Resources.Load<DialogueGroupDatabase>(DefaultResourcePath);
    }
}
