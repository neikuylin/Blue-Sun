using UnityEngine;

[DisallowMultipleComponent]
public class 技能栏位绑定 : MonoBehaviour
{
    [Header("技能栏格子模板")]
    public RectTransform 记忆格;

    [Header("装备附带技能角标")]
    public Sprite 装备附带技能角标;
    public Vector2 装备附带技能角标位置 = new Vector2(-6f, -6f);

    public RectTransform ResolveSkillSlotContainer()
    {
        return transform as RectTransform;
    }

    public RectTransform ResolveSkillSlotTemplate()
    {
        return 记忆格;
    }

    public static 技能栏位绑定 FindBindingInActiveScene()
    {
        return FindObjectOfType<技能栏位绑定>(true);
    }
}
