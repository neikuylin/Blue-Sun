using System;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 战斗模式服务
{
    internal struct 进入战斗结果
    {
        public BattleUnit 待进入战斗单位;
        public bool 进入战斗动画进行中;
        public bool 进入战斗后开始回合;
    }

    public BattleUnit 进入探索模式(
        bool 从战斗切换,
        Func<BattleUnit> 查找探索玩家单位,
        Action 播放退出战斗动画,
        Action 播放探索待机动画,
        Action<BattleUnit> 聚焦到单位,
        Action<bool> 设置战斗界面可见,
        Action 刷新模式音乐,
        Action 刷新选中描边,
        Action 刷新高亮,
        Action 刷新当前单位界面,
        Action 刷新时间轴)
    {
        BattleUnit 当前活动单位 = 查找探索玩家单位 != null ? 查找探索玩家单位() : null;
        if (从战斗切换)
        {
            播放退出战斗动画?.Invoke();
        }
        else
        {
            播放探索待机动画?.Invoke();
        }

        if (当前活动单位 != null)
        {
            聚焦到单位?.Invoke(当前活动单位);
        }

        设置战斗界面可见?.Invoke(false);
        刷新模式音乐?.Invoke();
        刷新选中描边?.Invoke();
        刷新高亮?.Invoke();
        刷新当前单位界面?.Invoke();
        刷新时间轴?.Invoke();
        return 当前活动单位;
    }

    public 进入战斗结果 进入战斗模式(
        bool 从探索切换,
        bool 播放进入战斗动画,
        Func<BattleUnit> 获取下一个存活回合单位,
        Action<BattleUnit> 聚焦到单位,
        Action 停止镜头跟随,
        Action<bool> 设置战斗界面可见,
        Action 刷新模式音乐,
        Action 刷新选中描边,
        Action 刷新高亮,
        Action 刷新当前单位界面,
        Action 刷新时间轴)
    {
        设置战斗界面可见?.Invoke(true);
        刷新模式音乐?.Invoke();
        刷新选中描边?.Invoke();
        刷新高亮?.Invoke();
        刷新当前单位界面?.Invoke();
        刷新时间轴?.Invoke();

        if (播放进入战斗动画 && 从探索切换)
        {
            BattleUnit 待进入战斗单位 = 获取下一个存活回合单位 != null ? 获取下一个存活回合单位() : null;
            if (待进入战斗单位 != null)
            {
                聚焦到单位?.Invoke(待进入战斗单位);
            }
            else
            {
                停止镜头跟随?.Invoke();
            }

            return new 进入战斗结果
            {
                待进入战斗单位 = 待进入战斗单位,
                进入战斗后开始回合 = 待进入战斗单位 != null,
                进入战斗动画进行中 = true
            };
        }

        停止镜头跟随?.Invoke();
        return new 进入战斗结果
        {
            待进入战斗单位 = null,
            进入战斗后开始回合 = false,
            进入战斗动画进行中 = false
        };
    }

    public void 设置战斗界面可见(
        BattleTurnTimelineService 时间轴服务,
        BattleSceneBindings 场景绑定,
        Button 结束回合按钮,
        Button 移动按钮,
        bool 可见)
    {
        时间轴服务?.SetVisible(场景绑定, 可见);

        if (结束回合按钮 != null)
        {
            结束回合按钮.gameObject.SetActive(可见);
        }

        if (移动按钮 != null)
        {
            移动按钮.gameObject.SetActive(可见);
        }

        if (场景绑定 != null && 场景绑定.actionPointPanel != null)
        {
            场景绑定.actionPointPanel.gameObject.SetActive(可见);
        }
    }
}
