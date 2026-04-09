using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "背包/物品数据库")]
public sealed class ItemDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "ItemDatabase";

    public enum ItemCategory
    {
        Equipment,
        Consumable,
        Material,
        Supply
    }

    public enum EquipmentSlotType
    {
        None,
        MainHand,
        OffHand,
        MainOrOffHand,
        Helmet,
        Armor,
        LegArmor,
        Gloves,
        Shoes,
        Accessory
    }

    public enum WeaponCategory
    {
        None,
        OneHanded,
        TwoHanded,
        Bow,
        Staff
    }

    public enum WeaponAttributeType
    {
        None,
        Strength,
        Agility,
        Intelligence
    }

    public enum ResistanceModifierType
    {
        None,
        Physical,
        Fire,
        Corruption,
        Cold
    }

    public enum ItemQuality
    {
        Common,
        Excellent,
        Epic,
        Blessed
    }

    [Serializable]
    public sealed class WeaponAttributeMultiplierEntry
    {
        public WeaponAttributeType attributeType = WeaponAttributeType.Strength;
        public float multiplier = 1f;
    }

    [Serializable]
    public sealed class WeaponResistancePenetrationEntry
    {
        public ResistanceModifierType resistanceType = ResistanceModifierType.Physical;
        public int value;
    }

    [Serializable]
    public sealed class WeaponDamageDistribution
    {
        public int physical = 100;
        public int fire;
        public int corruption;
        public int cold;

        public int Total => physical + fire + corruption + cold;
    }

    [Serializable]
    public sealed class ItemEntry
    {
        public string itemId = string.Empty;
        public string displayName = string.Empty;
        public ItemCategory category = ItemCategory.Equipment;
        public ItemQuality quality = ItemQuality.Common;
        public EquipmentSlotType equipmentSlot = EquipmentSlotType.None;
        public WeaponCategory weaponCategory = WeaponCategory.None;
        public WeaponDamageDistribution weaponDamageDistribution = new WeaponDamageDistribution();
        public float fixedDamage;
        public int criticalChanceBonus;
        public int criticalDamageBonus;
        public float staffDamageMultiplier = 1f;
        public int manaRecovery;
        public string description = string.Empty;
        public List<string> grantedSkillIds = new List<string>();
        public List<WeaponAttributeMultiplierEntry> weaponAttributeMultipliers = new List<WeaponAttributeMultiplierEntry>();
        public List<WeaponResistancePenetrationEntry> weaponResistancePenetrations = new List<WeaponResistancePenetrationEntry>();
        public GameObject prefab;
        public GameObject weaponModelPrefab;
    }

    [SerializeField] private List<ItemEntry> entries = new List<ItemEntry>();

    public List<ItemEntry> Entries => entries;

    public ItemEntry FindEntry(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemEntry entry = entries[i];
            if (entry != null && string.Equals(entry.itemId, itemId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public List<ItemEntry> FindEntries(
        ItemCategory category,
        EquipmentSlotType equipmentSlot = EquipmentSlotType.None,
        WeaponCategory weaponCategory = WeaponCategory.None)
    {
        List<ItemEntry> result = new List<ItemEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            ItemEntry entry = entries[i];
            if (entry == null || entry.category != category)
            {
                continue;
            }

            if (category == ItemCategory.Equipment)
            {
                if (equipmentSlot != EquipmentSlotType.None && entry.equipmentSlot != equipmentSlot)
                {
                    continue;
                }

                if (ShouldFilterWeaponCategory(equipmentSlot) &&
                    weaponCategory != WeaponCategory.None &&
                    entry.weaponCategory != weaponCategory)
                {
                    continue;
                }
            }

            result.Add(entry);
        }

        return result;
    }

    public static bool ShouldFilterWeaponCategory(EquipmentSlotType equipmentSlot)
    {
        return equipmentSlot == EquipmentSlotType.MainHand ||
            equipmentSlot == EquipmentSlotType.MainOrOffHand;
    }

    public static bool SupportsWeaponModelPrefab(EquipmentSlotType equipmentSlot)
    {
        return equipmentSlot == EquipmentSlotType.MainHand ||
            equipmentSlot == EquipmentSlotType.MainOrOffHand;
    }

    public static WeaponCategory NormalizeWeaponCategory(
        EquipmentSlotType equipmentSlot,
        WeaponCategory weaponCategory)
    {
        if (!ShouldFilterWeaponCategory(equipmentSlot))
        {
            return WeaponCategory.None;
        }

        if (equipmentSlot == EquipmentSlotType.MainOrOffHand &&
            weaponCategory == WeaponCategory.TwoHanded)
        {
            return WeaponCategory.OneHanded;
        }

        if (equipmentSlot == EquipmentSlotType.MainOrOffHand &&
            weaponCategory == WeaponCategory.Staff)
        {
            return WeaponCategory.None;
        }

        return weaponCategory;
    }

    public static bool ShouldShowGrantedSkillList(
        ItemCategory category,
        WeaponCategory weaponCategory)
    {
        if (category != ItemCategory.Equipment)
        {
            return false;
        }

        return weaponCategory == WeaponCategory.OneHanded ||
            weaponCategory == WeaponCategory.TwoHanded ||
            weaponCategory == WeaponCategory.Bow ||
            weaponCategory == WeaponCategory.Staff;
    }

    public static bool ShouldShowWeaponAttributeMultiplier(
        ItemCategory category,
        WeaponCategory weaponCategory)
    {
        if (category != ItemCategory.Equipment)
        {
            return false;
        }

        return weaponCategory == WeaponCategory.OneHanded ||
            weaponCategory == WeaponCategory.TwoHanded ||
            weaponCategory == WeaponCategory.Bow;
    }

    public static bool ShouldShowWeaponDamageDistribution(
        ItemCategory category,
        WeaponCategory weaponCategory)
    {
        return ShouldShowWeaponAttributeMultiplier(category, weaponCategory);
    }

    public static bool ShouldShowStaffFields(
        ItemCategory category,
        WeaponCategory weaponCategory)
    {
        return category == ItemCategory.Equipment &&
            weaponCategory == WeaponCategory.Staff;
    }

    public static WeaponDamageDistribution CreateDefaultWeaponDamageDistribution()
    {
        return new WeaponDamageDistribution
        {
            physical = 100,
            fire = 0,
            corruption = 0,
            cold = 0
        };
    }

    public static void EnsureValidWeaponAttributeList(ItemEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.weaponAttributeMultipliers == null)
        {
            entry.weaponAttributeMultipliers = new List<WeaponAttributeMultiplierEntry>();
        }

        if (entry.weaponAttributeMultipliers.Count == 0)
        {
            entry.weaponAttributeMultipliers.Add(new WeaponAttributeMultiplierEntry());
        }
    }

    public static void EnsureValidWeaponResistancePenetrationList(ItemEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.weaponResistancePenetrations == null)
        {
            entry.weaponResistancePenetrations = new List<WeaponResistancePenetrationEntry>();
        }

        if (entry.weaponResistancePenetrations.Count == 0)
        {
            entry.weaponResistancePenetrations.Add(new WeaponResistancePenetrationEntry());
        }
    }

    public static void EnsureValidGrantedSkillList(ItemEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.grantedSkillIds == null)
        {
            entry.grantedSkillIds = new List<string>();
        }

        if (entry.grantedSkillIds.Count == 0)
        {
            entry.grantedSkillIds.Add(string.Empty);
        }
    }

    public static ItemDatabase LoadDefault()
    {
        return Resources.Load<ItemDatabase>(DefaultResourcePath);
    }
}
