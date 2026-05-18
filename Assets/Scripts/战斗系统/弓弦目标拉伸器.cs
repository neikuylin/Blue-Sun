using System;
using UnityEngine;

public sealed class 弓弦目标拉伸器 : MonoBehaviour
{
    [SerializeField] private string 弦骨骼名称 = "WB.string";
    [SerializeField] private string 上弓臂IK名称 = "WB.top.ik";
    [SerializeField] private string 下弓臂IK名称 = "WB.down.ik";
    [SerializeField] private string 目标点名称 = "武器挂载点（右）";
    [SerializeField] private Transform 拉弦目标点;
    [SerializeField, Range(0f, 1f)] private float 拉弦进度;
    [SerializeField, Range(0f, 1f)] private float 弓臂跟随比例 = 0.15f;

    private Transform 弦骨骼;
    private Transform 上弓臂IK;
    private Transform 下弓臂IK;
    [SerializeField, HideInInspector] private Vector3 弦初始本地位置;
    [SerializeField, HideInInspector] private Vector3 上弓臂IK初始本地位置;
    [SerializeField, HideInInspector] private Vector3 下弓臂IK初始本地位置;
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

        应用当前拉弦();
    }

    private void OnValidate()
    {
        拉弦进度 = Mathf.Clamp01(拉弦进度);
        弓臂跟随比例 = Mathf.Clamp01(弓臂跟随比例);
    }

    public void 设置拉弦进度(float progress)
    {
        拉弦进度 = Mathf.Clamp01(progress);
    }

    public bool 重新查找引用(out string result)
    {
        弦骨骼 = FindDescendantByName(transform, 弦骨骼名称);
        上弓臂IK = FindDescendantByName(transform, 上弓臂IK名称);
        下弓臂IK = FindDescendantByName(transform, 下弓臂IK名称);

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
        ApplyBoneTowardTarget(上弓臂IK, 上弓臂IK初始本地位置, 拉弦进度 * 弓臂跟随比例);
        ApplyBoneTowardTarget(下弓臂IK, 下弓臂IK初始本地位置, 拉弦进度 * 弓臂跟随比例);
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

    private void 记录初始姿态()
    {
        if (弦骨骼 == null)
        {
            return;
        }

        弦初始本地位置 = 弦骨骼.localPosition;
        上弓臂IK初始本地位置 = 上弓臂IK != null ? 上弓臂IK.localPosition : Vector3.zero;
        下弓臂IK初始本地位置 = 下弓臂IK != null ? 下弓臂IK.localPosition : Vector3.zero;
        已记录初始姿态 = true;
    }

    private void 应用初始姿态()
    {
        if (弦骨骼 != null)
        {
            弦骨骼.localPosition = 弦初始本地位置;
        }

        if (上弓臂IK != null)
        {
            上弓臂IK.localPosition = 上弓臂IK初始本地位置;
        }

        if (下弓臂IK != null)
        {
            下弓臂IK.localPosition = 下弓臂IK初始本地位置;
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
