using UnityEngine;

[CreateAssetMenu(fileName = "ItemTooltipPrefabDatabase", menuName = "背包/物品详情预制体数据库")]
public sealed class ItemTooltipPrefabDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "ItemTooltipPrefabDatabase";

    public GameObject oneHandedTwoHandedTooltipPrefab;
    public GameObject commonBackgroundPrefab;
    public GameObject excellentBackgroundPrefab;
    public GameObject epicBackgroundPrefab;
    public GameObject blessedBackgroundPrefab;

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

    public static ItemTooltipPrefabDatabase LoadDefault()
    {
        return Resources.Load<ItemTooltipPrefabDatabase>(DefaultResourcePath);
    }
}
