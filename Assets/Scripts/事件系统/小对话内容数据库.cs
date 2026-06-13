using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "小对话内容数据库", menuName = "事件/小对话内容数据库")]
public sealed class 小对话内容数据库 : ScriptableObject
{
    public const string 默认资源路径 = "小对话内容数据库";

    public enum 对话形式
    {
        屏幕底部,
        角色头顶
    }

    [Serializable]
    public sealed class 小对话内容
    {
        public string id = string.Empty;
        public string 对话角色ID = string.Empty;
        public 对话形式 显示形式 = 对话形式.屏幕底部;
        public string 说话者文本 = string.Empty;
        [TextArea(2, 6)] public string 对话文本 = string.Empty;
        public AudioClip 配音;
    }

    [SerializeField] private List<小对话内容> 内容列表 = new List<小对话内容>();

    public List<小对话内容> 获取内容列表 => 内容列表;

    public 小对话内容 查找(string 内容ID)
    {
        if (string.IsNullOrWhiteSpace(内容ID))
        {
            return null;
        }

        string 目标ID = 内容ID.Trim();
        for (int i = 0; i < 内容列表.Count; i++)
        {
            小对话内容 内容 = 内容列表[i];
            if (内容 != null && string.Equals(内容.id, 目标ID, StringComparison.Ordinal))
            {
                return 内容;
            }
        }

        return null;
    }

    public 小对话内容 获取或创建(string 内容ID)
    {
        if (string.IsNullOrWhiteSpace(内容ID))
        {
            return null;
        }

        string 目标ID = 内容ID.Trim();
        小对话内容 已有内容 = 查找(目标ID);
        if (已有内容 != null)
        {
            return 已有内容;
        }

        小对话内容 新内容 = new 小对话内容
        {
            id = 目标ID
        };
        内容列表.Add(新内容);
        return 新内容;
    }

    public static 小对话内容数据库 加载默认库()
    {
        return Resources.Load<小对话内容数据库>(默认资源路径);
    }
}
