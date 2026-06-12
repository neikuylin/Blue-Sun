using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[AddComponentMenu("场景跳转/离开副本按钮")]
public sealed class 离开副本按钮 : MonoBehaviour
{
    private const string 清空副本事件ID = "清空副本";

    [Header("目标场景")]
    [SerializeField] private string 目标场景名 = "营地";

#if UNITY_EDITOR
    [Header("拖入场景资源")]
    [SerializeField] private SceneAsset 目标场景;
#endif

    public void 离开副本()
    {
        if (string.IsNullOrWhiteSpace(目标场景名))
        {
            Debug.LogWarning("离开副本按钮未配置目标场景。", this);
            return;
        }

        EventRuntimeState.SetState(清空副本事件ID, false);
        SceneManager.LoadScene(目标场景名);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (目标场景 != null)
        {
            目标场景名 = 目标场景.name;
        }
    }
#endif
}
