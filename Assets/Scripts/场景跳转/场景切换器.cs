using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class 场景切换器 : MonoBehaviour
{
    [Header("目标场景")]
    [SerializeField] private string targetSceneName = "\u6218\u6597\u526F\u672C";
#if UNITY_EDITOR
    [Header("拖入场景资源")]
    [SerializeField] private SceneAsset targetScene;
#endif

    public void 切换场景()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("场景切换器：targetSceneName 为空。");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetScene == null)
        {
            return;
        }

        if (!string.Equals(targetSceneName, targetScene.name, System.StringComparison.Ordinal))
        {
            targetSceneName = targetScene.name;
        }
    }
#endif
}
