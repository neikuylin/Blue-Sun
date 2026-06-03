using UnityEngine;

[DisallowMultipleComponent]
public sealed class 副本选择状态 : MonoBehaviour
{
    private static 副本选择状态 instance;

    [SerializeField] private string 当前地图模板ID = string.Empty;

    public static string 当前选择地图模板ID => instance != null ? instance.当前地图模板ID : string.Empty;
    public static bool 已选择地图模板 => !string.IsNullOrWhiteSpace(当前选择地图模板ID);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("副本选择状态");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<副本选择状态>();
    }

    public static void 选择地图模板(string 地图模板ID)
    {
        if (instance == null)
        {
            Bootstrap();
        }

        if (instance == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(地图模板ID))
        {
            Debug.LogWarning("副本选择状态：地图模板ID为空，无法记录副本选择。");
            return;
        }

        string resolvedId = 地图模板ID.Trim();
        MapTemplateDatabase database = MapTemplateDatabase.LoadDefault();
        if (database == null || database.FindEntry(resolvedId) == null)
        {
            Debug.LogWarning($"副本选择状态：地图模板ID不存在：{resolvedId}");
            return;
        }

        instance.当前地图模板ID = resolvedId;
        BattleBootstrap.开始新的副本模板(resolvedId);
    }

    public static void 清空选择()
    {
        if (instance == null)
        {
            Bootstrap();
        }

        if (instance != null)
        {
            instance.当前地图模板ID = string.Empty;
        }
    }
}
