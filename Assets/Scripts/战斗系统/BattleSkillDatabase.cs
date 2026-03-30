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

    public enum AreaCastType
    {
        ImpactPoint,
        CircularAxis
    }

    public enum CircularAxisAreaType
    {
        Ray,
        Fan
    }

    [Serializable]
    public sealed class SkillEntry
    {
        [Serializable]
        public sealed class WeaponScopedActionOverride
        {
            public bool enabled = true;
            public ItemDatabase.WeaponCategory weaponCategory = ItemDatabase.WeaponCategory.None;
            public string targetSelectionStateName = string.Empty;
            public float targetSelectionYawOffset;
            public string actionStateName = string.Empty;
            public float actionYawOffset;
            public AudioClip actionSound;
            public GameObject actionSoundPrefab;
            public int soundDelayFrame;
            public bool compensateActionMotion;
        }

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
        public AreaCastType areaCastType = AreaCastType.ImpactPoint;
        public CircularAxisAreaType circularAxisAreaType = CircularAxisAreaType.Ray;
        public int axisWidth = 3;
        public float axisAngle = 180f;
        public Vector2Int effectSize = new Vector2Int(3, 3);
        public List<ItemDatabase.WeaponCategory> requiredWeaponCategories = new List<ItemDatabase.WeaponCategory>();
        public List<WeaponScopedActionOverride> weaponActionOverrides = new List<WeaponScopedActionOverride>();

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

        public bool RequiresWeaponCategory(ItemDatabase.WeaponCategory weaponCategory)
        {
            if (requiredWeaponCategories == null || requiredWeaponCategories.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < requiredWeaponCategories.Count; i++)
            {
                if (requiredWeaponCategories[i] == weaponCategory)
                {
                    return true;
                }
            }

            return false;
        }

        public WeaponScopedActionOverride FindEnabledWeaponActionOverride(ItemDatabase.WeaponCategory weaponCategory)
        {
            if (weaponActionOverrides == null)
            {
                return null;
            }

            for (int i = 0; i < weaponActionOverrides.Count; i++)
            {
                WeaponScopedActionOverride entry = weaponActionOverrides[i];
                if (entry == null || !entry.enabled || entry.weaponCategory != weaponCategory)
                {
                    continue;
                }

                return entry;
            }

            return null;
        }

        public bool HasRequiredWeaponCategory(ItemDatabase.WeaponCategory weaponCategory)
        {
            if (requiredWeaponCategories == null || requiredWeaponCategories.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < requiredWeaponCategories.Count; i++)
            {
                if (requiredWeaponCategories[i] == weaponCategory)
                {
                    return true;
                }
            }

            return false;
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

