using UnityEngine;

public sealed class 启程选择提交器 : MonoBehaviour
{
    public void 提交当前选择()
    {
        CharacterSelectionState.CaptureFromCurrentScene();
    }
}
