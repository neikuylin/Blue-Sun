using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueConditionDatabase", menuName = "事件/对话条件数据库")]
public sealed class DialogueConditionDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueConditionDatabase";

    [Serializable]
    public sealed class ConditionDefinitionEntry
    {
        public string id = string.Empty;
        public int number;
    }

    [SerializeField] private List<ConditionDefinitionEntry> entries = new List<ConditionDefinitionEntry>();

    public List<ConditionDefinitionEntry> Entries => entries;

    public ConditionDefinitionEntry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            ConditionDefinitionEntry entry = entries[i];
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

    public ConditionDefinitionEntry GetOrCreateEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        ConditionDefinitionEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        ConditionDefinitionEntry created = new ConditionDefinitionEntry
        {
            id = resolvedId,
            number = 0
        };
        entries.Add(created);
        return created;
    }

    public static DialogueConditionDatabase LoadDefault()
    {
        return Resources.Load<DialogueConditionDatabase>(DefaultResourcePath);
    }
}
