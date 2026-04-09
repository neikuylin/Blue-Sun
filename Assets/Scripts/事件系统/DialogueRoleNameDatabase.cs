using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueRoleNameDatabase", menuName = "事件/对话角色名字数据库")]
public sealed class DialogueRoleNameDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueRoleNameDatabase";

    [Serializable]
    public sealed class RoleNameEntry
    {
        public string id = string.Empty;
    }

    [SerializeField] private List<RoleNameEntry> entries = new List<RoleNameEntry>();

    public List<RoleNameEntry> Entries => entries;

    public RoleNameEntry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            RoleNameEntry entry = entries[i];
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

    public RoleNameEntry GetOrCreateEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        RoleNameEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        RoleNameEntry created = new RoleNameEntry
        {
            id = resolvedId
        };
        entries.Add(created);
        return created;
    }

    public static DialogueRoleNameDatabase LoadDefault()
    {
        return Resources.Load<DialogueRoleNameDatabase>(DefaultResourcePath);
    }
}
