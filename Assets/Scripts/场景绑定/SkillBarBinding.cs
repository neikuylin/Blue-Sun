using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SkillBarBinding : MonoBehaviour
{
    [FormerlySerializedAs("skillSlotContainer")]
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
        return skillPanel != null ? skillPanel : transform as RectTransform;
    }

    public RectTransform ResolveSkillSlotContainer()
    {
        return skillSlotArea != null ? skillSlotArea : ResolveSkillPanel();
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
