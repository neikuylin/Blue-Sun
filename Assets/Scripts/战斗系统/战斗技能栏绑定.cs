using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 战斗技能栏绑定 : MonoBehaviour
{
    private const float 技能提示延迟秒 = 0.5f;

    [SerializeField] private RectTransform 技能格子prefab;
    [SerializeField] private RectTransform 战斗技能栏位;
    [SerializeField] private RectTransform 战斗技能格子区域;

    private readonly List<技能格组件> 已生成格子 = new List<技能格组件>();
    private BattleTurnSystem 战斗回合系统;
    private BattleSkillDatabase 技能数据库;
    private CanvasGroup 栏位画布组;
    private string 当前角色ID = string.Empty;
    private string 当前技能签名 = string.Empty;

    private sealed class 技能格组件
    {
        public RectTransform 根节点;
        public Button 按钮;
        public Image 技能图标;
        public Image 空图标;
        public string 技能ID = string.Empty;
        public string 技能来源 = string.Empty;
        public 技能悬停转发 悬停转发;
    }

    private sealed class 技能悬停转发 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private 战斗技能栏绑定 owner;
        private int index;

        public void 配置(战斗技能栏绑定 绑定, int 格子索引)
        {
            owner = 绑定;
            index = 格子索引;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.处理技能悬停进入(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.处理技能悬停离开(index, eventData);
        }
    }

    public void 初始化(BattleTurnSystem turnSystem)
    {
        战斗回合系统 = turnSystem;
        技能数据库 = BattleSkillDatabase.LoadDefault();
        校验绑定();
        缓存栏位画布组();
        立即刷新(true);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || 战斗回合系统 == null)
        {
            return;
        }

        立即刷新(false);
    }

    private void 校验绑定()
    {
        if (技能格子prefab == null)
        {
            Debug.LogWarning("[战斗技能栏绑定] 未绑定技能格子prefab。", this);
        }

        if (战斗技能栏位 == null)
        {
            Debug.LogWarning("[战斗技能栏绑定] 未绑定战斗技能栏位。", this);
        }

        if (战斗技能格子区域 == null)
        {
            Debug.LogWarning("[战斗技能栏绑定] 未绑定战斗技能格子区域。", this);
        }
    }

    private void 缓存栏位画布组()
    {
        if (战斗技能栏位 == null)
        {
            栏位画布组 = null;
            return;
        }

        栏位画布组 = 战斗技能栏位.GetComponent<CanvasGroup>();
        if (栏位画布组 == null)
        {
            栏位画布组 = 战斗技能栏位.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void 立即刷新(bool force)
    {
        string 角色ID = 解析当前角色ID();
        BattleUnit 当前单位 = 战斗回合系统 != null ? 战斗回合系统.ActiveUnit : null;
        List<CharacterSkillListUtility.DisplaySkillEntry> 最终技能列表 = CharacterSkillListUtility.BuildDisplaySkillEntries(角色ID);
        string 技能签名 = 构建技能签名(角色ID, 当前单位, 最终技能列表);

        刷新技能栏可见性(角色ID);

        if (!force &&
            string.Equals(当前角色ID, 角色ID, StringComparison.Ordinal) &&
            string.Equals(当前技能签名, 技能签名, StringComparison.Ordinal))
        {
            return;
        }

        当前角色ID = 角色ID;
        当前技能签名 = 技能签名;
        重建技能格子(最终技能列表);
    }

    private void 刷新技能栏可见性(string 角色ID)
    {
        if (战斗技能栏位 == null)
        {
            return;
        }

        bool 显示 = !string.IsNullOrWhiteSpace(角色ID);
        if (栏位画布组 != null)
        {
            栏位画布组.alpha = 显示 ? 1f : 0f;
            栏位画布组.interactable = 显示;
            栏位画布组.blocksRaycasts = 显示;
        }

        for (int i = 0; i < 已生成格子.Count; i++)
        {
            技能格组件 格子 = 已生成格子[i];
            if (格子?.根节点 != null)
            {
                格子.根节点.gameObject.SetActive(显示);
            }
        }
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

    private static string 构建技能签名(string 角色ID, BattleUnit 当前单位, List<CharacterSkillListUtility.DisplaySkillEntry> 技能列表)
    {
        int 当前行动力 = 当前单位 != null ? 当前单位.currentActionPoints : -1;
        int 当前法力 = 当前单位 != null ? 当前单位.currentMana : -1;
        if (技能列表 == null || 技能列表.Count == 0)
        {
            return $"{角色ID ?? string.Empty}|AP:{当前行动力}|MP:{当前法力}";
        }

        List<string> 签名片段 = new List<string>(技能列表.Count);
        for (int i = 0; i < 技能列表.Count; i++)
        {
            CharacterSkillListUtility.DisplaySkillEntry 条目 = 技能列表[i];
            签名片段.Add($"{条目.SkillId}:{条目.SkillSource}");
        }

        return $"{角色ID ?? string.Empty}|AP:{当前行动力}|MP:{当前法力}|" + string.Join("|", 签名片段);
    }

    private void 重建技能格子(List<CharacterSkillListUtility.DisplaySkillEntry> 技能列表)
    {
        清空已生成格子();
        if (战斗技能格子区域 == null || 技能格子prefab == null || 技能列表 == null)
        {
            return;
        }

        for (int i = 0; i < 技能列表.Count; i++)
        {
            CharacterSkillListUtility.DisplaySkillEntry 技能条目 = 技能列表[i];
            string 技能ID = 技能条目.SkillId;
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
                技能ID = 技能ID,
                技能来源 = 技能条目.SkillSource
            };

            刷新技能格显示(格子);
            确保悬停转发(格子, i);
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

        BattleUnit 当前单位 = 战斗回合系统 != null ? 战斗回合系统.ActiveUnit : null;
        if (SkillUsabilityUtility.技能无法使用(技能数据库, 当前角色ID, 格子.技能ID, 当前单位))
        {
            return;
        }

        战斗回合系统.ToggleSkillMode(格子.技能ID, 格子.技能来源);
    }

    private void 处理技能悬停进入(int 索引)
    {
        if (索引 < 0 || 索引 >= 已生成格子.Count)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        技能格组件 格子 = 已生成格子[索引];
        if (格子 == null || 格子.根节点 == null || string.IsNullOrWhiteSpace(格子.技能ID))
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        BattleSkillDatabase.SkillEntry 条目 = 技能数据库 != null ? 技能数据库.FindEntry(格子.技能ID) : null;
        if (条目 == null ||
            (条目.group != BattleSkillDatabase.SkillGroup.CombatArt &&
             条目.group != BattleSkillDatabase.SkillGroup.Spell))
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        float 攻击力 = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(当前角色ID, 格子.技能来源);
        float 倍率 = Mathf.Max(0f, 条目.damageMultiplier);
        SkillTooltipRuntime.Snapshot snapshot = new SkillTooltipRuntime.Snapshot
        {
            skillId = 格子.技能ID,
            displayName = 格子.技能ID,
            description = 条目.description ?? string.Empty,
            ownerCharacterId = 当前角色ID ?? string.Empty,
            skillSource = 格子.技能来源,
            hitRate = 解析显示命中率(当前角色ID, 条目),
            damage = Mathf.Max(0, Mathf.RoundToInt(攻击力 * 倍率)),
            icon = 条目.icon,
            isEmpty = false
        };

        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Skill,
            格子.根节点,
            技能提示延迟秒,
            () => SkillTooltipRuntime.Show(snapshot),
            SkillTooltipRuntime.Hide);
    }

    private void 处理技能悬停离开(int 索引, PointerEventData eventData)
    {
        技能格组件 格子 = 索引 >= 0 && 索引 < 已生成格子.Count ? 已生成格子[索引] : null;
        if (格子 == null || 格子.根节点 == null)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Skill, 格子.根节点, eventData);
    }

    private void 刷新技能格显示(技能格组件 格子)
    {
        if (格子 == null)
        {
            return;
        }

        Sprite 图标 = 解析技能图标(格子.技能ID);
        BattleUnit 当前单位 = 战斗回合系统 != null ? 战斗回合系统.ActiveUnit : null;
        bool 可用 = !SkillUsabilityUtility.技能无法使用(技能数据库, 当前角色ID, 格子.技能ID, 当前单位);

        if (格子.空图标 != null)
        {
            格子.空图标.gameObject.SetActive(图标 == null);
        }

        if (格子.技能图标 != null)
        {
            格子.技能图标.sprite = 图标;
            格子.技能图标.gameObject.SetActive(图标 != null);
            格子.技能图标.color = 可用 ? SkillUsabilityUtility.EnabledSkillColor : SkillUsabilityUtility.DisabledSkillColor;
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
        HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);

        for (int i = 0; i < 已生成格子.Count; i++)
        {
            技能格组件 格子 = 已生成格子[i];
            if (格子?.根节点 != null)
            {
                Destroy(格子.根节点.gameObject);
            }
        }

        已生成格子.Clear();
    }

    private void 确保悬停转发(技能格组件 格子, int 索引)
    {
        if (格子 == null || 格子.根节点 == null)
        {
            return;
        }

        if (格子.悬停转发 == null)
        {
            格子.悬停转发 = 格子.根节点.GetComponent<技能悬停转发>();
            if (格子.悬停转发 == null)
            {
                格子.悬停转发 = 格子.根节点.gameObject.AddComponent<技能悬停转发>();
            }
        }

        格子.悬停转发.配置(this, 索引);
    }

    private static int 解析显示命中率(string 角色ID, BattleSkillDatabase.SkillEntry 技能)
    {
        CharacterStatDatabase 属性数据库 = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry 属性条目 =
            属性数据库 != null ? 属性数据库.FindEntry(string.IsNullOrWhiteSpace(角色ID) ? "玩家" : 角色ID) : null;
        int 基础命中率 = 属性条目 != null ? 属性条目.ResolveHitRate() : 100;
        return Mathf.Max(0, 基础命中率 + (技能 != null ? 技能.ResolveHitRateModifier() : 0));
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
