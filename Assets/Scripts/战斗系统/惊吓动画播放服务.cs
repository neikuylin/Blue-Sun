using UnityEngine;

public static class 惊吓动画播放服务
{
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

        GameObject 实例 = Object.Instantiate(配置.动画预制体资源);
        实例.name = $"惊吓动画_{目标单位.characterId}";
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

        跟随器.初始化(目标单位, Camera.main, 配置);
        Object.Destroy(实例, 配置.显示时长秒数);
        return 实例;
    }
}
