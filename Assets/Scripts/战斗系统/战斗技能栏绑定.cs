using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 战斗技能栏绑定 : MonoBehaviour
{
    [SerializeField] private RectTransform 技能格子prefab;
    [SerializeField] private RectTransform 战斗技能栏位;
    [SerializeField] private RectTransform 战斗技能格子区域;

    private readonly List<技能格组件> 已生成格子 = new List<技能格组件>();
    private BattleTurnSystem 战斗回合系统;
    private BattleSkillDatabase 技能数据库;
    private string 当前角色ID = string.Empty;
    private string 当前技能签名 = string.Empty;

    private sealed class 技能格组件
    {
        public RectTransform 根节点;
        public Button 按钮;
        public Image 技能图标;
        public Image 空图标;
        public string 技能ID = string.Empty;
    }

    public void 初始化(BattleTurnSystem turnSystem, RectTransform 技能格子模板, RectTransform 技能栏位根, RectTransform 技能格子容器)
    {
        战斗回合系统 = turnSystem;
        技能数据库 = BattleSkillDatabase.LoadDefault();
        技能格子prefab = 技能格子模板;
        战斗技能栏位 = 技能栏位根;
        战斗技能格子区域 = 技能格子容器;
        立即刷新(force: true);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || 战斗回合系统 == null)
        {
            return;
        }

        立即刷新(force: false);
    }

    private void 立即刷新(bool force)
    {
        string 角色ID = 解析当前角色ID();
        List<string> 技能列表 = 构建技能列表(角色ID);
        string 技能签名 = 构建技能签名(角色ID, 技能列表);
        if (!force &&
            string.Equals(当前角色ID, 角色ID, StringComparison.Ordinal) &&
            string.Equals(当前技能签名, 技能签名, StringComparison.Ordinal))
        {
            return;
        }

        当前角色ID = 角色ID;
        当前技能签名 = 技能签名;
        刷新技能栏可见性();
        重建技能格子(技能列表);
    }

    private void 刷新技能栏可见性()
    {
        if (战斗技能栏位 == null)
        {
            return;
        }

        BattleUnit 当前单位 = 战斗回合系统 != null ? 战斗回合系统.ActiveUnit : null;
        bool 显示 = 当前单位 != null &&
            当前单位.IsAlive &&
            当前单位.isPlayerControlled &&
            !string.IsNullOrWhiteSpace(当前单位.characterId);
        战斗技能栏位.gameObject.SetActive(显示);
    }

    private string 解析当前角色ID()
    {
        BattleUnit 当前单位 = 战斗回合系统 != null ? 战斗回合系统.ActiveUnit : null;
        if (当前单位 == null || !当前单位.IsAlive || !当前单位.isPlayerControlled)
        {
            return string.Empty;
        }

        return 当前单位.characterId ?? string.Empty;
    }

    private static string 构建技能签名(string 角色ID, List<string> 技能列表)
    {
        if (技能列表 == null || 技能列表.Count == 0)
        {
            return 角色ID ?? string.Empty;
        }

        return (角色ID ?? string.Empty) + "|" + string.Join("|", 技能列表);
    }

    private List<string> 构建技能列表(string 角色ID)
    {
        if (string.IsNullOrWhiteSpace(角色ID))
        {
            return new List<string>();
        }

        return CharacterSkillListUtility.BuildSkillIds(角色ID);
    }

    private void 重建技能格子(List<string> 技能列表)
    {
        清空已生成格子();
        if (战斗技能格子区域 == null || 技能格子prefab == null || 技能列表 == null)
        {
            return;
        }

        for (int i = 0; i < 技能列表.Count; i++)
        {
            string 技能ID = 技能列表[i];
            if (string.IsNullOrWhiteSpace(技能ID))
            {
                continue;
            }

            RectTransform 实例 = Instantiate(技能格子prefab, 战斗技能格子区域, false);
            实例.name = $"战斗技能格_{i}";
            技能格组件 格子 = new 技能格组件
            {
                根节点 = 实例,
                按钮 = 实例.GetComponent<Button>() ?? 实例.gameObject.AddComponent<Button>(),
                技能图标 = 查找直接子图标(实例, "技能图案"),
                空图标 = 查找直接子图标(实例, "空技能图案"),
                技能ID = 技能ID
            };

            刷新技能格显示(格子);
            int 捕获索引 = i;
            格子.按钮.onClick.RemoveAllListeners();
            格子.按钮.onClick.AddListener(() => 点击技能(捕获索引));
            已生成格子.Add(格子);
        }
    }

    private void 点击技能(int 索引)
    {
        if (索引 < 0 || 索引 >= 已生成格子.Count || 战斗回合系统 == null)
        {
            return;
        }

        技能格组件 格子 = 已生成格子[索引];
        if (格子 == null || string.IsNullOrWhiteSpace(格子.技能ID))
        {
            return;
        }

        if (!SkillUsabilityUtility.IsSkillUsable(技能数据库, 当前角色ID, 格子.技能ID))
        {
            return;
        }

        战斗回合系统.ToggleSkillMode(格子.技能ID);
    }

    private void 刷新技能格显示(技能格组件 格子)
    {
        if (格子 == null)
        {
            return;
        }

        Sprite 图标 = 解析技能图标(格子.技能ID);
        bool 可用 = !string.IsNullOrWhiteSpace(格子.技能ID) &&
            SkillUsabilityUtility.IsSkillUsable(技能数据库, 当前角色ID, 格子.技能ID);

        if (格子.空图标 != null)
        {
            格子.空图标.gameObject.SetActive(图标 == null);
        }

        if (格子.技能图标 != null)
        {
            格子.技能图标.sprite = 图标;
            格子.技能图标.gameObject.SetActive(图标 != null);
            格子.技能图标.color = 可用 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (格子.按钮 != null)
        {
            格子.按钮.interactable = 图标 != null && 可用;
        }
    }

    private Sprite 解析技能图标(string 技能ID)
    {
        if (string.IsNullOrWhiteSpace(技能ID))
        {
            return null;
        }

        if (技能数据库 == null)
        {
            技能数据库 = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry 条目 = 技能数据库 != null ? 技能数据库.FindEntry(技能ID) : null;
        return 条目 != null ? 条目.icon : null;
    }

    private void 清空已生成格子()
    {
        for (int i = 0; i < 已生成格子.Count; i++)
        {
            技能格组件 格子 = 已生成格子[i];
            if (格子 != null && 格子.根节点 != null)
            {
                Destroy(格子.根节点.gameObject);
            }
        }

        已生成格子.Clear();
    }

    private static Image 查找直接子图标(RectTransform 根节点, string 名称)
    {
        if (根节点 == null)
        {
            return null;
        }

        for (int i = 0; i < 根节点.childCount; i++)
        {
            Transform 子物体 = 根节点.GetChild(i);
            if (子物体 != null && string.Equals(子物体.name, 名称, StringComparison.Ordinal))
            {
                return 子物体.GetComponent<Image>();
            }
        }

        return null;
    }
}
