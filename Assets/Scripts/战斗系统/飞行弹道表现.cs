using System;
using UnityEngine;

public sealed class 飞行弹道表现 : MonoBehaviour
{
    public enum 武器挂载点选择
    {
        左手,
        右手
    }

    private const string 左手武器挂载点名称 = "武器挂载点（左）";
    private const string 右手武器挂载点名称 = "武器挂载点（右）";

    [SerializeField] private float 飞行速度 = 8f;
    [SerializeField] private 武器挂载点选择 出发武器挂载点 = 武器挂载点选择.右手;

    private Transform 目标受击点;
    private Action 到达回调;
    private bool 正在飞行;

    public void 播放(BattleUnit 施法者, BattleUnit 目标, Action 到达后执行 = null)
    {
        if (施法者 == null)
        {
            Debug.LogWarning("[Projectile] 施法者为空，无法播放飞行弹道。", this);
            Destroy(gameObject);
            return;
        }

        if (目标 == null)
        {
            Debug.LogWarning("[Projectile] 目标为空，无法播放飞行弹道。", this);
            Destroy(gameObject);
            return;
        }

        Transform 出发点 = 查找出发武器挂载点(施法者.transform);
        if (出发点 == null)
        {
            Debug.LogWarning($"[Projectile] {施法者.unitName} 没有找到{取得出发挂载点名称()}，无法播放飞行弹道。", 施法者);
            Destroy(gameObject);
            return;
        }

        目标受击点 = 查找Avatar胸口受击点(目标);
        if (目标受击点 == null)
        {
            Destroy(gameObject);
            return;
        }

        到达回调 = 到达后执行;
        transform.position = 出发点.position;
        正在飞行 = true;
    }

    private void Update()
    {
        if (!正在飞行)
        {
            return;
        }

        if (目标受击点 == null)
        {
            Debug.LogWarning("[Projectile] 目标受击点已丢失，飞行弹道中断。", this);
            Destroy(gameObject);
            return;
        }

        float speed = Mathf.Max(0.01f, 飞行速度);
        Vector3 targetPosition = 目标受击点.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        if ((transform.position - targetPosition).sqrMagnitude <= 0.0001f)
        {
            正在飞行 = false;
            到达回调?.Invoke();
            Destroy(gameObject);
        }
    }

    private Transform 查找出发武器挂载点(Transform root)
    {
        string mountName = 取得出发挂载点名称();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && string.Equals(current.name, mountName, StringComparison.Ordinal))
            {
                return current;
            }
        }

        return null;
    }

    private string 取得出发挂载点名称()
    {
        return 出发武器挂载点 == 武器挂载点选择.左手 ? 左手武器挂载点名称 : 右手武器挂载点名称;
    }

    private static Transform 查找Avatar胸口受击点(BattleUnit target)
    {
        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogWarning($"[Projectile] {target.unitName} 没有 Animator，无法按 Avatar Chest 定位弹道目标。", target);
            return null;
        }

        if (!animator.isHuman)
        {
            Debug.LogWarning($"[Projectile] {target.unitName} 的 Animator 不是 Humanoid，无法按 Avatar Chest 定位弹道目标。", animator);
            return null;
        }

        Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null)
        {
            Debug.LogWarning($"[Projectile] {target.unitName} 的 Avatar 没有绑定 Chest，无法定位弹道目标。", animator);
            return null;
        }

        return chest;
    }
}
