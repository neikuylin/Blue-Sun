using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("特效/武器特效状态桥接器")]
public sealed class 武器特效状态桥接器 : MonoBehaviour
{
    [Serializable]
    public sealed class 效果驱动特效条目
    {
        public string 效果ID = string.Empty;
        public MonoBehaviour 特效脚本;
    }

    [SerializeField] private List<效果驱动特效条目> 效果驱动特效列表 = new List<效果驱动特效条目>();

    private BattleUnit 所属单位;
    private bool 已警告缺少单位;

    public List<效果驱动特效条目> 当前效果驱动特效列表 => 效果驱动特效列表;

    private void OnEnable()
    {
        解析所属单位();
        刷新武器特效状态();
    }

    private void Update()
    {
        刷新武器特效状态();
    }

    [ContextMenu("刷新武器特效状态")]
    public void 刷新武器特效状态()
    {
        if (所属单位 == null)
        {
            解析所属单位();
        }

        if (所属单位 == null)
        {
            if (!已警告缺少单位)
            {
                Debug.LogWarning($"[武器特效状态桥接器] {name} 找不到父级 BattleUnit，无法读取角色身上的效果。", this);
                已警告缺少单位 = true;
            }

            return;
        }

        已警告缺少单位 = false;
        if (效果驱动特效列表 == null)
        {
            return;
        }

        for (int i = 0; i < 效果驱动特效列表.Count; i++)
        {
            效果驱动特效条目 entry = 效果驱动特效列表[i];
            if (entry == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.效果ID))
            {
                Debug.LogWarning($"[武器特效状态桥接器] {name} 第{i + 1}条没有配置效果ID。", this);
                continue;
            }

            武器特效开关接口 effectSwitch = entry.特效脚本 as 武器特效开关接口;
            if (effectSwitch == null)
            {
                Debug.LogWarning($"[武器特效状态桥接器] {name} 第{i + 1}条没有绑定可开关的武器特效脚本。", this);
                continue;
            }

            effectSwitch.设置武器特效启用(单位拥有持续效果(所属单位, entry.效果ID));
        }
    }

    private void 解析所属单位()
    {
        所属单位 = GetComponentInParent<BattleUnit>(true);
    }

    private static bool 单位拥有持续效果(BattleUnit unit, string effectId)
    {
        if (unit == null || unit.ActiveEffects == null || string.IsNullOrWhiteSpace(effectId))
        {
            return false;
        }

        for (int i = 0; i < unit.ActiveEffects.Count; i++)
        {
            BattleUnit.ActiveEffectState activeEffect = unit.ActiveEffects[i];
            if (activeEffect == null || activeEffect.remainingTurns <= 0)
            {
                continue;
            }

            if (string.Equals(activeEffect.effectId, effectId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
