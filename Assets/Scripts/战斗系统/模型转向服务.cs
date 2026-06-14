using UnityEngine;

public static class 模型转向服务
{
    public static Quaternion 计算方向旋转(Transform 来源, 剧情数据库.模型朝向 朝向)
    {
        return 计算世界旋转(来源, 取得方向基础角度(朝向));
    }

    public static Quaternion 计算方向基础旋转(Transform 来源, 剧情数据库.模型朝向 朝向)
    {
        Vector3 世界欧拉角 = 来源 != null ? 来源.eulerAngles : Vector3.zero;
        世界欧拉角.y = 取得方向基础角度(朝向);
        return Quaternion.Euler(世界欧拉角);
    }

    private static float 取得方向基础角度(剧情数据库.模型朝向 朝向)
    {
        switch (朝向)
        {
            case 剧情数据库.模型朝向.东:
                return 90f;
            case 剧情数据库.模型朝向.南:
                return 180f;
            case 剧情数据库.模型朝向.西:
                return 270f;
            default:
                return 0f;
        }
    }

    public static Quaternion 计算面向单位旋转(BattleUnit 来源, BattleUnit 目标)
    {
        Vector3 方向 = 目标.transform.position - 来源.transform.position;
        方向.y = 0f;
        float 基础角度 = Mathf.Atan2(方向.x, 方向.z) * Mathf.Rad2Deg;
        return 计算世界旋转(来源.transform, 基础角度);
    }

    public static BattleUnit 查找确切敌人(BattleUnit 来源, string 目标角色ID)
    {
        if (来源 == null || string.IsNullOrWhiteSpace(目标角色ID))
        {
            return null;
        }

        string 目标ID = 目标角色ID.Trim();
        BattleUnit[] 单位列表 = Object.FindObjectsByType<BattleUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < 单位列表.Length; i++)
        {
            BattleUnit 单位 = 单位列表[i];
            if (是有效敌人(来源, 单位) &&
                string.Equals(单位.characterId, 目标ID, System.StringComparison.Ordinal))
            {
                return 单位;
            }
        }

        return null;
    }

    public static BattleUnit 查找最近敌人(BattleUnit 来源)
    {
        if (来源 == null)
        {
            return null;
        }

        BattleUnit 最近敌人 = null;
        float 最近距离平方 = float.PositiveInfinity;
        BattleUnit[] 单位列表 = Object.FindObjectsByType<BattleUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < 单位列表.Length; i++)
        {
            BattleUnit 单位 = 单位列表[i];
            if (!是有效敌人(来源, 单位))
            {
                continue;
            }

            Vector3 差值 = 单位.transform.position - 来源.transform.position;
            差值.y = 0f;
            float 距离平方 = 差值.sqrMagnitude;
            if (距离平方 < 最近距离平方)
            {
                最近距离平方 = 距离平方;
                最近敌人 = 单位;
            }
        }

        return 最近敌人;
    }

    public static bool 是有效敌人(BattleUnit 来源, BattleUnit 候选)
    {
        return 来源 != null &&
               候选 != null &&
               候选 != 来源 &&
               候选.IsAlive &&
               候选.team != 来源.team;
    }

    private static Quaternion 计算世界旋转(Transform 来源, float 基础角度)
    {
        Vector3 世界欧拉角 = 来源 != null ? 来源.eulerAngles : Vector3.zero;
        世界欧拉角.y = Mathf.Repeat(
            基础角度 + BattleAnimationSettingsResolver.ResolveIdleYawOffset(),
            360f);
        return Quaternion.Euler(世界欧拉角);
    }
}
