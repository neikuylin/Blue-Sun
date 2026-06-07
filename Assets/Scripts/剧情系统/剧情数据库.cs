using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "剧情数据库", menuName = "剧情/剧情数据库")]
public sealed class 剧情数据库 : ScriptableObject
{
    public const string 默认资源路径 = "剧情数据库";

    [Serializable]
    public sealed class 剧情条目
    {
        public string 剧情ID = string.Empty;
        [TextArea(2, 5)] public string 备注 = string.Empty;
    }

    [SerializeField] private List<剧情条目> 剧情列表 = new List<剧情条目>();

    public List<剧情条目> 取得剧情列表()
    {
        return 剧情列表;
    }

    public 剧情条目 查找剧情(string 剧情ID)
    {
        if (string.IsNullOrWhiteSpace(剧情ID))
        {
            return null;
        }

        for (int i = 0; i < 剧情列表.Count; i++)
        {
            剧情条目 条目 = 剧情列表[i];
            if (条目 != null && string.Equals(条目.剧情ID, 剧情ID, StringComparison.Ordinal))
            {
                return 条目;
            }
        }

        return null;
    }

    public static 剧情数据库 加载默认数据库()
    {
        return Resources.Load<剧情数据库>(默认资源路径);
    }
}
