using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JourneySceneLoader : MonoBehaviour
{
    private const string BattleSceneName = "\u6218\u6597\u526F\u672C";

    public void LoadBattleScene()
    {
        CharacterSelectionState.CaptureFromCurrentScene();
        SceneManager.LoadScene(BattleSceneName);
    }
}
