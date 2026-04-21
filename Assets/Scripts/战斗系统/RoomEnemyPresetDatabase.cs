using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomEnemyPresetDatabase", menuName = "战斗/房间敌人预设库")]
public sealed class RoomEnemyPresetDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "RoomEnemyPresetDatabase";

    [Serializable]
    public sealed class PresetEnemyEntry
    {
        public string enemyId = string.Empty;
        public BattleTeam team = BattleTeam.Enemy;
        public bool isPlayerControlled;
    }

    [Serializable]
    public sealed class RoomEnemyPresetEntry
    {
        public string presetId = string.Empty;
        public string roomTypeId = RoomTypeDatabase.EncounterBattleTypeId;
        public List<PresetEnemyEntry> enemies = new List<PresetEnemyEntry>();
    }

    [SerializeField] private List<RoomEnemyPresetEntry> entries = new List<RoomEnemyPresetEntry>();

    public List<RoomEnemyPresetEntry> Entries => entries;

    public RoomEnemyPresetEntry FindEntry(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RoomEnemyPresetEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.presetId, presetId, StringComparison.Ordinal))
            {
                EnsureValidEnemyList(entry);
                return entry;
            }
        }

        return null;
    }

    public RoomEnemyPresetEntry GetOrCreateEntry(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return null;
        }

        RoomEnemyPresetEntry existing = FindEntry(presetId.Trim());
        if (existing != null)
        {
            return existing;
        }

        RoomEnemyPresetEntry created = new RoomEnemyPresetEntry
        {
            presetId = presetId.Trim(),
            roomTypeId = RoomTypeDatabase.EncounterBattleTypeId
        };
        EnsureValidEnemyList(created);
        entries.Add(created);
        return created;
    }

    public bool RemoveEntry(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return false;
        }

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            RoomEnemyPresetEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.Equals(entry.presetId, presetId, StringComparison.Ordinal))
            {
                continue;
            }

            entries.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static void EnsureValidEnemyList(RoomEnemyPresetEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.enemies == null)
        {
            entry.enemies = new List<PresetEnemyEntry>();
        }
    }

    public static PresetEnemyEntry CloneEnemy(PresetEnemyEntry source)
    {
        if (source == null)
        {
            return new PresetEnemyEntry();
        }

        return new PresetEnemyEntry
        {
            enemyId = source.enemyId,
            team = source.team,
            isPlayerControlled = source.isPlayerControlled
        };
    }

    public static RoomEnemyPresetDatabase LoadDefault()
    {
        return Resources.Load<RoomEnemyPresetDatabase>(DefaultResourcePath);
    }
}
