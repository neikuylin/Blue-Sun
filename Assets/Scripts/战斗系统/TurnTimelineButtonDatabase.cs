using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TurnTimelineButtonDatabase", menuName = "角色/回合时间轴头像库")]
public sealed class TurnTimelineButtonDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "TurnTimelineButtonDatabase";

    [Serializable]
    public sealed class Entry
    {
        public string characterId;
        public GameObject buttonPrefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    [SerializeField] private GameObject defaultButtonPrefab;

    public List<Entry> Entries => entries;
    public GameObject DefaultButtonPrefab => defaultButtonPrefab;

    public Entry FindEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public GameObject FindAvatarPrefab(string characterId)
    {
        Entry entry = FindEntry(characterId);
        if (entry != null && entry.buttonPrefab != null)
        {
            return entry.buttonPrefab;
        }

        return defaultButtonPrefab;
    }

    public GameObject FindButtonPrefab(string characterId)
    {
        return FindAvatarPrefab(characterId);
    }

    public static TurnTimelineButtonDatabase LoadDefault()
    {
        return Resources.Load<TurnTimelineButtonDatabase>(DefaultResourcePath);
    }
}
