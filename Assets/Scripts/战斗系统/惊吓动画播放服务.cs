using UnityEngine;

public static class 惊吓动画播放服务
{
    private const string 位置锚点名称 = "__惊吓动画位置";

    public static GameObject 播放(BattleUnit 目标单位)
    {
        return 播放(目标单位, 惊吓动画配置.加载默认配置());
    }

    public static GameObject 播放(BattleUnit 目标单位, 惊吓动画配置 配置)
    {
        if (目标单位 == null)
        {
            Debug.LogWarning("惊吓动画：没有指定播放单位。");
            return null;
        }

        if (配置 == null || 配置.动画预制体资源 == null)
        {
            Debug.LogWarning("惊吓动画：缺少 Resources/惊吓动画配置 或动画预制体。");
            return null;
        }

        Transform 位置锚点 = 获取或创建位置锚点(目标单位, 配置);
        GameObject 实例 = Object.Instantiate(配置.动画预制体资源, 位置锚点, false);
        实例.name = $"惊吓动画_{目标单位.characterId}";
        实例.transform.localPosition = Vector3.zero;
        实例.transform.localRotation = Quaternion.identity;
        实例.transform.localScale = 配置.动画预制体资源.transform.localScale * 配置.整体缩放值;

        SpriteRenderer[] 精灵渲染器 = 实例.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < 精灵渲染器.Length; i++)
        {
            精灵渲染器[i].sortingOrder = 配置.渲染顺序值;
        }

        惊吓动画跟随器 跟随器 = 实例.GetComponent<惊吓动画跟随器>();
        if (跟随器 == null)
        {
            跟随器 = 实例.AddComponent<惊吓动画跟随器>();
        }

        跟随器.初始化(目标单位, Camera.main);
        return 实例;
    }

    private static Transform 获取或创建位置锚点(BattleUnit 目标单位, 惊吓动画配置 配置)
    {
        Transform 单位根节点 = 目标单位.transform;
        Transform 位置锚点 = 单位根节点.Find(位置锚点名称);
        if (位置锚点 == null)
        {
            GameObject 位置锚点对象 = new GameObject(位置锚点名称);
            位置锚点 = 位置锚点对象.transform;
            位置锚点.SetParent(单位根节点, false);
        }

        Bounds 模型局部边界;
        if (尝试获取模型局部边界(目标单位, 位置锚点, out 模型局部边界))
        {
            位置锚点.localPosition = new Vector3(
                模型局部边界.max.x + 配置.右侧偏移量,
                模型局部边界.max.y + 配置.顶部偏移量,
                模型局部边界.center.z);
        }
        else
        {
            位置锚点.localPosition = new Vector3(
                配置.右侧偏移量,
                2f + 配置.顶部偏移量,
                0f);
        }

        位置锚点.localRotation = Quaternion.identity;
        位置锚点.localScale = Vector3.one;
        return 位置锚点;
    }

    private static bool 尝试获取模型局部边界(
        BattleUnit 目标单位,
        Transform 忽略层级,
        out Bounds 模型局部边界)
    {
        Renderer[] 渲染器列表 = 目标单位.GetComponentsInChildren<Renderer>(true);
        bool 已有边界 = false;
        模型局部边界 = default;
        for (int i = 0; i < 渲染器列表.Length; i++)
        {
            Renderer 渲染器 = 渲染器列表[i];
            if (渲染器 == null ||
                !渲染器.enabled ||
                渲染器.GetComponent<BattleUnitOutlineMarker>() != null ||
                渲染器.transform.IsChildOf(忽略层级))
            {
                continue;
            }

            Bounds 世界边界 = 渲染器.bounds;
            Vector3 最小值 = 世界边界.min;
            Vector3 最大值 = 世界边界.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 世界角点 = new Vector3(
                            x == 0 ? 最小值.x : 最大值.x,
                            y == 0 ? 最小值.y : 最大值.y,
                            z == 0 ? 最小值.z : 最大值.z);
                        Vector3 局部角点 = 目标单位.transform.InverseTransformPoint(世界角点);
                        if (!已有边界)
                        {
                            模型局部边界 = new Bounds(局部角点, Vector3.zero);
                            已有边界 = true;
                        }
                        else
                        {
                            模型局部边界.Encapsulate(局部角点);
                        }
                    }
                }
            }
        }

        return 已有边界;
    }
}
