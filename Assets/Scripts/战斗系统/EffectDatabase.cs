using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectDatabase", menuName = "\u6218\u6597/\u6548\u679c\u5e93")]
public sealed class EffectDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "EffectDatabase";

    public enum ValueStackRule
    {
        NotStackable,
        Stackable
    }

    public enum DurationStackRule
    {
        NotStackable,
        Stackable,
        KeepHigher
    }

    public enum TurnOwner
    {
        Caster,
        Target
    }

    public enum CharacterStatField
    {
        TargetHealth,
        MaxHealth,
        Strength,
        Agility,
        Intelligence,
        ActionPoints,
        HitRate,
        DodgeRate,
        PhysicalResistance,
        FireResistance,
        CorruptionResistance,
        ColdResistance,
        PhysicalResistancePenetration,
        FireResistancePenetration,
        CorruptionResistancePenetration,
        ColdResistancePenetration,
        CriticalChance,
        CriticalDamage,
        WeaponEnchantment
    }

    [Serializable]
    public sealed class StatModifier
    {
        public enum HealthDamageType
        {
            Physical,
            Fire,
            Corruption,
            Cold
        }

        public enum AmountMode
        {
            Flat,
            Percent
        }

        public CharacterStatField statField;
        public HealthDamageType healthDamageType;
        public AmountMode amountMode;
        public int amount;
    }

    [Serializable]
    public sealed class EffectEntry
    {
        public string effectId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public Sprite icon;
        public ValueStackRule valueStackRule = ValueStackRule.NotStackable;
        public DurationStackRule durationStackRule = DurationStackRule.NotStackable;
        public TurnOwner durationTurnOwner = TurnOwner.Target;
        public List<StatModifier> statModifiers = new List<StatModifier>();
    }

    [SerializeField] private List<EffectEntry> entries = new List<EffectEntry>();

    public List<EffectEntry> Entries => entries;

    public EffectEntry FindEntry(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            EffectEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.effectId, effectId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public static EffectDatabase LoadDefault()
    {
        return Resources.Load<EffectDatabase>(DefaultResourcePath);
    }
}
