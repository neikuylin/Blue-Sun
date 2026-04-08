using UnityEngine;

[DisallowMultipleComponent]
public class SkillBarBinding : MonoBehaviour
{
    [SerializeField]
    private RectTransform skillSlotArea;

    [SerializeField]
    private RectTransform skillPanel;

    [SerializeField]
    private RectTransform slotTemplate;

    [SerializeField]
    private Sprite grantedMarkerSprite;

    [SerializeField]
    private Vector2 grantedMarkerPosition = new Vector2(-6f, -6f);

    public RectTransform ResolveSkillPanel()
    {
        return skillPanel;
    }

    public RectTransform ResolveSkillSlotContainer()
    {
        return skillSlotArea;
    }

    public RectTransform ResolveSkillSlotTemplate()
    {
        return slotTemplate;
    }

    public Sprite GrantedMarkerSprite => grantedMarkerSprite;
    public Vector2 GrantedMarkerPosition => grantedMarkerPosition;

    public void SetAutoBindReferences(RectTransform panel, RectTransform slotArea)
    {
        skillPanel = panel;
        skillSlotArea = slotArea;
    }

    public static SkillBarBinding FindBindingInActiveScene()
    {
        return FindObjectOfType<SkillBarBinding>(true);
    }
}
