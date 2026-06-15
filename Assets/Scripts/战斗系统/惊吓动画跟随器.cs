using System.Collections.Generic;
using UnityEngine;

public sealed class 惊吓动画跟随器 : MonoBehaviour
{
    private BattleUnit 目标单位;
    private Camera 目标摄像机;
    private float 右侧偏移;
    private float 顶部偏移;

    public void 初始化(BattleUnit 单位, Camera 摄像机, 惊吓动画配置 配置)
    {
        目标单位 = 单位;
        目标摄像机 = 摄像机 != null ? 摄像机 : Camera.main;
        右侧偏移 = 配置 != null ? 配置.右侧偏移量 : 0f;
        顶部偏移 = 配置 != null ? 配置.顶部偏移量 : 0f;
        更新位置和朝向();
    }

    private void LateUpdate()
    {
        if (目标单位 == null)
        {
            Destroy(gameObject);
            return;
        }

        if (目标摄像机 == null)
        {
            目标摄像机 = Camera.main;
        }

        更新位置和朝向();
    }

    private void 更新位置和朝向()
    {
        if (目标单位 == null)
        {
            return;
        }

        Bounds 模型边界;
        Vector3 顶部中心 = 尝试获取模型边界(目标单位, out 模型边界)
            ? new Vector3(模型边界.center.x, 模型边界.max.y, 模型边界.center.z)
            : 目标单位.transform.position + Vector3.up * 2f;

        Vector3 右方向 = 目标摄像机 != null ? 目标摄像机.transform.right : Vector3.right;
        transform.position = 顶部中心 + 右方向 * 右侧偏移 + Vector3.up * 顶部偏移;

        if (目标摄像机 != null)
        {
            transform.rotation = 目标摄像机.transform.rotation;
        }
    }

    private static bool 尝试获取模型边界(BattleUnit 单位, out Bounds 合并边界)
    {
        Renderer[] 渲染器列表 = 单位.GetComponentsInChildren<Renderer>(true);
        List<Renderer> 有效渲染器 = new List<Renderer>();
        for (int i = 0; i < 渲染器列表.Length; i++)
        {
            Renderer 渲染器 = 渲染器列表[i];
            if (渲染器 == null ||
                !渲染器.enabled ||
                渲染器.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            有效渲染器.Add(渲染器);
        }

        if (有效渲染器.Count == 0)
        {
            合并边界 = default;
            return false;
        }

        合并边界 = 有效渲染器[0].bounds;
        for (int i = 1; i < 有效渲染器.Count; i++)
        {
            合并边界.Encapsulate(有效渲染器[i].bounds);
        }

        return true;
    }
}
