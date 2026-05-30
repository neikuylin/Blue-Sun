using System;
using System.Collections;
using UnityEngine;

internal sealed class 战斗技能执行服务
{
    public Coroutine 尝试使用当前技能(
        MonoBehaviour 宿主,
        Coroutine 当前执行协程,
        BattleUnit 单位,
        Vector2Int 点击格子,
        BattleUnit 目标,
        bool 技能模式激活,
        bool 正在结算技能,
        bool 技能目标选择已就绪,
        string 当前技能ID,
        string 当前技能来源,
        BattleSkillDatabase.SkillEntry 当前技能,
        Func<BattleUnit, Vector2Int, BattleUnit, BattleSkillDatabase.SkillEntry, bool> 可以在目标释放技能,
        Action<BattleUnit, Vector2Int> 尝试移动,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> 获取技能行动点消耗,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> 获取技能法力消耗,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> 记录技能使用,
        Action<bool> 设置技能结算状态,
        Action<BattleUnit, BattleUnit> 面向目标单位,
        Action<BattleUnit, Vector2Int> 面向目标格子,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, Func<int, IEnumerator>, IEnumerator> 播放技能动画并在结算点执行,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> 结算单体技能,
        Action<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, int> 结算范围技能,
        Action 清理主动技能模式,
        Action 刷新高亮,
        Action 刷新时间轴,
        Action 尝试进入待处理探索模式,
        bool 消耗资源 = true,
        bool 执行后清理技能模式 = true)
    {
        if (!技能模式激活 || 正在结算技能 || !技能目标选择已就绪 || 宿主 == null || 单位 == null)
        {
            return 当前执行协程;
        }

        if (string.Equals(当前技能ID, BattleSkillDatabase.MoveSkillId, StringComparison.Ordinal))
        {
            if (目标 != null && 目标 != 单位)
            {
                return 当前执行协程;
            }

            尝试移动?.Invoke(单位, 点击格子);
            return 当前执行协程;
        }

        if (当前技能 == null || 可以在目标释放技能 == null || !可以在目标释放技能(单位, 点击格子, 目标, 当前技能))
        {
            return 当前执行协程;
        }

        if (当前执行协程 != null)
        {
            宿主.StopCoroutine(当前执行协程);
        }

        if (当前技能.skillType == BattleSkillDatabase.SkillType.Target)
        {
            return 宿主.StartCoroutine(执行单体技能协程(
                单位,
                目标,
                当前技能来源,
                当前技能,
                获取技能行动点消耗,
                获取技能法力消耗,
                记录技能使用,
                设置技能结算状态,
                面向目标单位,
                播放技能动画并在结算点执行,
                结算单体技能,
                清理主动技能模式,
                刷新高亮,
                刷新时间轴,
                尝试进入待处理探索模式,
                消耗资源,
                执行后清理技能模式));
        }

        if (当前技能.skillType == BattleSkillDatabase.SkillType.Area)
        {
            return 宿主.StartCoroutine(执行范围技能协程(
                单位,
                点击格子,
                当前技能来源,
                当前技能,
                获取技能行动点消耗,
                获取技能法力消耗,
                记录技能使用,
                设置技能结算状态,
                面向目标格子,
                播放技能动画并在结算点执行,
                结算范围技能,
                清理主动技能模式,
                刷新高亮,
                刷新时间轴,
                尝试进入待处理探索模式,
                消耗资源,
                执行后清理技能模式));
        }

        return 当前执行协程;
    }

