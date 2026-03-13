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
        Helmet,
        Armor,
        LegArmor,
        Gloves,
        Shoes,
        Accessory
    }

    [Serializable]
    public sealed class ItemEntry
    {
        public string itemId = string.Empty;
        public ItemCategory category = ItemCategory.Equipment;
        public EquipmentSlotType equipmentSlot = EquipmentSlotType.None;
        public Sprite icon;
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
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.itemId, itemId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public List<ItemEntry> FindEntries(ItemCategory category, EquipmentSlotType equipmentSlot = EquipmentSlotType.None)
    {
        List<ItemEntry> result = new List<ItemEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            ItemEntry entry = entries[i];
            if (entry == null || entry.category != category)
            {
                continue;
            }

            if (category == ItemCategory.Equipment &&
                equipmentSlot != EquipmentSlotType.None &&
                entry.equipmentSlot != equipmentSlot)
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    public static ItemDatabase LoadDefault()
    {
        return Resources.Load<ItemDatabase>(DefaultResourcePath);
    }
}
