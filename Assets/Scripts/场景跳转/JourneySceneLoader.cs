using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JourneySceneLoader : MonoBehaviour
{
    public void Load20x20()
    {
        SceneManager.LoadScene("20x20");
    }
}
