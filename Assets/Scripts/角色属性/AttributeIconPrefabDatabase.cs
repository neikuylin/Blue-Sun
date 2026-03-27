using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "AttributeIconPrefabDatabase", menuName = "角色/属性图标 TMP 库")]
public sealed class AttributeIconPrefabDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "AttributeIconPrefabDatabase";

    [Serializable]
    public sealed class Entry
    {
        public string attributeId = string.Empty;
        [HideInInspector] public GameObject prefab;
        public TMP_SpriteAsset spriteAsset;
        public string spriteName = string.Empty;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public List<Entry> Entries => entries;

    public Entry FindEntry(string attributeId)
    {
        if (string.IsNullOrWhiteSpace(attributeId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.attributeId, attributeId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public static AttributeIconPrefabDatabase LoadDefault()
    {
        return Resources.Load<AttributeIconPrefabDatabase>(DefaultResourcePath);
    }
}
