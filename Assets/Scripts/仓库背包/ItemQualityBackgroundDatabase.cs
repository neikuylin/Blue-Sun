using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ItemQualityBackgroundDatabase", menuName = "背包/物品品质底图数据库")]
public sealed class ItemQualityBackgroundDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "ItemQualityBackgroundDatabase";

    [System.Serializable]
    public sealed class QualityBackgroundEntry
    {
        public ItemDatabase.ItemQuality quality = ItemDatabase.ItemQuality.Common;

        [FormerlySerializedAs("prefab")]
        public GameObject oneByOnePrefab;

        public GameObject oneByTwoPrefab;
    }

    [SerializeField] private QualityBackgroundEntry common = new QualityBackgroundEntry
    {
        quality = ItemDatabase.ItemQuality.Common
    };

    [SerializeField] private QualityBackgroundEntry excellent = new QualityBackgroundEntry
    {
        quality = ItemDatabase.ItemQuality.Excellent
    };

    [SerializeField] private QualityBackgroundEntry epic = new QualityBackgroundEntry
    {
        quality = ItemDatabase.ItemQuality.Epic
    };

    [SerializeField] private QualityBackgroundEntry blessed = new QualityBackgroundEntry
    {
        quality = ItemDatabase.ItemQuality.Blessed
    };

    public GameObject GetPrefab(ItemDatabase.ItemQuality quality)
    {
        QualityBackgroundEntry entry = GetEntry(quality);
        return entry != null ? entry.oneByOnePrefab : null;
    }

    public GameObject GetPrefab(ItemDatabase.ItemQuality quality, bool useOneByTwo)
    {
        QualityBackgroundEntry entry = GetEntry(quality);
        if (entry == null)
        {
            return null;
        }

        if (useOneByTwo)
        {
            return entry.oneByTwoPrefab;
        }

        return entry.oneByOnePrefab;
    }

    private QualityBackgroundEntry GetEntry(ItemDatabase.ItemQuality quality)
    {
        switch (quality)
        {
            case ItemDatabase.ItemQuality.Excellent:
                return excellent;
            case ItemDatabase.ItemQuality.Epic:
                return epic;
            case ItemDatabase.ItemQuality.Blessed:
                return blessed;
            default:
                return common;
        }
    }

    public QualityBackgroundEntry Common => common;
    public QualityBackgroundEntry Excellent => excellent;
    public QualityBackgroundEntry Epic => epic;
    public QualityBackgroundEntry Blessed => blessed;

    public static ItemQualityBackgroundDatabase LoadDefault()
    {
        return Resources.Load<ItemQualityBackgroundDatabase>(DefaultResourcePath);
    }
}
