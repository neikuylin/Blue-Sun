using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomTypeDatabase", menuName = "战斗/房间类型库")]
public sealed class RoomTypeDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "RoomTypeDatabase";
    public const string EncounterBattleTypeId = "encounter_battle";
    public const string EncounterBattleTypeName = "遭遇战房间";

    [Serializable]
    public sealed class RoomTypeEntry
    {
        public string roomTypeId = string.Empty;
        public string displayName = string.Empty;
    }

    [SerializeField] private List<RoomTypeEntry> entries = new List<RoomTypeEntry>();

    public List<RoomTypeEntry> Entries => entries;

    public RoomTypeEntry FindEntry(string roomTypeId)
    {
        if (string.IsNullOrWhiteSpace(roomTypeId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RoomTypeEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.roomTypeId, roomTypeId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public RoomTypeEntry GetOrCreateEntry(string roomTypeId)
    {
        if (string.IsNullOrWhiteSpace(roomTypeId))
        {
            return null;
        }

        string resolvedId = roomTypeId.Trim();
        RoomTypeEntry entry = FindEntry(resolvedId);
        if (entry != null)
        {
            return entry;
        }

        entry = new RoomTypeEntry
        {
            roomTypeId = resolvedId,
            displayName = resolvedId
        };
        entries.Add(entry);
        return entry;
    }

    public static RoomTypeDatabase LoadDefault()
    {
        return Resources.Load<RoomTypeDatabase>(DefaultResourcePath);
    }
}
