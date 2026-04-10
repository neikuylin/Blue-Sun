using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class 按钮文字颜色同步 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("绑定")]
    public Button 按钮;
    public TMP_Text 文字;

    private bool 鼠标悬停;
    private bool 正在按下;

    private void Reset()
    {
        自动绑定();
        刷新文字颜色();
    }

    private void Awake()
    {
        自动绑定();
        刷新文字颜色();
    }

    private void OnEnable()
    {
        自动绑定();
        刷新文字颜色();
    }

    private void OnValidate()
    {
        自动绑定();
        刷新文字颜色();
    }

    private void Update()
    {
        if (按钮 == null || 文字 == null)
        {
            return;
        }

        if (!按钮.interactable && 正在按下)
        {
            正在按下 = false;
            刷新文字颜色();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        鼠标悬停 = true;
        刷新文字颜色();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        鼠标悬停 = false;
        正在按下 = false;
        刷新文字颜色();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        正在按下 = true;
        刷新文字颜色();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        正在按下 = false;
        刷新文字颜色();
    }

    public void 刷新文字颜色()
    {
        if (按钮 == null || 文字 == null)
        {
            return;
        }

        ColorBlock colors = 按钮.colors;
        Color targetColor = colors.normalColor;

        if (!按钮.interactable)
        {
            targetColor = colors.disabledColor;
        }
        else if (正在按下)
        {
            targetColor = colors.pressedColor;
        }
        else if (鼠标悬停)
        {
            targetColor = colors.highlightedColor;
        }

        文字.color = targetColor * colors.colorMultiplier;
    }

    private void 自动绑定()
    {
        if (按钮 == null)
        {
            按钮 = GetComponent<Button>();
        }

        if (文字 == null)
        {
            文字 = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
