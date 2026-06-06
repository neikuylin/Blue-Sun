using System;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("特效/效果特效状态启用脚本")]
public sealed class 效果特效状态启用脚本 : MonoBehaviour
{
    private BattleUnit 所属单位;
    private bool 已警告缺少单位;
    private bool 已警告缺少全局配置;

    private void OnEnable()
    {
        解析所属单位();
        刷新效果特效状态();
    }

    private void Update()
    {
        刷新效果特效状态();
    }

    [ContextMenu("刷新效果特效状态")]
    public void 刷新效果特效状态()
    {
        if (!Application.isPlaying && !gameObject.scene.IsValid())
        {
            return;
        }

        效果特效全局配置 config = 效果特效全局配置.LoadDefault();
        if (config == null)
        {
            if (!已警告缺少全局配置)
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 找不到 Resources/{效果特效全局配置.DefaultResourcePath}，无法读取模型特效绑定。", this);
                已警告缺少全局配置 = true;
            }

            return;
        }

        已警告缺少全局配置 = false;
        if (config.模型特效绑定 == null)
        {
            return;
        }

        确保模型特效组件(config);

        if (所属单位 == null && !存在预览启用(config))
        {
            解析所属单位();
        }

        if (所属单位 == null && !存在预览启用(config))
        {
            if (!已警告缺少单位)
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 找不到父级 BattleUnit，无法读取角色身上的效果。", this);
                已警告缺少单位 = true;
            }
        }
        else
        {
            已警告缺少单位 = false;
        }

        for (int i = 0; i < config.模型特效绑定.Count; i++)
        {
            效果特效全局配置.效果特效绑定条目 entry = config.模型特效绑定[i];
            if (entry == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.效果ID))
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{i + 1}条没有配置效果ID。", this);
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.特效脚本类型名))
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{i + 1}条没有配置特效脚本。", this);
                continue;
            }

            Type effectType = 解析特效脚本类型(entry.特效脚本类型名);
            if (effectType == null)
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{i + 1}条找不到特效脚本类型：{entry.特效脚本类型名}", this);
                continue;
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(effectType))
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{i + 1}条绑定的类型不是 MonoBehaviour：{effectType.Name}", this);
                continue;
            }

            if (!typeof(效果特效开关接口).IsAssignableFrom(effectType))
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{i + 1}条绑定的脚本没有实现“效果特效开关接口”：{effectType.Name}", this);
                continue;
            }

            MonoBehaviour effectComponent = 获取或添加特效组件(effectType, i);
            if (effectComponent == null)
            {
                continue;
            }

            effectComponent.enabled = true;
            效果特效开关接口 effectSwitch = effectComponent as 效果特效开关接口;
            if (effectSwitch == null)
            {
                Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{i + 1}条绑定的脚本没有实现“效果特效开关接口”：{effectType.Name}", this);
                continue;
            }

            effectSwitch.设置效果特效启用(entry.预览启用 || 单位拥有持续效果(所属单位, entry.效果ID));
        }
    }

    private void OnValidate()
    {
        刷新效果特效状态();
    }

    private void 确保模型特效组件(效果特效全局配置 config)
    {
        for (int i = 0; i < config.模型特效绑定.Count; i++)
        {
            效果特效全局配置.效果特效绑定条目 entry = config.模型特效绑定[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.特效脚本类型名))
            {
                continue;
            }

            Type effectType = 解析特效脚本类型(entry.特效脚本类型名);
            if (effectType == null || !typeof(MonoBehaviour).IsAssignableFrom(effectType))
            {
                continue;
            }

            MonoBehaviour effectComponent = 获取或添加特效组件(effectType, i);
            效果特效开关接口 effectSwitch = effectComponent as 效果特效开关接口;
            if (effectSwitch != null && 所属单位 == null)
            {
                effectComponent.enabled = true;
                effectSwitch.设置效果特效启用(entry.预览启用);
            }
        }
    }

    private static bool 存在预览启用(效果特效全局配置 config)
    {
        if (config == null || config.模型特效绑定 == null)
        {
            return false;
        }

        for (int i = 0; i < config.模型特效绑定.Count; i++)
        {
            效果特效全局配置.效果特效绑定条目 entry = config.模型特效绑定[i];
            if (entry != null && entry.预览启用)
            {
                return true;
            }
        }

        return false;
    }

    private MonoBehaviour 获取或添加特效组件(Type effectType, int entryIndex)
    {
        Component existing = GetComponent(effectType);
        if (existing != null)
        {
            return existing as MonoBehaviour;
        }

        Component added = gameObject.AddComponent(effectType);
        MonoBehaviour addedBehaviour = added as MonoBehaviour;
        if (addedBehaviour == null)
        {
            Debug.LogWarning($"[效果特效状态启用脚本] {name} 第{entryIndex + 1}条无法添加特效脚本：{effectType.Name}", this);
        }

        return addedBehaviour;
    }

    private static Type 解析特效脚本类型(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        string normalized = typeName.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return Type.GetType(normalized);
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
