using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueContentDatabase", menuName = "事件/对话内容数据库")]
public sealed class DialogueContentDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueContentDatabase";

    public enum NodeType
    {
        Line,
        Choice,
        Jump,
        End
    }

    [Serializable]
    public sealed class DialogueChoiceEntry
    {
        public string choiceId = string.Empty;
        public string text = string.Empty;
        public string nextNodeId = string.Empty;
    }

    [Serializable]
    public sealed class DialogueNodeEntry
    {
        public string nodeId = string.Empty;
        public string speakerId = string.Empty;
        public string speakerName = string.Empty;
        public NodeType nodeType = NodeType.Line;
        [TextArea(3, 8)] public string content = string.Empty;
        public Sprite portraitSprite;
        public GameObject dialoguePrefab;
        public AudioClip voiceClip;
        public string nextNodeId = string.Empty;
        public List<DialogueChoiceEntry> choices = new List<DialogueChoiceEntry>();
        public List<string> tags = new List<string>();
        [TextArea(2, 4)] public string note = string.Empty;
    }

    [Serializable]
    public sealed class DialogueEntry
    {
        public string dialogueId = string.Empty;
        public string displayName = string.Empty;
        public string openingNodeId = string.Empty;
        [TextArea(2, 4)] public string description = string.Empty;
        public List<DialogueNodeEntry> nodes = new List<DialogueNodeEntry>();
        public List<string> tags = new List<string>();
    }

    [SerializeField] private List<DialogueEntry> entries = new List<DialogueEntry>();

    public List<DialogueEntry> Entries => entries;

    public DialogueEntry FindEntry(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            DialogueEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.dialogueId, dialogueId.Trim(), StringComparison.Ordinal))
            {
                EnsureEntry(entry);
                return entry;
            }
        }

        return null;
    }

    public DialogueEntry GetOrCreateEntry(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId))
        {
            return null;
        }

        string resolvedId = dialogueId.Trim();
        DialogueEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        DialogueEntry created = new DialogueEntry
        {
            dialogueId = resolvedId,
            displayName = resolvedId,
            openingNodeId = "node_start"
        };
        EnsureEntry(created);
        if (created.nodes.Count == 0)
        {
            created.nodes.Add(new DialogueNodeEntry { nodeId = created.openingNodeId });
        }

        entries.Add(created);
        return created;
    }

    public static void EnsureEntry(DialogueEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.nodes == null)
        {
            entry.nodes = new List<DialogueNodeEntry>();
        }

        if (entry.tags == null)
        {
            entry.tags = new List<string>();
        }

        for (int i = 0; i < entry.nodes.Count; i++)
        {
            EnsureNode(entry.nodes[i]);
        }
    }

    public static void EnsureNode(DialogueNodeEntry node)
    {
        if (node == null)
        {
            return;
        }

        if (node.choices == null)
        {
            node.choices = new List<DialogueChoiceEntry>();
        }

        if (node.tags == null)
        {
            node.tags = new List<string>();
        }
    }

    public static DialogueContentDatabase LoadDefault()
    {
        return Resources.Load<DialogueContentDatabase>(DefaultResourcePath);
    }
}
