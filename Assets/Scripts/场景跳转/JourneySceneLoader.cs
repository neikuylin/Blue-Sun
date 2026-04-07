using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class JourneySceneLoader : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "\u6218\u6597\u526F\u672C";
#if UNITY_EDITOR
    [SerializeField] private SceneAsset targetScene;
#endif

    public void LoadBattleScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("JourneySceneLoader: targetSceneName is empty.");
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
