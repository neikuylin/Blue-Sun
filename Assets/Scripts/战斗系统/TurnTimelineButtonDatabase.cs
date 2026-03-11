using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TurnTimelineButtonDatabase", menuName = "角色/回合时间轴按钮库")]
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

    public List<Entry> Entries => entries;

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

    public GameObject FindButtonPrefab(string characterId)
    {
        Entry entry = FindEntry(characterId);
        return entry != null ? entry.buttonPrefab : null;
    }

    public static TurnTimelineButtonDatabase LoadDefault()
    {
        return Resources.Load<TurnTimelineButtonDatabase>(DefaultResourcePath);
    }
}
