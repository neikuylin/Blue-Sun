using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "小对话组数据库", menuName = "事件/小对话组数据库")]
public sealed class 小对话组数据库 : ScriptableObject
{
    public const string 默认资源路径 = "小对话组数据库";

    [Serializable]
    public sealed class 小对话组
    {
        public string id = string.Empty;
        public List<string> 内容ID顺序 = new List<string>();
    }

    [SerializeField] private List<小对话组> 对话组列表 = new List<小对话组>();

    public List<小对话组> 获取对话组列表 => 对话组列表;

    public 小对话组 查找(string 对话组ID)
    {
        if (string.IsNullOrWhiteSpace(对话组ID))
        {
            return null;
        }

        string 目标ID = 对话组ID.Trim();
        for (int i = 0; i < 对话组列表.Count; i++)
        {
            小对话组 对话组 = 对话组列表[i];
            if (对话组 != null && string.Equals(对话组.id, 目标ID, StringComparison.Ordinal))
            {
                return 对话组;
            }
        }

        return null;
    }

    public 小对话组 获取或创建(string 对话组ID)
    {
        if (string.IsNullOrWhiteSpace(对话组ID))
        {
            return null;
        }

        string 目标ID = 对话组ID.Trim();
        小对话组 已有对话组 = 查找(目标ID);
        if (已有对话组 != null)
        {
            确保内容列表(已有对话组);
            return 已有对话组;
        }

        小对话组 新对话组 = new 小对话组
        {
            id = 目标ID
        };
        对话组列表.Add(新对话组);
        return 新对话组;
    }

    public static void 确保内容列表(小对话组 对话组)
    {
        if (对话组 != null && 对话组.内容ID顺序 == null)
        {
            对话组.内容ID顺序 = new List<string>();
        }
    }

    public static 小对话组数据库 加载默认库()
    {
        return Resources.Load<小对话组数据库>(默认资源路径);
    }
}
