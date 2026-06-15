using UnityEngine;

public sealed class 角色特效锚点跟随器 : MonoBehaviour
{
    private BattleUnit 目标单位;
    private Camera 目标摄像机;
    private float 右侧偏移;
    private float 顶部偏移;

    public BattleUnit 目标 => 目标单位;

    public bool 初始化(BattleUnit 单位, Camera 摄像机, float 右偏移, float 上偏移)
    {
        目标单位 = 单位;
        目标摄像机 = 摄像机 != null ? 摄像机 : Camera.main;
        右侧偏移 = 右偏移;
        顶部偏移 = 上偏移;

        if (!尝试获取模型边界(out _))
        {
            return false;
        }

        更新位置和朝向();
        return true;
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

        if (!尝试获取模型边界(out Bounds 模型边界))
        {
            return;
        }

        Vector3 顶部中心 = new Vector3(模型边界.center.x, 模型边界.max.y, 模型边界.center.z);
        Vector3 摄像机右方向 = 目标摄像机 != null ? 目标摄像机.transform.right : Vector3.right;
        transform.position = 顶部中心 + 摄像机右方向 * 右侧偏移 + Vector3.up * 顶部偏移;
        transform.rotation = 目标摄像机 != null ? 目标摄像机.transform.rotation : Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private bool 尝试获取模型边界(out Bounds 合并边界)
    {
        SkinnedMeshRenderer[] 渲染器列表 = 目标单位.GetComponentsInChildren<SkinnedMeshRenderer>(false);
        bool 已有边界 = false;
        合并边界 = default;
        for (int i = 0; i < 渲染器列表.Length; i++)
        {
            SkinnedMeshRenderer 渲染器 = 渲染器列表[i];
            if (渲染器 == null ||
                !渲染器.enabled ||
                渲染器.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            if (!已有边界)
            {
                合并边界 = 渲染器.bounds;
                已有边界 = true;
            }
            else
            {
                合并边界.Encapsulate(渲染器.bounds);
            }
        }

        return 已有边界;
    }
}
