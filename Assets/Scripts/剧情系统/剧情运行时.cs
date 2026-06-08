using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class 剧情运行时 : MonoBehaviour
{
    private static 剧情运行时 instance;

    private 剧情数据库.剧情条目 当前剧情;
    private int 当前步骤索引 = -1;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        事件剧情硬编码规则.请求播放剧情 -= 播放剧情;
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
        当前步骤索引 = 0;
        执行当前步骤();
        return true;
    }

    private void 执行当前步骤()
    {
        if (当前剧情 == null || 当前剧情.步骤列表 == null || 当前步骤索引 < 0 || 当前步骤索引 >= 当前剧情.步骤列表.Count)
        {
            结束剧情();
            return;
        }

        剧情数据库.剧情步骤 步骤 = 当前剧情.步骤列表[当前步骤索引];
        if (步骤 == null)
        {
            当前步骤索引++;
            执行当前步骤();
            return;
        }

        switch (步骤.步骤类型)
        {
            case 剧情数据库.剧情步骤类型.播放对话:
                播放对话步骤(步骤);
                break;
            case 剧情数据库.剧情步骤类型.设置事件:
                设置事件步骤(步骤);
                当前步骤索引++;
                执行当前步骤();
                break;
            case 剧情数据库.剧情步骤类型.切换场景:
                切换场景步骤(步骤);
                break;
            default:
                Debug.LogWarning($"剧情运行时：未处理的剧情步骤类型“{步骤.步骤类型}”。");
                当前步骤索引++;
                执行当前步骤();
                break;
        }
    }

    private void 播放对话步骤(剧情数据库.剧情步骤 步骤)
    {
        if (!对话运行时.播放对话组(步骤.对话组ID))
        {
            Debug.LogWarning($"剧情运行时：播放对话组失败：{步骤.对话组ID}");
        }

        当前步骤索引++;
        结束剧情();
    }

    private static void 设置事件步骤(剧情数据库.剧情步骤 步骤)
    {
        if (string.IsNullOrWhiteSpace(步骤.事件ID))
        {
            Debug.LogWarning("剧情运行时：设置事件步骤缺少事件ID。");
            return;
        }

        EventRuntimeState.SetState(步骤.事件ID, 步骤.事件状态);
    }

    private void 切换场景步骤(剧情数据库.剧情步骤 步骤)
    {
        if (步骤.目标类型 == 剧情数据库.场景目标类型.战斗副本)
        {
            if (!出生剧情入口数据.尝试应用(当前剧情, 步骤))
            {
                出生剧情入口数据.登记战斗副本入口(步骤);
            }
        }

        if (string.IsNullOrWhiteSpace(步骤.场景名))
        {
            Debug.LogWarning("剧情运行时：切换场景步骤缺少场景名。");
            当前步骤索引++;
            执行当前步骤();
            return;
        }

        SceneManager.LoadScene(步骤.场景名.Trim());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (当前剧情 == null || 当前步骤索引 < 0)
        {
            return;
        }

        StartCoroutine(场景加载后继续剧情());
    }

    private IEnumerator 场景加载后继续剧情()
    {
        yield return null;
        当前步骤索引++;
        执行当前步骤();
    }

    private void 结束剧情()
    {
        当前剧情 = null;
        当前步骤索引 = -1;
    }
}
