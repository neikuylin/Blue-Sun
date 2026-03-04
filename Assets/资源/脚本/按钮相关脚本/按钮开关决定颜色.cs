using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle), typeof(Image))]
public class ToggleIconVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Color offColor = new Color(0.55f, 0.55f, 0.55f, 1f); // 未选中灰
    public Color onColor  = Color.white;                        // 选中白
    [Range(0.5f, 1f)] public float hoverDarkenMultiplier = 0.85f; // 移上去变暗系数

    private Toggle toggle;
    private Image icon;
    private bool hovering;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        icon = GetComponent<Image>();

        toggle.onValueChanged.AddListener(_ => Refresh());
        Refresh();
    }

    void OnEnable() => Refresh();

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        Refresh();
    }

    void Refresh()
    {
        Color baseColor = toggle.isOn ? onColor : offColor;

        if (hovering)
            baseColor *= hoverDarkenMultiplier; // 在当前颜色基础上变暗

        baseColor.a = 1f;
        icon.color = baseColor;
    }
}