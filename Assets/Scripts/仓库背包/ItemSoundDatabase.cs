using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSoundDatabase", menuName = "背包/物品音效库")]
public sealed class ItemSoundDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "ItemSoundDatabase";

    [Serializable]
    public sealed class CategorySoundEntry
    {
        public ItemDatabase.ItemCategory category = ItemDatabase.ItemCategory.Equipment;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [SerializeField] private List<CategorySoundEntry> entries = new List<CategorySoundEntry>();

    public List<CategorySoundEntry> Entries => entries;

    public CategorySoundEntry FindEntry(ItemDatabase.ItemCategory category)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CategorySoundEntry entry = entries[i];
            if (entry != null && entry.category == category)
            {
                return entry;
            }
        }

        return null;
    }

    public static ItemSoundDatabase LoadDefault()
    {
        return Resources.Load<ItemSoundDatabase>(DefaultResourcePath);
    }
}
