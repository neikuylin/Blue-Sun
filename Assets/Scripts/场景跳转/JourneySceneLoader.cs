using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JourneySceneLoader : MonoBehaviour
{
    private const string BattleSceneName = "战斗副本";

    public void LoadBattleScene()
    {
        CharacterSelectionState.CaptureFromCurrentScene();
        SceneManager.LoadScene(BattleSceneName);
    }
}
