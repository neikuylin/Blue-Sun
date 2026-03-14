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
        TwoHanded
    }

    public enum WeaponAttributeType
    {
        None,
        Strength,
        Agility,
        Intelligence
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
    public sealed class ItemEntry
    {
        public string itemId = string.Empty;
        public ItemCategory category = ItemCategory.Equipment;
        public ItemQuality quality = ItemQuality.Common;
        public EquipmentSlotType equipmentSlot = EquipmentSlotType.None;
        public WeaponCategory weaponCategory = WeaponCategory.None;
        public float fixedDamage;
        public List<string> grantedSkillIds = new List<string>();
        public List<WeaponAttributeMultiplierEntry> weaponAttributeMultipliers = new List<WeaponAttributeMultiplierEntry>();
        public GameObject prefab;
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

        return weaponCategory;
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
            weaponCategory == WeaponCategory.TwoHanded;
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
