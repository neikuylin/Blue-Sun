using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JourneySceneLoader : MonoBehaviour
{
    public void Load20x20()
    {
        CharacterSelectionState.CaptureFromCurrentScene();
        SceneManager.LoadScene("20x20");
    }
}
