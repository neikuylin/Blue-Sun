using UnityEngine;

[DisallowMultipleComponent]
public sealed class JourneySkillGridBinding : MonoBehaviour
{
    [Header("拖入拥有 GridLayoutGroup 的技能格容器")]
    public RectTransform skillSlotContainer;

    public RectTransform ResolveSkillSlotContainer()
    {
        if (skillSlotContainer != null)
        {
            return skillSlotContainer;
        }

        return transform as RectTransform;
    }

    public static RectTransform FindInActiveScene()
    {
        JourneySkillGridBinding binding = FindObjectOfType<JourneySkillGridBinding>(true);
        return binding != null ? binding.ResolveSkillSlotContainer() : null;
    }
}
