using System;
using UnityEngine;

public sealed class 弓弦目标拉伸器 : MonoBehaviour
{
    [SerializeField] private string 弦骨骼名称 = "WB.string";
    [SerializeField] private string 目标点名称 = "武器挂载点（右）";
    [SerializeField] private Transform 拉弦目标点;
    [SerializeField, Range(0f, 1f)] private float 拉弦进度;
    [SerializeField] private bool 动画状态驱动启用 = true;
    [SerializeField] private Animator 角色动画器;
    [SerializeField] private 弓弦拉伸动画规则配置 动画规则配置;

    private Transform 弦骨骼;
    [SerializeField, HideInInspector] private Vector3 弦初始本地位置;
    [SerializeField, HideInInspector] private bool 已记录初始姿态;

    public float 当前拉弦进度 => 拉弦进度;

    private void OnEnable()
    {
        重新查找引用(out _);
        if (!已记录初始姿态)
        {
            记录初始姿态();
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        应用动画状态驱动();
        应用当前拉弦();
    }

    private void OnValidate()
    {
        拉弦进度 = Mathf.Clamp01(拉弦进度);
    }

    public void 设置拉弦进度(float progress)
    {
        拉弦进度 = Mathf.Clamp01(progress);
    }

    public bool 重新查找引用(out string result)
    {
        弦骨骼 = FindDescendantByName(transform, 弦骨骼名称);

        if (拉弦目标点 == null)
        {
            拉弦目标点 = FindNearestExternalDescendantByName(transform, 目标点名称);
        }

        if (弦骨骼 == null)
        {
            result = $"没有找到弦骨骼：{弦骨骼名称}";
            return false;
        }

        if (拉弦目标点 == null)
        {
            result = $"没有找到拉弦目标点：{目标点名称}";
            return false;
        }

        result = $"已找到弦骨骼“{弦骨骼.name}”和目标点“{拉弦目标点.name}”。";
        return true;
    }

    public void 重新记录初始姿态()
    {
        重新查找引用(out _);
        记录初始姿态();
    }

    public void 应用当前拉弦()
    {
        if (弦骨骼 == null)
        {
            重新查找引用(out _);
        }

        if (弦骨骼 == null)
        {
            return;
        }

        if (!已记录初始姿态)
        {
            记录初始姿态();
        }

        if (!已记录初始姿态)
        {
            return;
        }

        if (拉弦进度 <= Mathf.Epsilon)
        {
            应用初始姿态();
            return;
        }

        if (拉弦目标点 == null)
        {
            重新查找引用(out _);
        }

        if (拉弦目标点 == null)
        {
            return;
        }

        ApplyBoneTowardTarget(弦骨骼, 弦初始本地位置, 拉弦进度);
    }

    public void 复位拉弦()
    {
        拉弦进度 = 0f;
        if (!已记录初始姿态)
        {
            return;
        }

        应用初始姿态();
    }

    private void 应用动画状态驱动()
    {
        弓弦拉伸动画规则配置.拉弦动画状态规则[] rules = 取得动画状态规则列表();
        if (!动画状态驱动启用 || rules == null || rules.Length == 0)
        {
            return;
        }

        Animator animator = 取得角色动画器();
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        for (int i = 0; i < rules.Length; i++)
        {
            弓弦拉伸动画规则配置.拉弦动画状态规则 rule = rules[i];
            if (rule == null || string.IsNullOrWhiteSpace(rule.状态名) || !状态匹配(stateInfo, rule.状态名))
            {
                continue;
            }

            float progress = Mathf.Clamp01(rule.进入进度);
            if (rule.按时间回零 && 取得当前状态播放秒数(stateInfo) >= rule.回零秒数)
            {
                progress = Mathf.Clamp01(rule.回零后进度);
            }

            设置拉弦进度(progress);
            return;
        }

        设置拉弦进度(0f);
    }

    private 弓弦拉伸动画规则配置.拉弦动画状态规则[] 取得动画状态规则列表()
    {
        弓弦拉伸动画规则配置 config = 动画规则配置;
        if (config == null)
        {
            config = 弓弦拉伸动画规则配置.加载默认配置();
            if (config != null)
            {
                动画规则配置 = config;
            }
        }

        return config != null ? config.规则列表 : null;
    }

    private Animator 取得角色动画器()
    {
        if (角色动画器 != null && 角色动画器.runtimeAnimatorController != null)
        {
            return 角色动画器;
        }

        Transform current = transform;
        while (current != null)
        {
            Animator animator = current.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                角色动画器 = animator;
                return 角色动画器;
            }

            current = current.parent;
        }

        return 角色动画器;
    }

    private static float 取得当前状态播放秒数(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.length <= 0f)
        {
            return 0f;
        }

        float normalizedTime = stateInfo.loop ? Mathf.Repeat(stateInfo.normalizedTime, 1f) : Mathf.Clamp01(stateInfo.normalizedTime);
        return normalizedTime * stateInfo.length;
    }

    private static bool 状态匹配(AnimatorStateInfo stateInfo, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int hash = Animator.StringToHash(stateName);
        return stateInfo.shortNameHash == hash ||
            stateInfo.fullPathHash == hash ||
            stateInfo.IsName(stateName);
    }

    private void 记录初始姿态()
    {
        if (弦骨骼 == null)
        {
            return;
        }

        弦初始本地位置 = 弦骨骼.localPosition;
        已记录初始姿态 = true;
    }

    private void 应用初始姿态()
    {
        if (弦骨骼 != null)
        {
            弦骨骼.localPosition = 弦初始本地位置;
        }
    }

    private void ApplyBoneTowardTarget(Transform bone, Vector3 restLocalPosition, float progress)
    {
        if (bone == null || bone.parent == null)
        {
            return;
        }

        Vector3 targetLocalPosition = bone.parent.InverseTransformPoint(拉弦目标点.position);
        bone.localPosition = Vector3.LerpUnclamped(restLocalPosition, targetLocalPosition, Mathf.Clamp01(progress));
    }

    private static Transform FindNearestExternalDescendantByName(Transform source, string targetName)
    {
        if (source == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Transform current = source.parent;
        while (current != null)
        {
            Transform found = FindDescendantByName(current, targetName);
            if (found != null && !found.IsChildOf(source))
            {
                return found;
            }

            current = current.parent;
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, targetName, StringComparison.Ordinal))
            {
                return child;
            }

            Transform descendant = FindDescendantByName(child, targetName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
