using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestContentDatabase", menuName = "地图/宝箱内容库")]
public sealed class 宝箱内容数据库 : ScriptableObject
{
    public const string DefaultResourcePath = "ChestContentDatabase";

    public enum 宝箱物品生成类型
    {
        指定物品,
        随机物品
    }

    [Serializable]
    public sealed class 宝箱物品条目
    {
        public ItemDatabase.ItemCategory 分类筛选 = ItemDatabase.ItemCategory.Equipment;
        public string 物品ID = string.Empty;
        public float 出现概率 = 1f;
        public int 数量 = 1;
    }

    [Serializable]
    public sealed class 宝箱内容组
    {
        public string 内容组ID = string.Empty;
        public 宝箱物品生成类型 生成类型 = 宝箱物品生成类型.指定物品;
        public List<宝箱物品条目> 物品列表 = new List<宝箱物品条目>();
    }

    [SerializeField] private List<宝箱内容组> groups = new List<宝箱内容组>();

    public List<宝箱内容组> Groups => groups;

    public 宝箱内容组 FindGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return null;
        }

        string resolvedId = groupId.Trim();
        for (int i = 0; i < groups.Count; i++)
        {
            宝箱内容组 group = groups[i];
            string currentId = group != null && !string.IsNullOrWhiteSpace(group.内容组ID)
                ? group.内容组ID.Trim()
                : string.Empty;
            if (string.Equals(currentId, resolvedId, StringComparison.Ordinal))
            {
                return group;
            }
        }

        return null;
    }

    public static 宝箱内容数据库 LoadDefault()
    {
        return Resources.Load<宝箱内容数据库>(DefaultResourcePath);
    }
}
