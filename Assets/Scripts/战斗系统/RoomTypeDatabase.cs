using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomTypeDatabase", menuName = "战斗/房间类型库")]
public sealed class RoomTypeDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "RoomTypeDatabase";
    public const string EncounterBattleTypeId = "encounter_battle";
    public const string EncounterBattleTypeName = "遭遇战房间";
    public const string ChestTypeId = "chest";
    public const string ChestTypeName = "宝箱房";
    public const string BossBattleTypeId = "boss_battle";
    public const string BossBattleTypeName = "BOSS房间";
    public const string TotemTypeId = "totem";
    public const string TotemTypeName = "图腾房间";

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

    public static bool IsEncounterBattleType(string roomTypeId)
    {
        return string.Equals(NormalizeRoomTypeId(roomTypeId), EncounterBattleTypeId, StringComparison.Ordinal);
    }

    public static bool IsChestType(string roomTypeId)
    {
        return string.Equals(NormalizeRoomTypeId(roomTypeId), ChestTypeId, StringComparison.Ordinal);
    }

    public static bool RequiresBattleGridTemplate(string roomTypeId)
    {
        string id = NormalizeRoomTypeId(roomTypeId);
        return string.Equals(id, EncounterBattleTypeId, StringComparison.Ordinal) ||
            string.Equals(id, ChestTypeId, StringComparison.Ordinal);
    }

    public static bool RequiresEncounterPreset(string roomTypeId)
    {
        return IsEncounterBattleType(roomTypeId);
    }

    public static string NormalizeRoomTypeId(string roomTypeId)
    {
        return string.IsNullOrWhiteSpace(roomTypeId) ? string.Empty : roomTypeId.Trim();
    }

    public static RoomTypeDatabase LoadDefault()
    {
        return Resources.Load<RoomTypeDatabase>(DefaultResourcePath);
    }
}
