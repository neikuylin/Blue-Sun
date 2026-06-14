using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleSkillDatabase", menuName = "战斗/技能库")]
public sealed class BattleSkillDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "BattleSkillDatabase";
    public const string MoveSkillId = "移动";
    public const string DualWieldNormalAttackSkillId = "双持普通攻击";
    public const string NoSkillSourceText = "无";

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

    public enum DamageType
    {
        Physical,
        Fire,
        Corruption,
        Cold
    }

    [Serializable]
    public sealed class SkillEntry
    {
        [Serializable]
        public sealed class AttachedEffectEntry
        {
            public string effectId = string.Empty;
            public int durationTurns = 1;
            public int applyChancePercent = 100;
        }

        [Serializable]
        public sealed class WeaponScopedActionOverride
        {
            public bool enabled = true;
            public ItemDatabase.WeaponCategory weaponCategory = ItemDatabase.WeaponCategory.None;
            public string raiseHandStateName = string.Empty;
            public float raiseHandYawOffset;
            public string targetSelectionStateName = string.Empty;
            public float targetSelectionYawOffset;
            public string actionStateName = string.Empty;
            public float actionYawOffset;
            public float postUseYawOffset;
            public AudioClip actionSound;
            public GameObject actionSoundPrefab;
            public AudioClip hitSound;
            public GameObject hitSoundPrefab;
            public int soundDelayFrame;
            public bool compensateActionMotion;
        }

        public string skillId = string.Empty;
        public string skillSource = string.Empty;
        public string description = string.Empty;
        public GameObject hitEffectPrefab;
        public bool useProjectile;
        public GameObject projectilePrefab;
        public int projectileStartFrame;
        public bool enableHitFeel;
        public int resolveFrame;
        public int castCount = 1;
        public int hitCount = 1;
        public List<int> extraProjectileStartFrames = new List<int>();
        public List<int> extraHitResolveFrames = new List<int>();
        public SkillGroup group = SkillGroup.CombatArt;
        public SkillType skillType = SkillType.Target;
        public CastTarget castTarget = CastTarget.Enemy;
        public Sprite icon;
        public bool noDamage;
        public float damageMultiplier = 1f;
        public float attributeMultiplier = 1f;
        public int fixedDamage;
        public int hitRateModifier;
        public DamageType damageType = DamageType.Physical;
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
        public List<AttachedEffectEntry> attachedEffects = new List<AttachedEffectEntry>();
        public List<ItemDatabase.WeaponCategory> requiredWeaponCategories = new List<ItemDatabase.WeaponCategory>();
        public List<WeaponScopedActionOverride> weaponActionOverrides = new List<WeaponScopedActionOverride>();
        public WeaponScopedActionOverride spellActionOverride = new WeaponScopedActionOverride();

        public int ResolveActionPointCost()
        {
            return Mathf.Max(0, actionPointCost);
        }

        public int ResolveCastCount()
        {
            return Mathf.Max(1, castCount);
        }

        public int ResolveHitCount()
        {
            return Mathf.Max(1, hitCount);
        }

        public bool TryResolveHitResolveFrame(int hitIndex, out int frame)
        {
            if (hitIndex <= 0)
            {
                frame = resolveFrame;
                return true;
            }

            int extraIndex = hitIndex - 1;
            if (extraHitResolveFrames == null ||
                extraIndex < 0 ||
                extraIndex >= extraHitResolveFrames.Count)
            {
                frame = 0;
                return false;
            }

            frame = extraHitResolveFrames[extraIndex];
            return frame > 0;
        }

        public bool TryResolveProjectileStartFrame(int hitIndex, out int frame)
        {
            if (hitIndex <= 0)
            {
                frame = projectileStartFrame;
                return true;
            }

            int extraIndex = hitIndex - 1;
            if (extraProjectileStartFrames == null ||
                extraIndex < 0 ||
                extraIndex >= extraProjectileStartFrames.Count)
            {
                frame = 0;
                return false;
            }

            frame = extraProjectileStartFrames[extraIndex];
            return frame > 0;
        }

        public int ResolveManaCost()
        {
            return Mathf.Max(0, manaCost);
        }

        public int ResolveHitRateModifier()
        {
            return group == SkillGroup.CombatArt || group == SkillGroup.Spell
                ? hitRateModifier
                : 0;
        }

        public int ResolveRange(int moveDistance)
        {
            return useMoveDistanceAsRange ? Mathf.Max(0, moveDistance) : Mathf.Max(0, range);
        }

        public bool RequiresWeaponCategory(ItemDatabase.WeaponCategory weaponCategory)
        {
            if (group == SkillGroup.Spell)
            {
                return true;
            }

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

        public WeaponScopedActionOverride FindEnabledSpellActionOverride()
        {
            return spellActionOverride != null && spellActionOverride.enabled
                ? spellActionOverride
                : null;
        }

        public bool HasRequiredWeaponCategory(ItemDatabase.WeaponCategory weaponCategory)
        {
            if (group == SkillGroup.Spell)
            {
                return false;
            }

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

    public static string ResolveSkillSource(string runtimeSkillSource, SkillEntry skill)
    {
        if (!string.IsNullOrWhiteSpace(runtimeSkillSource) && !string.Equals(runtimeSkillSource, NoSkillSourceText, StringComparison.Ordinal))
        {
            return runtimeSkillSource;
        }

        if (skill != null && !string.IsNullOrWhiteSpace(skill.skillSource))
        {
            return skill.skillSource;
        }

        return NoSkillSourceText;
    }

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

