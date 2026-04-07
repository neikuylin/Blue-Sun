using UnityEngine;

public sealed class JourneySelectionCommitter : MonoBehaviour
{
    public void CaptureCurrentSelection()
    {
        CharacterSelectionState.CaptureFromCurrentScene();
    }
}
