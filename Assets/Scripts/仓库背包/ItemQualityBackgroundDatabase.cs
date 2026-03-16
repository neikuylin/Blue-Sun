using UnityEngine;

[CreateAssetMenu(fileName = "ItemQualityBackgroundDatabase", menuName = "背包/物品品质底图数据库")]
public sealed class ItemQualityBackgroundDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "ItemQualityBackgroundDatabase";

    [System.Serializable]
    public sealed class QualityBackgroundEntry
    {
        public ItemDatabase.ItemQuality quality = ItemDatabase.ItemQuality.Common;
        public GameObject prefab;
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
        switch (quality)
        {
            case ItemDatabase.ItemQuality.Excellent:
                return excellent != null ? excellent.prefab : null;
            case ItemDatabase.ItemQuality.Epic:
                return epic != null ? epic.prefab : null;
            case ItemDatabase.ItemQuality.Blessed:
                return blessed != null ? blessed.prefab : null;
            default:
                return common != null ? common.prefab : null;
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
