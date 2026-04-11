using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class 界面刷新中心 : MonoBehaviour
{
    private static 界面刷新中心 instance;

    public static event Action 全部界面刷新;
    public static event Action<string> 当前角色切换刷新;
    public static event Action 仓储界面刷新;
    public static event Action<string> 技能装配变更;
    public static event Action<string> 装备变更;

    private string lastCurrentCharacterId = string.Empty;
    private Coroutine delayedSceneRefreshRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(界面刷新中心));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<界面刷新中心>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        lastCurrentCharacterId = ResolveCurrentCharacterId();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (delayedSceneRefreshRoutine != null)
        {
            StopCoroutine(delayedSceneRefreshRoutine);
            delayedSceneRefreshRoutine = null;
        }
    }

    private void Update()
    {
        string currentCharacterId = ResolveCurrentCharacterId();
        if (string.Equals(lastCurrentCharacterId, currentCharacterId, StringComparison.Ordinal))
        {
            return;
        }

        lastCurrentCharacterId = currentCharacterId;
        当前角色切换刷新?.Invoke(currentCharacterId);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (delayedSceneRefreshRoutine != null)
        {
            StopCoroutine(delayedSceneRefreshRoutine);
        }

        delayedSceneRefreshRoutine = StartCoroutine(DelayedSceneRefresh());
    }

    private IEnumerator DelayedSceneRefresh()
    {
        yield return null;
        delayedSceneRefreshRoutine = null;
        lastCurrentCharacterId = ResolveCurrentCharacterId();
        全部界面刷新?.Invoke();
        当前角色切换刷新?.Invoke(lastCurrentCharacterId);
    }

    public static void 请求刷新全部界面()
    {
        全部界面刷新?.Invoke();
    }

    public static void 请求刷新仓储界面()
    {
        仓储界面刷新?.Invoke();
    }

    public static void 请求技能装配变更(string characterId)
    {
        技能装配变更?.Invoke(NormalizeCharacterId(characterId));
    }

    public static void 请求装备变更(string characterId)
    {
        装备变更?.Invoke(NormalizeCharacterId(characterId));
    }

    private static string NormalizeCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId.Trim();
    }

    private static string ResolveCurrentCharacterId()
    {
        string currentCharacterId = 界面ID列表.当前ID;
        return string.IsNullOrWhiteSpace(currentCharacterId) ? string.Empty : currentCharacterId;
    }
}
