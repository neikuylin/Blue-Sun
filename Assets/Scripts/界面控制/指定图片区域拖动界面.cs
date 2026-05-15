using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 指定图片区域拖动界面 : MonoBehaviour
{
    [SerializeField] private GameObject 拖动输入口;

    private RectTransform 移动目标;
    private RectTransform 目标父级;
    private Camera 拖动相机;
    private Vector2 起始锚点位置;
    private Vector2 指针起始父级坐标;
    private bool 正在拖动;

    private void Awake()
    {
        移动目标 = transform as RectTransform;
    }

    private void OnDisable()
    {
        正在拖动 = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            尝试开始拖动(Input.mousePosition);
        }

        if (!正在拖动)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            正在拖动 = false;
            return;
        }

        更新拖动位置(Input.mousePosition);
    }

    private void 尝试开始拖动(Vector2 屏幕坐标)
    {
        if (!获取拖动输入口矩形(out RectTransform 输入口矩形))
        {
            return;
        }

        if (移动目标 == null)
        {
            移动目标 = transform as RectTransform;
        }

        目标父级 = 移动目标 != null ? 移动目标.parent as RectTransform : null;
        if (移动目标 == null || 目标父级 == null)
        {
            return;
        }

        Camera 输入口相机 = 获取界面相机(输入口矩形);
        if (!RectTransformUtility.RectangleContainsScreenPoint(输入口矩形, 屏幕坐标, 输入口相机))
        {
            return;
        }

        拖动相机 = 获取界面相机(移动目标);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(目标父级, 屏幕坐标, 拖动相机, out 指针起始父级坐标))
        {
            return;
        }

        起始锚点位置 = 移动目标.anchoredPosition;
        正在拖动 = true;
    }

    private void 更新拖动位置(Vector2 屏幕坐标)
    {
        if (移动目标 == null || 目标父级 == null)
        {
            正在拖动 = false;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(目标父级, 屏幕坐标, 拖动相机, out Vector2 当前父级坐标))
        {
            return;
        }

        移动目标.anchoredPosition = 起始锚点位置 + 当前父级坐标 - 指针起始父级坐标;
    }

    private bool 获取拖动输入口矩形(out RectTransform 输入口矩形)
    {
        输入口矩形 = null;
        if (拖动输入口 == null)
        {
            return false;
        }

        Image 输入口图片 = 拖动输入口.GetComponent<Image>();
        if (输入口图片 == null)
        {
            return false;
        }

        输入口矩形 = 输入口图片.transform as RectTransform;
        return 输入口矩形 != null;
    }

    private static Camera 获取界面相机(RectTransform 矩形)
    {
        Canvas 画布 = 矩形 != null ? 矩形.GetComponentInParent<Canvas>() : null;
        if (画布 == null || 画布.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return 画布.worldCamera != null ? 画布.worldCamera : Camera.main;
    }
}
