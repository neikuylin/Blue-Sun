using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueContentDatabase", menuName = "事件/对话内容数据")]
public sealed class DialogueContentDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "DialogueContentDatabase";

    public enum DialogueViewSide
    {
        Main,
        Secondary
    }

    public enum InteractionType
    {
        Button,
        JumpToDialogueGroup,
        ContinueDialogue
    }

    [Serializable]
    public sealed class InteractionEntry
    {
        public string buttonText = string.Empty;
        public InteractionType interactionType = InteractionType.ContinueDialogue;
        public string identifierId = string.Empty;
        public string targetDialogueGroupId = string.Empty;
    }

    [Serializable]
    public sealed class DialogueContentEntry
    {
        public string id = string.Empty;
        public string roleNameId = string.Empty;
        public GameObject portraitPrefab;
        public DialogueViewSide viewSide = DialogueViewSide.Main;
        [TextArea(3, 8)] public string content = string.Empty;
        [HideInInspector] public AudioClip voiceClip;
        [HideInInspector] public List<InteractionEntry> interactions = new List<InteractionEntry>();
    }

    [SerializeField] private List<DialogueContentEntry> entries = new List<DialogueContentEntry>();

    public List<DialogueContentEntry> Entries => entries;

    public DialogueContentEntry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            DialogueContentEntry entry = entries[i];
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

    public DialogueContentEntry GetOrCreateEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string resolvedId = id.Trim();
        DialogueContentEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            EnsureEntry(existing);
            return existing;
        }

        DialogueContentEntry created = new DialogueContentEntry
        {
            id = resolvedId
        };
        EnsureEntry(created);
        entries.Add(created);
        return created;
    }

    public static void EnsureEntry(DialogueContentEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.interactions == null)
        {
            entry.interactions = new List<InteractionEntry>();
        }
    }

    public static DialogueContentDatabase LoadDefault()
    {
        return Resources.Load<DialogueContentDatabase>(DefaultResourcePath);
    }
}