    private IEnumerator 执行单体技能协程(
        BattleUnit 施法者,
        BattleUnit 目标,
        string 技能来源,
        BattleSkillDatabase.SkillEntry 技能,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> 获取技能行动点消耗,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> 获取技能法力消耗,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> 记录技能使用,
        Action<bool> 设置技能结算状态,
        Action<BattleUnit, BattleUnit> 面向目标单位,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, Func<int, IEnumerator>, IEnumerator> 播放技能动画并在结算点执行,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> 结算单体技能,
        Action 清理主动技能模式,
        Action 刷新高亮,
        Action 刷新时间轴,
        Action 尝试进入待处理探索模式,
        bool 消耗资源,
        bool 执行后清理技能模式)
    {
        if (施法者 == null || 目标 == null || 技能 == null)
        {
            yield break;
        }

        int 行动点消耗 = 获取技能行动点消耗 != null ? 获取技能行动点消耗(施法者, 技能) : 0;
        int 法力消耗 = 获取技能法力消耗 != null ? 获取技能法力消耗(施法者, 技能) : 0;
        if (消耗资源 && (!施法者.CanSpendActionPoints(行动点消耗) || !施法者.CanSpendMana(法力消耗)))
        {
            yield break;
        }

        设置技能结算状态?.Invoke(true);
        if (消耗资源)
        {
            施法者.SpendActionPoints(行动点消耗);
            施法者.SpendMana(法力消耗);
            记录技能使用?.Invoke(施法者, 技能);
        }
        面向目标单位?.Invoke(施法者, 目标);
        if (播放技能动画并在结算点执行 != null)
        {
            yield return 播放技能动画并在结算点执行(施法者, 目标, 技能, hitIndex => 执行单体结算协程(施法者, 目标, 技能, hitIndex, 结算单体技能));
        }

        if (执行后清理技能模式)
        {
            清理主动技能模式?.Invoke();
        }

        刷新高亮?.Invoke();
        刷新时间轴?.Invoke();
        设置技能结算状态?.Invoke(false);
        if (执行后清理技能模式)
        {
            尝试进入待处理探索模式?.Invoke();
        }
    }

    private IEnumerator 执行范围技能协程(
        BattleUnit 施法者,
        Vector2Int 目标格子,
        string 技能来源,
        BattleSkillDatabase.SkillEntry 技能,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> 获取技能行动点消耗,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> 获取技能法力消耗,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> 记录技能使用,
        Action<bool> 设置技能结算状态,
        Action<BattleUnit, Vector2Int> 面向目标格子,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, Func<int, IEnumerator>, IEnumerator> 播放技能动画并在结算点执行,
        Action<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, int> 结算范围技能,
        Action 清理主动技能模式,
        Action 刷新高亮,
        Action 刷新时间轴,
        Action 尝试进入待处理探索模式,
        bool 消耗资源,
        bool 执行后清理技能模式)
    {
        if (施法者 == null || 技能 == null)
        {
            yield break;
        }

        int 行动点消耗 = 获取技能行动点消耗 != null ? 获取技能行动点消耗(施法者, 技能) : 0;
        int 法力消耗 = 获取技能法力消耗 != null ? 获取技能法力消耗(施法者, 技能) : 0;
        if (消耗资源 && (!施法者.CanSpendActionPoints(行动点消耗) || !施法者.CanSpendMana(法力消耗)))
        {
            yield break;
        }

        设置技能结算状态?.Invoke(true);
        if (消耗资源)
        {
            施法者.SpendActionPoints(行动点消耗);
            施法者.SpendMana(法力消耗);
            记录技能使用?.Invoke(施法者, 技能);
        }
        面向目标格子?.Invoke(施法者, 目标格子);
        if (播放技能动画并在结算点执行 != null)
        {
            yield return 播放技能动画并在结算点执行(施法者, null, 技能, hitIndex => 执行范围结算协程(施法者, 目标格子, 技能, hitIndex, 结算范围技能));
        }

        if (执行后清理技能模式)
        {
            清理主动技能模式?.Invoke();
        }

        刷新高亮?.Invoke();
        刷新时间轴?.Invoke();
        设置技能结算状态?.Invoke(false);
        if (执行后清理技能模式)
        {
            尝试进入待处理探索模式?.Invoke();
        }
    }

    private static IEnumerator 执行单体结算协程(
        BattleUnit 施法者,
        BattleUnit 目标,
        BattleSkillDatabase.SkillEntry 技能,
        int 命中序号,
        Action<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, int> 结算单体技能)
    {
        结算单体技能?.Invoke(施法者, 目标, 技能, 命中序号);
        yield break;
    }

    private static IEnumerator 执行范围结算协程(
        BattleUnit 施法者,
        Vector2Int 目标格子,
        BattleSkillDatabase.SkillEntry 技能,
        int 命中序号,
        Action<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, int> 结算范围技能)
    {
        结算范围技能?.Invoke(施法者, 目标格子, 技能, 命中序号);
        yield break;
    }
}
