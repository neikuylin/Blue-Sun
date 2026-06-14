using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 剧情运行时 : MonoBehaviour
{
    private static 剧情运行时 instance;

    private 剧情数据库.剧情条目 当前剧情;
    private CanvasGroup 黑幕组;
    private Image 黑幕图片;
    private readonly HashSet<string> 已开始蓝图节点 = new HashSet<string>(System.StringComparer.Ordinal);
    private readonly HashSet<string> 已完成蓝图节点 = new HashSet<string>(System.StringComparer.Ordinal);
    private int 正在执行蓝图节点数;
    private bool 正在执行蓝图;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("剧情运行时");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<剧情运行时>();
    }

    private void OnEnable()
    {
        事件剧情硬编码规则.请求播放剧情 += 播放剧情;
    }

    private void OnDisable()
    {
        事件剧情硬编码规则.请求播放剧情 -= 播放剧情;
    }

    public static bool 播放(string 剧情ID)
    {
        return instance != null && instance.开始播放剧情(剧情ID);
    }

    private void 播放剧情(string 剧情ID)
    {
        开始播放剧情(剧情ID);
    }

    private bool 开始播放剧情(string 剧情ID)
    {
        if (string.IsNullOrWhiteSpace(剧情ID))
        {
            Debug.LogError("剧情运行时：剧情ID为空。");
            return false;
        }

        剧情数据库 数据库 = 剧情数据库.加载默认数据库();
        if (数据库 == null)
        {
            Debug.LogError("剧情运行时：缺少剧情数据库。");
            return false;
        }

        剧情数据库.剧情条目 剧情 = 数据库.查找剧情(剧情ID.Trim());
        if (剧情 == null)
        {
            Debug.LogError($"剧情运行时：找不到剧情“{剧情ID}”。");
            return false;
        }

        当前剧情 = 剧情;
        if (剧情.蓝图节点列表 == null || 剧情.蓝图节点列表.Count <= 0)
        {
            Debug.LogError($"剧情运行时：剧情“{剧情ID}”没有蓝图节点。");
            当前剧情 = null;
            return false;
        }

        开始执行蓝图();
        return true;
    }

    private void 开始执行蓝图()
    {
        正在执行蓝图 = true;
        正在执行蓝图节点数 = 0;
        已开始蓝图节点.Clear();
        已完成蓝图节点.Clear();

        bool 已启动节点 = false;
        for (int i = 0; i < 当前剧情.蓝图节点列表.Count; i++)
        {
            剧情数据库.剧情蓝图节点 节点 = 当前剧情.蓝图节点列表[i];
            if (节点 == null || string.IsNullOrWhiteSpace(节点.节点ID))
            {
                continue;
            }

            if (节点.节点类型 == 剧情数据库.剧情蓝图节点类型.开始)
            {
                启动蓝图节点(节点);
                已启动节点 = true;
            }
        }

        if (已启动节点)
        {
            return;
        }

        for (int i = 0; i < 当前剧情.蓝图节点列表.Count; i++)
        {
            剧情数据库.剧情蓝图节点 节点 = 当前剧情.蓝图节点列表[i];
            if (节点 == null || string.IsNullOrWhiteSpace(节点.节点ID) || 取得输入节点ID列表(节点.节点ID).Count > 0)
            {
                continue;
            }

            启动蓝图节点(节点);
            已启动节点 = true;
        }

        if (!已启动节点)
        {
            Debug.LogError($"剧情运行时：剧情“{当前剧情.剧情ID}”没有可执行的蓝图开始节点。");
            结束剧情();
        }
    }

    private static void 设置事件节点(剧情数据库.剧情蓝图节点 节点)
    {
        if (string.IsNullOrWhiteSpace(节点.事件ID))
        {
            Debug.LogWarning("剧情运行时：设置事件节点缺少事件ID。");
            return;
        }

        EventRuntimeState.SetState(节点.事件ID, 节点.事件状态);
    }

    private static void 添加物品到装备栏节点(剧情数据库.剧情蓝图节点 节点)
    {
        if (string.IsNullOrWhiteSpace(节点.角色ID))
        {
            Debug.LogError("剧情运行时：添加物品到装备栏节点缺少角色ID。");
            return;
        }

        if (string.IsNullOrWhiteSpace(节点.物品ID))
        {
            Debug.LogError("剧情运行时：添加物品到装备栏节点缺少物品ID。");
            return;
        }

        if (节点.装备格子索引 < 0)
        {
            Debug.LogError($"剧情运行时：添加物品到装备栏节点的装备格子索引不合法：{节点.装备格子索引}。");
            return;
        }

        ItemDatabase 数据库 = ItemDatabase.LoadDefault();
        ItemDatabase.ItemEntry 物品 = 数据库 != null ? 数据库.FindEntry(节点.物品ID) : null;
        if (物品 == null)
        {
            Debug.LogError($"剧情运行时：添加物品到装备栏节点找不到物品“{节点.物品ID}”。");
            return;
        }

        InventoryShortcutRuntimeBinder.ItemSlotData 格子数据 = new InventoryShortcutRuntimeBinder.ItemSlotData
        {
            itemId = 节点.物品ID.Trim(),
            icon = 物品显示辅助服务.解析预制体显示图标(物品.prefab, 查找直接子物体, 查找后代物体),
            count = 1,
            maxStack = 1,
            isRotated = false,
            isFootprintExtension = false,
            primarySlotIndex = -1
        };

        if (!InventoryShortcutRuntimeBinder.TrySetEquipmentSlotData(节点.角色ID.Trim(), 节点.装备格子索引, 格子数据))
        {
            Debug.LogError($"剧情运行时：无法把物品“{节点.物品ID}”写入角色“{节点.角色ID}”的装备格子 {节点.装备格子索引}。");
        }
    }

    private IEnumerator 黑幕渐变协程(剧情数据库.剧情蓝图节点 节点, bool 淡入)
    {
        CanvasGroup 目标黑幕组 = 确保黑幕();
        float 开始透明度 = 淡入 ? 0f : 目标黑幕组.alpha;
        float 结束透明度 = 淡入 ? Mathf.Clamp01(节点.目标不透明度) : 0f;
        float 持续时间 = Mathf.Max(0f, 节点.持续时间);

        目标黑幕组.alpha = 开始透明度;
        if (持续时间 <= 0f)
        {
            目标黑幕组.alpha = 结束透明度;
        }
        else
        {
            float 已过时间 = 0f;
            while (已过时间 < 持续时间)
            {
                已过时间 += Time.unscaledDeltaTime;
                float 进度 = Mathf.Clamp01(已过时间 / 持续时间);
                目标黑幕组.alpha = Mathf.Lerp(开始透明度, 结束透明度, 进度);
                yield return null;
            }
        }

        目标黑幕组.alpha = 结束透明度;
        if (!淡入)
        {
            清理黑幕();
        }
    }

    private void 角色播放动画节点(剧情数据库.剧情蓝图节点 节点)
    {
        if (string.IsNullOrWhiteSpace(节点.角色ID))
        {
            Debug.LogError("剧情运行时：角色播放动画节点缺少角色ID。");
            return;
        }

        if (节点.动作控制器 == null)
        {
            Debug.LogError($"剧情运行时：角色“{节点.角色ID}”播放动画节点缺少动作控制器。");
            return;
        }

        if (string.IsNullOrWhiteSpace(节点.动画状态名))
        {
            Debug.LogError($"剧情运行时：角色“{节点.角色ID}”播放动画节点缺少动画状态名。");
            return;
        }

        BattleUnit 单位 = 查找战斗单位(节点.角色ID.Trim());
        if (单位 == null)
        {
            Debug.LogError($"剧情运行时：找不到角色“{节点.角色ID}”对应的战斗单位。");
            return;
        }

        Animator 动画器 = 单位.GetComponentInChildren<Animator>(true);
        if (动画器 == null)
        {
            Debug.LogError($"剧情运行时：角色“{节点.角色ID}”没有 Animator。");
            return;
        }

        动画器.runtimeAnimatorController = 节点.动作控制器;
        单位.PlayAnimationState(节点.动画状态名.Trim());
    }

    private IEnumerator 播放已配置动作节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        if (string.IsNullOrWhiteSpace(节点.角色ID))
        {
            Debug.LogError("剧情运行时：播放已配置动作节点缺少角色ID。");
            yield break;
        }

        BattleUnit 单位 = 查找战斗单位(节点.角色ID.Trim());
        if (单位 == null)
        {
            Debug.LogError($"剧情运行时：找不到角色“{节点.角色ID}”对应的战斗单位。");
            yield break;
        }

        string 动画状态名;
        AudioClip 音效;
        GameObject 音效预制体;
        bool 位移补偿;
        int 音效延迟帧 = 0;

        if (节点.动作来源 == 剧情数据库.已配置动作来源.技能动作栏)
        {
            if (string.IsNullOrWhiteSpace(节点.技能ID))
            {
                Debug.LogError("剧情运行时：播放已配置动作节点缺少技能ID。");
                yield break;
            }

            BattleSkillDatabase 技能数据库 = BattleSkillDatabase.LoadDefault();
            BattleSkillDatabase.SkillEntry 技能 =
                技能数据库 != null ? 技能数据库.FindEntry(节点.技能ID.Trim()) : null;
            if (技能 == null)
            {
                Debug.LogError($"剧情运行时：找不到技能“{节点.技能ID}”。");
                yield break;
            }

            战斗技能动作解析服务 解析服务 = new 战斗技能动作解析服务();
            动画状态名 = 解析服务.解析动作状态名(技能, 单位);
            音效 = 解析服务.解析动作音效(技能, 单位);
            音效预制体 = 解析服务.解析动作音效预制体(技能, 单位);
            位移补偿 = 解析服务.解析动作位移补偿(技能, 单位);
            音效延迟帧 = 解析服务.解析音效延迟帧(技能, 单位);
        }
        else
        {
            解析全局动作(
                节点.全局动作,
                单位.characterId,
                out 动画状态名,
                out 音效,
                out 音效预制体,
                out 位移补偿);
        }

        if (string.IsNullOrWhiteSpace(动画状态名))
        {
            Debug.LogError($"剧情运行时：角色“{节点.角色ID}”的已配置动作没有可用动画。");
            yield break;
        }

        单位.PlayAnimationState(动画状态名, 位移补偿);
        if (音效延迟帧 > 0)
        {
            yield return new WaitForSeconds(音效延迟帧 / 60f);
        }

        BattleAudioUtility.PlayOnce(音效, 音效预制体, 单位, Camera.main);
    }

    private IEnumerator 角色转向节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        if (string.IsNullOrWhiteSpace(节点.角色ID))
        {
            Debug.LogError("剧情运行时：角色转向节点缺少角色ID。");
            yield break;
        }

        BattleUnit 单位 = 查找战斗单位(节点.角色ID.Trim());
        if (单位 == null)
        {
            Debug.LogError($"剧情运行时：找不到角色“{节点.角色ID}”对应的战斗单位。");
            yield break;
        }

        float 基础角度;
        switch (节点.朝向)
        {
            case 剧情数据库.模型朝向.东:
                基础角度 = 90f;
                break;
            case 剧情数据库.模型朝向.南:
                基础角度 = 180f;
                break;
            case 剧情数据库.模型朝向.西:
                基础角度 = 270f;
                break;
            default:
                基础角度 = 0f;
                break;
        }

        Vector3 目标欧拉角 = 单位.transform.eulerAngles;
        目标欧拉角.y = Mathf.Repeat(
            基础角度 + BattleAnimationSettingsResolver.ResolveIdleYawOffset(),
            360f);
        Quaternion 目标旋转 = Quaternion.Euler(目标欧拉角);
        float 每秒角度 = 90f / BattleAnimationSettingsResolver.ResolveModelTurn90Duration();

        while (Quaternion.Angle(单位.transform.rotation, 目标旋转) > 0.01f)
        {
            单位.transform.rotation = Quaternion.RotateTowards(
                单位.transform.rotation,
                目标旋转,
                每秒角度 * Time.unscaledDeltaTime);
            yield return null;
        }

        单位.transform.rotation = 目标旋转;
    }

    private static void 解析全局动作(
        剧情数据库.全局动作类型 动作类型,
        string 角色ID,
        out string 动画状态名,
        out AudioClip 音效,
        out GameObject 音效预制体,
        out bool 位移补偿)
    {
        switch (动作类型)
        {
            case 剧情数据库.全局动作类型.进战:
                动画状态名 = BattleAnimationSettingsResolver.ResolveEnterBattleStateName(角色ID);
                音效 = BattleAnimationSettingsResolver.ResolveEnterBattleSound(角色ID);
                音效预制体 = BattleAnimationSettingsResolver.ResolveEnterBattleSoundPrefab(角色ID);
                位移补偿 = BattleAnimationSettingsResolver.ResolveEnterBattleCompensateMotion(角色ID);
                return;
            case 剧情数据库.全局动作类型.退战:
                动画状态名 = BattleAnimationSettingsResolver.ResolveExitBattleStateName(角色ID);
                音效 = BattleAnimationSettingsResolver.ResolveExitBattleSound(角色ID);
                音效预制体 = BattleAnimationSettingsResolver.ResolveExitBattleSoundPrefab(角色ID);
                位移补偿 = BattleAnimationSettingsResolver.ResolveExitBattleCompensateMotion(角色ID);
                return;
            case 剧情数据库.全局动作类型.受击:
                动画状态名 = BattleAnimationSettingsResolver.ResolveHitReactionStateName(角色ID);
                音效 = BattleAnimationSettingsResolver.ResolveHitReactionSound(角色ID);
                音效预制体 = BattleAnimationSettingsResolver.ResolveHitReactionSoundPrefab(角色ID);
                位移补偿 = BattleAnimationSettingsResolver.ResolveHitReactionCompensateMotion(角色ID);
                return;
            case 剧情数据库.全局动作类型.闪避:
                动画状态名 = BattleAnimationSettingsResolver.ResolveDodgeStateName(角色ID);
                音效 = BattleAnimationSettingsResolver.ResolveDodgeSound(角色ID);
                音效预制体 = BattleAnimationSettingsResolver.ResolveDodgeSoundPrefab(角色ID);
                位移补偿 = BattleAnimationSettingsResolver.ResolveDodgeCompensateMotion(角色ID);
                return;
            case 剧情数据库.全局动作类型.探索待机:
                动画状态名 = BattleAnimationSettingsResolver.ResolveExplorationIdleStateName();
                音效 = BattleAnimationSettingsResolver.ResolveExplorationIdleSound();
                音效预制体 = BattleAnimationSettingsResolver.ResolveExplorationIdleSoundPrefab();
                位移补偿 = BattleAnimationSettingsResolver.ResolveExplorationIdleCompensateMotion();
                return;
            case 剧情数据库.全局动作类型.探索移动:
                动画状态名 = BattleAnimationSettingsResolver.ResolveExplorationMoveStateName();
                音效 = BattleAnimationSettingsResolver.ResolveExplorationMoveSound();
                音效预制体 = BattleAnimationSettingsResolver.ResolveExplorationMoveSoundPrefab();
                位移补偿 = BattleAnimationSettingsResolver.ResolveExplorationMoveCompensateMotion();
                return;
            default:
                动画状态名 = BattleAnimationSettingsResolver.ResolveIdleStateName(角色ID);
                音效 = BattleAnimationSettingsResolver.ResolveIdleSound(角色ID);
                音效预制体 = BattleAnimationSettingsResolver.ResolveIdleSoundPrefab(角色ID);
                位移补偿 = BattleAnimationSettingsResolver.ResolveIdleCompensateMotion(角色ID);
                return;
        }
    }

    private void 结束剧情()
    {
        当前剧情 = null;
        正在执行蓝图 = false;
        正在执行蓝图节点数 = 0;
        已开始蓝图节点.Clear();
        已完成蓝图节点.Clear();
    }

    private void 启动蓝图节点(剧情数据库.剧情蓝图节点 节点)
    {
        if (节点 == null || string.IsNullOrWhiteSpace(节点.节点ID) || 已开始蓝图节点.Contains(节点.节点ID))
        {
            return;
        }

        已开始蓝图节点.Add(节点.节点ID);
        正在执行蓝图节点数++;
        StartCoroutine(执行蓝图节点协程(节点));
    }

    private IEnumerator 执行蓝图节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        switch (节点.节点类型)
        {
            case 剧情数据库.剧情蓝图节点类型.开始:
            case 剧情数据库.剧情蓝图节点类型.汇合:
                yield return null;
                break;
            case 剧情数据库.剧情蓝图节点类型.播放一句对话:
                yield return 播放单句对话蓝图节点协程(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.播放对话组:
                if (取得输出节点ID列表(节点.节点ID).Count <= 0)
                {
                    yield return 播放对话组蓝图节点协程(节点);
                }
                break;
            case 剧情数据库.剧情蓝图节点类型.播放一句小对话:
                yield return 播放单句小对话蓝图节点协程(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.播放小对话组:
                yield return 播放小对话组蓝图节点协程(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.设置事件:
                设置事件节点(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.切换场景:
                yield return 切换场景蓝图节点协程(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.添加物品到装备栏:
                添加物品到装备栏节点(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.黑幕淡入:
                yield return 黑幕渐变协程(节点, true);
                break;
            case 剧情数据库.剧情蓝图节点类型.黑幕淡出:
                yield return 黑幕渐变协程(节点, false);
                break;
            case 剧情数据库.剧情蓝图节点类型.角色播放动画:
                角色播放动画节点(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.播放已配置动作:
                yield return 播放已配置动作节点协程(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.角色转向:
                yield return 角色转向节点协程(节点);
                break;
            case 剧情数据库.剧情蓝图节点类型.隐藏界面:
                Canvas界面显隐服务.隐藏普通界面();
                while (Canvas界面显隐服务.正在播放动画)
                {
                    yield return null;
                }
                break;
            case 剧情数据库.剧情蓝图节点类型.显示界面:
                Canvas界面显隐服务.显示普通界面();
                while (Canvas界面显隐服务.正在播放动画)
                {
                    yield return null;
                }
                break;
            case 剧情数据库.剧情蓝图节点类型.等待:
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, 节点.持续时间));
                break;
            default:
                Debug.LogWarning($"剧情运行时：未处理的剧情蓝图节点类型“{节点.节点类型}”。");
                break;
        }

        完成蓝图节点(节点);
    }

    private IEnumerator 播放对话组蓝图节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        bool 已结束 = false;
        if (!对话运行时.播放对话组并等待(节点.对话组ID, () => 已结束 = true))
        {
            Debug.LogWarning($"剧情运行时：播放对话组失败：{节点.对话组ID}");
            yield break;
        }

        while (!已结束)
        {
            yield return null;
        }
    }

    private IEnumerator 播放单句对话蓝图节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        bool 已结束 = false;
        if (!对话运行时.播放对话内容并等待(节点.对话内容ID, () => 已结束 = true))
        {
            Debug.LogWarning($"剧情运行时：播放对话内容失败：{节点.对话内容ID}");
            yield break;
        }

        while (!已结束)
        {
            yield return null;
        }
    }

    private IEnumerator 播放单句小对话蓝图节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        bool 已结束 = false;
        if (!小对话运行时.播放内容并等待(节点.小对话内容ID, () => 已结束 = true))
        {
            Debug.LogWarning($"剧情运行时：播放小对话内容失败：{节点.小对话内容ID}");
            yield break;
        }

        while (!已结束)
        {
            yield return null;
        }
    }

    private IEnumerator 播放小对话组蓝图节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        bool 已结束 = false;
        if (!小对话运行时.播放对话组并等待(节点.小对话组ID, () => 已结束 = true))
        {
            Debug.LogWarning($"剧情运行时：播放小对话组失败：{节点.小对话组ID}");
            yield break;
        }

        while (!已结束)
        {
            yield return null;
        }
    }

    private IEnumerator 切换场景蓝图节点协程(剧情数据库.剧情蓝图节点 节点)
    {
        if (节点.目标类型 == 剧情数据库.场景目标类型.战斗副本)
        {
            if (!出生剧情入口数据.尝试应用(当前剧情, 节点))
            {
                出生剧情入口数据.登记战斗副本入口(节点);
            }
        }

        if (string.IsNullOrWhiteSpace(节点.场景名))
        {
            Debug.LogWarning("剧情运行时：切换场景蓝图节点缺少场景名。");
            yield break;
        }

        AsyncOperation 操作 = SceneManager.LoadSceneAsync(节点.场景名.Trim());
        if (操作 != null)
        {
            while (!操作.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }

    private void 完成蓝图节点(剧情数据库.剧情蓝图节点 节点)
    {
        if (节点 == null || string.IsNullOrWhiteSpace(节点.节点ID))
        {
            return;
        }

        已完成蓝图节点.Add(节点.节点ID);
        正在执行蓝图节点数 = Mathf.Max(0, 正在执行蓝图节点数 - 1);

        List<string> 输出节点ID列表 = 取得输出节点ID列表(节点.节点ID);
        for (int i = 0; i < 输出节点ID列表.Count; i++)
        {
            string 目标节点ID = 输出节点ID列表[i];
            剧情数据库.剧情蓝图节点 目标节点 = 查找蓝图节点(目标节点ID);
            if (目标节点 == null || 已开始蓝图节点.Contains(目标节点ID))
            {
                continue;
            }

            if (蓝图节点前置已完成(目标节点ID))
            {
                启动蓝图节点(目标节点);
            }
        }

        if (正在执行蓝图 && 正在执行蓝图节点数 <= 0)
        {
            结束剧情();
        }
    }

    private bool 蓝图节点前置已完成(string 节点ID)
    {
        List<string> 输入节点ID列表 = 取得输入节点ID列表(节点ID);
        for (int i = 0; i < 输入节点ID列表.Count; i++)
        {
            if (!已完成蓝图节点.Contains(输入节点ID列表[i]))
            {
                return false;
            }
        }

        return true;
    }

    private 剧情数据库.剧情蓝图节点 查找蓝图节点(string 节点ID)
    {
        if (当前剧情 == null || 当前剧情.蓝图节点列表 == null || string.IsNullOrWhiteSpace(节点ID))
        {
            return null;
        }

        for (int i = 0; i < 当前剧情.蓝图节点列表.Count; i++)
        {
            剧情数据库.剧情蓝图节点 节点 = 当前剧情.蓝图节点列表[i];
            if (节点 != null && string.Equals(节点.节点ID, 节点ID, System.StringComparison.Ordinal))
            {
                return 节点;
            }
        }

        return null;
    }

    private List<string> 取得输入节点ID列表(string 节点ID)
    {
        List<string> 结果 = new List<string>();
        if (当前剧情 == null || 当前剧情.蓝图连线列表 == null)
        {
            return 结果;
        }

        for (int i = 0; i < 当前剧情.蓝图连线列表.Count; i++)
        {
            剧情数据库.剧情蓝图连线 连线 = 当前剧情.蓝图连线列表[i];
            if (连线 != null &&
                string.Equals(连线.目标节点ID, 节点ID, System.StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(连线.来源节点ID))
            {
                结果.Add(连线.来源节点ID);
            }
        }

        return 结果;
    }

    private List<string> 取得输出节点ID列表(string 节点ID)
    {
        List<string> 结果 = new List<string>();
        if (当前剧情 == null || 当前剧情.蓝图连线列表 == null)
        {
            return 结果;
        }

        for (int i = 0; i < 当前剧情.蓝图连线列表.Count; i++)
        {
            剧情数据库.剧情蓝图连线 连线 = 当前剧情.蓝图连线列表[i];
            if (连线 != null &&
                string.Equals(连线.来源节点ID, 节点ID, System.StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(连线.目标节点ID))
            {
                结果.Add(连线.目标节点ID);
            }
        }

        return 结果;
    }

    private CanvasGroup 确保黑幕()
    {
        if (黑幕组 != null && 黑幕图片 != null)
        {
            return 黑幕组;
        }

        GameObject 画布物体 = new GameObject("剧情黑幕画布", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(画布物体);
        Canvas 画布 = 画布物体.GetComponent<Canvas>();
        画布.renderMode = RenderMode.ScreenSpaceOverlay;
        画布.sortingOrder = 29000;

        GameObject 黑幕物体 = new GameObject("剧情黑幕", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        黑幕物体.transform.SetParent(画布物体.transform, false);
        RectTransform 矩形 = 黑幕物体.GetComponent<RectTransform>();
        矩形.anchorMin = Vector2.zero;
        矩形.anchorMax = Vector2.one;
        矩形.offsetMin = Vector2.zero;
        矩形.offsetMax = Vector2.zero;

        黑幕图片 = 黑幕物体.GetComponent<Image>();
        黑幕图片.color = Color.black;
        黑幕图片.raycastTarget = false;

        黑幕组 = 黑幕物体.GetComponent<CanvasGroup>();
        黑幕组.alpha = 0f;
        黑幕组.blocksRaycasts = false;
        黑幕组.interactable = false;
        return 黑幕组;
    }

    private void 清理黑幕()
    {
        if (黑幕组 == null)
        {
            return;
        }

        GameObject 画布物体 = 黑幕组.transform.parent != null ? 黑幕组.transform.parent.gameObject : 黑幕组.gameObject;
        Destroy(画布物体);
        黑幕组 = null;
        黑幕图片 = null;
    }

    private static BattleUnit 查找战斗单位(string 角色ID)
    {
        BattleUnit[] 单位列表 = FindObjectsOfType<BattleUnit>(true);
        for (int i = 0; i < 单位列表.Length; i++)
        {
            BattleUnit 单位 = 单位列表[i];
            if (单位 != null && string.Equals(单位.characterId, 角色ID, System.StringComparison.Ordinal))
            {
                return 单位;
            }
        }

        return null;
    }

    private static Transform 查找直接子物体(Transform 父物体, string 名称)
    {
        if (父物体 == null || string.IsNullOrWhiteSpace(名称))
        {
            return null;
        }

        for (int i = 0; i < 父物体.childCount; i++)
        {
            Transform 子物体 = 父物体.GetChild(i);
            if (子物体 != null && string.Equals(子物体.name, 名称, System.StringComparison.Ordinal))
            {
                return 子物体;
            }
        }

        return null;
    }

    private static Transform 查找后代物体(Transform 父物体, string 名称)
    {
        if (父物体 == null || string.IsNullOrWhiteSpace(名称))
        {
            return null;
        }

        for (int i = 0; i < 父物体.childCount; i++)
        {
            Transform 子物体 = 父物体.GetChild(i);
            if (子物体 == null)
            {
                continue;
            }

            if (string.Equals(子物体.name, 名称, System.StringComparison.Ordinal))
            {
                return 子物体;
            }

            Transform 后代 = 查找后代物体(子物体, 名称);
            if (后代 != null)
            {
                return 后代;
            }
        }

        return null;
    }
}
