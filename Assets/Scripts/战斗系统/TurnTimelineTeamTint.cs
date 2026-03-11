using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TurnTimelineTeamTint : MonoBehaviour
{
    public void Apply(Color color)
    {
        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }
}
