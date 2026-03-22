using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleSkillDatabase", menuName = "战斗/技能库")]
public sealed class BattleSkillDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "BattleSkillDatabase";
    public const string MoveSkillId = "移动";

    public enum SkillGroup
    {
        Special,
        CombatArt,
        Spell
    }

    public enum SkillType
    {
        Target,
        Area
    }

    public enum CastTarget
    {
        Self,
        Enemy,
        Ally,
        All
    }

    [Serializable]
    public sealed class SkillEntry
    {
        public string skillId = string.Empty;
        public string description = string.Empty;
        public string actionStateName = string.Empty;
        public float actionYawOffset;
        public AudioClip actionSound;
        public GameObject actionSoundPrefab;
        public int soundDelayFrame;
        public bool enableHitFeel;
        public bool compensateActionMotion;
        public int resolveFrame;
        public SkillGroup group = SkillGroup.CombatArt;
        public SkillType skillType = SkillType.Target;
        public CastTarget castTarget = CastTarget.Enemy;
        public Sprite icon;
        public float damageMultiplier = 1f;
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

    public List<SkillEntry> FindByGroup(SkillGroup group)
    {
        List<SkillEntry> result = new List<SkillEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            SkillEntry entry = entries[i];
            if (entry != null && entry.group == group)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    public static BattleSkillDatabase LoadDefault()
    {
        return Resources.Load<BattleSkillDatabase>(DefaultResourcePath);
    }
}
