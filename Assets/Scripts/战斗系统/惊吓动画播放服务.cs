using UnityEngine;

public static class 惊吓动画播放服务
{
    private const string 位置锚点名称前缀 = "__角色右上特效锚点_";

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
        if (位置锚点 == null)
        {
            return null;
        }

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

        跟随器.初始化();
        return 实例;
    }

    private static Transform 获取或创建位置锚点(BattleUnit 目标单位, 惊吓动画配置 配置)
    {
        Transform 角色父级 = 目标单位.transform.parent;
        string 位置锚点名称 = 位置锚点名称前缀 + 目标单位.GetInstanceID();
        Transform 位置锚点 = 角色父级 != null ? 角色父级.Find(位置锚点名称) : null;
        if (位置锚点 == null && 角色父级 == null)
        {
            GameObject[] 根物体 = 目标单位.gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < 根物体.Length; i++)
            {
                if (根物体[i].name == 位置锚点名称)
                {
                    位置锚点 = 根物体[i].transform;
                    break;
                }
            }
        }

        if (位置锚点 == null)
        {
            GameObject 位置锚点对象 = new GameObject(位置锚点名称);
            位置锚点 = 位置锚点对象.transform;
            if (角色父级 != null)
            {
                位置锚点.SetParent(角色父级, false);
            }
        }

        角色特效锚点跟随器 锚点跟随器 = 位置锚点.GetComponent<角色特效锚点跟随器>();
        if (锚点跟随器 == null)
        {
            锚点跟随器 = 位置锚点.gameObject.AddComponent<角色特效锚点跟随器>();
        }

        if (!锚点跟随器.初始化(
                目标单位,
                Camera.main,
                配置.右侧偏移量,
                配置.顶部偏移量))
        {
            Debug.LogError($"惊吓动画：单位“{目标单位.characterId}”没有可用于定位的角色本体 SkinnedMeshRenderer。", 目标单位);
            Object.Destroy(位置锚点.gameObject);
            return null;
        }

        return 位置锚点;
    }
}
