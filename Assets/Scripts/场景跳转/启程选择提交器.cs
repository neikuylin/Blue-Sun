using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class 启程选择提交器 : MonoBehaviour
{
    public void 提交当前选择()
    {
        string currentCharacterId = 界面ID列表.当前ID;
        Debug.Log(
            $"[SkillLoadoutDebug] JourneySubmit scene={SceneManager.GetActiveScene().name}, " +
            $"currentCharacterId={currentCharacterId}, state={CharacterSkillLoadoutDatabase.DescribeDatabaseEntry(currentCharacterId)}");
        CharacterSelectionState.CaptureFromCurrentScene();
    }
}
