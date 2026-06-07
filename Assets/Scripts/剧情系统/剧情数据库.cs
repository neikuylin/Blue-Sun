using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "剧情数据库", menuName = "剧情/剧情数据库")]
public sealed class 剧情数据库 : ScriptableObject
{
    public const string 默认资源路径 = "剧情数据库";

    public enum 剧情步骤类型
    {
        播放对话,
        设置事件,
        切换场景
    }

    public enum 场景目标类型
    {
        普通场景,
        战斗副本
    }

    [Serializable]
    public sealed class 剧情步骤
    {
        public 剧情步骤类型 步骤类型 = 剧情步骤类型.播放对话;
        public string 对话组ID = string.Empty;
        public string 事件ID = string.Empty;
        public bool 事件状态 = true;
        public 场景目标类型 目标类型 = 场景目标类型.普通场景;
        public string 场景名 = string.Empty;
        public string 地图模板ID = string.Empty;
        public string 房间节点ID = string.Empty;
        [TextArea(2, 5)] public string 步骤备注 = string.Empty;
    }

    [Serializable]
    public sealed class 剧情条目
    {
        public string 剧情ID = string.Empty;
        [TextArea(2, 5)] public string 备注 = string.Empty;
        public List<剧情步骤> 步骤列表 = new List<剧情步骤>();
    }

    [SerializeField] private List<剧情条目> 剧情列表 = new List<剧情条目>();

    public List<剧情条目> 取得剧情列表()
    {
        确保剧情列表有效();
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

    public void 确保剧情列表有效()
    {
        if (剧情列表 == null)
        {
            剧情列表 = new List<剧情条目>();
            return;
        }

        for (int i = 0; i < 剧情列表.Count; i++)
        {
            剧情条目 条目 = 剧情列表[i];
            if (条目 == null)
            {
                continue;
            }

            if (条目.步骤列表 == null)
            {
                条目.步骤列表 = new List<剧情步骤>();
            }
        }
    }
}
