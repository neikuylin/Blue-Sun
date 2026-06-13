using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "剧情数据库", menuName = "剧情/剧情数据库")]
public sealed class 剧情数据库 : ScriptableObject
{
    public const string 默认资源路径 = "剧情数据库";

    public enum 场景目标类型
    {
        普通场景,
        战斗副本
    }

    public enum 剧情蓝图节点类型
    {
        开始,
        播放一句对话,
        播放对话组,
        设置事件,
        切换场景,
        添加物品到装备栏,
        黑幕淡入,
        黑幕淡出,
        角色播放动画,
        等待,
        汇合,
        播放一句小对话,
        播放小对话组
    }

    [Serializable]
    public sealed class 剧情蓝图节点
    {
        public string 节点ID = string.Empty;
        public 剧情蓝图节点类型 节点类型 = 剧情蓝图节点类型.播放一句对话;
        public Vector2 位置 = Vector2.zero;
        public string 对话组ID = string.Empty;
        public string 对话内容ID = string.Empty;
        public string 小对话组ID = string.Empty;
        public string 小对话内容ID = string.Empty;
        public string 事件ID = string.Empty;
        public bool 事件状态 = true;
        public 场景目标类型 目标类型 = 场景目标类型.普通场景;
        public string 场景名 = string.Empty;
        public string 地图模板ID = string.Empty;
        public string 房间节点ID = string.Empty;
        public string 角色ID = string.Empty;
        public string 物品ID = string.Empty;
        public int 装备格子索引;
        public float 持续时间 = 1f;
        public float 目标不透明度 = 1f;
        public RuntimeAnimatorController 动作控制器;
        public string 动画状态名 = string.Empty;
        [TextArea(2, 5)] public string 节点备注 = string.Empty;
    }

    [Serializable]
    public sealed class 剧情蓝图连线
    {
        public string 来源节点ID = string.Empty;
        public string 目标节点ID = string.Empty;
    }

    [Serializable]
    public sealed class 剧情条目
    {
        public string 剧情ID = string.Empty;
        [TextArea(2, 5)] public string 备注 = string.Empty;
        public List<剧情蓝图节点> 蓝图节点列表 = new List<剧情蓝图节点>();
        public List<剧情蓝图连线> 蓝图连线列表 = new List<剧情蓝图连线>();
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

            if (条目.蓝图节点列表 == null)
            {
                条目.蓝图节点列表 = new List<剧情蓝图节点>();
            }

            if (条目.蓝图连线列表 == null)
            {
                条目.蓝图连线列表 = new List<剧情蓝图连线>();
            }

        }
    }
}
