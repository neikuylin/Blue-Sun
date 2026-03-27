using UnityEngine;

[DisallowMultipleComponent]
public sealed class JourneySkillGridBinding : MonoBehaviour
{
    [Header("拖入拥有 GridLayoutGroup 的技能格容器")]
    public RectTransform skillSlotContainer;

    [Header("装备附带技能角标")]
    public Sprite grantedSkillCornerSprite;
    public Vector2 grantedSkillCornerSize = new Vector2(18f, 18f);
    public Vector2 grantedSkillCornerAnchoredPosition = new Vector2(-6f, -6f);

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

    public static JourneySkillGridBinding FindBindingInActiveScene()
    {
        return FindObjectOfType<JourneySkillGridBinding>(true);
    }
}
