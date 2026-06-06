using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "效果特效全局配置", menuName = "战斗/效果特效全局配置")]
public sealed class 效果特效全局配置 : ScriptableObject
{
    public const string DefaultResourcePath = "效果特效全局配置";

    [Serializable]
    public sealed class 效果特效绑定条目
    {
        public string 效果ID = string.Empty;
        public string 特效脚本类型名 = string.Empty;
        public bool 预览启用;
    }

    [SerializeField] private List<效果特效绑定条目> 模型特效绑定列表 = new List<效果特效绑定条目>();
    [SerializeField] private List<效果特效绑定条目> 武器特效绑定列表 = new List<效果特效绑定条目>();

    public List<效果特效绑定条目> 模型特效绑定 => 模型特效绑定列表;
    public List<效果特效绑定条目> 武器特效绑定 => 武器特效绑定列表;

    public static 效果特效全局配置 LoadDefault()
    {
        return Resources.Load<效果特效全局配置>(DefaultResourcePath);
    }
}
