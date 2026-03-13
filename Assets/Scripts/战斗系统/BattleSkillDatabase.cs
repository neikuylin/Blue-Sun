using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleSkillDatabase", menuName = "\u6218\u6597/\u6280\u80FD\u5E93")]
public sealed class BattleSkillDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "BattleSkillDatabase";
    public const string MoveSkillId = "\u79FB\u52A8";

    public enum SkillType
    {
        Move,
        Target,
        Area
    }

    [Serializable]
    public sealed class SkillEntry
    {
        public string skillId = string.Empty;
        public string group = "\u6280\u80FD";
        public SkillType skillType = SkillType.Move;
        public int actionPointCost = 1;
        public int manaCost;
        public int cooldownTurns;
        public bool useMoveDistanceAsRange = true;
        public int range;
        public Vector2Int effectSize = new Vector2Int(3, 3);

        public int ResolveActionPointCost()
        {
            return Mathf.Max(0, actionPointCost);
        }

        public int ResolveManaCost()
        {
            return Mathf.Max(0, manaCost);
        }

        public int ResolveRange(int moveDistance)
        {
            return useMoveDistanceAsRange ? Mathf.Max(0, moveDistance) : Mathf.Max(0, range);
        }
    }

    [SerializeField] private List<SkillEntry> entries = new List<SkillEntry>();

    public List<SkillEntry> Entries => entries;

    public SkillEntry FindEntry(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.skillId, skillId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public static BattleSkillDatabase LoadDefault()
    {
        return Resources.Load<BattleSkillDatabase>(DefaultResourcePath);
    }
}
