using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JourneySceneLoader : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "\u6218\u6597\u526F\u672C";

    public void LoadBattleScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("JourneySceneLoader: targetSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
