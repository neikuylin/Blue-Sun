using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TurnTimelineTeamTint : MonoBehaviour
{
    public void Apply(Color color)
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                images[i].color = color;
            }
        }
    }
}
