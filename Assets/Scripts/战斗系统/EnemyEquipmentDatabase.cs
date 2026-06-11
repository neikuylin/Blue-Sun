using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyEquipmentDatabase", menuName = "\u6218\u6597/\u654C\u4EBA\u88C5\u5907\u5E93")]
public sealed class EnemyEquipmentDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "EnemyEquipmentDatabase";
    public const int SlotCount = 9;
    public const int RuntimeSlotCount = 8;

    public static readonly string[] SlotLabels =
    {
        "\u4E3B\u624B",
        "\u526F\u624B",
        "\u4E3B\u526F\u624B",
        "\u5934\u76D4",
        "\u80F8\u7532",
        "\u624B\u5957",
        "\u978B\u5B50",
        "\u817F\u7532",
        "\u9970\u54C1"
    };

    public static readonly ItemDatabase.EquipmentSlotType[] SlotTypes =
    {
        ItemDatabase.EquipmentSlotType.MainHand,
        ItemDatabase.EquipmentSlotType.OffHand,
        ItemDatabase.EquipmentSlotType.MainOrOffHand,
        ItemDatabase.EquipmentSlotType.Helmet,
        ItemDatabase.EquipmentSlotType.Armor,
        ItemDatabase.EquipmentSlotType.Gloves,
        ItemDatabase.EquipmentSlotType.Shoes,
        ItemDatabase.EquipmentSlotType.LegArmor,
        ItemDatabase.EquipmentSlotType.Accessory
    };

    public static readonly ItemDatabase.EquipmentSlotType[] RuntimeSlotTypes =
    {
        ItemDatabase.EquipmentSlotType.MainHand,
        ItemDatabase.EquipmentSlotType.OffHand,
        ItemDatabase.EquipmentSlotType.Helmet,
        ItemDatabase.EquipmentSlotType.Armor,
        ItemDatabase.EquipmentSlotType.Gloves,
        ItemDatabase.EquipmentSlotType.Shoes,
        ItemDatabase.EquipmentSlotType.LegArmor,
        ItemDatabase.EquipmentSlotType.Accessory
    };

    public static int ResolveRuntimeSlotIndex(int databaseSlotIndex, bool mainHandOccupied, bool offHandOccupied)
    {
        if (databaseSlotIndex < 0 || databaseSlotIndex >= SlotTypes.Length)
        {
            return -1;
        }

        ItemDatabase.EquipmentSlotType sourceType = SlotTypes[databaseSlotIndex];
        if (sourceType == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return !mainHandOccupied ? 0 : (!offHandOccupied ? 1 : -1);
        }

        for (int i = 0; i < RuntimeSlotTypes.Length; i++)
        {
            if (RuntimeSlotTypes[i] == sourceType)
            {
                return i;
            }
        }

        return -1;
    }

    [Serializable]
    public sealed class EnemyEquipmentEntry
    {
        public string characterId = string.Empty;
        public List<string> itemIds = new List<string>();
    }

    [SerializeField] private List<EnemyEquipmentEntry> entries = new List<EnemyEquipmentEntry>();

    public List<EnemyEquipmentEntry> Entries => entries;

    public EnemyEquipmentEntry FindEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            EnemyEquipmentEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                EnsureValidItemList(entry);
                return entry;
            }
        }

        return null;
    }

    public EnemyEquipmentEntry GetOrCreateEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        EnemyEquipmentEntry existing = FindEntry(characterId);
        if (existing != null)
        {
            return existing;
        }

        EnemyEquipmentEntry created = new EnemyEquipmentEntry
        {
            characterId = characterId.Trim()
        };
        EnsureValidItemList(created);
        entries.Add(created);
        return created;
    }

    public bool RemoveEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            EnemyEquipmentEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            entries.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static void EnsureValidItemList(EnemyEquipmentEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.itemIds == null)
        {
            entry.itemIds = new List<string>();
        }

        while (entry.itemIds.Count < SlotCount)
        {
            entry.itemIds.Add(string.Empty);
        }

        while (entry.itemIds.Count > SlotCount)
        {
            entry.itemIds.RemoveAt(entry.itemIds.Count - 1);
        }
    }

    public static EnemyEquipmentDatabase LoadDefault()
    {
        return Resources.Load<EnemyEquipmentDatabase>(DefaultResourcePath);
    }
}
