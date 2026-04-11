using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemTooltipPrefabDatabase", menuName = "\u80cc\u5305/\u7269\u54c1\u8be6\u60c5\u9884\u5236\u4f53\u6570\u636e\u5e93")]
public sealed class ItemTooltipPrefabDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "ItemTooltipPrefabDatabase";

    [Serializable]
    public sealed class WeaponTooltipPrefabEntry
    {
        public ItemDatabase.WeaponCategory weaponCategory = ItemDatabase.WeaponCategory.None;
        public GameObject tooltipPrefab;
    }

    [SerializeField] private List<WeaponTooltipPrefabEntry> weaponTooltipPrefabs = new List<WeaponTooltipPrefabEntry>();

    public GameObject commonBackgroundPrefab;
    public GameObject excellentBackgroundPrefab;
    public GameObject epicBackgroundPrefab;
    public GameObject blessedBackgroundPrefab;

    public IReadOnlyList<WeaponTooltipPrefabEntry> WeaponTooltipPrefabs => weaponTooltipPrefabs;

    public GameObject GetQualityBackgroundPrefab(ItemDatabase.ItemQuality quality)
    {
        switch (quality)
        {
            case ItemDatabase.ItemQuality.Excellent:
                return excellentBackgroundPrefab;
            case ItemDatabase.ItemQuality.Epic:
                return epicBackgroundPrefab;
            case ItemDatabase.ItemQuality.Blessed:
                return blessedBackgroundPrefab;
            default:
                return commonBackgroundPrefab;
        }
    }

    public GameObject GetWeaponTooltipPrefab(ItemDatabase.WeaponCategory weaponCategory)
    {
        EnsureWeaponTooltipEntries();

        for (int i = 0; i < weaponTooltipPrefabs.Count; i++)
        {
            WeaponTooltipPrefabEntry entry = weaponTooltipPrefabs[i];
            if (entry == null || entry.weaponCategory != weaponCategory)
            {
                continue;
            }

            return entry.tooltipPrefab;
        }

        return null;
    }

    public void SetWeaponTooltipPrefab(ItemDatabase.WeaponCategory weaponCategory, GameObject prefab)
    {
        EnsureWeaponTooltipEntries();

        for (int i = 0; i < weaponTooltipPrefabs.Count; i++)
        {
            WeaponTooltipPrefabEntry entry = weaponTooltipPrefabs[i];
            if (entry == null || entry.weaponCategory != weaponCategory)
            {
                continue;
            }

            entry.tooltipPrefab = prefab;
            return;
        }

        weaponTooltipPrefabs.Add(new WeaponTooltipPrefabEntry
        {
            weaponCategory = weaponCategory,
            tooltipPrefab = prefab
        });
    }

    public bool EnsureWeaponTooltipEntries()
    {
        Dictionary<ItemDatabase.WeaponCategory, GameObject> existingPrefabs =
            new Dictionary<ItemDatabase.WeaponCategory, GameObject>();

        for (int i = 0; i < weaponTooltipPrefabs.Count; i++)
        {
            WeaponTooltipPrefabEntry entry = weaponTooltipPrefabs[i];
            if (entry == null || entry.weaponCategory == ItemDatabase.WeaponCategory.None)
            {
                continue;
            }

            if (!existingPrefabs.ContainsKey(entry.weaponCategory))
            {
                existingPrefabs.Add(entry.weaponCategory, entry.tooltipPrefab);
            }
        }

        Array categories = Enum.GetValues(typeof(ItemDatabase.WeaponCategory));
        List<WeaponTooltipPrefabEntry> syncedEntries = new List<WeaponTooltipPrefabEntry>();
        for (int i = 0; i < categories.Length; i++)
        {
            ItemDatabase.WeaponCategory category = (ItemDatabase.WeaponCategory)categories.GetValue(i);
            if (category == ItemDatabase.WeaponCategory.None)
            {
                continue;
            }

            existingPrefabs.TryGetValue(category, out GameObject prefab);
            syncedEntries.Add(new WeaponTooltipPrefabEntry
            {
                weaponCategory = category,
                tooltipPrefab = prefab
            });
        }

        bool changed = weaponTooltipPrefabs.Count != syncedEntries.Count;
        if (!changed)
        {
            for (int i = 0; i < syncedEntries.Count; i++)
            {
                WeaponTooltipPrefabEntry current = weaponTooltipPrefabs[i];
                WeaponTooltipPrefabEntry next = syncedEntries[i];
                if (current == null ||
                    current.weaponCategory != next.weaponCategory ||
                    current.tooltipPrefab != next.tooltipPrefab)
                {
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
        {
            weaponTooltipPrefabs = syncedEntries;
        }

        return changed;
    }

    public static ItemTooltipPrefabDatabase LoadDefault()
    {
        return Resources.Load<ItemTooltipPrefabDatabase>(DefaultResourcePath);
    }
}
